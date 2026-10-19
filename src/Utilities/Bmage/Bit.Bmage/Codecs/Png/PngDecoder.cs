using System.Buffers.Binary;
using System.IO.Compression;
using Bit.Bmage.Geometry;
using Bit.Bmage.Memory;
using Bit.Bmage.Metadata;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Codecs.Png;

/// <summary>Managed PNG decoder supporting all colour types, bit depths 1–16, palettes, tRNS transparency and Adam7 interlacing.</summary>
public sealed class PngDecoder : IImageDecoder
{
    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.Png];
    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    public ImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        if (ImageFormats.Detect(data) != ImageFormat.Png || data.Length < 33) return null;
        var chunks = PngContainer.EnumerateChunks(data);
        if (chunks.Count == 0 || chunks[0].TypeName != "IHDR" || chunks[0].DataLength < 13) return null;
        var ihdr = data.Slice(chunks[0].DataOffset, 13);
        var w = BinaryPrimitives.ReadInt32BigEndian(ihdr);
        var h = BinaryPrimitives.ReadInt32BigEndian(ihdr[4..]);
        var colorType = ihdr[9];
        var hasAlpha = colorType is 4 or 6 || chunks.Any(c => c.TypeName == "tRNS");
        var orientation = Orientation.Normal;
        foreach (var c in chunks)
        {
            if (c.TypeName == "eXIf") { orientation = ExifReader.Read(data.Slice(c.DataOffset, c.DataLength))?.Orientation ?? Orientation.Normal; break; }
            if (c.TypeName == "IDAT") break;
        }
        return w <= 0 || h <= 0 ? null : new ImageInfo(w, h, ImageFormat.Png, orientation, hasAlpha);
    }

    public ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(Decode(data.Span, options, allocator, cancellationToken));

    public static ImageBuffer Decode(ReadOnlySpan<byte> data, DecodeOptions? options = null, IPixelAllocator? allocator = null, CancellationToken cancellationToken = default)
    {
        options ??= DecodeOptions.Default;
        allocator ??= PooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        var chunks = PngContainer.EnumerateChunks(data);
        if (chunks.Count == 0 || chunks[0].TypeName != "IHDR" || chunks[0].DataLength < 13)
            throw new ImageDecodeException("Invalid PNG: missing IHDR.");
        var ihdr = data.Slice(chunks[0].DataOffset, 13);
        var width = BinaryPrimitives.ReadInt32BigEndian(ihdr);
        var height = BinaryPrimitives.ReadInt32BigEndian(ihdr[4..]);
        int bitDepth = ihdr[8], colorType = ihdr[9], compression = ihdr[10], filterMethod = ihdr[11], interlace = ihdr[12];
        if (width <= 0 || height <= 0) throw new ImageDecodeException("Invalid PNG dimensions.");
        options.Limits.Validate(width, height);
        if (compression != 0 || filterMethod != 0 || interlace > 1) throw new ImageDecodeException("Unsupported PNG compression/filter/interlace method.");
        var channels = colorType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => throw new ImageDecodeException($"Invalid PNG colour type {colorType}.") };
        var validDepth = colorType switch
        {
            0 => bitDepth is 1 or 2 or 4 or 8 or 16,
            3 => bitDepth is 1 or 2 or 4 or 8,
            _ => bitDepth is 8 or 16,
        };
        if (!validDepth) throw new ImageDecodeException($"Invalid PNG bit depth {bitDepth} for colour type {colorType}.");

        ReadOnlySpan<byte> palette = default, trns = default;
        var idatLength = 0;
        var idatChunks = new List<PngContainer.Chunk>();
        foreach (var c in chunks)
        {
            switch (c.TypeName)
            {
                case "PLTE": palette = data.Slice(c.DataOffset, c.DataLength); break;
                case "tRNS": trns = data.Slice(c.DataOffset, c.DataLength); break;
                case "IDAT": idatChunks.Add(c); idatLength += c.DataLength; break;
            }
        }
        if (idatChunks.Count == 0) throw new ImageDecodeException("Invalid PNG: no image data.");
        if (colorType == 3 && palette.IsEmpty) throw new ImageDecodeException("Invalid PNG: palette missing.");

        // Concatenate IDAT and inflate to exactly the expected size.
        var compressed = new byte[idatLength];
        var p = 0;
        foreach (var c in idatChunks) { data.Slice(c.DataOffset, c.DataLength).CopyTo(compressed.AsSpan(p)); p += c.DataLength; }
        var bitsPerPixel = channels * bitDepth;
        long rawSize = interlace == 0
            ? (long)height * (1 + ((long)width * bitsPerPixel + 7) / 8)
            : Adam7RawSize(width, height, bitsPerPixel);
        if (rawSize > int.MaxValue) throw new ImageLimitExceededException("PNG raw data too large.");
        // Deflate cannot expand data by more than about 1032:1, so a header that promises far more than the compressed
        // data could hold is lying. Checking first stops a tiny file from forcing a huge allocation.
        if (rawSize > (long)idatLength * 1032 + 1024) throw new ImageDecodeException("Truncated PNG image data.");
        var raw = new byte[rawSize];
        try
        {
            using var zs = new ZLibStream(new MemoryStream(compressed, writable: false), CompressionMode.Decompress);
            var read = 0;
            while (read < raw.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var n = zs.Read(raw, read, Math.Min(raw.Length - read, 1 << 20));
                if (n <= 0) break;
                read += n;
            }
            if (read < raw.Length) throw new ImageDecodeException("Truncated PNG image data.");
        }
        catch (InvalidDataException ex)
        {
            throw new ImageDecodeException("Corrupt PNG image data.", ex);
        }

        var image = ImageBuffer.Create(width, height, allocator, clear: false, options.Limits);
        try
        {
            var converter = new PixelConverter(colorType, bitDepth, palette, trns);
            if (interlace == 0)
            {
                var stride = (int)(((long)width * bitsPerPixel + 7) / 8);
                var bpp = Math.Max(1, bitsPerPixel / 8);
                var prev = new byte[stride];
                var cur = new byte[stride];
                var pos = 0;
                for (var y = 0; y < height; y++)
                {
                    if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
                    var filter = raw[pos++];
                    raw.AsSpan(pos, stride).CopyTo(cur);
                    pos += stride;
                    Unfilter(filter, cur, prev, bpp);
                    converter.ConvertRow(cur, image.GetRow(y), width, 0, 1);
                    (prev, cur) = (cur, prev);
                }
            }
            else
            {
                DecodeAdam7(raw, image, width, height, bitsPerPixel, converter, cancellationToken);
            }

            if (options.ReadMetadata)
            {
                var md = PngContainer.ReadMetadata(data, options.FileName);
                image.Metadata = md;
                if (options.AutoOrient && md.Orientation is not (Orientation.Normal or Orientation.Unspecified))
                {
                    var oriented = new OrientationOperation(md.Orientation).Apply(image, new Operations.OperationContext(allocator, options.Limits, cancellationToken, canMutateSource: true));
                    if (!ReferenceEquals(oriented, image)) image.Dispose();
                    image = oriented;
                    image.Metadata = md.WithOrientation(Orientation.Normal);
                }
            }
            else image.Metadata = new ImageMetadata(sourceFormat: ImageFormat.Png, fileName: options.FileName);
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }

    private static readonly int[] Adam7StartX = [0, 4, 0, 2, 0, 1, 0];
    private static readonly int[] Adam7StartY = [0, 0, 4, 0, 2, 0, 1];
    private static readonly int[] Adam7StepX = [8, 8, 4, 4, 2, 2, 1];
    private static readonly int[] Adam7StepY = [8, 8, 8, 4, 4, 2, 2];

    private static long Adam7RawSize(int width, int height, int bitsPerPixel)
    {
        long total = 0;
        for (var pass = 0; pass < 7; pass++)
        {
            var pw = (width - Adam7StartX[pass] + Adam7StepX[pass] - 1) / Adam7StepX[pass];
            var ph = (height - Adam7StartY[pass] + Adam7StepY[pass] - 1) / Adam7StepY[pass];
            if (pw <= 0 || ph <= 0) continue;
            total += (long)ph * (1 + ((long)pw * bitsPerPixel + 7) / 8);
        }
        return total;
    }

    private static void DecodeAdam7(byte[] raw, ImageBuffer image, int width, int height, int bitsPerPixel, PixelConverter converter, CancellationToken ct)
    {
        var pos = 0;
        var bpp = Math.Max(1, bitsPerPixel / 8);
        for (var pass = 0; pass < 7; pass++)
        {
            var pw = (width - Adam7StartX[pass] + Adam7StepX[pass] - 1) / Adam7StepX[pass];
            var ph = (height - Adam7StartY[pass] + Adam7StepY[pass] - 1) / Adam7StepY[pass];
            if (pw <= 0 || ph <= 0) continue;
            var stride = (int)(((long)pw * bitsPerPixel + 7) / 8);
            var prev = new byte[stride];
            var cur = new byte[stride];
            for (var py = 0; py < ph; py++)
            {
                ct.ThrowIfCancellationRequested();
                var filter = raw[pos++];
                raw.AsSpan(pos, stride).CopyTo(cur);
                pos += stride;
                Unfilter(filter, cur, prev, bpp);
                var y = Adam7StartY[pass] + py * Adam7StepY[pass];
                converter.ConvertRow(cur, image.GetRow(y), pw, Adam7StartX[pass], Adam7StepX[pass]);
                (prev, cur) = (cur, prev);
            }
        }
    }

    internal static void Unfilter(byte filter, Span<byte> cur, ReadOnlySpan<byte> prev, int bpp)
    {
        switch (filter)
        {
            case 0: break;
            case 1:
                for (var i = bpp; i < cur.Length; i++) cur[i] += cur[i - bpp];
                break;
            case 2:
                for (var i = 0; i < cur.Length; i++) cur[i] += prev[i];
                break;
            case 3:
                for (var i = 0; i < cur.Length; i++)
                {
                    var left = i >= bpp ? cur[i - bpp] : 0;
                    cur[i] += (byte)((left + prev[i]) >> 1);
                }
                break;
            case 4:
                for (var i = 0; i < cur.Length; i++)
                {
                    int a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
                    var pp = a + b - c;
                    var pa = Math.Abs(pp - a); var pb = Math.Abs(pp - b); var pc = Math.Abs(pp - c);
                    cur[i] += (byte)(pa <= pb && pa <= pc ? a : pb <= pc ? b : c);
                }
                break;
            default:
                throw new ImageDecodeException($"Invalid PNG filter type {filter}.");
        }
    }

    /// <summary>Converts unfiltered scanline bytes of any PNG pixel format into RGBA32.</summary>
    private readonly ref struct PixelConverter
    {
        private readonly int _colorType, _bitDepth;
        private readonly ReadOnlySpan<byte> _palette, _trns;
        private readonly int _trnsGray, _trnsR, _trnsG, _trnsB;

        public PixelConverter(int colorType, int bitDepth, ReadOnlySpan<byte> palette, ReadOnlySpan<byte> trns)
        {
            _colorType = colorType; _bitDepth = bitDepth; _palette = palette; _trns = trns;
            _trnsGray = _trnsR = _trnsG = _trnsB = -1;
            if (colorType == 0 && trns.Length >= 2) _trnsGray = BinaryPrimitives.ReadUInt16BigEndian(trns);
            if (colorType == 2 && trns.Length >= 6)
            {
                _trnsR = BinaryPrimitives.ReadUInt16BigEndian(trns);
                _trnsG = BinaryPrimitives.ReadUInt16BigEndian(trns[2..]);
                _trnsB = BinaryPrimitives.ReadUInt16BigEndian(trns[4..]);
            }
        }

        public void ConvertRow(ReadOnlySpan<byte> src, Span<Rgba32> dst, int count, int startX, int stepX)
        {
            var x = startX;
            switch (_colorType)
            {
                case 0: // grayscale
                    for (var i = 0; i < count; i++, x += stepX)
                    {
                        var raw = ReadSample(src, i, 0, 1);
                        var g = Scale(raw);
                        var a = raw == _trnsGray ? (byte)0 : (byte)255;
                        dst[x] = new Rgba32(g, g, g, a);
                    }
                    break;
                case 2: // RGB
                    for (var i = 0; i < count; i++, x += stepX)
                    {
                        var r = ReadSample(src, i, 0, 3); var g = ReadSample(src, i, 1, 3); var b = ReadSample(src, i, 2, 3);
                        var a = r == _trnsR && g == _trnsG && b == _trnsB ? (byte)0 : (byte)255;
                        dst[x] = new Rgba32(Scale(r), Scale(g), Scale(b), a);
                    }
                    break;
                case 3: // palette
                    for (var i = 0; i < count; i++, x += stepX)
                    {
                        var idx = ReadSample(src, i, 0, 1);
                        var pi = idx * 3;
                        if (pi + 2 >= _palette.Length) { dst[x] = Rgba32.Transparent; continue; }
                        var a = idx < _trns.Length ? _trns[idx] : (byte)255;
                        dst[x] = new Rgba32(_palette[pi], _palette[pi + 1], _palette[pi + 2], a);
                    }
                    break;
                case 4: // gray + alpha
                    for (var i = 0; i < count; i++, x += stepX)
                    {
                        var g = Scale(ReadSample(src, i, 0, 2));
                        var a = Scale(ReadSample(src, i, 1, 2));
                        dst[x] = new Rgba32(g, g, g, a);
                    }
                    break;
                case 6: // RGBA
                    if (_bitDepth == 8)
                    {
                        for (var i = 0; i < count; i++, x += stepX)
                        {
                            var o = i * 4;
                            dst[x] = new Rgba32(src[o], src[o + 1], src[o + 2], src[o + 3]);
                        }
                    }
                    else
                    {
                        for (var i = 0; i < count; i++, x += stepX)
                            dst[x] = new Rgba32(Scale(ReadSample(src, i, 0, 4)), Scale(ReadSample(src, i, 1, 4)), Scale(ReadSample(src, i, 2, 4)), Scale(ReadSample(src, i, 3, 4)));
                    }
                    break;
            }
        }

        private int ReadSample(ReadOnlySpan<byte> src, int pixel, int channel, int channels)
        {
            switch (_bitDepth)
            {
                case 8: return src[pixel * channels + channel];
                case 16: return BinaryPrimitives.ReadUInt16BigEndian(src[((pixel * channels + channel) * 2)..]);
                default:
                {
                    var bitIndex = (pixel * channels + channel) * _bitDepth;
                    var b = src[bitIndex >> 3];
                    var shift = 8 - _bitDepth - (bitIndex & 7);
                    return (b >> shift) & ((1 << _bitDepth) - 1);
                }
            }
        }

        private byte Scale(int sample) => _bitDepth switch
        {
            8 => (byte)sample,
            16 => (byte)((sample * 255 + 32767) / 65535),
            _ => (byte)(sample * 255 / ((1 << _bitDepth) - 1)),
        };
    }
}
