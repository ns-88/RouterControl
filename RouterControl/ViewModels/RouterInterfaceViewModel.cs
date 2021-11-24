using System;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Models;
using RouterControl.Interfaces.Services;

namespace RouterControl.ViewModels
{
    internal class RouterInterfaceViewModel : ViewModelBase, IDisposable
    {
        private readonly IDisposable? _subscriptionToken;
        private readonly IUiDispatcherService _dispatcherService;

        public RouterInterfaceViewModel(IRouterInterface routerInterface,
                                        IRouterStateObservableService stateNotifierService,
                                        IUiDispatcherService dispatcherService)
        {
            Guard.ThrowIfNull(routerInterface, nameof(routerInterface));
            Guard.ThrowIfNull(stateNotifierService, nameof(stateNotifierService));
            Guard.ThrowIfNull(dispatcherService, out _dispatcherService, nameof(dispatcherService));

            Update(routerInterface);

            _subscriptionToken = stateNotifierService.Subscribe(new Observer(Update), routerInterface.Name);
        }

        #region IsEnabled
        private string? _state;
        public string? State
        {
            get => _state;
            private set => SetProperty(ref _state, value);
        }
        #endregion

        #region Name
        private string? _name;
        public string? Name
        {
            get => _name;
            private set => SetProperty(ref _name, value);
        }
        #endregion

        #region ClientName
        private string? _clientName;
        public string? ClientName
        {
            get => _clientName;
            private set => SetProperty(ref _clientName, value);
        }
        #endregion

        #region ClientMacAddress
        private string? _clientMacAddress;
        public string? ClientMacAddress
        {
            get => _clientMacAddress;
            private set => SetProperty(ref _clientMacAddress, value);
        }
        #endregion

        #region ClientIpAddress
        private string? _clientIpAddress;
        public string? ClientIpAddress
        {
            get => _clientIpAddress;
            private set => SetProperty(ref _clientIpAddress, value);
        }
        #endregion

        private void Update(IRouterInterface routerInterface)
        {
            const string emptyText = "Н/д";

            _dispatcherService.Invoke(() =>
            {
                State = routerInterface.IsEnabled ? "Вкл." : "Откл.";
                Name = routerInterface.Name;

                ClientName = string.IsNullOrWhiteSpace(routerInterface.ClientName) ? emptyText : routerInterface.ClientName;
                ClientMacAddress = string.IsNullOrWhiteSpace(routerInterface.ClientMacAddress) ? emptyText : routerInterface.ClientMacAddress;
                ClientIpAddress = string.IsNullOrWhiteSpace(routerInterface.ClientIpAddress) ? emptyText : routerInterface.ClientIpAddress;
            });
        }

        public void Dispose()
        {
            _subscriptionToken?.Dispose();
        }

        #region Nested types

        private class Observer : IObserver<IRouterInterface>
        {
            private readonly Action<IRouterInterface> _onNext;

            public Observer(Action<IRouterInterface> onNext)
            {
                _onNext = onNext;
            }

            public void OnNext(IRouterInterface value)
            {
                _onNext(value);
            }

            public void OnCompleted()
            {
                throw new NotImplementedException();
            }

            public void OnError(Exception error)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}