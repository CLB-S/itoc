namespace ITOC.Models;

public class CubeBottomTopModel : CubeDirectionalModel
{
    public CubeBottomTopModel(string texturePathSide, string texturePathBottom, string texturePathTop)
    {
        var materialSettings = new MaterialSettings(texturePathSide);
        _materials[Direction.PositiveX] = MaterialManager.Instance.GetMaterial(materialSettings);
        _materials[Direction.NegativeX] = MaterialManager.Instance.GetMaterial(materialSettings);
        _materials[Direction.PositiveZ] = MaterialManager.Instance.GetMaterial(materialSettings);
        _materials[Direction.NegativeZ] = MaterialManager.Instance.GetMaterial(materialSettings);

        materialSettings = new MaterialSettings(texturePathBottom);
        _materials[Direction.NegativeY] = MaterialManager.Instance.GetMaterial(materialSettings);

        materialSettings = new MaterialSettings(texturePathTop);
        _materials[Direction.PositiveY] = MaterialManager.Instance.GetMaterial(materialSettings);
    }
}