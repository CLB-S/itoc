using Godot;

namespace ITOC.Core.BlockModels;

public class CubeAllModel : CubeModelBase
{
    private Material _material;
    private int _textureId = 0;

    public CubeAllModel(Material material)
    {
        _material = material ?? MaterialManager.Instance.GetFallbackMaterial();
    }

    public CubeAllModel(string texturePath)
    {
        var materialSettings = new MaterialSettings(texturePath);
        _material = MaterialManager.Instance.GetMaterial(materialSettings);

        _textureId = TextureManager.Instance.GetTextureId(texturePath);
    }

    public override Material GetMaterial(Direction face = Direction.PositiveY)
    {
        return _material ?? MaterialManager.Instance.GetFallbackMaterial();
    }

    public override int GetTextureId(Direction face = Direction.PositiveY)
    {
        return _textureId;
    }
}