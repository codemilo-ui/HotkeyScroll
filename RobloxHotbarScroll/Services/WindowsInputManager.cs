using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using RobloxHotbarScroll.Models;
using RobloxHotbarScroll.Utilities;

namespace RobloxHotbarScroll.Services;

public class WindowsInputManager : IInputManager
{
    private readonly ILoggerService _logger;
    private readonly ISettingsService _settingsService;
    private readonly IHotbarController _hotbarController;

    private Thread? _hookThread;
    private uint _hookThreadId;
    private readonly ManualResetEventSlim _startedEvent = new(false);

    private IntPtr _mouseHook = IntPtr.Zero;
    private IntPtr _keyboardHook = IntPtr.Zero;

    private NativeMethods.LowLevelProc? _mouseProc;
    private NativeMethods.LowLevelProc? _keyboardProc;

    private int _wheelDeltaAccumulator;
    private bool _isDisposed;

    public bool IsRunning => _hookThread != null && _hookThread.IsAlive;
    public bool IsHookActive { get; private set; }
    public string? LastError { get; private set; }
    public event EventHandler<bool>? HookStatusChanged;

    public WindowsInputManager(
        ILoggerService logger,
        ISettingsService settingsService,
        IHotbarController hotbarController)
    {
        _logger = logger;
        _settingsService = settingsService;
        _hotbarController = hotbarController;
    }

    public void Start()
    {
        if (IsRunning)
        {
            Stop();
        }

        try
        {
            _startedEvent.Reset();
            _hookThread = new Thread(HookThreadLoop)
            {
                Name = "RobloxHotbarScroll.InputHookThread",
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };
            _hookThread.SetApartmentState(ApartmentState.STA);
            _hookThread.Start();

            bool initialized = _startedEvent.Wait(TimeSpan.FromSeconds(2));
            if (!initialized)
            {
                LastError = "Input hook initialization timed out.";
                _logger.LogWarning(LastError);
                IsHookActive = false;
                HookStatusChanged?.Invoke(this, false);
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            IsHookActive = false;
            _logger.LogException("Failed to start input manager hook thread", ex);
            HookStatusChanged?.Invoke(this, false);
        }
    }

    public void Stop()
    {
        if (!IsRunning || _hookThreadId == 0) return;

        try
        {
            NativeMethods.PostThreadMessage(_hookThreadId, NativeMethods.WM_QUIT, UIntPtr.Zero, IntPtr.Zero);
            if (_hookThread != null && !_hookThread.Join(1500))
            {
                _logger.LogWarning("Input hook thread did not stop within timeout.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogException("Error stopping input manager thread", ex);
        }
        finally
        {
            _hookThread = null;
            _hookThreadId = 0;
            IsHookActive = false;
            HookStatusChanged?.Invoke(this, false);
            _logger.LogInfo("Input manager stopped.");
        }
    }

    private void HookThreadLoop()
    {
        _hookThreadId = NativeMethods.GetCurrentThreadId();
        bool success = false;

        try
        {
            using var currentProcess = Process.GetCurrentProcess();
            using var mainModule = currentProcess.MainModule;
            IntPtr moduleHandle = NativeMethods.GetModuleHandle(mainModule?.ModuleName);

            _mouseProc = MouseHookCallback;
            _mouseHook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_MOUSE_LL,
                _mouseProc,
                moduleHandle,
                0
            );

            if (_mouseHook == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                LastError = $"Mouse hook installation failed (Win32 error: {error})";
                _logger.LogWarning(LastError);
            }

            _keyboardProc = KeyboardHookCallback;
            _keyboardHook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL,
                _keyboardProc,
                moduleHandle,
                0
            );

            if (_keyboardHook == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                LastError = $"Keyboard hook installation failed (Win32 error: {error})";
                _logger.LogWarning(LastError);
            }

            success = (_mouseHook != IntPtr.Zero && _keyboardHook != IntPtr.Zero);
            IsHookActive = success;

            if (success)
            {
                LastError = null;
                _logger.LogInfo("Input manager initialized (Mouse & Keyboard hooks active).");
                HookStatusChanged?.Invoke(this, true);
            }
            else
            {
                HookStatusChanged?.Invoke(this, false);
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            IsHookActive = false;
            _logger.LogException("Exception inside input hook thread initialization", ex);
            HookStatusChanged?.Invoke(this, false);
        }
        finally
        {
            _startedEvent.Set();
        }

        try
        {
            while (NativeMethods.GetMessage(out NativeMethods.MSG msg, IntPtr.Zero, 0, 0))
            {
                NativeMethods.TranslateMessage(ref msg);
                NativeMethods.DispatchMessage(ref msg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogException("Exception in input hook message pump", ex);
        }

        try
        {
            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }

            if (_keyboardHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }
        }
        catch { }

        _mouseProc = null;
        _keyboardProc = null;
        IsHookActive = false;
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_MOUSEWHEEL)
        {
            try
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

                if (hookStruct.dwExtraInfo == NativeMethods.SYNTHETIC_EXTRA_INFO)
                {
                    return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
                }

                // Check whether remapping is active (Enabled && focused in Roblox)
                bool isActive = _hotbarController.IsActive;

                if (isActive)
                {
                    short rawDelta = unchecked((short)((hookStruct.mouseData >> 16) & 0xFFFF));
                    ProcessWheelDelta(rawDelta);

                    // When Roblox is focused and remapping is active,
                    // INTERCEPT the mouse wheel completely so Roblox does NOT zoom its camera
                    if (_settingsService.CurrentSettings.BlockMouseWheelInRoblox)
                    {
                        return (IntPtr)1;
                    }
                }
                else
                {
                    _wheelDeltaAccumulator = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("Error in mouse hook callback", ex);
            }
        }

        // Pass through wheel normally when paused or outside Roblox
        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private bool ProcessWheelDelta(short rawDelta)
    {
        _wheelDeltaAccumulator += rawDelta;
        int sensitivity = Math.Max(1, _settingsService.CurrentSettings.ScrollSensitivity);
        int threshold = 120 * sensitivity;

        bool stepTriggered = false;
        while (Math.Abs(_wheelDeltaAccumulator) >= threshold)
        {
            int direction = _wheelDeltaAccumulator > 0 ? 1 : -1;
            _wheelDeltaAccumulator -= direction * threshold;
            _hotbarController.HandleScroll(direction);
            stepTriggered = true;
        }

        return stepTriggered;
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN))
        {
            try
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

                if (hookStruct.dwExtraInfo == NativeMethods.SYNTHETIC_EXTRA_INFO)
                {
                    return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
                }

                uint vk = hookStruct.vkCode;

                // 1. Check Global Toggle Hotkey (e.g. F8 or Ctrl+F8)
                if (CheckToggleHotkey(vk))
                {
                    _logger.LogInfo($"Global toggle hotkey '{_settingsService.CurrentSettings.ToggleHotkey}' pressed.");
                    _hotbarController.ToggleEnabled();

                    // Intercept and consume the toggle hotkey so Roblox never receives F8!
                    return (IntPtr)1;
                }

                // 2. Observe physical main number row keys (VK_0: 0x30 to VK_9: 0x39, non-extended)
                bool isExtended = (hookStruct.flags & NativeMethods.LLKHF_EXTENDED) != 0;
                if (!isExtended && vk >= 0x30 && vk <= 0x39)
                {
                    int slot = (vk == 0x30) ? 10 : (int)(vk - 0x30);
                    _hotbarController.HandleManualKey(slot);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("Error in keyboard hook callback", ex);
            }
        }

        // Always pass physical key through to Roblox without re-sending
        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private bool CheckToggleHotkey(uint vk)
    {
        string hotkey = _settingsService.CurrentSettings.ToggleHotkey;
        if (string.IsNullOrWhiteSpace(hotkey)) hotkey = "F8";

        bool reqCtrl = hotkey.Contains("Ctrl", StringComparison.OrdinalIgnoreCase) || hotkey.Contains("Control", StringComparison.OrdinalIgnoreCase);
        bool reqAlt = hotkey.Contains("Alt", StringComparison.OrdinalIgnoreCase);
        bool reqShift = hotkey.Contains("Shift", StringComparison.OrdinalIgnoreCase);

        bool isCtrlDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
        bool isAltDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;
        bool isShiftDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;

        if (reqCtrl != isCtrlDown || reqAlt != isAltDown || reqShift != isShiftDown)
        {
            return false;
        }

        uint targetVk = ParseKeyToVk(hotkey);
        return vk == targetVk;
    }

    public static uint ParseKeyToVk(string hotkeyStr)
    {
        string[] parts = hotkeyStr.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        string keyPart = parts[^1].ToUpperInvariant();

        if (keyPart.StartsWith("F") && int.TryParse(keyPart.Substring(1), out int fNum) && fNum >= 1 && fNum <= 12)
        {
            return (uint)(0x70 + (fNum - 1)); // 0x70 = VK_F1, 0x77 = VK_F8
        }

        if (keyPart.Length == 1)
        {
            char c = keyPart[0];
            if (c >= 'A' && c <= 'Z') return (uint)c;
            if (c >= '0' && c <= '9') return (uint)c;
        }

        return (uint)NativeMethods.VK_F8;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Stop();
        _startedEvent.Dispose();
    }
}
