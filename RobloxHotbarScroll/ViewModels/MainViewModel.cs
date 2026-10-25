using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using RobloxHotbarScroll.Models;
using RobloxHotbarScroll.Services;

namespace RobloxHotbarScroll.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ILoggerService _logger;
    private readonly ISettingsService _settingsService;
    private readonly IRobloxDetector _robloxDetector;
    private readonly IHotbarController _hotbarController;
    private readonly ITrayService _trayService;
    private readonly IInputManager _inputManager;
    private readonly IToastNotificationService _toastService;
    private readonly IUpdateService _updateService;

    private int _currentSlot = 1;
    private string _robloxStatusText = "Roblox: Not running";
    private bool _isRobloxRunning;
    private bool _isRobloxFocused;
    private double _indicatorTargetOffset;
    private bool _isInputHookActive = true;
    private string? _hookErrorMessage;
    private UpdateInfo? _availableUpdate;
    private bool _isUpdateNotificationDismissed;

    private InputActivity _lastInput = new()
    {
        ActionName = "Waiting for input...",
        CurrentSlot = 1,
        TargetSlot = 1,
        SentKey = "None",
        Result = "Ready"
    };

    public ObservableCollection<HotbarSlotViewModel> Slots { get; } = new();
    public ObservableCollection<LogEntry> RecentLogs => _logger.Entries;
    public ObservableCollection<ToastNotification> ActiveToasts => _toastService.ActiveToasts;

    public int CurrentSlot
    {
        get => _currentSlot;
        set
        {
            if (SetProperty(ref _currentSlot, value))
            {
                OnPropertyChanged(nameof(CurrentSlotText));
                OnPropertyChanged(nameof(CurrentKeyText));
                OnPropertyChanged(nameof(PrevSlotHint));
                OnPropertyChanged(nameof(NextSlotHint));
                UpdateSlotSelection();
            }
        }
    }

    public string CurrentSlotText => $"SLOT {_currentSlot}";
    public string CurrentKeyText => $"Key [{(_currentSlot == 10 ? "0" : _currentSlot.ToString())}]";

    public string PrevSlotHint
    {
        get
        {
            int prev = HotbarController.CalculateNextSlot(_currentSlot, -1, WrapAround);
            char key = prev == 10 ? '0' : (char)('0' + prev);
            return $"Scroll ↑ Previous (send \"{key}\")";
        }
    }

    public string NextSlotHint
    {
        get
        {
            int next = HotbarController.CalculateNextSlot(_currentSlot, 1, WrapAround);
            char key = next == 10 ? '0' : (char)('0' + next);
            return $"Scroll ↓ Next (send \"{key}\")";
        }
    }

    public bool IsRemapperEnabled
    {
        get => _settingsService.CurrentSettings.Enabled;
        set
        {
            if (_settingsService.CurrentSettings.Enabled != value)
            {
                _hotbarController.SetEnabled(value);
                OnPropertyChanged();
                UpdateStatus();
            }
        }
    }

    public bool IsUtilityActive => _hotbarController.IsActive && IsInputHookActive;

    public string StatusTitle => IsUtilityActive ? "REMAPPER ACTIVE" : (IsRemapperEnabled ? "WAITING FOR ROBLOX" : "REMAPPER PAUSED");

    public string StatusBadgeText
    {
        get
        {
            if (!IsInputHookActive) return "⚠ Input Offline";
            if (!IsRemapperEnabled) return "● PAUSED";
            return IsUtilityActive ? "● ACTIVE" : "○ READY";
        }
    }

    public string StatusBadgeColor
    {
        get
        {
            if (!IsInputHookActive) return "#EF4444";
            if (!IsRemapperEnabled) return "#F59E0B";
            return IsUtilityActive ? "#10B981" : "#8B5CF6";
        }
    }

    public string ToggleButtonText => IsRemapperEnabled ? "Pause Remapper" : "Enable Remapper";
    public string ToggleButtonHint => $"({ToggleHotkey})";

    public string ToggleHotkey
    {
        get => _settingsService.CurrentSettings.ToggleHotkey;
        set
        {
            if (_settingsService.CurrentSettings.ToggleHotkey != value)
            {
                _settingsService.CurrentSettings.ToggleHotkey = value;
                _settingsService.SaveSettings();
                OnPropertyChanged();
                OnPropertyChanged(nameof(ToggleButtonHint));
            }
        }
    }

    public bool IsInputHookActive
    {
        get => _isInputHookActive;
        set
        {
            if (SetProperty(ref _isInputHookActive, value))
            {
                OnPropertyChanged(nameof(HasHookError));
                OnPropertyChanged(nameof(InputStatusText));
                OnPropertyChanged(nameof(InputStatusColor));
                UpdateStatus();
            }
        }
    }

    public bool HasHookError => !IsInputHookActive;

    public string? HookErrorMessage
    {
        get => _hookErrorMessage;
        set => SetProperty(ref _hookErrorMessage, value);
    }

    public string InputStatusText => IsInputHookActive ? "Input: Active" : "Input: ⚠ Unavailable";
    public string InputStatusColor => IsInputHookActive ? "#10B981" : "#EF4444";

    public bool IsRobloxRunning
    {
        get => _isRobloxRunning;
        set => SetProperty(ref _isRobloxRunning, value);
    }

    public bool IsRobloxFocused
    {
        get => _isRobloxFocused;
        set => SetProperty(ref _isRobloxFocused, value);
    }

    public string RobloxStatusText
    {
        get => _robloxStatusText;
        set => SetProperty(ref _robloxStatusText, value);
    }

    public double IndicatorTargetOffset
    {
        get => _indicatorTargetOffset;
        set => SetProperty(ref _indicatorTargetOffset, value);
    }

    public InputActivity LastInput
    {
        get => _lastInput;
        set => SetProperty(ref _lastInput, value);
    }

    // ================= Settings Two-Way Bindings =================
    public bool RobloxOnly
    {
        get => _settingsService.CurrentSettings.RobloxOnly;
        set
        {
            if (_settingsService.CurrentSettings.RobloxOnly != value)
            {
                _settingsService.CurrentSettings.RobloxOnly = value;
                _settingsService.SaveSettings();
                OnPropertyChanged();
                UpdateStatus();
            }
        }
    }

    public bool RobloxOnlyMode
    {
        get => RobloxOnly;
        set => RobloxOnly = value;
    }

    public bool WrapAround
    {
        get => _settingsService.CurrentSettings.WrapAround;
        set
        {
            if (_settingsService.CurrentSettings.WrapAround != value)
            {
                _settingsService.CurrentSettings.WrapAround = value;
                _settingsService.SaveSettings();
                OnPropertyChanged();
                OnPropertyChanged(nameof(PrevSlotHint));
                OnPropertyChanged(nameof(NextSlotHint));
            }
        }
    }

    public bool ReverseScroll
    {
        get => _settingsService.CurrentSettings.ReverseScroll;
        set
        {
            if (_settingsService.CurrentSettings.ReverseScroll != value)
            {
                _settingsService.CurrentSettings.ReverseScroll = value;
                _settingsService.SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool DebugMode
    {
        get => _settingsService.CurrentSettings.DebugMode;
        set
        {
            if (_settingsService.CurrentSettings.DebugMode != value)
            {
                _settingsService.CurrentSettings.DebugMode = value;
                _settingsService.SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool StartWithWindows
    {
        get => _settingsService.CurrentSettings.StartWithWindows;
        set
        {
            if (_settingsService.CurrentSettings.StartWithWindows != value)
            {
                _settingsService.CurrentSettings.StartWithWindows = value;
                _settingsService.SetStartWithWindows(value);
                _settingsService.SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool StartMinimized
    {
        get => _settingsService.CurrentSettings.StartMinimized;
        set
        {
            if (_settingsService.CurrentSettings.StartMinimized != value)
            {
                _settingsService.CurrentSettings.StartMinimized = value;
                _settingsService.SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool MinimizeToTray
    {
        get => _settingsService.CurrentSettings.MinimizeToTray;
        set
        {
            if (_settingsService.CurrentSettings.MinimizeToTray != value)
            {
                _settingsService.CurrentSettings.MinimizeToTray = value;
                _settingsService.SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool ShowNotifications
    {
        get => _settingsService.CurrentSettings.ShowNotifications;
        set
        {
            if (_settingsService.CurrentSettings.ShowNotifications != value)
            {
                _settingsService.CurrentSettings.ShowNotifications = value;
                _settingsService.SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool AnimationsEnabled
    {
        get => _settingsService.CurrentSettings.AnimationsEnabled;
        set
        {
            if (_settingsService.CurrentSettings.AnimationsEnabled != value)
            {
                _settingsService.CurrentSettings.AnimationsEnabled = value;
                _settingsService.SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public double AnimationSpeed
    {
        get => _settingsService.CurrentSettings.AnimationSpeed;
        set
        {
            if (Math.Abs(_settingsService.CurrentSettings.AnimationSpeed - value) > 0.01)
            {
                _settingsService.CurrentSettings.AnimationSpeed = value;
                _settingsService.SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool ReducedMotion
    {
        get => _settingsService.CurrentSettings.ReducedMotion;
        set
        {
            if (_settingsService.CurrentSettings.ReducedMotion != value)
            {
                _settingsService.CurrentSettings.ReducedMotion = value;
                _settingsService.SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public UpdateInfo? AvailableUpdate
    {
        get => _availableUpdate;
        set
        {
            if (SetProperty(ref _availableUpdate, value))
            {
                OnPropertyChanged(nameof(HasUpdateNotification));
                OnPropertyChanged(nameof(AvailableUpdateTitle));
                OnPropertyChanged(nameof(AvailableUpdateSubtext));
            }
        }
    }

    public bool HasUpdateNotification => AvailableUpdate != null && !_isUpdateNotificationDismissed;
    public string AvailableUpdateTitle => AvailableUpdate != null ? $"HotkeyScroll {AvailableUpdate.VersionString} is available." : string.Empty;
    public string AvailableUpdateSubtext => AvailableUpdate != null ? "A newer version of HotkeyScroll is available on GitHub." : string.Empty;

    public SettingsViewModel SettingsVM { get; }
    public AboutViewModel AboutVM { get; }

    // Commands
    public ICommand ToggleActiveCommand { get; }
    public ICommand NextSlotCommand { get; }
    public ICommand PreviousSlotCommand { get; }
    public ICommand RetryHookCommand { get; }
    public ICommand OpenLogsFolderCommand { get; }
    public ICommand OpenSettingsCommand { get; set; } = null!;
    public ICommand OpenAboutCommand { get; set; } = null!;
    public ICommand ClearLogsCommand { get; }
    public ICommand ViewUpdateCommand { get; }
    public ICommand UpdateNowCommand => ViewUpdateCommand;
    public ICommand DismissUpdateNotificationCommand { get; }
    public ICommand CheckForUpdatesCommand { get; }

    public MainViewModel(
        ILoggerService logger,
        ISettingsService settingsService,
        IRobloxDetector robloxDetector,
        IHotbarController hotbarController,
        ITrayService trayService,
        IInputManager inputManager,
        IToastNotificationService toastService,
        IUpdateService updateService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _robloxDetector = robloxDetector;
        _hotbarController = hotbarController;
        _trayService = trayService;
        _inputManager = inputManager;
        _toastService = toastService;
        _updateService = updateService;

        SettingsVM = new SettingsViewModel(settingsService, logger, updateService);
        AboutVM = new AboutViewModel(logger, robloxDetector, inputManager, settingsService, updateService);

        // Populate slots 1 to 10
        for (int i = 1; i <= 10; i++)
        {
            Slots.Add(new HotbarSlotViewModel(i, OnSlotClicked));
        }

        _currentSlot = _hotbarController.CurrentSlot;
        UpdateSlotSelection();

        ToggleActiveCommand = new RelayCommand(() => _hotbarController.ToggleEnabled());
        NextSlotCommand = new RelayCommand(() => _hotbarController.NextSlot());
        PreviousSlotCommand = new RelayCommand(() => _hotbarController.PreviousSlot());
        RetryHookCommand = new RelayCommand(RetryInputHook);
        ClearLogsCommand = new RelayCommand(() => _logger.Clear());
        OpenLogsFolderCommand = new RelayCommand(OpenLogsFolder);

        ViewUpdateCommand = new RelayCommand(() =>
        {
            _updateService.OpenReleasePage(AvailableUpdate?.HtmlUrl);
        });

        DismissUpdateNotificationCommand = new RelayCommand(() =>
        {
            _isUpdateNotificationDismissed = true;
            OnPropertyChanged(nameof(HasUpdateNotification));
        });

        CheckForUpdatesCommand = new RelayCommand(async () =>
        {
            await _updateService.CheckForUpdatesAsync(true);
        });

        // Subscribe to events
        _hotbarController.SlotChanged += OnControllerSlotChanged;
        _hotbarController.EnabledChanged += OnControllerEnabledChanged;
        _hotbarController.InputActivityUpdated += OnInputActivityUpdated;
        _robloxDetector.StateChanged += OnRobloxStateChanged;
        _settingsService.SettingsChanged += OnSettingsChanged;
        _inputManager.HookStatusChanged += OnHookStatusChanged;
        _updateService.UpdateAvailable += OnUpdateAvailable;

        _isInputHookActive = _inputManager.IsHookActive;
        _hookErrorMessage = _inputManager.LastError;
        UpdateStatus();

        // Background update check on startup (non-blocking)
        if (_settingsService.CurrentSettings.CheckForUpdatesAutomatically)
        {
            Task.Run(async () =>
            {
                await Task.Delay(2500);
                await _updateService.CheckForUpdatesAsync(false);
            });
        }
    }

    private void OnUpdateAvailable(object? sender, UpdateInfo updateInfo)
    {
        RunOnUI(() =>
        {
            _isUpdateNotificationDismissed = false;
            AvailableUpdate = updateInfo;
        });
    }

    private void OnSlotClicked(int slot)
    {
        _hotbarController.SetSlot(slot, SlotChangeSource.UserInterface);
    }

    private void OnControllerSlotChanged(object? sender, HotbarChangedEventArgs e)
    {
        RunOnUI(() =>
        {
            CurrentSlot = e.NewSlot;
            UpdateStatus();
        });
    }

    private void OnControllerEnabledChanged(object? sender, bool enabled)
    {
        RunOnUI(() =>
        {
            OnPropertyChanged(nameof(IsRemapperEnabled));
            OnPropertyChanged(nameof(ToggleButtonText));
            UpdateStatus();
        });
    }

    private void OnInputActivityUpdated(object? sender, InputActivity activity)
    {
        RunOnUI(() =>
        {
            LastInput = activity;
        });
    }

    private void OnHookStatusChanged(object? sender, bool active)
    {
        RunOnUI(() =>
        {
            IsInputHookActive = active;
            HookErrorMessage = _inputManager.LastError;
            if (!active)
            {
                _toastService.Show("Input Hook Failed", "Windows input monitoring could not be started.", ToastType.Error);
            }
            UpdateStatus();
        });
    }

    private void OnRobloxStateChanged(object? sender, RobloxProcessState state)
    {
        RunOnUI(() =>
        {
            IsRobloxRunning = state.IsRunning;
            IsRobloxFocused = state.IsFocused;

            if (state.IsFocused)
            {
                RobloxStatusText = $"Roblox: Focused ({state.ProcessName})";
                _toastService.Show("Roblox Focused", "Hotbar remapping is active", ToastType.Info);
            }
            else if (state.IsRunning)
            {
                RobloxStatusText = "Roblox: Running in background";
            }
            else
            {
                RobloxStatusText = "Roblox: Not running";
            }

            UpdateStatus();
        });
    }

    private void OnSettingsChanged(object? sender, AppSettings e)
    {
        RunOnUI(() =>
        {
            OnPropertyChanged(nameof(IsRemapperEnabled));
            OnPropertyChanged(nameof(ToggleHotkey));
            OnPropertyChanged(nameof(ToggleButtonHint));
            OnPropertyChanged(nameof(RobloxOnly));
            OnPropertyChanged(nameof(WrapAround));
            OnPropertyChanged(nameof(ReverseScroll));
            OnPropertyChanged(nameof(DebugMode));
            OnPropertyChanged(nameof(StartWithWindows));
            OnPropertyChanged(nameof(StartMinimized));
            OnPropertyChanged(nameof(MinimizeToTray));
            OnPropertyChanged(nameof(ShowNotifications));
            OnPropertyChanged(nameof(AnimationsEnabled));
            OnPropertyChanged(nameof(AnimationSpeed));
            UpdateStatus();
        });
    }

    public void RetryInputHook()
    {
        _logger.LogInfo("Retrying input hook installation on user request...");
        _inputManager.Start();
        IsInputHookActive = _inputManager.IsHookActive;
        HookErrorMessage = _inputManager.LastError;
        if (IsInputHookActive)
        {
            _toastService.Show("Input Hook Recovered", "Input monitoring started successfully.", ToastType.Success);
        }
        UpdateStatus();
    }

    public void DismissToast(ToastNotification toast)
    {
        _toastService.Dismiss(toast);
    }

    public void UpdateStatus()
    {
        bool isEnabled = _settingsService.CurrentSettings.Enabled;
        bool isActive = _hotbarController.IsActive && IsInputHookActive;

        OnPropertyChanged(nameof(IsUtilityActive));
        OnPropertyChanged(nameof(StatusTitle));
        OnPropertyChanged(nameof(StatusBadgeText));
        OnPropertyChanged(nameof(StatusBadgeColor));
        OnPropertyChanged(nameof(ToggleButtonText));

        _trayService.UpdateState(isEnabled, isActive, _currentSlot, RobloxStatusText);
    }

    private void UpdateSlotSelection()
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            Slots[i].IsSelected = (Slots[i].SlotNumber == _currentSlot);
        }

        IndicatorTargetOffset = (_currentSlot - 1) * 50.0;
        OnPropertyChanged(nameof(IndicatorTargetOffset));
    }

    private void OpenLogsFolder()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logDir = Path.Combine(appData, "RobloxHotbarScroll", "Logs");
            Directory.CreateDirectory(logDir);
            Process.Start(new ProcessStartInfo
            {
                FileName = logDir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to open logs folder.", ex);
        }
    }

    private static void RunOnUI(Action action)
    {
        var app = System.Windows.Application.Current;
        if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.BeginInvoke(action);
        }
        else
        {
            action();
        }
    }
}
