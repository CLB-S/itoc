using Godot;

public class DirectionalBlock : Block
{
    private Direction _direction;
    private Material _materialBottom;
    private Material _materialRound;
    private Material _materialTop;
    private Texture2D _textureBottom;
    private Texture2D _textureRound;
    private Texture2D _textureTop;
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
        _textureTop = ResourceLoader.Load(_textureTopPath) as Texture2D;
        _textureRound = ResourceLoader.Load(_textureRoundPath) as Texture2D;
        _textureBottom = ResourceLoader.Load(_textureBottomPath) as Texture2D;

        _materialTop = BlockHelper.GetMaterialByTexture(_textureTop);
        _materialRound = BlockHelper.GetMaterialByTexture(_textureRound);
        _materialBottom = BlockHelper.GetMaterialByTexture(_textureBottom);
    }

    public override Material GetMaterial(Direction face = Direction.PositiveY)
    {
        if (face == Direction)
            return _materialTop;
        if (face == Direction.Opposite())
            return _materialBottom;
        return _materialRound;
    }

    public override Texture2D GetTexture(Direction face = Direction.PositiveY)
    {
        if (face == Direction)
            return _textureTop;
        if (face == Direction.Opposite())
            return _textureBottom;
        return _textureRound;
    }

    public override bool Equals(Block other)
    {
        return other.BlockId == BlockId &&
               (other as DirectionalBlock).Direction == Direction;
    }
}
