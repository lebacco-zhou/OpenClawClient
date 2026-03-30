using System.Security.Cryptography;
using System.Text;

namespace OpenClawClient.Core.Services;

/// <summary>
/// 中间件加密服务 - AES-256-GCM + RSA
/// </summary>
public interface IMiddlewareCryptoService
{
    byte[] GenerateNonce(int size = 12);
    bool ValidateTimestamp(long timestamp, int maxAgeSeconds = 300);
    Task<(byte[] encryptedData, byte[] tag)> EncryptAsync(byte[] plaintext, byte[] key, byte[] nonce);
    Task<byte[]> DecryptAsync(byte[] encryptedData, byte[] key, byte[] nonce, byte[] tag);
    Task<byte[]> ImportSessionKeyAsync(string encryptedSessionKeyBase64);
    
    // Nonce 管理方法
    bool IsNonceUsed(byte[] nonce);
    void MarkNonceAsUsed(byte[] nonce);
}

public class MiddlewareCryptoService : IMiddlewareCryptoService
{
    private readonly RSA _rsa;
    private readonly HashSet<string> _usedNonces = new();
    private readonly object _nonceLock = new();

    public MiddlewareCryptoService()
    {
        // 加载中间件的 RSA 公钥
        // 注意：实际部署时应该从安全位置加载公钥
        _rsa = LoadMiddlewarePublicKey();
    }

    public byte[] GenerateNonce(int size = 12)
    {
        var nonce = new byte[size];
        RandomNumberGenerator.Fill(nonce);
        return nonce;
    }

    public bool ValidateTimestamp(long timestamp, int maxAgeSeconds = 300)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var diff = Math.Abs(now - timestamp);
        return diff <= (maxAgeSeconds * 1000L);
    }

    public async Task<(byte[] encryptedData, byte[] tag)> EncryptAsync(byte[] plaintext, byte[] key, byte[] nonce)
    {
        using var aes = new AesGcm(key);
        var tag = new byte[16]; // GCM 标签长度
        var encryptedData = new byte[plaintext.Length];
        
        await Task.Run(() => aes.Encrypt(nonce, plaintext, encryptedData, tag));
        
        return (encryptedData, tag);
    }

    public async Task<byte[]> DecryptAsync(byte[] encryptedData, byte[] key, byte[] nonce, byte[] tag)
    {
        using var aes = new AesGcm(key);
        var plaintext = new byte[encryptedData.Length];
        
        await Task.Run(() => aes.Decrypt(nonce, encryptedData, tag, plaintext));
        
        return plaintext;
    }

    public async Task<byte[]> ImportSessionKeyAsync(string encryptedSessionKeyBase64)
    {
        var encryptedKey = Convert.FromBase64String(encryptedSessionKeyBase64);
        return await Task.Run(() => _rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256));
    }

    private RSA LoadMiddlewarePublicKey()
    {
        // 从嵌入的公钥文件加载中间件的 RSA 公钥
        var publicKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MiddlewarePublicKey.pem");
        if (File.Exists(publicKeyPath))
        {
            var publicKey = File.ReadAllText(publicKeyPath);
            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);
            return rsa;
        }
        
        // 备用：如果公钥文件不存在，使用硬编码的公钥
        var hardcodedPublicKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAufLBAOm+MeRTJnXQRbwG
NsUQA9S2cQtXBtzqGdeTLXQUTkGH4gGvzVf+534S/lkdCRdc9JqDSzLemKi1x3sx
+Zvf64KtSt+6LcCIGrJdM9FuTfsQ10S8ifXIvsLkZnnmbxdpV6vgDTcYod3OzEMG
EhCzPWmQGvHtYmkVbkq8uv6UNyGJ7HCrgHfEGTpeghrzcQEZ8JCcJoyJxpvE8SeI
HJkiCz1BfMq4d6iuIx9KNuZzsIKtcyzc3u06UGdx93/8Zx5MY4F5a4yhZr0yIIz2
ymLdME1U22JnOgiIBpVGcyb6eUUwkEQxgI1/mG2/QiI4Pep58Y26Qn5UoFHLjkU4
/wIDAQAB
-----END PUBLIC KEY-----";
        
        var rsa = RSA.Create();
        rsa.ImportFromPem(hardcodedPublicKey);
        return rsa;
    }

    public bool IsNonceUsed(byte[] nonce)
    {
        lock (_nonceLock)
        {
            var nonceStr = Convert.ToBase64String(nonce);
            return _usedNonces.Contains(nonceStr);
        }
    }

    public void MarkNonceAsUsed(byte[] nonce)
    {
        lock (_nonceLock)
        {
            var nonceStr = Convert.ToBase64String(nonce);
            _usedNonces.Add(nonceStr);
            
            // 清理旧的 Nonce（保持最近1000个）
            if (_usedNonces.Count > 1000)
            {
                var oldest = _usedNonces.First();
                _usedNonces.Remove(oldest);
            }
        }
    }
}