using System;

namespace RobloxHotbarScroll.Services;

public interface IInputManager : IDisposable
{
    bool IsRunning { get; }
    bool IsHookActive { get; }
    string? LastError { get; }
    event EventHandler<bool>? HookStatusChanged;

    void Start();
    void Stop();
}
