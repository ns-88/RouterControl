using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RouterControl.Interfaces.Models;

namespace RouterControl.Interfaces.Services
{
    internal interface IRouterStateObservableService
    {
        IDisposable Subscribe(IObserver<IRouterInterface> observer, string interfaceName);
    }

    internal interface IRouterStateNotifierService : IRouterStateObservableService
    {
        Task StartNotificationsAsync(CancellationToken token);
        Task<IReadOnlyList<IRouterInterface>> GetInterfacesInfoAsync();
    }
}