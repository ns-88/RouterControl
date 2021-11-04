using System;
using System.Net;
using System.Threading;
using RouterControl.Infrastructure.Exceptions;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Models;
using RouterControl.Interfaces.Providers;
using RouterControl.Interfaces.Services;
using RouterControl.Models;

namespace RouterControl.Services
{
    internal class SettingsService : ISettingsService
    {
        public event EventHandler<SettingsChangedArgs>? Changed;
        public IProgramSettings ProgramSettings { get; }

        public SettingsService(ISettingsStoresProvider settingsProvider)
        {
            ProgramSettings = new ProgramSettingsItem(settingsProvider, RaiseSettingsChanged);
        }

        private void RaiseSettingsChanged(string name, ISettingsItem item)
        {
            Volatile.Read(ref Changed)?.Invoke(this, new SettingsChangedArgs(name, item));
        }

        public void LoadSettings()
        {
            ((ProgramSettingsItem)ProgramSettings).Load();
        }

        public void SaveProgramSettings(IProgramSettings settings)
        {
            ((ProgramSettingsItem)ProgramSettings).Save(settings);
        }

        #region Nested types

        private abstract class SettingsItem<TSource> : ISettingsItem
            where TSource : class
        {
            private readonly string _settingsName;
            private readonly string _contractName;
            private readonly string _collectionName;
            private readonly ISettingsStoresProvider _settingsProvider;
            private readonly Action<string, ISettingsItem> _changeNotify;
            private readonly object _lockObject;

            protected SettingsItem(string settingsName,
                                   string contractName,
                                   string collectionName,
                                   ISettingsStoresProvider settingsProvider,
                                   Action<string, ISettingsItem> changeNotify)
            {
                _settingsName = settingsName;
                _contractName = contractName;
                _collectionName = collectionName;
                _changeNotify = changeNotify;
                _settingsProvider = settingsProvider;
                _lockObject = new object();
            }

#nullable disable
            // ReSharper disable once NotNullMemberIsNotInitialized
            protected SettingsItem()
            {
            }
#nullable restore

            #region Get/Set methods
            protected string GetStringValue(string propertyName)
            {
                var store = _settingsProvider.ReadableSettingsStore;

                return store.PropertyExists(_collectionName, propertyName)
                    ? store.GetStringValue(_collectionName, propertyName)
                    : string.Empty;
            }

            protected void SetStringValue(string value, string propertyName)
            {
                var store = _settingsProvider.WriteableSettingsStore;
                store.SetStringValue(value, _collectionName, propertyName);
            }

            protected int GetIntValue(string propertyName)
            {
                var store = _settingsProvider.ReadableSettingsStore;

                return store.PropertyExists(_collectionName, propertyName)
                    ? store.GetIntValue(_collectionName, propertyName)
                    : 0;
            }

            protected void SetIntValue(int value, string propertyName)
            {
                var store = _settingsProvider.WriteableSettingsStore;
                store.SetIntValue(value, _collectionName, propertyName);
            }

            protected ReadOnlyMemory<byte> GetBytesValue(string propertyName)
            {
                var store = _settingsProvider.ReadableSettingsStore;

                return store.PropertyExists(_collectionName, propertyName)
                    ? store.GetBytesValue(_collectionName, propertyName)
                    : ReadOnlyMemory<byte>.Empty;
            }

            protected void SetBytesValue(ReadOnlyMemory<byte> value, string propertyName)
            {
                var store = _settingsProvider.WriteableSettingsStore;
                store.SetBytesValue(value, _collectionName, propertyName);
            }

            protected bool GetBoolValue(string propertyName)
            {
                var store = _settingsProvider.ReadableSettingsStore;

                if (!store.PropertyExists(_collectionName, propertyName))
                    return false;

                var text = store.GetStringValue(_collectionName, propertyName);

                return !bool.TryParse(text, out var value)
                    ? throw new SettingsSaveLoadFaultException($"Значение \"{text}\" имеет неверный формат.")
                    : value;
            }

            protected void SetBoolValue(bool value, string propertyName)
            {
                var store = _settingsProvider.WriteableSettingsStore;
                var text = value
                    ? bool.TrueString
                    : bool.FalseString;

                store.SetStringValue(text, _collectionName, propertyName);
            }
            #endregion

            public void Save(TSource source)
            {
                Guard.ThrowIfNull(source, nameof(source));

                lock (_lockObject)
                {
                    try
                    {
                        OnSave(source);

                        Update(source);
                        
                        _changeNotify(_contractName, this);
                    }
                    catch (Exception ex)
                    {
                        throw new SettingsSaveLoadFaultException($"Сохранение настроек \"{_settingsName}\" не было завершено.\r\nОшибка: {ex.Message}", ex);
                    }
                }
            }

            public void Load()
            {
                lock (_lockObject)
                {
                    try
                    {
                        var settings = OnLoad();

                        if (settings == null)
                            throw new SettingsSaveLoadFaultException($"Настройки \"{_settingsName}\" не были получены.");

                        Update(settings);

                        _changeNotify(_contractName, this);
                    }
                    catch (Exception ex)
                    {
                        throw new SettingsSaveLoadFaultException($"Загрузка настроек \"{_settingsName}\" не была завершена успешно.\r\nОшибка: {ex.Message}", ex);
                    }
                }
            }

            protected abstract void OnSave(TSource source);

            protected abstract TSource OnLoad();

            protected abstract void Update(TSource source);
        }

        private class ProgramSettingsItem : SettingsItem<IProgramSettings>, IProgramSettings
        {
            public string UserName { get; private set; }
            public ReadOnlyMemory<byte> UserPassword { get; private set; }
            public IPEndPoint RouterAddress { get; private set; }
            public INetworkInterfaces NetworkInterfaces { get; private set; }
            public bool IsApplicationAutorun { get; private set; }

            private ProgramSettingsItem(string userName,
                                        ReadOnlyMemory<byte> userPassword,
                                        IPEndPoint routerAddress,
                                        INetworkInterfaces networkInterfaces,
                                        bool isApplicationAutorun)
            {
                UserName = userName;
                UserPassword = userPassword;
                RouterAddress = routerAddress;
                NetworkInterfaces = networkInterfaces;
                IsApplicationAutorun = isApplicationAutorun;
            }

            public ProgramSettingsItem(ISettingsStoresProvider settingsProvider,
                                       Action<string, ISettingsItem> changeNotify)
                : base("Общие настройки приложения",
                       nameof(IProgramSettings),
                       "CommonSettings",
                       settingsProvider,
                       changeNotify)
            {
                UserName = string.Empty;
                UserPassword = ReadOnlyMemory<byte>.Empty;
                RouterAddress = new IPEndPoint(IPAddress.Loopback, IPEndPoint.MinPort);
                NetworkInterfaces = new NetworkInterfacesModel();
                IsApplicationAutorun = false;
            }

            private IPEndPoint GetRouterAddress()
            {
                var rawAddress = GetStringValue(nameof(IProgramSettings.RouterAddress.Address));
                var port = GetIntValue(nameof(IProgramSettings.RouterAddress.Port));

                if (!IPAddress.TryParse(rawAddress, out var address))
                    address = IPAddress.None;

                return new IPEndPoint(address, port);
            }

            private INetworkInterfaces GetNetworkInterfaces()
            {
                return new NetworkInterfacesModel(GetStringValue(nameof(INetworkInterfaces.PppoeInterface)),
                                                  GetStringValue(nameof(INetworkInterfaces.EtherInterface)));
            }

            protected override void OnSave(IProgramSettings source)
            {
                // UserName
                SetStringValue(source.UserName, nameof(IProgramSettings.UserName));
                // UserPassword
                SetBytesValue(source.UserPassword, nameof(IProgramSettings.UserPassword));
                // Port
                SetIntValue(source.RouterAddress.Port, nameof(IProgramSettings.RouterAddress.Port));
                // Address
                SetStringValue(source.RouterAddress.Address.ToString(), nameof(IProgramSettings.RouterAddress.Address));
                // PppoeInterface
                SetStringValue(source.NetworkInterfaces.PppoeInterface, nameof(INetworkInterfaces.PppoeInterface));
                // EtherInterface
                SetStringValue(source.NetworkInterfaces.EtherInterface, nameof(INetworkInterfaces.EtherInterface));
                // IsApplicationAutorun
                SetBoolValue(source.IsApplicationAutorun, nameof(IProgramSettings.IsApplicationAutorun));
            }

            protected override IProgramSettings OnLoad()
            {
                return new ProgramSettingsItem(GetStringValue(nameof(IProgramSettings.UserName)),
                                               GetBytesValue(nameof(IProgramSettings.UserPassword)),
                                               GetRouterAddress(),
                                               GetNetworkInterfaces(),
                                               GetBoolValue(nameof(IProgramSettings.IsApplicationAutorun)));
            }

            protected override void Update(IProgramSettings source)
            {
                UserName = source.UserName;
                UserPassword = source.UserPassword;
                RouterAddress = source.RouterAddress;
                NetworkInterfaces = source.NetworkInterfaces;
                IsApplicationAutorun = source.IsApplicationAutorun;
            }
        }

        #endregion
    }
}