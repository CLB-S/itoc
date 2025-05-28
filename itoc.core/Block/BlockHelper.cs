using Godot;

namespace ITOC.Core;

public static class BlockHelper
{
    public static Material GetMaterialByTexture(Texture2D texture)
    {
        return new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Disabled,
            TextureRepeat = true,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps,
            AlbedoTexture = texture
            // CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
    }

    public static Material GetMaterialByTexture(string texturePath)
    {
        var texture = ResourceLoader.Load(texturePath) as Texture2D;
        return new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Disabled,
            TextureRepeat = true,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps,
            AlbedoTexture = texture
            // CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
    }
}