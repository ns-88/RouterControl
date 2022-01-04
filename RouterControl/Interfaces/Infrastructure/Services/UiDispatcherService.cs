using System;

namespace RouterControl.Interfaces.Infrastructure.Services
{
    internal interface IUiDispatcherService
    {
        void Invoke(Action action);
    }
}