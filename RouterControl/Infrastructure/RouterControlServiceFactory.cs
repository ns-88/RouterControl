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
        private readonly IReadOnlySettingsService _settingsService;
        private readonly IReadOnlyCredentialService _credentialService;

        public RouterControlServiceFactory(IApiFactory apiFactory, IReadOnlySettingsService settingsService, IReadOnlyCredentialService credentialService)
        {
            Guard.ThrowIfNull(apiFactory, out _apiFactory, nameof(apiFactory));
            Guard.ThrowIfNull(settingsService, out _settingsService, nameof(settingsService));
            Guard.ThrowIfNull(credentialService, out _credentialService, nameof(credentialService));
        }

        public IRouterControlService Create()
        {
            return new RouterControlService(_apiFactory, _settingsService, _credentialService);
        }
    }
}