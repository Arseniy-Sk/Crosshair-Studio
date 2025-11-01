using System;
using System.Globalization;
using System.Windows.Data;

namespace Simple_Customized_Crosshair_SCC.Converters
{
    public class CrosshairStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible)
            {
                return isVisible ? "Status: Crosshair Visible" : "Status: Ready";
            }
            return "Status: Ready";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}