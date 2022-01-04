using RouterControl.Interfaces.Executors;

namespace RouterControl.Interfaces.Infrastructure
{
    internal interface IRouterActionExecutorFactory
    {
        IRouterActionExecutor Create();
    }
}