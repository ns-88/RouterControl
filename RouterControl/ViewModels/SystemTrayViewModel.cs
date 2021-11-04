﻿using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using RouterControl.Infrastructure.Constants;
using RouterControl.Infrastructure.Extensions;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Strategies;
using RouterControl.Strategies;

namespace RouterControl.ViewModels
{
    internal class SystemTrayViewModel : BindableBase
    {
        private readonly IDialogService _dialogService;
        private readonly CommandManager _commandManager;

        public DelegateCommand EnableConnectionCommand { get; }
        public DelegateCommand DisableConnectionCommand { get; }
        public DelegateCommand SettingsCommand { get; }

        public SystemTrayViewModel(IDialogService dialogService)
        {
            Guard.ThrowIfNull(dialogService, out _dialogService, nameof(dialogService));

            _commandManager = new CommandManager(true, RaiseCanExecuteCommands);

            EnableConnectionCommand = new DelegateCommand(EnableConnectionHandler, _commandManager.CanExecuteCommand);
            DisableConnectionCommand = new DelegateCommand(DisableConnectionHandler, _commandManager.CanExecuteCommand);
            SettingsCommand = new DelegateCommand(SettingsHandler, _commandManager.CanExecuteCommand);
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

            public CommandHelper GetCommandHelper(bool enable)
            {
                return new CommandHelper(enable, this);
            }

            public readonly ref struct CommandHelper
            {
                private readonly CommandManager _commandManager;
                private readonly bool _enable;

                public CommandHelper(bool enable, CommandManager commandManager)
                {
                    _enable = enable;
                    _commandManager = commandManager;

                    commandManager._canExecuteCommand = enable;
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