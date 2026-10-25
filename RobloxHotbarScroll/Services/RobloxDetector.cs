using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using RobloxHotbarScroll.Models;
using RobloxHotbarScroll.Utilities;

namespace RobloxHotbarScroll.Services;

public class RobloxDetector : IRobloxDetector
{
    private readonly ILoggerService _logger;
    private readonly ISettingsService _settingsService;
    private readonly object _stateLock = new();

    private IntPtr _foregroundHook = IntPtr.Zero;
    private NativeMethods.WinEventDelegate? _foregroundDelegate;
    private System.Threading.Timer? _pollingTimer;
    private bool _isDisposed;

    private static readonly string[] KnownRobloxProcessNames =
    {
        "robloxplayerbeta",
        "robloxplayer",
        "robloxstudiobeta",
        "roblox"
    };

    public bool IsRobloxRunning { get; private set; }
    public bool IsRobloxForeground { get; private set; }
    public RobloxProcessState State { get; private set; } = new();

    public event EventHandler<RobloxProcessState>? StateChanged;

    public RobloxDetector(ILoggerService logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    public void Start()
    {
        lock (_stateLock)
        {
            if (_foregroundHook != IntPtr.Zero) return;

            try
            {
                _foregroundDelegate = OnForegroundEvent;
                _foregroundHook = NativeMethods.SetWinEventHook(
                    NativeMethods.EVENT_SYSTEM_FOREGROUND,
                    NativeMethods.EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero,
                    _foregroundDelegate,
                    0,
                    0,
                    NativeMethods.WINEVENT_OUTOFCONTEXT
                );

                if (_foregroundHook == IntPtr.Zero)
                {
                    _logger.LogWarning("SetWinEventHook returned 0. Foreground window detection using polling fallback.");
                }
                else
                {
                    _logger.LogDebug("Foreground window WinEvent hook installed successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to initialize WinEvent hook.", ex);
            }

            try
            {
                _pollingTimer = new System.Threading.Timer(_ => RefreshNow(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(1500));
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to start Roblox process polling timer.", ex);
            }
        }

        RefreshNow();
    }

    public void Stop()
    {
        lock (_stateLock)
        {
            try
            {
                _pollingTimer?.Dispose();
                _pollingTimer = null;
            }
            catch { }

            if (_foregroundHook != IntPtr.Zero)
            {
                try
                {
                    NativeMethods.UnhookWinEvent(_foregroundHook);
                }
                catch { }
                _foregroundHook = IntPtr.Zero;
                _foregroundDelegate = null;
                _logger.LogDebug("Foreground window hook uninstalled.");
            }
        }
    }

    private void OnForegroundEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        EvaluateState(hwnd);
    }

    public void RefreshNow()
    {
        IntPtr fgWnd = IntPtr.Zero;
        try
        {
            fgWnd = NativeMethods.GetForegroundWindow();
        }
        catch { }

        EvaluateState(fgWnd);
    }

    private void EvaluateState(IntPtr foregroundHwnd)
    {
        bool wasRunning = IsRobloxRunning;
        bool wasFocused = IsRobloxForeground;

        bool running = CheckAnyRobloxRunning(out uint runningPid, out string runningName);
        bool focused = false;
        string currentTitle = string.Empty;
        uint fgPid = 0;

        if (foregroundHwnd != IntPtr.Zero)
        {
            try
            {
                NativeMethods.GetWindowThreadProcessId(foregroundHwnd, out fgPid);
                currentTitle = GetWindowTitle(foregroundHwnd);
                string fgProcessName = GetProcessName(fgPid);

                focused = IsRobloxProcessOrWindow(fgProcessName, currentTitle, fgPid);
                if (focused && !running)
                {
                    running = true;
                    runningPid = fgPid;
                    runningName = fgProcessName;
                }
            }
            catch { }
        }

        RobloxProcessState newState;
        bool stateChanged = false;

        lock (_stateLock)
        {
            IsRobloxRunning = running;
            IsRobloxForeground = focused;

            newState = new RobloxProcessState
            {
                IsRunning = running,
                IsFocused = focused,
                ProcessId = focused ? fgPid : runningPid,
                ProcessName = focused ? GetProcessName(fgPid) : runningName,
                WindowTitle = currentTitle,
                LastUpdatedUtc = DateTime.UtcNow
            };
            State = newState;

            if (wasRunning != running || wasFocused != focused)
            {
                stateChanged = true;
            }
        }

        if (stateChanged)
        {
            if (!wasRunning && running)
            {
                _logger.LogInfo($"Roblox detected (PID: {newState.ProcessId}, Name: {newState.ProcessName})");
            }
            else if (wasRunning && !running)
            {
                _logger.LogInfo("Roblox closed / not running");
            }

            if (!wasFocused && focused)
            {
                _logger.LogInfo($"Roblox focused ({newState.ProcessName} - '{newState.WindowTitle}')");
            }
            else if (wasFocused && !focused)
            {
                _logger.LogInfo("Roblox lost focus");
            }

            StateChanged?.Invoke(this, newState);
        }
    }

    private static bool CheckAnyRobloxRunning(out uint pid, out string name)
    {
        pid = 0;
        name = string.Empty;
        uint currentPid = (uint)Environment.ProcessId;

        try
        {
            Process[] processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                try
                {
                    if (p.Id == currentPid) continue;

                    string pName = p.ProcessName.ToLowerInvariant();
                    // Explicitly ignore our own utility
                    if (pName.Contains("hotbarscroll")) continue;

                    foreach (var known in KnownRobloxProcessNames)
                    {
                        if (pName == known)
                        {
                            pid = (uint)p.Id;
                            name = p.ProcessName;
                            return true;
                        }
                    }
                }
                catch
                {
                    // Ignore inaccessible process
                }
                finally
                {
                    p.Dispose();
                }
            }
        }
        catch
        {
            // Ignore system enumeration failure
        }

        return false;
    }

    private static bool IsRobloxProcessOrWindow(string processName, string windowTitle, uint pid)
    {
        if (pid != 0 && pid == (uint)Environment.ProcessId) return false;

        string pLower = processName.ToLowerInvariant();
        string tLower = windowTitle.ToLowerInvariant();

        // Must NOT match our own application window or process
        if (pLower.Contains("hotbarscroll") || tLower.Contains("roblox hotbar scroll"))
        {
            return false;
        }

        foreach (var known in KnownRobloxProcessNames)
        {
            if (pLower == known) return true;
        }

        // Check window title for Roblox (e.g. UWP version or custom client wrapper)
        if ((pLower == "applicationframehost" || pLower == "windows10universal") &&
            (tLower == "roblox" || tLower.StartsWith("roblox -") || tLower.StartsWith("roblox ")))
        {
            return true;
        }

        return false;
    }

    private static string GetProcessName(uint pid)
    {
        if (pid == 0) return string.Empty;
        try
        {
            using var p = Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return string.Empty;
        var sb = new StringBuilder(256);
        NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Stop();
    }
}
