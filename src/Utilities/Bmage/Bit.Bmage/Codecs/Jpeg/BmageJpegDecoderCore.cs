using System.Buffers.Binary;

namespace Bit.Bmage;

internal sealed class BmageJpegDecoderCore
{
    private const int MaxProgressiveScans = 500;

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
    private readonly BmageDecodeOptions _options;
    private readonly CancellationToken _ct;
    private int _pos;
    private readonly ushort[][] _qt = new ushort[4][];
    private readonly BmageJpegHuffmanTable?[] _dc = new BmageJpegHuffmanTable?[4];
    private readonly BmageJpegHuffmanTable?[] _ac = new BmageJpegHuffmanTable?[4];
    private Component[] _components = [];
    private int _width, _height, _maxH, _maxV, _mcusPerLine, _mcusPerColumn, _restartInterval;
    private bool _progressive, _frameSeen, _planesAllocated, _adobe, _jfif;
    private int _adobeTransform = -1;
    private int _eobRun;

    // Bit reader
    private uint _bitBuf;
    private int _bitCount;
    private bool _markerHit;

    public BmageJpegDecoderCore(ReadOnlySpan<byte> data, BmageDecodeOptions options, CancellationToken ct)
    {
        _data = data.ToArray();
        _options = options;
        _ct = ct;
    }

    private ReadOnlySpan<byte> Data => _data.Span;

    public BmageImageBuffer Decode(IBmagePixelAllocator allocator)
    {
        var d = Data;
        if (d.Length < 4 || d[0] != 0xFF || d[1] != 0xD8) throw new BmageImageDecodeException("Not a JPEG file.");
        _pos = 2;
        var scans = 0;
        var complete = false;
        while (!complete && _pos < d.Length)
        {
            _ct.ThrowIfCancellationRequested();
            // Find next marker.
            if (d[_pos] != 0xFF) { _pos++; continue; }
            while (_pos < d.Length && d[_pos] == 0xFF) _pos++;
            if (_pos >= d.Length) break;
            var marker = d[_pos++];
            if (marker == 0xD9) break; // EOI
            if (marker is 0x00 or 0x01 or (>= 0xD0 and <= 0xD7)) continue;
            if (_pos + 2 > d.Length) throw new BmageImageDecodeException("Truncated JPEG segment.");
            int len = BinaryPrimitives.ReadUInt16BigEndian(d[_pos..]);
            if (len < 2 || _pos + len > d.Length) throw new BmageImageDecodeException("Truncated JPEG segment.");
            var seg = d.Slice(_pos + 2, len - 2);
            var segEnd = _pos + len;
            switch (marker)
            {
                case 0xC0 or 0xC1 or 0xC2: ReadFrame(seg, marker == 0xC2); break;
                case 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF:
                    throw new BmageImageDecodeException("Unsupported JPEG process (lossless, hierarchical or arithmetic coding).");
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
                    if (!_frameSeen) throw new BmageImageDecodeException("JPEG scan before frame header.");
                    // Each scan costs a pass over the whole image, so decoding stops at as many as the frame allows.
                    // A scan past them, repeated by a faulty encoder or left over after the image, adds nothing, so the
                    // image decoded so far is kept rather than refused.
                    if (scans == (_progressive ? MaxProgressiveScans : _components.Length))
                    {
                        complete = true;
                        continue;
                    }
                    _pos = segEnd;
                    ReadScan(seg);
                    scans++;
                    continue; // _pos already advanced past entropy data
            }
            _pos = segEnd;
        }
        if (!_frameSeen || scans == 0) throw new BmageImageDecodeException("JPEG contains no image data.");

        if (_progressive)
        {
            foreach (var c in _components)
            {
                var q = _qt[c.Tq] ?? throw new BmageImageDecodeException("Missing quantisation table.");
                var coeffs = c.Coeffs!;
                var block = new int[64];
                for (var by = 0; by < c.PaddedBlocksPerColumn; by++)
                {
                    _ct.ThrowIfCancellationRequested();
                    for (var bx = 0; bx < c.PaddedBlocksPerLine; bx++)
                    {
                        var off = (by * c.PaddedBlocksPerLine + bx) * 64;
                        for (var i = 0; i < 64; i++) block[i] = coeffs[off + i] * q[i];
                        BmageIdct.Transform(block, c.Plane, by * 8 * c.PlaneStride + bx * 8, c.PlaneStride);
                    }
                }
                c.Coeffs = null;
            }
        }
        return ConvertToRgba(allocator);
    }

    private void ReadFrame(ReadOnlySpan<byte> seg, bool progressive)
    {
        if (_frameSeen) throw new BmageImageDecodeException("Multiple JPEG frames are not supported.");
        if (seg.Length < 6) throw new BmageImageDecodeException("Invalid SOF segment.");
        if (seg[0] != 8) throw new BmageImageDecodeException($"Unsupported JPEG sample precision {seg[0]} (only 8-bit is supported).");
        _height = BinaryPrimitives.ReadUInt16BigEndian(seg[1..]);
        _width = BinaryPrimitives.ReadUInt16BigEndian(seg[3..]);
        int n = seg[5];
        if (_width == 0 || _height == 0) throw new BmageImageDecodeException("Invalid JPEG dimensions (DNL markers are not supported).");
        if (n is not (1 or 3 or 4)) throw new BmageImageDecodeException($"Unsupported JPEG component count {n}.");
        if (seg.Length < 6 + n * 3) throw new BmageImageDecodeException("Invalid SOF segment.");
        _options.Limits.Validate(_width, _height);
        _progressive = progressive;
        _components = new Component[n];
        _maxH = _maxV = 1;
        for (var i = 0; i < n; i++)
        {
            var c = new Component { Id = seg[6 + i * 3], H = seg[7 + i * 3] >> 4, V = seg[7 + i * 3] & 15, Tq = seg[8 + i * 3] & 3 };
            if (c.H is < 1 or > 4 || c.V is < 1 or > 4) throw new BmageImageDecodeException("Invalid JPEG sampling factors.");
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
            if (planeBytes > int.MaxValue) throw new BmageImageLimitExceededException("JPEG component plane too large.");
            if (_progressive && (long)c.PaddedBlocksPerLine * c.PaddedBlocksPerColumn * 64 > int.MaxValue / 2)
                throw new BmageImageLimitExceededException("Progressive JPEG too large.");
        }
        // The planes are allocated at the first scan (AllocatePlanes), once its data is known to be long enough for the
        // frame: a header alone can declare a frame of hundreds of megabytes.
        _frameSeen = true;
    }

    /// <summary>
    /// Allocates the component planes, and a progressive image's coefficients, for the first scan - unless that scan's
    /// entropy-coded data is too short to code the blocks it covers. A sequential scan spends two bits a block at the
    /// least (a DC code and an end of block), and a progressive image's first, DC, scan one. The decoder reads past the
    /// end of the data as zeros, so without this a file of a kilobyte that declares 8192 x 8192 decoded to a gray image,
    /// allocating half a gigabyte on the way. Only the scan's own data counts: what follows the marker that ends it is
    /// never read for it.
    /// </summary>
    private void AllocatePlanes(Component[] scanComps)
    {
        if (_planesAllocated) return;

        var blocks = scanComps.Length == 1
            ? (long)scanComps[0].BlocksPerLine * scanComps[0].BlocksPerColumn
            : (long)_mcusPerLine * _mcusPerColumn * scanComps.Sum(c => c.H * c.V);
        var bitsPerBlock = _progressive ? 1 : 2;
        if (!HasEntropyBits(blocks * bitsPerBlock))
            throw new BmageImageDecodeException("Truncated JPEG: the image data is too short for the frame it declares.");

        foreach (var c in _components)
        {
            c.Plane = new byte[(long)c.PlaneStride * c.PaddedBlocksPerColumn * 8];
            if (_progressive) c.Coeffs = new short[(long)c.PaddedBlocksPerLine * c.PaddedBlocksPerColumn * 64];
        }
        _planesAllocated = true;
    }

    /// <summary>
    /// Whether the entropy-coded data from the current position holds at least <paramref name="bits"/> bits, counted the
    /// way <see cref="Fill"/> reads them: a stuffed 0xFF00 is the one byte it codes, fill bytes and restart markers code
    /// nothing, and any other marker ends the scan.
    /// </summary>
    private bool HasEntropyBits(long bits)
    {
        var d = Data;
        long count = 0;
        for (var i = _pos; i < d.Length && count * 8 < bits; i++)
        {
            if (d[i] != 0xFF) { count++; continue; }
            if (i + 1 >= d.Length) break;
            var next = d[i + 1];
            if (next == 0x00) { count++; i++; }
            else if (next is >= 0xD0 and <= 0xD7) i++;
            else if (next != 0xFF) break;
        }
        return count * 8 >= bits;
    }

    private void ReadQuantTables(ReadOnlySpan<byte> seg)
    {
        var p = 0;
        while (p < seg.Length)
        {
            var pq = seg[p] >> 4;
            var tq = seg[p] & 15;
            p++;
            if (tq > 3) throw new BmageImageDecodeException("Invalid quantisation table id.");
            var table = new ushort[64];
            if (pq == 0)
            {
                if (p + 64 > seg.Length) throw new BmageImageDecodeException("Truncated DQT.");
                for (var i = 0; i < 64; i++) table[BmageJpegConstants.ZigZag[i]] = seg[p + i];
                p += 64;
            }
            else
            {
                if (p + 128 > seg.Length) throw new BmageImageDecodeException("Truncated DQT.");
                for (var i = 0; i < 64; i++) table[BmageJpegConstants.ZigZag[i]] = BinaryPrimitives.ReadUInt16BigEndian(seg[(p + i * 2)..]);
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
            if (th > 3 || tc > 1) throw new BmageImageDecodeException("Invalid Huffman table id.");
            var bits = seg.Slice(p + 1, 16);
            var total = 0;
            foreach (var b in bits) total += b;
            if (total > 256 || p + 17 + total > seg.Length) throw new BmageImageDecodeException("Invalid Huffman table.");
            var values = seg.Slice(p + 17, total);
            var table = new BmageJpegHuffmanTable(bits, values);
            if (tc == 0) _dc[th] = table; else _ac[th] = table;
            p += 17 + total;
        }
    }

    private void ReadScan(ReadOnlySpan<byte> seg)
    {
        if (seg.Length < 1) throw new BmageImageDecodeException("Invalid SOS segment.");
        int n = seg[0];
        if (n < 1 || n > 4 || seg.Length < 1 + n * 2 + 3) throw new BmageImageDecodeException("Invalid SOS segment.");
        var scanComps = new Component[n];
        for (var i = 0; i < n; i++)
        {
            var id = seg[1 + i * 2];
            var tables = seg[2 + i * 2];
            var c = Array.Find(_components, x => x.Id == id) ?? throw new BmageImageDecodeException("Scan references unknown component.");
            c.DcTable = tables >> 4; c.AcTable = tables & 15;
            if (c.DcTable > 3 || c.AcTable > 3) throw new BmageImageDecodeException("Invalid Huffman table selector.");
            scanComps[i] = c;
        }
        int ss = seg[1 + n * 2], se = seg[2 + n * 2], ah = seg[3 + n * 2] >> 4, al = seg[3 + n * 2] & 15;
        if (!_progressive) { ss = 0; se = 63; ah = 0; al = 0; }
        else if (ss > se || se > 63 || (ss == 0 && se != 0) || al > 13) throw new BmageImageDecodeException("Invalid progressive scan parameters.");
        // A progressive image starts with its DC scan; an AC scan first codes a run of blocks in a few bits, so nothing
        // bounds what its data could cover, and it has no DC to refine anyway.
        else if (!_planesAllocated && ss != 0) throw new BmageImageDecodeException("Invalid progressive JPEG: the first scan is not a DC scan.");
        AllocatePlanes(scanComps);

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
            var q = _qt[c.Tq] ?? throw new BmageImageDecodeException("Missing quantisation table.");
            for (var i = 0; i < 64; i++) block[i] *= q[i];
            BmageIdct.Transform(block, c.Plane, by * 8 * c.PlaneStride + bx * 8, c.PlaneStride);
            return;
        }
        if (by >= c.PaddedBlocksPerColumn || bx >= c.PaddedBlocksPerLine) return;
        var coeffs = c.Coeffs!;
        var off = (by * c.PaddedBlocksPerLine + bx) * 64;
        if (ss == 0)
        {
            if (ah == 0)
            {
                var dc = _dc[c.DcTable] ?? throw new BmageImageDecodeException("Missing DC Huffman table.");
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
        var ac = _ac[c.AcTable] ?? throw new BmageImageDecodeException("Missing AC Huffman table.");
        if (ah == 0) DecodeAcFirst(ac, coeffs, off, ss, se, al);
        else DecodeAcRefine(ac, coeffs, off, ss, se, al);
    }

    private void DecodeBaselineBlock(Component c, int[] block)
    {
        var dc = _dc[c.DcTable] ?? throw new BmageImageDecodeException("Missing DC Huffman table.");
        var ac = _ac[c.AcTable] ?? throw new BmageImageDecodeException("Missing AC Huffman table.");
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
            if (k > 63) throw new BmageImageDecodeException("Corrupt JPEG block data.");
            block[BmageJpegConstants.ZigZag[k]] = Extend(Receive(s), s);
            k++;
        }
    }

    private void DecodeAcFirst(BmageJpegHuffmanTable ac, short[] coeffs, int off, int ss, int se, int al)
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
            if (k > 63) throw new BmageImageDecodeException("Corrupt progressive JPEG data.");
            coeffs[off + BmageJpegConstants.ZigZag[k]] = (short)(Extend(Receive(s), s) * (1 << al));
            k++;
        }
    }

    private void DecodeAcRefine(BmageJpegHuffmanTable ac, short[] coeffs, int off, int ss, int se, int al)
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
                    if (s != 1) throw new BmageImageDecodeException("Corrupt progressive JPEG data.");
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
                    var idx = off + BmageJpegConstants.ZigZag[k];
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
                if (s != 0 && k <= se) coeffs[off + BmageJpegConstants.ZigZag[k]] = (short)s;
            }
        }
        if (_eobRun > 0)
        {
            for (; k <= se; k++)
            {
                var idx = off + BmageJpegConstants.ZigZag[k];
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

    private int DecodeHuffman(BmageJpegHuffmanTable table)
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
        throw new BmageImageDecodeException("Corrupt JPEG Huffman data.");
    }

    // ---- Colour conversion ----

    private BmageImageBuffer ConvertToRgba(IBmagePixelAllocator allocator)
    {
        var image = BmageImageBuffer.Create(_width, _height, allocator, clear: false, _options.Limits);
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
                        for (var x = 0; x < _width; x++) { var g = rows[0][x]; dst[x] = new BmageRgba32(g, g, g); }
                        break;
                    case ColorTransform.Rgb:
                        for (var x = 0; x < _width; x++) dst[x] = new BmageRgba32(rows[0][x], rows[1][x], rows[2][x]);
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

    private static BmageRgba32 YccToRgb(int y, int cb, int cr)
    {
        cb -= 128; cr -= 128;
        var r = y + ((91881 * cr + 32768) >> 16);
        var g = y + ((-22554 * cb - 46802 * cr + 32768) >> 16); // rounds like the R and B terms (libjpeg)
        var b = y + ((116130 * cb + 32768) >> 16);
        return new BmageRgba32(BmageRgba32.ClampToByte(r), BmageRgba32.ClampToByte(g), BmageRgba32.ClampToByte(b));
    }

    private static BmageRgba32 CmykToRgb(int c, int m, int y, int k, bool inverted)
    {
        if (inverted)
            return new BmageRgba32((byte)(c * k / 255), (byte)(m * k / 255), (byte)(y * k / 255));
        return new BmageRgba32((byte)((255 - c) * (255 - k) / 255), (byte)((255 - m) * (255 - k) / 255), (byte)((255 - y) * (255 - k) / 255));
    }
}
