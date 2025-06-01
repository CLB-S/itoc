using Godot;
using ITOC.Core;

namespace ITOC;

public partial class GuiBlockItem : Control
{
    public void SetBlock(Block block)
    {
        var _faceTop = GetNode<Sprite2D>("TopFace");
        var _faceRight = GetNode<Sprite2D>("RightFace");
        var _faceLeft = GetNode<Sprite2D>("LeftFace");

        _faceTop.Texture = (block as CubeBlock).BlockModel.GetTexture();
        _faceRight.Texture = (block as CubeBlock).BlockModel.GetTexture(Direction.PositiveX);
        _faceLeft.Texture = (block as CubeBlock).BlockModel.GetTexture(Direction.PositiveZ);
    }
}