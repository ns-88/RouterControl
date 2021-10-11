using System;
using System.Threading.Tasks;
using RouterControl.Interfaces.Services;
using RouterControl.Interfaces.Strategies;

namespace RouterControl.Strategies
{
    internal class EnableConCmdExecutionStrategy : ICommandExecutionStrategy
    {
        public Task InvokeAsync(IRouterControlService routerControlService, IProgress<string> progress)
        {
            return routerControlService.ChangeConnectionStateAsync(true, progress);
        }
    }

    internal class DisableConCmdExecutionStrategy : ICommandExecutionStrategy
    {
        public Task InvokeAsync(IRouterControlService routerControlService, IProgress<string> progress)
        {
            return routerControlService.ChangeConnectionStateAsync(false, progress);
        }
    }
}