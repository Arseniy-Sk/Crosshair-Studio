using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CrosshairStudio.Presentation;

public sealed class ArgbToBrushConverter : IValueConverter
{
    public static readonly ArgbToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is uint argb)
            return new SolidColorBrush(Color.FromUInt32(argb));
        if (value is int i)
            return new SolidColorBrush(Color.FromUInt32(unchecked((uint)i)));
        return Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class EqualsConverter : IValueConverter
{
    public static readonly EqualsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Equals(value?.ToString(), parameter?.ToString());

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
