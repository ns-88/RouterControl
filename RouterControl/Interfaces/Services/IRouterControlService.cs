using System;
using System.Threading.Tasks;

namespace RouterControl.Interfaces.Services
{
    internal interface IRouterControlService
    {
        Task ChangeInterfacesStateAsync(bool enable, IProgress<string> progress);
        ValueTask<bool> GetInterfacesStateAsync();
    }
}