using System.Buffers.Binary;
using System.IO.Compression;

namespace Bit.Bink;

/// <summary>
/// A dependency-free PNG encoder and decoder covering what Bink needs: 8-bit RGBA/RGB/grayscale,
/// non-interlaced images. Output is deterministic, which makes exported images comparable in tests.
/// </summary>
public static class BinkPngCodec
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Encodes straight-alpha RGBA pixels as a PNG.</summary>
    /// <param name="rgba">Pixels, 4 bytes per pixel, row major.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="level">Compression level; <see cref="CompressionLevel.SmallestSize"/> gives the smallest files.</param>
    /// <param name="pixelsPerMeter">Optional physical resolution written as a pHYs chunk (0 omits it). 11811 is roughly 300 DPI.</param>
    public static byte[] Encode(ReadOnlySpan<byte> rgba, int width, int height, CompressionLevel level = CompressionLevel.Optimal, int pixelsPerMeter = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        long expected = (long)width * height * 4;
        if (rgba.Length < expected) throw new ArgumentException($"Expected {expected} bytes of pixel data but received {rgba.Length}.", nameof(rgba));

        using var output = new MemoryStream(Math.Max(1024, width * height / 2));
        output.Write(Signature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8;  // bit depth
        header[9] = 6;  // color type: RGBA
        header[10] = 0; // deflate
        header[11] = 0; // adaptive filtering
        header[12] = 0; // no interlace
        WriteChunk(output, "IHDR", header);

        if (pixelsPerMeter > 0)
        {
            Span<byte> phys = stackalloc byte[9];
            BinaryPrimitives.WriteInt32BigEndian(phys, pixelsPerMeter);
            BinaryPrimitives.WriteInt32BigEndian(phys[4..], pixelsPerMeter);
            phys[8] = 1; // unit: metres
            WriteChunk(output, "pHYs", phys);
        }

        byte[] compressed = Compress(rgba, width, height, level);
        WriteChunk(output, "IDAT", compressed);
        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static byte[] Compress(ReadOnlySpan<byte> rgba, int width, int height, CompressionLevel level)
    {
        int stride = width * 4;
        byte[] scanline = new byte[stride + 1];
        byte[] filtered = new byte[stride];
        byte[] previous = new byte[stride];
        byte[] current = new byte[stride];

        using var buffer = new MemoryStream(Math.Max(1024, width * height / 2));
        using (var deflate = new ZLibStream(buffer, level, leaveOpen: true))
        {
            for (int y = 0; y < height; y++)
            {
                rgba.Slice(y * stride, stride).CopyTo(current);
                scanline[0] = ChooseFilter(current, previous, filtered, stride);
                filtered.CopyTo(scanline.AsSpan(1));
                deflate.Write(scanline, 0, scanline.Length);
                (previous, current) = (current, previous);
            }
        }
        return buffer.ToArray();
    }

    /// <summary>
    /// Picks a per-scanline filter with the minimum-sum-of-absolute-differences heuristic from the PNG
    /// specification, which typically halves the encoded size of ink drawings. The scores are computed first and
    /// only the winning filter is applied, so no per-scanline buffers are allocated.
    /// </summary>
    private static byte ChooseFilter(byte[] current, byte[] previous, byte[] output, int stride)
    {
        const int bpp = 4;
        long none = 0, sub = 0, up = 0, average = 0, paeth = 0;
        for (int i = 0; i < stride; i++)
        {
            byte value = current[i];
            byte left = i >= bpp ? current[i - bpp] : (byte)0;
            byte above = previous[i];
            byte aboveLeft = i >= bpp ? previous[i - bpp] : (byte)0;
            none += Magnitude(value);
            sub += Magnitude((byte)(value - left));
            up += Magnitude((byte)(value - above));
            average += Magnitude((byte)(value - (byte)((left + above) >> 1)));
            paeth += Magnitude((byte)(value - Paeth(left, above, aboveLeft)));
        }

        byte filter = 0;
        long best = none;
        if (sub < best) { best = sub; filter = 1; }
        if (up < best) { best = up; filter = 2; }
        if (average < best) { best = average; filter = 3; }
        if (paeth < best) { filter = 4; }

        for (int i = 0; i < stride; i++)
        {
            byte value = current[i];
            byte left = i >= bpp ? current[i - bpp] : (byte)0;
            byte above = previous[i];
            byte aboveLeft = i >= bpp ? previous[i - bpp] : (byte)0;
            output[i] = filter switch
            {
                1 => (byte)(value - left),
                2 => (byte)(value - above),
                3 => (byte)(value - (byte)((left + above) >> 1)),
                4 => (byte)(value - Paeth(left, above, aboveLeft)),
                _ => value,
            };
        }
        return filter;
    }

    /// <summary>
    /// The filter heuristic treats each byte as a signed value and sums the magnitudes. Computing this with
    /// <c>Math.Abs((sbyte)value)</c> overflows on -128, so the magnitude is derived directly.
    /// </summary>
    private static int Magnitude(byte value) => value < 128 ? value : 256 - value;

    private static byte Paeth(byte a, byte b, byte c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        return pb <= pc ? b : c;
    }

    private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);

        Span<byte> typeBytes = stackalloc byte[4];
        for (int i = 0; i < 4; i++) typeBytes[i] = (byte)type[i];
        stream.Write(typeBytes);
        stream.Write(data);

        uint crc = BinkCrc32.Compute(typeBytes, data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }

    /// <summary>A decoded image: straight-alpha RGBA pixels and their dimensions.</summary>
    /// <param name="Width">Width in pixels.</param>
    /// <param name="Height">Height in pixels.</param>
    /// <param name="Rgba">Pixels, 4 bytes each, row major.</param>
    public readonly record struct DecodedImage(int Width, int Height, byte[] Rgba);

    /// <summary>True when the bytes start with the PNG signature.</summary>
    public static bool IsPng(ReadOnlySpan<byte> data) => data.Length >= 8 && data[..8].SequenceEqual(Signature);

    /// <summary>
    /// Decodes an 8-bit non-interlaced PNG (grayscale, RGB, palette or RGBA, with or without an alpha channel).
    /// </summary>
    /// <param name="data">The PNG bytes.</param>
    /// <param name="maxPixels">Rejects images larger than this many pixels, so a malicious file cannot exhaust memory.</param>
    /// <exception cref="FormatException">The data is not a supported PNG.</exception>
    public static DecodedImage Decode(ReadOnlySpan<byte> data, long maxPixels = 64_000_000)
    {
        if (!IsPng(data)) throw new FormatException("Not a PNG image.");
        int offset = 8;
        int width = 0, height = 0, bitDepth = 0, colorType = 0, interlace = 0;
        byte[]? palette = null;
        byte[]? paletteAlpha = null;
        using var idat = new MemoryStream();
        bool sawHeader = false;

        while (offset + 8 <= data.Length)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(data[offset..]);
            // Computed in 64 bits: a hostile length near int.MaxValue would wrap in 32-bit arithmetic and slip
            // past this guard, then read out of bounds.
            if (length < 0 || (long)offset + 12 + length > data.Length) throw new FormatException("PNG chunk length is invalid.");
            ReadOnlySpan<byte> type = data.Slice(offset + 4, 4);
            ReadOnlySpan<byte> chunk = data.Slice(offset + 8, length);
            offset += 12 + length;

            if (type.SequenceEqual("IHDR"u8))
            {
                if (length < 13) throw new FormatException("PNG header chunk is truncated.");
                width = BinaryPrimitives.ReadInt32BigEndian(chunk);
                height = BinaryPrimitives.ReadInt32BigEndian(chunk[4..]);
                bitDepth = chunk[8];
                colorType = chunk[9];
                interlace = chunk[12];
                if (width <= 0 || height <= 0) throw new FormatException("PNG dimensions are invalid.");
                if ((long)width * height > maxPixels) throw new FormatException($"The PNG is {width} by {height} pixels, which exceeds the limit of {maxPixels} pixels.");
                if (bitDepth != 8) throw new FormatException($"Only 8-bit PNGs are supported (this one is {bitDepth}-bit).");
                if (interlace != 0) throw new FormatException("Interlaced PNGs are not supported.");
                sawHeader = true;
            }
            else if (type.SequenceEqual("PLTE"u8)) palette = chunk.ToArray();
            else if (type.SequenceEqual("tRNS"u8)) paletteAlpha = chunk.ToArray();
            else if (type.SequenceEqual("IDAT"u8)) idat.Write(chunk);
            else if (type.SequenceEqual("IEND"u8)) break;
        }

        if (!sawHeader) throw new FormatException("The PNG has no header chunk.");
        int channels = colorType switch
        {
            0 => 1, // grayscale
            2 => 3, // RGB
            3 => 1, // palette index
            4 => 2, // grayscale + alpha
            6 => 4, // RGBA
            _ => throw new FormatException($"Unsupported PNG color type {colorType}."),
        };
        if (colorType == 3 && palette is null) throw new FormatException("The palette PNG has no palette chunk.");

        // The pixel limit is the caller's choice, so the buffer sizes are checked in 64 bits: with a generous limit
        // they would otherwise wrap and allocate a buffer that does not match the image.
        if ((long)width * height * 4 > Array.MaxLength || ((long)width * channels + 1) * height > Array.MaxLength)
            throw new FormatException($"The PNG is {width} by {height} pixels, which is too large to decode.");
        int stride = width * channels;
        byte[] raw = new byte[(stride + 1) * height];
        idat.Position = 0;
        try
        {
            using var inflate = new ZLibStream(idat, CompressionMode.Decompress);
            int read = 0;
            while (read < raw.Length)
            {
                int n = inflate.Read(raw, read, raw.Length - read);
                if (n <= 0) break;
                read += n;
            }
            if (read < raw.Length) throw new FormatException("PNG pixel data is truncated.");
        }
        catch (InvalidDataException ex)
        {
            // Corrupt compressed data must look like any other malformed image to callers.
            throw new FormatException("PNG pixel data is not valid compressed data.", ex);
        }

        byte[] pixels = new byte[width * height * 4];
        byte[] previous = new byte[stride];
        byte[] current = new byte[stride];
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * (stride + 1);
            byte filter = raw[rowStart];
            raw.AsSpan(rowStart + 1, stride).CopyTo(current);
            Unfilter(filter, current, previous, channels);
            ExpandRow(current, pixels.AsSpan(y * width * 4), width, colorType, palette, paletteAlpha);
            (previous, current) = (current, previous);
        }
        return new DecodedImage(width, height, pixels);
    }

    private static void Unfilter(byte filter, Span<byte> current, ReadOnlySpan<byte> previous, int bpp)
    {
        switch (filter)
        {
            case 0: break;
            case 1:
                for (int i = bpp; i < current.Length; i++) current[i] = (byte)(current[i] + current[i - bpp]);
                break;
            case 2:
                for (int i = 0; i < current.Length; i++) current[i] = (byte)(current[i] + previous[i]);
                break;
            case 3:
                for (int i = 0; i < current.Length; i++)
                {
                    byte left = i >= bpp ? current[i - bpp] : (byte)0;
                    current[i] = (byte)(current[i] + ((left + previous[i]) >> 1));
                }
                break;
            case 4:
                for (int i = 0; i < current.Length; i++)
                {
                    byte left = i >= bpp ? current[i - bpp] : (byte)0;
                    byte upLeft = i >= bpp ? previous[i - bpp] : (byte)0;
                    current[i] = (byte)(current[i] + Paeth(left, previous[i], upLeft));
                }
                break;
            default:
                throw new FormatException($"Unsupported PNG filter type {filter}.");
        }
    }

    private static void ExpandRow(ReadOnlySpan<byte> source, Span<byte> destination, int width, int colorType, byte[]? palette, byte[]? paletteAlpha)
    {
        for (int x = 0; x < width; x++)
        {
            int d = x * 4;
            switch (colorType)
            {
                case 0:
                {
                    byte v = source[x];
                    destination[d] = v; destination[d + 1] = v; destination[d + 2] = v; destination[d + 3] = 255;
                    break;
                }
                case 2:
                {
                    int s = x * 3;
                    destination[d] = source[s]; destination[d + 1] = source[s + 1]; destination[d + 2] = source[s + 2]; destination[d + 3] = 255;
                    break;
                }
                case 3:
                {
                    int index = source[x];
                    int p = index * 3;
                    if (palette is null || p + 2 >= palette.Length) throw new FormatException("A PNG palette index is out of range.");
                    destination[d] = palette[p]; destination[d + 1] = palette[p + 1]; destination[d + 2] = palette[p + 2];
                    destination[d + 3] = paletteAlpha is not null && index < paletteAlpha.Length ? paletteAlpha[index] : (byte)255;
                    break;
                }
                case 4:
                {
                    int s = x * 2;
                    byte v = source[s];
                    destination[d] = v; destination[d + 1] = v; destination[d + 2] = v; destination[d + 3] = source[s + 1];
                    break;
                }
                default:
                {
                    int s = x * 4;
                    destination[d] = source[s]; destination[d + 1] = source[s + 1]; destination[d + 2] = source[s + 2]; destination[d + 3] = source[s + 3];
                    break;
                }
            }
        }
    }
}

/// <summary>CRC-32 as used by PNG chunks.</summary>
internal static class BinkCrc32
{
    private static readonly uint[] Table = CreateTable();

    private static uint[] CreateTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }

    public static uint Compute(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (byte value in a) crc = Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
        foreach (byte value in b) crc = Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }
}
