using Godot;

namespace ITOC.Core.BlockModels;

public class CubeAllModel : CubeModelBase
{
    private Material _material;

    public CubeAllModel(Material material)
    {
        _material = material ?? MaterialManager.Instance.GetFallbackMaterial();
    }

    public CubeAllModel(MaterialSettings materialSettings)
    {
        _material = MaterialManager.Instance.GetMaterial(materialSettings);
    }


    public CubeAllModel(string texturePath)
    {
        var materialSettings = new MaterialSettings(texturePath);
        _material = MaterialManager.Instance.GetMaterial(materialSettings);
    }

    public override Material GetMaterial(Direction face = Direction.PositiveY)
    {
        return _material ?? MaterialManager.Instance.GetFallbackMaterial();
    }
}