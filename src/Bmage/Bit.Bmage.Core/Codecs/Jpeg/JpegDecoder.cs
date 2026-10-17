using System.Buffers.Binary;
using Bit.Bmage.Geometry;
using Bit.Bmage.Memory;
using Bit.Bmage.Metadata;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Codecs.Jpeg;

/// <summary>
/// Managed JPEG decoder supporting baseline and progressive Huffman-coded images, 8-bit precision, grayscale / YCbCr / RGB /
/// CMYK / YCCK colour, arbitrary chroma subsampling, restart intervals and EXIF orientation.
/// </summary>
public sealed class JpegDecoder : IImageDecoder
{
    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.Jpeg];
    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    public ImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        if (ImageFormats.Detect(data) != ImageFormat.Jpeg) return null;
        foreach (var seg in JpegContainer.EnumerateSegments(data))
        {
            if (seg.Marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
            {
                if (seg.PayloadLength < 6) return null;
                var p = data.Slice(seg.PayloadOffset, seg.PayloadLength);
                var h = BinaryPrimitives.ReadUInt16BigEndian(p[1..]);
                var w = BinaryPrimitives.ReadUInt16BigEndian(p[3..]);
                var orientation = JpegContainer.ReadOrientation(data);
                return w == 0 || h == 0 ? null : new ImageInfo(w, h, ImageFormat.Jpeg, orientation, HasAlpha: false);
            }
        }
        return null;
    }

    public ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(Decode(data.Span, options, allocator, cancellationToken));

    public static ImageBuffer Decode(ReadOnlySpan<byte> data, DecodeOptions? options = null, IPixelAllocator? allocator = null, CancellationToken cancellationToken = default)
    {
        options ??= DecodeOptions.Default;
        allocator ??= PooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        var core = new JpegDecoderCore(data, options, cancellationToken);
        var image = core.Decode(allocator);
        try
        {
            if (options.ReadMetadata)
            {
                var md = JpegContainer.ReadMetadata(data, options.FileName);
                image.Metadata = md;
                if (options.AutoOrient && md.Orientation is not (Orientation.Normal or Orientation.Unspecified))
                {
                    var oriented = new OrientationOperation(md.Orientation).Apply(image, new Operations.OperationContext(allocator, options.Limits, cancellationToken, canMutateSource: true));
                    if (!ReferenceEquals(oriented, image)) image.Dispose();
                    image = oriented;
                    image.Metadata = md.WithOrientation(Orientation.Normal);
                }
            }
            else image.Metadata = new ImageMetadata(sourceFormat: ImageFormat.Jpeg, fileName: options.FileName);
            return image;
        }
        catch { image.Dispose(); throw; }
    }
}

internal static class JpegConstants
{
    /// <summary>Natural-order index for each zigzag position.</summary>
    public static readonly int[] ZigZag =
    [
        0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5, 12, 19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51, 58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63,
    ];
}

internal sealed class HuffmanTable
{
    // Fast lookup for codes up to 9 bits: (length << 8) | value, 0 when the code is longer.
    public readonly ushort[] Lookup = new ushort[512];
    public readonly int[] MaxCode = new int[18];
    public readonly int[] ValPtr = new int[17];
    public readonly int[] MinCode = new int[17];
    public readonly byte[] Values;

    public HuffmanTable(ReadOnlySpan<byte> bits, ReadOnlySpan<byte> values)
    {
        Values = values.ToArray();
        var code = 0;
        var k = 0;
        for (var len = 1; len <= 16; len++)
        {
            var n = bits[len - 1];
            ValPtr[len] = k;
            MinCode[len] = code;
            if (n == 0) { MaxCode[len] = -1; }
            else
            {
                // More codes than this length can hold means the table is not a prefix code; trusting it would index
                // past the lookup table.
                if (code + n > 1 << len) throw new ImageDecodeException("Invalid JPEG Huffman table.");
                for (var i = 0; i < n; i++)
                {
                    if (len <= 9)
                    {
                        var shift = 9 - len;
                        var start = code << shift;
                        for (var j = 0; j < (1 << shift); j++)
                            Lookup[start + j] = (ushort)((len << 8) | Values[k]);
                    }
                    code++; k++;
                }
                MaxCode[len] = code - 1;
            }
            code <<= 1;
        }
        MaxCode[17] = int.MaxValue;
    }
}

internal sealed class JpegDecoderCore
{
    private sealed class Component
    {
        public int Id, H, V, Tq;
        public int BlocksPerLine, BlocksPerColumn;      // actual blocks covering the component
        public int PaddedBlocksPerLine, PaddedBlocksPerColumn; // MCU-aligned
        public int DcTable, AcTable, DcPred;
        public short[]? Coeffs;   // progressive: PaddedBlocksPerLine*PaddedBlocksPerColumn*64
        public byte[] Plane = [];  // decoded samples, PaddedBlocksPerLine*8 wide
        public int PlaneStride;
    }

    private readonly ReadOnlyMemory<byte> _data;
    private readonly DecodeOptions _options;
    private readonly CancellationToken _ct;
    private int _pos;
    private readonly ushort[][] _qt = new ushort[4][];
    private readonly HuffmanTable?[] _dc = new HuffmanTable?[4];
    private readonly HuffmanTable?[] _ac = new HuffmanTable?[4];
    private Component[] _components = [];
    private int _width, _height, _maxH, _maxV, _mcusPerLine, _mcusPerColumn, _restartInterval;
    private bool _progressive, _frameSeen, _adobe, _jfif;
    private int _adobeTransform = -1;
    private int _eobRun;

    // Bit reader
    private uint _bitBuf;
    private int _bitCount;
    private bool _markerHit;

    public JpegDecoderCore(ReadOnlySpan<byte> data, DecodeOptions options, CancellationToken ct)
    {
        _data = data.ToArray();
        _options = options;
        _ct = ct;
    }

    private ReadOnlySpan<byte> Data => _data.Span;

    public ImageBuffer Decode(IPixelAllocator allocator)
    {
        var d = Data;
        if (d.Length < 4 || d[0] != 0xFF || d[1] != 0xD8) throw new ImageDecodeException("Not a JPEG file.");
        _pos = 2;
        var scans = 0;
        while (_pos < d.Length)
        {
            _ct.ThrowIfCancellationRequested();
            // Find next marker.
            if (d[_pos] != 0xFF) { _pos++; continue; }
            while (_pos < d.Length && d[_pos] == 0xFF) _pos++;
            if (_pos >= d.Length) break;
            var marker = d[_pos++];
            if (marker == 0xD9) break; // EOI
            if (marker is 0x00 or 0x01 or (>= 0xD0 and <= 0xD7)) continue;
            if (_pos + 2 > d.Length) throw new ImageDecodeException("Truncated JPEG segment.");
            int len = BinaryPrimitives.ReadUInt16BigEndian(d[_pos..]);
            if (len < 2 || _pos + len > d.Length) throw new ImageDecodeException("Truncated JPEG segment.");
            var seg = d.Slice(_pos + 2, len - 2);
            var segEnd = _pos + len;
            switch (marker)
            {
                case 0xC0 or 0xC1 or 0xC2: ReadFrame(seg, marker == 0xC2); break;
                case 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF:
                    throw new ImageDecodeException("Unsupported JPEG process (lossless, hierarchical or arithmetic coding).");
                case 0xC4: ReadHuffmanTables(seg); break;
                case 0xDB: ReadQuantTables(seg); break;
                case 0xDD:
                    if (seg.Length >= 2) _restartInterval = BinaryPrimitives.ReadUInt16BigEndian(seg);
                    break;
                case 0xE0:
                    if (seg.Length >= 5 && seg[0] == 'J' && seg[1] == 'F' && seg[2] == 'I' && seg[3] == 'F') _jfif = true;
                    break;
                case 0xEE:
                    if (seg.Length >= 12 && seg[0] == 'A' && seg[1] == 'd' && seg[2] == 'o' && seg[3] == 'b' && seg[4] == 'e') { _adobe = true; _adobeTransform = seg[11]; }
                    break;
                case 0xDA:
                    if (!_frameSeen) throw new ImageDecodeException("JPEG scan before frame header.");
                    _pos = segEnd;
                    ReadScan(seg);
                    scans++;
                    continue; // _pos already advanced past entropy data
            }
            _pos = segEnd;
        }
        if (!_frameSeen || scans == 0) throw new ImageDecodeException("JPEG contains no image data.");

        if (_progressive)
        {
            foreach (var c in _components)
            {
                var q = _qt[c.Tq] ?? throw new ImageDecodeException("Missing quantisation table.");
                var coeffs = c.Coeffs!;
                var block = new int[64];
                for (var by = 0; by < c.PaddedBlocksPerColumn; by++)
                {
                    _ct.ThrowIfCancellationRequested();
                    for (var bx = 0; bx < c.PaddedBlocksPerLine; bx++)
                    {
                        var off = (by * c.PaddedBlocksPerLine + bx) * 64;
                        for (var i = 0; i < 64; i++) block[i] = coeffs[off + i] * q[i];
                        Idct.Transform(block, c.Plane, by * 8 * c.PlaneStride + bx * 8, c.PlaneStride);
                    }
                }
                c.Coeffs = null;
            }
        }
        return ConvertToRgba(allocator);
    }

    private void ReadFrame(ReadOnlySpan<byte> seg, bool progressive)
    {
        if (_frameSeen) throw new ImageDecodeException("Multiple JPEG frames are not supported.");
        if (seg.Length < 6) throw new ImageDecodeException("Invalid SOF segment.");
        if (seg[0] != 8) throw new ImageDecodeException($"Unsupported JPEG sample precision {seg[0]} (only 8-bit is supported).");
        _height = BinaryPrimitives.ReadUInt16BigEndian(seg[1..]);
        _width = BinaryPrimitives.ReadUInt16BigEndian(seg[3..]);
        int n = seg[5];
        if (_width == 0 || _height == 0) throw new ImageDecodeException("Invalid JPEG dimensions (DNL markers are not supported).");
        if (n is not (1 or 3 or 4)) throw new ImageDecodeException($"Unsupported JPEG component count {n}.");
        if (seg.Length < 6 + n * 3) throw new ImageDecodeException("Invalid SOF segment.");
        _options.Limits.Validate(_width, _height);
        _progressive = progressive;
        _components = new Component[n];
        _maxH = _maxV = 1;
        for (var i = 0; i < n; i++)
        {
            var c = new Component { Id = seg[6 + i * 3], H = seg[7 + i * 3] >> 4, V = seg[7 + i * 3] & 15, Tq = seg[8 + i * 3] & 3 };
            if (c.H is < 1 or > 4 || c.V is < 1 or > 4) throw new ImageDecodeException("Invalid JPEG sampling factors.");
            _components[i] = c;
            _maxH = Math.Max(_maxH, c.H);
            _maxV = Math.Max(_maxV, c.V);
        }
        _mcusPerLine = (_width + 8 * _maxH - 1) / (8 * _maxH);
        _mcusPerColumn = (_height + 8 * _maxV - 1) / (8 * _maxV);
        foreach (var c in _components)
        {
            var compW = (_width * c.H + _maxH - 1) / _maxH;
            var compH = (_height * c.V + _maxV - 1) / _maxV;
            c.BlocksPerLine = (compW + 7) / 8;
            c.BlocksPerColumn = (compH + 7) / 8;
            c.PaddedBlocksPerLine = _mcusPerLine * c.H;
            c.PaddedBlocksPerColumn = _mcusPerColumn * c.V;
            c.PlaneStride = c.PaddedBlocksPerLine * 8;
            var planeBytes = (long)c.PlaneStride * c.PaddedBlocksPerColumn * 8;
            if (planeBytes > int.MaxValue) throw new ImageLimitExceededException("JPEG component plane too large.");
            c.Plane = new byte[planeBytes];
            if (_progressive)
            {
                var coeffCount = (long)c.PaddedBlocksPerLine * c.PaddedBlocksPerColumn * 64;
                if (coeffCount > int.MaxValue / 2) throw new ImageLimitExceededException("Progressive JPEG too large.");
                c.Coeffs = new short[coeffCount];
            }
        }
        _frameSeen = true;
    }

    private void ReadQuantTables(ReadOnlySpan<byte> seg)
    {
        var p = 0;
        while (p < seg.Length)
        {
            var pq = seg[p] >> 4;
            var tq = seg[p] & 15;
            p++;
            if (tq > 3) throw new ImageDecodeException("Invalid quantisation table id.");
            var table = new ushort[64];
            if (pq == 0)
            {
                if (p + 64 > seg.Length) throw new ImageDecodeException("Truncated DQT.");
                for (var i = 0; i < 64; i++) table[JpegConstants.ZigZag[i]] = seg[p + i];
                p += 64;
            }
            else
            {
                if (p + 128 > seg.Length) throw new ImageDecodeException("Truncated DQT.");
                for (var i = 0; i < 64; i++) table[JpegConstants.ZigZag[i]] = BinaryPrimitives.ReadUInt16BigEndian(seg[(p + i * 2)..]);
                p += 128;
            }
            _qt[tq] = table;
        }
    }

    private void ReadHuffmanTables(ReadOnlySpan<byte> seg)
    {
        var p = 0;
        while (p + 17 <= seg.Length)
        {
            var tc = seg[p] >> 4;
            var th = seg[p] & 15;
            if (th > 3 || tc > 1) throw new ImageDecodeException("Invalid Huffman table id.");
            var bits = seg.Slice(p + 1, 16);
            var total = 0;
            foreach (var b in bits) total += b;
            if (total > 256 || p + 17 + total > seg.Length) throw new ImageDecodeException("Invalid Huffman table.");
            var values = seg.Slice(p + 17, total);
            var table = new HuffmanTable(bits, values);
            if (tc == 0) _dc[th] = table; else _ac[th] = table;
            p += 17 + total;
        }
    }

    private void ReadScan(ReadOnlySpan<byte> seg)
    {
        if (seg.Length < 1) throw new ImageDecodeException("Invalid SOS segment.");
        int n = seg[0];
        if (n < 1 || n > 4 || seg.Length < 1 + n * 2 + 3) throw new ImageDecodeException("Invalid SOS segment.");
        var scanComps = new Component[n];
        for (var i = 0; i < n; i++)
        {
            var id = seg[1 + i * 2];
            var tables = seg[2 + i * 2];
            var c = Array.Find(_components, x => x.Id == id) ?? throw new ImageDecodeException("Scan references unknown component.");
            c.DcTable = tables >> 4; c.AcTable = tables & 15;
            if (c.DcTable > 3 || c.AcTable > 3) throw new ImageDecodeException("Invalid Huffman table selector.");
            scanComps[i] = c;
        }
        int ss = seg[1 + n * 2], se = seg[2 + n * 2], ah = seg[3 + n * 2] >> 4, al = seg[3 + n * 2] & 15;
        if (!_progressive) { ss = 0; se = 63; ah = 0; al = 0; }
        else if (ss > se || se > 63 || (ss == 0 && se != 0) || al > 13) throw new ImageDecodeException("Invalid progressive scan parameters.");

        ResetBits();
        _eobRun = 0;
        foreach (var c in _components) c.DcPred = 0;
        var restartsLeft = _restartInterval;
        var block = new int[64];

        if (n == 1)
        {
            var c = scanComps[0];
            var total = c.BlocksPerLine * c.BlocksPerColumn;
            for (var i = 0; i < total; i++)
            {
                if ((i & 255) == 0) _ct.ThrowIfCancellationRequested();
                if (_restartInterval > 0 && restartsLeft == 0)
                {
                    HandleRestart();
                    restartsLeft = _restartInterval;
                }
                var by = i / c.BlocksPerLine;
                var bx = i % c.BlocksPerLine;
                DecodeBlock(c, by, bx, ss, se, ah, al, block);
                if (_restartInterval > 0) restartsLeft--;
            }
        }
        else
        {
            var totalMcus = _mcusPerLine * _mcusPerColumn;
            for (var m = 0; m < totalMcus; m++)
            {
                if ((m & 63) == 0) _ct.ThrowIfCancellationRequested();
                if (_restartInterval > 0 && restartsLeft == 0)
                {
                    HandleRestart();
                    restartsLeft = _restartInterval;
                }
                var mcuRow = m / _mcusPerLine;
                var mcuCol = m % _mcusPerLine;
                foreach (var c in scanComps)
                    for (var v = 0; v < c.V; v++)
                        for (var h = 0; h < c.H; h++)
                            DecodeBlock(c, mcuRow * c.V + v, mcuCol * c.H + h, ss, se, ah, al, block);
                if (_restartInterval > 0) restartsLeft--;
            }
        }
        // Position after entropy-coded data: skip to the next marker (not RST).
        AlignToMarker();
    }

    private void HandleRestart()
    {
        // Expect an RSTn marker: discard remaining bits, find marker.
        _bitBuf = 0; _bitCount = 0; _markerHit = false;
        var d = Data;
        while (_pos + 1 < d.Length)
        {
            if (d[_pos] == 0xFF && d[_pos + 1] is >= 0xD0 and <= 0xD7) { _pos += 2; break; }
            if (d[_pos] == 0xFF && d[_pos + 1] != 0 && d[_pos + 1] != 0xFF) break; // some other marker: tolerate
            _pos++;
        }
        _eobRun = 0;
        foreach (var c in _components) c.DcPred = 0;
    }

    private void AlignToMarker()
    {
        _bitBuf = 0; _bitCount = 0; _markerHit = false;
        var d = Data;
        while (_pos + 1 < d.Length)
        {
            if (d[_pos] == 0xFF && d[_pos + 1] != 0 && !(d[_pos + 1] is >= 0xD0 and <= 0xD7) && d[_pos + 1] != 0xFF) return;
            _pos++;
        }
        _pos = d.Length;
    }

    private void DecodeBlock(Component c, int by, int bx, int ss, int se, int ah, int al, int[] block)
    {
        if (!_progressive)
        {
            Array.Clear(block);
            DecodeBaselineBlock(c, block);
            if (by >= c.PaddedBlocksPerColumn || bx >= c.PaddedBlocksPerLine) return;
            var q = _qt[c.Tq] ?? throw new ImageDecodeException("Missing quantisation table.");
            for (var i = 0; i < 64; i++) block[i] *= q[i];
            Idct.Transform(block, c.Plane, by * 8 * c.PlaneStride + bx * 8, c.PlaneStride);
            return;
        }
        if (by >= c.PaddedBlocksPerColumn || bx >= c.PaddedBlocksPerLine) return;
        var coeffs = c.Coeffs!;
        var off = (by * c.PaddedBlocksPerLine + bx) * 64;
        if (ss == 0)
        {
            if (ah == 0)
            {
                var dc = _dc[c.DcTable] ?? throw new ImageDecodeException("Missing DC Huffman table.");
                var t = DecodeHuffman(dc);
                var diff = t == 0 ? 0 : Extend(Receive(t), t);
                c.DcPred += diff;
                coeffs[off] = (short)(c.DcPred << al);
            }
            else
            {
                if (ReadBit() != 0) coeffs[off] |= (short)(1 << al);
            }
            return;
        }
        var ac = _ac[c.AcTable] ?? throw new ImageDecodeException("Missing AC Huffman table.");
        if (ah == 0) DecodeAcFirst(ac, coeffs, off, ss, se, al);
        else DecodeAcRefine(ac, coeffs, off, ss, se, al);
    }

    private void DecodeBaselineBlock(Component c, int[] block)
    {
        var dc = _dc[c.DcTable] ?? throw new ImageDecodeException("Missing DC Huffman table.");
        var ac = _ac[c.AcTable] ?? throw new ImageDecodeException("Missing AC Huffman table.");
        var t = DecodeHuffman(dc);
        var diff = t == 0 ? 0 : Extend(Receive(t), t);
        c.DcPred += diff;
        block[0] = c.DcPred;
        var k = 1;
        while (k < 64)
        {
            var rs = DecodeHuffman(ac);
            var s = rs & 15;
            var r = rs >> 4;
            if (s == 0)
            {
                if (r < 15) break; // EOB
                k += 16;
                continue;
            }
            k += r;
            if (k > 63) throw new ImageDecodeException("Corrupt JPEG block data.");
            block[JpegConstants.ZigZag[k]] = Extend(Receive(s), s);
            k++;
        }
    }

    private void DecodeAcFirst(HuffmanTable ac, short[] coeffs, int off, int ss, int se, int al)
    {
        if (_eobRun > 0) { _eobRun--; return; }
        var k = ss;
        while (k <= se)
        {
            var rs = DecodeHuffman(ac);
            var s = rs & 15;
            var r = rs >> 4;
            if (s == 0)
            {
                if (r < 15)
                {
                    _eobRun = (1 << r) - 1;
                    if (r > 0) _eobRun += Receive(r);
                    break;
                }
                k += 16;
                continue;
            }
            k += r;
            if (k > 63) throw new ImageDecodeException("Corrupt progressive JPEG data.");
            coeffs[off + JpegConstants.ZigZag[k]] = (short)(Extend(Receive(s), s) * (1 << al));
            k++;
        }
    }

    private void DecodeAcRefine(HuffmanTable ac, short[] coeffs, int off, int ss, int se, int al)
    {
        var p1 = 1 << al;
        var m1 = -1 << al;
        var k = ss;
        if (_eobRun <= 0)
        {
            for (; k <= se; k++)
            {
                var rs = DecodeHuffman(ac);
                var r = rs >> 4;
                var s = rs & 15;
                if (s != 0)
                {
                    if (s != 1) throw new ImageDecodeException("Corrupt progressive JPEG data.");
                    s = ReadBit() != 0 ? p1 : m1;
                }
                else if (r != 15)
                {
                    _eobRun = 1 << r;
                    if (r > 0) _eobRun += Receive(r);
                    break;
                }
                do
                {
                    var idx = off + JpegConstants.ZigZag[k];
                    var coef = coeffs[idx];
                    if (coef != 0)
                    {
                        if (ReadBit() != 0 && (coef & p1) == 0)
                            coeffs[idx] = (short)(coef >= 0 ? coef + p1 : coef + m1);
                    }
                    else
                    {
                        if (--r < 0) break;
                    }
                    k++;
                } while (k <= se);
                if (s != 0 && k <= se) coeffs[off + JpegConstants.ZigZag[k]] = (short)s;
            }
        }
        if (_eobRun > 0)
        {
            for (; k <= se; k++)
            {
                var idx = off + JpegConstants.ZigZag[k];
                var coef = coeffs[idx];
                if (coef != 0 && ReadBit() != 0 && (coef & p1) == 0)
                    coeffs[idx] = (short)(coef >= 0 ? coef + p1 : coef + m1);
            }
            _eobRun--;
        }
    }

    // ---- Bit reading ----

    private void ResetBits() { _bitBuf = 0; _bitCount = 0; _markerHit = false; }

    private void Fill()
    {
        var d = Data;
        while (_bitCount <= 24)
        {
            uint b = 0;
            if (!_markerHit && _pos < d.Length)
            {
                b = d[_pos];
                if (b == 0xFF)
                {
                    var next = _pos + 1 < d.Length ? d[_pos + 1] : (byte)0xD9;
                    if (next == 0x00) _pos += 2;
                    else if (next == 0xFF) { _pos++; continue; }
                    else { _markerHit = true; b = 0; }
                }
                else _pos++;
            }
            _bitBuf |= b << (24 - _bitCount);
            _bitCount += 8;
        }
    }

    private int ReadBit()
    {
        if (_bitCount < 1) Fill();
        var bit = (int)(_bitBuf >> 31);
        _bitBuf <<= 1;
        _bitCount--;
        return bit;
    }

    private int Receive(int n)
    {
        if (n == 0) return 0;
        if (_bitCount < n) Fill();
        var v = (int)(_bitBuf >> (32 - n));
        _bitBuf <<= n;
        _bitCount -= n;
        return v;
    }

    private static int Extend(int v, int t) => v < (1 << (t - 1)) ? v - (1 << t) + 1 : v;

    private int DecodeHuffman(HuffmanTable table)
    {
        if (_bitCount < 16) Fill();
        var peek = (int)(_bitBuf >> 23);
        var entry = table.Lookup[peek];
        if (entry != 0)
        {
            var len = entry >> 8;
            _bitBuf <<= len;
            _bitCount -= len;
            return entry & 0xFF;
        }
        // Slow path: codes longer than 9 bits.
        var code = peek;
        var l = 9;
        _bitBuf <<= 9; _bitCount -= 9;
        while (l < 16)
        {
            l++;
            code = (code << 1) | ReadBit();
            if (table.MaxCode[l] >= 0 && code <= table.MaxCode[l] && code >= table.MinCode[l])
            {
                var idx = table.ValPtr[l] + code - table.MinCode[l];
                if (idx < table.Values.Length) return table.Values[idx];
                break;
            }
        }
        throw new ImageDecodeException("Corrupt JPEG Huffman data.");
    }

    // ---- Colour conversion ----

    private ImageBuffer ConvertToRgba(IPixelAllocator allocator)
    {
        var image = ImageBuffer.Create(_width, _height, allocator, clear: false, _options.Limits);
        try
        {
            var n = _components.Length;
            var rows = new byte[n][];
            for (var i = 0; i < n; i++) rows[i] = new byte[_width];
            var temp = new byte[Math.Max(1, _components.Max(c => c.PlaneStride))];
            var transform = ResolveTransform();
            for (var y = 0; y < _height; y++)
            {
                if ((y & 31) == 0) _ct.ThrowIfCancellationRequested();
                for (var i = 0; i < n; i++) UpsampleRow(_components[i], y, rows[i], temp);
                var dst = image.GetRow(y);
                switch (transform)
                {
                    case ColorTransform.Grayscale:
                        for (var x = 0; x < _width; x++) { var g = rows[0][x]; dst[x] = new Rgba32(g, g, g); }
                        break;
                    case ColorTransform.Rgb:
                        for (var x = 0; x < _width; x++) dst[x] = new Rgba32(rows[0][x], rows[1][x], rows[2][x]);
                        break;
                    case ColorTransform.YCbCr:
                        for (var x = 0; x < _width; x++) dst[x] = YccToRgb(rows[0][x], rows[1][x], rows[2][x]);
                        break;
                    case ColorTransform.Cmyk:
                        for (var x = 0; x < _width; x++) dst[x] = CmykToRgb(rows[0][x], rows[1][x], rows[2][x], rows[3][x], _adobe);
                        break;
                    case ColorTransform.Ycck:
                        for (var x = 0; x < _width; x++)
                        {
                            // YCCK stores the YCC of the complemented CMY channels, so the converted values are inverted back.
                            var rgb = YccToRgb(rows[0][x], rows[1][x], rows[2][x]);
                            dst[x] = CmykToRgb(255 - rgb.R, 255 - rgb.G, 255 - rgb.B, rows[3][x], _adobe);
                        }
                        break;
                }
            }
            return image;
        }
        catch { image.Dispose(); throw; }
    }

    private enum ColorTransform { Grayscale, Rgb, YCbCr, Cmyk, Ycck }

    private ColorTransform ResolveTransform()
    {
        switch (_components.Length)
        {
            case 1: return ColorTransform.Grayscale;
            case 3:
                if (_adobe) return _adobeTransform == 0 ? ColorTransform.Rgb : ColorTransform.YCbCr;
                if (!_jfif && _components[0].Id == 'R' && _components[1].Id == 'G' && _components[2].Id == 'B') return ColorTransform.Rgb;
                return ColorTransform.YCbCr;
            default:
                if (_adobe) return _adobeTransform == 2 ? ColorTransform.Ycck : ColorTransform.Cmyk;
                return ColorTransform.Cmyk;
        }
    }

    private void UpsampleRow(Component c, int y, byte[] dst, byte[] temp)
    {
        var sx = _maxH / c.H;
        var sy = _maxV / c.V;
        var compW = c.BlocksPerLine * 8;
        var compH = c.PaddedBlocksPerColumn * 8;
        ReadOnlySpan<byte> src;
        if (sy == 1)
        {
            src = c.Plane.AsSpan(Math.Min(y, compH - 1) * c.PlaneStride, c.PlaneStride);
        }
        else if (sy == 2)
        {
            // Triangle (fancy) vertical upsampling: 3/4 nearest row + 1/4 the other neighbour.
            var y0 = Math.Min(y / 2, compH - 1);
            var y1 = Math.Clamp((y & 1) == 0 ? y0 - 1 : y0 + 1, 0, compH - 1);
            var r0 = c.Plane.AsSpan(y0 * c.PlaneStride, c.PlaneStride);
            var r1 = c.Plane.AsSpan(y1 * c.PlaneStride, c.PlaneStride);
            var limit = Math.Min(c.PlaneStride, temp.Length);
            for (var x = 0; x < limit; x++) temp[x] = (byte)((3 * r0[x] + r1[x] + 2) >> 2);
            src = temp.AsSpan(0, limit);
        }
        else
        {
            src = c.Plane.AsSpan(Math.Min(y / sy, compH - 1) * c.PlaneStride, c.PlaneStride);
        }
        var maxX = Math.Min(src.Length, c.PlaneStride) - 1;
        if (sx == 1)
        {
            src[..Math.Min(_width, src.Length)].CopyTo(dst);
        }
        else if (sx == 2)
        {
            var lastSrc = Math.Min(maxX, Math.Max(0, (compW * 1) - 1));
            for (var x = 0; x < _width; x++)
            {
                var x0 = Math.Min(x >> 1, lastSrc);
                var x1 = Math.Clamp((x & 1) == 0 ? x0 - 1 : x0 + 1, 0, lastSrc);
                dst[x] = (byte)((3 * src[x0] + src[x1] + 2) >> 2);
            }
        }
        else
        {
            for (var x = 0; x < _width; x++) dst[x] = src[Math.Min(x / sx, maxX)];
        }
    }

    private static Rgba32 YccToRgb(int y, int cb, int cr)
    {
        cb -= 128; cr -= 128;
        var r = y + ((91881 * cr + 32768) >> 16);
        var g = y + ((-22554 * cb - 46802 * cr + 32768) >> 16); // rounds like the R and B terms (libjpeg)
        var b = y + ((116130 * cb + 32768) >> 16);
        return new Rgba32(Rgba32.ClampToByte(r), Rgba32.ClampToByte(g), Rgba32.ClampToByte(b));
    }

    private static Rgba32 CmykToRgb(int c, int m, int y, int k, bool inverted)
    {
        if (inverted)
            return new Rgba32((byte)(c * k / 255), (byte)(m * k / 255), (byte)(y * k / 255));
        return new Rgba32((byte)((255 - c) * (255 - k) / 255), (byte)((255 - m) * (255 - k) / 255), (byte)((255 - y) * (255 - k) / 255));
    }
}

/// <summary>Integer inverse DCT (Loeffler-Ligtenberg-Moschytz, 13-bit fixed point) matching the accuracy of the reference "islow" method.</summary>
internal static class Idct
{
    private const int ConstBits = 13;
    private const int Pass1Bits = 2;
    private const int F0298 = 2446, F0390 = 3196, F0541 = 4433, F0765 = 6270, F0899 = 7373, F1175 = 9633,
        F1501 = 12299, F1847 = 15137, F1961 = 16069, F2053 = 16819, F2562 = 20995, F3072 = 25172;

    [ThreadStatic] private static int[]? t_workspace;

    /// <summary>Transforms 64 dequantised coefficients (natural order) and writes 8×8 samples into <paramref name="output"/>.</summary>
    public static void Transform(ReadOnlySpan<int> input, byte[] output, int offset, int stride)
    {
        var ws = t_workspace ??= new int[64];
        // Pass 1: columns.
        for (var col = 0; col < 8; col++)
        {
            if (input[8 + col] == 0 && input[16 + col] == 0 && input[24 + col] == 0 && input[32 + col] == 0 && input[40 + col] == 0 && input[48 + col] == 0 && input[56 + col] == 0)
            {
                var dc = input[col] << Pass1Bits;
                for (var r = 0; r < 8; r++) ws[r * 8 + col] = dc;
                continue;
            }
            var z2 = input[16 + col]; var z3 = input[48 + col];
            var z1 = (z2 + z3) * F0541;
            var tmp2 = z1 + z3 * -F1847;
            var tmp3 = z1 + z2 * F0765;
            z2 = input[col]; z3 = input[32 + col];
            var tmp0 = (z2 + z3) << ConstBits;
            var tmp1 = (z2 - z3) << ConstBits;
            var tmp10 = tmp0 + tmp3; var tmp13 = tmp0 - tmp3; var tmp11 = tmp1 + tmp2; var tmp12 = tmp1 - tmp2;

            tmp0 = input[56 + col]; tmp1 = input[40 + col]; tmp2 = input[24 + col]; tmp3 = input[8 + col];
            z1 = tmp0 + tmp3; z2 = tmp1 + tmp2; z3 = tmp0 + tmp2; var z4 = tmp1 + tmp3;
            var z5 = (z3 + z4) * F1175;
            tmp0 *= F0298; tmp1 *= F2053; tmp2 *= F3072; tmp3 *= F1501;
            z1 *= -F0899; z2 *= -F2562; z3 *= -F1961; z4 *= -F0390;
            z3 += z5; z4 += z5;
            tmp0 += z1 + z3; tmp1 += z2 + z4; tmp2 += z2 + z3; tmp3 += z1 + z4;

            const int shift = ConstBits - Pass1Bits;
            const int round = 1 << (shift - 1);
            ws[col] = (tmp10 + tmp3 + round) >> shift;
            ws[56 + col] = (tmp10 - tmp3 + round) >> shift;
            ws[8 + col] = (tmp11 + tmp2 + round) >> shift;
            ws[48 + col] = (tmp11 - tmp2 + round) >> shift;
            ws[16 + col] = (tmp12 + tmp1 + round) >> shift;
            ws[40 + col] = (tmp12 - tmp1 + round) >> shift;
            ws[24 + col] = (tmp13 + tmp0 + round) >> shift;
            ws[32 + col] = (tmp13 - tmp0 + round) >> shift;
        }
        // Pass 2: rows.
        const int shift2 = ConstBits + Pass1Bits + 3;
        const int round2 = 1 << (shift2 - 1);
        for (var row = 0; row < 8; row++)
        {
            var w = row * 8;
            var o = offset + row * stride;
            if (ws[w + 1] == 0 && ws[w + 2] == 0 && ws[w + 3] == 0 && ws[w + 4] == 0 && ws[w + 5] == 0 && ws[w + 6] == 0 && ws[w + 7] == 0)
            {
                var dc = Clamp(((ws[w] + (1 << (Pass1Bits + 2))) >> (Pass1Bits + 3)) + 128);
                for (var i = 0; i < 8; i++) output[o + i] = dc;
                continue;
            }
            var z2 = ws[w + 2]; var z3 = ws[w + 6];
            var z1 = (z2 + z3) * F0541;
            var tmp2 = z1 + z3 * -F1847;
            var tmp3 = z1 + z2 * F0765;
            var tmp0 = (ws[w] + ws[w + 4]) << ConstBits;
            var tmp1 = (ws[w] - ws[w + 4]) << ConstBits;
            var tmp10 = tmp0 + tmp3; var tmp13 = tmp0 - tmp3; var tmp11 = tmp1 + tmp2; var tmp12 = tmp1 - tmp2;

            tmp0 = ws[w + 7]; tmp1 = ws[w + 5]; tmp2 = ws[w + 3]; tmp3 = ws[w + 1];
            z1 = tmp0 + tmp3; z2 = tmp1 + tmp2; z3 = tmp0 + tmp2; var z4 = tmp1 + tmp3;
            var z5 = (z3 + z4) * F1175;
            tmp0 *= F0298; tmp1 *= F2053; tmp2 *= F3072; tmp3 *= F1501;
            z1 *= -F0899; z2 *= -F2562; z3 *= -F1961; z4 *= -F0390;
            z3 += z5; z4 += z5;
            tmp0 += z1 + z3; tmp1 += z2 + z4; tmp2 += z2 + z3; tmp3 += z1 + z4;

            output[o] = Clamp(((tmp10 + tmp3 + round2) >> shift2) + 128);
            output[o + 7] = Clamp(((tmp10 - tmp3 + round2) >> shift2) + 128);
            output[o + 1] = Clamp(((tmp11 + tmp2 + round2) >> shift2) + 128);
            output[o + 6] = Clamp(((tmp11 - tmp2 + round2) >> shift2) + 128);
            output[o + 2] = Clamp(((tmp12 + tmp1 + round2) >> shift2) + 128);
            output[o + 5] = Clamp(((tmp12 - tmp1 + round2) >> shift2) + 128);
            output[o + 3] = Clamp(((tmp13 + tmp0 + round2) >> shift2) + 128);
            output[o + 4] = Clamp(((tmp13 - tmp0 + round2) >> shift2) + 128);
        }
    }

    private static byte Clamp(int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
}
