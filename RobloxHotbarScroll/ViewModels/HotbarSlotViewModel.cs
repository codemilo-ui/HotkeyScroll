using System;
using System.Windows.Input;

namespace RobloxHotbarScroll.ViewModels;

public class HotbarSlotViewModel : ViewModelBase
{
    private bool _isSelected;

    public int SlotNumber { get; }
    public string KeyLabel { get; }
    public string SlotDisplayName => $"Slot {SlotNumber}";
    public string TooltipText => $"Slot {SlotNumber} (Keyboard '{KeyLabel}')";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ICommand SelectCommand { get; }

    public HotbarSlotViewModel(int slotNumber, Action<int> onSelected)
    {
        SlotNumber = slotNumber;
        KeyLabel = slotNumber == 10 ? "0" : slotNumber.ToString();
        SelectCommand = new RelayCommand(() => onSelected(slotNumber));
    }
}
