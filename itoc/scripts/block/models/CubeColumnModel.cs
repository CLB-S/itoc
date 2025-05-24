namespace ITOC.Models;

public class CubeColumnModel : CubeDirectionalModel
{
    public CubeColumnModel(string texturePathSide, string texturePathEnd)
    {
        var materialSettings = new MaterialSettings(texturePathSide);
        _materials[Direction.PositiveX] = MaterialManager.Instance.GetMaterial(materialSettings);
        _materials[Direction.NegativeX] = MaterialManager.Instance.GetMaterial(materialSettings);
        _materials[Direction.PositiveZ] = MaterialManager.Instance.GetMaterial(materialSettings);
        _materials[Direction.NegativeZ] = MaterialManager.Instance.GetMaterial(materialSettings);

        materialSettings = new MaterialSettings(texturePathEnd);
        _materials[Direction.PositiveY] = MaterialManager.Instance.GetMaterial(materialSettings);
        _materials[Direction.NegativeY] = MaterialManager.Instance.GetMaterial(materialSettings);
    }
}