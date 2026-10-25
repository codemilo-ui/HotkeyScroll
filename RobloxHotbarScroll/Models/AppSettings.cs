using System;
using System.Text.Json.Serialization;

namespace RobloxHotbarScroll.Models;

public class AppSettings
{
    // Remapping Core
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("hotbarScrollEnabled")] // Backwards compatibility with v1.0
    public bool HotbarScrollEnabled
    {
        get => Enabled;
        set => Enabled = value;
    }

    [JsonPropertyName("toggleHotkey")]
    public string ToggleHotkey { get; set; } = "F8";

    [JsonPropertyName("robloxOnly")]
    public bool RobloxOnly { get; set; } = false;

    [JsonPropertyName("robloxOnlyMode")] // Backwards compatibility
    public bool RobloxOnlyMode
    {
        get => RobloxOnly;
        set => RobloxOnly = value;
    }

    [JsonPropertyName("wrapAround")]
    public bool WrapAround { get; set; } = true;

    [JsonPropertyName("reverseScroll")]
    public bool ReverseScroll { get; set; } = false;

    [JsonPropertyName("startingSlot")]
    public int StartingSlot { get; set; } = 1;

    [JsonPropertyName("scrollSensitivity")]
    public int ScrollSensitivity { get; set; } = 1;

    [JsonPropertyName("blockMouseWheelInRoblox")]
    public bool BlockMouseWheelInRoblox { get; set; } = true;

    [JsonPropertyName("manualKeyTracking")]
    public bool ManualKeyTracking { get; set; } = true;

    [JsonPropertyName("pauseWhenRobloxNotFocused")]
    public bool PauseWhenRobloxNotFocused { get; set; } = true;

    // Startup & Tray
    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; set; } = false;

    [JsonPropertyName("startMinimized")]
    public bool StartMinimized { get; set; } = false;

    [JsonPropertyName("minimizeToTray")]
    public bool MinimizeToTray { get; set; } = true;

    [JsonPropertyName("showNotifications")]
    public bool ShowNotifications { get; set; } = true;

    // Diagnostics & Appearance
    [JsonPropertyName("debugMode")]
    public bool DebugMode { get; set; } = false;

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Dark Purple";

    [JsonPropertyName("accentColor")]
    public string AccentColor { get; set; } = "Purple";

    [JsonPropertyName("animationsEnabled")]
    public bool AnimationsEnabled { get; set; } = true;

    [JsonPropertyName("reducedMotion")]
    public bool ReducedMotion { get; set; } = false;

    [JsonPropertyName("animationSpeed")]
    public double AnimationSpeed { get; set; } = 1.0;

    // Updates
    [JsonPropertyName("checkForUpdatesAutomatically")]
    public bool CheckForUpdatesAutomatically { get; set; } = true;

    [JsonPropertyName("lastUpdateCheckTime")]
    public DateTime? LastUpdateCheckTime { get; set; }

    public AppSettings Clone()
    {
        return (AppSettings)MemberwiseClone();
    }
}
