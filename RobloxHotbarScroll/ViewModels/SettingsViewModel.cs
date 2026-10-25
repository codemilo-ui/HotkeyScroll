using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using RobloxHotbarScroll.Models;
using RobloxHotbarScroll.Services;

namespace RobloxHotbarScroll.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILoggerService _logger;
    private readonly IUpdateService _updateService;

    public AppSettings Settings => _settingsService.CurrentSettings;

    // Remapping Core
    public bool Enabled
    {
        get => Settings.Enabled;
        set
        {
            if (Settings.Enabled != value)
            {
                Settings.Enabled = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public string ToggleHotkey
    {
        get => Settings.ToggleHotkey;
        set
        {
            if (Settings.ToggleHotkey != value && !string.IsNullOrWhiteSpace(value))
            {
                Settings.ToggleHotkey = value.Trim();
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public bool RobloxOnly
    {
        get => Settings.RobloxOnly;
        set
        {
            if (Settings.RobloxOnly != value)
            {
                Settings.RobloxOnly = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public bool WrapAround
    {
        get => Settings.WrapAround;
        set
        {
            if (Settings.WrapAround != value)
            {
                Settings.WrapAround = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public bool ReverseScroll
    {
        get => Settings.ReverseScroll;
        set
        {
            if (Settings.ReverseScroll != value)
            {
                Settings.ReverseScroll = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public bool BlockMouseWheelInRoblox
    {
        get => Settings.BlockMouseWheelInRoblox;
        set
        {
            if (Settings.BlockMouseWheelInRoblox != value)
            {
                Settings.BlockMouseWheelInRoblox = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public bool ManualKeyTracking
    {
        get => Settings.ManualKeyTracking;
        set
        {
            if (Settings.ManualKeyTracking != value)
            {
                Settings.ManualKeyTracking = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public int ScrollSensitivity
    {
        get => Settings.ScrollSensitivity;
        set
        {
            if (Settings.ScrollSensitivity != value && value >= 1 && value <= 5)
            {
                Settings.ScrollSensitivity = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    // Startup & Tray
    public bool StartWithWindows
    {
        get => Settings.StartWithWindows;
        set
        {
            if (Settings.StartWithWindows != value)
            {
                Settings.StartWithWindows = value;
                _settingsService.SetStartWithWindows(value);
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public bool StartMinimized
    {
        get => Settings.StartMinimized;
        set
        {
            if (Settings.StartMinimized != value)
            {
                Settings.StartMinimized = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public bool MinimizeToTray
    {
        get => Settings.MinimizeToTray;
        set
        {
            if (Settings.MinimizeToTray != value)
            {
                Settings.MinimizeToTray = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public bool ShowNotifications
    {
        get => Settings.ShowNotifications;
        set
        {
            if (Settings.ShowNotifications != value)
            {
                Settings.ShowNotifications = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    // Diagnostics & Appearance
    public bool DebugMode
    {
        get => Settings.DebugMode;
        set
        {
            if (Settings.DebugMode != value)
            {
                Settings.DebugMode = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public string Theme
    {
        get => Settings.Theme;
        set
        {
            if (Settings.Theme != value)
            {
                Settings.Theme = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public bool AnimationsEnabled
    {
        get => Settings.AnimationsEnabled;
        set
        {
            if (Settings.AnimationsEnabled != value)
            {
                Settings.AnimationsEnabled = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public double AnimationSpeed
    {
        get => Settings.AnimationSpeed;
        set
        {
            if (Math.Abs(Settings.AnimationSpeed - value) > 0.01)
            {
                Settings.AnimationSpeed = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public ObservableCollection<string> AvailableHotkeys { get; } = new()
    {
        "F8",
        "Ctrl + F8",
        "Alt + F8",
        "Ctrl + Shift + F8",
        "F7",
        "F9",
        "F10"
    };

    public ObservableCollection<string> AvailableThemes { get; } = new()
    {
        "Dark Purple",
        "Dark Slate",
        "Midnight"
    };

    public bool ReducedMotion
    {
        get => Settings.ReducedMotion;
        set
        {
            if (Settings.ReducedMotion != value)
            {
                Settings.ReducedMotion = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public bool CheckForUpdatesAutomatically
    {
        get => Settings.CheckForUpdatesAutomatically;
        set
        {
            if (Settings.CheckForUpdatesAutomatically != value)
            {
                Settings.CheckForUpdatesAutomatically = value;
                OnPropertyChanged();
                AutoSave();
            }
        }
    }

    public string AppVersionText => $"v{UpdateConfig.CurrentVersionString}";

    public bool HasUpdate => _updateService.LastCheckResult?.IsUpdateAvailable == true;
    public string NewVersionText => _updateService.LastCheckResult?.VersionString ?? string.Empty;
    public string DownloadButtonText => !string.IsNullOrEmpty(NewVersionText) ? $"Download {NewVersionText}" : "Download Update";
    public string UpdateStatus => _updateService.StatusMessage;
    public bool HasUpdateError => _updateService.HasError;

    public string LastCheckedText
    {
        get
        {
            if (!Settings.LastUpdateCheckTime.HasValue) return "Never";
            var diff = DateTime.Now - Settings.LastUpdateCheckTime.Value;
            if (diff.TotalMinutes < 2) return "Just now";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} minutes ago";
            if (diff.TotalDays < 1) return Settings.LastUpdateCheckTime.Value.ToString("h:mm tt");
            return Settings.LastUpdateCheckTime.Value.ToString("MMM d, yyyy");
        }
    }

    public bool IsCheckingUpdates => _updateService.IsChecking;

    public ICommand ResetDefaultsCommand { get; }
    public ICommand OpenLogsFolderCommand { get; }
    public ICommand CheckUpdatesCommand { get; }
    public ICommand DownloadUpdateCommand { get; }

    public SettingsViewModel(ISettingsService settingsService, ILoggerService logger, IUpdateService updateService)
    {
        _settingsService = settingsService;
        _logger = logger;
        _updateService = updateService;

        _updateService.CheckingStatusChanged += (s, checking) =>
        {
            OnPropertyChanged(nameof(IsCheckingUpdates));
            OnPropertyChanged(nameof(LastCheckedText));
            OnPropertyChanged(nameof(UpdateStatus));
            OnPropertyChanged(nameof(HasUpdate));
            OnPropertyChanged(nameof(NewVersionText));
            OnPropertyChanged(nameof(DownloadButtonText));
            OnPropertyChanged(nameof(HasUpdateError));
        };

        _updateService.UpdateAvailable += (s, info) =>
        {
            OnPropertyChanged(nameof(UpdateStatus));
            OnPropertyChanged(nameof(HasUpdate));
            OnPropertyChanged(nameof(NewVersionText));
            OnPropertyChanged(nameof(DownloadButtonText));
            OnPropertyChanged(nameof(HasUpdateError));
        };

        ResetDefaultsCommand = new RelayCommand(ResetToDefaults);
        OpenLogsFolderCommand = new RelayCommand(OpenLogsFolder);
        DownloadUpdateCommand = new RelayCommand(() =>
        {
            _updateService.OpenReleasePage(_updateService.LastCheckResult?.HtmlUrl);
        });
        CheckUpdatesCommand = new RelayCommand(async () =>
        {
            await _updateService.CheckForUpdatesAsync(true);
            OnPropertyChanged(nameof(LastCheckedText));
            OnPropertyChanged(nameof(UpdateStatus));
            OnPropertyChanged(nameof(HasUpdate));
            OnPropertyChanged(nameof(NewVersionText));
            OnPropertyChanged(nameof(DownloadButtonText));
            OnPropertyChanged(nameof(HasUpdateError));
        });
    }

    private void AutoSave()
    {
        _settingsService.SaveSettings();
    }

    private void ResetToDefaults()
    {
        _settingsService.ResetToDefaults();
        RefreshAllProperties();
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

    public void RefreshAllProperties()
    {
        OnPropertyChanged(nameof(Enabled));
        OnPropertyChanged(nameof(ToggleHotkey));
        OnPropertyChanged(nameof(RobloxOnly));
        OnPropertyChanged(nameof(WrapAround));
        OnPropertyChanged(nameof(ReverseScroll));
        OnPropertyChanged(nameof(BlockMouseWheelInRoblox));
        OnPropertyChanged(nameof(ManualKeyTracking));
        OnPropertyChanged(nameof(ScrollSensitivity));
        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(StartMinimized));
        OnPropertyChanged(nameof(MinimizeToTray));
        OnPropertyChanged(nameof(ShowNotifications));
        OnPropertyChanged(nameof(DebugMode));
        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(AnimationsEnabled));
        OnPropertyChanged(nameof(AnimationSpeed));
    }
}
