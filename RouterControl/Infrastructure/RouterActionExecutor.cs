using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MikroTikMiniApi.Interfaces.Factories;
using MikroTikMiniApi.Interfaces.Networking;

namespace RouterControl.Infrastructure
{
    using Interfaces.Executors;
    using Interfaces.Infrastructure.Services;
    using Interfaces.Models;
    using Interfaces.RouterActions;
    using Utilities;

    internal class RouterActionExecutor : IRouterActionExecutor
    {
        private readonly IApiFactory _apiFactory;
        private readonly IReadOnlyCredentialService _credentialService;

        public IProgramSettings Settings { get; }
        public bool CanExecuteAction => SettingsHelper.CheckSettings(Settings);

        public RouterActionExecutor(IApiFactory apiFactory,
                                    IProgramSettings settings,
                                    IReadOnlyCredentialService credentialService)
        {
            Guard.ThrowIfNull(apiFactory, out _apiFactory, nameof(apiFactory));
            Guard.ThrowIfNull(credentialService, out _credentialService, nameof(credentialService));

            _apiFactory = apiFactory;
            _credentialService = credentialService;

            Settings = Guard.ThrowIfNullRet(settings, nameof(settings));
        }

        private static string GetPassword(IProgramSettings settings, IReadOnlyCredentialService credentialService)
        {
            try
            {
                return credentialService.DecryptPassword(settings.UserPassword);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Пароль пользователя не был получен.", ex);
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
                        throw new InvalidOperationException("Объект подключения не был создан.", ex);
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

                        throw new InvalidOperationException("Подключение не было устновлено.", ex);
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

                        throw new InvalidOperationException("Аутентификация пользователя не была произведена.", ex);
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

                        throw new InvalidOperationException("Выход из системы не был произведен.", ex);
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
}