using Bit.Bmage;

namespace Boilerplate.Tests.Infrastructure;

public static class TestImageFactory
{
    public static readonly BmageRgba32 Red = new(255, 0, 0);
    public static readonly BmageRgba32 Green = new(0, 128, 0);
    public static readonly BmageRgba32 Blue = new(0, 0, 255);
    public static readonly BmageRgba32 Teal = new(0, 128, 128);

    private static readonly BmageImageProcessor processor = new();

    public static byte[] Solid(BmageRgba32 color, int width, int height, BmageImageFormat format = BmageImageFormat.Png, BmageImageMetadata? metadata = null)
    {
        using var image = BmageImageBuffer.Create(width, height, color);
        image.Metadata = metadata;

        return processor.EncodeAsync(image, new BmageImageExportOptions { Format = format, Metadata = BmageMetadataPolicy.Preserve }).AsTask().GetAwaiter().GetResult().Data;
    }

    public static byte[] Asset(string name)
    {
        using var stream = typeof(TestImageFactory).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"{name} is not embedded in the test assembly.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return bytes.ToArray();
    }

    public static BmageImageInfo Identify(byte[] bytes)
    {
        return processor.Identify(bytes) ?? throw new InvalidOperationException("The bytes are not an image.");
    }

    public static async Task<BmageImageMetadata?> ReadMetadata(byte[] bytes)
    {
        using var image = await processor.DecodeAsync(bytes, new BmageDecodeOptions { AutoOrient = false });
        return image.Metadata;
    }
}
