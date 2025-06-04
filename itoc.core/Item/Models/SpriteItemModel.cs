using Godot;

namespace ITOC.Core.Items.Models;

public class SpriteItemModel : IItemModel
{
    public static readonly SpriteItemModel FallBack = new SpriteItemModel("res://assets/blocks/debug.png");

    public Texture2D ItemTexture { get; private set; }

    public SpriteItemModel(string texturePath)
    {
        ItemTexture = ResourceLoader.Load<Texture2D>(texturePath);
        if (ItemTexture == null)
            throw new InvalidOperationException($"Failed to load texture from path: {texturePath}");
    }
}