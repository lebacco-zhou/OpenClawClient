using System.IO;
using System.Windows;
using OpenClawClient.Core.Models;
using OpenClawClient.Core.Services;

namespace OpenClawClient.UI.Views;

/// <summary>
/// LoginView.xaml 的交互逻辑
/// </summary>
public partial class LoginView : Window
{
    private readonly IConfigService _configService;
    private LoginConfig? _savedConfig;

    public LoginView()
    {
        InitializeComponent();
        _configService = new ConfigService();
        
        // 加载保存的配置
        _ = LoadSavedConfigAsync();
    }

    private async Task LoadSavedConfigAsync()
    {
        _savedConfig = await _configService.LoadConfigAsync();
        
        if (_savedConfig != null)
        {
            ServerUrlTextBox.Text = _savedConfig.ServerUrl;
            // 如果用户选择记住登录，则填充Token
            if (_savedConfig.RememberLogin)
            {
                TokenPasswordBox.Password = _savedConfig.GatewayToken;
            }
        }
        else
        {
            // 默认服务器地址 - 中间件 WebSocket
            ServerUrlTextBox.Text = "wss://www.lebacco.cn:8445";
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(ServerUrlTextBox.Text))
        {
            ShowError("请输入服务器地址");
            return;
        }

        var token = TokenPasswordBox.Password;
        if (string.IsNullOrWhiteSpace(token))
        {
            ShowError("请输入 Gateway Token");
            return;
        }

        // 准备配置 - 使用默认值
        var config = new LoginConfig
        {
            ServerUrl = ServerUrlTextBox.Text.Trim(),
            GatewayToken = token,
            SelectedModel = "qwen3.5-plus", // 默认模型
            DownloadPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "OpenClaw"), // 默认下载路径
            AutoSubfolder = true, // 默认自动创建月度子文件夹
            RememberLogin = false // 默认不记住，除非用户在设置中启用
        };

        // 尝连接
        LoginButton.IsEnabled = false;
        LoginButton.Content = "连接中...";
        ErrorTextBlock.Visibility = Visibility.Collapsed;

        try
        {
            // 记录登录尝试
            ClientLogger.LogInfo($"Login attempt - Server: {config.ServerUrl}, Token length: {config.GatewayToken?.Length ?? 0}");
            
            // 使用网络服务工厂
            var networkService = NetworkServiceFactory.CreateNetworkService();
            
            // 验证并修正服务器 URL
            var serverUrl = config.ServerUrl;
            if (serverUrl.StartsWith("https://") || serverUrl.StartsWith("http://"))
            {
                // 如果用户输入了 HTTPS/HTTP，自动转换为 WSS/WS
                serverUrl = serverUrl.Replace("https://", "wss://").Replace("http://", "ws://");
            }
            
            ClientLogger.LogInfo($"Connecting to: {serverUrl}");
            var success = await networkService.ConnectAsync(serverUrl, config.GatewayToken);

            if (success)
            {
                // 打开主聊天窗口
                ClientLogger.LogInfo("Login successful, opening chat window");
                var chatWindow = new ChatWindow(config, networkService);
                chatWindow.Show();
                this.Close();
            }
            else
            {
                ClientLogger.LogError("Login failed");
                ShowError("连接失败，请检查服务器地址和 Token 是否正确");
            }
        }
        catch (Exception ex)
        {
            ShowError($"连接错误：{ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Content = "登 录";
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // 打开设置窗口
        var settingsWindow = new SettingsView(_savedConfig ?? new LoginConfig());
        settingsWindow.ShowDialog();
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }
}