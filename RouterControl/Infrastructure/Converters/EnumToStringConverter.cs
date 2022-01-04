using System;
using System.Globalization;
using System.Windows.Data;

namespace RouterControl.Infrastructure.Converters
{
    internal class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not InternetConnectionStates state)
                throw new InvalidOperationException();

            return state switch
            {
                InternetConnectionStates.Undefined => "Не определено",
                InternetConnectionStates.Connected => "Подключено",
                InternetConnectionStates.NotConnected => "Отсутствует",
                _ => throw new InvalidOperationException()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}