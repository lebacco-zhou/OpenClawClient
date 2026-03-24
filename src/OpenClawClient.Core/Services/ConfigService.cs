using System.Security.Cryptography;
using System.Text.Json;
using OpenClawClient.Core.Models;

namespace OpenClawClient.Core.Services;

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigService
{
    Task<LoginConfig?> LoadConfigAsync();
    Task SaveConfigAsync(LoginConfig config);
    Task ClearConfigAsync();
    string GetConfigPath();
}

/// <summary>
/// 配置服务实现 - 支持安装包版和绿色版
/// </summary>
public class ConfigService : IConfigService
{
    private readonly string _configPath;
    private readonly string _portableConfigPath;

    public ConfigService()
    {
        // 绿色版配置路径：./config/appsettings.json
        var baseDir = AppContext.BaseDirectory;
        _portableConfigPath = Path.Combine(baseDir, "config", "appsettings.json");
        
        // 安装包版配置路径：%APPDATA%\OpenClawClient\appsettings.json
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configPath = Path.Combine(appDataPath, "OpenClawClient", "appsettings.json");
    }

    public string GetConfigPath()
    {
        return File.Exists(_portableConfigPath) ? _portableConfigPath : _configPath;
    }

    public async Task<LoginConfig?> LoadConfigAsync()
    {
        var path = GetConfigPath();
        
        if (!File.Exists(path))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var config = JsonSerializer.Deserialize<LoginConfig>(json);
            
            // DPAPI 解密敏感字段
            if (config != null && !string.IsNullOrEmpty(config.GatewayToken))
            {
                config.GatewayToken = DecryptString(config.GatewayToken);
            }
            if (config != null && !string.IsNullOrEmpty(config.AesKey))
            {
                config.AesKey = DecryptString(config.AesKey);
            }
            
            return config;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task SaveConfigAsync(LoginConfig config)
    {
        var path = GetConfigPath();
        var dir = Path.GetDirectoryName(path)!;
        
        Directory.CreateDirectory(dir);

        // DPAPI 加密敏感字段
        var configToSave = new LoginConfig
        {
            ServerUrl = config.ServerUrl,
            GatewayToken = EncryptString(config.GatewayToken),
            AesKey = config.AesKey != null ? EncryptString(config.AesKey) : null,
            DownloadPath = config.DownloadPath,
            AutoSubfolder = config.AutoSubfolder,
            RememberLogin = config.RememberLogin,
            WindowPosition = config.WindowPosition
        };

        var json = JsonSerializer.Serialize(configToSave, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        await File.WriteAllTextAsync(path, json);
    }

    public async Task ClearConfigAsync()
    {
        var path = GetConfigPath();
        
        if (File.Exists(path))
        {
            await Task.Run(() => File.Delete(path));
        }
    }

    private static string EncryptString(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(
            plainBytes, 
            null, 
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    private static string DecryptString(string encryptedText)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedText);
        var plainBytes = ProtectedData.Unprotect(
            encryptedBytes, 
            null, 
            DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
