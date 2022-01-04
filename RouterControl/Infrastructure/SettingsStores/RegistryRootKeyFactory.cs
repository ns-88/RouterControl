using System;
using Microsoft.Win32;

namespace RouterControl.Infrastructure.SettingsStores
{
    using Utilities;

    internal class RegistryRootKeyFactory
    {
        private readonly bool _onlyOpen;
        private readonly string _path;

        public RegistryRootKeyFactory(string path, bool onlyOpen)
        {
            Guard.ThrowIfEmptyString(path, nameof(path));

            _path = path;
            _onlyOpen = onlyOpen;
        }

        public RegistryKey CreateKey()
        {
            var rootKey = Registry.CurrentUser.OpenSubKey(_path, true);

            if (rootKey == null && !_onlyOpen)
                rootKey = Registry.CurrentUser.CreateSubKey(_path, true);

            if (rootKey == null)
                throw new InvalidOperationException($"Не удалось получить ветку реестра \"{_path}\".");

            return rootKey;
        }
    }
}