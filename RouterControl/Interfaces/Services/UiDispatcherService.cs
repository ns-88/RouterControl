using System;

namespace RouterControl.Interfaces.Services
{
    internal interface IUiDispatcherService
    {
        void Invoke(Action action);
    }
}