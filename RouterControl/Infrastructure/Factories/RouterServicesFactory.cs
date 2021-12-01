using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Infrastructure;
using RouterControl.Interfaces.Infrastructure.Factories;
using RouterControl.Interfaces.Services;
using RouterControl.Services;

namespace RouterControl.Infrastructure.Factories
{
    internal class RouterServicesFactory : IRouterServicesFactory
    {
        private readonly IRouterActionExecutorFactory _actionExecutorFactory;

        public RouterServicesFactory(IRouterActionExecutorFactory actionExecutorFactory)
        {
            Guard.ThrowIfNull(actionExecutorFactory, out _actionExecutorFactory, nameof(actionExecutorFactory));
        }

        public IRouterControlService CreateControlService()
        {
            return new RouterControlService(_actionExecutorFactory);
        }

        public IRouterStateNotifierService CreateStateNotifierService()
        {
            return new RouterStateNotifierService(_actionExecutorFactory);
        }
    }
}