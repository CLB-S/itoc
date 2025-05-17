using System;
using Godot;

public partial class GuiInventoryHotbar : GridContainer
{
    private GuiHotbarSlot[] _slots;
    private int _activeSlotIndex;

    public IItem ActiveItem => _slots[_activeSlotIndex].Item;

    public override void _Ready()
    {
        _slots = new GuiHotbarSlot[GetChildCount()];
        for (var i = 0; i < GetChildCount(); i++) _slots[i] = GetChild<GuiHotbarSlot>(i);

        _slots[_activeSlotIndex].IsActive = true;

        _slots[0].SetItem(BlockManager.Instance.GetBlock("dirt"));
        _slots[1].SetItem(BlockManager.Instance.GetBlock("stone"));
        _slots[2].SetItem(BlockManager.Instance.GetBlock("grass_block"));
        _slots[3].SetItem(BlockManager.Instance.GetBlock("sand"));
        _slots[4].SetItem(BlockManager.Instance.GetBlock("snow"));
        _slots[5].SetItem(BlockManager.Instance.GetBlock("debug"));
    }

    public void SetActiveSlot(int index)
    {
        if (index < 0 || index >= _slots.Length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the range of available slots.");

        _slots[_activeSlotIndex].IsActive = false;
        _activeSlotIndex = index;
        _slots[_activeSlotIndex].IsActive = true;
    }

    public void NextSlot()
    {
        SetActiveSlot((_activeSlotIndex + 1) % _slots.Length);
    }

    public void PreviousSlot()
    {
        SetActiveSlot((_activeSlotIndex - 1 + _slots.Length) % _slots.Length);
    }
}