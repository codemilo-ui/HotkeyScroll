using System;
using RobloxHotbarScroll.Models;

namespace RobloxHotbarScroll.Services;

public class HotbarController : IHotbarController
{
    private readonly ILoggerService _logger;
    private readonly ISettingsService _settingsService;
    private readonly IRobloxDetector _robloxDetector;
    private readonly IInputSimulator _inputSimulator;
    private readonly IToastNotificationService? _toastService;
    private readonly object _lock = new();

    private int _currentSlot;

    public int CurrentSlot
    {
        get
        {
            lock (_lock)
            {
                return _currentSlot;
            }
        }
        private set
        {
            lock (_lock)
            {
                _currentSlot = value;
            }
        }
    }

    public bool IsEnabled => _settingsService.CurrentSettings.Enabled;

    public bool IsActive
    {
        get
        {
            var settings = _settingsService.CurrentSettings;
            if (!settings.Enabled) return false;

            if (settings.RobloxOnly)
            {
                if (settings.PauseWhenRobloxNotFocused && !_robloxDetector.IsRobloxForeground)
                {
                    return false;
                }
                if (!_robloxDetector.IsRobloxForeground)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public InputActivity LastInput { get; private set; } = new();

    public event EventHandler<HotbarChangedEventArgs>? SlotChanged;
    public event EventHandler<bool>? EnabledChanged;
    public event EventHandler<InputActivity>? InputActivityUpdated;

    public HotbarController(
        ILoggerService logger,
        ISettingsService settingsService,
        IRobloxDetector robloxDetector,
        IInputSimulator inputSimulator,
        IToastNotificationService? toastService = null)
    {
        _logger = logger;
        _settingsService = settingsService;
        _robloxDetector = robloxDetector;
        _inputSimulator = inputSimulator;
        _toastService = toastService;

        int startSlot = settingsService.CurrentSettings.StartingSlot;
        _currentSlot = (startSlot >= 1 && startSlot <= 10) ? startSlot : 1;
    }

    public void ToggleEnabled()
    {
        SetEnabled(!_settingsService.CurrentSettings.Enabled);
    }

    public void SetEnabled(bool enabled)
    {
        if (_settingsService.CurrentSettings.Enabled == enabled) return;

        _settingsService.CurrentSettings.Enabled = enabled;
        _settingsService.SaveSettings();

        string hotkey = _settingsService.CurrentSettings.ToggleHotkey;
        if (enabled)
        {
            _logger.LogInfo($"Remapper ENABLED via {hotkey}. Mouse wheel remapping active.");
            _toastService?.Show("Remapper Enabled", $"{hotkey} toggled the remapper ON", ToastType.Success);
        }
        else
        {
            _logger.LogInfo($"Remapper PAUSED via {hotkey}. Mouse wheel is now untouched.");
            _toastService?.Show("Remapper Paused", "Mouse wheel is now untouched", ToastType.Warning);
        }

        EnabledChanged?.Invoke(this, enabled);
    }

    public void HandleScroll(int direction)
    {
        var settings = _settingsService.CurrentSettings;

        // If paused, do nothing
        if (!settings.Enabled)
        {
            if (settings.DebugMode)
            {
                _logger.LogDebug($"Wheel {(direction < 0 ? "DOWN" : "UP")} ignored: Remapper is PAUSED");
            }
            return;
        }

        // If RobloxOnly and not focused, do nothing
        if (settings.RobloxOnly && !_robloxDetector.IsRobloxForeground)
        {
            if (settings.DebugMode)
            {
                _logger.LogDebug($"Wheel {(direction < 0 ? "DOWN" : "UP")} ignored: Roblox not focused");
            }
            return;
        }

        // Direction > 0 is Scroll UP; Direction < 0 is Scroll DOWN
        // Default: Scroll DOWN (-1) -> Next Slot (+1)
        //          Scroll UP (+1)   -> Previous Slot (-1)
        int step = direction < 0 ? 1 : -1;
        if (settings.ReverseScroll)
        {
            step = -step;
        }

        int oldSlot;
        int nextSlot;

        lock (_lock)
        {
            oldSlot = _currentSlot;
            nextSlot = CalculateNextSlot(oldSlot, step, settings.WrapAround);

            if (nextSlot == oldSlot) return;

            _currentSlot = nextSlot;
        }

        char keyChar = nextSlot == 10 ? '0' : (char)('0' + nextSlot);

        // Update live activity
        var activity = new InputActivity
        {
            ActionName = direction < 0 ? "Mouse Wheel Down" : "Mouse Wheel Up",
            CurrentSlot = oldSlot,
            TargetSlot = nextSlot,
            SentKey = $"MAIN KEY \"{keyChar}\"",
            Result = "Simulated Windows Keystroke",
            IsSimulated = true
        };
        LastInput = activity;
        InputActivityUpdated?.Invoke(this, activity);

        // Debug logging
        if (settings.DebugMode)
        {
            _logger.LogDebug($"Wheel {(direction < 0 ? "DOWN" : "UP")}");
            _logger.LogDebug($"Current Slot: {oldSlot}");
            _logger.LogDebug($"Target Slot: {nextSlot}");
        }
        else
        {
            _logger.LogInfo($"Wheel {(direction < 0 ? "DOWN" : "UP")}: Slot {oldSlot} → {nextSlot} (Pressing Key '{keyChar}')");
        }

        _inputSimulator.SendSlotKey(nextSlot);
        SlotChanged?.Invoke(this, new HotbarChangedEventArgs(nextSlot, oldSlot, SlotChangeSource.Scroll));
    }

    public void HandleManualKey(int slot)
    {
        var settings = _settingsService.CurrentSettings;
        if (!settings.ManualKeyTracking) return;
        if (slot < 1 || slot > 10) return;

        if (settings.RobloxOnly && !_robloxDetector.IsRobloxForeground)
        {
            return;
        }

        int oldSlot;
        lock (_lock)
        {
            oldSlot = _currentSlot;
            _currentSlot = slot;
        }

        char keyChar = slot == 10 ? '0' : (char)('0' + slot);

        var activity = new InputActivity
        {
            ActionName = $"Physical Key '{keyChar}'",
            CurrentSlot = oldSlot,
            TargetSlot = slot,
            SentKey = "None (Physical Key Observed)",
            Result = "Slot updated",
            IsSimulated = false
        };
        LastInput = activity;
        InputActivityUpdated?.Invoke(this, activity);

        if (settings.DebugMode)
        {
            _logger.LogDebug($"Physical MAIN KEY \"{keyChar}\"");
            _logger.LogDebug($"Current slot updated → {slot}");
        }
        else
        {
            _logger.LogInfo($"Physical key '{keyChar}' observed: Current slot updated → {slot}");
        }

        SlotChanged?.Invoke(this, new HotbarChangedEventArgs(slot, oldSlot, SlotChangeSource.ManualKey));
    }

    public void NextSlot()
    {
        var settings = _settingsService.CurrentSettings;
        int oldSlot;
        int nextSlot;

        lock (_lock)
        {
            oldSlot = _currentSlot;
            nextSlot = CalculateNextSlot(oldSlot, 1, settings.WrapAround);
            if (nextSlot == oldSlot) return;
            _currentSlot = nextSlot;
        }

        _logger.LogInfo($"Slot changed: {oldSlot} → {nextSlot}");
        _inputSimulator.SendSlotKey(nextSlot);
        SlotChanged?.Invoke(this, new HotbarChangedEventArgs(nextSlot, oldSlot, SlotChangeSource.UserInterface));
    }

    public void PreviousSlot()
    {
        var settings = _settingsService.CurrentSettings;
        int oldSlot;
        int nextSlot;

        lock (_lock)
        {
            oldSlot = _currentSlot;
            nextSlot = CalculateNextSlot(oldSlot, -1, settings.WrapAround);
            if (nextSlot == oldSlot) return;
            _currentSlot = nextSlot;
        }

        _logger.LogInfo($"Slot changed: {oldSlot} → {nextSlot}");
        _inputSimulator.SendSlotKey(nextSlot);
        SlotChanged?.Invoke(this, new HotbarChangedEventArgs(nextSlot, oldSlot, SlotChangeSource.UserInterface));
    }

    public void SetSlot(int slot, SlotChangeSource source)
    {
        if (slot < 1 || slot > 10) return;

        int oldSlot;
        lock (_lock)
        {
            oldSlot = _currentSlot;
            _currentSlot = slot;
        }

        _logger.LogInfo($"Tracked slot synchronized to: {slot} (Source: {source})");
        SlotChanged?.Invoke(this, new HotbarChangedEventArgs(slot, oldSlot, source));
    }

    public static int CalculateNextSlot(int current, int step, bool wrapAround)
    {
        int candidate = current + step;

        if (wrapAround)
        {
            while (candidate > 10) candidate -= 10;
            while (candidate < 1) candidate += 10;
            return candidate;
        }
        else
        {
            if (candidate > 10) return 10;
            if (candidate < 1) return 1;
            return candidate;
        }
    }
}
