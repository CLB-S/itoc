using Godot;
using System;

public static class BlockHelper
{
    public static Material GetMaterial(string texturePath)
    {
        var texture = ResourceLoader.Load(texturePath) as Texture2D;
        return new StandardMaterial3D()
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Disabled,
            TextureRepeat = true,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlbedoTexture = texture,
            // CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
    }
}