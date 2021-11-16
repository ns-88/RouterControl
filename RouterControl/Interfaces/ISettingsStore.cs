using System;

namespace RouterControl.Interfaces.SettingsStores
{
    internal interface ISettingsStore
    {
        bool CollectionExists(string collectionName);
        bool PropertyExists(string collectionName, string propertyName);
    }

    internal interface IReadableSettingsStore : ISettingsStore
    {
        string GetStringValue(string collectionName, string propertyName);

        int GetIntValue(string collectionName, string propertyName);

        ReadOnlyMemory<byte> GetBytesValue(string collectionName, string propertyName);
    }

    internal interface IWriteableSettingsStore : ISettingsStore
    {
        void SetStringValue(string value, string collectionName, string propertyName);

        void SetIntValue(int value, string collectionName, string propertyName);

        void SetBytesValue(ReadOnlyMemory<byte> value, string collectionName, string propertyName);
    }
}