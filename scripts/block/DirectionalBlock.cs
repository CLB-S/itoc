using Godot;
using System;
using System.Collections.Generic;

public class DirectionalBlock : Block
{
    public Texture2D TextureTop { get; private set; }
    public Texture2D TextureRound { get; private set; }
    public Texture2D TextureBottom { get; private set; }

    public Direction? FreezeDirection { get; private set; }

    private Direction _direction;
    public Direction Direction
    {
        get { return FreezeDirection ?? _direction; }
        set { _direction = value; }
    }

    private string _textureTopPath;
    private string _textureRoundPath;
    private string _textureBottomPath;

    public DirectionalBlock(int id, string name, Direction? freezeDirection = null, string textureTopPath = null, string textureRoundPath = null, string textureBottomPath = null)
    {
        BlockID = id;
        BlockName = name;
        FreezeDirection = freezeDirection;
        _textureTopPath = textureTopPath ?? $"res://assets/blocks/{BlockName}/top.png";
        _textureRoundPath = textureRoundPath ?? $"res://assets/blocks/{BlockName}/round.png";
        _textureBottomPath = textureBottomPath ?? $"res://assets/blocks/{BlockName}/bottom.png";
    }

    public override void LoadResources()
    {
        TextureTop = ResourceLoader.Load(_textureTopPath) as Texture2D;
        TextureRound = ResourceLoader.Load(_textureRoundPath) as Texture2D;
        TextureBottom = ResourceLoader.Load(_textureBottomPath) as Texture2D;
    }

    public override Texture2D GetTexture(Direction face = Direction.PositiveX)
    {

        if (face == this.Direction)
        {
            return TextureTop;
        }
        else if (face == this.Direction.Opposite())
        {
            return TextureBottom;
        }
        else
        {
            return TextureRound;
        }
    }
}
