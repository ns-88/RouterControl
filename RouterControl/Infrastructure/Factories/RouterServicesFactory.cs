using RouterControl.Services;

namespace RouterControl.Infrastructure.Factories
{
    using Interfaces.Infrastructure;
    using Interfaces.Infrastructure.Factories;
    using Interfaces.Services;
    using Utilities;

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