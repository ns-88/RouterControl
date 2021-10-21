using System;
using System.Linq;
using Microsoft.Win32;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.SettingsStores;

namespace RouterControl.Infrastructure.SettingsStores
{
    internal class RegistrySettingsStore : IReadableSettingsStore, IWriteableSettingsStore
    {
        private static RegistryKey OpenOrCreateKey(RegistryKey parentKey, string keyName, bool create = true)
        {
            var key = parentKey.OpenSubKey(keyName, true);

            if (key == null && create)
                key = parentKey.CreateSubKey(keyName);

            return key ?? throw new InvalidOperationException($"Не удалось получить ключ реестра \"{keyName}\".");
        }

        private static void MoveToRootKey(Action<RegistryKey> action)
        {
            using var softwareKey = OpenOrCreateKey(Registry.CurrentUser, "Software");
            using var rootKey = OpenOrCreateKey(softwareKey, "RouterControl");

            action(rootKey);
        }

        private static void SetValueInternal<T>(T value, string collectionName, string propertyName, RegistryValueKind registryValueKind)
            where T : notnull
        {
            Guard.ThrowIfNull(value, nameof(value));
            Guard.ThrowIfEmptyString(collectionName, nameof(collectionName));
            Guard.ThrowIfEmptyString(propertyName, nameof(propertyName));

            MoveToRootKey(rootKey =>
            {
                using var collectionKey = OpenOrCreateKey(rootKey, collectionName);
                collectionKey.SetValue(propertyName, value, registryValueKind);
            });
        }

        private static T GetValueInternal<T>(string collectionName, string propertyName)
        {
            Guard.ThrowIfEmptyString(collectionName, nameof(collectionName));
            Guard.ThrowIfEmptyString(propertyName, nameof(propertyName));

            object? retValue = null;

            MoveToRootKey(rootKey =>
            {
                using var collectionKey = OpenOrCreateKey(rootKey, collectionName);
                retValue = collectionKey.GetValue(propertyName);
            });

            return retValue == null
                ? throw new InvalidOperationException($"Значение не указано. Коллекция: \"{collectionName}\", свойство: \"{propertyName}\".")
                : (T)retValue;
        }

        public bool CollectionExists(string collectionName)
        {
            var exist = false;

            MoveToRootKey(rootKey =>
            {
                exist = rootKey.GetSubKeyNames()
                               .FirstOrDefault(x => x.Equals(collectionName, StringComparison.Ordinal)) != null;
            });

            return exist;
        }

        public bool PropertyExists(string collectionName, string propertyName)
        {
            var exist = false;

            MoveToRootKey(rootKey =>
            {
                var collectionKey = rootKey.OpenSubKey(collectionName);

                if (collectionKey == null)
                    return;

                using (collectionKey)
                {
                    exist = collectionKey.GetValue(propertyName) != null;
                }
            });

            return exist;
        }

        public void SetStringValue(string value, string collectionName, string propertyName)
        {
            SetValueInternal(value, collectionName, propertyName, RegistryValueKind.String);
        }

        public string GetStringValue(string collectionName, string propertyName)
        {
            return GetValueInternal<string>(collectionName, propertyName)!;
        }

        public void SetIntValue(int value, string collectionName, string propertyName)
        {
            SetValueInternal(value, collectionName, propertyName, RegistryValueKind.DWord);
        }

        public int GetIntValue(string collectionName, string propertyName)
        {
            return GetValueInternal<int>(collectionName, propertyName)!;
        }

        public void SetBytesValue(ReadOnlyMemory<byte> value, string collectionName, string propertyName)
        {
            SetValueInternal(value.ToArray(), collectionName, propertyName, RegistryValueKind.Binary);
        }

        public ReadOnlyMemory<byte> GetBytesValue(string collectionName, string propertyName)
        {
            return GetValueInternal<byte[]>(collectionName, propertyName);
        }
    }
}