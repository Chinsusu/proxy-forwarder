using System;
using System.Globalization;
using System.Windows.Data;

namespace ProxyForwarder.App.Converters;

public class LocalPortConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int port && port > 0)
            return $"127.0.0.1:{port}";
        
        return "Not assigned";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
