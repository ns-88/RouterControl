using System;
using System.Threading.Tasks;

namespace RouterControl.Interfaces.Services
{
    internal interface IRouterControlService
    {
        Task ChangeConnectionStateAsync(bool enable, IProgress<string> progress);
    }
}