using System;
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
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Models;
using RouterControl.Interfaces.Services;

namespace RouterControl.Services
{
    internal class RouterControlService : IRouterControlService
    {
        private readonly IApiFactory _apiFactory;
        private readonly IProgramSettings _settings;
        private readonly IReadOnlyCredentialService _credentialService;

        public RouterControlService(IApiFactory apiFactory,
                                    IReadOnlySettingsService settingsService,
                                    IReadOnlyCredentialService credentialService)
        {
            Guard.ThrowIfNull(apiFactory, out _apiFactory, nameof(apiFactory));
            Guard.ThrowIfNull(settingsService, nameof(settingsService));
            Guard.ThrowIfNull(settingsService.ProgramSettings, out _settings, nameof(settingsService.ProgramSettings));
            Guard.ThrowIfNull(credentialService, out _credentialService, nameof(credentialService));
        }

        private static void ThrowIfWrongSettings(IProgramSettings settings)
        {
            Guard.ThrowIfNull(settings.RouterAddress, nameof(settings.RouterAddress));
            Guard.ThrowIfNull(settings.NetworkInterfaces, nameof(settings.NetworkInterfaces));

            var wrongSettings = new StringBuilder();

            if (string.IsNullOrWhiteSpace(settings.UserName))
                wrongSettings.AppendLine("Имя пользователя");

            if (settings.UserPassword.IsEmpty)
                wrongSettings.AppendLine("Пароль");

            if (!CheckIpAddress(settings.RouterAddress.Address))
                wrongSettings.AppendLine("IP-адрес");

            if (settings.RouterAddress.Port <= IPEndPoint.MinPort)
                wrongSettings.AppendLine("Порт");

            if (string.IsNullOrWhiteSpace(settings.NetworkInterfaces.PppoeInterface))
                wrongSettings.AppendLine("PPPoE интерфейс");

            if (string.IsNullOrWhiteSpace(settings.NetworkInterfaces.EtherInterface))
                wrongSettings.AppendLine("Ethernet интерфейс");

            if (wrongSettings.Length != 0)
                throw new InvalidOperationException($"Следующие настройки не заданы:\r\n{wrongSettings}");

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
        }

        private static async Task ExecuteCommandAsync(IRouterApi routerApi, IApiCommand command, IProgress<string> progress)
        {
            IApiSentence response;

            try
            {
                response = await routerApi.ExecuteCommandAsync(command).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                progress.Report("Ошибка выполнения команды.");

                throw new InvalidOperationException($"Команда не была выполнена.\r\nОшибка: {ex.Message}", ex);
            }

            if (response is IApiDoneSentence)
            {
                progress.Report("Команда выполнена.");
            }
            else
            {
                progress.Report("Ошибка выполнения команды.");

                throw new InvalidOperationException($"Команда не была выполнена - неожиданный ответ API. Тип ответа: \"{response.GetType().Name}\", текст ответа: \"{response.GetText()}\".");
            }
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

        public Task ChangeConnectionStateAsync(bool enable, IProgress<string> progress)
        {
            Guard.ThrowIfNull(progress, nameof(progress));

            ThrowIfWrongSettings(_settings);

            return Task.Run(async () =>
            {
                IControlledConnection connection = null!;
                
                try
                {
                    try
                    {
                        connection = _apiFactory.CreateConnection(_settings.RouterAddress);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Объект подключения не был создан.\r\nОшибка: {ex.Message}", ex);
                    }

                    //Подключение к роутеру.
                    progress.Report("Подключение к роутеру...");
                    
                    try
                    {
                        await connection.ConnectAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        progress.Report("Ошибка подключения к роутеру.");

                        throw new InvalidOperationException($"Подключение не было устновлено.\r\nОшибка: {ex.Message}", ex);
                    }

                    progress.Report("Подключение установлено.");

                    //Аутентификация пользователя.
                    var routerApi = _apiFactory.CreateRouterApi(connection);

                    progress.Report("Аутентификация пользователя...");

                    try
                    {
                        await routerApi.AuthenticationAsync(_settings.UserName, GetPassword(_settings, _credentialService)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        progress.Report("Ошибка аутентификации пользователя.");

                        throw new InvalidOperationException($"Аутентификация пользователя не была произведена.\r\nОшибка: {ex.Message}", ex);
                    }

                    progress.Report("Аутентификация выполнена.");
                    
                    //Выполнение команды.
                    var cmdArg = enable ? "false" : "true";

                    //Включение/выключение интерфейса с типом pppoe.
                    var pppoeCommand = ApiCommand.New("/interface/set")
                        .AddParameter("disabled", cmdArg)
                        .AddParameter(".id", _settings.NetworkInterfaces.PppoeInterface)
                        .Build();

                    progress.Report(GetMessageText(enable, _settings.NetworkInterfaces.PppoeInterface));

                    await ExecuteCommandAsync(routerApi, pppoeCommand, progress).ConfigureAwait(false);

                    //Включение/выключение интерфейса с типом ether.
                    var etherCommand = ApiCommand.New("/interface/set")
                        .AddParameter("disabled", cmdArg)
                        .AddParameter(".id", _settings.NetworkInterfaces.EtherInterface)
                        .Build();

                    progress.Report(GetMessageText(enable, _settings.NetworkInterfaces.EtherInterface));

                    await ExecuteCommandAsync(routerApi, etherCommand, progress).ConfigureAwait(false);

                    //Выход из системы.
                    progress.Report("Выход из системы...");
                    
                    try
                    {
                        await routerApi.QuitAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        progress.Report("Ошибка выхода из системы.");

                        throw new InvalidOperationException($"Выход из системы не был произведен.\r\nОшибка: {ex.Message}", ex);
                    }

                    progress.Report("Выход из системы произведен.");
                }
                finally
                {
                    if (connection != null!)
                        connection.Dispose();
                }
            });

            static string GetMessageText(bool enable, string interfaceName)
            {
                return $"{(enable ? "Включение" : "Выключение")} интерфейса {interfaceName}...";
            }
        }
    }
}