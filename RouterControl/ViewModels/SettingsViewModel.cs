using System;
using System.Collections.Generic;
using Prism.Commands;
using Prism.Services.Dialogs;
using RouterControl.Infrastructure.Enums;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Services;
using RouterControl.Models;

namespace RouterControl.ViewModels
{
    internal class SettingsViewModel : DialogViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly INotificationService _notificationService;
        private readonly SettingsModel _settingsModel;
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

            _settingsModel = new SettingsModel(credentialService);
            _commandManager = new CommandManager(false);

            SaveCommand = new DelegateCommand(SaveHandler, _commandManager.CanExecuteCommand);
        }

        #region UserName
        public string? UserName
        {
            get => _settingsModel.UserName;
            set
            {
                _settingsModel.UserName = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region UserPassword
        public string? UserPassword
        {
            get => _settingsModel.UserPassword;
            set
            {
                _settingsModel.UserPassword = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region RouterIpAddress
        public string? RouterIpAddress
        {
            get => _settingsModel.RouterIpAddress;
            set
            {
                _settingsModel.RouterIpAddress = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region RouterPort
        public string? RouterPort
        {
            get => _settingsModel.RouterPort;
            set
            {
                _settingsModel.RouterPort = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region PppoeInterface
        public string? PppoeInterface
        {
            get => _settingsModel.PppoeInterface;
            set
            {
                _settingsModel.PppoeInterface = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region EthernetInterface
        public string? EthernetInterface
        {
            get => _settingsModel.EthernetInterface;
            set
            {
                _settingsModel.EthernetInterface = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        private void RaiseCanExecuteCommand()
        {
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void SaveHandler()
        {
            if (!_settingsModel.CheckModel())
            {
                _notificationService.Notify("Не все поля заполнены. Проверьте правильность ввода.",
                    "Ошибка сохранения настроек", notificationImage: NotificationImages.Warning);
                return;
            }

            try
            {
                _settingsService.SaveProgramSettings(_settingsModel.ToEntity());
            }
            catch (Exception ex)
            {
                _notificationService.Notify($"Сохранение настроек не было выполнено.\r\nОшибка: {ex.Message}",
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
        }

        protected override void OnViewOpened()
        {
            try
            {
                _settingsModel.FromEntity(_settingsService.ProgramSettings);
            }
            catch (Exception ex)
            {
                _notificationService.Notify($"Настройки не были получены.\r\nОшибка: {ex.Message}",
                    "Ошибка", notificationImage: NotificationImages.Error);
                return;
            }

            _commandManager.SetCanExecuteCommand(_settingsModel.IsModelFilled);

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