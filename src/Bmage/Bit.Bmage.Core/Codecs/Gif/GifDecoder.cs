using System.Buffers.Binary;
using Bit.Bmage.Memory;
using Bit.Bmage.Metadata;

namespace Bit.Bmage.Codecs.Gif;

/// <summary>Managed GIF decoder (first frame, LZW, interlacing, transparency). Animation frames beyond the first are ignored.</summary>
public sealed class GifDecoder : IImageDecoder
{
    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.Gif];
    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    public ImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        if (ImageFormats.Detect(data) != ImageFormat.Gif || data.Length < 13) return null;
        var w = BinaryPrimitives.ReadUInt16LittleEndian(data[6..]);
        var h = BinaryPrimitives.ReadUInt16LittleEndian(data[8..]);
        return w == 0 || h == 0 ? null : new ImageInfo(w, h, ImageFormat.Gif, HasAlpha: null);
    }

    public ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(Decode(data.Span, options, allocator, cancellationToken));

    public static ImageBuffer Decode(ReadOnlySpan<byte> data, DecodeOptions? options = null, IPixelAllocator? allocator = null, CancellationToken cancellationToken = default)
    {
        options ??= DecodeOptions.Default;
        allocator ??= PooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        if (ImageFormats.Detect(data) != ImageFormat.Gif || data.Length < 13) throw new ImageDecodeException("Not a GIF file.");
        int width = BinaryPrimitives.ReadUInt16LittleEndian(data[6..]);
        int height = BinaryPrimitives.ReadUInt16LittleEndian(data[8..]);
        if (width == 0 || height == 0) throw new ImageDecodeException("Invalid GIF dimensions.");
        options.Limits.Validate(width, height);
        var flags = data[10];
        var bgIndex = data[11];
        var pos = 13;
        ReadOnlySpan<byte> globalPalette = default;
        if ((flags & 0x80) != 0)
        {
            var size = 3 * (1 << ((flags & 7) + 1));
            if (pos + size > data.Length) throw new ImageDecodeException("Truncated GIF palette.");
            globalPalette = data.Slice(pos, size);
            pos += size;
        }

        var transparentIndex = -1;
        var image = ImageBuffer.Create(width, height, allocator, clear: true, options.Limits);
        try
        {
            while (pos < data.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var block = data[pos++];
                if (block == 0x3B) break; // trailer
                if (block == 0x21)
                {
                    if (pos >= data.Length) break;
                    var label = data[pos++];
                    if (label == 0xF9 && pos + 5 < data.Length && data[pos] == 4)
                    {
                        var packed = data[pos + 1];
                        if ((packed & 1) != 0) transparentIndex = data[pos + 4];
                    }
                    pos = SkipSubBlocks(data, pos);
                    continue;
                }
                if (block != 0x2C) throw new ImageDecodeException($"Unexpected GIF block 0x{block:X2}.");
                if (pos + 9 > data.Length) throw new ImageDecodeException("Truncated GIF image descriptor.");
                int left = BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]);
                int top = BinaryPrimitives.ReadUInt16LittleEndian(data[(pos + 2)..]);
                int fw = BinaryPrimitives.ReadUInt16LittleEndian(data[(pos + 4)..]);
                int fh = BinaryPrimitives.ReadUInt16LittleEndian(data[(pos + 6)..]);
                var iflags = data[pos + 8];
                pos += 9;
                var palette = globalPalette;
                if ((iflags & 0x80) != 0)
                {
                    var size = 3 * (1 << ((iflags & 7) + 1));
                    if (pos + size > data.Length) throw new ImageDecodeException("Truncated GIF local palette.");
                    palette = data.Slice(pos, size);
                    pos += size;
                }
                if (palette.IsEmpty) throw new ImageDecodeException("GIF has no colour table.");
                var interlaced = (iflags & 0x40) != 0;
                if (pos >= data.Length) throw new ImageDecodeException("Truncated GIF.");
                var minCodeSize = data[pos++];
                if (minCodeSize is < 2 or > 11) throw new ImageDecodeException("Invalid GIF LZW code size.");
                var lzwData = ReadSubBlocks(data, ref pos);
                if ((long)fw * fh > options.Limits.MaxPixels) throw new ImageLimitExceededException("GIF frame too large.");
                var indices = new byte[fw * fh];
                Lzw.Decode(lzwData, minCodeSize, indices);

                // Composite frame into the canvas.
                for (var fy = 0; fy < fh; fy++)
                {
                    var y = top + (interlaced ? Deinterlace(fy, fh) : fy);
                    if (y < 0 || y >= height) continue;
                    var dst = image.GetRow(y);
                    for (var fx = 0; fx < fw; fx++)
                    {
                        var x = left + fx;
                        if (x < 0 || x >= width) continue;
                        int idx = indices[fy * fw + fx];
                        if (idx == transparentIndex) continue;
                        var pi = idx * 3;
                        if (pi + 2 >= palette.Length) continue;
                        dst[x] = new Rgba32(palette[pi], palette[pi + 1], palette[pi + 2]);
                    }
                }
                break; // first frame only
            }
            _ = bgIndex;
            image.Metadata = new ImageMetadata(sourceFormat: ImageFormat.Gif, fileName: options.FileName);
            return image;
        }
        catch { image.Dispose(); throw; }
    }

    private static int Deinterlace(int row, int height)
    {
        // Pass 1: rows 0,8,16..; pass 2: 4,12..; pass 3: 2,6,10..; pass 4: 1,3,5..
        var p1 = (height + 7) / 8;
        var p2 = (height + 3) / 8;
        var p3 = (height + 1) / 4;
        if (row < p1) return row * 8;
        row -= p1;
        if (row < p2) return row * 8 + 4;
        row -= p2;
        if (row < p3) return row * 4 + 2;
        row -= p3;
        return row * 2 + 1;
    }

    private static int SkipSubBlocks(ReadOnlySpan<byte> data, int pos)
    {
        while (pos < data.Length)
        {
            var len = data[pos++];
            if (len == 0) break;
            pos += len;
        }
        return pos;
    }

    private static byte[] ReadSubBlocks(ReadOnlySpan<byte> data, ref int pos)
    {
        var ms = new MemoryStream();
        while (pos < data.Length)
        {
            var len = data[pos++];
            if (len == 0) break;
            if (pos + len > data.Length) len = (byte)(data.Length - pos);
            ms.Write(data.Slice(pos, len));
            pos += len;
        }
        return ms.ToArray();
    }

    private static class Lzw
    {
        public static void Decode(ReadOnlySpan<byte> input, int minCodeSize, Span<byte> output)
        {
            var clearCode = 1 << minCodeSize;
            var endCode = clearCode + 1;
            var codeSize = minCodeSize + 1;
            var nextCode = endCode + 1;
            var prefix = new int[4096];
            var suffix = new byte[4096];
            var stack = new byte[4097];
            for (var i = 0; i < clearCode; i++) { prefix[i] = -1; suffix[i] = (byte)i; }
            var outPos = 0;
            var prev = -1;
            uint bitBuf = 0; var bitCount = 0; var inPos = 0;
            var first = (byte)0;
            while (outPos < output.Length)
            {
                while (bitCount < codeSize)
                {
                    if (inPos >= input.Length) return; // truncated: leave the remainder as-is
                    bitBuf |= (uint)input[inPos++] << bitCount;
                    bitCount += 8;
                }
                var code = (int)(bitBuf & ((1u << codeSize) - 1));
                bitBuf >>= codeSize;
                bitCount -= codeSize;
                if (code == clearCode)
                {
                    codeSize = minCodeSize + 1;
                    nextCode = endCode + 1;
                    prev = -1;
                    continue;
                }
                if (code == endCode) return;
                int c;
                var sp = 0;
                if (prev == -1)
                {
                    if (code >= clearCode) throw new ImageDecodeException("Corrupt GIF LZW stream.");
                    output[outPos++] = suffix[code];
                    prev = code; first = suffix[code];
                    continue;
                }
                if (code < nextCode)
                {
                    c = code;
                }
                else if (code == nextCode)
                {
                    // KwKwK case
                    c = prev;
                    stack[sp++] = first;
                }
                else throw new ImageDecodeException("Corrupt GIF LZW stream.");
                while (c >= clearCode)
                {
                    stack[sp++] = suffix[c];
                    c = prefix[c];
                    if (sp >= stack.Length) throw new ImageDecodeException("Corrupt GIF LZW stream.");
                }
                first = suffix[c];
                stack[sp++] = first;
                while (sp > 0 && outPos < output.Length) output[outPos++] = stack[--sp];
                if (nextCode < 4096)
                {
                    prefix[nextCode] = prev;
                    suffix[nextCode] = first;
                    nextCode++;
                    if (nextCode == (1 << codeSize) && codeSize < 12) codeSize++;
                }
                prev = code;
            }
        }
    }
}
