using System.Buffers.Binary;

namespace Boilerplate.WorkbenchTests.Infrastructure;

/// <summary>The size and color type a PNG's header declares.</summary>
public sealed record PngInfo(int Width, int Height, byte ColorType)
{
    /// <summary>Whether the image carries an alpha channel, which the App Store refuses in screenshots.</summary>
    public bool HasAlpha => ColorType is 4 or 6;

    public static PngInfo Read(byte[] png)
    {
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        Assert.IsGreaterThan(26, png.Length, "Too short for a PNG.");
        Assert.IsTrue(png.AsSpan(0, 8).SequenceEqual(signature), "Not a PNG.");

        return new(BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)), png[25]);
    }
}
