using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MikroTikMiniApi.Commands;
using MikroTikMiniApi.Interfaces;
using MikroTikMiniApi.Models.Api;

namespace RouterControl.Services
{
    using Infrastructure.RouterActions;
    using Infrastructure.Utilities;
    using Interfaces.Infrastructure;
    using Interfaces.Models;
    using Interfaces.Services;

    internal class RouterControlService : IRouterControlService
    {
        private readonly IRouterActionExecutorFactory _routerActionExecutorFactory;

        public RouterControlService(IRouterActionExecutorFactory routerActionExecutorFactory)
        {
            Guard.ThrowIfNull(routerActionExecutorFactory, out _routerActionExecutorFactory, nameof(routerActionExecutorFactory));
        }

        public Task ChangeInterfacesStateAsync(bool enable, IProgress<string> progress)
        {
            Guard.ThrowIfNull(progress, nameof(progress));

            var executor = _routerActionExecutorFactory.Create();
            var action = new RouterChangeInterfacesStateAction(enable);
            
            return executor.ExecuteActionAsync(action, progress);
        }

        public async ValueTask<bool> GetInterfacesStateAsync()
        {
            var executor = _routerActionExecutorFactory.Create();
            var action = new RequestInterfacesStatusAction(executor.Settings);

            if (!executor.CanExecuteAction)
                return false;

            await executor.ExecuteActionAsync(action).ConfigureAwait(false);
            
            return action.AreInterfacesActive;
        }

        #region Nested types

        private class RouterChangeInterfacesStateAction : RouterActionBase
        {
            private readonly bool _enable;

            public RouterChangeInterfacesStateAction(bool enable)
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
                    throw new InvalidOperationException("Команда не была выполнена.", ex);
                }

                if (interfaces is not { Count: 2 } || interfaces[0].IsDisabled == null || interfaces[1].IsDisabled == null)
                    throw new InvalidOperationException("Ответ API содержит неверные данные.");

                AreInterfacesActive = !(bool)interfaces[0].IsDisabled! && !(bool)interfaces[1].IsDisabled!;
            }
        }

        #endregion
    }
}