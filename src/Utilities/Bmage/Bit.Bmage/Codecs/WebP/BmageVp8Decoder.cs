using System.Numerics;
using System.Runtime.InteropServices;

namespace Bit.Bmage;

/// <summary>
/// Decoder for the lossy VP8 key frames WebP stores (RFC 6386): boolean entropy coding, intra prediction of 16x16 and
/// 4x4 luma and 8x8 chroma blocks, the 4x4 DCT and Walsh-Hadamard transforms and the normal and simple loop filters.
/// </summary>
/// <remarks>
/// Prediction reads the unfiltered reconstruction of the neighbouring blocks, so the frame is reconstructed in full first
/// and the loop filter runs over it afterwards, in macroblock order, exactly as the format defines it.
/// </remarks>
internal sealed class BmageVp8Decoder
{
    // 4x4 subblock modes, numbered as the format's mode trees and probability tables number them. The first four
    // double as the 16x16 and chroma modes they are implied by.
    internal const int BDc = 0, BTm = 1, BVe = 2, BHe = 3, BLd = 4, BRd = 5, BVr = 6, BVl = 7, BHd = 8, BHu = 9;

    private static ReadOnlySpan<sbyte> BModeTree => [-BDc, 2, -BTm, 4, -BVe, 6, 8, 12, -BHe, 10, -BRd, -BVr, -BLd, 14, -BVl, 16, -BHd, -BHu];

    private readonly BmageVp8Frame _frame;
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

    private readonly byte[] _probas = BmageVp8Tables.DefaultCoeffProbas.ToArray();
    private bool _useSkipProba;
    private int _skipProba;
    private readonly Quant[] _quant = new Quant[4];

    // Per macroblock state the loop filter needs once the frame is reconstructed.
    private readonly byte[] _mbSegment;
    private readonly bool[] _mbIs4x4;
    private readonly bool[] _mbHasCoeffs;

    private BmageVp8Decoder(int width, int height, CancellationToken ct)
    {
        _frame = new BmageVp8Frame(width, height);
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
    public static BmageVp8Frame Decode(ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        var data = payload.Span;
        if (ReadHeader(data) is not { } size) throw new BmageImageDecodeException("Invalid WebP lossy header: not a VP8 key frame.");
        var tag = data[0] | data[1] << 8 | data[2] << 16;
        var version = (tag >> 1) & 7;
        var show = (tag >> 4) & 1;
        var firstPartitionSize = tag >> 5;
        if (version > 3) throw new BmageImageDecodeException($"Unsupported VP8 version {version}.");
        if (show == 0) throw new BmageImageDecodeException("Invalid WebP lossy data: the frame is not displayable.");
        if (firstPartitionSize > data.Length - 10) throw new BmageImageDecodeException("Truncated WebP lossy data.");

        var decoder = new BmageVp8Decoder(size.Width, size.Height, ct);
        var br = new BmageVp8BoolDecoder(payload.Slice(10, firstPartitionSize));
        var partitions = decoder.ParseHeader(br, payload[(10 + firstPartitionSize)..]);
        decoder.DecodeMacroblocks(br, partitions);
        decoder.LoopFilter();
        return decoder._frame;
    }

    // ------------------------------------------------------------------ frame header

    private BmageVp8BoolDecoder[] ParseHeader(BmageVp8BoolDecoder br, ReadOnlyMemory<byte> rest)
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
        if (rest.Length < sizesLength) throw new BmageImageDecodeException("Truncated WebP lossy data.");
        var partitions = new BmageVp8BoolDecoder[count];
        var sizes = rest.Span;
        var start = sizesLength;
        for (var p = 0; p < count; p++)
        {
            var length = p < count - 1 ? sizes[3 * p] | sizes[3 * p + 1] << 8 | sizes[3 * p + 2] << 16 : rest.Length - start;
            length = Math.Min(length, rest.Length - start);
            partitions[p] = new BmageVp8BoolDecoder(rest.Slice(start, length));
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

        var update = BmageVp8Tables.CoeffUpdateProbas;
        for (var i = 0; i < _probas.Length; i++)
            if (br.GetBit(update[i])) _probas[i] = (byte)br.ReadLiteral(8);

        _useSkipProba = br.ReadFlag();
        if (_useSkipProba) _skipProba = br.ReadLiteral(8);
        if (br.IsOverrun) throw new BmageImageDecodeException("Truncated WebP lossy header.");
        return partitions;
    }

    // ------------------------------------------------------------------ macroblocks

    private void DecodeMacroblocks(BmageVp8BoolDecoder br, BmageVp8BoolDecoder[] partitions)
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
                    var kf = BmageVp8Tables.KeyFrameBModeProbas;
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
                if (tokens.IsOverrun || br.IsOverrun) throw new BmageImageDecodeException("Truncated WebP lossy data.");

                _mbSegment[mb] = (byte)segment;
                _mbIs4x4[mb] = is4x4;
                _mbHasCoeffs[mb] = hasCoeffs;
                Reconstruct(mbx, mby, is4x4, yMode, modes, uvMode, coeffs, work);
            }
        }
    }

    /// <summary>Reads the coefficients of one macroblock, dequantised, in raster order per block. Returns true if any is non-zero.</summary>
    private bool ParseResiduals(BmageVp8BoolDecoder br, Quant q, bool is4x4, short[] coeffs, Span<byte> nzTop, Span<byte> nzLeft)
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
    private int GetCoeffs(BmageVp8BoolDecoder br, int type, int ctx, int dcQ, int acQ, int n, Span<short> output)
    {
        var probas = _probas;
        var bands = BmageVp8Tables.Bands;
        var zigzag = BmageVp8Tables.Zigzag;
        var p = BmageVp8Tables.ProbaIndex(type, bands[n], ctx);
        for (; n < 16; n++)
        {
            if (!br.GetBit(probas[p])) return n; // end of block
            while (!br.GetBit(probas[p + 1])) // zero; no end of block can follow one
            {
                if (++n == 16) return 16;
                p = BmageVp8Tables.ProbaIndex(type, bands[n], 0);
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
            p = BmageVp8Tables.ProbaIndex(type, bands[n + 1], next);
        }
        return 16;
    }

    private static int GetLargeValue(BmageVp8BoolDecoder br, byte[] probas, int p)
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
        var table = cat switch { 0 => BmageVp8Tables.Cat3, 1 => BmageVp8Tables.Cat4, 2 => BmageVp8Tables.Cat5, _ => BmageVp8Tables.Cat6 };
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
                BmageVp8Predict.Luma4(modes[n], work, offset);
                AddResidual(coeffs.AsSpan(n * 16, 16), work, offset);
            }
        }
        else
        {
            BmageVp8Predict.Luma16(yMode, work, YOffset, mbx > 0, mby > 0);
            for (var n = 0; n < 16; n++) AddResidual(coeffs.AsSpan(n * 16, 16), work, YOffset + (n >> 2) * 4 * Bps + (n & 3) * 4);
        }
        BmageVp8Predict.Chroma8(uvMode, work, UOffset, mbx > 0, mby > 0);
        BmageVp8Predict.Chroma8(uvMode, work, VOffset, mbx > 0, mby > 0);
        for (var n = 0; n < 4; n++)
        {
            var o = (n >> 1) * 4 * Bps + (n & 1) * 4;
            AddResidual(coeffs.AsSpan((16 + n) * 16, 16), work, UOffset + o);
            AddResidual(coeffs.AsSpan((20 + n) * 16, 16), work, VOffset + o);
        }

        StoreMacroblock(_frame, mbx, mby, work);
    }

    /// <summary>Copies a reconstructed macroblock from the work area into the frame.</summary>
    internal static void StoreMacroblock(BmageVp8Frame f, int mbx, int mby, ReadOnlySpan<byte> work)
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
    internal static void LoadEdges(BmageVp8Frame f, int mbx, int mby, Span<byte> work)
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
    internal static void LoopFilter(BmageVp8Frame f, ReadOnlySpan<byte> levels, ReadOnlySpan<bool> inner, bool simple, int sharpness, CancellationToken ct)
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
                    if (mbx > 0) BmageVp8Filter.SimpleEdge(yp, y, 1, ys, mbLimit);
                    if (filterInner) for (var i = 4; i < 16; i += 4) BmageVp8Filter.SimpleEdge(yp, y + i, 1, ys, bLimit);
                    if (mby > 0) BmageVp8Filter.SimpleEdge(yp, y, ys, 1, mbLimit);
                    if (filterInner) for (var i = 4; i < 16; i += 4) BmageVp8Filter.SimpleEdge(yp, y + i * ys, ys, 1, bLimit);
                    continue;
                }
                if (mbx > 0)
                {
                    BmageVp8Filter.MacroblockEdge(yp, y, 1, ys, 16, level + 2, interior, hev);
                    BmageVp8Filter.MacroblockEdge(up, uv, 1, uvs, 8, level + 2, interior, hev);
                    BmageVp8Filter.MacroblockEdge(vp, uv, 1, uvs, 8, level + 2, interior, hev);
                }
                if (filterInner)
                {
                    for (var i = 4; i < 16; i += 4) BmageVp8Filter.SubblockEdge(yp, y + i, 1, ys, 16, level, interior, hev);
                    BmageVp8Filter.SubblockEdge(up, uv + 4, 1, uvs, 8, level, interior, hev);
                    BmageVp8Filter.SubblockEdge(vp, uv + 4, 1, uvs, 8, level, interior, hev);
                }
                if (mby > 0)
                {
                    BmageVp8Filter.MacroblockEdge(yp, y, ys, 1, 16, level + 2, interior, hev);
                    BmageVp8Filter.MacroblockEdge(up, uv, uvs, 1, 8, level + 2, interior, hev);
                    BmageVp8Filter.MacroblockEdge(vp, uv, uvs, 1, 8, level + 2, interior, hev);
                }
                if (filterInner)
                {
                    for (var i = 4; i < 16; i += 4) BmageVp8Filter.SubblockEdge(yp, y + i * ys, ys, 1, 16, level, interior, hev);
                    BmageVp8Filter.SubblockEdge(up, uv + 4 * uvs, uvs, 1, 8, level, interior, hev);
                    BmageVp8Filter.SubblockEdge(vp, uv + 4 * uvs, uvs, 1, 8, level, interior, hev);
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
            var dc = BmageVp8Tables.DcTable;
            var ac = BmageVp8Tables.AcTable;
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
