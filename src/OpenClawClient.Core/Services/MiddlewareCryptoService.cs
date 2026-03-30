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
        // TODO: 从配置或文件加载中间件的 RSA 公钥
        // 这里使用临时密钥对进行演示
        var rsa = RSA.Create();
        rsa.KeySize = 2048;
        
        // 在实际应用中，这里应该加载真实的公钥
        // 例如：rsa.ImportFromPem(File.ReadAllText("middleware-public-key.pem"));
        
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