using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using MikroTikMiniApi.Factories;
using MikroTikMiniApi.Interfaces.Factories;
using Prism.Ioc;
using RouterControl.Infrastructure.Constants;
using RouterControl.Infrastructure.Factories;
using RouterControl.Interfaces.Infrastructure.Factories;
using RouterControl.ViewModels;
using RouterControl.Views;

namespace RouterControl
{
    public partial class App
    {
        private TaskbarIcon? _taskbarIcon;

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // CommandExecutionProcessView
            containerRegistry.RegisterDialog<CommandExecutionProcessView, CommandExecutionViewModel>(UiConstants.CommandExecutionView);
            // IApiFactory
            containerRegistry.RegisterSingleton<IApiFactory, MicrotikApiFactory>();
            // IRouterControlServiceFactory
            containerRegistry.Register<IRouterControlServiceFactory, RouterControlServiceFactory>();
        }

        protected override Window CreateShell()
        {
            return null!;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                _taskbarIcon = (TaskbarIcon?)FindResource("TaskbarIconSystemTray");

                if (_taskbarIcon != null)
                    _taskbarIcon.DataContext = Container.Resolve<SystemTrayViewModel>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _taskbarIcon?.Dispose();
            base.OnExit(e);
        }
    }
}