using System;
using RouterControl.Interfaces.Services;

namespace RouterControl.Interfaces.Infrastructure.Trackers
{
    internal interface ISettingsEventTracker : IDisposable
    {
        void Register(string settingsName, IObserver<ISettingsItem> listener);
    }
}