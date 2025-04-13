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

    public DirectionalBlock(string blockId, string name, Direction? freezeDirection = null, string textureTopPath = null,
        string textureRoundPath = null, string textureBottomPath = null) : base(blockId, name)
    {
        FreezeDirection = freezeDirection;
        _textureTopPath = textureTopPath ?? $"res://assets/blocks/{BlockId}/top.png";
        _textureRoundPath = textureRoundPath ?? $"res://assets/blocks/{BlockId}/round.png";
        _textureBottomPath = textureBottomPath ?? $"res://assets/blocks/{BlockId}/bottom.png";
    }

    public Direction? FreezeDirection { get; }

    public Direction Direction
    {
        get => FreezeDirection ?? _direction;
        set => _direction = value;
    }

    public override void LoadResources()
    {
        _materialTop = BlockHelper.GetMaterialByTexture(_textureTopPath);
        _materialRound = BlockHelper.GetMaterialByTexture(_textureRoundPath);
        _materialBottom = BlockHelper.GetMaterialByTexture(_textureBottomPath);
    }

    public override Material GetMaterial(Direction face = Direction.PositiveY)
    {
        if (face == Direction)
            return _materialTop;
        if (face == Direction.Opposite())
            return _materialBottom;
        return _materialRound;
    }

    public override bool Equals(Block other)
    {
        return other.BlockId == BlockId &&
               (other as DirectionalBlock).Direction == Direction;
    }
}
