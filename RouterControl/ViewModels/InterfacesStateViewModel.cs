using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace RouterControl.ViewModels
{
    using Infrastructure;
    using Infrastructure.Enums;
    using Infrastructure.Extensions;
    using Infrastructure.Utilities;
    using Interfaces.Infrastructure.Factories;
    using Interfaces.Infrastructure.Services;
    using Interfaces.Services;

    internal class InterfacesStateViewModel : DialogViewModelBase
    {
        private readonly CancellationTokenSource _cts;
        private readonly IRouterServicesFactory _routerServicesFactory;
        private readonly INotificationService _notificationService;
        private readonly IConnectionStateNotifierService _conStateNotifierService;
        private readonly IUiDispatcherService _dispatcherService;

        public ObservableCollection<RouterInterfaceViewModel> Interfaces { get; }

        public InterfacesStateViewModel(IRouterServicesFactory routerServicesFactory,
                                        INotificationService notificationService,
                                        IConnectionStateNotifierService conStateNotifierService,
                                        IUiDispatcherService dispatcherService)
            : base("Состояние интерфейсов")
        {
            Guard.ThrowIfNull(routerServicesFactory, out _routerServicesFactory, nameof(routerServicesFactory));
            Guard.ThrowIfNull(notificationService, out _notificationService, nameof(notificationService));
            Guard.ThrowIfNull(conStateNotifierService, out _conStateNotifierService, nameof(conStateNotifierService));
            Guard.ThrowIfNull(dispatcherService, out _dispatcherService, nameof(dispatcherService));

            _cts = new CancellationTokenSource();

            Interfaces = new ObservableCollection<RouterInterfaceViewModel>();
            IsBusy = true;
            ConnectionState = InternetConnectionStates.Undefined;
            RemoteIpAddress = "Получение...";

            _conStateNotifierService.StateChanged += ConnectionStateChanged;
        }

        #region IsBusy
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }
        #endregion

        #region ConnectionState
        private InternetConnectionStates _connectionState;
        public InternetConnectionStates ConnectionState
        {
            get => _connectionState;
            private set => SetProperty(ref _connectionState, value);
        }
        #endregion

        #region RemoteIpAddress
        private string? _remoteIpAddress;
        public string? RemoteIpAddress
        {
            get => _remoteIpAddress;
            private set => SetProperty(ref _remoteIpAddress, value);
        }
        #endregion

        private async void ConnectionStateChanged(object? sender, bool state)
        {
            _dispatcherService.Invoke(() =>
            {
                ConnectionState = state
                    ? InternetConnectionStates.Connected
                    : InternetConnectionStates.NotConnected;

                RemoteIpAddress = state ? "Получение..." : "Не задан";
            });

            if (!state)
                return;

            try
            {
                var ipAddress = await _conStateNotifierService.GetRemoteIpAddressAsync(_cts.Token).ConfigureAwait(false);

                _dispatcherService.Invoke(() => RemoteIpAddress = ipAddress);
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                _dispatcherService.Invoke(() => RemoteIpAddress = "Ошибка получения");

                _notificationService.Notify($"Удаленный IP-адрес не был получен.\r\n{ex.CreateErrorText()}", "Ошибка получения адреса",
                    notificationImage: NotificationImages.Error);
            }
        }

        private Task FillInterfacesListAsync(IRouterStateNotifierService routerStateNotifierService)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var interfaces = await routerStateNotifierService.GetInterfacesInfoAsync().ConfigureAwait(false);

                    foreach (var @interface in interfaces)
                    {
                        _dispatcherService.Invoke(() => Interfaces.Add(new RouterInterfaceViewModel(@interface, routerStateNotifierService, _dispatcherService)));
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.Notify($"Загрузка списка интерфейсов роутера не была произведена.\r\n{ex.CreateErrorText()}", "Ошибка получения данных",
                        notificationImage: NotificationImages.Error);
                }
                finally
                {
                    _dispatcherService.Invoke(() => IsBusy = false);
                }
            });
        }

        private void StartRouterStateNotifications(IRouterStateNotifierService routerStateNotifierService)
        {
            routerStateNotifierService.StartNotificationsAsync(_cts.Token).ContinueWith(x =>
            {
                if (x.Exception?.InnerException == null)
                    return;

                var ex = x.Exception.InnerException;

                _notificationService.Notify($"Состояние интерфейсов роутера не было получено.\r\n{ex.CreateErrorText()}", "Ошибка обновления данных",
                    notificationImage: NotificationImages.Error);
            });
        }

        private void StartConStateNotifications()
        {
            _conStateNotifierService.StartNotificationsAsync(_cts.Token).ContinueWith(x =>
            {
                if (x.Exception?.InnerException == null)
                    return;

                var ex = x.Exception.InnerException;

                _notificationService.Notify($"Проверка подключения к сети Интернет не была выполнена.\r\n{ex.CreateErrorText()}", "Ошибка проверки подключения",
                    notificationImage: NotificationImages.Error);
            });
        }

        protected override async void OnViewOpened()
        {
            var routerStateNotifierService = _routerServicesFactory.CreateStateNotifierService();

            await FillInterfacesListAsync(routerStateNotifierService).ConfigureAwait(false);

            StartRouterStateNotifications(routerStateNotifierService);

            StartConStateNotifications();
        }

        protected override void OnViewClosed()
        {
            _cts.Cancel();

            _conStateNotifierService.StateChanged -= ConnectionStateChanged;
            _conStateNotifierService.Dispose();

            foreach (var @interface in Interfaces)
            {
                @interface.Dispose();
            }

            Interfaces.Clear();
        }
    }
}