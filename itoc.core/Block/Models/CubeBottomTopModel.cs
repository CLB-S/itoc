namespace ITOC.Core.BlockModels;

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

        _textureIds[Direction.PositiveX] = TextureManager.Instance.GetTextureId(texturePathSide);
        _textureIds[Direction.NegativeX] = TextureManager.Instance.GetTextureId(texturePathSide);
        _textureIds[Direction.PositiveZ] = TextureManager.Instance.GetTextureId(texturePathSide);
        _textureIds[Direction.NegativeZ] = TextureManager.Instance.GetTextureId(texturePathSide);
        _textureIds[Direction.NegativeY] = TextureManager.Instance.GetTextureId(texturePathBottom);
        _textureIds[Direction.PositiveY] = TextureManager.Instance.GetTextureId(texturePathTop);
    }
}