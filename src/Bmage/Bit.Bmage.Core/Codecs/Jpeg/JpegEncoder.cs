using System.Buffers.Binary;
using Bit.Bmage.Metadata;

namespace Bit.Bmage.Codecs.Jpeg;

/// <summary>
/// Managed baseline JPEG encoder with 4:2:0 or 4:4:4 chroma subsampling, standard Huffman tables and quality-scaled
/// quantisation tables. Transparent pixels are composited over the export background colour.
/// </summary>
public sealed class JpegEncoder : IImageEncoder
{
    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.Jpeg];
    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    public ValueTask<EncodedImage> EncodeAsync(ImageBuffer image, ImageExportOptions options, CancellationToken cancellationToken = default)
        => new(Encode(image, options, cancellationToken));

    public static EncodedImage Encode(ImageBuffer image, ImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= ImageExportOptions.Jpeg;
        var quality = Math.Clamp(options.Quality, 0.01, 1.0);
        var subsample = quality < 0.9; // 4:2:0 below 90, 4:4:4 above (matches common encoder behaviour)
        var writer = new JpegWriter(image, quality, subsample, options.Background, cancellationToken);
        var bytes = writer.Write();
        var metadata = image.Metadata?.Apply(options.Metadata, image.Width, image.Height);
        if (metadata is { IsEmpty: false }) bytes = JpegContainer.WriteMetadata(bytes, metadata);
        return new EncodedImage(bytes, ImageFormat.Jpeg, image.Width, image.Height, quality);
    }
}

internal sealed class JpegWriter
{
    private static readonly int[] StdLuminanceQt =
    [
        16, 11, 10, 16, 24, 40, 51, 61, 12, 12, 14, 19, 26, 58, 60, 55, 14, 13, 16, 24, 40, 57, 69, 56, 14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77, 24, 35, 55, 64, 81, 104, 113, 92, 49, 64, 78, 87, 103, 121, 120, 101, 72, 92, 95, 98, 112, 100, 103, 99,
    ];

    private static readonly int[] StdChrominanceQt =
    [
        17, 18, 24, 47, 99, 99, 99, 99, 18, 21, 26, 66, 99, 99, 99, 99, 24, 26, 56, 99, 99, 99, 99, 99, 47, 66, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
    ];

    private static readonly byte[] DcLuminanceBits = [0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] DcLuminanceValues = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
    private static readonly byte[] DcChrominanceBits = [0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0];
    private static readonly byte[] DcChrominanceValues = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

    private static readonly byte[] AcLuminanceBits = [0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7d];
    private static readonly byte[] AcLuminanceValues =
    [
        0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07, 0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xa1, 0x08,
        0x23, 0x42, 0xb1, 0xc1, 0x15, 0x52, 0xd1, 0xf0, 0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0a, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x25, 0x26, 0x27, 0x28,
        0x29, 0x2a, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
        0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
        0x8a, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9a, 0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7, 0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4, 0xb5, 0xb6,
        0xb7, 0xb8, 0xb9, 0xba, 0xc2, 0xc3, 0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda, 0xe1, 0xe2,
        0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xea, 0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 0xfa,
    ];

    private static readonly byte[] AcChrominanceBits = [0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77];
    private static readonly byte[] AcChrominanceValues =
    [
        0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21, 0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71, 0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91,
        0xa1, 0xb1, 0xc1, 0x09, 0x23, 0x33, 0x52, 0xf0, 0x15, 0x62, 0x72, 0xd1, 0x0a, 0x16, 0x24, 0x34, 0xe1, 0x25, 0xf1, 0x17, 0x18, 0x19, 0x1a, 0x26,
        0x27, 0x28, 0x29, 0x2a, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
        0x59, 0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
        0x88, 0x89, 0x8a, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9a, 0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7, 0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4,
        0xb5, 0xb6, 0xb7, 0xb8, 0xb9, 0xba, 0xc2, 0xc3, 0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda,
        0xe2, 0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xea, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 0xfa,
    ];

    private readonly ImageBuffer _image;
    private readonly bool _subsample;
    private readonly Rgba32 _background;
    private readonly CancellationToken _ct;
    private readonly int[] _lumaQt = new int[64];
    private readonly int[] _chromaQt = new int[64];
    private readonly float[] _lumaScale = new float[64];
    private readonly float[] _chromaScale = new float[64];
    private readonly (ushort Code, byte Length)[] _dcLuma = new (ushort, byte)[256];
    private readonly (ushort Code, byte Length)[] _acLuma = new (ushort, byte)[256];
    private readonly (ushort Code, byte Length)[] _dcChroma = new (ushort, byte)[256];
    private readonly (ushort Code, byte Length)[] _acChroma = new (ushort, byte)[256];

    private MemoryStream _out = null!;
    private uint _bitBuf;
    private int _bitCount;

    public JpegWriter(ImageBuffer image, double quality, bool subsample, Rgba32 background, CancellationToken ct)
    {
        _image = image;
        _subsample = subsample;
        _background = background;
        _ct = ct;
        BuildQuantTables(quality);
        BuildHuffman(DcLuminanceBits, DcLuminanceValues, _dcLuma);
        BuildHuffman(AcLuminanceBits, AcLuminanceValues, _acLuma);
        BuildHuffman(DcChrominanceBits, DcChrominanceValues, _dcChroma);
        BuildHuffman(AcChrominanceBits, AcChrominanceValues, _acChroma);
    }

    private void BuildQuantTables(double quality)
    {
        // IJG quality scaling.
        var q = quality * 100;
        var scale = q < 50 ? 5000 / q : 200 - q * 2;
        for (var i = 0; i < 64; i++)
        {
            _lumaQt[i] = Math.Clamp((int)((StdLuminanceQt[i] * scale + 50) / 100), 1, 255);
            _chromaQt[i] = Math.Clamp((int)((StdChrominanceQt[i] * scale + 50) / 100), 1, 255);
        }
        // AAN scale factors for the fast float DCT.
        ReadOnlySpan<double> aan = [1.0, 1.387039845, 1.306562965, 1.175875602, 1.0, 0.785694958, 0.541196100, 0.275899379];
        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
            {
                var i = y * 8 + x;
                var f = 1.0 / (aan[x] * aan[y] * 8.0);
                _lumaScale[i] = (float)(f / _lumaQt[i]);
                _chromaScale[i] = (float)(f / _chromaQt[i]);
            }
    }

    private static void BuildHuffman(byte[] bits, byte[] values, (ushort Code, byte Length)[] table)
    {
        var code = 0;
        var k = 0;
        for (var len = 1; len <= 16; len++)
        {
            for (var i = 0; i < bits[len - 1]; i++)
            {
                table[values[k]] = ((ushort)code, (byte)len);
                code++; k++;
            }
            code <<= 1;
        }
    }

    public byte[] Write()
    {
        int w = _image.Width, h = _image.Height;
        _out = new MemoryStream(Math.Max(4096, w * h / 6));
        WriteMarker(0xD8);
        WriteJfif();
        WriteDqt();
        WriteSof(w, h);
        WriteDht();
        WriteSos();
        WriteScan();
        FlushBits();
        WriteMarker(0xD9);
        return _out.ToArray();
    }

    private void WriteMarker(byte marker) => _out.Write([0xFF, marker]);

    private void WriteSegment(byte marker, ReadOnlySpan<byte> payload)
    {
        Span<byte> head = stackalloc byte[4];
        head[0] = 0xFF; head[1] = marker;
        BinaryPrimitives.WriteUInt16BigEndian(head[2..], (ushort)(payload.Length + 2));
        _out.Write(head);
        _out.Write(payload);
    }

    private void WriteJfif()
    {
        Span<byte> p = stackalloc byte[14];
        "JFIF\0"u8.CopyTo(p);
        p[5] = 1; p[6] = 1; p[7] = 0;
        BinaryPrimitives.WriteUInt16BigEndian(p[8..], 72);
        BinaryPrimitives.WriteUInt16BigEndian(p[10..], 72);
        p[12] = 0; p[13] = 0;
        WriteSegment(0xE0, p);
    }

    private void WriteDqt()
    {
        Span<byte> p = stackalloc byte[130];
        p[0] = 0;
        for (var i = 0; i < 64; i++) p[1 + i] = (byte)_lumaQt[JpegConstants.ZigZag[i]];
        p[65] = 1;
        for (var i = 0; i < 64; i++) p[66 + i] = (byte)_chromaQt[JpegConstants.ZigZag[i]];
        WriteSegment(0xDB, p);
    }

    private void WriteSof(int w, int h)
    {
        // The frame header stores 16-bit dimensions; a larger value would be silently truncated into a corrupt file.
        if (w > ushort.MaxValue || h > ushort.MaxValue)
            throw new ImageEncodeException($"JPEG cannot store an image of {w}x{h}; each dimension must be at most {ushort.MaxValue}.");
        Span<byte> p = stackalloc byte[15];
        p[0] = 8;
        BinaryPrimitives.WriteUInt16BigEndian(p[1..], (ushort)h);
        BinaryPrimitives.WriteUInt16BigEndian(p[3..], (ushort)w);
        p[5] = 3;
        p[6] = 1; p[7] = (byte)(_subsample ? 0x22 : 0x11); p[8] = 0;
        p[9] = 2; p[10] = 0x11; p[11] = 1;
        p[12] = 3; p[13] = 0x11; p[14] = 1;
        WriteSegment(0xC0, p);
    }

    private void WriteDht()
    {
        WriteHuffmanTable(0x00, DcLuminanceBits, DcLuminanceValues);
        WriteHuffmanTable(0x10, AcLuminanceBits, AcLuminanceValues);
        WriteHuffmanTable(0x01, DcChrominanceBits, DcChrominanceValues);
        WriteHuffmanTable(0x11, AcChrominanceBits, AcChrominanceValues);
    }

    private void WriteHuffmanTable(byte id, byte[] bits, byte[] values)
    {
        var p = new byte[1 + 16 + values.Length];
        p[0] = id;
        bits.CopyTo(p, 1);
        values.CopyTo(p, 17);
        WriteSegment(0xC4, p);
    }

    private void WriteSos()
    {
        Span<byte> p = stackalloc byte[10];
        p[0] = 3;
        p[1] = 1; p[2] = 0x00;
        p[3] = 2; p[4] = 0x11;
        p[5] = 3; p[6] = 0x11;
        p[7] = 0; p[8] = 63; p[9] = 0;
        WriteSegment(0xDA, p);
    }

    /// <summary>
    /// Encodes the entropy-coded data one MCU row at a time.
    /// </summary>
    /// <remarks>
    /// Converting the whole image to three floating point planes up front would cost twelve bytes per pixel, which is
    /// about 100 MB for a 4K image and enough to fail outright on a phone. Working a strip at a time keeps the colour
    /// conversion buffers proportional to the width instead of the whole frame.
    /// </remarks>
    private void WriteScan()
    {
        int w = _image.Width, h = _image.Height;
        var hFactor = _subsample ? 2 : 1;
        var mcuSize = 8 * hFactor;
        var mcusX = (w + mcuSize - 1) / mcuSize;
        var mcusY = (h + mcuSize - 1) / mcuSize;

        var strideY = mcusX * mcuSize;          // luma samples per row, MCU aligned
        var strideC = strideY / hFactor;        // chroma samples per row
        var rowsC = mcuSize / hFactor;          // chroma rows per MCU row

        var yStrip = new float[strideY * mcuSize];
        var cbStrip = new float[strideC * rowsC];
        var crStrip = new float[strideC * rowsC];
        var cbFull = hFactor == 1 ? cbStrip : new float[strideY * mcuSize];
        var crFull = hFactor == 1 ? crStrip : new float[strideY * mcuSize];

        var block = new float[64];
        var coeffs = new int[64];
        int dcY = 0, dcCb = 0, dcCr = 0;

        for (var my = 0; my < mcusY; my++)
        {
            _ct.ThrowIfCancellationRequested();
            LoadStrip(my * mcuSize, mcuSize, strideY, yStrip, cbFull, crFull);
            if (hFactor == 2) DownsampleChroma(cbFull, cbStrip, strideY, strideC, mcuSize, rowsC);
            if (hFactor == 2) DownsampleChroma(crFull, crStrip, strideY, strideC, mcuSize, rowsC);

            for (var mx = 0; mx < mcusX; mx++)
            {
                for (var by = 0; by < hFactor; by++)
                    for (var bx = 0; bx < hFactor; bx++)
                    {
                        Extract(yStrip, strideY, mcuSize, mx * mcuSize + bx * 8, by * 8, block);
                        EncodeBlock(block, _lumaScale, coeffs, ref dcY, _dcLuma, _acLuma);
                    }
                Extract(cbStrip, strideC, rowsC, mx * 8, 0, block);
                EncodeBlock(block, _chromaScale, coeffs, ref dcCb, _dcChroma, _acChroma);
                Extract(crStrip, strideC, rowsC, mx * 8, 0, block);
                EncodeBlock(block, _chromaScale, coeffs, ref dcCr, _dcChroma, _acChroma);
            }
        }
    }

    /// <summary>Converts one MCU-tall strip of the image to level-shifted Y, Cb and Cr, replicating edge pixels.</summary>
    private void LoadStrip(int startY, int rows, int stride, float[] y, float[] cb, float[] cr)
    {
        int w = _image.Width, h = _image.Height;
        for (var row = 0; row < rows; row++)
        {
            var source = _image.GetRow(Math.Min(startY + row, h - 1));
            var offset = row * stride;
            for (var x = 0; x < stride; x++)
            {
                var p = source[Math.Min(x, w - 1)];
                float r = p.R, g = p.G, b = p.B;
                if (p.A != 255)
                {
                    // Composite over the export background: JPEG has no alpha channel.
                    var a = p.A / 255f;
                    r = r * a + _background.R * (1 - a);
                    g = g * a + _background.G * (1 - a);
                    b = b * a + _background.B * (1 - a);
                }
                var i = offset + x;
                y[i] = 0.299f * r + 0.587f * g + 0.114f * b - 128f;
                cb[i] = -0.168736f * r - 0.331264f * g + 0.5f * b;
                cr[i] = 0.5f * r - 0.418688f * g - 0.081312f * b;
            }
        }
    }

    /// <summary>Averages 2x2 blocks of a full resolution chroma strip into the subsampled strip.</summary>
    private static void DownsampleChroma(float[] full, float[] target, int strideFull, int strideTarget, int rowsFull, int rowsTarget)
    {
        for (var y = 0; y < rowsTarget; y++)
        {
            var t = y * strideTarget;
            var a = (y * 2) * strideFull;
            var b = a + strideFull;
            for (var x = 0; x < strideTarget; x++)
            {
                var i = x * 2;
                target[t + x] = (full[a + i] + full[a + i + 1] + full[b + i] + full[b + i + 1]) * 0.25f;
            }
        }
    }

    private static void Extract(float[] plane, int stride, int height, int x0, int y0, float[] block)
    {
        for (var y = 0; y < 8; y++)
        {
            var sy = Math.Min(y0 + y, height - 1);
            for (var x = 0; x < 8; x++)
                block[y * 8 + x] = plane[sy * stride + Math.Min(x0 + x, stride - 1)];
        }
    }

    private void EncodeBlock(float[] block, float[] scale, int[] coeffs, ref int dcPred, (ushort Code, byte Length)[] dcTable, (ushort Code, byte Length)[] acTable)
    {
        Fdct.Transform(block);
        for (var i = 0; i < 64; i++)
        {
            var v = block[i] * scale[i];
            coeffs[i] = (int)(v < 0 ? v - 0.5f : v + 0.5f);
        }
        // DC
        var diff = coeffs[0] - dcPred;
        dcPred = coeffs[0];
        if (diff == 0) WriteBits(dcTable[0]);
        else
        {
            var (size, bits) = Magnitude(diff);
            WriteBits(dcTable[size]);
            WriteBits(bits, size);
        }
        // AC in zigzag order
        var run = 0;
        for (var k = 1; k < 64; k++)
        {
            var v = coeffs[JpegConstants.ZigZag[k]];
            if (v == 0) { run++; continue; }
            while (run > 15) { WriteBits(acTable[0xF0]); run -= 16; }
            var (size, bits) = Magnitude(v);
            WriteBits(acTable[(run << 4) | size]);
            WriteBits(bits, size);
            run = 0;
        }
        if (run > 0) WriteBits(acTable[0]);
    }

    private static (int Size, int Bits) Magnitude(int value)
    {
        var abs = Math.Abs(value);
        var size = 0;
        while (abs > 0) { size++; abs >>= 1; }
        var bits = value > 0 ? value : value - 1 + (1 << size);
        return (size, bits & ((1 << size) - 1));
    }

    private void WriteBits((ushort Code, byte Length) entry)
    {
        if (entry.Length == 0) throw new ImageEncodeException("Invalid Huffman code encountered while encoding.");
        WriteBits(entry.Code, entry.Length);
    }

    private void WriteBits(int value, int length)
    {
        if (length == 0) return;
        _bitBuf |= (uint)(value & ((1 << length) - 1)) << (32 - _bitCount - length);
        _bitCount += length;
        while (_bitCount >= 8)
        {
            var b = (byte)(_bitBuf >> 24);
            _out.WriteByte(b);
            if (b == 0xFF) _out.WriteByte(0);
            _bitBuf <<= 8;
            _bitCount -= 8;
        }
    }

    private void FlushBits()
    {
        // After WriteBits the buffer always holds fewer than 8 bits; pad with 1s to a byte boundary.
        if (_bitCount > 0)
        {
            var pad = 8 - _bitCount;
            WriteBits((1 << pad) - 1, pad);
        }
        _bitBuf = 0;
        _bitCount = 0;
    }
}

/// <summary>Float AAN forward DCT operating in place on an 8×8 block.</summary>
internal static class Fdct
{
    public static void Transform(float[] b)
    {
        // Rows
        for (var i = 0; i < 8; i++)
        {
            var o = i * 8;
            var t0 = b[o] + b[o + 7]; var t7 = b[o] - b[o + 7];
            var t1 = b[o + 1] + b[o + 6]; var t6 = b[o + 1] - b[o + 6];
            var t2 = b[o + 2] + b[o + 5]; var t5 = b[o + 2] - b[o + 5];
            var t3 = b[o + 3] + b[o + 4]; var t4 = b[o + 3] - b[o + 4];

            var t10 = t0 + t3; var t13 = t0 - t3;
            var t11 = t1 + t2; var t12 = t1 - t2;
            b[o] = t10 + t11;
            b[o + 4] = t10 - t11;
            var z1 = (t12 + t13) * 0.707106781f;
            b[o + 2] = t13 + z1;
            b[o + 6] = t13 - z1;

            t10 = t4 + t5; t11 = t5 + t6; t12 = t6 + t7;
            var z5 = (t10 - t12) * 0.382683433f;
            var z2 = 0.541196100f * t10 + z5;
            var z4 = 1.306562965f * t12 + z5;
            var z3 = t11 * 0.707106781f;
            var z11 = t7 + z3; var z13 = t7 - z3;
            b[o + 5] = z13 + z2;
            b[o + 3] = z13 - z2;
            b[o + 1] = z11 + z4;
            b[o + 7] = z11 - z4;
        }
        // Columns
        for (var i = 0; i < 8; i++)
        {
            var t0 = b[i] + b[56 + i]; var t7 = b[i] - b[56 + i];
            var t1 = b[8 + i] + b[48 + i]; var t6 = b[8 + i] - b[48 + i];
            var t2 = b[16 + i] + b[40 + i]; var t5 = b[16 + i] - b[40 + i];
            var t3 = b[24 + i] + b[32 + i]; var t4 = b[24 + i] - b[32 + i];

            var t10 = t0 + t3; var t13 = t0 - t3;
            var t11 = t1 + t2; var t12 = t1 - t2;
            b[i] = t10 + t11;
            b[32 + i] = t10 - t11;
            var z1 = (t12 + t13) * 0.707106781f;
            b[16 + i] = t13 + z1;
            b[48 + i] = t13 - z1;

            t10 = t4 + t5; t11 = t5 + t6; t12 = t6 + t7;
            var z5 = (t10 - t12) * 0.382683433f;
            var z2 = 0.541196100f * t10 + z5;
            var z4 = 1.306562965f * t12 + z5;
            var z3 = t11 * 0.707106781f;
            var z11 = t7 + z3; var z13 = t7 - z3;
            b[40 + i] = z13 + z2;
            b[24 + i] = z13 - z2;
            b[8 + i] = z11 + z4;
            b[56 + i] = z11 - z4;
        }
    }
}
