using System.Buffers.Binary;

namespace Bit.Bmage;

/// <summary>Managed BMP encoder writing 24-bit (opaque) or 32-bit BGRA (with alpha, BITMAPV4HEADER) images.</summary>
public sealed class BmageBmpEncoder : IBmageImageEncoder
{
    private static readonly BmageImageFormat[] SupportedFormats = [BmageImageFormat.Bmp];
    public IReadOnlyCollection<BmageImageFormat> Formats => SupportedFormats;

    public ValueTask<BmageEncodedImage> EncodeAsync(BmageImageBuffer image, BmageImageExportOptions options, CancellationToken cancellationToken = default)
        => new(Encode(image, cancellationToken));

    public static BmageEncodedImage Encode(BmageImageBuffer image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        var alpha = image.HasTransparency();
        int w = image.Width, h = image.Height;
        var bpp = alpha ? 32 : 24;
        var stride = ((w * bpp + 31) / 32) * 4;
        var headerSize = alpha ? 108 : 40;
        var pixelOffset = 14 + headerSize;
        var total = pixelOffset + stride * h;
        var buf = new byte[total];
        buf[0] = (byte)'B'; buf[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(2), total);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(10), pixelOffset);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(14), headerSize);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(18), w);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(22), h);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(26), 1);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(28), (short)bpp);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(30), alpha ? 3 : 0);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(34), stride * h);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(38), 2835);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(42), 2835);
        if (alpha)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(54), 0x00FF0000);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(58), 0x0000FF00);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(62), 0x000000FF);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(66), 0xFF000000);
            buf[70] = (byte)'B'; buf[71] = (byte)'G'; buf[72] = (byte)'R'; buf[73] = (byte)'s';
        }
        for (var y = 0; y < h; y++)
        {
            if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            var row = image.GetRow(h - 1 - y);
            var o = pixelOffset + y * stride;
            if (alpha)
                for (var x = 0; x < w; x++) { var p = row[x]; buf[o + x * 4] = p.B; buf[o + x * 4 + 1] = p.G; buf[o + x * 4 + 2] = p.R; buf[o + x * 4 + 3] = p.A; }
            else
                for (var x = 0; x < w; x++) { var p = row[x]; buf[o + x * 3] = p.B; buf[o + x * 3 + 1] = p.G; buf[o + x * 3 + 2] = p.R; }
        }
        return new BmageEncodedImage(buf, BmageImageFormat.Bmp, w, h);
    }
}
