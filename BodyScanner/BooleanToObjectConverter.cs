using System;
using System.Windows.Data;

namespace BodyScanner
{
    [ValueConversion(typeof(bool), typeof(object))]
    public class BooleanToObjectConverter : IValueConverter
    {
        public object TrueValue { get; set; }

        public object FalseValue { get; set; }

        public BooleanToObjectConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            return (bool)value == true ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Equals(value, TrueValue)
                ? true
                : Equals(value, FalseValue)
                    ? false
                    : (bool?)null;
        }
    }
}
