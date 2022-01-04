using MikroTikMiniApi.Interfaces.Factories;

namespace RouterControl.Infrastructure.Factories
{
    using Interfaces.Executors;
    using Interfaces.Infrastructure;
    using Interfaces.Infrastructure.Services;
    using Interfaces.Models;
    using Utilities;

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