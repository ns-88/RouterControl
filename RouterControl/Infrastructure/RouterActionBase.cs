using System;
using System.Threading.Tasks;
using MikroTikMiniApi.Interfaces;
using MikroTikMiniApi.Interfaces.Commands;
using MikroTikMiniApi.Interfaces.Sentences;

namespace RouterControl.Infrastructure.RouterActions
{
    using Interfaces.Models;
    using Interfaces.RouterActions;

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

                throw new InvalidOperationException("Команда не была выполнена.", ex);
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