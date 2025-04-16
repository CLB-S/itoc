using Godot;
using System;

public partial class GuiInventoryHotbar : GridContainer
{
    private GuiHotbarSlot[] _slots;
    private int _activeSlotIndex = 0;

    public Block ActiveBlock
    {
        get => _slots[_activeSlotIndex].Block;
    }

    public override void _Ready()
    {
        _slots = new GuiHotbarSlot[GetChildCount()];
        for (int i = 0; i < GetChildCount(); i++)
        {
            _slots[i] = GetChild<GuiHotbarSlot>(i);
        }

        _slots[_activeSlotIndex].IsActive = true;
    }

    public void SetActiveSlot(int index)
    {
        if (index < 0 || index >= _slots.Length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the range of available slots.");

        _slots[_activeSlotIndex].IsActive = false;
        _activeSlotIndex = index;
        _slots[_activeSlotIndex].IsActive = true;
    }
}
