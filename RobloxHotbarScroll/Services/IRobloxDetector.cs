using System;
using RobloxHotbarScroll.Models;

namespace RobloxHotbarScroll.Services;

public interface IRobloxDetector : IDisposable
{
    bool IsRobloxRunning { get; }
    bool IsRobloxForeground { get; }
    RobloxProcessState State { get; }
    event EventHandler<RobloxProcessState>? StateChanged;

    void Start();
    void Stop();
    void RefreshNow();
}
