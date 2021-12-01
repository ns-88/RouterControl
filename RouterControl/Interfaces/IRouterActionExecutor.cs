using System;
using System.Threading.Tasks;
using RouterControl.Interfaces.Models;
using RouterControl.Interfaces.RouterActions;

namespace RouterControl.Interfaces.Executors
{
    internal interface IRouterActionExecutor
    {
        IProgramSettings Settings { get; }

        bool CanExecuteAction { get; }

        Task ExecuteActionAsync(IRouterAction action, IProgress<string>? progress = null);
    }
}