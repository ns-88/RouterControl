using MikroTikMiniApi.Interfaces.Factories;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Executors;
using RouterControl.Interfaces.Infrastructure;
using RouterControl.Interfaces.Models;
using RouterControl.Interfaces.Services;

namespace RouterControl.Infrastructure.Factories
{
    internal class RouterActionExecutorFactory : IRouterActionExecutorFactory
    {
        private readonly IApiFactory _apiFactory;
        private readonly IProgramSettings _settings;
        private readonly IReadOnlyCredentialService _credentialService;

        public RouterActionExecutorFactory(IApiFactory apiFactory,
                                           IReadOnlySettingsService settingsService,
                                           IReadOnlyCredentialService credentialService)
        {
            Guard.ThrowIfNull(apiFactory, out _apiFactory, nameof(apiFactory));
            Guard.ThrowIfNull(settingsService, nameof(settingsService));
            Guard.ThrowIfNull(settingsService.ProgramSettings, nameof(settingsService.ProgramSettings));
            Guard.ThrowIfNull(credentialService, out _credentialService, nameof(credentialService));

            _apiFactory = apiFactory;
            _settings = settingsService.ProgramSettings;
            _credentialService = credentialService;
        }

        public IRouterActionExecutor Create()
        {
            return new RouterActionExecutor(_apiFactory, _settings, _credentialService);
        }
    }
}