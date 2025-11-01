using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Simple_Customized_Crosshair_SCC.Models;

namespace Simple_Customized_Crosshair_SCC.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Обработка параметра для типа CrosshairType
            if (parameter is string parameterString)
            {
                if (Enum.TryParse<CrosshairType>(parameterString, out var expectedType))
                {
                    if (value is CrosshairType currentType)
                    {
                        return currentType == expectedType ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }

            // Стандартная логика для bool значений
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}