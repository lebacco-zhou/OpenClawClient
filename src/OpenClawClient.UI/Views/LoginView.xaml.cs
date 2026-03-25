using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
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
            DownloadPathTextBox.Text = _savedConfig.DownloadPath;
            RememberLoginCheckBox.IsChecked = _savedConfig.RememberLogin;
            AutoSubfolderCheckBox.IsChecked = _savedConfig.AutoSubfolder;
            
            // 设置模型选择
            SetModelSelection(_savedConfig.SelectedModel);
        }
        else
        {
            // 默认下载路径
            DownloadPathTextBox.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "OpenClaw");
            
            // 默认模型选择
            SetModelSelection("qwen3.5-plus");
        }
    }
    
    private void SetModelSelection(string modelName)
    {
        switch (modelName.ToLower())
        {
            case "qwen3.5-plus":
                ModelComboBox.SelectedIndex = 0;
                break;
            case "qwen3-coder-plus":
                ModelComboBox.SelectedIndex = 1;
                break;
            case "qwen3-coder-max":
                ModelComboBox.SelectedIndex = 2;
                break;
            case "qwen3-max":
                ModelComboBox.SelectedIndex = 3;
                break;
            case "qwen2.5":
                ModelComboBox.SelectedIndex = 4;
                break;
            default:
                ModelComboBox.SelectedIndex = 0; // 默认 Qwen 3.5 Plus
                break;
        }
    }
    
    private string GetSelectedModel()
    {
        return ModelComboBox.SelectedIndex switch
        {
            0 => "qwen3.5-plus",
            1 => "qwen3-coder-plus", 
            2 => "qwen3-coder-max",
            3 => "qwen3-max",
            4 => "qwen2.5",
            5 => "custom-model", // Custom Model
            _ => "qwen3.5-plus" // 默认
        };
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择文件下载路径",
            InitialDirectory = DownloadPathTextBox.Text
        };

        if (dialog.ShowDialog() == true)
        {
            DownloadPathTextBox.Text = dialog.FolderName;
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

        if (string.IsNullOrWhiteSpace(DownloadPathTextBox.Text))
        {
            ShowError("请选择文件下载路径");
            return;
        }

        // 准备配置
        var config = new LoginConfig
        {
            ServerUrl = ServerUrlTextBox.Text.Trim(),
            GatewayToken = token,
            SelectedModel = GetSelectedModel(),
            DownloadPath = DownloadPathTextBox.Text,
            AutoSubfolder = AutoSubfolderCheckBox.IsChecked ?? true,
            RememberLogin = RememberLoginCheckBox.IsChecked ?? true
        };

        // 保存配置（如果选择记住登录）
        if (config.RememberLogin)
        {
            await _configService.SaveConfigAsync(config);
        }

        // 尝试连接
        LoginButton.IsEnabled = false;
        LoginButton.Content = "连接中...";
        ErrorTextBlock.Visibility = Visibility.Collapsed;

        try
        {
            var networkService = new NetworkService();
            var success = await networkService.ConnectAsync(config.ServerUrl, config.GatewayToken);

            if (success)
            {
                // 打开主聊天窗口
                var chatWindow = new ChatWindow(config, networkService);
                chatWindow.Show();
                this.Close();
            }
            else
            {
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

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }
}
