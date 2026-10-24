using ImageMagick;

namespace Boilerplate.Tests.Infrastructure;

public static class TestImageFactory
{
    public static MagickImage Solid(IMagickColor<ushort> color, uint width, uint height)
    {
        var pixels = new byte[width * height * 3];

        for (var i = 0; i < pixels.Length; i += 3)
        {
            pixels[i] = (byte)(color.R >> 8);
            pixels[i + 1] = (byte)(color.G >> 8);
            pixels[i + 2] = (byte)(color.B >> 8);
        }

        return new MagickImage(pixels, new PixelReadSettings(width, height, StorageType.Char, PixelMapping.RGB));
    }
}
