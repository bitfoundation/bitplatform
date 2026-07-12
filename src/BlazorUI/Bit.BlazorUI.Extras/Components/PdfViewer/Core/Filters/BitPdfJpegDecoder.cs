// A baseline (sequential DCT, Huffman) JPEG decoder. Ported to the scope needed
// by the PDF image pipeline: it lets us decode CMYK/YCCK JPEGs (which browsers
// render wrong) and apply /SMask, /Mask and /Decode to DCT images. Progressive
// JPEGs are not handled here (the caller keeps browser passthrough for those).

namespace Bit.BlazorUI;

/// <summary>Decodes baseline sequential-DCT JPEG streams.</summary>
internal static class BitPdfJpegDecoder
{
    private static readonly int[] ZigZag =
    [
        0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63,
    ];

    private sealed class Component
    {
        public int Id;
        public int H, V;            // sampling factors
        public int QuantId;
        public int DcTable, AcTable;
        public int Pred;            // DC predictor
        public int BlocksPerLine, BlocksPerColumn;
        public byte[]? Output;      // per-component plane (blocksPerLine*8 x blocksPerColumn*8)
        public int LineStride;
    }

    private sealed class HuffTable
    {
        // Fast lookup: maps a canonical (length, code) to a value via a dictionary.
        public readonly Dictionary<int, byte> Codes = new();
        public int MaxLen;
    }

    /// <summary>Decodes a baseline JPEG, or returns <c>null</c> if the stream is
    /// progressive/unsupported or malformed.</summary>
    public static BitPdfJpegImage? Decode(byte[] data)
    {
        try
        {
            return DecodeCore(data);
        }
        catch
        {
            return null;
        }
    }

    private static BitPdfJpegImage? DecodeCore(byte[] data)
    {
        int pos = 0;
        var quant = new int[4][];
        var huffDc = new HuffTable[4];
        var huffAc = new HuffTable[4];
        int frameWidth = 0, frameHeight = 0, precision = 8;
        Component[]? components = null;
        int restartInterval = 0;
        int adobeTransform = -1;
        bool adobe = false;

        if (data.Length < 2 || data[0] != 0xFF || data[1] != 0xD8)
        {
            return null; // not SOI
        }
        pos = 2;

        while (pos + 1 < data.Length)
        {
            if (data[pos] != 0xFF)
            {
                pos++;
                continue;
            }
            int marker = data[pos + 1];
            pos += 2;
            if (marker == 0xD9) // EOI
            {
                break;
            }
            if (marker is 0x01 || (marker >= 0xD0 && marker <= 0xD7))
            {
                continue; // standalone markers
            }
            if (pos + 1 >= data.Length)
            {
                break;
            }
            int len = (data[pos] << 8) | data[pos + 1];
            int segStart = pos + 2;
            int segEnd = pos + len;
            if (segEnd > data.Length)
            {
                return null;
            }

            switch (marker)
            {
                case 0xC0: // SOF0 baseline
                case 0xC1: // SOF1 extended sequential (Huffman) — same layout
                {
                    precision = data[segStart];
                    frameHeight = (data[segStart + 1] << 8) | data[segStart + 2];
                    frameWidth = (data[segStart + 3] << 8) | data[segStart + 4];
                    int nc = data[segStart + 5];
                    components = new Component[nc];
                    int cp = segStart + 6;
                    for (int i = 0; i < nc; i++)
                    {
                        components[i] = new Component
                        {
                            Id = data[cp],
                            H = data[cp + 1] >> 4,
                            V = data[cp + 1] & 0x0F,
                            QuantId = data[cp + 2],
                        };
                        cp += 3;
                    }
                    break;
                }
                case 0xC2: // SOF2 progressive — not supported here
                    return null;
                case 0xC4: // DHT
                    ReadHuffmanTables(data, segStart, segEnd, huffDc, huffAc);
                    break;
                case 0xDB: // DQT
                    ReadQuantTables(data, segStart, segEnd, quant);
                    break;
                case 0xDD: // DRI
                    restartInterval = (data[segStart] << 8) | data[segStart + 1];
                    break;
                case 0xEE: // APP14 (Adobe)
                    if (len >= 14 && data[segStart] == (byte)'A' && data[segStart + 1] == (byte)'d')
                    {
                        adobe = true;
                        adobeTransform = data[segStart + 11];
                    }
                    break;
                case 0xDA: // SOS
                {
                    if (components is null)
                    {
                        return null;
                    }
                    int scanComps = data[segStart];
                    int sp = segStart + 1;
                    var scan = new Component[scanComps];
                    for (int i = 0; i < scanComps; i++)
                    {
                        int cid = data[sp];
                        var comp = Array.Find(components, c => c.Id == cid) ?? components[i];
                        comp.DcTable = data[sp + 1] >> 4;
                        comp.AcTable = data[sp + 1] & 0x0F;
                        scan[i] = comp;
                        sp += 2;
                    }
                    // Skip Ss, Se, Ah/Al (baseline: 0, 63, 0).
                    pos = sp + 3;
                    pos = DecodeScan(data, pos, frameWidth, frameHeight, components, scan,
                        quant, huffDc, huffAc, restartInterval);
                    continue;
                }
                default:
                    break;
            }
            pos = segEnd;
        }

        if (components is null || frameWidth <= 0 || frameHeight <= 0 || precision != 8)
        {
            return null;
        }

        return AssembleImage(frameWidth, frameHeight, components, adobe, adobeTransform);
    }

    private static void ReadQuantTables(byte[] data, int start, int end, int[][] quant)
    {
        int p = start;
        while (p < end)
        {
            int pq = data[p] >> 4;   // precision (0=8-bit,1=16-bit)
            int tq = data[p] & 0x0F; // table id
            p++;
            var table = new int[64];
            for (int i = 0; i < 64; i++)
            {
                if (pq == 0)
                {
                    table[i] = data[p++];
                }
                else
                {
                    table[i] = (data[p] << 8) | data[p + 1];
                    p += 2;
                }
            }
            if (tq < 4)
            {
                quant[tq] = table;
            }
        }
    }

    private static void ReadHuffmanTables(byte[] data, int start, int end, HuffTable[] dc, HuffTable[] ac)
    {
        int p = start;
        while (p < end)
        {
            int tc = data[p] >> 4;   // 0 = DC, 1 = AC
            int th = data[p] & 0x0F; // table id
            p++;
            var counts = new int[17];
            int total = 0;
            for (int i = 1; i <= 16; i++)
            {
                counts[i] = data[p++];
                total += counts[i];
            }
            var table = new HuffTable();
            int code = 0;
            for (int len = 1; len <= 16; len++)
            {
                for (int i = 0; i < counts[len]; i++)
                {
                    table.Codes[(len << 16) | code] = data[p++];
                    code++;
                }
                code <<= 1;
                if (counts[len] > 0)
                {
                    table.MaxLen = len;
                }
            }
            if (th < 4)
            {
                if (tc == 0)
                {
                    dc[th] = table;
                }
                else
                {
                    ac[th] = table;
                }
            }
        }
    }

    private sealed class BitStream
    {
        private readonly byte[] _data;
        private int _pos;
        private int _bits;
        private int _count;
        public bool Marker; // set when a non-restart marker is hit

        public BitStream(byte[] data, int pos)
        {
            _data = data;
            _pos = pos;
        }

        public int Pos => _pos;

        public int ReadBit()
        {
            if (_count == 0)
            {
                if (_pos >= _data.Length)
                {
                    return 0;
                }
                int b = _data[_pos++];
                if (b == 0xFF)
                {
                    int next = _pos < _data.Length ? _data[_pos] : 0;
                    if (next == 0)
                    {
                        _pos++; // stuffed zero
                    }
                    else
                    {
                        Marker = true;
                        _pos--; // leave the marker for the caller
                        return 0;
                    }
                }
                _bits = b;
                _count = 8;
            }
            _count--;
            return (_bits >> _count) & 1;
        }

        public int ReadBits(int n)
        {
            int v = 0;
            for (int i = 0; i < n; i++)
            {
                v = (v << 1) | ReadBit();
            }
            return v;
        }

        public void Reset()
        {
            _count = 0;
            Marker = false;
        }

        public void AlignAndSkipRestart()
        {
            _count = 0;
            // Skip to and past an RSTn marker.
            while (_pos + 1 < _data.Length)
            {
                if (_data[_pos] == 0xFF && _data[_pos + 1] >= 0xD0 && _data[_pos + 1] <= 0xD7)
                {
                    _pos += 2;
                    return;
                }
                _pos++;
            }
        }
    }

    private static int DecodeHuffman(BitStream bits, HuffTable table)
    {
        int code = 0;
        for (int len = 1; len <= 16; len++)
        {
            code = (code << 1) | bits.ReadBit();
            if (table.Codes.TryGetValue((len << 16) | code, out byte value))
            {
                return value;
            }
        }
        return 0;
    }

    private static int Receive(BitStream bits, int size)
    {
        if (size == 0)
        {
            return 0;
        }
        int v = bits.ReadBits(size);
        // Extend the sign per JPEG spec.
        return v < (1 << (size - 1)) ? v - (1 << size) + 1 : v;
    }

    private static int DecodeScan(byte[] data, int pos, int width, int height,
        Component[] components, Component[] scan, int[][] quant,
        HuffTable[] huffDc, HuffTable[] huffAc, int restartInterval)
    {
        int maxH = 0, maxV = 0;
        foreach (var c in components)
        {
            maxH = Math.Max(maxH, c.H);
            maxV = Math.Max(maxV, c.V);
        }
        int mcusPerLine = (width + 8 * maxH - 1) / (8 * maxH);
        int mcusPerColumn = (height + 8 * maxV - 1) / (8 * maxV);

        foreach (var c in components)
        {
            c.BlocksPerLine = mcusPerLine * c.H;
            c.BlocksPerColumn = mcusPerColumn * c.V;
            c.LineStride = c.BlocksPerLine * 8;
            c.Output = new byte[c.LineStride * c.BlocksPerColumn * 8];
            c.Pred = 0;
        }

        var bits = new BitStream(data, pos);
        int mcu = 0;
        int totalMcus = mcusPerLine * mcusPerColumn;
        var block = new int[64];

        while (mcu < totalMcus)
        {
            if (restartInterval > 0 && mcu > 0 && mcu % restartInterval == 0)
            {
                bits.AlignAndSkipRestart();
                foreach (var c in scan)
                {
                    c.Pred = 0;
                }
            }

            int mcuRow = mcu / mcusPerLine;
            int mcuCol = mcu % mcusPerLine;
            foreach (var c in scan)
            {
                for (int by = 0; by < c.V; by++)
                {
                    for (int bx = 0; bx < c.H; bx++)
                    {
                        DecodeBlock(bits, c, quant[c.QuantId] ?? Identity, huffDc[c.DcTable], huffAc[c.AcTable], block);
                        int blockRow = mcuRow * c.V + by;
                        int blockCol = mcuCol * c.H + bx;
                        PlaceBlock(c, block, blockRow, blockCol);
                    }
                }
            }
            mcu++;
            if (bits.Marker && restartInterval == 0)
            {
                break;
            }
        }

        return bits.Pos;
    }

    private static readonly int[] Identity = BuildIdentity();
    private static int[] BuildIdentity()
    {
        var t = new int[64];
        Array.Fill(t, 1);
        return t;
    }

    private static void DecodeBlock(BitStream bits, Component c, int[] quant, HuffTable dc, HuffTable ac, int[] block)
    {
        Array.Clear(block);
        int t = DecodeHuffman(bits, dc);
        int diff = Receive(bits, t);
        c.Pred += diff;
        block[0] = c.Pred * quant[0];

        int k = 1;
        while (k < 64)
        {
            int rs = DecodeHuffman(bits, ac);
            int r = rs >> 4;
            int s = rs & 0x0F;
            if (s == 0)
            {
                if (r != 15)
                {
                    break; // EOB
                }
                k += 16;
                continue;
            }
            k += r;
            if (k >= 64)
            {
                break;
            }
            int coeff = Receive(bits, s);
            block[ZigZag[k]] = coeff * quant[k];
            k++;
        }

        Idct(block);
    }

    private static void PlaceBlock(Component c, int[] block, int blockRow, int blockCol)
    {
        byte[] output = c.Output!;
        int stride = c.LineStride;
        int baseY = blockRow * 8;
        int baseX = blockCol * 8;
        for (int y = 0; y < 8; y++)
        {
            int rowIdx = (baseY + y) * stride + baseX;
            if (rowIdx < 0 || rowIdx + 8 > output.Length)
            {
                continue;
            }
            for (int x = 0; x < 8; x++)
            {
                int v = block[y * 8 + x] + 128;
                output[rowIdx + x] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
            }
        }
    }

    // Separable inverse DCT (float; adequate fidelity for display).
    private static void Idct(int[] block)
    {
        var tmp = new double[64];
        for (int u = 0; u < 8; u++)
        {
            for (int x = 0; x < 8; x++)
            {
                double sum = 0;
                for (int v = 0; v < 8; v++)
                {
                    double cv = v == 0 ? Math.Sqrt(0.5) : 1;
                    sum += cv * block[v * 8 + u] * Math.Cos((2 * x + 1) * v * Math.PI / 16);
                }
                tmp[x * 8 + u] = sum * 0.5;
            }
        }
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                double sum = 0;
                for (int u = 0; u < 8; u++)
                {
                    double cu = u == 0 ? Math.Sqrt(0.5) : 1;
                    sum += cu * tmp[y * 8 + u] * Math.Cos((2 * x + 1) * u * Math.PI / 16);
                }
                block[y * 8 + x] = (int)Math.Round(sum * 0.5);
            }
        }
    }

    private static BitPdfJpegImage AssembleImage(int width, int height, Component[] components,
        bool adobe, int adobeTransform)
    {
        int maxH = 0, maxV = 0;
        foreach (var c in components)
        {
            maxH = Math.Max(maxH, c.H);
            maxV = Math.Max(maxV, c.V);
        }

        int nc = components.Length;
        var data = new byte[width * height * nc];

        // Sample each component (with chroma upsampling) into the interleaved buffer.
        for (int ci = 0; ci < nc; ci++)
        {
            Component c = components[ci];
            byte[] plane = c.Output ?? [];
            int stride = c.LineStride;
            double sx = (double)c.H / maxH;
            double sy = (double)c.V / maxV;
            for (int y = 0; y < height; y++)
            {
                int py = (int)(y * sy);
                for (int x = 0; x < width; x++)
                {
                    int px = (int)(x * sx);
                    int idx = py * stride + px;
                    byte v = idx >= 0 && idx < plane.Length ? plane[idx] : (byte)0;
                    data[(y * width + x) * nc + ci] = v;
                }
            }
        }

        // Apply the JPEG colour transform to the JPEG's natural output space.
        if (nc == 3)
        {
            bool transform = adobeTransform != 0; // default YCbCr for 3 components
            if (transform)
            {
                YCbCrToRgb(data);
            }
        }
        else if (nc == 4)
        {
            if (adobeTransform == 2)
            {
                YccKToCmyk(data);
            }
            // Adobe CMYK JPEGs store inverted CMYK.
            if (adobe)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (byte)(255 - data[i]);
                }
            }
        }

        return new BitPdfJpegImage { Width = width, Height = height, Components = nc, Data = data };
    }

    private static void YCbCrToRgb(byte[] data)
    {
        for (int i = 0; i + 2 < data.Length; i += 3)
        {
            double yy = data[i];
            double cb = data[i + 1] - 128.0;
            double cr = data[i + 2] - 128.0;
            data[i] = Clamp(yy + 1.402 * cr);
            data[i + 1] = Clamp(yy - 0.344136 * cb - 0.714136 * cr);
            data[i + 2] = Clamp(yy + 1.772 * cb);
        }
    }

    private static void YccKToCmyk(byte[] data)
    {
        for (int i = 0; i + 3 < data.Length; i += 4)
        {
            double yy = data[i];
            double cb = data[i + 1] - 128.0;
            double cr = data[i + 2] - 128.0;
            data[i] = Clamp(yy + 1.402 * cr);
            data[i + 1] = Clamp(yy - 0.344136 * cb - 0.714136 * cr);
            data[i + 2] = Clamp(yy + 1.772 * cb);
            // data[i+3] (K) is passed through.
        }
    }

    private static byte Clamp(double v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : Math.Round(v));
}
