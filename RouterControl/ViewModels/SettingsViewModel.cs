using System;
using System.Collections.Generic;
using Prism.Commands;
using Prism.Services.Dialogs;

namespace RouterControl.ViewModels
{
    using Infrastructure.Enums;
    using Infrastructure.Extensions;
    using Infrastructure.Utilities;
    using Interfaces.Infrastructure.Services;
    using Models;

    internal class SettingsViewModel : DialogViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly INotificationService _notificationService;
        private readonly SettingsModel _model;
        private readonly CommandManager _commandManager;

        public DelegateCommand SaveCommand { get; }

        public SettingsViewModel(ISettingsService settingsService,
                                 ICredentialService credentialService,
                                 INotificationService notificationService)
            : base("Настройки")
        {
            Guard.ThrowIfNull(settingsService, out _settingsService, nameof(settingsService));
            Guard.ThrowIfNull(credentialService, nameof(credentialService));
            Guard.ThrowIfNull(notificationService, out _notificationService, nameof(notificationService));

            _model = new SettingsModel(credentialService);
            _commandManager = new CommandManager(false);

            SaveCommand = new DelegateCommand(SaveHandler, _commandManager.CanExecuteCommand);
        }

        #region UserName
        public string? UserName
        {
            get => _model.UserName;
            set => SetProperty(() => _model.UserName, x => _model.UserName = x, value);
        }
        #endregion

        #region UserPassword
        public string? UserPassword
        {
            get => _model.UserPassword;
            set => SetProperty(() => _model.UserPassword, x => _model.UserPassword = x, value);
        }

        #endregion

        #region RouterIpAddress
        public string? RouterIpAddress
        {
            get => _model.RouterIpAddress;
            set => SetProperty(() => _model.RouterIpAddress, x => _model.RouterIpAddress = x, value);
        }
        #endregion

        #region RouterPort
        public string? RouterPort
        {
            get => _model.RouterPort;
            set => SetProperty(() => _model.RouterPort, x => _model.RouterPort = x, value);
        }
        #endregion

        #region PppoeInterface
        public string? PppoeInterface
        {
            get => _model.PppoeInterface;
            set => SetProperty(() => _model.PppoeInterface, x => _model.PppoeInterface = x, value);
        }
        #endregion

        #region EthernetInterface
        public string? EthernetInterface
        {
            get => _model.EthernetInterface;
            set => SetProperty(() => _model.EthernetInterface, x => _model.EthernetInterface = x, value);

        }
        #endregion

        #region IsApplicationAutorun
        public bool IsApplicationAutorun
        {
            get => _model.IsApplicationAutorun;
            set => SetProperty(() => _model.IsApplicationAutorun, x => _model.IsApplicationAutorun = x, value);
        }
        #endregion

        private void RaiseCanExecuteCommand()
        {
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void SaveHandler()
        {
            if (!_model.CheckModel())
            {
                _notificationService.Notify("Не все поля заполнены. Проверьте правильность ввода.",
                    "Ошибка сохранения настроек", notificationImage: NotificationImages.Warning);
                return;
            }

            try
            {
                _settingsService.SaveProgramSettings(_model.ToEntity());
            }
            catch (Exception ex)
            {
                _notificationService.Notify($"Сохранение настроек не было выполнено.\r\n{ex.CreateErrorText()}",
                    "Ошибка сохранения настроек", notificationImage: NotificationImages.Error);
                return;
            }

            _notificationService.Notify("Настройки сохранены.", string.Empty, notificationImage: NotificationImages.Information);

            RaiseRequestClose(new DialogResult());
        }

        protected override IEnumerable<string> GetPropertyNames()
        {
            yield return nameof(UserName);
            yield return nameof(UserPassword);
            yield return nameof(RouterIpAddress);
            yield return nameof(RouterPort);
            yield return nameof(PppoeInterface);
            yield return nameof(EthernetInterface);
            yield return nameof(IsApplicationAutorun);
        }

        protected override void OnViewOpened()
        {
            try
            {
                _model.FromEntity(_settingsService.ProgramSettings);
            }
            catch (Exception ex)
            {
                _notificationService.Notify($"Настройки не были получены.\r\n{ex.CreateErrorText()}", 
                    "Ошибка", notificationImage: NotificationImages.Error);
                return;
            }

            _commandManager.SetCanExecuteCommand(_model.IsModelFilled);

            RaisePropertiesChanged();
            RaiseCanExecuteCommand();
        }

        #region Nested types

        private class CommandManager
        {
            private bool _canExecuteCommand;

            public CommandManager(bool canExecuteCommand)
            {
                _canExecuteCommand = canExecuteCommand;
            }

            public bool CanExecuteCommand()
            {
                return _canExecuteCommand;
            }

            public void SetCanExecuteCommand(bool canExecute)
            {
                _canExecuteCommand = canExecute;
            }
        }

        #endregion
    }
}