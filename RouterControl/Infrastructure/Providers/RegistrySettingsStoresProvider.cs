using RouterControl.Infrastructure.SettingsStores;
using RouterControl.Interfaces.Providers;
using RouterControl.Interfaces.SettingsStores;

namespace RouterControl.Infrastructure.Providers
{
    internal class RegistrySettingsStoresProvider : ISettingsStoresProvider
    {
        public IReadableSettingsStore ReadableSettingsStore { get; }
        public IWriteableSettingsStore WriteableSettingsStore { get; }

        public RegistrySettingsStoresProvider()
        {
            var settingsStore = new RegistrySettingsStore();

            ReadableSettingsStore = settingsStore;
            WriteableSettingsStore = settingsStore;
        }
    }
}