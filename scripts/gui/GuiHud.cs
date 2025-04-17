using Godot;
using System;

public partial class GuiHud : Control
{
    public GuiInventoryHotbar InventoryHotbar;

    public override void _Ready()
    {
        InventoryHotbar = GetNode<GuiInventoryHotbar>("InventoryHotbar");
    }
}