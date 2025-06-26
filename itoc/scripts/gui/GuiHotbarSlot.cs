using Godot;
using ITOC.Core.Item;

namespace ITOC;

public partial class GuiHotbarSlot : TextureRect
{
    [Export]
    public Texture2D TextureNormal;

    [Export]
    public Texture2D TextureActive;

    private GuiItem _itemControl;

    public IItem Item => _itemControl.Item;

    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            UpdateTexture();
        }
    }

    public override void _Ready()
    {
        _itemControl = GetNode<GuiItem>("Item");
        UpdateTexture();
    }

    public void Clear() => _itemControl.Clear();

    public void SetItem(IItem item) => _itemControl.SetItem(item);

    private void UpdateTexture() => Texture = IsActive ? TextureActive : TextureNormal;
}
