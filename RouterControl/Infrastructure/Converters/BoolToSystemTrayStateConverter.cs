using System;
using System.Globalization;
using System.Windows.Data;

namespace RouterControl.Infrastructure.Converters
{
    internal class BoolToIconSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool connected)
                throw new InvalidOperationException();

            const string path = "Infrastructure/Resources/Icons/";

            return connected
                ? $"{path}ConnectionOn.ico"
                : $"{path}ConnectionOff.ico";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}