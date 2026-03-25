using System.Security.Cryptography;
using System.Text;

namespace OpenClawClient.Core.Services;

/// <summary>
/// 加密服务接口
/// </summary>
public interface ICryptoService
{
    string Encrypt(string plainText, string key);
    string Decrypt(string cipherText, string key);
    byte[] EncryptFile(byte[] fileData, string key);
    byte[] DecryptFile(byte[] encryptedData, string key);
    string GenerateAesKey();
    string HashPassword(string password);
}

/// <summary>
/// AES-256-GCM 加密服务实现
/// </summary>
public class CryptoService : ICryptoService
{
    private const int KeySize = 256;
    private const int IvSize = 12; // GCM 推荐的 IV 长度
    private const int TagSize = 16; // GCM 认证标签长度

    public string Encrypt(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var keyBytes = Encoding.UTF8.GetBytes(key);
        using var aes = new AesGcm(keyBytes, tagSize: 16);
        var iv = new byte[IvSize];
        RandomNumberGenerator.Fill(iv);
        
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        aes.Encrypt(keyBytes, iv, plainBytes, cipherBytes, tag);

        // 组合：IV + Cipher + Tag
        var result = new byte[iv.Length + cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, result, iv.Length + cipherBytes.Length, tag.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText, string key)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        var keyBytes = Encoding.UTF8.GetBytes(key);
        using var aes = new AesGcm(keyBytes, tagSize: 16);
        var fullCipher = Convert.FromBase64String(cipherText);

        var iv = new byte[IvSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[fullCipher.Length - IvSize - TagSize];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, IvSize);
        Buffer.BlockCopy(fullCipher, IvSize + cipherBytes.Length, tag, 0, TagSize);
        Buffer.BlockCopy(fullCipher, IvSize, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];
        aes.Decrypt(keyBytes, iv, tag, cipherBytes, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public byte[] EncryptFile(byte[] fileData, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        using var aes = new AesGcm(keyBytes, tagSize: 16);
        var iv = new byte[IvSize];
        RandomNumberGenerator.Fill(iv);

        var cipherBytes = new byte[fileData.Length];
        var tag = new byte[TagSize];

        aes.Encrypt(keyBytes, iv, fileData, cipherBytes, tag);

        var result = new byte[iv.Length + cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, result, iv.Length + cipherBytes.Length, tag.Length);

        return result;
    }

    public byte[] DecryptFile(byte[] encryptedData, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        using var aes = new AesGcm(keyBytes, tagSize: 16);

        var iv = new byte[IvSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[encryptedData.Length - IvSize - TagSize];

        Buffer.BlockCopy(encryptedData, 0, iv, 0, IvSize);
        Buffer.BlockCopy(encryptedData, IvSize + cipherBytes.Length, tag, 0, TagSize);
        Buffer.BlockCopy(encryptedData, IvSize, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];
        aes.Decrypt(keyBytes, iv, tag, cipherBytes, plainBytes);

        return plainBytes;
    }

    public string GenerateAesKey()
    {
        var key = new byte[KeySize / 8];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }
}
