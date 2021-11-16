using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MikroTikMiniApi.Commands;
using MikroTikMiniApi.Interfaces;
using MikroTikMiniApi.Interfaces.Commands;
using MikroTikMiniApi.Interfaces.Factories;
using MikroTikMiniApi.Interfaces.Networking;
using MikroTikMiniApi.Interfaces.Sentences;
using MikroTikMiniApi.Models.Api;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Models;
using RouterControl.Interfaces.Services;

namespace RouterControl.Services
{
    internal class RouterControlService : IRouterControlService
    {
        private readonly RouterActionExecutor _executor;

        public RouterControlService(IApiFactory apiFactory,
                                    IReadOnlySettingsService settingsService,
                                    IReadOnlyCredentialService credentialService)
        {
            Guard.ThrowIfNull(settingsService, nameof(settingsService));
            _executor = new RouterActionExecutor(apiFactory, settingsService.ProgramSettings, credentialService);
        }

        public Task ChangeConnectionStateAsync(bool enable, IProgress<string> progress)
        {
            Guard.ThrowIfNull(progress, nameof(progress));

            var action = new RouterChangeInterfaceStateAction(enable);

            return _executor.ExecuteActionAsync(action, progress);
        }

        public async ValueTask<bool> GetConnectionStateAsync()
        {
            var action = new RequestInterfacesStatusAction(_executor.Settings);

            if (!_executor.CanExecuteAction)
                return false;

            await _executor.ExecuteActionAsync(action).ConfigureAwait(false);
            
            return action.AreInterfacesActive;
        }

        #region Nested types

        private interface IRouterAction
        {
            Task ExecuteAsync(IRouterApi routerApi, IProgramSettings settings, IProgress<string>? progress);
        }

        private abstract class RouterActionBase : IRouterAction
        {
            protected static async Task ExecuteCommandAsync(IRouterApi routerApi, IApiCommand command, IProgress<string>? progress)
            {
                IApiSentence response;

                try
                {
                    response = await routerApi.ExecuteCommandAsync(command).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    progress?.Report("Ошибка выполнения команды.");

                    throw new InvalidOperationException($"Команда не была выполнена.\r\nОшибка: {ex.Message}", ex);
                }

                if (response is IApiDoneSentence)
                {
                    progress?.Report("Команда выполнена.");
                }
                else
                {
                    progress?.Report("Ошибка выполнения команды.");

                    throw new InvalidOperationException($"Команда не была выполнена - неожиданный ответ API. Тип ответа: \"{response.GetType().Name}\", текст ответа: \"{response.GetText()}\".");
                }
            }

            public abstract Task ExecuteAsync(IRouterApi routerApi, IProgramSettings settings, IProgress<string>? progress);
        }

        private class RouterChangeInterfaceStateAction : RouterActionBase
        {
            private readonly bool _enable;

            public RouterChangeInterfaceStateAction(bool enable)
            {
                _enable = enable;
            }

            private static string GetMessageText(bool enable, string interfaceName)
            {
                return $"{(enable ? "Включение" : "Выключение")} интерфейса {interfaceName}...";
            }

            public override async Task ExecuteAsync(IRouterApi routerApi, IProgramSettings settings, IProgress<string>? progress)
            {
                var cmdArg = _enable ? "false" : "true";

                //Включение/выключение интерфейса с типом pppoe.
                var pppoeCommand = ApiCommand.New("/interface/set")
                    .AddParameter("disabled", cmdArg)
                    .AddParameter(".id", settings.NetworkInterfaces.PppoeInterface)
                    .Build();

                progress?.Report(GetMessageText(_enable, settings.NetworkInterfaces.PppoeInterface));

                await ExecuteCommandAsync(routerApi, pppoeCommand, progress).ConfigureAwait(false);

                //Включение/выключение интерфейса с типом ether.
                var etherCommand = ApiCommand.New("/interface/set")
                    .AddParameter("disabled", cmdArg)
                    .AddParameter(".id", settings.NetworkInterfaces.EtherInterface)
                    .Build();

                progress?.Report(GetMessageText(_enable, settings.NetworkInterfaces.EtherInterface));

                await ExecuteCommandAsync(routerApi, etherCommand, progress).ConfigureAwait(false);
            }
        }

        private class RequestInterfacesStatusAction : RouterActionBase
        {
            private readonly IProgramSettings _settings;
            public bool AreInterfacesActive { get; private set; }

            public RequestInterfacesStatusAction(IProgramSettings settings)
            {
                _settings = settings;
            }

            public override async Task ExecuteAsync(IRouterApi routerApi, IProgramSettings settings, IProgress<string>? progress)
            {
                var requestCommand = ApiCommand.New("/interface/print")
                    .AddParameter("=.proplist=disabled")
                    .AddParameter($"?name={_settings.NetworkInterfaces.PppoeInterface}")
                    .AddParameter($"?name={_settings.NetworkInterfaces.EtherInterface}")
                    .AddParameter("?#|")
                    .Build();

                IReadOnlyList<Interface> interfaces;
                
                try
                {
                    interfaces = await routerApi.ExecuteCommandToListAsync<Interface>(requestCommand).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Команда не была выполнена.\r\nОшибка: {ex.Message}", ex);
                }

                if (interfaces is not { Count: 2 } || interfaces[0].IsDisabled == null || interfaces[1].IsDisabled == null)
                    throw new InvalidOperationException("Ответ API содержит неверные данные.");

                AreInterfacesActive = !(bool)interfaces[0].IsDisabled! && !(bool)interfaces[1].IsDisabled!;
            }
        }

        private class RouterActionExecutor
        {
            private readonly IApiFactory _apiFactory;
            private readonly IReadOnlyCredentialService _credentialService;

            public readonly IProgramSettings Settings;
            public bool CanExecuteAction => SettingsHelper.CheckSettings(Settings);

            public RouterActionExecutor(IApiFactory apiFactory,
                                        IProgramSettings settings,
                                        IReadOnlyCredentialService credentialService)
            {
                Guard.ThrowIfNull(apiFactory, out _apiFactory, nameof(apiFactory));
                Guard.ThrowIfNull(settings, out Settings, nameof(settings));
                Guard.ThrowIfNull(credentialService, out _credentialService, nameof(credentialService));

                _apiFactory = apiFactory;
                _credentialService = credentialService;

                Settings = settings;
            }

            private static string GetPassword(IProgramSettings settings, IReadOnlyCredentialService credentialService)
            {
                try
                {
                    return credentialService.DecryptPassword(settings.UserPassword);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Пароль пользователя не был получен.\r\nОшибка: {ex.Message}", ex);
                }
            }

            public Task ExecuteActionAsync(IRouterAction action, IProgress<string>? progress = null)
            {
                SettingsHelper.ThrowIfWrongSettings(Settings);

                return Task.Run(async () =>
                {
                    IControlledConnection connection = null!;

                    try
                    {
                        try
                        {
                            connection = _apiFactory.CreateConnection(Settings.RouterAddress);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Объект подключения не был создан.\r\nОшибка: {ex.Message}", ex);
                        }

                        //Подключение к роутеру.
                        progress?.Report("Подключение к роутеру...");

                        try
                        {
                            await connection.ConnectAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            progress?.Report("Ошибка подключения к роутеру.");

                            throw new InvalidOperationException($"Подключение не было устновлено.\r\nОшибка: {ex.Message}", ex);
                        }

                        progress?.Report("Подключение установлено.");

                        //Аутентификация пользователя.
                        var routerApi = _apiFactory.CreateRouterApi(connection);

                        progress?.Report("Аутентификация пользователя...");

                        try
                        {
                            await routerApi.AuthenticationAsync(Settings.UserName, GetPassword(Settings, _credentialService)).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            progress?.Report("Ошибка аутентификации пользователя.");

                            throw new InvalidOperationException($"Аутентификация пользователя не была произведена.\r\nОшибка: {ex.Message}", ex);
                        }

                        progress?.Report("Аутентификация выполнена.");

                        //Выполнение действия.
                        await action.ExecuteAsync(routerApi, Settings, progress).ConfigureAwait(false);

                        //Выход из системы.
                        progress?.Report("Выход из системы...");

                        try
                        {
                            await routerApi.QuitAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            progress?.Report("Ошибка выхода из системы.");

                            throw new InvalidOperationException($"Выход из системы не был произведен.\r\nОшибка: {ex.Message}", ex);
                        }

                        progress?.Report("Выход из системы произведен.");
                    }
                    finally
                    {
                        if (connection != null!)
                            connection.Dispose();
                    }
                });
            }

            #region Nested types

            private static class SettingsHelper
            {
                public static bool CheckSettings(IProgramSettings settings, StringBuilder? wrongSettings = null)
                {
                    Guard.ThrowIfNull(settings.RouterAddress, nameof(settings.RouterAddress));
                    Guard.ThrowIfNull(settings.NetworkInterfaces, nameof(settings.NetworkInterfaces));

                    var result = true;

                    if (string.IsNullOrWhiteSpace(settings.UserName))
                        SetWrongValue("Имя пользователя");

                    if (settings.UserPassword.IsEmpty)
                        SetWrongValue("Пароль");

                    if (!CheckIpAddress(settings.RouterAddress.Address))
                        SetWrongValue("IP-адрес");

                    if (settings.RouterAddress.Port <= IPEndPoint.MinPort)
                        SetWrongValue("Порт");

                    if (string.IsNullOrWhiteSpace(settings.NetworkInterfaces.PppoeInterface))
                        SetWrongValue("PPPoE интерфейс");

                    if (string.IsNullOrWhiteSpace(settings.NetworkInterfaces.EtherInterface))
                        SetWrongValue("Ethernet интерфейс");

                    return result;

                    static bool CheckIpAddress(IPAddress address)
                    {
                        return address.AddressFamily == AddressFamily.InterNetwork &&
                               !address.Equals(IPAddress.None) &&
                               !address.Equals(IPAddress.Any) &&
                               !address.Equals(IPAddress.Broadcast) &&
                               !address.Equals(IPAddress.Loopback) &&
                               !address.Equals(IPAddress.IPv6Any) &&
                               !address.Equals(IPAddress.IPv6Loopback) &&
                               !address.Equals(IPAddress.IPv6None);
                    }

                    void SetWrongValue(string value)
                    {
                        wrongSettings?.AppendLine(value);
                        result = false;
                    }
                }

                public static void ThrowIfWrongSettings(IProgramSettings settings)
                {
                    var wrongSettings = new StringBuilder();

                    if (!CheckSettings(settings, wrongSettings))
                        throw new InvalidOperationException($"Следующие настройки не заданы:\r\n{wrongSettings}");
                }
            }

            #endregion
        }

        #endregion
    }
}