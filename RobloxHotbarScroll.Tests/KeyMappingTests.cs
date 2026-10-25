using RobloxHotbarScroll.Utilities;
using Xunit;

namespace RobloxHotbarScroll.Tests;

public class KeyMappingTests
{
    // Helper replicating the WindowsInputManager filter logic
    public static bool TryParseMainNumberRowKey(uint vkCode, uint flags, out int slot)
    {
        slot = 0;
        bool isExtended = (flags & NativeMethods.LLKHF_EXTENDED) != 0;

        // Main number row keys VK_0 (0x30) to VK_9 (0x39), without extended flag
        if (!isExtended && vkCode >= 0x30 && vkCode <= 0x39)
        {
            slot = (vkCode == 0x30) ? 10 : (int)(vkCode - 0x30);
            return true;
        }

        return false;
    }

    [Theory]
    [InlineData(0x31, 1)] // Main '1' -> Slot 1
    [InlineData(0x32, 2)] // Main '2' -> Slot 2
    [InlineData(0x33, 3)] // Main '3' -> Slot 3
    [InlineData(0x34, 4)] // Main '4' -> Slot 4
    [InlineData(0x35, 5)] // Main '5' -> Slot 5
    [InlineData(0x36, 6)] // Main '6' -> Slot 6
    [InlineData(0x37, 7)] // Main '7' -> Slot 7
    [InlineData(0x38, 8)] // Main '8' -> Slot 8
    [InlineData(0x39, 9)] // Main '9' -> Slot 9
    [InlineData(0x30, 10)] // Main '0' -> Slot 10
    public void MainNumberRow_MappedToCorrectSlot(uint vk, int expectedSlot)
    {
        bool parsed = TryParseMainNumberRowKey(vk, 0, out int slot);
        Assert.True(parsed);
        Assert.Equal(expectedSlot, slot);
    }

    [Theory]
    [InlineData(0x60)] // VK_NUMPAD0
    [InlineData(0x61)] // VK_NUMPAD1
    [InlineData(0x62)] // VK_NUMPAD2
    [InlineData(0x63)] // VK_NUMPAD3
    [InlineData(0x64)] // VK_NUMPAD4
    [InlineData(0x65)] // VK_NUMPAD5
    [InlineData(0x66)] // VK_NUMPAD6
    [InlineData(0x67)] // VK_NUMPAD7
    [InlineData(0x68)] // VK_NUMPAD8
    [InlineData(0x69)] // VK_NUMPAD9
    public void NumpadKeys_AreStrictlyIgnored(uint numpadVk)
    {
        // Must return false and not produce any slot
        bool parsed = TryParseMainNumberRowKey(numpadVk, 0, out int slot);
        Assert.False(parsed);
        Assert.Equal(0, slot);
    }

    [Theory]
    [InlineData(0x23)] // VK_END (Numpad 1 with NumLock off)
    [InlineData(0x28)] // VK_DOWN (Numpad 2 with NumLock off)
    [InlineData(0x22)] // VK_NEXT (Numpad 3 with NumLock off)
    [InlineData(0x25)] // VK_LEFT (Numpad 4 with NumLock off)
    [InlineData(0x0C)] // VK_CLEAR (Numpad 5 with NumLock off)
    [InlineData(0x27)] // VK_RIGHT (Numpad 6 with NumLock off)
    [InlineData(0x24)] // VK_HOME (Numpad 7 with NumLock off)
    [InlineData(0x26)] // VK_UP (Numpad 8 with NumLock off)
    [InlineData(0x21)] // VK_PRIOR (Numpad 9 with NumLock off)
    [InlineData(0x2D)] // VK_INSERT (Numpad 0 with NumLock off)
    public void NumpadNumLockOff_AreStrictlyIgnored(uint numpadNavVk)
    {
        bool parsed = TryParseMainNumberRowKey(numpadNavVk, 0, out int slot);
        Assert.False(parsed);
        Assert.Equal(0, slot);
    }

    [Fact]
    public void SendInput_StructureSize_AndAlignment_MatchesWin32Spec()
    {
        int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.INPUT));
        int offsetU = (int)System.Runtime.InteropServices.Marshal.OffsetOf(typeof(NativeMethods.INPUT), "u");
        int offsetExtra = (int)System.Runtime.InteropServices.Marshal.OffsetOf(typeof(NativeMethods.KEYBDINPUT), "dwExtraInfo");

        if (IntPtr.Size == 8) // x64 Windows
        {
            Assert.Equal(40, size);
            Assert.Equal(8, offsetU);
            Assert.Equal(16, offsetExtra);
        }
        else // x86 Windows
        {
            Assert.Equal(28, size);
            Assert.Equal(4, offsetU);
            Assert.Equal(12, offsetExtra);
        }
    }

    [Theory]
    [InlineData("F8", 0x77)] // VK_F8
    [InlineData("F1", 0x70)] // VK_F1
    [InlineData("F12", 0x7B)] // VK_F12
    [InlineData("Ctrl+F8", 0x77)]
    [InlineData("Alt+F8", 0x77)]
    [InlineData("Ctrl+Alt+F8", 0x77)]
    [InlineData("K", (uint)'K')]
    public void ParseKeyToVk_ParsesCorrectly(string hotkey, uint expectedVk)
    {
        uint vk = RobloxHotbarScroll.Services.WindowsInputManager.ParseKeyToVk(hotkey);
        Assert.Equal(expectedVk, vk);
    }
}
