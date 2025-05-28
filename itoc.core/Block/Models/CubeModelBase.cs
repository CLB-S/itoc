using System.Collections.Generic;
using Godot;

namespace ITOC.Core.BlockModels;

public abstract class CubeModelBase : BlockModelBase
{
    protected readonly Dictionary<Direction, Material> _materials = new();

    public virtual Material GetMaterial(Direction face = Direction.PositiveY)
    {
        return _materials.TryGetValue(face, out var material) ? material : MaterialManager.Instance.GetFallbackMaterial();
    }

    public virtual Texture2D GetTexture(Direction face = Direction.PositiveY)
    {
        return (GetMaterial(face) as BaseMaterial3D).AlbedoTexture;
    }
}