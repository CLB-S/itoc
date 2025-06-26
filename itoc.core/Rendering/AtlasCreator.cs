using Godot;

namespace ITOC.Core.Rendering.Atlas;

public class AtlasCreator
{
    private readonly List<Image> _textureImages = new();

    private readonly int _imageWidth;
    private readonly int _imageHeight;
    private readonly Image.Format _imageFormat;

    public AtlasCreator(int imageWidth, int imageHeight, Image.Format format = Image.Format.Rgba8)
    {
        _imageWidth = imageWidth;
        _imageHeight = imageHeight;
        _imageFormat = format;

        GD.Print(
            $"AtlasCreator initialized with size: {_imageWidth}x{_imageHeight}, format: {_imageFormat}"
        );
    }

    public int AddImage(Image image)
    {
        if (image.GetWidth() != _imageWidth || image.GetHeight() != _imageHeight)
            throw new ArgumentException(
                $"All images must have the same size: {_imageWidth}x{_imageHeight}"
            );

        _textureImages.Add(image);
        return _textureImages.Count - 1;
    }

    public IEnumerable<int> AddImages(IEnumerable<Image> images)
    {
        var indices = new List<int>();
        foreach (var image in images)
            indices.Add(AddImage(image));
        return indices;
    }

    public (ImageTexture Atlas, int TextureCountX, int TextureCountY) CreateAtlas()
    {
        var imageCount = _textureImages.Count;
        if (imageCount == 0)
            throw new InvalidOperationException("No images to create an atlas from.");

        var imageCountX = Mathf.CeilToInt(Mathf.Sqrt(imageCount));
        var imageCountY = Mathf.CeilToInt((double)imageCount / imageCountX);
        var atlasWidth = imageCountX * _imageWidth;
        var atlasHeight = imageCountY * _imageHeight;

        var atlasImage = Image.CreateEmpty(atlasWidth, atlasHeight, false, _imageFormat);

        for (var i = 0; i < imageCount; i++)
        {
            var image = _textureImages[i];
            var x = (i % imageCountX) * _imageWidth;
            var y = (i / imageCountX) * _imageHeight;

            atlasImage.BlitRect(
                image,
                new Rect2I(0, 0, _imageWidth, _imageHeight),
                new Vector2I(x, y)
            );
        }

        atlasImage.GenerateMipmaps();

        var texture = ImageTexture.CreateFromImage(atlasImage);

        GD.Print(
            $"Atlas created with size: {atlasWidth}x{atlasHeight}, format: {_imageFormat}, "
                + $"images: {imageCount}, countX: {imageCountX}, countY: {imageCountY}"
        );

        return (texture, imageCountX, imageCountY);
    }
}
