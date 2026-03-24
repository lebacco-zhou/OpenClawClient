using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenClawClient.Core.Models;

namespace OpenClawClient.UI.Views;

public partial class ChatWindow : Window
{
    private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Image image && image.Source is System.Windows.Media.Imaging.BitmapSource bitmap)
        {
            var viewer = new ImageViewerWindow(bitmap);
            viewer.Owner = this;
            viewer.ShowDialog();
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // TODO: 实现搜索过滤
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 打开搜索对话框
    }
}
