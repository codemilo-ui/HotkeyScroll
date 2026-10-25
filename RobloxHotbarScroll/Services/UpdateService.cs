using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RobloxHotbarScroll.Models;

namespace RobloxHotbarScroll.Services;

public class UpdateService : IUpdateService
{
    private readonly ILoggerService _logger;
    private readonly ISettingsService _settingsService;
    private readonly IToastNotificationService _toastService;
    private readonly HttpClient _httpClient;

    public UpdateInfo? LastCheckResult { get; private set; }
    public bool IsChecking { get; private set; }
    public DateTime? LastCheckTime => _settingsService.CurrentSettings.LastUpdateCheckTime;
    public string StatusMessage { get; private set; } = "You're up to date.";
    public bool HasError { get; private set; }

    public event EventHandler<UpdateInfo>? UpdateAvailable;
    public event EventHandler<bool>? CheckingStatusChanged;

    public UpdateService(ILoggerService logger, ISettingsService settingsService, IToastNotificationService toastService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _toastService = toastService;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"CodeMilo-{UpdateConfig.AppName}-App/{UpdateConfig.CurrentVersionString}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync(bool isManual, CancellationToken cancellationToken = default)
    {
        if (IsChecking) return LastCheckResult;

        IsChecking = true;
        CheckingStatusChanged?.Invoke(this, true);

        try
        {
            _logger.LogInfo($"Checking for updates via GitHub Releases ({UpdateConfig.LatestReleaseApiUrl})...");

            using var response = await _httpClient.GetAsync(UpdateConfig.LatestReleaseApiUrl, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInfo("No published GitHub releases found for repository yet.");
                StatusMessage = "You're up to date.";
                HasError = false;
                return RecordCheckResult(new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = UpdateConfig.CurrentVersion });
            }

            if ((int)response.StatusCode == 403)
            {
                _logger.LogWarning("GitHub API rate limit reached. Update check postponed.");
                StatusMessage = "Couldn't check for updates.\n\nHotkeyScroll is still working normally.";
                HasError = true;
                if (isManual)
                {
                    _toastService.Show("Updates", "Couldn't check for updates. HotkeyScroll is still working normally.", ToastType.Warning);
                }
                return LastCheckResult;
            }

            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            string releaseTitle = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? tagName : tagName;
            string releaseNotes = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
            string htmlUrl = root.TryGetProperty("html_url", out var htmlProp) ? htmlProp.GetString() ?? UpdateConfig.ReleasesWebUrl : UpdateConfig.ReleasesWebUrl;

            DateTime? publishedAt = null;
            if (root.TryGetProperty("published_at", out var pubProp) && pubProp.TryGetDateTime(out var dt))
            {
                publishedAt = dt;
            }

            if (!UpdateConfig.TryParseVersion(tagName, out var latestVersion))
            {
                _logger.LogWarning($"Unable to parse semantic version from release tag: '{tagName}'");
                StatusMessage = "You're up to date.";
                HasError = false;
                return RecordCheckResult(new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = UpdateConfig.CurrentVersion });
            }

            bool isNewer = UpdateConfig.IsNewerVersion(latestVersion, UpdateConfig.CurrentVersion);

            var updateInfo = new UpdateInfo
            {
                IsUpdateAvailable = isNewer,
                CurrentVersion = UpdateConfig.CurrentVersion,
                LatestVersion = latestVersion,
                VersionString = tagName,
                ReleaseTitle = releaseTitle,
                ReleaseNotes = releaseNotes,
                HtmlUrl = htmlUrl,
                PublishedAt = publishedAt
            };

            RecordCheckResult(updateInfo);

            if (isNewer)
            {
                StatusMessage = "A newer version of HotkeyScroll is available.";
                HasError = false;
                _logger.LogInfo($"New update available on GitHub: {tagName} (Current: v{UpdateConfig.CurrentVersionString})");
                UpdateAvailable?.Invoke(this, updateInfo);
                _toastService.Show("✦ Update Available", $"HotkeyScroll {tagName} is available.", ToastType.Update);
            }
            else
            {
                StatusMessage = "You're up to date.";
                HasError = false;
                _logger.LogInfo($"HotkeyScroll is up to date (Version {UpdateConfig.CurrentVersionString}).");
                if (isManual)
                {
                    _toastService.Show("Up to Date", "You're up to date.", ToastType.Success);
                }
            }

            return updateInfo;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("Update check cancelled.");
            return LastCheckResult;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning($"Update check network issue: {ex.Message}");
            StatusMessage = "Couldn't check for updates.\n\nHotkeyScroll is still working normally.";
            HasError = true;
            if (isManual)
            {
                _toastService.Show("Updates", "Couldn't check for updates. HotkeyScroll is still working normally.", ToastType.Warning);
            }
            return LastCheckResult;
        }
        catch (Exception ex)
        {
            _logger.LogException("Unexpected error during update check", ex);
            StatusMessage = "Couldn't check for updates.\n\nHotkeyScroll is still working normally.";
            HasError = true;
            if (isManual)
            {
                _toastService.Show("Updates", "Couldn't check for updates. HotkeyScroll is still working normally.", ToastType.Warning);
            }
            return LastCheckResult;
        }
        finally
        {
            IsChecking = false;
            CheckingStatusChanged?.Invoke(this, false);
        }
    }

    public void OpenReleasePage(string? url = null)
    {
        string target = !string.IsNullOrWhiteSpace(url) ? url : UpdateConfig.ReleasesWebUrl;
        try
        {
            _logger.LogInfo($"Opening GitHub release page in browser: {target}");
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to open release page URL: {target}", ex);
        }
    }

    private UpdateInfo RecordCheckResult(UpdateInfo info)
    {
        LastCheckResult = info;
        _settingsService.CurrentSettings.LastUpdateCheckTime = DateTime.Now;
        _settingsService.SaveSettings();
        return info;
    }
}
