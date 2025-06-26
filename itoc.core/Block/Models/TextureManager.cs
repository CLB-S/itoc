using Godot;

namespace ITOC.Core.BlockModels;

public sealed class TextureManager : IFreezable
{
    private static TextureManager _instance;
    public static TextureManager Instance => _instance ??= new TextureManager();

    private bool _isFrozen = false;
    public bool IsFrozen => _isFrozen;

    private readonly Dictionary<string, (int, Image)> _textureCache =
        new Dictionary<string, (int, Image)>();

    private TextureManager() { }

    public int GetTextureId(string textureImagePath)
    {
        if (_isFrozen)
            throw new InvalidOperationException(
                "Cannot get texture ID when TextureManager is frozen."
            );

        // Check if the texture has already been processed
        if (_textureCache.TryGetValue(textureImagePath, out var idAndImage))
            return idAndImage.Item1;

        var image = ResourceLoader.Load<Texture2D>(textureImagePath).GetImage();

        ArgumentNullException.ThrowIfNull(image, $"Texture image not found: {textureImagePath}");

        _textureCache[textureImagePath] = (_textureCache.Count, image);

        return _textureCache.Count - 1;
    }

    public void BuildTextureArray()
    {
        if (_isFrozen)
            throw new InvalidOperationException(
                "Cannot build texture array when TextureManager is frozen."
            );

        var imageArray = new Godot.Collections.Array<Image>();

        foreach (var (_, texture) in _textureCache.Values)
            imageArray.Add(texture);

        var textureArray = new Texture2DArray();
        textureArray.CreateFromImages(imageArray);
        RenderingServer.GlobalShaderParameterSet("block_textures", textureArray);

        Freeze();
    }

    public void Freeze() => _isFrozen = true;
}
