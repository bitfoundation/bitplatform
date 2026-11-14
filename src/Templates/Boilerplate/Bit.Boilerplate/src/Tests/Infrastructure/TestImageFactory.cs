using Bit.Bmage;
using Bit.Bmage.Codecs;
using Bit.Bmage.Metadata;
using Bit.Bmage.Pipeline;

namespace Boilerplate.Tests.Infrastructure;

public static class TestImageFactory
{
    public static readonly Rgba32 Red = new(255, 0, 0);
    public static readonly Rgba32 Green = new(0, 128, 0);
    public static readonly Rgba32 Blue = new(0, 0, 255);
    public static readonly Rgba32 Teal = new(0, 128, 128);

    private static readonly ImageProcessor processor = new();

    public static byte[] Solid(Rgba32 color, int width, int height, ImageFormat format = ImageFormat.Png, ImageMetadata? metadata = null)
    {
        using var image = ImageBuffer.Create(width, height, color);
        image.Metadata = metadata;

        return processor.EncodeAsync(image, new ImageExportOptions { Format = format, Metadata = MetadataPolicy.Preserve }).AsTask().GetAwaiter().GetResult().Data;
    }

    public static byte[] Asset(string name)
    {
        using var stream = typeof(TestImageFactory).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"{name} is not embedded in the test assembly.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return bytes.ToArray();
    }

    public static ImageInfo Identify(byte[] bytes)
    {
        return processor.Identify(bytes) ?? throw new InvalidOperationException("The bytes are not an image.");
    }

    public static async Task<ImageMetadata?> ReadMetadata(byte[] bytes)
    {
        using var image = await processor.DecodeAsync(bytes, new DecodeOptions { AutoOrient = false });
        return image.Metadata;
    }
}
