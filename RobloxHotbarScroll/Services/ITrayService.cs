using System;

namespace RobloxHotbarScroll.Services;

public interface ITrayService : IDisposable
{
    void Initialize();
    void UpdateState(bool isEnabled, bool isActive, int currentSlot, string robloxStatus);
    void ShowNotification(string title, string message);
    event EventHandler? OpenDashboardRequested;
    event EventHandler? OpenSettingsRequested;
    event EventHandler? OpenLogsRequested;
    event EventHandler? ExitRequested;
}
