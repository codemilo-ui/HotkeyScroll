using System;
using System.Threading;
using System.Threading.Tasks;
using RobloxHotbarScroll.Models;

namespace RobloxHotbarScroll.Services;

public interface IUpdateService
{
    UpdateInfo? LastCheckResult { get; }
    bool IsChecking { get; }
    DateTime? LastCheckTime { get; }
    string StatusMessage { get; }
    bool HasError { get; }

    event EventHandler<UpdateInfo>? UpdateAvailable;
    event EventHandler<bool>? CheckingStatusChanged;

    Task<UpdateInfo?> CheckForUpdatesAsync(bool isManual, CancellationToken cancellationToken = default);
    void OpenReleasePage(string? url = null);
}
