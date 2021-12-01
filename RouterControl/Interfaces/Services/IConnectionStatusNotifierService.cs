using System;
using System.Threading;
using System.Threading.Tasks;

namespace RouterControl.Interfaces.Services
{
    internal interface IConnectionStateNotifierService : IDisposable
    {
        event EventHandler<bool> StateChanged;

        Task StartNotificationsAsync(CancellationToken token);

        Task<string> GetRemoteIpAddressAsync(CancellationToken token);
    }
}