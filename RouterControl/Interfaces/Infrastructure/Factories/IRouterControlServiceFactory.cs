using RouterControl.Interfaces.Services;

namespace RouterControl.Interfaces.Infrastructure.Factories
{
    internal interface IRouterControlServiceFactory
    {
        IRouterControlService Create();
    }
}