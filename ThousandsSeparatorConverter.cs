using System;
using System.Globalization;
using System.Windows.Data;

namespace BrokenHelper
{
    public class ThousandsSeparatorConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is IFormattable formattable)
                return formattable.ToString("N0", CultureInfo.InvariantCulture);

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
