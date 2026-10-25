using System;

namespace RobloxHotbarScroll.Models;

public class RobloxProcessState
{
    public bool IsRunning { get; set; }
    public bool IsFocused { get; set; }
    public uint ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    public string StatusSummary
    {
        get
        {
            if (IsFocused) return "Roblox is active & focused";
            if (IsRunning) return "Roblox running in background";
            return "Roblox not running";
        }
    }
}
