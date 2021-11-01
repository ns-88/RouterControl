using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using MikroTikMiniApi.Factories;
using MikroTikMiniApi.Interfaces.Factories;
using Prism.Ioc;
using RouterControl.Infrastructure.Constants;
using RouterControl.Infrastructure.Factories;
using RouterControl.Infrastructure.Providers;
using RouterControl.Infrastructure.Trackers;
using RouterControl.Interfaces.Infrastructure.Factories;
using RouterControl.Interfaces.Infrastructure.Trackers;
using RouterControl.Interfaces.Models;
using RouterControl.Interfaces.Providers;
using RouterControl.Interfaces.Services;
using RouterControl.Services;
using RouterControl.ViewModels;
using RouterControl.Views;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace RouterControl
{
    public partial class App
    {
        private TaskbarIcon? _taskbarIcon;
        private ISettingsEventTracker? _settingsEventTracker;

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // AdonisDialogHostWindow
            containerRegistry.RegisterDialogWindow<AdonisDialogHostWindow>();

            // CommandExecutionView
            containerRegistry.RegisterDialog<CommandExecutionView, CommandExecutionViewModel>(UiConstants.CommandExecutionViewName);

            // SettingsView
            containerRegistry.RegisterDialog<SettingsView, SettingsViewModel>(UiConstants.SettingsViewName);

            // IApiFactory
            containerRegistry.Register<IApiFactory, MicrotikApiFactory>();

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

            // ISettingsEventTracker
            containerRegistry.RegisterSingleton<ISettingsEventTracker, SettingsEventTracker>();
        }

        protected override Window CreateShell()
        {
            return null!;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                _settingsEventTracker = Container.Resolve<ISettingsEventTracker>();
                _taskbarIcon = (TaskbarIcon?)FindResource(UiConstants.TaskbarIconSystemTrayName);

                if (_taskbarIcon == null)
                    throw new InvalidOperationException($"Элемент управления с именем \"{UiConstants.TaskbarIconSystemTrayName}\" не найден.");

                _taskbarIcon.DataContext = Container.Resolve<SystemTrayViewModel>();

                var settingsService = Container.Resolve<ISettingsService>();
                var appAutorunService = Container.Resolve<ApplicationAutorunService>();

                _settingsEventTracker.Register(nameof(IProgramSettings), appAutorunService);

                settingsService.LoadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Запуск приложения не был успешно произведен.\r\nОшибка: {ex.Message}", icon: MessageBoxImage.Error);
                Environment.Exit(-1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _taskbarIcon?.Dispose();
            _settingsEventTracker?.Dispose();

            base.OnExit(e);
        }
    }
}