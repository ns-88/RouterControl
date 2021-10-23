using RouterControl.Interfaces.Models;

namespace RouterControl.Interfaces.Services
{
    internal interface IReadOnlySettingsService
    {
        IProgramSettings ProgramSettings { get; }
    }

    internal interface ISettingsService : IReadOnlySettingsService
    {
        void LoadSettings();
        void SaveProgramSettings(IProgramSettings settings);
    }
}