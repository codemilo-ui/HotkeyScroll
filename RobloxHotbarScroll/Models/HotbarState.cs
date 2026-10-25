using System;

namespace RobloxHotbarScroll.Models;

public enum SlotChangeSource
{
    Initial,
    Scroll,
    ManualKey,
    UserInterface,
    Api
}

public class HotbarState
{
    public int CurrentSlot { get; set; } = 1;
    public int PreviousSlot { get; set; } = 1;
    public SlotChangeSource LastSource { get; set; } = SlotChangeSource.Initial;
    public DateTime LastChangedUtc { get; set; } = DateTime.UtcNow;

    public HotbarState() { }

    public HotbarState(int currentSlot, int previousSlot, SlotChangeSource source)
    {
        CurrentSlot = currentSlot;
        PreviousSlot = previousSlot;
        LastSource = source;
        LastChangedUtc = DateTime.UtcNow;
    }
}
