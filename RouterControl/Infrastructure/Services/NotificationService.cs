using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using AdonisUI.Controls;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

namespace RouterControl.Infrastructure.Services
{
    using Enums;
    using Interfaces.Infrastructure.Services;

    internal class NotificationService : INotificationService
    {
        private readonly Dispatcher _dispatcher;

        public NotificationService()
        {
            _dispatcher = Application.Current.Dispatcher;
        }

        private static Exception GetException<T>(T value) where T : Enum
        {
            return new InvalidOperationException($"Неизвестное значение перечисления \"{nameof(T)}\". Значение: \"{value}\".");
        }

        private static MessageBoxButton GetMessageBoxButton(NotificationButtons notificationButton)
        {
            return notificationButton switch
            {
                NotificationButtons.Ok => MessageBoxButton.OK,
                NotificationButtons.OkCancel => MessageBoxButton.OKCancel,
                NotificationButtons.YesNo => MessageBoxButton.YesNo,
                NotificationButtons.YesNoCancel => MessageBoxButton.YesNoCancel,
                _ => throw GetException(notificationButton)
            };
        }

        private static MessageBoxImage GetMessageBoxImage(NotificationImages notificationImage)
        {
            return notificationImage switch
            {
                NotificationImages.None => MessageBoxImage.None,
                NotificationImages.Asterisk => MessageBoxImage.Asterisk,
                NotificationImages.Error => MessageBoxImage.Error,
                NotificationImages.Exclamation => MessageBoxImage.Exclamation,
                NotificationImages.Hand => MessageBoxImage.Hand,
                NotificationImages.Information => MessageBoxImage.Information,
                NotificationImages.Question => MessageBoxImage.Question,
                NotificationImages.Stop => MessageBoxImage.Stop,
                NotificationImages.Warning => MessageBoxImage.Warning,
                _ => throw GetException(notificationImage)
            };
        }

        private static NotificationResult GetMessageBoxResult(MessageBoxResult messageBoxResult)
        {
            return messageBoxResult switch
            {
                MessageBoxResult.None => NotificationResult.None,
                MessageBoxResult.OK => NotificationResult.Ok,
                MessageBoxResult.Cancel => NotificationResult.Cancel,
                MessageBoxResult.Yes => NotificationResult.Yes,
                MessageBoxResult.No => NotificationResult.No,
                MessageBoxResult.Custom => throw GetException(messageBoxResult),
                _ => throw GetException(messageBoxResult)
            };
        }

        public NotificationResult Notify(string text, string caption, NotificationButtons notificationButton = NotificationButtons.Ok,
            NotificationImages notificationImage = NotificationImages.None)
        {
            return _dispatcher.Invoke(() =>
            {
                var activeWindow = Application.Current?.Windows.OfType<AdonisWindow>()
                                                               .FirstOrDefault(x => x.IsLoaded);

                var messageBoxButton = GetMessageBoxButton(notificationButton);
                var messageBoxImage = GetMessageBoxImage(notificationImage);

                var result = activeWindow != null
                    ? MessageBox.Show(activeWindow, text, caption, messageBoxButton, messageBoxImage)
                    : MessageBox.Show(text, caption, messageBoxButton, messageBoxImage);

                return GetMessageBoxResult(result);
            });
        }
    }
}