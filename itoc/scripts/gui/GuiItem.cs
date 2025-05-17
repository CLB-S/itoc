using Godot;

public partial class GuiItem : Control
{
    public IItem Item { get; private set; }
    public string ItemId => Item?.Id;
    public string ItemName => Item?.Name;
    public string ItemDescription => Item?.Description;

    private Control _itemIcon;

    public void Clear()
    {
        if (_itemIcon != null)
        {
            _itemIcon.QueueFree();
            _itemIcon = null;
        }
    }

    public void SetItem(IItem item)
    {
        if (item.Type == ItemType.Block)
        {
            var blockItem = GD.Load<PackedScene>("res://scenes/gui/block_item.tscn").Instantiate<GuiBlockItem>();
            AddChild(blockItem);
            blockItem.SetBlock(item as Block);
            _itemIcon = blockItem;
            Item = item;
        }
        else
        {
            GD.PrintErr($"Unsupported item type: {item.Type}");
        }
    }
}