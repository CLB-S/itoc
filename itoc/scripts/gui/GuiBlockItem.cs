using Godot;

public partial class GuiBlockItem : Control
{
    public void SetBlock(Block block)
    {
        var _faceTop = GetNode<Sprite2D>("TopFace");
        var _faceRight = GetNode<Sprite2D>("RightFace");
        var _faceLeft = GetNode<Sprite2D>("LeftFace");

        _faceTop.Texture = block.GetTexture();
        _faceRight.Texture = block.GetTexture(Direction.PositiveX);
        _faceLeft.Texture = block.GetTexture(Direction.PositiveZ);
    }
}