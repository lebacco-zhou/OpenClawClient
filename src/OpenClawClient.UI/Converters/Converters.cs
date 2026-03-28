using System.Globalization;
using System.Windows;
using System.Windows.Data;
using OpenClawClient.Core.Models;

namespace OpenClawClient.UI.Converters;

/// <summary>
/// 消息角色转背景色转换器
/// </summary>
public class RoleToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            return role switch
            {
                MessageRole.User => Application.Current.FindResource("PrimaryHueLightBrush"),
                MessageRole.Assistant => Application.Current.FindResource("MaterialDesignPaper"),
                MessageRole.System => Application.Current.FindResource("MaterialDesignDivider"),
                _ => Application.Current.FindResource("MaterialDesignPaper")
            };
        }
        return Application.Current.FindResource("MaterialDesignPaper");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 连接状态转文本转换器
/// </summary>
public class ConnectionStateToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Core.Services.ConnectionState state)
        {
            return state switch
            {
                Core.Services.ConnectionState.Disconnected => "已断开",
                Core.Services.ConnectionState.Connecting => "连接中...",
                Core.Services.ConnectionState.Connected => "已连接",
                Core.Services.ConnectionState.Reconnecting => "重连中...",
                Core.Services.ConnectionState.Failed => "连接失败",
                _ => "未知"
            };
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 消息类型转图标转换器
/// </summary>
public class MessageTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageType type)
        {
            return type switch
            {
                MessageType.Text => "📝",
                MessageType.Image => "🖼️",
                MessageType.File => "📎",
                MessageType.System => "⚙️",
                _ => "📝"
            };
        }
        return "📝";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
