namespace OpenClawClient.Core.Models;

/// <summary>
/// 登录配置模型
/// </summary>
public class LoginConfig
{
    public string ServerUrl { get; set; } = "https://www.lebacco.cn:8443";
    public string GatewayToken { get; set; } = string.Empty;
    public string? AesKey { get; set; }
    public string SelectedModel { get; set; } = "qwen3.5-plus"; // 默认模型
    public string DownloadPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads", "OpenClaw");
    public bool AutoSubfolder { get; set; } = true; // 用于按月自动创建子文件夹
    public bool RememberLogin { get; set; } = true;
    public WindowPosition? WindowPosition { get; set; }
    public int ReconnectInterval { get; set; } = 10;
    public int MessageHistoryCount { get; set; } = 50;
}

public class WindowPosition
{
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double Width { get; set; } = 1200;
    public double Height { get; set; } = 800;
}
