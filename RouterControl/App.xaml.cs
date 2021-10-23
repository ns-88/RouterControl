using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using MikroTikMiniApi.Factories;
using MikroTikMiniApi.Interfaces.Factories;
using Prism.Ioc;
using RouterControl.Infrastructure.Constants;
using RouterControl.Infrastructure.Factories;
using RouterControl.Infrastructure.Providers;
using RouterControl.Interfaces.Infrastructure.Factories;
using RouterControl.Interfaces.Providers;
using RouterControl.Interfaces.Services;
using RouterControl.Services;
using RouterControl.ViewModels;
using RouterControl.Views;

namespace RouterControl
{
    public partial class App
    {
        private TaskbarIcon? _taskbarIcon;

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // AdonisDialogHostWindow
            containerRegistry.RegisterDialogWindow<AdonisDialogHostWindow>();

            // CommandExecutionProcessView
            containerRegistry.RegisterDialog<CommandExecutionProcessView, CommandExecutionViewModel>(UiConstants.CommandExecutionView);

            // SettingsView
            containerRegistry.RegisterDialog<SettingsView, SettingsViewModel>(UiConstants.SettingsView);

            // IApiFactory
            containerRegistry.RegisterSingleton<IApiFactory, MicrotikApiFactory>();

            // IRouterControlServiceFactory
            containerRegistry.Register<IRouterControlServiceFactory, RouterControlServiceFactory>();

            // INotificationService
            containerRegistry.RegisterSingleton<INotificationService, NotificationService>();

            // ISettingsStoresProvider
            containerRegistry.Register<ISettingsStoresProvider, RegistrySettingsStoresProvider>();

            // ISettingsService
            containerRegistry.RegisterSingleton<SettingsService>();
            containerRegistry.Register<ISettingsService, SettingsService>();
            containerRegistry.Register<IReadOnlySettingsService, SettingsService>();

            // ICredentialService
            containerRegistry.Register<ICredentialService, CredentialService>();
            containerRegistry.Register<IReadOnlyCredentialService, CredentialService>();
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

                var settingsService = Container.Resolve<ISettingsService>();

                settingsService.LoadSettings();
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