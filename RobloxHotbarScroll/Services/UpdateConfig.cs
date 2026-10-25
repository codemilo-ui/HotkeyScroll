using System;

namespace RobloxHotbarScroll.Services;

public static class UpdateConfig
{
    public const string GitHubOwner = "codemilo-ui";
    public const string GitHubRepo = "HotkeyScroll";
    public const string AppName = "HotkeyScroll";
    public const string CompanyName = "CodeMilo";

    private static readonly Lazy<Version> _currentVersion = new(() =>
    {
        var asm = typeof(UpdateConfig).Assembly;
        var infoVerAttr = (System.Reflection.AssemblyInformationalVersionAttribute?)Attribute.GetCustomAttribute(
            asm, typeof(System.Reflection.AssemblyInformationalVersionAttribute));
        if (infoVerAttr != null && TryParseVersion(infoVerAttr.InformationalVersion, out var infoVer))
        {
            return infoVer;
        }

        var asmVer = asm.GetName().Version;
        if (asmVer != null)
        {
            return new Version(asmVer.Major, asmVer.Minor, Math.Max(0, asmVer.Build));
        }

        return new Version(1, 2, 0);
    });

    public static Version CurrentVersion => _currentVersion.Value;
    public static string CurrentVersionString => $"{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";

    public static string LatestReleaseApiUrl =>
        $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

    public static string ReleasesWebUrl =>
        $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases";

    public static string RepositoryUrl =>
        $"https://github.com/{GitHubOwner}/{GitHubRepo}";

    public static string IssuesUrl =>
        $"https://github.com/{GitHubOwner}/{GitHubRepo}/issues";

    public static bool TryParseVersion(string versionString, out Version parsedVersion)
    {
        parsedVersion = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(versionString)) return false;

        string cleaned = versionString.Trim().TrimStart('v', 'V');
        int hyphenIdx = cleaned.IndexOf('-');
        if (hyphenIdx > 0)
        {
            cleaned = cleaned.Substring(0, hyphenIdx);
        }

        string[] parts = cleaned.Split('.');
        if (parts.Length >= 2 && int.TryParse(parts[0], out int major) && int.TryParse(parts[1], out int minor))
        {
            if (parts.Length == 2)
            {
                parsedVersion = new Version(major, minor);
                return true;
            }
            if (parts.Length == 3 && int.TryParse(parts[2], out int build))
            {
                parsedVersion = new Version(major, minor, build);
                return true;
            }
            if (parts.Length >= 4 && int.TryParse(parts[2], out int b) && int.TryParse(parts[3], out int rev))
            {
                parsedVersion = new Version(major, minor, b, rev);
                return true;
            }
        }

        return Version.TryParse(cleaned, out parsedVersion!);
    }

    public static bool IsNewerVersion(Version latest, Version current)
    {
        int lMaj = latest.Major;
        int cMaj = current.Major;
        if (lMaj != cMaj) return lMaj > cMaj;

        int lMin = latest.Minor;
        int cMin = current.Minor;
        if (lMin != cMin) return lMin > cMin;

        int lBuild = Math.Max(0, latest.Build);
        int cBuild = Math.Max(0, current.Build);
        if (lBuild != cBuild) return lBuild > cBuild;

        int lRev = Math.Max(0, latest.Revision);
        int cRev = Math.Max(0, current.Revision);
        return lRev > cRev;
    }

    public static bool IsNewerThanCurrent(string? versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString)) return false;
        if (TryParseVersion(versionString, out var parsed))
        {
            return IsNewerVersion(parsed, CurrentVersion);
        }
        return false;
    }
}
