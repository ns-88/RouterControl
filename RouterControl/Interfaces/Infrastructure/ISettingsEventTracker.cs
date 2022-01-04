using System;

namespace RouterControl.Interfaces.Infrastructure.Trackers
{
    using Services;

    internal interface ISettingsEventTracker : IDisposable
    {
        void Register(string settingsName, IObserver<ISettingsItem> listener);
    }
}