using System;
using System.Threading.Tasks;

namespace RouterControl.Infrastructure.Strategies
{
    using Interfaces.Services;
    using Interfaces.Strategies;

    internal class ConCmdExecutionStrategy : ICommandExecutionStrategy
    {
        public bool IsEnabled { get; }

        public ConCmdExecutionStrategy(bool enable) => IsEnabled = enable;

        public Task InvokeAsync(IRouterControlService routerControlService, IProgress<string> progress)
        {
            return routerControlService.ChangeInterfacesStateAsync(IsEnabled, progress);
        }
    }
}