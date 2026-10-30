using System.Runtime.InteropServices;

namespace Bit.Bmage.Codecs.WebP;

/// <summary>
/// Decoder for the WebP lossless bitstream (RFC 9649 section 3): prefix-coded ARGB with LZ77 back references, a colour
/// cache, spatially varying prefix codes and the predictor, colour, subtract-green and colour-indexing transforms.
/// </summary>
/// <remarks>
/// Pixels are produced as ARGB words (alpha in the top byte), the representation the format's transforms are defined
/// on. Every length and count read from the stream is checked before it is used, because the data is untrusted.
/// </remarks>
internal sealed class Vp8LDecoder
{
    private const int NumLengthCodes = 24;
    private const int NumDistanceCodes = 40;
    private const int MaxCacheBits = 11;
    private const int CodeLengthCodes = 19;

    private static ReadOnlySpan<byte> CodeLengthCodeOrder => [17, 18, 0, 1, 2, 3, 4, 5, 16, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];

    // (dx, dy) pairs of the 120 short distance codes, packed as dy * 16 + 8 - dx (RFC 9649 section 3.6.2.2.1).
    private static ReadOnlySpan<byte> DistanceMap =>
    [
        0x18, 0x07, 0x17, 0x19, 0x28, 0x06, 0x27, 0x29, 0x16, 0x1a, 0x26, 0x2a, 0x38, 0x05, 0x37, 0x39, 0x15, 0x1b, 0x36, 0x3a,
        0x25, 0x2b, 0x48, 0x04, 0x47, 0x49, 0x14, 0x1c, 0x35, 0x3b, 0x46, 0x4a, 0x24, 0x2c, 0x58, 0x45, 0x4b, 0x34, 0x3c, 0x03,
        0x57, 0x59, 0x13, 0x1d, 0x56, 0x5a, 0x23, 0x2d, 0x44, 0x4c, 0x55, 0x5b, 0x33, 0x3d, 0x68, 0x02, 0x67, 0x69, 0x12, 0x1e,
        0x66, 0x6a, 0x22, 0x2e, 0x54, 0x5c, 0x43, 0x4d, 0x65, 0x6b, 0x32, 0x3e, 0x78, 0x01, 0x77, 0x79, 0x53, 0x5d, 0x11, 0x1f,
        0x64, 0x6c, 0x42, 0x4e, 0x76, 0x7a, 0x21, 0x2f, 0x75, 0x7b, 0x31, 0x3f, 0x63, 0x6d, 0x52, 0x5e, 0x00, 0x74, 0x7c, 0x41,
        0x4f, 0x10, 0x20, 0x62, 0x6e, 0x30, 0x73, 0x7d, 0x51, 0x5f, 0x40, 0x72, 0x7e, 0x61, 0x6f, 0x50, 0x71, 0x7f, 0x60, 0x70,
    ];

    private readonly Vp8LBitReader _br;
    private readonly CancellationToken _ct;

    private Vp8LDecoder(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        _br = new Vp8LBitReader(data);
        _ct = ct;
    }

    /// <summary>Reads the dimensions and alpha hint from a VP8L chunk payload, or null when it is not one.</summary>
    public static (int Width, int Height, bool HasAlpha)? ReadHeader(ReadOnlySpan<byte> data)
    {
        if (data.Length < 5 || data[0] != 0x2F) return null;
        var bits = (uint)(data[1] | data[2] << 8 | data[3] << 16 | data[4] << 24);
        if (bits >> 29 != 0) return null; // version must be 0
        return ((int)(bits & 0x3FFF) + 1, (int)((bits >> 14) & 0x3FFF) + 1, ((bits >> 28) & 1) != 0);
    }

    /// <summary>Decodes a complete VP8L chunk payload (signature and header included), read in place, into <paramref name="argb"/>.</summary>
    public static void Decode(ReadOnlyMemory<byte> data, int width, int height, Span<uint> argb, CancellationToken ct)
    {
        if (ReadHeader(data.Span) is not { } header || header.Width != width || header.Height != height)
            throw new ImageDecodeException("Invalid WebP lossless header.");
        var decoder = new Vp8LDecoder(data[5..], ct);
        decoder.DecodeImageStream(width, height, argb);
    }

    /// <summary>
    /// Decodes a headerless image stream of known dimensions, as an 'ALPH' chunk stores it, keeping only the green
    /// channel, which is where the alpha values live.
    /// </summary>
    public static void DecodeAlpha(ReadOnlyMemory<byte> data, int width, int height, Span<byte> alpha, CancellationToken ct)
    {
        var argb = new uint[(long)width * height];
        new Vp8LDecoder(data, ct).DecodeImageStream(width, height, argb);
        for (var i = 0; i < argb.Length; i++) alpha[i] = (byte)(argb[i] >> 8);
    }

    private void DecodeImageStream(int width, int height, Span<uint> output)
    {
        // The transforms come first; each may shrink the width the rest of the stream is coded at.
        var transforms = new List<Transform>(4);
        var codedWidth = width;
        var seen = 0;
        while (_br.ReadBits(1) != 0)
        {
            var type = (int)_br.ReadBits(2);
            if ((seen & (1 << type)) != 0) throw new ImageDecodeException("Invalid WebP lossless stream: a transform is repeated.");
            seen |= 1 << type;
            var t = new Transform { Type = type, Width = codedWidth };
            switch (type)
            {
                case 0: // predictor
                case 1: // colour
                    t.Bits = (int)_br.ReadBits(3) + 2;
                    t.Data = DecodeSubImage(DivRoundUp(codedWidth, t.Bits), DivRoundUp(height, t.Bits));
                    break;
                case 3: // colour indexing
                {
                    var size = (int)_br.ReadBits(8) + 1;
                    t.Bits = size > 16 ? 0 : size > 4 ? 1 : size > 2 ? 2 : 3;
                    var palette = DecodeSubImage(size, 1);
                    for (var i = 1; i < size; i++) palette[i] = AddPixels(palette[i], palette[i - 1]);
                    // Indices past the table read as transparent black, so the table is padded to every value a packed
                    // index can take.
                    var table = new uint[256];
                    palette.AsSpan(0, size).CopyTo(table);
                    t.Data = table;
                    codedWidth = DivRoundUp(codedWidth, t.Bits);
                    break;
                }
            }
            transforms.Add(t);
        }

        // Unless palette indices are packed several to a pixel, the pixels are decoded straight into the output and
        // every inverse transform runs in place.
        var current = codedWidth == width ? output : new uint[(long)codedWidth * height];
        DecodeEntropyCodedImage(codedWidth, height, current, isMainImage: true);
        if (_br.IsOverrun) throw new ImageDecodeException("Truncated WebP lossless data.");

        for (var i = transforms.Count - 1; i >= 0; i--)
        {
            var t = transforms[i];
            switch (t.Type)
            {
                case 0: InversePredictor(t, height, current); break;
                case 1: InverseColorTransform(t, height, current); break;
                case 2: AddGreen(current[..(t.Width * height)]); break;
                case 3 when t.Bits == 0: MapPalette(t.Data!, current[..(t.Width * height)]); break;
                case 3:
                    // Unpacking widens the image to the full width, which the transforms read before it work at.
                    ExpandPalette(t, height, current, output);
                    current = output;
                    break;
            }
        }
    }

    private uint[] DecodeSubImage(int width, int height)
    {
        var pixels = new uint[(long)width * height];
        DecodeEntropyCodedImage(width, height, pixels, isMainImage: false);
        return pixels;
    }

    private void DecodeEntropyCodedImage(int width, int height, Span<uint> pixels, bool isMainImage)
    {
        var cacheBits = 0;
        if (_br.ReadBits(1) != 0)
        {
            cacheBits = (int)_br.ReadBits(4);
            if (cacheBits is < 1 or > MaxCacheBits) throw new ImageDecodeException("Invalid WebP lossless colour cache size.");
        }

        uint[]? entropyImage = null;
        var prefixBits = 0;
        var entropyWidth = 0;
        var numGroups = 1;
        if (isMainImage && _br.ReadBits(1) != 0)
        {
            prefixBits = (int)_br.ReadBits(3) + 2;
            entropyWidth = DivRoundUp(width, prefixBits);
            entropyImage = DecodeSubImage(entropyWidth, DivRoundUp(height, prefixBits));
            var max = 0;
            for (var i = 0; i < entropyImage.Length; i++)
            {
                var group = (int)((entropyImage[i] >> 8) & 0xFFFF);
                entropyImage[i] = (uint)group;
                if (group > max) max = group;
            }
            numGroups = max + 1;
            // Every group is read in full, a few bits at least each, so a count beyond what the remaining data could
            // describe is a lie meant to force a large allocation.
            if (numGroups > _br.RemainingBits / 20 + 1) throw new ImageDecodeException("Invalid WebP lossless prefix code count.");
        }

        var cacheSize = cacheBits > 0 ? 1 << cacheBits : 0;
        var groups = new PrefixGroup[numGroups];
        for (var i = 0; i < numGroups; i++) groups[i] = ReadPrefixGroup(cacheSize);

        var cache = cacheBits > 0 ? new uint[cacheSize] : null;
        var cacheShift = 32 - cacheBits;
        var total = pixels.Length;
        var mask = entropyImage is null ? -1 : (1 << prefixBits) - 1;
        var group0 = groups[0];
        var g = group0;
        int x = 0, y = 0, pos = 0;
        var cachedUpTo = 0;
        while (pos < total)
        {
            if ((x & mask) == 0) g = entropyImage is null ? group0 : groups[entropyImage[(y >> prefixBits) * entropyWidth + (x >> prefixBits)]];
            var code = g.Green.ReadSymbol(_br);
            if (code < 256)
            {
                if (g.IsTrivialLiteral) pixels[pos] = g.LiteralArb | (uint)(code << 8);
                else
                {
                    var red = g.Red.ReadSymbol(_br);
                    var blue = g.Blue.ReadSymbol(_br);
                    var alpha = g.Alpha.ReadSymbol(_br);
                    pixels[pos] = (uint)(alpha << 24 | red << 16 | code << 8 | blue);
                }
                pos++;
                if (++x == width) { x = 0; y++; if ((y & 15) == 0) Check(); }
            }
            else if (code < 256 + NumLengthCodes)
            {
                var length = ReadPrefixedValue(code - 256);
                var distSymbol = g.Distance.ReadSymbol(_br);
                var dist = ToPlaneDistance(width, ReadPrefixedValue(distSymbol));
                if (dist > pos || length > total - pos) throw new ImageDecodeException("Invalid WebP lossless back reference.");
                // The source may overlap the destination (a run), so the copy goes forwards one pixel at a time.
                for (var i = 0; i < length; i++) pixels[pos + i] = pixels[pos + i - dist];
                pos += length;
                x += length;
                while (x >= width) { x -= width; y++; if ((y & 15) == 0) Check(); }
                if ((x & mask) != 0 && entropyImage is not null) g = groups[entropyImage[(y >> prefixBits) * entropyWidth + (x >> prefixBits)]];
            }
            else
            {
                if (cache is null) throw new ImageDecodeException("Invalid WebP lossless colour cache reference.");
                // Every pixel before this one enters the cache in order; doing it lazily, only when a lookup needs it,
                // keeps the common literal and copy paths free of the hashing.
                while (cachedUpTo < pos) { var c = pixels[cachedUpTo++]; cache[(0x1e35a7bdu * c) >> cacheShift] = c; }
                var index = code - 256 - NumLengthCodes;
                if (index >= cacheSize) throw new ImageDecodeException("Invalid WebP lossless colour cache reference.");
                pixels[pos++] = cache[index];
                if (++x == width) { x = 0; y++; if ((y & 15) == 0) Check(); }
            }
        }
    }

    private void Check()
    {
        _ct.ThrowIfCancellationRequested();
        if (_br.IsOverrun) throw new ImageDecodeException("Truncated WebP lossless data.");
    }

    private PrefixGroup ReadPrefixGroup(int cacheSize)
    {
        var green = ReadPrefixCode(256 + NumLengthCodes + cacheSize);
        var red = ReadPrefixCode(256);
        var blue = ReadPrefixCode(256);
        var alpha = ReadPrefixCode(256);
        var distance = ReadPrefixCode(NumDistanceCodes);
        return new PrefixGroup(green, red, blue, alpha, distance);
    }

    private HuffmanTable ReadPrefixCode(int alphabetSize)
    {
        var lengths = new byte[alphabetSize];
        if (_br.ReadBits(1) != 0)
        {
            // Simple code: one or two symbols of length one.
            var count = (int)_br.ReadBits(1) + 1;
            var first = (int)_br.ReadBits(_br.ReadBits(1) == 0 ? 1 : 8);
            if (first >= alphabetSize) throw new ImageDecodeException("Invalid WebP lossless prefix code.");
            lengths[first] = 1;
            if (count == 2)
            {
                var second = (int)_br.ReadBits(8);
                if (second >= alphabetSize) throw new ImageDecodeException("Invalid WebP lossless prefix code.");
                lengths[second] = 1;
            }
        }
        else
        {
            Span<byte> codeLengthLengths = stackalloc byte[CodeLengthCodes];
            var numCodes = (int)_br.ReadBits(4) + 4;
            for (var i = 0; i < numCodes; i++) codeLengthLengths[CodeLengthCodeOrder[i]] = (byte)_br.ReadBits(3);
            var lengthCode = HuffmanTable.Build(codeLengthLengths, rootBits: 7)
                ?? throw new ImageDecodeException("Invalid WebP lossless code length code.");

            var maxSymbol = alphabetSize;
            if (_br.ReadBits(1) != 0)
            {
                var lengthBits = 2 + 2 * (int)_br.ReadBits(3);
                maxSymbol = 2 + (int)_br.ReadBits(lengthBits);
                if (maxSymbol > alphabetSize) throw new ImageDecodeException("Invalid WebP lossless prefix code.");
            }

            var symbol = 0;
            var previous = 8;
            while (symbol < alphabetSize)
            {
                if (maxSymbol-- == 0) break;
                var code = lengthCode.ReadSymbol(_br);
                if (code < 16)
                {
                    lengths[symbol++] = (byte)code;
                    if (code != 0) previous = code;
                    continue;
                }
                var (extraBits, offset) = code switch { 16 => (2, 3), 17 => (3, 3), _ => (7, 11) };
                var repeat = (int)_br.ReadBits(extraBits) + offset;
                if (symbol + repeat > alphabetSize) throw new ImageDecodeException("Invalid WebP lossless prefix code.");
                var value = code == 16 ? (byte)previous : (byte)0;
                lengths.AsSpan(symbol, repeat).Fill(value);
                symbol += repeat;
            }
            if (_br.IsOverrun) throw new ImageDecodeException("Truncated WebP lossless data.");
        }
        return HuffmanTable.Build(lengths, rootBits: 8) ?? throw new ImageDecodeException("Invalid WebP lossless prefix code.");
    }

    private int ReadPrefixedValue(int prefix)
    {
        if (prefix < 4) return prefix + 1;
        var extraBits = (prefix - 2) >> 1;
        var offset = (2 + (prefix & 1)) << extraBits;
        return offset + (int)_br.ReadBits(extraBits) + 1;
    }

    /// <summary>The packed (dx, dy) of a short distance code in 1..120.</summary>
    internal static int DistanceMapEntry(int code) => DistanceMap[code - 1];

    private static int ToPlaneDistance(int width, int code)
    {
        if (code > 120) return code - 120;
        var packed = DistanceMap[code - 1];
        var dy = packed >> 4;
        var dx = 8 - (packed & 15);
        var dist = dy * width + dx;
        return dist >= 1 ? dist : 1;
    }

    private static int DivRoundUp(int value, int bits) => (value + (1 << bits) - 1) >> bits;

    // ------------------------------------------------------------------ inverse transforms

    private static void InversePredictor(Transform t, int height, Span<uint> data)
    {
        var width = t.Width;
        var blocksPerRow = DivRoundUp(width, t.Bits);
        var modes = t.Data!;
        // The first row predicts from the left (its first pixel from opaque black), every first column from above.
        data[0] = AddPixels(data[0], 0xFF000000u);
        for (var x = 1; x < width; x++) data[x] = AddPixels(data[x], data[x - 1]);
        for (var y = 1; y < height; y++)
        {
            var row = y * width;
            data[row] = AddPixels(data[row], data[row - width]);
            var modeRow = (y >> t.Bits) * blocksPerRow;
            var blockSize = 1 << t.Bits;
            for (var x = 1; x < width;)
            {
                var mode = (int)((modes[modeRow + (x >> t.Bits)] >> 8) & 15);
                var end = Math.Min(width, (x & ~(blockSize - 1)) + blockSize);
                for (; x < end; x++)
                {
                    var i = row + x;
                    // For the last column, the pixel after "top" is the first of this row, which the flat layout gives.
                    var predicted = Predict(mode, data[i - 1], data[i - width], data[i - width + 1], data[i - width - 1]);
                    data[i] = AddPixels(data[i], predicted);
                }
            }
        }
    }

    internal static uint Predict(int mode, uint left, uint top, uint topRight, uint topLeft) => mode switch
    {
        1 => left,
        2 => top,
        3 => topRight,
        4 => topLeft,
        5 => Average2(Average2(left, topRight), top),
        6 => Average2(left, topLeft),
        7 => Average2(left, top),
        8 => Average2(topLeft, top),
        9 => Average2(top, topRight),
        10 => Average2(Average2(left, topLeft), Average2(top, topRight)),
        11 => Select(left, top, topLeft),
        12 => ClampAddSubtractFull(left, top, topLeft),
        13 => ClampAddSubtractHalf(Average2(left, top), topLeft),
        // 0, and the two values a 4 bit mode can take that name no predictor.
        _ => 0xFF000000u,
    };

    internal static uint AddPixels(uint a, uint b)
        => (((a & 0xFF00FF00u) + (b & 0xFF00FF00u)) & 0xFF00FF00u) | (((a & 0x00FF00FFu) + (b & 0x00FF00FFu)) & 0x00FF00FFu);

    internal static uint SubtractPixels(uint a, uint b)
        => (((a | 0x00FF00FFu) - (b & 0xFF00FF00u)) & 0xFF00FF00u) | (((a | 0xFF00FF00u) - (b & 0x00FF00FFu)) & 0x00FF00FFu);

    private static uint Average2(uint a, uint b) => (((a ^ b) & 0xFEFEFEFEu) >> 1) + (a & b);

    private static uint Select(uint left, uint top, uint topLeft)
    {
        var pl = Math.Abs((int)(top >> 24) - (int)(topLeft >> 24)) + Math.Abs((int)((top >> 16) & 255) - (int)((topLeft >> 16) & 255))
               + Math.Abs((int)((top >> 8) & 255) - (int)((topLeft >> 8) & 255)) + Math.Abs((int)(top & 255) - (int)(topLeft & 255));
        var pt = Math.Abs((int)(left >> 24) - (int)(topLeft >> 24)) + Math.Abs((int)((left >> 16) & 255) - (int)((topLeft >> 16) & 255))
               + Math.Abs((int)((left >> 8) & 255) - (int)((topLeft >> 8) & 255)) + Math.Abs((int)(left & 255) - (int)(topLeft & 255));
        // |estimate - L| reduces to |T - TL| and |estimate - T| to |L - TL|, channel by channel.
        return pl < pt ? left : top;
    }

    private static uint ClampAddSubtractFull(uint a, uint b, uint c)
    {
        var r = 0u;
        for (var shift = 0; shift < 32; shift += 8)
        {
            var v = (int)((a >> shift) & 255) + (int)((b >> shift) & 255) - (int)((c >> shift) & 255);
            r |= (uint)(v < 0 ? 0 : v > 255 ? 255 : v) << shift;
        }
        return r;
    }

    private static uint ClampAddSubtractHalf(uint a, uint b)
    {
        var r = 0u;
        for (var shift = 0; shift < 32; shift += 8)
        {
            var ca = (int)((a >> shift) & 255);
            var v = ca + (ca - (int)((b >> shift) & 255)) / 2;
            r |= (uint)(v < 0 ? 0 : v > 255 ? 255 : v) << shift;
        }
        return r;
    }

    private static void InverseColorTransform(Transform t, int height, Span<uint> data)
    {
        var width = t.Width;
        var blocksPerRow = DivRoundUp(width, t.Bits);
        var elements = t.Data!;
        for (var y = 0; y < height; y++)
        {
            var row = y * width;
            var elementRow = (y >> t.Bits) * blocksPerRow;
            for (var x = 0; x < width; x++)
            {
                var e = elements[elementRow + (x >> t.Bits)];
                data[row + x] = ApplyColorTransform(data[row + x], (sbyte)e, (sbyte)(e >> 8), (sbyte)(e >> 16), inverse: true);
            }
        }
    }

    /// <summary>Adds (inverse) or subtracts the colour transform deltas of one element to a pixel.</summary>
    internal static uint ApplyColorTransform(uint argb, sbyte greenToRed, sbyte greenToBlue, sbyte redToBlue, bool inverse)
    {
        var green = (sbyte)(argb >> 8);
        var red = (int)((argb >> 16) & 255);
        var blue = (int)(argb & 255);
        if (inverse)
        {
            red = (red + ((greenToRed * green) >> 5)) & 255;
            blue = (blue + ((greenToBlue * green) >> 5) + ((redToBlue * (sbyte)red) >> 5)) & 255;
        }
        else
        {
            var originalRed = (sbyte)red;
            red = (red - ((greenToRed * green) >> 5)) & 255;
            blue = (blue - ((greenToBlue * green) >> 5) - ((redToBlue * originalRed) >> 5)) & 255;
        }
        return (argb & 0xFF00FF00u) | (uint)(red << 16) | (uint)blue;
    }

    private static void AddGreen(Span<uint> data)
    {
        for (var i = 0; i < data.Length; i++)
        {
            var p = data[i];
            var green = (p >> 8) & 255;
            data[i] = (p & 0xFF00FF00u) | (((p & 0x00FF00FFu) + (green << 16 | green)) & 0x00FF00FFu);
        }
    }

    private static void MapPalette(uint[] table, Span<uint> data)
    {
        for (var i = 0; i < data.Length; i++) data[i] = table[(data[i] >> 8) & 255];
    }

    private static void ExpandPalette(Transform t, int height, ReadOnlySpan<uint> packed, Span<uint> output)
    {
        var table = t.Data!;
        var width = t.Width;
        var packedWidth = DivRoundUp(width, t.Bits);
        var bitsPerPixel = 8 >> t.Bits;
        var pixelsPerByte = 1 << t.Bits;
        var indexMask = (1 << bitsPerPixel) - 1;
        // Bottom up and right to left, so that a one pixel wide image, the one case where both share memory, never
        // overwrites a packed pixel before reading it.
        for (var y = height - 1; y >= 0; y--)
        {
            var src = y * packedWidth;
            var dst = y * width;
            for (var x = width - 1; x >= 0; x--)
            {
                var bits = (int)((packed[src + x / pixelsPerByte] >> 8) & 255);
                var index = (bits >> ((x % pixelsPerByte) * bitsPerPixel)) & indexMask;
                output[dst + x] = table[index];
            }
        }
    }

    private sealed class Transform
    {
        public int Type;
        public int Bits;
        public int Width;
        public uint[]? Data;
    }

    private sealed class PrefixGroup
    {
        public PrefixGroup(HuffmanTable green, HuffmanTable red, HuffmanTable blue, HuffmanTable alpha, HuffmanTable distance)
        {
            Green = green; Red = red; Blue = blue; Alpha = alpha; Distance = distance;
            // When red, blue and alpha each have a single symbol they cost no bits, so a literal is its green value
            // over a constant, which is common in palette and alpha images.
            if (red.SingleSymbol >= 0 && blue.SingleSymbol >= 0 && alpha.SingleSymbol >= 0)
            {
                IsTrivialLiteral = true;
                LiteralArb = (uint)(alpha.SingleSymbol << 24 | red.SingleSymbol << 16 | blue.SingleSymbol);
            }
        }

        public HuffmanTable Green { get; }
        public HuffmanTable Red { get; }
        public HuffmanTable Blue { get; }
        public HuffmanTable Alpha { get; }
        public HuffmanTable Distance { get; }
        public bool IsTrivialLiteral { get; }
        public uint LiteralArb { get; }
    }
}

/// <summary>Reads the least significant bit first, as the WebP lossless format packs its bits.</summary>
internal sealed class Vp8LBitReader
{
    private readonly byte[] _data;
    private int _pos;
    private readonly int _end;
    private readonly int _length;
    private ulong _buffer;
    private int _bits;
    private long _consumed;

    public Vp8LBitReader(ReadOnlyMemory<byte> data)
    {
        // The caller's array is read in place; only memory no array backs is copied.
        var segment = MemoryMarshal.TryGetArray(data, out var array) ? array : new ArraySegment<byte>(data.ToArray());
        _data = segment.Array!;
        _pos = segment.Offset;
        _end = segment.Offset + segment.Count;
        _length = segment.Count;
        Fill();
    }

    /// <summary>True once more bits were read than the data holds; they read as zeros.</summary>
    public bool IsOverrun => _consumed > (long)_length * 8;

    public long RemainingBits => (long)_length * 8 - _consumed;

    private void Fill()
    {
        while (_bits <= 56)
        {
            var b = _pos < _end ? _data[_pos] : (byte)0;
            _pos++;
            _buffer |= (ulong)b << _bits;
            _bits += 8;
        }
    }

    public uint ReadBits(int count)
    {
        if (count == 0) return 0;
        if (_bits < count) Fill();
        var value = (uint)(_buffer & ((1UL << count) - 1));
        _buffer >>= count;
        _bits -= count;
        _consumed += count;
        return value;
    }

    /// <summary>The next bits without consuming them; at least 32 are always available.</summary>
    public uint Peek()
    {
        if (_bits < 32) Fill();
        return (uint)_buffer;
    }

    public void Skip(int count)
    {
        _buffer >>= count;
        _bits -= count;
        _consumed += count;
    }
}

/// <summary>
/// A canonical prefix code with a two level lookup table: codes up to the root length resolve in one lookup, longer
/// ones through a second table per root prefix. Bits are read least significant first, so codes are stored reversed.
/// </summary>
internal sealed class HuffmanTable
{
    private const int MaxLength = 15;

    // Each entry: symbol in the upper bits and the number of bits it consumes in the low 4; for a root entry that leads
    // to a second level table, the table's offset and its index width instead, flagged by a length above the root.
    private readonly int[] _entries;
    private readonly int _rootBits;

    private HuffmanTable(int[] entries, int rootBits, int singleSymbol)
    {
        _entries = entries;
        _rootBits = rootBits;
        SingleSymbol = singleSymbol;
    }

    /// <summary>The only symbol when the code has just one, which then costs no bits; otherwise -1.</summary>
    public int SingleSymbol { get; }

    /// <summary>Builds the table, or returns null when the lengths do not form a complete prefix code.</summary>
    public static HuffmanTable? Build(ReadOnlySpan<byte> lengths, int rootBits)
    {
        Span<int> count = stackalloc int[MaxLength + 1];
        var symbols = 0;
        var last = -1;
        for (var s = 0; s < lengths.Length; s++)
        {
            if (lengths[s] == 0) continue;
            count[lengths[s]]++;
            symbols++;
            last = s;
        }
        if (symbols == 0) return null;
        if (symbols == 1) return new HuffmanTable([last << 4], 0, last);

        // A complete code fills the code space exactly.
        var space = 0;
        for (var len = 1; len <= MaxLength; len++) space += count[len] << (MaxLength - len);
        if (space != 1 << MaxLength) return null;

        Span<int> nextCode = stackalloc int[MaxLength + 2];
        var code = 0;
        for (var len = 1; len <= MaxLength; len++)
        {
            code = (code + count[len - 1]) << 1;
            nextCode[len] = code;
        }

        // Size the second level tables: for every root prefix, the longest code that starts with it.
        var rootSize = 1 << rootBits;
        var subBits = new int[rootSize];
        var codes = new int[lengths.Length];
        for (var s = 0; s < lengths.Length; s++)
        {
            int len = lengths[s];
            if (len == 0) continue;
            var reversed = Reverse(nextCode[len]++, len);
            codes[s] = reversed;
            if (len > rootBits)
            {
                var root = reversed & (rootSize - 1);
                subBits[root] = Math.Max(subBits[root], len - rootBits);
            }
        }
        var total = rootSize;
        var offsets = new int[rootSize];
        for (var r = 0; r < rootSize; r++)
        {
            if (subBits[r] == 0) continue;
            offsets[r] = total;
            total += 1 << subBits[r];
        }

        var entries = new int[total];
        for (var r = 0; r < rootSize; r++)
            if (subBits[r] != 0) entries[r] = (offsets[r] << 8) | ((rootBits + subBits[r]) << 4) | 0xF;
        for (var s = 0; s < lengths.Length; s++)
        {
            int len = lengths[s];
            if (len == 0) continue;
            var reversed = codes[s];
            if (len <= rootBits)
            {
                for (var i = reversed; i < rootSize; i += 1 << len) entries[i] = (s << 4) | len;
            }
            else
            {
                var root = reversed & (rootSize - 1);
                var width = subBits[root];
                var rest = reversed >> rootBits;
                var subLen = len - rootBits;
                for (var i = rest; i < 1 << width; i += 1 << subLen) entries[offsets[root] + i] = (s << 4) | subLen;
            }
        }
        return new HuffmanTable(entries, rootBits, -1);
    }

    private static int Reverse(int code, int length)
    {
        var r = 0;
        for (var i = 0; i < length; i++) { r = (r << 1) | (code & 1); code >>= 1; }
        return r;
    }

    public int ReadSymbol(Vp8LBitReader br)
    {
        if (SingleSymbol >= 0) return SingleSymbol;
        var bits = br.Peek();
        var e = _entries[bits & ((1u << _rootBits) - 1)];
        if ((e & 0xF) != 0xF)
        {
            br.Skip(e & 0xF);
            return e >> 4;
        }
        var width = ((e >> 4) & 0xF) - _rootBits;
        br.Skip(_rootBits);
        var sub = _entries[(e >> 8) + (int)((bits >> _rootBits) & ((1u << width) - 1))];
        br.Skip(sub & 0xF);
        return sub >> 4;
    }
}
