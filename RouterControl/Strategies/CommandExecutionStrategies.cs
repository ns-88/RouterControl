using System;
using System.Threading.Tasks;
using RouterControl.Interfaces.Services;
using RouterControl.Interfaces.Strategies;

namespace RouterControl.Strategies
{
    internal class ConCmdExecutionStrategy : ICommandExecutionStrategy
    {
        public bool IsEnabled { get; }

        public ConCmdExecutionStrategy(bool enable) => IsEnabled = enable;

        public Task InvokeAsync(IRouterControlService routerControlService, IProgress<string> progress)
        {
            return routerControlService.ChangeConnectionStateAsync(IsEnabled, progress);
        }
    }
}