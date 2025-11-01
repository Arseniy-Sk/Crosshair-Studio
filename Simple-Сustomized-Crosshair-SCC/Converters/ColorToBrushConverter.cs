using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Simple_Customized_Crosshair_SCC.Converters
{
    /// <summary>
    /// Конвертер Color в Brush
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Color color)
            {
                return new SolidColorBrush(color);
            }
            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return System.Windows.Media.Colors.Transparent;
        }
    }
}