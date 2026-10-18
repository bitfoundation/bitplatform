using Bit.Bmage.Codecs.Png;

namespace Bit.Bmage.Tests.E2E.Infrastructure;

/// <summary>The upload every test uses: a deterministic gradient PNG, built with this library's own encoder.</summary>
public static class SampleImage
{
    public const int Width = 800;

    public const int Height = 600;

    /// <summary>The encoded PNG.</summary>
    public static byte[] Png { get; } = Create(Width, Height);

    /// <summary>Writes the sample image to a temporary file for upload, returning its path. The caller deletes it.</summary>
    public static string WriteToTempFile(string name = "sample.png")
    {
        var path = Path.Combine(Path.GetTempPath(), $"bmage-{Guid.NewGuid():N}-{name}");
        File.WriteAllBytes(path, Png);
        return path;
    }

    // Red rises left to right and green top to bottom, so any pixel says where it came from; the blue
    // checkerboard gives a resampler something to blur and an encoder something to spend bytes on.
    private static byte[] Create(int width, int height)
    {
        using var image = ImageBuffer.Create(width, height, clear: false);
        for (var y = 0; y < height; y++)
        {
            var row = image.GetRow(y);
            for (var x = 0; x < width; x++)
            {
                row[x] = new Rgba32(
                    (byte)(x * 255 / Math.Max(1, width - 1)),
                    (byte)(y * 255 / Math.Max(1, height - 1)),
                    (byte)(((x >> 5) + (y >> 5)) % 2 == 0 ? 40 : 220));
            }
        }
        return PngEncoder.Encode(image).Data;
    }
}
