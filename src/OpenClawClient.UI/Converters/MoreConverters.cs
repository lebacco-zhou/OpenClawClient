using System.Globalization;
using System.Windows;
using System.Windows.Data;
using OpenClawClient.Core.Models;

namespace OpenClawClient.UI.Converters;

/// <summary>
/// 消息状态转符号转换器
/// </summary>
public class StatusToSymbolConverter : IValueConverter
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

/// <summary>
/// 消息角色转边框颜色转换器
/// </summary>
public class RoleToBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            return role switch
            {
                MessageRole.User => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 51, 153)), // Purple
                MessageRole.Assistant => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204)), // Gray
                MessageRole.System => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 180, 180)), // Light Gray
                _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204))
            };
        }
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}