using System;
using System.Globalization;
using System.Windows.Data;

namespace ProxyForwarder.App.Converters;

public class StatusToButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string status)
            return "Start";
        
        return status switch
        {
            "Running" => "Stop",
            "Starting..." => "Starting...",
            _ => "Start"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
