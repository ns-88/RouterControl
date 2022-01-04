using System;
using System.Text;

namespace RouterControl.Infrastructure.Extensions
{
    using Utilities;

    internal static class ExceptionExtension
    {
        public static string CreateErrorText(this Exception exception)
        {
            Guard.ThrowIfNull(exception, nameof(exception));

            var builder = new StringBuilder();
            
            while (exception != null!)
            {
                var message = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Текст ошибки не задан."
                    : exception.Message;

                builder.AppendLine($"Ошибка: {message}");

                exception = exception.InnerException!;
            }

            return builder.ToString();
        }
    }
}