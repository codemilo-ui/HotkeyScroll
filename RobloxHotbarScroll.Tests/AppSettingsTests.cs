using System.Text.Json;
using RobloxHotbarScroll.Models;
using Xunit;

namespace RobloxHotbarScroll.Tests;

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_DefaultValues_AreCorrect()
    {
        var settings = new AppSettings();

        Assert.True(settings.HotbarScrollEnabled);
        Assert.True(settings.ManualKeyTracking);
        Assert.True(settings.WrapAround);
        Assert.False(settings.ReverseScroll);
        Assert.Equal(1, settings.StartingSlot);
        Assert.Equal(1, settings.ScrollSensitivity);
        Assert.True(settings.BlockMouseWheelInRoblox);
        Assert.False(settings.RobloxOnlyMode);
        Assert.True(settings.PauseWhenRobloxNotFocused);
        Assert.False(settings.StartWithWindows);
        Assert.False(settings.StartMinimized);
        Assert.True(settings.MinimizeToTray);
        Assert.True(settings.AnimationsEnabled);
        Assert.Equal(1.0, settings.AnimationSpeed);
    }

    [Fact]
    public void JsonSerialization_RoundTrip_PreservesProperties()
    {
        var original = new AppSettings
        {
            HotbarScrollEnabled = true,
            ManualKeyTracking = true,
            WrapAround = false,
            ReverseScroll = true,
            StartingSlot = 5,
            ScrollSensitivity = 2,
            RobloxOnlyMode = false,
            BlockMouseWheelInRoblox = true,
            StartWithWindows = true,
            StartMinimized = true,
            MinimizeToTray = true,
            AnimationsEnabled = false,
            AnimationSpeed = 1.5,
            Theme = "Midnight Blue",
            AccentColor = "Emerald"
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(original, options);

        var restored = JsonSerializer.Deserialize<AppSettings>(json, options);
        Assert.NotNull(restored);

        Assert.Equal(original.HotbarScrollEnabled, restored.HotbarScrollEnabled);
        Assert.Equal(original.WrapAround, restored.WrapAround);
        Assert.Equal(original.ReverseScroll, restored.ReverseScroll);
        Assert.Equal(original.StartingSlot, restored.StartingSlot);
        Assert.Equal(original.ScrollSensitivity, restored.ScrollSensitivity);
        Assert.Equal(original.RobloxOnlyMode, restored.RobloxOnlyMode);
        Assert.Equal(original.Theme, restored.Theme);
        Assert.Equal(original.AccentColor, restored.AccentColor);
    }
}
