using System;
using RobloxHotbarScroll.Models;
using RobloxHotbarScroll.Services;
using RobloxHotbarScroll.ViewModels;
using Xunit;

namespace RobloxHotbarScroll.Tests;

public class UpdateServiceAndVersioningTests
{
    [Theory]
    [InlineData("1.2.1", 1, 2, 1, -1)]
    [InlineData("v1.2.1", 1, 2, 1, -1)]
    [InlineData("v1.2.10", 1, 2, 10, -1)]
    [InlineData("2.0.0", 2, 0, 0, -1)]
    [InlineData("1.0.0.4", 1, 0, 0, 4)]
    [InlineData("v1.2.3-beta", 1, 2, 3, -1)]
    public void TryParseVersion_ParsesValidStringsCorrectly(string input, int major, int minor, int build, int revision)
    {
        bool success = UpdateConfig.TryParseVersion(input, out var ver);

        Assert.True(success);
        Assert.NotNull(ver);
        Assert.Equal(major, ver!.Major);
        Assert.Equal(minor, ver.Minor);
        Assert.Equal(build, ver.Build);
        Assert.Equal(revision, ver.Revision);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("version_one")]
    public void TryParseVersion_ReturnsFalseForInvalidStrings(string? input)
    {
        bool success = UpdateConfig.TryParseVersion(input ?? string.Empty, out _);

        Assert.False(success);
    }

    [Fact]
    public void SemanticVersioning_CorrectlyIdentifies_1_2_10_GreaterThan_1_2_9()
    {
        Assert.True(UpdateConfig.TryParseVersion("1.2.10", out var v10));
        Assert.True(UpdateConfig.TryParseVersion("1.2.9", out var v9));

        Assert.True(v10 > v9, "Semantic version 1.2.10 must be strictly greater than 1.2.9 (not alphabetical comparison!)");
    }

    [Fact]
    public void SemanticVersioning_CorrectlyIdentifies_1_3_0_GreaterThan_1_2_10()
    {
        Assert.True(UpdateConfig.TryParseVersion("1.3.0", out var v130));
        Assert.True(UpdateConfig.TryParseVersion("1.2.10", out var v1210));

        Assert.True(v130 > v1210);
    }

    [Fact]
    public void SemanticVersioning_IdentifiesEquality_1_2_0_Equals_1_2_0()
    {
        Assert.True(UpdateConfig.TryParseVersion("1.2.0", out var v1));
        Assert.True(UpdateConfig.TryParseVersion("v1.2.0", out var v2));

        Assert.False(UpdateConfig.IsNewerVersion(v1, v2));
        Assert.False(UpdateConfig.IsNewerVersion(v2, v1));
    }

    [Theory]
    [InlineData("1.2.1", true)]
    [InlineData("v1.2.1", true)]
    [InlineData("1.2.10", true)]
    [InlineData("1.3.0", true)]
    [InlineData("2.0.0", true)]
    [InlineData("1.2.0", false)]
    [InlineData("v1.2.0", false)]
    [InlineData("1.1.9", false)]
    [InlineData("1.0.0", false)]
    [InlineData("invalid", false)]
    public void IsNewerThanCurrent_ComparesAgainstCurrentVersionCorrectly(string remoteVersion, bool expectedResult)
    {
        bool isNewer = UpdateConfig.IsNewerThanCurrent(remoteVersion);

        Assert.Equal(expectedResult, isNewer);
    }

    [Fact]
    public void CurrentVersion_IsLoadedCorrectlyFromAssembly()
    {
        Assert.NotNull(UpdateConfig.CurrentVersion);
        Assert.True(UpdateConfig.CurrentVersion.Major >= 1);
        Assert.Equal($"{UpdateConfig.CurrentVersion.Major}.{UpdateConfig.CurrentVersion.Minor}.{UpdateConfig.CurrentVersion.Build}", UpdateConfig.CurrentVersionString);
    }

    [Fact]
    public void MainViewModel_UpdateNotificationFlow_OpensReleasePage()
    {
        var logger = new TestLogger();
        var settingsService = new TestSettingsService();
        var detector = new TestRobloxDetector();
        var simulator = new TestInputSimulator();
        var toastService = new TestToastNotificationService();
        var trayService = new TestTrayService();
        var inputManager = new TestInputManager();
        var updateService = new TestUpdateService();

        var controller = new HotbarController(logger, settingsService, detector, simulator, toastService);
        var vm = new MainViewModel(logger, settingsService, detector, controller, trayService, inputManager, toastService, updateService);

        // Initially no update notification
        Assert.Null(vm.AvailableUpdate);
        Assert.False(vm.HasUpdateNotification);

        // Simulate update detected
        var updateInfo = new UpdateInfo
        {
            LatestVersion = new Version(1, 3, 0),
            VersionString = "v1.3.0",
            ReleaseTitle = "HotkeyScroll v1.3.0",
            ReleaseNotes = "New features and enhancements.",
            HtmlUrl = "https://github.com/codemilo-ui/HotkeyScroll/releases/tag/v1.3.0",
            PublishedAt = DateTime.UtcNow
        };

        vm.AvailableUpdate = updateInfo;

        Assert.True(vm.HasUpdateNotification);
        Assert.Contains("v1.3.0", vm.AvailableUpdateTitle);

        // Test ViewUpdate command opens the release page
        vm.ViewUpdateCommand.Execute(null);
        Assert.True(updateService.OpenReleasePageCalled);
        Assert.Equal(updateInfo.HtmlUrl, updateService.OpenedReleasePageUrl);

        // Test Dismiss notification command
        vm.DismissUpdateNotificationCommand.Execute(null);
        Assert.False(vm.HasUpdateNotification);
    }

    [Fact]
    public void SettingsViewModel_UpdateUI_MatchesRequiredSpecification()
    {
        var logger = new TestLogger();
        var settingsService = new TestSettingsService();
        var updateService = new TestUpdateService();

        var vm = new SettingsViewModel(settingsService, logger, updateService);

        // Check current version
        Assert.Equal("v1.2.0", vm.AppVersionText);

        // Initial status
        Assert.Equal("You're up to date.", vm.UpdateStatus);
        Assert.False(vm.HasUpdate);

        // Simulate update available
        var update = new UpdateInfo
        {
            IsUpdateAvailable = true,
            VersionString = "v1.3.0",
            HtmlUrl = "https://github.com/codemilo-ui/HotkeyScroll/releases/tag/v1.3.0"
        };
        updateService.MockUpdate = update;
        updateService.StatusMessage = "A newer version of HotkeyScroll is available.";

        vm.CheckUpdatesCommand.Execute(null);

        Assert.True(vm.HasUpdate);
        Assert.Equal("v1.3.0", vm.NewVersionText);
        Assert.Equal("Download v1.3.0", vm.DownloadButtonText);
        Assert.Equal("A newer version of HotkeyScroll is available.", vm.UpdateStatus);

        // Test DownloadUpdateCommand opens release page
        vm.DownloadUpdateCommand.Execute(null);
        Assert.True(updateService.OpenReleasePageCalled);
        Assert.Equal(update.HtmlUrl, updateService.OpenedReleasePageUrl);

        // Test Automatic updates toggle (default ON)
        Assert.True(vm.CheckForUpdatesAutomatically);
        vm.CheckForUpdatesAutomatically = false;
        Assert.False(settingsService.CurrentSettings.CheckForUpdatesAutomatically);
        Assert.False(vm.CheckForUpdatesAutomatically);
    }

    [Fact]
    public void SettingsViewModel_OfflineOrError_ShowsFriendlyMessageWithoutCrash()
    {
        var logger = new TestLogger();
        var settingsService = new TestSettingsService();
        var updateService = new TestUpdateService
        {
            MockUpdate = null,
            StatusMessage = "Couldn't check for updates.\n\nHotkeyScroll is still working normally.",
            HasError = true
        };

        var vm = new SettingsViewModel(settingsService, logger, updateService);

        vm.CheckUpdatesCommand.Execute(null);

        Assert.False(vm.HasUpdate);
        Assert.True(vm.HasUpdateError);
        Assert.Contains("Couldn't check for updates", vm.UpdateStatus);
        Assert.Contains("working normally", vm.UpdateStatus);
    }
}
