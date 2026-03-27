using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using OpenClawClient.Core.Models;

namespace OpenClawClient.UI.Converters;

public class MessageTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageType messageType)
        {
            return messageType switch
            {
                MessageType.File or MessageType.Image => Visibility.Visible,
                _ => Visibility.Collapsed
            };
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}