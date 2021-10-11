using System;
using System.Threading.Tasks;
using RouterControl.Interfaces.Services;

namespace RouterControl.Interfaces.Strategies
{
    internal interface ICommandExecutionStrategy
    {
        Task InvokeAsync(IRouterControlService routerControlService, IProgress<string> progress);
    }
}