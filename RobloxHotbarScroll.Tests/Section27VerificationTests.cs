using System;
using System.Linq;
using RobloxHotbarScroll.Models;
using RobloxHotbarScroll.Services;
using RobloxHotbarScroll.ViewModels;
using Xunit;

namespace RobloxHotbarScroll.Tests;

public class Section27VerificationTests
{
    private class TestContext
    {
        public TestLogger Logger { get; set; } = null!;
        public TestSettingsService SettingsService { get; set; } = null!;
        public TestRobloxDetector Detector { get; set; } = null!;
        public TestInputSimulator Simulator { get; set; } = null!;
        public TestToastNotificationService ToastService { get; set; } = null!;
        public TestTrayService TrayService { get; set; } = null!;
        public TestInputManager InputManager { get; set; } = null!;
        public HotbarController Controller { get; set; } = null!;
        public MainViewModel Vm { get; set; } = null!;
        public TestUpdateService UpdateService { get; set; } = null!;

        public void Deconstruct(
            out TestLogger logger,
            out TestSettingsService settingsService,
            out TestRobloxDetector detector,
            out TestInputSimulator simulator,
            out TestToastNotificationService toastService,
            out TestTrayService trayService,
            out TestInputManager inputManager,
            out HotbarController controller,
            out MainViewModel vm)
        {
            logger = Logger;
            settingsService = SettingsService;
            detector = Detector;
            simulator = Simulator;
            toastService = ToastService;
            trayService = TrayService;
            inputManager = InputManager;
            controller = Controller;
            vm = Vm;
        }
    }

    private TestContext CreateTestContext()
    {
        var logger = new TestLogger();
        var settingsService = new TestSettingsService();
        var detector = new TestRobloxDetector { IsRobloxForeground = true, IsRobloxRunning = true };
        var simulator = new TestInputSimulator();
        var toastService = new TestToastNotificationService();
        var trayService = new TestTrayService();
        var inputManager = new TestInputManager();
        var updateService = new TestUpdateService();

        var controller = new HotbarController(logger, settingsService, detector, simulator, toastService);
        var vm = new MainViewModel(logger, settingsService, detector, controller, trayService, inputManager, toastService, updateService);

        return new TestContext
        {
            Logger = logger,
            SettingsService = settingsService,
            Detector = detector,
            Simulator = simulator,
            ToastService = toastService,
            TrayService = trayService,
            InputManager = inputManager,
            Controller = controller,
            Vm = vm,
            UpdateService = updateService
        };
    }

    [Fact]
    public void Test1_ApplicationStartup_InitializesProperly()
    {
        var (_, _, _, _, _, _, _, controller, vm) = CreateTestContext();

        Assert.Equal(1, controller.CurrentSlot);
        Assert.Equal(1, vm.CurrentSlot);
        Assert.True(controller.IsEnabled);
        Assert.True(controller.IsActive);
        Assert.Equal(10, vm.Slots.Count);
        Assert.True(vm.Slots[0].IsSelected);
        Assert.False(vm.HasHookError);
    }

    [Fact]
    public void Test2_ToggleRemapper_ActiveToPaused_UpdatesStateAndUI()
    {
        var (_, _, _, _, toastService, _, _, controller, vm) = CreateTestContext();

        Assert.True(vm.IsRemapperEnabled);
        Assert.Equal("Pause Remapper", vm.ToggleButtonText);
        Assert.Contains("ACTIVE", vm.StatusTitle);

        // Execute toggle command (simulating pressing F8 or clicking Toggle button)
        vm.ToggleActiveCommand.Execute(null);

        Assert.False(controller.IsEnabled);
        Assert.False(controller.IsActive);
        Assert.False(vm.IsRemapperEnabled);
        Assert.Equal("Enable Remapper", vm.ToggleButtonText);
        Assert.Contains("PAUSED", vm.StatusTitle);
        Assert.Single(toastService.ShownToasts);
        Assert.Equal("Remapper Paused", toastService.ShownToasts[0].Title);

        // Toggle back
        vm.ToggleActiveCommand.Execute(null);

        Assert.True(controller.IsEnabled);
        Assert.True(controller.IsActive);
        Assert.True(vm.IsRemapperEnabled);
        Assert.Equal("Pause Remapper", vm.ToggleButtonText);
        Assert.Contains("ACTIVE", vm.StatusTitle);
        Assert.Equal(2, toastService.ShownToasts.Count);
        Assert.Equal("Remapper Enabled", toastService.ShownToasts[1].Title);
    }

    [Fact]
    public void Test3_WheelDownInRoblox_SendsNextKey_IncrementsSlot()
    {
        var (_, _, _, simulator, _, _, _, controller, vm) = CreateTestContext();

        // Start at slot 2
        controller.SetSlot(2, SlotChangeSource.UserInterface);
        Assert.Equal(2, controller.CurrentSlot);
        Assert.Equal(2, vm.CurrentSlot);

        // Wheel DOWN (-1) -> increments to 3
        controller.HandleScroll(-1);

        Assert.Equal(3, controller.CurrentSlot);
        Assert.Equal(3, vm.CurrentSlot);
        Assert.True(vm.Slots[2].IsSelected);
        Assert.False(vm.Slots[1].IsSelected);

        Assert.Single(simulator.SentSlots);
        Assert.Equal(3, simulator.SentSlots[0]); // Sent key '3'

        Assert.Equal("Mouse Wheel Down", vm.LastInput.ActionName);
        Assert.Equal("MAIN KEY \"3\"", vm.LastInput.SentKey);
        Assert.True(vm.LastInput.IsSimulated);
    }

    [Fact]
    public void Test4_WheelUpInRoblox_SendsPreviousKey_DecrementsSlot()
    {
        var (_, _, _, simulator, _, _, _, controller, vm) = CreateTestContext();

        controller.SetSlot(4, SlotChangeSource.UserInterface);

        // Wheel UP (+1) -> decrements to 3
        controller.HandleScroll(1);

        Assert.Equal(3, controller.CurrentSlot);
        Assert.Equal(3, vm.CurrentSlot);
        Assert.Single(simulator.SentSlots);
        Assert.Equal(3, simulator.SentSlots[0]); // Sent key '3'

        Assert.Equal("Mouse Wheel Up", vm.LastInput.ActionName);
        Assert.Equal("MAIN KEY \"3\"", vm.LastInput.SentKey);
    }

    [Fact]
    public void Test5_PhysicalKey7_ObservedOnly_UpdatesSlotWithoutDuplicate_SubsequentScrollSends8()
    {
        var (_, _, _, simulator, _, _, _, controller, vm) = CreateTestContext();

        // Starting at slot 4
        controller.SetSlot(4, SlotChangeSource.UserInterface);
        int initialSends = simulator.SentSlots.Count;

        // User physically presses '7'
        controller.HandleManualKey(7);

        // currentSlot must immediately update to 7
        Assert.Equal(7, controller.CurrentSlot);
        Assert.Equal(7, vm.CurrentSlot);
        Assert.True(vm.Slots[6].IsSelected);

        // Physical key was observed only: NO keys sent by simulator
        Assert.Equal(initialSends, simulator.SentSlots.Count);
        Assert.Equal("Physical Key '7'", vm.LastInput.ActionName);
        Assert.False(vm.LastInput.IsSimulated);

        // Subsequent Wheel DOWN -> must switch to 8 and send key '8'
        controller.HandleScroll(-1);

        Assert.Equal(8, controller.CurrentSlot);
        Assert.Equal(8, vm.CurrentSlot);
        Assert.Equal(initialSends + 1, simulator.SentSlots.Count);
        Assert.Equal(8, simulator.SentSlots.Last());
    }

    [Fact]
    public void Test6_WrapAround_At10And1()
    {
        var (_, settingsService, _, simulator, _, _, _, controller, _) = CreateTestContext();
        settingsService.CurrentSettings.WrapAround = true;

        // Slot 10 -> Wheel DOWN wraps to 1
        controller.SetSlot(10, SlotChangeSource.UserInterface);
        controller.HandleScroll(-1);
        Assert.Equal(1, controller.CurrentSlot);
        Assert.Equal(1, simulator.SentSlots.Last());

        // Slot 1 -> Wheel UP wraps to 10
        controller.HandleScroll(1);
        Assert.Equal(10, controller.CurrentSlot);
        Assert.Equal(10, simulator.SentSlots.Last());

        // Now test with Wrap disabled:
        settingsService.CurrentSettings.WrapAround = false;

        // At 10, wheel down should clamp at 10 and not send new key
        int countBefore = simulator.SentSlots.Count;
        controller.HandleScroll(-1);
        Assert.Equal(10, controller.CurrentSlot);
        Assert.Equal(countBefore, simulator.SentSlots.Count);

        // At 1, wheel up should clamp at 1 and not send new key
        controller.SetSlot(1, SlotChangeSource.UserInterface);
        countBefore = simulator.SentSlots.Count;
        controller.HandleScroll(1);
        Assert.Equal(1, controller.CurrentSlot);
        Assert.Equal(countBefore, simulator.SentSlots.Count);
    }

    [Fact]
    public void Test7_OtherAppFocused_WhenRobloxOnlyEnabled_NoRemappingOccurs()
    {
        var (_, settingsService, detector, simulator, _, _, _, controller, vm) = CreateTestContext();
        settingsService.CurrentSettings.RobloxOnly = true;

        // Roblox loses focus
        detector.TriggerFocusChange(false);

        Assert.False(controller.IsActive);
        Assert.False(vm.IsUtilityActive);
        Assert.Contains("WAITING", vm.StatusTitle);

        // Wheel DOWN while Roblox is not focused
        controller.HandleScroll(-1);

        // Slot should NOT change, and no keys should be sent
        Assert.Equal(1, controller.CurrentSlot);
        Assert.Empty(simulator.SentSlots);

        // Refocus Roblox -> Remapper becomes active again
        detector.TriggerFocusChange(true);
        Assert.True(controller.IsActive);
        Assert.True(vm.IsUtilityActive);

        controller.HandleScroll(-1);
        Assert.Equal(2, controller.CurrentSlot);
        Assert.Single(simulator.SentSlots);
    }

    [Fact]
    public void Test7b_NotepadTest_WhenRobloxOnlyIsFalse_RemapsInAnyApp()
    {
        var (_, settingsService, detector, simulator, _, _, _, controller, vm) = CreateTestContext();
        // By default, RobloxOnly is false so it works in Notepad!
        Assert.False(settingsService.CurrentSettings.RobloxOnly);

        // Roblox is NOT running or focused
        detector.TriggerFocusChange(false);
        detector.IsRobloxRunning = false;

        // Remapper is still ACTIVE because RobloxOnly is false
        Assert.True(controller.IsActive);
        Assert.True(vm.IsUtilityActive);
        Assert.Equal("REMAPPER ACTIVE", vm.StatusTitle);

        // User is in Notepad, starting at slot 1, scrolls DOWN 4 times
        controller.HandleScroll(-1); // 1 -> 2
        Assert.Equal(2, controller.CurrentSlot);

        controller.HandleScroll(-1); // 2 -> 3
        Assert.Equal(3, controller.CurrentSlot);

        controller.HandleScroll(-1); // 3 -> 4
        Assert.Equal(4, controller.CurrentSlot);

        controller.HandleScroll(-1); // 4 -> 5
        Assert.Equal(5, controller.CurrentSlot);

        // Notepad received simulated keypresses: 2, 3, 4, 5
        Assert.Equal(new[] { 2, 3, 4, 5 }, simulator.SentSlots);

        // User physically types '7'
        controller.HandleManualKey(7);
        Assert.Equal(7, controller.CurrentSlot);
        // Physical key was not duplicated
        Assert.Equal(new[] { 2, 3, 4, 5 }, simulator.SentSlots);

        // User scrolls DOWN -> types '8'
        controller.HandleScroll(-1); // 7 -> 8
        Assert.Equal(8, controller.CurrentSlot);
        Assert.Equal(new[] { 2, 3, 4, 5, 8 }, simulator.SentSlots);
    }

    [Fact]
    public void Test8_PausedMode_NoRemappingOccurs()
    {
        var (_, settingsService, _, simulator, _, _, _, controller, vm) = CreateTestContext();

        // Pause remapper
        controller.SetEnabled(false);
        Assert.False(controller.IsEnabled);
        Assert.False(controller.IsActive);

        // Wheel DOWN
        controller.HandleScroll(-1);
        Assert.Equal(1, controller.CurrentSlot);
        Assert.Empty(simulator.SentSlots);

        // Wheel UP
        controller.HandleScroll(1);
        Assert.Equal(1, controller.CurrentSlot);
        Assert.Empty(simulator.SentSlots);
    }

    [Fact]
    public void HookErrorRecovery_BannerAndRetryCommand()
    {
        var (_, _, _, _, _, _, inputManager, _, vm) = CreateTestContext();

        Assert.False(vm.HasHookError);
        Assert.True(vm.IsInputHookActive);

        // Simulate hook failure
        inputManager.TriggerHookFailure("Simulated hook error");

        Assert.True(vm.HasHookError);
        Assert.False(vm.IsInputHookActive);
        Assert.Equal("Simulated hook error", vm.HookErrorMessage);

        // Execute RetryHookCommand
        vm.RetryHookCommand.Execute(null);

        Assert.False(vm.HasHookError);
        Assert.True(vm.IsInputHookActive);
    }
}
