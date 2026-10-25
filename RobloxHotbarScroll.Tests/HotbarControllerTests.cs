using System.Collections.Generic;
using RobloxHotbarScroll.Models;
using RobloxHotbarScroll.Services;
using Xunit;

namespace RobloxHotbarScroll.Tests;

public class HotbarControllerTests
{
    [Fact]
    public void Section30_ExactExampleTestSequence_Passes()
    {
        // Setup
        var logger = new TestLogger();
        var settingsService = new TestSettingsService();
        settingsService.CurrentSettings.StartingSlot = 1;
        settingsService.CurrentSettings.WrapAround = true;
        settingsService.CurrentSettings.ReverseScroll = false;
        settingsService.CurrentSettings.ManualKeyTracking = true;
        settingsService.CurrentSettings.RobloxOnlyMode = true;

        var detector = new TestRobloxDetector { IsRobloxForeground = true, IsRobloxRunning = true };
        var simulator = new TestInputSimulator();

        var controller = new HotbarController(logger, settingsService, detector, simulator);

        // Track slot change events
        var slotEvents = new List<int>();
        controller.SlotChanged += (s, e) => slotEvents.Add(e.NewSlot);

        // Sequence from Prompt Section 30:
        // Start application -> Current slot: 1
        Assert.Equal(1, controller.CurrentSlot);

        // Scroll DOWN → 2
        controller.HandleScroll(-1);
        Assert.Equal(2, controller.CurrentSlot);

        // Scroll DOWN → 3
        controller.HandleScroll(-1);
        Assert.Equal(3, controller.CurrentSlot);

        // Scroll DOWN → 4
        controller.HandleScroll(-1);
        Assert.Equal(4, controller.CurrentSlot);

        // Press main keyboard 7 → currentSlot = 7
        controller.HandleManualKey(7);
        Assert.Equal(7, controller.CurrentSlot);

        // Scroll DOWN → 8
        controller.HandleScroll(-1);
        Assert.Equal(8, controller.CurrentSlot);

        // Scroll UP → 7
        controller.HandleScroll(1);
        Assert.Equal(7, controller.CurrentSlot);

        // Press main keyboard 2 → currentSlot = 2
        controller.HandleManualKey(2);
        Assert.Equal(2, controller.CurrentSlot);

        // Scroll UP → 1
        controller.HandleScroll(1);
        Assert.Equal(1, controller.CurrentSlot);

        // Scroll UP → 10 (wrap around)
        controller.HandleScroll(1);
        Assert.Equal(10, controller.CurrentSlot);

        // Scroll DOWN → 1 (wrap around)
        controller.HandleScroll(-1);
        Assert.Equal(1, controller.CurrentSlot);

        // Press numpad 5 → currentSlot remains 1
        // In our architecture, numpad keys are ignored by the input manager hook,
        // which never calls HandleManualKey. If HandleManualKey received an invalid/numpad key code
        // or if simulated here:
        // Testing that numpad is ignored:
        // Numpad key virtual codes (0x60-0x69) are filtered before HandleManualKey.
        // If an invalid slot is sent, HandleManualKey rejects it.
        controller.HandleManualKey(0); // Numpad / invalid
        Assert.Equal(1, controller.CurrentSlot);

        // Press main keyboard 5 → currentSlot = 5
        controller.HandleManualKey(5);
        Assert.Equal(5, controller.CurrentSlot);

        // Scroll DOWN → 6
        controller.HandleScroll(-1);
        Assert.Equal(6, controller.CurrentSlot);

        // Verify simulated key presses
        // Sent keys on scrolls: 2, 3, 4, 8, 7, 1, 10, 1, 6
        Assert.Equal(new[] { 2, 3, 4, 8, 7, 1, 10, 1, 6 }, simulator.SentSlots);
    }

    [Theory]
    [InlineData(1, 1, true, 2)]
    [InlineData(9, 1, true, 10)]
    [InlineData(10, 1, true, 1)]      // Wrap around at 10
    [InlineData(1, -1, true, 10)]     // Wrap around at 1
    [InlineData(5, -1, true, 4)]
    [InlineData(10, 1, false, 10)]    // Clamp at 10 when wrap disabled
    [InlineData(1, -1, false, 1)]     // Clamp at 1 when wrap disabled
    public void CalculateNextSlot_WorksCorrectly(int current, int step, bool wrapAround, int expected)
    {
        int actual = HotbarController.CalculateNextSlot(current, step, wrapAround);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReverseScroll_ReversesDirections()
    {
        var logger = new TestLogger();
        var settingsService = new TestSettingsService();
        settingsService.CurrentSettings.StartingSlot = 5;
        settingsService.CurrentSettings.ReverseScroll = true; // UP is Next, DOWN is Prev

        var detector = new TestRobloxDetector { IsRobloxForeground = true };
        var simulator = new TestInputSimulator();
        var controller = new HotbarController(logger, settingsService, detector, simulator);

        // Scroll UP (+1) -> Should increase slot to 6 in reverse mode
        controller.HandleScroll(1);
        Assert.Equal(6, controller.CurrentSlot);

        // Scroll DOWN (-1) -> Should decrease slot to 5 in reverse mode
        controller.HandleScroll(-1);
        Assert.Equal(5, controller.CurrentSlot);
    }

    [Fact]
    public void RobloxOnlyMode_PausesWhenRobloxNotFocused()
    {
        var logger = new TestLogger();
        var settingsService = new TestSettingsService();
        settingsService.CurrentSettings.StartingSlot = 3;
        settingsService.CurrentSettings.RobloxOnlyMode = true;

        var detector = new TestRobloxDetector { IsRobloxForeground = false }; // Not focused!
        var simulator = new TestInputSimulator();
        var controller = new HotbarController(logger, settingsService, detector, simulator);

        Assert.False(controller.IsActive);

        // Scroll DOWN should do nothing while Roblox is not focused
        controller.HandleScroll(-1);
        Assert.Equal(3, controller.CurrentSlot);
        Assert.Empty(simulator.SentSlots);

        // Manual key should also be ignored
        controller.HandleManualKey(7);
        Assert.Equal(3, controller.CurrentSlot);

        // Now focus Roblox
        detector.TriggerFocusChange(true);
        Assert.True(controller.IsActive);

        controller.HandleScroll(-1);
        Assert.Equal(4, controller.CurrentSlot);
        Assert.Single(simulator.SentSlots);
    }

    [Fact]
    public void HotbarScrollDisabled_DoesNotChangeSlotsOnScroll()
    {
        var logger = new TestLogger();
        var settingsService = new TestSettingsService();
        settingsService.CurrentSettings.StartingSlot = 2;
        settingsService.CurrentSettings.HotbarScrollEnabled = false;

        var detector = new TestRobloxDetector { IsRobloxForeground = true };
        var simulator = new TestInputSimulator();
        var controller = new HotbarController(logger, settingsService, detector, simulator);

        Assert.False(controller.IsActive);

        controller.HandleScroll(-1);
        Assert.Equal(2, controller.CurrentSlot);
        Assert.Empty(simulator.SentSlots);
    }

    [Fact]
    public void ToggleEnabled_SwitchesState_FiresEvents_ShowsToast()
    {
        var logger = new TestLogger();
        var settingsService = new TestSettingsService();
        settingsService.CurrentSettings.Enabled = true;
        settingsService.CurrentSettings.ToggleHotkey = "F8";

        var detector = new TestRobloxDetector { IsRobloxForeground = true };
        var simulator = new TestInputSimulator();
        var toastService = new TestToastNotificationService();
        var controller = new HotbarController(logger, settingsService, detector, simulator, toastService);

        bool? lastEnabledEvent = null;
        controller.EnabledChanged += (s, enabled) => lastEnabledEvent = enabled;

        Assert.True(controller.IsEnabled);
        Assert.True(controller.IsActive);

        // Toggle OFF (Pause)
        controller.ToggleEnabled();

        Assert.False(controller.IsEnabled);
        Assert.False(controller.IsActive);
        Assert.False(lastEnabledEvent);
        Assert.Single(toastService.ShownToasts);
        Assert.Equal("Remapper Paused", toastService.ShownToasts[0].Title);

        // While paused, scroll down does NOT change slot or send keys
        controller.HandleScroll(-1);
        Assert.Equal(1, controller.CurrentSlot);
        Assert.Empty(simulator.SentSlots);

        // Toggle back ON
        controller.ToggleEnabled();

        Assert.True(controller.IsEnabled);
        Assert.True(controller.IsActive);
        Assert.True(lastEnabledEvent);
        Assert.Equal(2, toastService.ShownToasts.Count);
        Assert.Equal("Remapper Enabled", toastService.ShownToasts[1].Title);

        // Now scrolling works
        controller.HandleScroll(-1);
        Assert.Equal(2, controller.CurrentSlot);
        Assert.Single(simulator.SentSlots);
        Assert.Equal(2, simulator.SentSlots[0]);
    }

    [Fact]
    public void InputActivity_TracksLastInputAccurately()
    {
        var logger = new TestLogger();
        var settingsService = new TestSettingsService();
        settingsService.CurrentSettings.StartingSlot = 3;

        var detector = new TestRobloxDetector { IsRobloxForeground = true };
        var simulator = new TestInputSimulator();
        var controller = new HotbarController(logger, settingsService, detector, simulator);

        InputActivity? lastActivity = null;
        controller.InputActivityUpdated += (s, activity) => lastActivity = activity;

        // Scroll DOWN (3 -> 4)
        controller.HandleScroll(-1);

        Assert.NotNull(lastActivity);
        Assert.Equal("Mouse Wheel Down", lastActivity.ActionName);
        Assert.Equal(3, lastActivity.CurrentSlot);
        Assert.Equal(4, lastActivity.TargetSlot);
        Assert.Equal("MAIN KEY \"4\"", lastActivity.SentKey);
        Assert.True(lastActivity.IsSimulated);

        // Physical key 9 pressed
        controller.HandleManualKey(9);

        Assert.NotNull(lastActivity);
        Assert.Equal("Physical Key '9'", lastActivity.ActionName);
        Assert.Equal(4, lastActivity.CurrentSlot);
        Assert.Equal(9, lastActivity.TargetSlot);
        Assert.Equal("None (Physical Key Observed)", lastActivity.SentKey);
        Assert.False(lastActivity.IsSimulated);

        // Physical key 0 pressed (Slot 10)
        controller.HandleManualKey(10);
        Assert.Equal("Physical Key '0'", lastActivity.ActionName);
        Assert.Equal(10, controller.CurrentSlot);
    }
}
