using System.IO;
using System.Windows;
using Microsoft.Win32;
using OpenClawClient.Core.Models;

namespace OpenClawClient.UI.Views;

/// <summary>
/// SettingsView.xaml 的交互逻辑
/// </summary>
public partial class SettingsView : Window
{
    private LoginConfig _config;

    public SettingsView()
    {
        InitializeComponent();
        _config = new LoginConfig();
        
        // 设置默认值
        ServerUrlTextBox.Text = "https://www.lebacco.cn:8443";
        DownloadPathTextBox.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "OpenClaw");
        ReconnectIntervalTextBox.Text = "10";
        MessageHistoryCountTextBox.Text = "50";
        AutoSubfolderByMonthCheckBox.IsChecked = true;
        RememberLoginCheckBox.IsChecked = false;
    }

    public SettingsView(LoginConfig config) : this()
    {
        _config = config;
        LoadConfig(config);
    }

    private void LoadConfig(LoginConfig config)
    {
        ServerUrlTextBox.Text = config.ServerUrl;
        DownloadPathTextBox.Text = config.DownloadPath;
        AutoSubfolderByMonthCheckBox.IsChecked = config.AutoSubfolder;
        RememberLoginCheckBox.IsChecked = config.RememberLogin;
        ReconnectIntervalTextBox.Text = config.ReconnectInterval.ToString();
        MessageHistoryCountTextBox.Text = config.MessageHistoryCount.ToString();
    }

    private LoginConfig SaveConfig()
    {
        _config.ServerUrl = ServerUrlTextBox.Text;
        _config.DownloadPath = DownloadPathTextBox.Text;
        _config.AutoSubfolder = AutoSubfolderByMonthCheckBox.IsChecked ?? true;
        _config.RememberLogin = RememberLoginCheckBox.IsChecked ?? false;
        _config.ReconnectInterval = int.TryParse(ReconnectIntervalTextBox.Text, out var reconnectInterval) ? reconnectInterval : 10;
        _config.MessageHistoryCount = int.TryParse(MessageHistoryCountTextBox.Text, out var messageHistoryCount) ? messageHistoryCount : 50;
        
        return _config;
    }

    private void BrowsePathButton_Click(object sender, RoutedEventArgs e)
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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var config = SaveConfig();
        
        // 如果选择记住登录，保存配置
        if (config.RememberLogin)
        {
            var configService = new Core.Services.ConfigService();
            _ = configService.SaveConfigAsync(config);
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}