using System;
using System.Globalization;
using System.Windows.Data;

namespace Simple_Customized_Crosshair_SCC.Converters
{
    /// <summary>
    /// Конвертер для отображения enum в ComboBox
    /// </summary>
    public class EnumToCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Type enumType && enumType.IsEnum)
            {
                return Enum.GetValues(enumType);
            }
            return Array.Empty<object>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}