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

        public DelegateCommand EnableConnectionCommand { get; }
        public DelegateCommand DisableConnectionCommand { get; }
        public DelegateCommand SettingsCommand { get; }

        public SystemTrayViewModel(IDialogService dialogService)
        {
            Guard.ThrowIfNull(dialogService, out _dialogService, nameof(dialogService));

            EnableConnectionCommand = new DelegateCommand(EnableConnectionHandler);
            DisableConnectionCommand = new DelegateCommand(DisableConnectionHandler);
            SettingsCommand = new DelegateCommand(SettingsHandler);
        }

        private void EnableConnectionHandler()
        {
            _dialogService.ShowDialog(UiConstants.CommandExecutionView, nameof(ICommandExecutionStrategy), new EnableConCmdExecutionStrategy());
        }

        private void DisableConnectionHandler()
        {
            _dialogService.ShowDialog(UiConstants.CommandExecutionView, nameof(ICommandExecutionStrategy), new DisableConCmdExecutionStrategy());
        }

        private void SettingsHandler()
        {

        }
    }
}