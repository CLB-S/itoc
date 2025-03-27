using Godot;
using System;
using System.Collections.Generic;

public class DirectionalBlock : Block
{
    private Material _materialTop;
    private Material _materialRound;
    private Material _materialBottom;

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
        _materialTop = BlockHelper.GetMaterial(_textureTopPath);
        _materialRound = BlockHelper.GetMaterial(_textureRoundPath);
        _materialBottom = BlockHelper.GetMaterial(_textureBottomPath);
    }

    public override Material GetMaterial(Direction face = Direction.PositiveX)
    {
        if (face == Direction.PositiveY)
            return _materialTop;
        if (face == Direction.NegativeY)
            return _materialBottom;
        return _materialRound;
    }
}
