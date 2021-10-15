using System;
using System.Net;
using System.Threading.Tasks;
using MikroTikMiniApi.Commands;
using MikroTikMiniApi.Interfaces;
using MikroTikMiniApi.Interfaces.Commands;
using MikroTikMiniApi.Interfaces.Factories;
using MikroTikMiniApi.Interfaces.Networking;
using MikroTikMiniApi.Interfaces.Sentences;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Services;

namespace RouterControl.Services
{
    internal class RouterControlService : IRouterControlService
    {
        private readonly IApiFactory _apiFactory;

        public RouterControlService(IApiFactory apiFactory)
        {
            Guard.ThrowIfNull(apiFactory, out _apiFactory, nameof(apiFactory));
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

                throw new InvalidOperationException($"Команда не была выполнена. Сообщение об ошибке:\r\n{ex.Message}", ex);
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

        public Task ChangeConnectionStateAsync(bool enable, IProgress<string> progress)
        {
            return Task.Run(async () =>
            {
                IControlledConnection connection = null!;
                
                try
                {
                    try
                    {
                        connection = _apiFactory.CreateConnection(new IPEndPoint(IPAddress.Parse("192.168.88.1"), 8728));
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Объект подключения не был создан. Сообщение об ошибке:\r\n{ex.Message}", ex);
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

                        throw new InvalidOperationException($"Подключение не было устновлено. Сообщение об ошибке:\r\n{ex.Message}", ex);
                    }

                    progress.Report("Подключение установлено.");

                    //Аутентификация пользователя.
                    var routerApi = _apiFactory.CreateRouterApi(connection);

                    progress.Report("Аутентификация пользователя...");

                    try
                    {
                        await routerApi.AuthenticationAsync("name", "password").ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        progress.Report("Ошибка аутентификации пользователя.");

                        throw new InvalidOperationException($"Аутентификация пользователя не была произведена. Сообщение об ошибке:\r\n{ex.Message}", ex);
                    }

                    progress.Report("Аутентификация выполнена.");
                    
                    //Выполнение команды.
                    var cmdArg = enable ? "false" : "true";

                    //Включение/выключение интерфейса с типом pppoe.
                    var pppoeCommand = ApiCommand.New("/interface/set")
                        .AddParameter("disabled", cmdArg)
                        .AddParameter(".id", "Rostelecom")
                        .Build();

                    progress.Report(GetMessageText(enable, "Rostelecom"));

                    await ExecuteCommandAsync(routerApi, pppoeCommand, progress).ConfigureAwait(false);

                    //Включение/выключение интерфейса с типом ether.
                    var etherCommand = ApiCommand.New("/interface/set")
                        .AddParameter("disabled", cmdArg)
                        .AddParameter(".id", "ether2")
                        .Build();

                    progress.Report(GetMessageText(enable, "ether2"));

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

                        throw new InvalidOperationException($"Выход из системы не был произведен. Сообщение об ошибке:\r\n{ex.Message}", ex);
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