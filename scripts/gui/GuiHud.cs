using Godot;
using System;

public partial class GuiHud : Control
{
    public GuiInventoryHotbar InventoryHotbar;

    public override void _Ready()
    {
        InventoryHotbar = GetNode<GuiInventoryHotbar>("InventoryHotbar");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("hide_hud"))
            Visible = !Visible;
    }
}