using System;
using RobloxHotbarScroll.Models;

namespace RobloxHotbarScroll.Services;

public class HotbarChangedEventArgs : EventArgs
{
    public int NewSlot { get; }
    public int PreviousSlot { get; }
    public SlotChangeSource Source { get; }

    public HotbarChangedEventArgs(int newSlot, int previousSlot, SlotChangeSource source)
    {
        NewSlot = newSlot;
        PreviousSlot = previousSlot;
        Source = source;
    }
}

public interface IHotbarController
{
    int CurrentSlot { get; }
    bool IsEnabled { get; }
    bool IsActive { get; }
    InputActivity LastInput { get; }

    event EventHandler<HotbarChangedEventArgs>? SlotChanged;
    event EventHandler<bool>? EnabledChanged;
    event EventHandler<InputActivity>? InputActivityUpdated;

    void ToggleEnabled();
    void SetEnabled(bool enabled);
    void SetSlot(int slot, SlotChangeSource source);
    void NextSlot();
    void PreviousSlot();
    void HandleScroll(int direction);
    void HandleManualKey(int slot);
}
