using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace KvmDesktop.Converters;

public class FpsToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int fps)
        {
            if (fps >= 50) return Brushes.Lime;
            if (fps >= 20) return Brushes.Yellow;
            return Brushes.Red;
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
