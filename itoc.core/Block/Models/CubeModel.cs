namespace ITOC.Core.BlockModels;

public class CubeModel : CubeModelBase
{
    public CubeModel(
        string texturePathPY,
        string texturePathNY,
        string texturePathPX,
        string texturePathNX,
        string texturePathPZ,
        string texturePathNZ
    )
        : base()
    {
        var materialSettings = new MaterialSettings(texturePathPY);
        _materials[Direction.PositiveY] = MaterialManager.Instance.GetMaterial(materialSettings);

        materialSettings = new MaterialSettings(texturePathNY);
        _materials[Direction.NegativeY] = MaterialManager.Instance.GetMaterial(materialSettings);

        materialSettings = new MaterialSettings(texturePathPX);
        _materials[Direction.PositiveX] = MaterialManager.Instance.GetMaterial(materialSettings);

        materialSettings = new MaterialSettings(texturePathNX);
        _materials[Direction.NegativeX] = MaterialManager.Instance.GetMaterial(materialSettings);

        materialSettings = new MaterialSettings(texturePathPZ);
        _materials[Direction.PositiveZ] = MaterialManager.Instance.GetMaterial(materialSettings);

        materialSettings = new MaterialSettings(texturePathNZ);
        _materials[Direction.NegativeZ] = MaterialManager.Instance.GetMaterial(materialSettings);

        _textureIds[Direction.PositiveY] = TextureManager.Instance.GetTextureId(texturePathPY);
        _textureIds[Direction.NegativeY] = TextureManager.Instance.GetTextureId(texturePathNY);
        _textureIds[Direction.PositiveX] = TextureManager.Instance.GetTextureId(texturePathPX);
        _textureIds[Direction.NegativeX] = TextureManager.Instance.GetTextureId(texturePathNX);
        _textureIds[Direction.PositiveZ] = TextureManager.Instance.GetTextureId(texturePathPZ);
        _textureIds[Direction.NegativeZ] = TextureManager.Instance.GetTextureId(texturePathNZ);
    }
}
