using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using RobloxHotbarScroll.Utilities;

namespace RobloxHotbarScroll.Services;

public class WindowsInputSimulator : IInputSimulator, IDisposable
{
    private readonly ILoggerService _logger;
    private readonly BlockingCollection<SimulatedKeyCommand> _queue = new();
    private readonly Thread _workerThread;
    private volatile bool _isRunning = true;
    private bool _isDisposed;

    private record SimulatedKeyCommand(int Slot, ushort Vk, ushort ScanCode, char KeyChar);

    public WindowsInputSimulator(ILoggerService logger)
    {
        _logger = logger;
        _workerThread = new Thread(ProcessQueueLoop)
        {
            Name = "RobloxHotbarScroll.InputSimulator",
            IsBackground = true
        };
        _workerThread.Start();
    }

    public bool SendSlotKey(int slot)
    {
        if (slot < 1 || slot > 10)
        {
            _logger.LogWarning($"Invalid slot requested for keyboard simulation: {slot}");
            return false;
        }

        // Slot 1..9 -> Main keyboard '1'..'9' (VK: 0x31..0x39, Scan: 0x02..0x0A)
        // Slot 10   -> Main keyboard '0' (VK: 0x30, Scan: 0x0B)
        ushort vk = (ushort)(slot == 10 ? 0x30 : (0x30 + slot));
        ushort scanCode = (ushort)NativeMethods.MapVirtualKey(vk, NativeMethods.MAPVK_VK_TO_VSC);
        char keyChar = slot == 10 ? '0' : (char)('0' + slot);

        try
        {
            if (!_queue.IsAddingCompleted)
            {
                _queue.Add(new SimulatedKeyCommand(slot, vk, scanCode, keyChar));
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogException($"Failed to enqueue slot key for slot {slot}", ex);
        }

        return false;
    }

    private void ProcessQueueLoop()
    {
        while (_isRunning)
        {
            try
            {
                if (!_queue.TryTake(out var cmd, 500))
                {
                    continue;
                }

                ExecuteKeySimulation(cmd);
            }
            catch (ThreadAbortException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogException("Error processing input simulator queue", ex);
            }
        }
    }

    private void ExecuteKeySimulation(SimulatedKeyCommand cmd)
    {
        _logger.LogDebug("Generating Windows keyboard input");
        _logger.LogDebug($"KEY DOWN: {cmd.KeyChar}");

        bool success = false;
        string? failureReason = null;

        // Method 1: SendInput with exact 40-byte x64 INPUT layout
        try
        {
            int cbSize = Marshal.SizeOf<NativeMethods.INPUT>();

            var inputDown = new NativeMethods.INPUT[1];
            inputDown[0].type = NativeMethods.INPUT_KEYBOARD;
            inputDown[0].u.ki.wVk = cmd.Vk;
            inputDown[0].u.ki.wScan = cmd.ScanCode;
            inputDown[0].u.ki.dwFlags = 0; // KeyDown
            inputDown[0].u.ki.time = 0;
            inputDown[0].u.ki.dwExtraInfo = NativeMethods.SYNTHETIC_EXTRA_INFO;

            uint sentDown = NativeMethods.SendInput(1, inputDown, cbSize);
            if (sentDown > 0)
            {
                // Hold duration (~18ms) for DirectX / DirectInput game frame loops
                Thread.Sleep(18);

                _logger.LogDebug($"KEY UP: {cmd.KeyChar}");

                var inputUp = new NativeMethods.INPUT[1];
                inputUp[0].type = NativeMethods.INPUT_KEYBOARD;
                inputUp[0].u.ki.wVk = cmd.Vk;
                inputUp[0].u.ki.wScan = cmd.ScanCode;
                inputUp[0].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
                inputUp[0].u.ki.time = 0;
                inputUp[0].u.ki.dwExtraInfo = NativeMethods.SYNTHETIC_EXTRA_INFO;

                uint sentUp = NativeMethods.SendInput(1, inputUp, cbSize);
                if (sentUp > 0)
                {
                    success = true;
                }
                else
                {
                    int err = Marshal.GetLastWin32Error();
                    failureReason = $"SendInput KeyUp Win32 Error: {err}";
                }
            }
            else
            {
                int err = Marshal.GetLastWin32Error();
                failureReason = $"SendInput KeyDown Win32 Error: {err}";
            }
        }
        catch (Exception ex)
        {
            failureReason = $"SendInput exception: {ex.Message}";
        }

        // Method 2: Fallback to Win32 keybd_event if SendInput failed (e.g. UIPI or restricted session)
        if (!success)
        {
            try
            {
                _logger.LogWarning($"SendInput fallback to keybd_event for key '{cmd.KeyChar}' (Reason: {failureReason})");

                NativeMethods.keybd_event((byte)cmd.Vk, (byte)cmd.ScanCode, 0, NativeMethods.SYNTHETIC_EXTRA_INFO);
                Thread.Sleep(18);

                _logger.LogDebug($"KEY UP: {cmd.KeyChar}");
                NativeMethods.keybd_event((byte)cmd.Vk, (byte)cmd.ScanCode, NativeMethods.KEYEVENTF_KEYUP, NativeMethods.SYNTHETIC_EXTRA_INFO);

                success = true;
            }
            catch (Exception ex)
            {
                failureReason = $"{failureReason}; keybd_event: {ex.Message}";
            }
        }

        if (success)
        {
            _logger.LogDebug("SUCCESS");
            _logger.LogInfo($"Simulated physical keyboard key '{cmd.KeyChar}' (Slot {cmd.Slot}, VK: 0x{cmd.Vk:X2}, Scan: 0x{cmd.ScanCode:X2})");
        }
        else
        {
            _logger.LogError($"Generating Windows keyboard input FAILED for slot {cmd.Slot}: {failureReason}");
        }

        // Brief delay between successive keystrokes to ensure recipient app's message loop processes cleanly
        Thread.Sleep(8);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _isRunning = false;
        try
        {
            _queue.CompleteAdding();
            if (!_workerThread.Join(500))
            {
                _workerThread.Interrupt();
            }
        }
        catch { }

        _queue.Dispose();
    }
}
