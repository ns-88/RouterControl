using System;
using System.Threading.Tasks;
using MikroTikMiniApi.Interfaces;
using RouterControl.Interfaces.Models;

namespace RouterControl.Interfaces.RouterActions
{
    internal interface IRouterAction
    {
        Task ExecuteAsync(IRouterApi routerApi, IProgramSettings settings, IProgress<string>? progress);
    }
}