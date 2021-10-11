using MikroTikMiniApi.Interfaces.Factories;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Infrastructure.Factories;
using RouterControl.Interfaces.Services;
using RouterControl.Services;

namespace RouterControl.Infrastructure.Factories
{
    internal class RouterControlServiceFactory : IRouterControlServiceFactory
    {
        private readonly IApiFactory _apiFactory;

        public RouterControlServiceFactory(IApiFactory apiFactory)
        {
            Guard.ThrowIfNull(apiFactory, out _apiFactory, nameof(apiFactory));
        }

        public IRouterControlService Create()
        {
            return new RouterControlService(_apiFactory);
        }
    }
}