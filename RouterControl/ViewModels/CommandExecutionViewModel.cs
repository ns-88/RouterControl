using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using RouterControl.Infrastructure.Enums;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Infrastructure.Factories;
using RouterControl.Interfaces.Services;
using RouterControl.Interfaces.Strategies;
using RouterControl.Models;

namespace RouterControl.ViewModels
{
    internal class CommandExecutionViewModel : DialogViewModelBase
    {
        private readonly IRouterControlServiceFactory _routerControlServiceFactory;
        private readonly INotificationService _notificationService;

        public ObservableCollection<LogEntryModel> CommandLog { get; }

        public CommandExecutionViewModel(IRouterControlServiceFactory routerControlServiceFactory, INotificationService notificationService)
            : base(string.Empty)
        {
            Guard.ThrowIfNull(routerControlServiceFactory, out _routerControlServiceFactory, nameof(routerControlServiceFactory));
            Guard.ThrowIfNull(notificationService, out _notificationService, nameof(notificationService));

            CommandLog = new ObservableCollection<LogEntryModel>();
        }

        public override async void OnDialogOpened(IDialogParameters parameters)
        {
            var strategy = parameters.GetValue<ICommandExecutionStrategy>(nameof(ICommandExecutionStrategy));
            var routerControlService = _routerControlServiceFactory.Create();
            var progress = new Progress<string>(x => CommandLog.Add(new LogEntryModel(x, DateTime.Now)));

            try
            {
                await strategy.InvokeAsync(routerControlService, progress);
            }
            catch (Exception ex)
            {
                _notificationService.Notify(ex.Message, "Ошибка выполнения команды", notificationImage: NotificationImages.Error);
            }

            await Task.Delay(3000);

            RaiseRequestClose(new DialogResult());
        }
    }
}