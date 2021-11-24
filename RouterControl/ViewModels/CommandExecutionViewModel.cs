using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using RouterControl.Infrastructure.Constants;
using RouterControl.Infrastructure.Enums;
using RouterControl.Infrastructure.Extensions;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Infrastructure.Factories;
using RouterControl.Interfaces.Services;
using RouterControl.Interfaces.Strategies;
using RouterControl.Models;

namespace RouterControl.ViewModels
{
    internal class CommandExecutionViewModel : DialogViewModelBase
    {
        private readonly IRouterServicesFactory _routerServicesFactory;
        private readonly INotificationService _notificationService;

        public ObservableCollection<LogEntryModel> CommandLog { get; }
        
        public CommandExecutionViewModel(IRouterServicesFactory routerServicesFactory, INotificationService notificationService)
            : base(string.Empty)
        {
            Guard.ThrowIfNull(routerServicesFactory, out _routerServicesFactory, nameof(routerServicesFactory));
            Guard.ThrowIfNull(notificationService, out _notificationService, nameof(notificationService));

            CommandLog = new ObservableCollection<LogEntryModel>();
        }

        #region ProgressValue
        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            private set => SetProperty(ref _progressValue, value);
        }
        #endregion

        public override async void OnDialogOpened(IDialogParameters parameters)
        {
            bool? connected = null;
            var strategy = parameters.GetValue<ICommandExecutionStrategy>(nameof(ICommandExecutionStrategy));
            var routerControlService = _routerServicesFactory.CreateControlService();
            var progress = new Progress<string>(x =>
            {
                CommandLog.Add(new LogEntryModel(x, DateTime.Now));
                ProgressValue++;
            });

            try
            {
                await strategy.InvokeAsync(routerControlService, progress);
                connected = strategy.IsEnabled;
            }
            catch (Exception ex)
            {
                _notificationService.Notify(ex.Message, "Ошибка выполнения команды", notificationImage: NotificationImages.Error);
            }

            await Task.Delay(2000);

            RaiseRequestClose(new DialogResult().WithParameter(UiConstants.CommandExecutionResultName, connected));
        }
    }
}