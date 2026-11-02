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

    /// <summary>
    /// The size browsers decode the image at: the logical screen, grown to take in a first frame that overhangs it, where
    /// this decoder clips the frame instead. Null when neither gives a size.
    /// </summary>
    internal static (int Width, int Height)? BrowserCanvasSize(ReadOnlySpan<byte> data)
    {
        if (ImageFormats.Detect(data) != ImageFormat.Gif || data.Length < 13) return null;
        int width = BinaryPrimitives.ReadUInt16LittleEndian(data[6..]);
        int height = BinaryPrimitives.ReadUInt16LittleEndian(data[8..]);
        var pos = 13;
        if ((data[10] & 0x80) != 0) pos += 3 * (1 << ((data[10] & 7) + 1));
        while (pos < data.Length)
        {
            var block = data[pos++];
            if (block == 0x21)
            {
                pos = SkipSubBlocks(data, pos + 1);
                continue;
            }
            if (block != 0x2C || pos + 9 > data.Length) break;
            width = Math.Max(width, BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]) + BinaryPrimitives.ReadUInt16LittleEndian(data[(pos + 4)..]));
            height = Math.Max(height, BinaryPrimitives.ReadUInt16LittleEndian(data[(pos + 2)..]) + BinaryPrimitives.ReadUInt16LittleEndian(data[(pos + 6)..]));
            break;
        }
        return width == 0 || height == 0 ? null : (width, height);
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
                // A frame may overhang the logical screen. Browsers draw such files, so the overhang is clipped below
                // rather than the file refused; the frame's own size is still held to the limits, which bound the
                // work of decoding it, and it is decoded a row at a time, so the overhang is never stored.
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
                if (fw > 0 && fh > 0) options.Limits.Validate(fw, fh);
                var lzw = new Lzw(lzwData, minCodeSize);
                var indices = new byte[fw];

                // Composite frame into the canvas.
                for (var fy = 0; fy < fh; fy++)
                {
                    // Rows arrive top to bottom unless interlaced, so once one falls below the screen the rest do too.
                    if (!interlaced && top + fy >= height) break;
                    // A stream that ends early leaves the rest of the frame at index 0.
                    indices.AsSpan(lzw.Read(indices)).Clear();
                    var y = top + (interlaced ? Deinterlace(fy, fh) : fy);
                    if (y < 0 || y >= height) continue;
                    var dst = image.GetRow(y);
                    for (var fx = 0; fx < fw; fx++)
                    {
                        var x = left + fx;
                        if (x < 0 || x >= width) continue;
                        int idx = indices[fx];
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

    /// <summary>An LZW decoder that hands out its indices a row at a time, so a frame never needs a buffer of its own.</summary>
    private sealed class Lzw
    {
        private readonly byte[] _input;
        private readonly int _minCodeSize, _clearCode, _endCode;
        private readonly int[] _prefix = new int[4096];
        private readonly byte[] _suffix = new byte[4096];
        private readonly byte[] _stack = new byte[4097];
        private int _sp, _codeSize, _nextCode, _prev = -1, _inPos, _bitCount;
        private uint _bitBuf;
        private byte _first;
        private bool _ended;

        public Lzw(byte[] input, int minCodeSize)
        {
            _input = input;
            _minCodeSize = minCodeSize;
            _clearCode = 1 << minCodeSize;
            _endCode = _clearCode + 1;
            _codeSize = minCodeSize + 1;
            _nextCode = _endCode + 1;
            for (var i = 0; i < _clearCode; i++) { _prefix[i] = -1; _suffix[i] = (byte)i; }
        }

        /// <summary>Fills <paramref name="output"/> with the next indices; fewer come back only once the stream has ended.</summary>
        public int Read(Span<byte> output)
        {
            var outPos = 0;
            // What is left of a code that did not fit the previous row.
            while (_sp > 0 && outPos < output.Length) output[outPos++] = _stack[--_sp];
            while (outPos < output.Length && !_ended)
            {
                while (_bitCount < _codeSize)
                {
                    // Truncated: the caller leaves the remainder as-is.
                    if (_inPos >= _input.Length) { _ended = true; return outPos; }
                    _bitBuf |= (uint)_input[_inPos++] << _bitCount;
                    _bitCount += 8;
                }
                var code = (int)(_bitBuf & ((1u << _codeSize) - 1));
                _bitBuf >>= _codeSize;
                _bitCount -= _codeSize;
                if (code == _clearCode)
                {
                    _codeSize = _minCodeSize + 1;
                    _nextCode = _endCode + 1;
                    _prev = -1;
                    continue;
                }
                if (code == _endCode) { _ended = true; break; }
                int c;
                if (_prev == -1)
                {
                    if (code >= _clearCode) throw new ImageDecodeException("Corrupt GIF LZW stream.");
                    output[outPos++] = _suffix[code];
                    _prev = code; _first = _suffix[code];
                    continue;
                }
                if (code < _nextCode)
                {
                    c = code;
                }
                else if (code == _nextCode)
                {
                    // KwKwK case
                    c = _prev;
                    _stack[_sp++] = _first;
                }
                else throw new ImageDecodeException("Corrupt GIF LZW stream.");
                while (c >= _clearCode)
                {
                    _stack[_sp++] = _suffix[c];
                    c = _prefix[c];
                    if (_sp >= _stack.Length) throw new ImageDecodeException("Corrupt GIF LZW stream.");
                }
                _first = _suffix[c];
                _stack[_sp++] = _first;
                while (_sp > 0 && outPos < output.Length) output[outPos++] = _stack[--_sp];
                if (_nextCode < 4096)
                {
                    _prefix[_nextCode] = _prev;
                    _suffix[_nextCode] = _first;
                    _nextCode++;
                    if (_nextCode == (1 << _codeSize) && _codeSize < 12) _codeSize++;
                }
                _prev = code;
            }
            return outPos;
        }
    }
}
