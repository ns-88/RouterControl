namespace RouterControl.Infrastructure.Providers
{
    using Interfaces.Providers;
    using Interfaces.SettingsStores;
    using SettingsStores;

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