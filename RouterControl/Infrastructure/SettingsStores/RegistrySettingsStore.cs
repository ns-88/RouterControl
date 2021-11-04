using System;
using System.Linq;
using Microsoft.Win32;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.SettingsStores;

namespace RouterControl.Infrastructure.SettingsStores
{
    internal class RegistrySettingsStore : IReadableSettingsStore, IWriteableSettingsStore
    {
        private readonly RegistryRootKeyFactory _rootKeyFactory;

        public RegistrySettingsStore(RegistryRootKeyFactory rootKeyFactory)
        {
            Guard.ThrowIfNull(rootKeyFactory, out _rootKeyFactory, nameof(rootKeyFactory));
        }

        public RegistrySettingsStore()
        {
            _rootKeyFactory = new RegistryRootKeyFactory(@"Software\RouterControl", false);
        }

        private static RegistryKey OpenOrCreateKey(RegistryKey parentKey, string keyName, bool create = true)
        {
            var key = parentKey.OpenSubKey(keyName, true);

            if (key == null && create)
                key = parentKey.CreateSubKey(keyName);

            return key ?? throw new InvalidOperationException($"Не удалось получить ключ реестра \"{keyName}\".");
        }

        private static void SetValueInternal<T>(T value,
                                                string collectionName,
                                                string propertyName,
                                                RegistryValueKind registryValueKind,
                                                RegistryRootKeyFactory rootKeyFactory)
            where T : notnull
        {
            Guard.ThrowIfNull(value, nameof(value));
            Guard.ThrowIfEmptyString(collectionName, nameof(collectionName));
            Guard.ThrowIfEmptyString(propertyName, nameof(propertyName));

            using var rootKey = rootKeyFactory.CreateKey();
            using var collectionKey = OpenOrCreateKey(rootKey, collectionName);

            collectionKey.SetValue(propertyName, value, registryValueKind);
        }

        private static T GetValueInternal<T>(string collectionName, string propertyName, RegistryRootKeyFactory rootKeyFactory)
        {
            Guard.ThrowIfEmptyString(collectionName, nameof(collectionName));
            Guard.ThrowIfEmptyString(propertyName, nameof(propertyName));

            using var rootKey = rootKeyFactory.CreateKey();
            using var collectionKey = OpenOrCreateKey(rootKey, collectionName);
            var retValue = collectionKey.GetValue(propertyName);

            return retValue == null
                ? throw new InvalidOperationException($"Значение не указано. Ключ реестра: \"{collectionName}\", свойство: \"{propertyName}\".")
                : (T)retValue;
        }

        public bool CollectionExists(string collectionName)
        {
            using var rootKey = _rootKeyFactory.CreateKey();

            return rootKey.GetSubKeyNames()
                          .FirstOrDefault(x => x.Equals(collectionName, StringComparison.Ordinal)) != null;
        }

        public bool PropertyExists(string collectionName, string propertyName)
        {
            using var rootKey = _rootKeyFactory.CreateKey();
            using var collectionKey = rootKey.OpenSubKey(collectionName);

            return collectionKey?.GetValue(propertyName) != null;
        }

        #region Get/Set/Delete methods
        public void SetStringValue(string value, string collectionName, string propertyName)
        {
            SetValueInternal(value, collectionName, propertyName, RegistryValueKind.String, _rootKeyFactory);
        }

        public string GetStringValue(string collectionName, string propertyName)
        {
            return GetValueInternal<string>(collectionName, propertyName, _rootKeyFactory);
        }

        public void SetIntValue(int value, string collectionName, string propertyName)
        {
            SetValueInternal(value, collectionName, propertyName, RegistryValueKind.DWord, _rootKeyFactory);
        }

        public int GetIntValue(string collectionName, string propertyName)
        {
            return GetValueInternal<int>(collectionName, propertyName, _rootKeyFactory);
        }

        public void SetBytesValue(ReadOnlyMemory<byte> value, string collectionName, string propertyName)
        {
            SetValueInternal(value.ToArray(), collectionName, propertyName, RegistryValueKind.Binary, _rootKeyFactory);
        }

        public ReadOnlyMemory<byte> GetBytesValue(string collectionName, string propertyName)
        {
            return GetValueInternal<byte[]>(collectionName, propertyName, _rootKeyFactory);
        }

        public void DeleteProperty(string collectionName, string propertyName)
        {
            using var rootKey = _rootKeyFactory.CreateKey();
            using var collectionKey = OpenOrCreateKey(rootKey, collectionName, false);

            try
            {
                collectionKey.DeleteValue(propertyName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось удалить значение. Ключ реестра: \"{collectionName}\", свойство: \"{propertyName}\".\r\nОшибка: {ex.Message}", ex);
            }
        }
        #endregion
    }
}