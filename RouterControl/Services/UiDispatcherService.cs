using System;
using System.Windows;
using System.Windows.Threading;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Services;

namespace RouterControl.Services
{
    internal class UiDispatcherService : IUiDispatcherService
    {
        private readonly Dispatcher _dispatcher;

        public UiDispatcherService()
        {
            _dispatcher = Application.Current.Dispatcher;
        }

        public void Invoke(Action action)
        {
            Guard.ThrowIfNull(action, nameof(action));

            if (!_dispatcher.CheckAccess())
                _dispatcher.Invoke(action);
            else
                action();
        }
    }
}