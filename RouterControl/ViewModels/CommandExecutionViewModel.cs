using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Infrastructure.Factories;
using RouterControl.Interfaces.Strategies;
using RouterControl.Models;

namespace RouterControl.ViewModels
{
    internal class CommandExecutionViewModel : DialogViewModelBase
    {
        private readonly IRouterControlServiceFactory _routerControlServiceFactory;
        public ObservableCollection<LogEntryModel> CommandLog { get; }

        public CommandExecutionViewModel(IRouterControlServiceFactory routerControlServiceFactory)
            : base(string.Empty)
        {
            Guard.ThrowIfNull(routerControlServiceFactory, out _routerControlServiceFactory, nameof(routerControlServiceFactory));
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
            catch (Exception)
            {
                
            }

            await Task.Delay(3000);

            RaiseRequestClose(new DialogResult());
        }
    }
}