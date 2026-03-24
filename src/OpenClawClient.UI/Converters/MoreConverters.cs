using System.Globalization;
using System.Windows;
using System.Windows.Data;
using OpenClawClient.Core.Models;

namespace OpenClawClient.UI.Converters;

/// <summary>
/// 消息角色转对齐方式转换器
/// </summary>
public class RoleToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            return role switch
            {
                MessageRole.User => HorizontalAlignment.Right,
                MessageRole.Assistant => HorizontalAlignment.Left,
                MessageRole.System => HorizontalAlignment.Center,
                _ => HorizontalAlignment.Left
            };
        }
        return HorizontalAlignment.Left;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 文件类型转可见性转换器
/// </summary>
public class FileTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageType type)
        {
            return (type == MessageType.File || type == MessageType.Image)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 发送状态转图标转换器
/// </summary>
public class StatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DeliveryStatus status)
        {
            return status switch
            {
                DeliveryStatus.Pending => "⏳",
                DeliveryStatus.Sent => "✓",
                DeliveryStatus.Delivered => "✓✓",
                DeliveryStatus.Failed => "✕",
                _ => ""
            };
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
