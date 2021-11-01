using System;
using System.Collections.Generic;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Infrastructure.Trackers;
using RouterControl.Interfaces.Services;

namespace RouterControl.Infrastructure.Trackers
{
    internal class SettingsEventTracker : ISettingsEventTracker
    {
        private readonly IReadOnlySettingsService _settingsService;
        private readonly Dictionary<string, IObserver<ISettingsItem>> _listeners;
        private readonly object _listenersLock;

        public SettingsEventTracker(IReadOnlySettingsService settingsService)
        {
            Guard.ThrowIfNull(settingsService, out _settingsService, nameof(settingsService));

            settingsService.Changed += SettingsServiceChanged;

            _listeners = new Dictionary<string, IObserver<ISettingsItem>>();
            _listenersLock = new object();
        }

        private void SettingsServiceChanged(object? sender, SettingsChangedArgs args)
        {
            Guard.ThrowIfNull(args, nameof(args));
            Guard.ThrowIfEmptyString(args.Name, nameof(args.Name));
            Guard.ThrowIfNull(args.Item, nameof(args.Item));

            lock (_listenersLock)
            {
                if (_listeners.TryGetValue(args.Name, out var listener))
                    listener.OnNext(args.Item);
            }
        }

        public void Register(string settingsName, IObserver<ISettingsItem> listener)
        {
            Guard.ThrowIfEmptyString(settingsName, nameof(settingsName));
            Guard.ThrowIfNull(listener, nameof(listener));

            lock (_listenersLock)
                _listeners.Add(settingsName, listener);
        }

        public void Dispose()
        {
            _settingsService.Changed -= SettingsServiceChanged;

            lock (_listenersLock)
                _listeners.Clear();
        }
    }
}