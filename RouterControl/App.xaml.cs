using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using MikroTikMiniApi.Factories;
using MikroTikMiniApi.Interfaces.Factories;
using Prism.Ioc;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace RouterControl
{
    using Infrastructure.Constants;
    using Infrastructure.Extensions;
    using Infrastructure.Factories;
    using Infrastructure.Providers;
    using Infrastructure.Services;
    using Infrastructure.Trackers;
    using Interfaces.Infrastructure;
    using Interfaces.Infrastructure.Factories;
    using Interfaces.Infrastructure.Services;
    using Interfaces.Infrastructure.Trackers;
    using Interfaces.Models;
    using Interfaces.Providers;
    using Interfaces.Services;
    using Services;
    using ViewModels;
    using Views;

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

            // InterfacesStateView
            containerRegistry.RegisterDialog<InterfacesStateView, InterfacesStateViewModel>(UiConstants.InterfacesStateView);

            // IApiFactory
            containerRegistry.Register<IApiFactory, MicrotikApiFactory>();

            // IRouterControlServiceFactory
            containerRegistry.Register<IRouterServicesFactory, RouterServicesFactory>();

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

            // IRouterActionExecutorFactory
            containerRegistry.Register<IRouterActionExecutorFactory, RouterActionExecutorFactory>();
            
            // IUiDispatcherService
            containerRegistry.RegisterSingleton<IUiDispatcherService, UiDispatcherService>();

            // IConnectionStateNotifierService
            containerRegistry.Register<IConnectionStateNotifierService, ConnectionStateNotifierService>();
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

                var settingsService = Container.Resolve<ISettingsService>();
                var appAutorunService = Container.Resolve<ApplicationAutorunService>();

                _settingsEventTracker.Register(nameof(IProgramSettings), appAutorunService);

                settingsService.LoadSettings();

                _taskbarIcon = (TaskbarIcon?)FindResource(UiConstants.TaskbarIconSystemTrayName);

                if (_taskbarIcon == null)
                    throw new InvalidOperationException($"Элемент управления с именем \"{UiConstants.TaskbarIconSystemTrayName}\" не найден.");

                _taskbarIcon.DataContext = Container.Resolve<SystemTrayViewModel>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Запуск приложения не был успешно произведен.\r\n{ex.CreateErrorText()}", icon: MessageBoxImage.Error);
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