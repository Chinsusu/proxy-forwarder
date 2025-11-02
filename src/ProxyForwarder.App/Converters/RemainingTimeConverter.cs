using System;
using System.Globalization;
using System.Windows.Data;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.App.Converters;

public class RemainingTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ProxyRecord proxy)
            return "N/A";
        
        return proxy.GetRemainingTime();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
