using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using RobloxHotbarScroll.Models;

namespace RobloxHotbarScroll.Services;

public class SettingsService : ISettingsService
{
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunRegistryValueName = "RobloxHotbarScroll";

    private readonly ILoggerService _logger;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppSettings CurrentSettings { get; private set; }

    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService(ILoggerService logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string configDir = Path.Combine(appData, "RobloxHotbarScroll");
        Directory.CreateDirectory(configDir);
        _settingsFilePath = Path.Combine(configDir, "settings.json");

        CurrentSettings = LoadSettings();
    }

    private AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            _logger.LogInfo("Configuration file not found. Creating default settings (startMinimized = false).");
            var defaults = new AppSettings
            {
                StartMinimized = false,
                MinimizeToTray = true
            };
            SaveInternal(defaults);
            return defaults;
        }

        try
        {
            string json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
            if (settings != null)
            {
                // Validate bounds
                if (settings.StartingSlot < 1 || settings.StartingSlot > 10)
                    settings.StartingSlot = 1;
                if (settings.ScrollSensitivity < 1)
                    settings.ScrollSensitivity = 1;
                if (settings.AnimationSpeed < 0.2 || settings.AnimationSpeed > 3.0)
                    settings.AnimationSpeed = 1.0;

                // Ensure startMinimized is false by default so manual launch opens the main dashboard
                if (settings.StartMinimized)
                {
                    settings.StartMinimized = false;
                }

                // Ensure remapper starts enabled and ready for input on launch
                if (!settings.Enabled)
                {
                    settings.Enabled = true;
                }
                SaveInternal(settings);

                _logger.LogInfo("Configuration loaded successfully.");
                return settings;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Corrupted configuration file detected. Restoring safe defaults.", ex);
            try
            {
                string backupPath = _settingsFilePath + ".bak";
                File.Copy(_settingsFilePath, backupPath, overwrite: true);
                _logger.LogInfo($"Corrupted settings backed up to {backupPath}");
            }
            catch { }
        }

        var fallback = new AppSettings
        {
            StartMinimized = false,
            MinimizeToTray = true
        };
        SaveInternal(fallback);
        return fallback;
    }

    public void SaveSettings()
    {
        SaveInternal(CurrentSettings);
        SetStartWithWindows(CurrentSettings.StartWithWindows);
        SettingsChanged?.Invoke(this, CurrentSettings);
        _logger.LogInfo("Settings saved.");
    }

    private void SaveInternal(AppSettings settings)
    {
        try
        {
            string tempFile = _settingsFilePath + ".tmp";
            string json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(tempFile, json);
            File.Move(tempFile, _settingsFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to write settings to disk.", ex);
        }
    }

    public void ResetToDefaults()
    {
        CurrentSettings = new AppSettings
        {
            StartMinimized = false,
            MinimizeToTray = true
        };
        SaveSettings();
        _logger.LogInfo("Settings reset to defaults.");
    }

    public void SetStartWithWindows(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: true);
            if (key == null) return;

            if (enable)
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    string command = $"\"{exePath}\"";
                    if (CurrentSettings.StartMinimized)
                    {
                        command += " --minimized";
                    }
                    key.SetValue(RunRegistryValueName, command);
                    _logger.LogInfo($"Configured Windows startup: {command}");
                }
            }
            else
            {
                if (key.GetValue(RunRegistryValueName) != null)
                {
                    key.DeleteValue(RunRegistryValueName, throwOnMissingValue: false);
                    _logger.LogInfo("Removed from Windows startup.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to modify Windows startup registry key.", ex);
        }
    }

    public bool CheckStartWithWindows()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: false);
            return key?.GetValue(RunRegistryValueName) != null;
        }
        catch
        {
            return false;
        }
    }
}
