using Godot;
using System;

public abstract class Block
{
    public int BlockID { get; protected set; }
    public string BlockName { get; protected set; }
    public Color Color { get; protected set; } = Colors.White;
    public float Hardness { get; protected set; } = 1.0f;
    public bool IsSolid { get; } = true;
    public bool IsTransparent { get; protected set; } = false;
    public bool IsLightSource { get; protected set; } = false;
    public float LightStrength { get; protected set; } = 0;
    public virtual string[] ModelTypes => new[] { "cube" };

    public abstract void LoadResources();
    public virtual Mesh GetMesh(string modelType = "cube") => null;
    public virtual Texture2D GetTexture(Direction face = Direction.PositiveX) => null;
}