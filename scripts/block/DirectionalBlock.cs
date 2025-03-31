using Godot;

public class DirectionalBlock : Block
{
    private Direction _direction;
    private Material _materialBottom;
    private Material _materialRound;
    private Material _materialTop;
    private readonly string _textureBottomPath;
    private readonly string _textureRoundPath;

    private readonly string _textureTopPath;

    public DirectionalBlock(uint blockId, string name, Direction? freezeDirection = null, string textureTopPath = null,
        string textureRoundPath = null, string textureBottomPath = null)
    {
        BlockId = blockId;
        BlockName = name;
        FreezeDirection = freezeDirection;
        _textureTopPath = textureTopPath ?? $"res://assets/blocks/{BlockName}/top.png";
        _textureRoundPath = textureRoundPath ?? $"res://assets/blocks/{BlockName}/round.png";
        _textureBottomPath = textureBottomPath ?? $"res://assets/blocks/{BlockName}/bottom.png";
    }

    public Direction? FreezeDirection { get; }

    public Direction Direction
    {
        get => FreezeDirection ?? _direction;
        set => _direction = value;
    }

    public override void LoadResources()
    {
        _materialTop = BlockHelper.GetMaterial(_textureTopPath);
        _materialRound = BlockHelper.GetMaterial(_textureRoundPath);
        _materialBottom = BlockHelper.GetMaterial(_textureBottomPath);
    }

    public override Material GetMaterial(Direction face = Direction.PositiveY)
    {
        if (face == Direction)
            return _materialTop;
        if (face == Direction.Opposite())
            return _materialBottom;
        return _materialRound;
    }
}