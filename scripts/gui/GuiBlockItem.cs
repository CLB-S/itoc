using Godot;
using System;

public partial class GuiBlockItem : Control
{
    private Sprite2D _faceTop;
    private Sprite2D _faceRight;
    private Sprite2D _faceLeft;

    public Block Block { get; private set; }

    public GuiBlockItem() { }

    public GuiBlockItem(Block block)
    {
        Block = block;
    }

    public override void _Ready()
    {
        _faceTop = GetNode<Sprite2D>("TopFace");
        _faceRight = GetNode<Sprite2D>("RightFace");
        _faceLeft = GetNode<Sprite2D>("LeftFace");

        if (Block != null)
            SetBlock(Block);
    }

    public void Clear()
    {
        _faceTop.Texture = null;
        _faceRight.Texture = null;
        _faceLeft.Texture = null;
    }

    public void SetBlock(Block block)
    {
        _faceTop.Texture = block.GetTexture(Direction.PositiveY);
        _faceRight.Texture = block.GetTexture(Direction.PositiveX);
        _faceLeft.Texture = block.GetTexture(Direction.PositiveZ);

        Block = block;
    }
}
