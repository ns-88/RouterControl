using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MikroTikMiniApi.Commands;
using MikroTikMiniApi.Interfaces;
using MikroTikMiniApi.Interfaces.Factories;
using MikroTikMiniApi.Interfaces.Sentences;
using MikroTikMiniApi.Models.Api;

namespace RouterControl.Services
{
    using Infrastructure.Resources;
    using Infrastructure.RouterActions;
    using Infrastructure.Utilities;
    using Interfaces.Infrastructure;
    using Interfaces.Models;
    using Interfaces.Services;
    using Observers = Dictionary<string, IObserver<Interfaces.Models.IRouterInterface>>;

    internal class RouterStateNotifierService : IRouterStateNotifierService
    {
        private readonly ObserversManager _observersManager;
        private readonly IRouterActionExecutorFactory _actionExecutorFactory;

        public RouterStateNotifierService(IRouterActionExecutorFactory actionExecutorFactory)
        {
            Guard.ThrowIfNull(actionExecutorFactory, out _actionExecutorFactory, nameof(actionExecutorFactory));
            _observersManager = new ObserversManager();
        }

        private void UpdatingData(object? sender, IReadOnlyList<IRouterInterface> models)
        {
            foreach (var model in models)
            {
                if (_observersManager.TryGetObserver(model.Name, out var observer))
                    observer.OnNext(model);
            }
        }

        public IDisposable Subscribe(IObserver<IRouterInterface> observer, string interfaceName)
        {
            return _observersManager.Add(observer, interfaceName);
        }

        public async Task StartNotificationsAsync(CancellationToken token)
        {
            var executor = _actionExecutorFactory.Create();
            var action = new RouterStatusNotifierAction(token);

            action.UpdatingData += UpdatingData;

            try
            {
                await executor.ExecuteActionAsync(action).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Обновление данных интерфейсов роутера не было завершено.", ex);
            }
            finally
            {
                action.UpdatingData -= UpdatingData;
            }
        }

        public async Task<IReadOnlyList<IRouterInterface>> GetInterfacesInfoAsync()
        {
            var executor = _actionExecutorFactory.Create();
            var action = new RouterInterfacesInfoAction();

            try
            {
                await executor.ExecuteActionAsync(action).ConfigureAwait(false);

                return action.Interfaces!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Список интерфейсов роутера не был получен.", ex);
            }
        }

        #region Nested types

        private class ObserversManager
        {
            private readonly Observers _observers;
            private readonly object _observersLock;

            public ObserversManager()
            {
                _observers = new Observers();
                _observersLock = new object();
            }

            public IDisposable Add(IObserver<IRouterInterface> observer, string interfaceName)
            {
                Guard.ThrowIfNull(observer, nameof(observer));
                Guard.ThrowIfEmptyString(interfaceName, nameof(interfaceName));

                lock (_observersLock)
                {
                    if (_observers.ContainsKey(interfaceName))
                        throw new InvalidOperationException($"Подписчик для интерфейса \"{interfaceName}\" был добавлен ранее.");

                    _observers.Add(interfaceName, observer);

                    return new Unsubscriber(interfaceName, _observers, _observersLock);
                }
            }

            public bool TryGetObserver(string interfaceName, [MaybeNullWhen(false)] out IObserver<IRouterInterface> observer)
            {
                return _observers.TryGetValue(interfaceName, out observer);
            }

            #region Nested types

            private readonly struct Unsubscriber : IDisposable
            {
                private readonly string _interfaceName;
                private readonly Observers _observers;
                private readonly object _observersLock;

                public Unsubscriber(string interfaceName, Observers observers, object observersLock)
                {
                    _interfaceName = interfaceName;
                    _observers = observers;
                    _observersLock = observersLock;
                }

                public void Dispose()
                {
                    lock (_observersLock)
                    {
                        _observers.Remove(_interfaceName);
                    }
                }
            }

            #endregion
        }

        private abstract class RouterStatusActionBase : RouterActionBase
        {
            protected static async Task<IReadOnlyList<IRouterInterface>> GetInterfacesInfoAsync(IRouterApi routerApi)
            {
                var hosts = new Dictionary<string, string>();

                try
                {
                    // Получение данных из таблицы Bridge/Hosts.
                    var hostsEnumerable = routerApi.ExecuteCommandToEnumerableAsync<HostModel>(ApiCommand.New("/interface/bridge/host/print")
                        .AddParameter("=.proplist=mac-address,on-interface")
                        .AddParameter("?external=true")
                        .Build());

                    await foreach (var host in hostsEnumerable.ConfigureAwait(false))
                    {
                        CheckingModels.Check(host);

                        if (hosts.ContainsKey(host.Interface))
                            throw new InvalidOperationException(string.Format(Strings.ItemHasAlreadyBeenAdded, host.Interface));

                        hosts.Add(host.Interface, host.MacAddress);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(string.Format(Strings.FailedGettingDataFromTable, "Bridge/Hosts", ex.Message), ex);
                }

                var leasesAddresses = new Dictionary<string, (string IpAddress, string HostName)>();

                try
                {
                    // Получение данных из таблицы IP/DHCP Server/Leases.
                    var leasesAddressesEnumerable = routerApi.ExecuteCommandToEnumerableAsync<LeaseAddressModel>(ApiCommand.New("/ip/dhcp-server/lease/print")
                            .AddParameter("=.proplist=address,mac-address,host-name")
                            .Build());

                    await foreach (var address in leasesAddressesEnumerable.ConfigureAwait(false))
                    {
                        CheckingModels.Check(address);

                        if (hosts.ContainsKey(address.MacAddress))
                            throw new InvalidOperationException(string.Format(Strings.ItemHasAlreadyBeenAdded, address.MacAddress));

                        leasesAddresses.Add(address.MacAddress, (address.IpAddress, address.HostName));
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(string.Format(Strings.FailedGettingDataFromTable, "IP/DHCP Server/Leases", ex.Message), ex);
                }

                var routerInterfaces = new List<IRouterInterface>();

                try
                {
                    // Получение данных из таблицы Interfaces и объединение с данными из таблиц IP/Addresses, Bridge/Hosts и IP/DHCP Server/Leases.
                    var interfacesList = await routerApi.ExecuteCommandToListAsync<Interface>(ApiCommand.New("/interface/print")
                            .AddParameter("=.proplist=name,type,disabled")
                            .Build())
                        .ConfigureAwait(false);

                    foreach (var @interface in interfacesList)
                    {
                        CheckingModels.Check(@interface);

                        var name = @interface.Name!;
                        var isEnabled = !@interface.IsDisabled!.Value;
                        var type = @interface.Type!;

                        if (type.Equals("pppoe-out", StringComparison.OrdinalIgnoreCase))
                        {
                            if (isEnabled)
                            {
                                IReadOnlyList<Address> pppoeAddresses;

                                try
                                {
                                    pppoeAddresses = await routerApi.ExecuteCommandToListAsync<Address>(ApiCommand.New("/ip/address/print")
                                            .AddParameter("=.proplist=address")
                                            .AddParameter($"?interface={name}")
                                            .Build())
                                        .ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    throw new InvalidOperationException("Команда не была выполнена.", ex);
                                }

                                try
                                {
                                    switch (pppoeAddresses.Count)
                                    {
                                        case 0:
                                            routerInterfaces.Add(new RouterInterfaceModel(isEnabled, name));
                                            break;
                                        case 1:
                                            {
                                                var address = pppoeAddresses[0];

                                                CheckingModels.Check(address);

                                                var tmp = address.IpAddress.Split('/', StringSplitOptions.RemoveEmptyEntries);

                                                if (tmp.Length != 2)
                                                    throw new InvalidOperationException("Неверный формат строки.");

                                                routerInterfaces.Add(new RouterInterfaceModel(isEnabled, name, string.Empty, string.Empty, tmp[0]));
                                            }
                                            break;
                                        default:
                                            throw new InvalidOperationException("Неверный ответ API.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    throw new InvalidOperationException($"IP-адрес для PPPoE интерфейса \"{name}\" не был получен.", ex);
                                }
                            }
                            else
                            {
                                routerInterfaces.Add(new RouterInterfaceModel(isEnabled, name));
                            }

                            continue;
                        }

                        if (!hosts.TryGetValue(name, out var macAddress))
                        {
                            routerInterfaces.Add(new RouterInterfaceModel(isEnabled, name));
                            continue;
                        }

                        if (!leasesAddresses.TryGetValue(macAddress, out var interfaceInfo))
                        {
                            routerInterfaces.Add(new RouterInterfaceModel(isEnabled, name, macAddress));
                            continue;
                        }

                        routerInterfaces.Add(new RouterInterfaceModel(isEnabled, name, macAddress, interfaceInfo.HostName, interfaceInfo.IpAddress));
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(string.Format(Strings.FailedGettingDataFromTable, "Interfaces", ex.Message), ex);
                }

                return routerInterfaces;
            }

            #region Nested types

#nullable disable
            private class RouterInterfaceModel : IRouterInterface
            {
                public bool IsEnabled { get; }
                public string Name { get; }
                public string ClientMacAddress { get; }
                public string ClientName { get; }
                public string ClientIpAddress { get; }

                public RouterInterfaceModel(bool isEnabled, string name)
                {
                    IsEnabled = isEnabled;
                    Name = name;
                    ClientMacAddress = string.Empty;
                    ClientName = string.Empty;
                    ClientIpAddress = string.Empty;
                }

                public RouterInterfaceModel(bool isEnabled, string name, string clientMacAddress)
                {
                    IsEnabled = isEnabled;
                    Name = name;
                    ClientMacAddress = clientMacAddress;
                    ClientName = string.Empty;
                    ClientIpAddress = string.Empty;
                }

                public RouterInterfaceModel(bool isEnabled, string name, string clientMacAddress, string clientName, string clientIpAddress)
                {
                    IsEnabled = isEnabled;
                    Name = name;
                    ClientMacAddress = clientMacAddress;
                    ClientName = clientName;
                    ClientIpAddress = clientIpAddress;
                }
            }

            private class HostModel : ModelBase, IModelFactory<HostModel>
            {
                public string Interface { get; private init; }
                public string MacAddress { get; private init; }

                HostModel IModelFactory<HostModel>.Create(IApiSentence sentence)
                {
                    return new HostModel
                    {
                        Interface = GetStringValueOrDefault("on-interface", sentence),
                        MacAddress = GetStringValueOrDefault("mac-address", sentence)
                    };
                }
            }

            private class LeaseAddressModel : ModelBase, IModelFactory<LeaseAddressModel>
            {
                public string IpAddress { get; private init; }
                public string MacAddress { get; private init; }
                public string HostName { get; private init; }

                LeaseAddressModel IModelFactory<LeaseAddressModel>.Create(IApiSentence sentence)
                {
                    return new LeaseAddressModel
                    {
                        IpAddress = GetStringValueOrDefault("address", sentence),
                        MacAddress = GetStringValueOrDefault("mac-address", sentence),
                        HostName = GetStringValueOrDefault("host-name", sentence)
                    };
                }
            }

            private class Address : ModelBase, IModelFactory<Address>
            {
                public string IpAddress { get; private init; }

                Address IModelFactory<Address>.Create(IApiSentence sentence)
                {
                    return new Address
                    {
                        IpAddress = GetStringValueOrDefault("address", sentence)
                    };
                }
            }
#nullable restore
            private static class CheckingModels
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void ThrowIfNull(string? propertyValue, string propertyName, string modelName)
                {
                    if (string.IsNullOrWhiteSpace(propertyValue))
                        throw new InvalidOperationException($"Значение свойства \"{propertyName}\" не задано. Модель: \"{modelName}\".");
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void ThrowIfNull<T>(T? propertyValue, string propertyName, string modelName)
                {
                    if (propertyValue == null)
                        throw new InvalidOperationException($"Значение свойства \"{propertyName}\" не задано. Модель: \"{modelName}\".");
                }

                public static void Check(HostModel model)
                {
                    const string modelName = nameof(HostModel);

                    ThrowIfNull(model.Interface, nameof(model.Interface), modelName);
                    ThrowIfNull(model.MacAddress, nameof(model.MacAddress), modelName);
                }

                public static void Check(LeaseAddressModel model)
                {
                    const string modelName = nameof(LeaseAddressModel);

                    ThrowIfNull(model.MacAddress, nameof(model.MacAddress), modelName);
                    ThrowIfNull(model.HostName, nameof(model.HostName), modelName);
                    ThrowIfNull(model.IpAddress, nameof(model.IpAddress), modelName);
                }

                public static void Check(Interface model)
                {
                    const string modelName = nameof(Interface);

                    ThrowIfNull(model.Name, nameof(model.Name), modelName);
                    ThrowIfNull(model.Type, nameof(model.Type), modelName);
                    ThrowIfNull(model.IsDisabled, nameof(model.IsDisabled), modelName);
                }

                public static void Check(Address model)
                {
                    const string modelName = nameof(Address);

                    ThrowIfNull(model.IpAddress, nameof(model.IpAddress), modelName);
                }
            }

            #endregion
        }

        private class RouterStatusNotifierAction : RouterStatusActionBase
        {
            private readonly CancellationToken _token;
            public event EventHandler<IReadOnlyList<IRouterInterface>>? UpdatingData;

            public RouterStatusNotifierAction(CancellationToken token)
            {
                _token = token;
            }

            public override async Task ExecuteAsync(IRouterApi routerApi, IProgramSettings settings, IProgress<string>? progress)
            {
                var timeout = TimeSpan.FromSeconds(1);

                while (true)
                {
                    try
                    {
                        await Task.Delay(timeout, _token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {

                    }

                    if (_token.IsCancellationRequested)
                        break;

                    var routerInterfaces = await GetInterfacesInfoAsync(routerApi).ConfigureAwait(false);

                    Volatile.Read(ref UpdatingData)?.Invoke(this, routerInterfaces);
                }
            }
        }

        private class RouterInterfacesInfoAction : RouterStatusActionBase
        {
            public IReadOnlyList<IRouterInterface>? Interfaces { get; private set; }

            public override async Task ExecuteAsync(IRouterApi routerApi, IProgramSettings settings, IProgress<string>? progress)
            {
                Interfaces = await GetInterfacesInfoAsync(routerApi).ConfigureAwait(false);
            }
        }

        #endregion
    }
}