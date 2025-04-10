using Godot;

public abstract class Block
{
    /// <summary>
    ///     The unique ID of the block. Top 3 bits are reserved for direction.
    /// </summary>
    public ushort BlockId { get; protected set; }

    public string BlockName { get; protected set; }
    public Color Color { get; set; } = Colors.White;
    public float Hardness { get; set; } = 1.0f;
    public bool IsSolid { get; set; } = true;
    public bool IsOpaque { get; set; } = true;
    public bool IsLightSource { get; set; } = false;

    public float LightStrength { get; set; } = 0;
    // public virtual string[] ModelTypes => new[] { "cube" };

    public abstract void LoadResources();

    public abstract Material GetMaterial(Direction face = Direction.PositiveY);
    // public virtual Mesh GetMesh(string modelType = "cube") => null;
}