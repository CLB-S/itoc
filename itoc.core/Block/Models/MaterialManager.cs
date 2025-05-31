using Godot;
using System.Collections.Concurrent;

namespace ITOC.Core.BlockModels;

/// <summary>
/// Settings for customizing material properties
/// </summary>
public class MaterialSettings
{
    // NOTE: This should be revised later when adding more blocks.
    // May ask modders to use Godot .tres files instead of this class.

    public Texture2D AlbedoTexture { get; set; } = null;
    public BaseMaterial3D.TransparencyEnum Transparency { get; set; } = BaseMaterial3D.TransparencyEnum.Disabled;
    public BaseMaterial3D.SpecularModeEnum SpecularMode { get; set; } = BaseMaterial3D.SpecularModeEnum.Disabled;
    public bool TextureRepeat { get; set; } = true;
    public BaseMaterial3D.TextureFilterEnum TextureFilter { get; set; } = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps;
    public Texture2D NormalMap { get; set; } = null;
    public float NormalScale { get; set; } = 1.0f;
    public Texture2D RoughnessTexture { get; set; } = null;
    public float Roughness { get; set; } = 1.0f;
    public Texture2D MetallicTexture { get; set; } = null;
    public float Metallic { get; set; } = 0.0f;

    public MaterialSettings() { }
    public MaterialSettings(string texturePath)
    {
        AlbedoTexture = ResourceLoader.Load(texturePath) as Texture2D;
    }

    // Generate a unique key for this settings configuration
    public string GenerateKey()
    {
        return $"{AlbedoTexture?.ResourcePath}_{(int)Transparency}_{(int)SpecularMode}_{TextureRepeat}_{(int)TextureFilter}_{NormalMap?.ResourcePath ?? "null"}_{NormalScale}_" +
               $"{RoughnessTexture?.ResourcePath ?? "null"}_{Roughness}_{MetallicTexture?.ResourcePath ?? "null"}_{Metallic}";
    }
}

public class MaterialManager
{
    private static MaterialManager _instance;

    // Dictionary to cache materials by texture path and settings key
    private readonly ConcurrentDictionary<string, Material> _materialCache = new();

    // Private constructor to enforce singleton pattern
    private MaterialManager()
    {
        var fallbackMaterial = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Disabled,
            TextureRepeat = true,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps,
            SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled,
            AlbedoColor = Colors.Purple
        };

        _materialCache.TryAdd("fallback", fallbackMaterial);
    }

    public static MaterialManager Instance => _instance ??= new MaterialManager();

    public Material GetFallbackMaterial() => _materialCache["fallback"];

    /// <summary>
    /// Gets a material for a texture. Creates a new one if it doesn't exist yet.
    /// </summary>
    /// <param name="settings">Material settings</param>
    /// <returns>The material</returns>
    public Material GetMaterial(MaterialSettings settings)
    {
        var cacheKey = settings.GenerateKey();

        _materialCache.GetOrAdd(cacheKey, _ =>
        {
            var material = CreateMaterial(settings);
            return material;
        });

        return _materialCache[cacheKey];
    }

    /// <summary>
    /// Creates a standard material with the given texture and settings
    /// </summary>
    private static StandardMaterial3D CreateMaterial(MaterialSettings settings)
    {
        var material = new StandardMaterial3D
        {
            Transparency = settings.Transparency,
            TextureRepeat = settings.TextureRepeat,
            TextureFilter = settings.TextureFilter,
            AlbedoTexture = settings.AlbedoTexture,
            SpecularMode = settings.SpecularMode,
        };

        // Apply normal mapping if provided
        if (settings.NormalMap != null)
        {
            material.NormalEnabled = true;
            material.NormalTexture = settings.NormalMap;
            material.NormalScale = settings.NormalScale;
        }

        // Apply roughness if provided
        if (settings.RoughnessTexture != null)
            material.RoughnessTexture = settings.RoughnessTexture;
        material.Roughness = settings.Roughness;

        // Apply metallic if provided
        if (settings.MetallicTexture != null)
            material.MetallicTexture = settings.MetallicTexture;
        material.Metallic = settings.Metallic;

        return material;
    }

    /// <summary>
    /// Clears the material cache
    /// </summary>
    public void ClearCache()
    {
        _materialCache.Clear();
    }
}
