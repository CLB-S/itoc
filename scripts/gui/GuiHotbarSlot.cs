using Godot;
using System;

public partial class GuiHotbarSlot : TextureRect
{
    [Export] public Texture2D TextureNormal;
    [Export] public Texture2D TextureActive;
    public Block Block { get => _blockItem.Block; set => _blockItem.SetBlock(value); }

    private bool _isActive = false;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            UpdateTexture();
        }
    }

    private GuiBlockItem _blockItem;
    public override void _Ready()
    {
        _blockItem = GetNode<GuiBlockItem>("BlockItem");
        UpdateTexture();
    }

    public void Clear()
    {
        _blockItem.Clear();
    }

    public void SetBlock(Block block)
    {
        _blockItem.SetBlock(block);
        UpdateTexture();
    }

    private void UpdateTexture()
    {
        Texture = IsActive ? TextureActive : TextureNormal;
    }
}
