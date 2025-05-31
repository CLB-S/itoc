using Godot;
using ITOC.Core.Rendering.Atlas;

namespace ITOC.Core.BlockModels;

public sealed class TextureManager : IFreezable
{
    private static TextureManager _instance;
    public static TextureManager Instance => _instance ??= new TextureManager();

    private bool _isFrozen = false;
    public bool IsFrozen => _isFrozen;

    private AtlasCreator _atlasCreator;
    private Dictionary<string, int> _textureCache = new Dictionary<string, int>();

    private TextureManager()
    {
    }

    public int GetTextureId(string textureImagePath)
    {
        if (_isFrozen)
            throw new InvalidOperationException("Cannot get texture ID when TextureManager is frozen.");

        // Check if the texture has already been processed
        if (_textureCache.TryGetValue(textureImagePath, out var id))
            return id;

        var image = ResourceLoader.Load<Texture2D>(textureImagePath).GetImage();

        ArgumentNullException.ThrowIfNull(image, $"Texture image not found: {textureImagePath}");

        if (_atlasCreator == null)
        {
            var size = image.GetSize();
            _atlasCreator = new AtlasCreator(size.X, size.Y, image.GetFormat());
        }

        // Store the texture ID in the cache
        id = _atlasCreator.AddImage(image);
        _textureCache[textureImagePath] = id;

        return id;
    }

    public void GenerateAtlas()
    {
        if (_isFrozen)
            throw new InvalidOperationException("Cannot generate atlas when TextureManager is frozen.");

        var (atlas, textureCountX, textureCountY) = _atlasCreator.CreateAtlas();

        RenderingServer.GlobalShaderParameterSet("block_atlas", atlas);
        RenderingServer.GlobalShaderParameterSet("block_atlas_count", new Vector2(textureCountX, textureCountY));

        Freeze();
    }

    public void Freeze()
    {
        _isFrozen = true;
    }
}
