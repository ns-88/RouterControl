using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RouterControl.Infrastructure.Converters
{
    internal class EnumToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not InternetConnectionStates state)
                throw new InvalidOperationException();

            return state switch
            {
                InternetConnectionStates.Undefined => Brushes.Yellow,
                InternetConnectionStates.Connected => Brushes.PaleGreen,
                InternetConnectionStates.NotConnected => Brushes.PaleVioletRed,
                _ => throw new InvalidOperationException()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}