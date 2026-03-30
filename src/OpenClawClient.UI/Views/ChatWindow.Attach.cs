using System.Windows;
using System.Windows.Input;
using OpenClawClient.Core.Services;

namespace OpenClawClient.UI.Views;

public partial class ChatWindow : Window
{
    private readonly IConfigService _configService = new ConfigService();

    private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // TODO: 打开图片查看器或下载图片
        MessageBox.Show("图片预览功能 - Phase 3 实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
