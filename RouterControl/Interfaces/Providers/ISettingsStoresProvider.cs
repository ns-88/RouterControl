using RouterControl.Interfaces.SettingsStores;

namespace RouterControl.Interfaces.Providers
{
    internal interface ISettingsStoresProvider
    {
        IReadableSettingsStore ReadableSettingsStore { get; }
        IWriteableSettingsStore WriteableSettingsStore { get; }
    }
}