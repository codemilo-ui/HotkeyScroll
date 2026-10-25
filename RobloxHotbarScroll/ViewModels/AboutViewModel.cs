using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using RobloxHotbarScroll.Services;

namespace RobloxHotbarScroll.ViewModels;

public class AboutViewModel : ViewModelBase
{
    private readonly ILoggerService _logger;
    private readonly IRobloxDetector _robloxDetector;
    private readonly IInputManager _inputManager;
    private readonly ISettingsService _settingsService;
    private readonly IUpdateService _updateService;

    public string AppName => UpdateConfig.AppName;
    public string Subtitle => $"by {UpdateConfig.CompanyName}";
    public string Version => $"v{UpdateConfig.CurrentVersionString}";
    public string Description => "A lightweight Windows utility for keyboard-based hotbar navigation.";
    public string DevelopedBy => $"Developed by {UpdateConfig.CompanyName}";
    public string Copyright => $"© {DateTime.Now.Year} {UpdateConfig.CompanyName}";

    public string RuntimeInfo => $".NET {Environment.Version} ({RuntimeInformation.ProcessArchitecture})";
    public string OsInfo => $"{RuntimeInformation.OSDescription}";
    public string InputHookStatus => _inputManager.IsHookActive ? "Active" : "Offline / Unavailable";
    public string RobloxStateInfo => _robloxDetector.State.StatusSummary;
    public string CurrentHotkey => _settingsService.CurrentSettings.ToggleHotkey;

    public ICommand OpenGitHubCommand { get; }
    public ICommand ReportIssueCommand { get; }
    public ICommand CheckUpdatesCommand { get; }
    public ICommand ViewLogsCommand { get; set; } = null!;

    public AboutViewModel(
        ILoggerService logger,
        IRobloxDetector robloxDetector,
        IInputManager inputManager,
        ISettingsService settingsService,
        IUpdateService updateService)
    {
        _logger = logger;
        _robloxDetector = robloxDetector;
        _inputManager = inputManager;
        _settingsService = settingsService;
        _updateService = updateService;

        OpenGitHubCommand = new RelayCommand(() => OpenUrl(UpdateConfig.RepositoryUrl));
        ReportIssueCommand = new RelayCommand(() => OpenUrl(UpdateConfig.IssuesUrl));
        CheckUpdatesCommand = new RelayCommand(async () => await _updateService.CheckForUpdatesAsync(true));
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            _logger.LogInfo($"Navigated to link: {url}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to open URL: {url}", ex);
        }
    }
}
