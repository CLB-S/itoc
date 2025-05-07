using Godot;
using System;
using System.Threading.Tasks;

public static class HeightMapUtils
{
    public static double[,] ConstructHeightMap(int resolutionX, int resolutionY, Rect2I rect, Func<double, double, double> getHeight,
        bool parallel = false, int upscaleLevel = 3)
    {
        if (upscaleLevel < 0)
            throw new ArgumentException("Upscale level must be non-negative.");

        // Calculate low resolution dimensions
        var lowResX = resolutionX >> upscaleLevel;
        var lowResY = resolutionY >> upscaleLevel;

        lowResX = Math.Max(lowResX, 1);
        lowResY = Math.Max(lowResY, 1);

        // Create low resolution height map
        var lowResMap = ConstructHeightMapOriginal(lowResX, lowResY, rect, getHeight, parallel);

        // Upscale to full resolution using bilinear interpolation
        return UpscaleHeightMap(lowResMap, resolutionX, resolutionY);
    }

    private static double[,] ConstructHeightMapOriginal(int resolutionX, int resolutionY, Rect2I rect, Func<double, double, double> getHeight,
        bool parallel = false)
    {
        var heightMap = new double[resolutionX, resolutionY];
        var stepX = resolutionX > 1 ? (double)(rect.Size.X - 1) / (resolutionX - 1) : 0;
        var stepY = resolutionY > 1 ? (double)(rect.Size.Y - 1) / (resolutionY - 1) : 0;

        if (parallel)
            Parallel.For(0, resolutionX, i =>
            {
                var x = resolutionX > 1 ? rect.Position.X + 0.5 + i * stepX : rect.Position.X + rect.Size.X / 2;
                for (var j = 0; j < resolutionY; j++)
                {
                    var y = resolutionY > 1 ? rect.Position.Y + 0.5 + j * stepY : rect.Position.Y + rect.Size.Y / 2;
                    heightMap[i, j] = getHeight(x, y);
                }
            });
        else
            for (var i = 0; i < resolutionX; i++)
            {
                var x = resolutionX > 1 ? rect.Position.X + i * stepX : rect.Position.X + rect.Size.X / 2;
                for (var j = 0; j < resolutionY; j++)
                {
                    var y = resolutionY > 1 ? rect.Position.Y + j * stepY : rect.Position.Y + rect.Size.Y / 2;
                    heightMap[i, j] = getHeight(x, y);
                }
            }

        return heightMap;
    }

    private static double[,] UpscaleHeightMap(double[,] lowResMap, int targetX, int targetY)
    {
        var lowResX = lowResMap.GetLength(0);
        var lowResY = lowResMap.GetLength(1);

        var highResMap = new double[targetX, targetY];

        // Handle case where lowRes is 1x1
        if (lowResX == 1 && lowResY == 1)
        {
            var val = lowResMap[0, 0];
            for (var x = 0; x < targetX; x++)
                for (var y = 0; y < targetY; y++)
                    highResMap[x, y] = val;
            return highResMap;
        }

        // Handle case where one dimension is 1
        if (lowResX == 1)
        {
            for (var y = 0; y < targetY; y++)
            {
                var t = (double)y / (targetY - 1) * (lowResY - 1);
                var y0 = (int)Math.Floor(t);
                var y1 = Math.Min(y0 + 1, lowResY - 1);
                var ty = t - y0;

                var val = Lerp(lowResMap[0, y0], lowResMap[0, y1], ty);
                for (var x = 0; x < targetX; x++)
                    highResMap[x, y] = val;
            }

            return highResMap;
        }

        if (lowResY == 1)
        {
            for (var x = 0; x < targetX; x++)
            {
                var t = (double)x / (targetX - 1) * (lowResX - 1);
                var x0 = (int)Math.Floor(t);
                var x1 = Math.Min(x0 + 1, lowResX - 1);
                var tx = t - x0;

                var val = Lerp(lowResMap[x0, 0], lowResMap[x1, 0], tx);
                for (var y = 0; y < targetY; y++)
                    highResMap[x, y] = val;
            }

            return highResMap;
        }

        // Bilinear interpolation for 2D case
        for (var x = 0; x < targetX; x++)
        {
            var tx = (double)x / (targetX - 1) * (lowResX - 1);
            var x0 = (int)Math.Floor(tx);
            var x1 = Math.Min(x0 + 1, lowResX - 1);
            var fx = tx - x0;

            for (var y = 0; y < targetY; y++)
            {
                var ty = (double)y / (targetY - 1) * (lowResY - 1);
                var y0 = (int)Math.Floor(ty);
                var y1 = Math.Min(y0 + 1, lowResY - 1);
                var fy = ty - y0;

                // Bilinear interpolation
                var v00 = lowResMap[x0, y0];
                var v10 = lowResMap[x1, y0];
                var v01 = lowResMap[x0, y1];
                var v11 = lowResMap[x1, y1];

                var v0 = Lerp(v00, v10, fx);
                var v1 = Lerp(v01, v11, fx);
                highResMap[x, y] = Lerp(v0, v1, fy);
            }
        }

        return highResMap;
    }

    public static double[,] ConstructChunkHeightMap(Rect2I chunkRect, Func<double, double, double> getHeight, int upscaleLevel = 3)
    {
        return ConstructHeightMap(chunkRect.Size.X, chunkRect.Size.Y, chunkRect, getHeight, upscaleLevel: upscaleLevel);
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }
}