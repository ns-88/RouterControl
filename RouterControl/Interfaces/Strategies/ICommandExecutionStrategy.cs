using System;
using System.Threading.Tasks;
using RouterControl.Interfaces.Services;

namespace RouterControl.Interfaces.Strategies
{
    internal interface ICommandExecutionStrategy
    {
        bool IsEnabled { get; }
        Task InvokeAsync(IRouterControlService routerControlService, IProgress<string> progress);
    }
}