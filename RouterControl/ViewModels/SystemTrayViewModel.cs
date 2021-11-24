using System;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using RouterControl.Infrastructure.Constants;
using RouterControl.Infrastructure.Enums;
using RouterControl.Infrastructure.Extensions;
using RouterControl.Infrastructure.Strategies;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Infrastructure.Factories;
using RouterControl.Interfaces.Services;
using RouterControl.Interfaces.Strategies;

namespace RouterControl.ViewModels
{
    internal class SystemTrayViewModel : BindableBase
    {
        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly CommandManager _commandManager;

        public DelegateCommand EnableConnectionCommand { get; }
        public DelegateCommand DisableConnectionCommand { get; }
        public DelegateCommand SettingsCommand { get; }
        public DelegateCommand OpenInterfacesStateCommand { get; }

        public SystemTrayViewModel(IRouterServicesFactory routerControlServiceFactory,
                                   INotificationService notificationService,
                                   IDialogService dialogService)
        {
            Guard.ThrowIfNull(routerControlServiceFactory, nameof(routerControlServiceFactory));
            Guard.ThrowIfNull(notificationService, out _notificationService, nameof(notificationService));
            Guard.ThrowIfNull(dialogService, out _dialogService, nameof(dialogService));

            _commandManager = new CommandManager(false, RaiseCanExecuteCommands);

            EnableConnectionCommand = new DelegateCommand(EnableConnectionHandler, _commandManager.CanExecuteCommand);
            DisableConnectionCommand = new DelegateCommand(DisableConnectionHandler, _commandManager.CanExecuteCommand);
            SettingsCommand = new DelegateCommand(SettingsHandler, _commandManager.CanExecuteCommand);
            OpenInterfacesStateCommand = new DelegateCommand(OpenInterfacesStateHandler, _commandManager.CanExecuteCommand);

            InitializationAsync(routerControlServiceFactory);
        }

        #region IsConnected
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                _isConnected = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        private async void InitializationAsync(IRouterServicesFactory routerServicesFactory)
        {
            var routerControlService = routerServicesFactory.CreateControlService();
            bool connectionState;

            using (_commandManager.GetCommandHelper(false, false))
            {
                try
                {
                    connectionState = await routerControlService.GetConnectionStateAsync();
                }
                catch (Exception ex)
                {
                    _notificationService.Notify($"Запрос состояния подключения не был успешно выполнен.\r\nОшибка: {ex.Message}",
                        "Ошибка выполнения команды", notificationImage: NotificationImages.Error);

                    return;
                }
            }

            IsConnected = connectionState;
        }

        private void RaiseCanExecuteCommands()
        {
            EnableConnectionCommand.RaiseCanExecuteChanged();
            DisableConnectionCommand.RaiseCanExecuteChanged();
            SettingsCommand.RaiseCanExecuteChanged();
        }

        private void CallbackHandler(IDialogResult dialogResult)
        {
            var connected = dialogResult.Parameters.GetValue<bool?>(UiConstants.CommandExecutionResultName);

            if (connected.HasValue)
                IsConnected = (bool)connected;
        }

        private void EnableConnectionHandler()
        {
            using (_commandManager.GetCommandHelper(false))
            {
                _dialogService.ShowDialog(UiConstants.CommandExecutionViewName, nameof(ICommandExecutionStrategy),
                    new ConCmdExecutionStrategy(true), CallbackHandler);
            }
        }

        private void DisableConnectionHandler()
        {
            using (_commandManager.GetCommandHelper(false))
            {
                _dialogService.ShowDialog(UiConstants.CommandExecutionViewName, nameof(ICommandExecutionStrategy),
                    new ConCmdExecutionStrategy(false), CallbackHandler);
            }
        }

        private void SettingsHandler()
        {
            using (_commandManager.GetCommandHelper(false))
            {
                _dialogService.ShowDialog(UiConstants.SettingsViewName);
            }
        }

        private void OpenInterfacesStateHandler()
        {
            using (_commandManager.GetCommandHelper(false))
            {
                _dialogService.ShowDialog(UiConstants.InterfacesStateView);
            }
        }

        #region Nested types

        private class CommandManager
        {
            private bool _canExecuteCommand;
            private readonly Action _raiseAction;

            public CommandManager(bool canExecuteCommand, Action raiseAction)
            {
                _canExecuteCommand = canExecuteCommand;
                _raiseAction = raiseAction;
            }

            public bool CanExecuteCommand()
            {
                return _canExecuteCommand;
            }

            public CommandHelper GetCommandHelper(bool enable, bool isRaiseAction = true)
            {
                return new CommandHelper(enable, this, isRaiseAction);
            }

            public readonly struct CommandHelper : IDisposable
            {
                private readonly CommandManager _commandManager;
                private readonly bool _enable;

                public CommandHelper(bool enable, CommandManager commandManager, bool isRaiseAction = true)
                {
                    _enable = enable;
                    _commandManager = commandManager;

                    commandManager._canExecuteCommand = enable;

                    if (isRaiseAction)
                        commandManager._raiseAction();
                }

                public void Dispose()
                {
                    _commandManager._canExecuteCommand = !_enable;
                    _commandManager._raiseAction();
                }
            }
        }

        #endregion
    }
}