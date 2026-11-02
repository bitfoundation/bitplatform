using System.Numerics;
using System.Runtime.InteropServices;

namespace Bit.Bmage.Codecs.WebP;

/// <summary>A VP8 key frame's 8-bit Y, U and V planes, chroma at half resolution, padded to whole macroblocks.</summary>
internal sealed class Vp8Frame
{
    public Vp8Frame(int width, int height)
    {
        Width = width;
        Height = height;
        MbWidth = (width + 15) >> 4;
        MbHeight = (height + 15) >> 4;
        YStride = MbWidth * 16;
        UVStride = MbWidth * 8;
        Y = new byte[YStride * MbHeight * 16];
        U = new byte[UVStride * MbHeight * 8];
        V = new byte[UVStride * MbHeight * 8];
    }

    public int Width { get; }
    public int Height { get; }
    public int MbWidth { get; }
    public int MbHeight { get; }
    public byte[] Y { get; }
    public byte[] U { get; }
    public byte[] V { get; }
    public int YStride { get; }
    public int UVStride { get; }
}

/// <summary>
/// Decoder for the lossy VP8 key frames WebP stores (RFC 6386): boolean entropy coding, intra prediction of 16x16 and
/// 4x4 luma and 8x8 chroma blocks, the 4x4 DCT and Walsh-Hadamard transforms and the normal and simple loop filters.
/// </summary>
/// <remarks>
/// Prediction reads the unfiltered reconstruction of the neighbouring blocks, so the frame is reconstructed in full first
/// and the loop filter runs over it afterwards, in macroblock order, exactly as the format defines it.
/// </remarks>
internal sealed class Vp8Decoder
{
    // 4x4 subblock modes, numbered as the format's mode trees and probability tables number them. The first four
    // double as the 16x16 and chroma modes they are implied by.
    internal const int BDc = 0, BTm = 1, BVe = 2, BHe = 3, BLd = 4, BRd = 5, BVr = 6, BVl = 7, BHd = 8, BHu = 9;

    private static ReadOnlySpan<sbyte> BModeTree => [-BDc, 2, -BTm, 4, -BVe, 6, 8, 12, -BHe, 10, -BRd, -BVr, -BLd, 14, -BVl, 16, -BHd, -BHu];

    private readonly Vp8Frame _frame;
    private readonly int _mbWidth, _mbHeight;
    private readonly CancellationToken _ct;

    // Segment and loop filter header.
    private bool _segmentationEnabled, _updateMap, _absoluteDelta;
    private readonly int[] _segmentQuant = new int[4];
    private readonly int[] _segmentFilter = new int[4];
    private readonly byte[] _segmentProbas = [255, 255, 255];
    private bool _simpleFilter;
    private int _filterLevel, _sharpness;
    private bool _useLfDelta;
    private readonly int[] _refLfDelta = new int[4];
    private readonly int[] _modeLfDelta = new int[4];

    private readonly byte[] _probas = Vp8Tables.DefaultCoeffProbas.ToArray();
    private bool _useSkipProba;
    private int _skipProba;
    private readonly Quant[] _quant = new Quant[4];

    // Per macroblock state the loop filter needs once the frame is reconstructed.
    private readonly byte[] _mbSegment;
    private readonly bool[] _mbIs4x4;
    private readonly bool[] _mbHasCoeffs;

    private Vp8Decoder(int width, int height, CancellationToken ct)
    {
        _frame = new Vp8Frame(width, height);
        _mbWidth = _frame.MbWidth;
        _mbHeight = _frame.MbHeight;
        _ct = ct;
        var count = _mbWidth * _mbHeight;
        _mbSegment = new byte[count];
        _mbIs4x4 = new bool[count];
        _mbHasCoeffs = new bool[count];
    }

    /// <summary>Reads the frame dimensions from a 'VP8 ' chunk payload, or null when it is not a key frame.</summary>
    public static (int Width, int Height)? ReadHeader(ReadOnlySpan<byte> data)
    {
        if (data.Length < 10) return null;
        var tag = data[0] | data[1] << 8 | data[2] << 16;
        if ((tag & 1) != 0 || data[3] != 0x9D || data[4] != 0x01 || data[5] != 0x2A) return null;
        var width = (data[6] | data[7] << 8) & 0x3FFF;
        var height = (data[8] | data[9] << 8) & 0x3FFF;
        return width > 0 && height > 0 ? (width, height) : null;
    }

    /// <summary>Decodes a 'VP8 ' chunk payload, read in place. The caller has validated the dimensions against its limits.</summary>
    public static Vp8Frame Decode(ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        var data = payload.Span;
        if (ReadHeader(data) is not { } size) throw new ImageDecodeException("Invalid WebP lossy header: not a VP8 key frame.");
        var tag = data[0] | data[1] << 8 | data[2] << 16;
        var version = (tag >> 1) & 7;
        var show = (tag >> 4) & 1;
        var firstPartitionSize = tag >> 5;
        if (version > 3) throw new ImageDecodeException($"Unsupported VP8 version {version}.");
        if (show == 0) throw new ImageDecodeException("Invalid WebP lossy data: the frame is not displayable.");
        if (firstPartitionSize > data.Length - 10) throw new ImageDecodeException("Truncated WebP lossy data.");

        var decoder = new Vp8Decoder(size.Width, size.Height, ct);
        var br = new Vp8BoolDecoder(payload.Slice(10, firstPartitionSize));
        var partitions = decoder.ParseHeader(br, payload[(10 + firstPartitionSize)..]);
        decoder.DecodeMacroblocks(br, partitions);
        decoder.LoopFilter();
        return decoder._frame;
    }

    // ------------------------------------------------------------------ frame header

    private Vp8BoolDecoder[] ParseHeader(Vp8BoolDecoder br, ReadOnlyMemory<byte> rest)
    {
        br.ReadLiteral(1); // colour space: only YUV is defined
        br.ReadLiteral(1); // clamping type: reconstruction always clamps

        _segmentationEnabled = br.ReadFlag();
        if (_segmentationEnabled)
        {
            _updateMap = br.ReadFlag();
            if (br.ReadFlag())
            {
                _absoluteDelta = br.ReadFlag();
                for (var s = 0; s < 4; s++) _segmentQuant[s] = br.ReadFlag() ? br.ReadSigned(7) : 0;
                for (var s = 0; s < 4; s++) _segmentFilter[s] = br.ReadFlag() ? br.ReadSigned(6) : 0;
            }
            if (_updateMap)
                for (var i = 0; i < 3; i++) _segmentProbas[i] = br.ReadFlag() ? (byte)br.ReadLiteral(8) : (byte)255;
        }

        _simpleFilter = br.ReadFlag();
        _filterLevel = br.ReadLiteral(6);
        _sharpness = br.ReadLiteral(3);
        _useLfDelta = br.ReadFlag();
        if (_useLfDelta && br.ReadFlag())
        {
            for (var i = 0; i < 4; i++) if (br.ReadFlag()) _refLfDelta[i] = br.ReadSigned(6);
            for (var i = 0; i < 4; i++) if (br.ReadFlag()) _modeLfDelta[i] = br.ReadSigned(6);
        }

        // Token partitions: their sizes, three bytes each, precede them; the last takes what remains.
        var count = 1 << br.ReadLiteral(2);
        var sizesLength = 3 * (count - 1);
        if (rest.Length < sizesLength) throw new ImageDecodeException("Truncated WebP lossy data.");
        var partitions = new Vp8BoolDecoder[count];
        var sizes = rest.Span;
        var start = sizesLength;
        for (var p = 0; p < count; p++)
        {
            var length = p < count - 1 ? sizes[3 * p] | sizes[3 * p + 1] << 8 | sizes[3 * p + 2] << 16 : rest.Length - start;
            length = Math.Min(length, rest.Length - start);
            partitions[p] = new Vp8BoolDecoder(rest.Slice(start, length));
            start += length;
        }

        var baseQ = br.ReadLiteral(7);
        var yDc = br.ReadFlag() ? br.ReadSigned(4) : 0;
        var y2Dc = br.ReadFlag() ? br.ReadSigned(4) : 0;
        var y2Ac = br.ReadFlag() ? br.ReadSigned(4) : 0;
        var uvDc = br.ReadFlag() ? br.ReadSigned(4) : 0;
        var uvAc = br.ReadFlag() ? br.ReadSigned(4) : 0;
        for (var s = 0; s < 4; s++)
        {
            var q = _segmentationEnabled ? _segmentQuant[s] + (_absoluteDelta ? 0 : baseQ) : baseQ;
            _quant[s] = Quant.Create(q, yDc, y2Dc, y2Ac, uvDc, uvAc);
        }

        br.ReadLiteral(1); // refresh entropy probabilities: there is no next frame

        var update = Vp8Tables.CoeffUpdateProbas;
        for (var i = 0; i < _probas.Length; i++)
            if (br.GetBit(update[i])) _probas[i] = (byte)br.ReadLiteral(8);

        _useSkipProba = br.ReadFlag();
        if (_useSkipProba) _skipProba = br.ReadLiteral(8);
        if (br.IsOverrun) throw new ImageDecodeException("Truncated WebP lossy header.");
        return partitions;
    }

    // ------------------------------------------------------------------ macroblocks

    private void DecodeMacroblocks(Vp8BoolDecoder br, Vp8BoolDecoder[] partitions)
    {
        // Non-zero flags of the blocks along the bottom of the row above and the right of the macroblock to the left,
        // which select the probabilities of the next block's first token.
        var nzTop = new byte[_mbWidth * 9];
        Span<byte> nzLeft = stackalloc byte[9];
        // The 4x4 modes along the same edges, the context of the next block's mode.
        var modesTop = new byte[_mbWidth * 4];
        Span<byte> modesLeft = stackalloc byte[4];
        Span<byte> modes = stackalloc byte[16];
        var coeffs = new short[25 * 16];
        Span<byte> work = stackalloc byte[WorkSize];

        for (var mby = 0; mby < _mbHeight; mby++)
        {
            _ct.ThrowIfCancellationRequested();
            nzLeft.Clear();
            modesLeft.Clear();
            var tokens = partitions[mby & (partitions.Length - 1)];
            for (var mbx = 0; mbx < _mbWidth; mbx++)
            {
                var mb = mby * _mbWidth + mbx;

                // Macroblock header, from the first partition.
                var segment = 0;
                if (_updateMap)
                    segment = !br.GetBit(_segmentProbas[0]) ? br.GetBit(_segmentProbas[1]) ? 1 : 0 : br.GetBit(_segmentProbas[2]) ? 3 : 2;
                var skip = _useSkipProba && br.GetBit(_skipProba);
                var is4x4 = !br.GetBit(145);
                int yMode = BDc;
                if (is4x4)
                {
                    var kf = Vp8Tables.KeyFrameBModeProbas;
                    for (var y = 0; y < 4; y++)
                    {
                        int left = modesLeft[y];
                        for (var x = 0; x < 4; x++)
                        {
                            var probas = kf.Slice((modesTop[mbx * 4 + x] * 10 + left) * 9, 9);
                            var i = 0;
                            do i = BModeTree[i + (br.GetBit(probas[i >> 1]) ? 1 : 0)];
                            while (i > 0);
                            left = -i;
                            modes[y * 4 + x] = (byte)left;
                            modesTop[mbx * 4 + x] = (byte)left;
                        }
                        modesLeft[y] = (byte)left;
                    }
                }
                else
                {
                    yMode = br.GetBit(156) ? (br.GetBit(128) ? BTm : BHe) : (br.GetBit(163) ? BVe : BDc);
                    modesTop.AsSpan(mbx * 4, 4).Fill((byte)yMode);
                    modesLeft.Fill((byte)yMode);
                }
                var uvMode = !br.GetBit(142) ? BDc : !br.GetBit(114) ? BVe : br.GetBit(183) ? BTm : BHe;

                // Residuals, from the token partition of this row.
                var nzTopMb = nzTop.AsSpan(mbx * 9, 9);
                bool hasCoeffs;
                if (skip)
                {
                    coeffs.AsSpan().Clear();
                    nzLeft[..8].Clear();
                    nzTopMb[..8].Clear();
                    if (!is4x4) { nzLeft[8] = 0; nzTopMb[8] = 0; }
                    hasCoeffs = false;
                }
                else hasCoeffs = ParseResiduals(tokens, _quant[segment], is4x4, coeffs, nzTopMb, nzLeft);
                if (tokens.IsOverrun || br.IsOverrun) throw new ImageDecodeException("Truncated WebP lossy data.");

                _mbSegment[mb] = (byte)segment;
                _mbIs4x4[mb] = is4x4;
                _mbHasCoeffs[mb] = hasCoeffs;
                Reconstruct(mbx, mby, is4x4, yMode, modes, uvMode, coeffs, work);
            }
        }
    }

    /// <summary>Reads the coefficients of one macroblock, dequantised, in raster order per block. Returns true if any is non-zero.</summary>
    private bool ParseResiduals(Vp8BoolDecoder br, Quant q, bool is4x4, short[] coeffs, Span<byte> nzTop, Span<byte> nzLeft)
    {
        coeffs.AsSpan().Clear();
        var any = false;
        int first, type;
        if (!is4x4)
        {
            // The DCs of the 16 luma blocks are coded together in a 17th block and Walsh-Hadamard transformed.
            Span<short> dc = coeffs.AsSpan(24 * 16, 16);
            var nz = GetCoeffs(br, 1, nzTop[8] + nzLeft[8], q.Y2Dc, q.Y2Ac, 0, dc);
            nzTop[8] = nzLeft[8] = (byte)(nz > 0 ? 1 : 0);
            if (nz > 1) InverseWalsh(dc, coeffs);
            else
            {
                var dc0 = (short)((dc[0] + 3) >> 3);
                for (var i = 0; i < 16; i++) coeffs[i * 16] = dc0;
            }
            first = 1;
            type = 0;
        }
        else
        {
            first = 0;
            type = 3;
        }

        for (var y = 0; y < 4; y++)
        {
            for (var x = 0; x < 4; x++)
            {
                var block = coeffs.AsSpan((y * 4 + x) * 16, 16);
                var nz = GetCoeffs(br, type, nzTop[x] + nzLeft[y], q.Y1Dc, q.Y1Ac, first, block);
                var flag = (byte)(nz > first ? 1 : 0);
                nzTop[x] = nzLeft[y] = flag;
                if (flag != 0 || block[0] != 0) any = true;
            }
        }
        for (var plane = 0; plane < 2; plane++)
        {
            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 2; x++)
                {
                    var block = coeffs.AsSpan((16 + plane * 4 + y * 2 + x) * 16, 16);
                    var t = 4 + plane * 2 + x;
                    var l = 4 + plane * 2 + y;
                    var nz = GetCoeffs(br, 2, nzTop[t] + nzLeft[l], q.UvDc, q.UvAc, 0, block);
                    var flag = (byte)(nz > 0 ? 1 : 0);
                    nzTop[t] = nzLeft[l] = flag;
                    if (flag != 0) any = true;
                }
            }
        }
        return any;
    }

    /// <summary>
    /// Decodes the tokens of one block from position <paramref name="n"/>, storing dequantised values in raster order.
    /// Returns the position after the last token read, so a block whose first token ends it returns its start.
    /// </summary>
    private int GetCoeffs(Vp8BoolDecoder br, int type, int ctx, int dcQ, int acQ, int n, Span<short> output)
    {
        var probas = _probas;
        var bands = Vp8Tables.Bands;
        var zigzag = Vp8Tables.Zigzag;
        var p = Vp8Tables.ProbaIndex(type, bands[n], ctx);
        for (; n < 16; n++)
        {
            if (!br.GetBit(probas[p])) return n; // end of block
            while (!br.GetBit(probas[p + 1])) // zero; no end of block can follow one
            {
                if (++n == 16) return 16;
                p = Vp8Tables.ProbaIndex(type, bands[n], 0);
            }
            int v, next;
            if (!br.GetBit(probas[p + 2]))
            {
                v = 1;
                next = 1;
            }
            else
            {
                v = GetLargeValue(br, probas, p);
                next = 2;
            }
            var value = v * (n > 0 ? acQ : dcQ);
            output[zigzag[n]] = (short)(br.GetBit(128) ? -value : value);
            p = Vp8Tables.ProbaIndex(type, bands[n + 1], next);
        }
        return 16;
    }

    private static int GetLargeValue(Vp8BoolDecoder br, byte[] probas, int p)
    {
        if (!br.GetBit(probas[p + 3]))
            return !br.GetBit(probas[p + 4]) ? 2 : 3 + (br.GetBit(probas[p + 5]) ? 1 : 0);
        if (!br.GetBit(probas[p + 6]))
        {
            if (!br.GetBit(probas[p + 7])) return 5 + (br.GetBit(159) ? 1 : 0);
            return 7 + (br.GetBit(165) ? 2 : 0) + (br.GetBit(145) ? 1 : 0);
        }
        var bit1 = br.GetBit(probas[p + 8]) ? 1 : 0;
        var bit0 = br.GetBit(probas[p + 9 + bit1]) ? 1 : 0;
        var cat = 2 * bit1 + bit0;
        var table = cat switch { 0 => Vp8Tables.Cat3, 1 => Vp8Tables.Cat4, 2 => Vp8Tables.Cat5, _ => Vp8Tables.Cat6 };
        var v = 0;
        foreach (var prob in table) v += v + (br.GetBit(prob) ? 1 : 0);
        return v + 3 + (8 << cat);
    }

    // ------------------------------------------------------------------ reconstruction

    // The work area holds one macroblock with the row above it (and four pixels past its right edge, which 4x4
    // prediction reaches) and the column to its left: luma at the top, then the two chroma planes.
    internal const int Bps = 32;
    internal const int YOffset = Bps + 8;
    internal const int UOffset = YOffset + 16 * Bps + Bps;
    internal const int VOffset = UOffset + 16;
    internal const int WorkSize = UOffset + 8 * Bps + 32;

    private void Reconstruct(int mbx, int mby, bool is4x4, int yMode, ReadOnlySpan<byte> modes, int uvMode, short[] coeffs, Span<byte> work)
    {
        LoadEdges(_frame, mbx, mby, work);

        if (is4x4)
        {
            // Blocks in the right column of rows below the first take the pixels past the macroblock's own top right
            // corner as their above-right neighbours, as the bitstream defines.
            for (var r = 3; r < 12; r += 4) work.Slice(YOffset - Bps + 16, 4).CopyTo(work.Slice(YOffset + r * Bps + 16, 4));
            for (var n = 0; n < 16; n++)
            {
                var offset = YOffset + (n >> 2) * 4 * Bps + (n & 3) * 4;
                Vp8Predict.Luma4(modes[n], work, offset);
                AddResidual(coeffs.AsSpan(n * 16, 16), work, offset);
            }
        }
        else
        {
            Vp8Predict.Luma16(yMode, work, YOffset, mbx > 0, mby > 0);
            for (var n = 0; n < 16; n++) AddResidual(coeffs.AsSpan(n * 16, 16), work, YOffset + (n >> 2) * 4 * Bps + (n & 3) * 4);
        }
        Vp8Predict.Chroma8(uvMode, work, UOffset, mbx > 0, mby > 0);
        Vp8Predict.Chroma8(uvMode, work, VOffset, mbx > 0, mby > 0);
        for (var n = 0; n < 4; n++)
        {
            var o = (n >> 1) * 4 * Bps + (n & 1) * 4;
            AddResidual(coeffs.AsSpan((16 + n) * 16, 16), work, UOffset + o);
            AddResidual(coeffs.AsSpan((20 + n) * 16, 16), work, VOffset + o);
        }

        StoreMacroblock(_frame, mbx, mby, work);
    }

    /// <summary>Copies a reconstructed macroblock from the work area into the frame.</summary>
    internal static void StoreMacroblock(Vp8Frame f, int mbx, int mby, ReadOnlySpan<byte> work)
    {
        for (var r = 0; r < 16; r++) work.Slice(YOffset + r * Bps, 16).CopyTo(f.Y.AsSpan((mby * 16 + r) * f.YStride + mbx * 16, 16));
        for (var r = 0; r < 8; r++)
        {
            work.Slice(UOffset + r * Bps, 8).CopyTo(f.U.AsSpan((mby * 8 + r) * f.UVStride + mbx * 8, 8));
            work.Slice(VOffset + r * Bps, 8).CopyTo(f.V.AsSpan((mby * 8 + r) * f.UVStride + mbx * 8, 8));
        }
    }

    /// <summary>
    /// Fills the row above and the column left of the macroblock in the work area from the reconstruction so far. Outside
    /// the frame, the row above reads 127 and the column to the left 129, the corner 127 on the top row and 129 below it.
    /// </summary>
    internal static void LoadEdges(Vp8Frame f, int mbx, int mby, Span<byte> work)
    {
        var top = work.Slice(YOffset - Bps - 1, 21);
        if (mby == 0)
        {
            top.Fill(127);
            work.Slice(UOffset - Bps - 1, 9).Fill(127);
            work.Slice(VOffset - Bps - 1, 9).Fill(127);
        }
        else
        {
            var row = (mby * 16 - 1) * f.YStride + mbx * 16;
            top[0] = mbx > 0 ? f.Y[row - 1] : (byte)129;
            f.Y.AsSpan(row, 16).CopyTo(top[1..]);
            if (mbx < f.MbWidth - 1) f.Y.AsSpan(row + 16, 4).CopyTo(top[17..]);
            else top[17..].Fill(f.Y[row + 15]);

            var uvRow = (mby * 8 - 1) * f.UVStride + mbx * 8;
            work[UOffset - Bps - 1] = mbx > 0 ? f.U[uvRow - 1] : (byte)129;
            work[VOffset - Bps - 1] = mbx > 0 ? f.V[uvRow - 1] : (byte)129;
            f.U.AsSpan(uvRow, 8).CopyTo(work[(UOffset - Bps)..]);
            f.V.AsSpan(uvRow, 8).CopyTo(work[(VOffset - Bps)..]);
        }

        if (mbx == 0)
        {
            for (var r = 0; r < 16; r++) work[YOffset + r * Bps - 1] = 129;
            for (var r = 0; r < 8; r++) work[UOffset + r * Bps - 1] = work[VOffset + r * Bps - 1] = 129;
        }
        else
        {
            for (var r = 0; r < 16; r++) work[YOffset + r * Bps - 1] = f.Y[(mby * 16 + r) * f.YStride + mbx * 16 - 1];
            for (var r = 0; r < 8; r++)
            {
                var i = (mby * 8 + r) * f.UVStride + mbx * 8 - 1;
                work[UOffset + r * Bps - 1] = f.U[i];
                work[VOffset + r * Bps - 1] = f.V[i];
            }
        }
    }

    internal static void AddResidual(ReadOnlySpan<short> block, Span<byte> dst, int offset)
    {
        var acs = false;
        for (var i = 1; i < 16; i++) if (block[i] != 0) { acs = true; break; }
        if (!acs)
        {
            if (block[0] == 0) return;
            var dc = (block[0] + 4) >> 3;
            for (var y = 0; y < 4; y++)
                for (var x = 0; x < 4; x++)
                {
                    ref var p = ref dst[offset + y * Bps + x];
                    p = Clip8(p + dc);
                }
            return;
        }
        InverseDct(block, dst, offset);
    }

    private const int C1 = 20091; // cos(pi/8) * sqrt(2) - 1, in 16 bit fixed point
    private const int C2 = 35468; // sin(pi/8) * sqrt(2)

    /// <summary>The exact inverse DCT of RFC 6386 section 14.4, added to the prediction in place.</summary>
    internal static void InverseDct(ReadOnlySpan<short> input, Span<byte> dst, int offset)
    {
        Span<int> tmp = stackalloc int[16];
        for (var i = 0; i < 4; i++)
        {
            int i0 = input[i], i4 = input[4 + i], i8 = input[8 + i], i12 = input[12 + i];
            var a = i0 + i8;
            var b = i0 - i8;
            var c = ((i4 * C2) >> 16) - (i12 + ((i12 * C1) >> 16));
            var d = (i4 + ((i4 * C1) >> 16)) + ((i12 * C2) >> 16);
            tmp[i] = a + d;
            tmp[12 + i] = a - d;
            tmp[4 + i] = b + c;
            tmp[8 + i] = b - c;
        }
        for (var i = 0; i < 4; i++)
        {
            int t0 = tmp[i * 4], t1 = tmp[i * 4 + 1], t2 = tmp[i * 4 + 2], t3 = tmp[i * 4 + 3];
            var a = t0 + t2;
            var b = t0 - t2;
            var c = ((t1 * C2) >> 16) - (t3 + ((t3 * C1) >> 16));
            var d = (t1 + ((t1 * C1) >> 16)) + ((t3 * C2) >> 16);
            var row = offset + i * Bps;
            dst[row] = Clip8(dst[row] + ((a + d + 4) >> 3));
            dst[row + 3] = Clip8(dst[row + 3] + ((a - d + 4) >> 3));
            dst[row + 1] = Clip8(dst[row + 1] + ((b + c + 4) >> 3));
            dst[row + 2] = Clip8(dst[row + 2] + ((b - c + 4) >> 3));
        }
    }

    /// <summary>The inverse Walsh-Hadamard transform of RFC 6386 section 14.3, writing each result as a luma block's DC.</summary>
    internal static void InverseWalsh(ReadOnlySpan<short> input, Span<short> coeffs)
    {
        Span<int> tmp = stackalloc int[16];
        for (var i = 0; i < 4; i++)
        {
            var a1 = input[i] + input[12 + i];
            var b1 = input[4 + i] + input[8 + i];
            var c1 = input[4 + i] - input[8 + i];
            var d1 = input[i] - input[12 + i];
            tmp[i] = a1 + b1;
            tmp[4 + i] = c1 + d1;
            tmp[8 + i] = a1 - b1;
            tmp[12 + i] = d1 - c1;
        }
        for (var i = 0; i < 4; i++)
        {
            var a1 = tmp[i * 4] + tmp[i * 4 + 3];
            var b1 = tmp[i * 4 + 1] + tmp[i * 4 + 2];
            var c1 = tmp[i * 4 + 1] - tmp[i * 4 + 2];
            var d1 = tmp[i * 4] - tmp[i * 4 + 3];
            coeffs[(i * 4) * 16] = (short)((a1 + b1 + 3) >> 3);
            coeffs[(i * 4 + 1) * 16] = (short)((c1 + d1 + 3) >> 3);
            coeffs[(i * 4 + 2) * 16] = (short)((a1 - b1 + 3) >> 3);
            coeffs[(i * 4 + 3) * 16] = (short)((d1 - c1 + 3) >> 3);
        }
    }

    internal static byte Clip8(int v) => (uint)v <= 255 ? (byte)v : v < 0 ? (byte)0 : (byte)255;

    // ------------------------------------------------------------------ loop filter

    private void LoopFilter()
    {
        // A frame level of 0 turns the filter off whatever the segments and deltas say, as in libvpx and libwebp.
        if (_filterLevel == 0) return;
        var levels = new byte[_mbSegment.Length];
        var inner = new bool[_mbSegment.Length];
        for (var mb = 0; mb < levels.Length; mb++)
        {
            levels[mb] = (byte)FilterLevel(_mbSegment[mb], _mbIs4x4[mb]);
            inner[mb] = _mbIs4x4[mb] || _mbHasCoeffs[mb];
        }
        LoopFilter(_frame, levels, inner, _simpleFilter, _sharpness, _ct);
    }

    /// <summary>
    /// Runs the loop filter over a reconstructed frame in macroblock order: each macroblock's left edge, its inner
    /// vertical edges, its top edge and its inner horizontal edges. <paramref name="inner"/> says which macroblocks have
    /// their inner edges filtered: those predicted in 4x4 blocks or with any non-zero coefficient.
    /// </summary>
    internal static void LoopFilter(Vp8Frame f, ReadOnlySpan<byte> levels, ReadOnlySpan<bool> inner, bool simple, int sharpness, CancellationToken ct)
    {
        byte[] yp = f.Y, up = f.U, vp = f.V;
        int ys = f.YStride, uvs = f.UVStride;
        for (var mby = 0; mby < f.MbHeight; mby++)
        {
            if ((mby & 7) == 0) ct.ThrowIfCancellationRequested();
            for (var mbx = 0; mbx < f.MbWidth; mbx++)
            {
                var mb = mby * f.MbWidth + mbx;
                int level = levels[mb];
                if (level == 0) continue;
                var interior = level;
                if (sharpness > 0)
                {
                    interior >>= sharpness > 4 ? 2 : 1;
                    if (interior > 9 - sharpness) interior = 9 - sharpness;
                }
                if (interior < 1) interior = 1;
                var hev = level >= 40 ? 2 : level >= 15 ? 1 : 0;
                var filterInner = inner[mb];
                var y = (mby * 16) * ys + mbx * 16;
                var uv = (mby * 8) * uvs + mbx * 8;
                if (simple)
                {
                    var mbLimit = (level + 2) * 2 + interior;
                    var bLimit = level * 2 + interior;
                    if (mbx > 0) Vp8Filter.SimpleEdge(yp, y, 1, ys, mbLimit);
                    if (filterInner) for (var i = 4; i < 16; i += 4) Vp8Filter.SimpleEdge(yp, y + i, 1, ys, bLimit);
                    if (mby > 0) Vp8Filter.SimpleEdge(yp, y, ys, 1, mbLimit);
                    if (filterInner) for (var i = 4; i < 16; i += 4) Vp8Filter.SimpleEdge(yp, y + i * ys, ys, 1, bLimit);
                    continue;
                }
                if (mbx > 0)
                {
                    Vp8Filter.MacroblockEdge(yp, y, 1, ys, 16, level + 2, interior, hev);
                    Vp8Filter.MacroblockEdge(up, uv, 1, uvs, 8, level + 2, interior, hev);
                    Vp8Filter.MacroblockEdge(vp, uv, 1, uvs, 8, level + 2, interior, hev);
                }
                if (filterInner)
                {
                    for (var i = 4; i < 16; i += 4) Vp8Filter.SubblockEdge(yp, y + i, 1, ys, 16, level, interior, hev);
                    Vp8Filter.SubblockEdge(up, uv + 4, 1, uvs, 8, level, interior, hev);
                    Vp8Filter.SubblockEdge(vp, uv + 4, 1, uvs, 8, level, interior, hev);
                }
                if (mby > 0)
                {
                    Vp8Filter.MacroblockEdge(yp, y, ys, 1, 16, level + 2, interior, hev);
                    Vp8Filter.MacroblockEdge(up, uv, uvs, 1, 8, level + 2, interior, hev);
                    Vp8Filter.MacroblockEdge(vp, uv, uvs, 1, 8, level + 2, interior, hev);
                }
                if (filterInner)
                {
                    for (var i = 4; i < 16; i += 4) Vp8Filter.SubblockEdge(yp, y + i * ys, ys, 1, 16, level, interior, hev);
                    Vp8Filter.SubblockEdge(up, uv + 4 * uvs, uvs, 1, 8, level, interior, hev);
                    Vp8Filter.SubblockEdge(vp, uv + 4 * uvs, uvs, 1, 8, level, interior, hev);
                }
            }
        }
    }

    private int FilterLevel(int segment, bool is4x4)
    {
        var level = _filterLevel;
        if (_segmentationEnabled)
        {
            level = _absoluteDelta ? _segmentFilter[segment] : level + _segmentFilter[segment];
            level = Math.Clamp(level, 0, 63);
        }
        if (_useLfDelta)
        {
            level += _refLfDelta[0];
            if (is4x4) level += _modeLfDelta[0];
            level = Math.Clamp(level, 0, 63);
        }
        return level;
    }

    /// <summary>Dequantisation factors of one segment.</summary>
    internal readonly record struct Quant(int Y1Dc, int Y1Ac, int Y2Dc, int Y2Ac, int UvDc, int UvAc)
    {
        public static Quant Create(int q, int yDc, int y2Dc, int y2Ac, int uvDc, int uvAc)
        {
            var dc = Vp8Tables.DcTable;
            var ac = Vp8Tables.AcTable;
            var y2AcValue = ac[Math.Clamp(q + y2Ac, 0, 127)] * 155 / 100;
            return new Quant(
                dc[Math.Clamp(q + yDc, 0, 127)],
                ac[Math.Clamp(q, 0, 127)],
                dc[Math.Clamp(q + y2Dc, 0, 127)] * 2,
                y2AcValue < 8 ? 8 : y2AcValue,
                dc[Math.Clamp(q + uvDc, 0, 117)],
                ac[Math.Clamp(q + uvAc, 0, 127)]);
        }
    }
}

/// <summary>The VP8 boolean entropy decoder (RFC 6386 section 7), reading ahead up to eight bytes at a time.</summary>
internal sealed class Vp8BoolDecoder
{
    private readonly byte[] _data;
    private int _pos;
    private readonly int _end;
    private ulong _value; // unread bits, most significant first
    private int _bits;    // how many of _value's bits are real data
    private uint _range = 255;

    public Vp8BoolDecoder(ReadOnlyMemory<byte> data)
    {
        // The caller's array is read in place; only memory no array backs is copied.
        var segment = MemoryMarshal.TryGetArray(data, out var array) ? array : new ArraySegment<byte>(data.ToArray());
        _data = segment.Array!;
        _pos = segment.Offset;
        _end = segment.Offset + segment.Count;
        Fill();
    }

    /// <summary>True once decoding needed bits past the end of the data, which means the stream is truncated.</summary>
    public bool IsOverrun { get; private set; }

    private void Fill()
    {
        while (_bits <= 56 && _pos < _end)
        {
            _value |= (ulong)_data[_pos++] << (56 - _bits);
            _bits += 8;
        }
        if (_bits < 8)
        {
            // Past the end the stream reads as zeros; needing them means the data was cut short.
            IsOverrun = true;
            _bits = 64;
        }
    }

    public bool GetBit(int probability)
    {
        if (_bits < 8) Fill();
        var split = 1 + (((_range - 1) * (uint)probability) >> 8);
        var bigSplit = (ulong)split << 56;
        bool bit;
        if (_value >= bigSplit)
        {
            _range -= split;
            _value -= bigSplit;
            bit = true;
        }
        else
        {
            _range = split;
            bit = false;
        }
        var shift = BitOperations.LeadingZeroCount(_range) - 24;
        _range <<= shift;
        _value <<= shift;
        _bits -= shift;
        return bit;
    }

    public bool ReadFlag() => GetBit(128);

    public int ReadLiteral(int bits)
    {
        var v = 0;
        while (bits-- > 0) v = (v << 1) | (GetBit(128) ? 1 : 0);
        return v;
    }

    /// <summary>A magnitude of <paramref name="bits"/> bits followed by a sign bit, as the frame header codes deltas.</summary>
    public int ReadSigned(int bits)
    {
        var v = ReadLiteral(bits);
        return ReadFlag() ? -v : v;
    }
}

/// <summary>Intra predictors of VP8, writing into a macroblock work area laid out <see cref="Vp8Decoder.Bps"/> bytes per row.</summary>
internal static class Vp8Predict
{
    private const int Bps = Vp8Decoder.Bps;

    public static void Luma16(int mode, Span<byte> b, int o, bool hasLeft, bool hasTop) => Block(mode, b, o, 16, hasLeft, hasTop);

    public static void Chroma8(int mode, Span<byte> b, int o, bool hasLeft, bool hasTop) => Block(mode, b, o, 8, hasLeft, hasTop);

    private static void Block(int mode, Span<byte> b, int o, int size, bool hasLeft, bool hasTop)
    {
        switch (mode)
        {
            case Vp8Decoder.BDc:
            {
                var shift = size == 16 ? 4 : 3;
                int dc;
                if (hasTop && hasLeft)
                {
                    var sum = 0;
                    for (var i = 0; i < size; i++) sum += b[o - Bps + i] + b[o + i * Bps - 1];
                    dc = (sum + size) >> (shift + 1);
                }
                else if (hasTop || hasLeft)
                {
                    var sum = 0;
                    for (var i = 0; i < size; i++) sum += hasTop ? b[o - Bps + i] : b[o + i * Bps - 1];
                    dc = (sum + (size >> 1)) >> shift;
                }
                else dc = 128;
                for (var y = 0; y < size; y++) b.Slice(o + y * Bps, size).Fill((byte)dc);
                break;
            }
            case Vp8Decoder.BTm:
                TrueMotion(b, o, size);
                break;
            case Vp8Decoder.BVe:
                for (var y = 0; y < size; y++) b.Slice(o - Bps, size).CopyTo(b.Slice(o + y * Bps, size));
                break;
            default: // horizontal
                for (var y = 0; y < size; y++) b.Slice(o + y * Bps, size).Fill(b[o + y * Bps - 1]);
                break;
        }
    }

    private static void TrueMotion(Span<byte> b, int o, int size)
    {
        int topLeft = b[o - Bps - 1];
        for (var y = 0; y < size; y++)
        {
            var left = b[o + y * Bps - 1] - topLeft;
            var row = o + y * Bps;
            for (var x = 0; x < size; x++) b[row + x] = Vp8Decoder.Clip8(b[o - Bps + x] + left);
        }
    }

    private static byte Avg3(int x, int y, int z) => (byte)((x + 2 * y + z + 2) >> 2);

    private static byte Avg2(int x, int y) => (byte)((x + y + 1) >> 1);

    /// <summary>Predicts a 4x4 block at <paramref name="o"/> from the eight pixels above it, the four to its left and the corner.</summary>
    public static void Luma4(int mode, Span<byte> b, int o)
    {
        int p = b[o - Bps - 1];
        int a0 = b[o - Bps], a1 = b[o - Bps + 1], a2 = b[o - Bps + 2], a3 = b[o - Bps + 3];
        int a4 = b[o - Bps + 4], a5 = b[o - Bps + 5], a6 = b[o - Bps + 6], a7 = b[o - Bps + 7];
        int l0 = b[o - 1], l1 = b[o + Bps - 1], l2 = b[o + 2 * Bps - 1], l3 = b[o + 3 * Bps - 1];
        Span<byte> r = stackalloc byte[16]; // r[row * 4 + column]
        switch (mode)
        {
            case Vp8Decoder.BDc:
                r.Fill((byte)((a0 + a1 + a2 + a3 + l0 + l1 + l2 + l3 + 4) >> 3));
                break;
            case Vp8Decoder.BTm:
                TrueMotion(b, o, 4);
                return;
            case Vp8Decoder.BVe:
            {
                byte c0 = Avg3(p, a0, a1), c1 = Avg3(a0, a1, a2), c2 = Avg3(a1, a2, a3), c3 = Avg3(a2, a3, a4);
                for (var y = 0; y < 4; y++) { r[y * 4] = c0; r[y * 4 + 1] = c1; r[y * 4 + 2] = c2; r[y * 4 + 3] = c3; }
                break;
            }
            case Vp8Decoder.BHe:
                r[..4].Fill(Avg3(p, l0, l1));
                r[4..8].Fill(Avg3(l0, l1, l2));
                r[8..12].Fill(Avg3(l1, l2, l3));
                r[12..].Fill(Avg3(l2, l3, l3));
                break;
            case Vp8Decoder.BLd:
                r[0] = Avg3(a0, a1, a2);
                r[1] = r[4] = Avg3(a1, a2, a3);
                r[2] = r[5] = r[8] = Avg3(a2, a3, a4);
                r[3] = r[6] = r[9] = r[12] = Avg3(a3, a4, a5);
                r[7] = r[10] = r[13] = Avg3(a4, a5, a6);
                r[11] = r[14] = Avg3(a5, a6, a7);
                r[15] = Avg3(a6, a7, a7);
                break;
            case Vp8Decoder.BRd:
                r[12] = Avg3(l3, l2, l1);
                r[13] = r[8] = Avg3(l2, l1, l0);
                r[14] = r[9] = r[4] = Avg3(l1, l0, p);
                r[15] = r[10] = r[5] = r[0] = Avg3(l0, p, a0);
                r[11] = r[6] = r[1] = Avg3(p, a0, a1);
                r[7] = r[2] = Avg3(a0, a1, a2);
                r[3] = Avg3(a1, a2, a3);
                break;
            case Vp8Decoder.BVr:
                r[12] = Avg3(l2, l1, l0);
                r[8] = Avg3(l1, l0, p);
                r[13] = r[4] = Avg3(l0, p, a0);
                r[9] = r[0] = Avg2(p, a0);
                r[14] = r[5] = Avg3(p, a0, a1);
                r[10] = r[1] = Avg2(a0, a1);
                r[15] = r[6] = Avg3(a0, a1, a2);
                r[11] = r[2] = Avg2(a1, a2);
                r[7] = Avg3(a1, a2, a3);
                r[3] = Avg2(a2, a3);
                break;
            case Vp8Decoder.BVl:
                r[0] = Avg2(a0, a1);
                r[4] = Avg3(a0, a1, a2);
                r[8] = r[1] = Avg2(a1, a2);
                r[5] = r[12] = Avg3(a1, a2, a3);
                r[9] = r[2] = Avg2(a2, a3);
                r[13] = r[6] = Avg3(a2, a3, a4);
                r[10] = r[3] = Avg2(a3, a4);
                r[14] = r[7] = Avg3(a3, a4, a5);
                r[11] = Avg3(a4, a5, a6);
                r[15] = Avg3(a5, a6, a7);
                break;
            case Vp8Decoder.BHd:
                r[12] = Avg2(l3, l2);
                r[13] = Avg3(l3, l2, l1);
                r[8] = r[14] = Avg2(l2, l1);
                r[9] = r[15] = Avg3(l2, l1, l0);
                r[10] = r[4] = Avg2(l1, l0);
                r[11] = r[5] = Avg3(l1, l0, p);
                r[6] = r[0] = Avg2(l0, p);
                r[7] = r[1] = Avg3(l0, p, a0);
                r[2] = Avg3(p, a0, a1);
                r[3] = Avg3(a0, a1, a2);
                break;
            default: // horizontal up
                r[0] = Avg2(l0, l1);
                r[1] = Avg3(l0, l1, l2);
                r[2] = r[4] = Avg2(l1, l2);
                r[3] = r[5] = Avg3(l1, l2, l3);
                r[6] = r[8] = Avg2(l2, l3);
                r[7] = r[9] = Avg3(l2, l3, l3);
                r[10] = r[11] = r[12] = r[13] = r[14] = r[15] = (byte)l3;
                break;
        }
        for (var y = 0; y < 4; y++) r.Slice(y * 4, 4).CopyTo(b[(o + y * Bps)..]);
    }
}

/// <summary>The VP8 loop filters (RFC 6386 section 15), along one edge of <c>count</c> pixels.</summary>
/// <remarks>
/// <c>step</c> crosses the edge (1 for a vertical edge, the stride for a horizontal one); <c>advance</c> moves along it.
/// </remarks>
internal static class Vp8Filter
{
    private static int SignedClamp(int v) => v < -128 ? -128 : v > 127 ? 127 : v;

    public static void SimpleEdge(byte[] px, int pos, int step, int advance, int limit)
    {
        for (var i = 0; i < 16; i++, pos += advance)
        {
            int p1 = px[pos - 2 * step], p0 = px[pos - step], q0 = px[pos], q1 = px[pos + step];
            if (Math.Abs(p0 - q0) * 2 + (Math.Abs(p1 - q1) >> 1) <= limit) Common(px, pos, step, outerTaps: true);
        }
    }

    public static void MacroblockEdge(byte[] px, int pos, int step, int advance, int count, int edgeLimit, int interior, int hevThreshold)
    {
        for (var i = 0; i < count; i++, pos += advance)
        {
            if (!NormalThreshold(px, pos, step, edgeLimit, interior)) continue;
            if (HighEdgeVariance(px, pos, step, hevThreshold)) Common(px, pos, step, outerTaps: true);
            else
            {
                int p2 = px[pos - 3 * step], p1 = px[pos - 2 * step], p0 = px[pos - step];
                int q0 = px[pos], q1 = px[pos + step], q2 = px[pos + 2 * step];
                var w = SignedClamp(SignedClamp(p1 - q1) + 3 * (q0 - p0));
                var a = (27 * w + 63) >> 7;
                px[pos - step] = Vp8Decoder.Clip8(p0 + a);
                px[pos] = Vp8Decoder.Clip8(q0 - a);
                a = (18 * w + 63) >> 7;
                px[pos - 2 * step] = Vp8Decoder.Clip8(p1 + a);
                px[pos + step] = Vp8Decoder.Clip8(q1 - a);
                a = (9 * w + 63) >> 7;
                px[pos - 3 * step] = Vp8Decoder.Clip8(p2 + a);
                px[pos + 2 * step] = Vp8Decoder.Clip8(q2 - a);
            }
        }
    }

    public static void SubblockEdge(byte[] px, int pos, int step, int advance, int count, int edgeLimit, int interior, int hevThreshold)
    {
        for (var i = 0; i < count; i++, pos += advance)
            if (NormalThreshold(px, pos, step, edgeLimit, interior))
                Common(px, pos, step, HighEdgeVariance(px, pos, step, hevThreshold));
    }

    private static bool NormalThreshold(byte[] px, int pos, int step, int edgeLimit, int interior)
    {
        int p3 = px[pos - 4 * step], p2 = px[pos - 3 * step], p1 = px[pos - 2 * step], p0 = px[pos - step];
        int q0 = px[pos], q1 = px[pos + step], q2 = px[pos + 2 * step], q3 = px[pos + 3 * step];
        return Math.Abs(p0 - q0) * 2 + (Math.Abs(p1 - q1) >> 1) <= 2 * edgeLimit + interior
            && Math.Abs(p3 - p2) <= interior && Math.Abs(p2 - p1) <= interior && Math.Abs(p1 - p0) <= interior
            && Math.Abs(q3 - q2) <= interior && Math.Abs(q2 - q1) <= interior && Math.Abs(q1 - q0) <= interior;
    }

    private static bool HighEdgeVariance(byte[] px, int pos, int step, int threshold)
        => Math.Abs(px[pos - 2 * step] - px[pos - step]) > threshold || Math.Abs(px[pos + step] - px[pos]) > threshold;

    private static void Common(byte[] px, int pos, int step, bool outerTaps)
    {
        int p1 = px[pos - 2 * step], p0 = px[pos - step], q0 = px[pos], q1 = px[pos + step];
        var a = 3 * (q0 - p0);
        if (outerTaps) a += SignedClamp(p1 - q1);
        a = SignedClamp(a);
        var f1 = (a + 4 > 127 ? 127 : a + 4) >> 3;
        var f2 = (a + 3 > 127 ? 127 : a + 3) >> 3;
        px[pos - step] = Vp8Decoder.Clip8(p0 + f2);
        px[pos] = Vp8Decoder.Clip8(q0 - f1);
        if (!outerTaps)
        {
            a = (f1 + 1) >> 1;
            px[pos - 2 * step] = Vp8Decoder.Clip8(p1 + a);
            px[pos + step] = Vp8Decoder.Clip8(q1 - a);
        }
    }
}
