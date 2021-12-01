using RouterControl.Interfaces.Services;

namespace RouterControl.Interfaces.Infrastructure.Factories
{
    internal interface IRouterServicesFactory
    {
        IRouterControlService CreateControlService();
        IRouterStateNotifierService CreateStateNotifierService();
    }
}