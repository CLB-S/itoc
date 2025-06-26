using Godot;

namespace ITOC.Core.Utils;

public static class TextureBuffer
{
    public static (ImageTexture Buffer, int Width) Create(byte[] buffer)
    {
        if (buffer == null || buffer.Length == 0)
            throw new ArgumentException("Buffer cannot be null or empty.", nameof(buffer));

        var numPixels = buffer.Length / 4; // 4 bytes per pixel
        var width = Mathf.CeilToInt(Mathf.Sqrt(numPixels));
        var height = Mathf.CeilToInt((double)numPixels / width);

        var requiredPixels = width * height;
        if (requiredPixels > numPixels)
        {
            // If the buffer is not large enough, we need to pad it with transparent pixels
            var paddedBuffer = new byte[requiredPixels * 4];
            Buffer.BlockCopy(buffer, 0, paddedBuffer, 0, buffer.Length);
            buffer = paddedBuffer;
        }

        var image = Image.CreateFromData(width, height, false, Image.Format.Rf, buffer);
        var texture = ImageTexture.CreateFromImage(image);

        return (texture, width);
    }
}
