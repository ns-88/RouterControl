using System;

namespace RouterControl.Infrastructure.Services
{
    using Interfaces.Infrastructure.Services;
    using Interfaces.Models;
    using SettingsStores;

    internal class ApplicationAutorunService : IObserver<ISettingsItem>
    {
        private static string GetBaseDirectory()
        {
            var directory = AppContext.BaseDirectory;

            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Не удалось получить базовую директорию приложения.");

            return directory;
        }

        public void OnNext(ISettingsItem value)
        {
            const string collectionName = "Run";
            const string propertyName = "RouterControl";

            try
            {
                var factory = new RegistryRootKeyFactory(@"Software\Microsoft\Windows\CurrentVersion", true);
                var store = new RegistrySettingsStore(factory);

                if (value is not IProgramSettings settings)
                    throw new InvalidOperationException($"Объект настроек имеет тип отличный от ожидаемого. Ожидаемый тип: \"{nameof(IProgramSettings)}\", текущий: \"{value.GetType().Name}\"");

                if (settings.IsApplicationAutorun)
                {
                    store.SetStringValue($"{GetBaseDirectory()}RouterControl.exe", collectionName, propertyName);
                }
                else
                {
                    if (store.PropertyExists(collectionName, propertyName))
                        store.DeleteProperty(collectionName, propertyName);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Не удалось применить настройки автозапуска приложения.", ex);
            }
        }

        #region NotImplemented
        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}