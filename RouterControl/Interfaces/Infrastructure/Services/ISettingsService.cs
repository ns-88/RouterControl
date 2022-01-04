using System;
using RouterControl.Interfaces.Models;

namespace RouterControl.Interfaces.Infrastructure.Services
{
    internal interface IReadOnlySettingsService
    {
        event EventHandler<SettingsChangedArgs> Changed;
        IProgramSettings ProgramSettings { get; }
    }

    internal interface ISettingsService : IReadOnlySettingsService
    {
        void LoadSettings();
        void SaveProgramSettings(IProgramSettings settings);
    }

    internal interface ISettingsItem
    {

    }

    internal class SettingsChangedArgs : EventArgs
    {
        public readonly string Name;
        public readonly ISettingsItem Item;

        public SettingsChangedArgs(string name, ISettingsItem item)
        {
            Name = name;
            Item = item;
        }
    }
}