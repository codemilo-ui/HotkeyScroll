using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RobloxHotbarScroll.Models;
using RobloxHotbarScroll.Services;

namespace RobloxHotbarScroll.Tests;

public class TestLogger : ILoggerService
{
    public ObservableCollection<LogEntry> Entries { get; } = new();
    public List<string> LogMessages { get; } = new();
    public string LogFilePath => "test.log";

    public void LogInfo(string message, string? details = null) => LogMessages.Add($"[INFO] {message}");
    public void LogDebug(string message, string? details = null) => LogMessages.Add($"[DEBUG] {message}");
    public void LogWarning(string message, string? details = null) => LogMessages.Add($"[WARN] {message}");
    public void LogError(string message, Exception? ex = null) => LogMessages.Add($"[ERROR] {message}");
    public void LogException(string context, Exception ex) => LogMessages.Add($"[EXCEPTION] {context}: {ex.Message}");
    public void Clear() => LogMessages.Clear();
}

public class TestInputSimulator : IInputSimulator
{
    public List<int> SentSlots { get; } = new();

    public bool SendSlotKey(int slot)
    {
        SentSlots.Add(slot);
        return true;
    }
}

public class TestRobloxDetector : IRobloxDetector
{
    public bool IsRobloxRunning { get; set; } = true;
    public bool IsRobloxForeground { get; set; } = true;
    public RobloxProcessState State { get; set; } = new()
    {
        IsRunning = true,
        IsFocused = true,
        ProcessName = "RobloxPlayerBeta"
    };

    public event EventHandler<RobloxProcessState>? StateChanged;

    public void TriggerFocusChange(bool focused)
    {
        IsRobloxForeground = focused;
        State.IsFocused = focused;
        StateChanged?.Invoke(this, State);
    }

    public void Start() { }
    public void Stop() { }
    public void RefreshNow() { }
    public void Dispose() { }
}

public class TestSettingsService : ISettingsService
{
    public AppSettings CurrentSettings { get; set; } = new();
    public event EventHandler<AppSettings>? SettingsChanged;

    public void SaveSettings()
    {
        SettingsChanged?.Invoke(this, CurrentSettings);
    }

    public void ResetToDefaults()
    {
        CurrentSettings = new AppSettings();
        SaveSettings();
    }

    public void SetStartWithWindows(bool enable)
    {
        CurrentSettings.StartWithWindows = enable;
    }

    public bool CheckStartWithWindows() => CurrentSettings.StartWithWindows;
}

public class TestToastNotificationService : IToastNotificationService
{
    public ObservableCollection<ToastNotification> ActiveToasts { get; } = new();
    public List<ToastNotification> ShownToasts { get; } = new();
    public event EventHandler<ToastNotification>? ToastShown;

    public void Show(string title, string message, ToastType type = ToastType.Info)
    {
        var toast = new ToastNotification(title, message, type);
        ActiveToasts.Add(toast);
        ShownToasts.Add(toast);
        ToastShown?.Invoke(this, toast);
    }

    public void Dismiss(ToastNotification toast)
    {
        ActiveToasts.Remove(toast);
    }
}

public class TestTrayService : ITrayService
{
#pragma warning disable CS0067
    public bool Initialized { get; private set; }
    public bool LastEnabled { get; private set; }
    public bool LastActive { get; private set; }
    public int LastSlot { get; private set; }
    public string? LastRobloxStatus { get; private set; }

    public event EventHandler? OpenDashboardRequested;
    public event EventHandler? OpenSettingsRequested;
    public event EventHandler? OpenLogsRequested;
    public event EventHandler? ExitRequested;
#pragma warning restore CS0067

    public void Initialize() => Initialized = true;

    public void UpdateState(bool isEnabled, bool isActive, int currentSlot, string robloxStatus)
    {
        LastEnabled = isEnabled;
        LastActive = isActive;
        LastSlot = currentSlot;
        LastRobloxStatus = robloxStatus;
    }

    public void ShowNotification(string title, string message) { }
    public void Dispose() { }
}

public class TestInputManager : IInputManager
{
    public bool IsRunning { get; set; } = true;
    public bool IsHookActive { get; set; } = true;
    public string? LastError { get; set; }
    public event EventHandler<bool>? HookStatusChanged;

    public void Start() { IsRunning = true; IsHookActive = true; }
    public void Stop() { IsRunning = false; IsHookActive = false; }
    public void Dispose() { Stop(); }

    public void TriggerHookFailure(string error)
    {
        LastError = error;
        IsHookActive = false;
        HookStatusChanged?.Invoke(this, false);
    }
}

public class TestUpdateService : IUpdateService
{
    public UpdateInfo? MockUpdate { get; set; }
    public bool CheckCalled { get; private set; }
    public bool OpenReleasePageCalled { get; private set; }
    public string? OpenedReleasePageUrl { get; private set; }

    public UpdateInfo? LastCheckResult => MockUpdate;
    public bool IsChecking { get; set; }
    public DateTime? LastCheckTime { get; set; } = DateTime.Now;
    public string StatusMessage { get; set; } = "You're up to date.";
    public bool HasError { get; set; }

#pragma warning disable CS0067
    public event EventHandler<UpdateInfo>? UpdateAvailable;
    public event EventHandler<bool>? CheckingStatusChanged;
#pragma warning restore CS0067

    public System.Threading.Tasks.Task<UpdateInfo?> CheckForUpdatesAsync(bool isManual, System.Threading.CancellationToken cancellationToken = default)
    {
        CheckCalled = true;
        if (MockUpdate != null)
        {
            UpdateAvailable?.Invoke(this, MockUpdate);
        }
        return System.Threading.Tasks.Task.FromResult(MockUpdate);
    }

    public void OpenReleasePage(string? url = null)
    {
        OpenReleasePageCalled = true;
        OpenedReleasePageUrl = url;
    }
}
