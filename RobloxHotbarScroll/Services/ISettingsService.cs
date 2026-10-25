using System;
using RobloxHotbarScroll.Models;

namespace RobloxHotbarScroll.Services;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    event EventHandler<AppSettings>? SettingsChanged;
    void SaveSettings();
    void ResetToDefaults();
    void SetStartWithWindows(bool enable);
    bool CheckStartWithWindows();
}
