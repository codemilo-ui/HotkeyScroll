using System;

namespace RobloxHotbarScroll.Models;

public class UpdateInfo
{
    public bool IsUpdateAvailable { get; init; }
    public Version CurrentVersion { get; init; } = Services.UpdateConfig.CurrentVersion;
    public Version? LatestVersion { get; init; }
    public string VersionString { get; init; } = string.Empty;
    public string ReleaseTitle { get; init; } = string.Empty;
    public string ReleaseNotes { get; init; } = string.Empty;
    public string HtmlUrl { get; init; } = string.Empty;
    public DateTime? PublishedAt { get; init; }
}
