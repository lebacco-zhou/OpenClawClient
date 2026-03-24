using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using OpenClawClient.Core.Services;

namespace OpenClawClient.UI.Views;

public partial class ChatWindow : Window
{
    private readonly IConfigService _configService = new ConfigService();

    private async void AttachButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择文件",
            Multiselect = true,
            Filter = "所有文件 (*.*)|*.*|图片文件 (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif|文档文件 (*.pdf;*.doc;*.docx;*.xls;*.xlsx)|*.pdf;*.doc;*.docx;*.xls;*.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                await SendFileAsync(file);
            }
        }
    }

    private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // TODO: 打开图片查看器或下载图片
        MessageBox.Show("图片预览功能 - Phase 3 实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
