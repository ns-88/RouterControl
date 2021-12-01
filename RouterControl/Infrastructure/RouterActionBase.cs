using System;
using System.Threading.Tasks;
using MikroTikMiniApi.Interfaces;
using MikroTikMiniApi.Interfaces.Commands;
using MikroTikMiniApi.Interfaces.Sentences;
using RouterControl.Interfaces.Models;
using RouterControl.Interfaces.RouterActions;

namespace RouterControl.Infrastructure.RouterActions
{
    internal abstract class RouterActionBase : IRouterAction
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
}