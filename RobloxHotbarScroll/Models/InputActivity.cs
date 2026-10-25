using System;

namespace RobloxHotbarScroll.Models;

public class InputActivity
{
    public DateTime Timestamp { get; } = DateTime.Now;
    public string ActionName { get; set; } = "None";
    public int CurrentSlot { get; set; } = 1;
    public int TargetSlot { get; set; } = 1;
    public string SentKey { get; set; } = "None";
    public string Result { get; set; } = "Ready";
    public bool IsSimulated { get; set; }

    public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");

    public string SlotTransition => $"{CurrentSlot} → {TargetSlot}";
}
