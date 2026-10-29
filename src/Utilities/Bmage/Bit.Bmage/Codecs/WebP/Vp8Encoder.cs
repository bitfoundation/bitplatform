namespace Bit.Bmage.Codecs.WebP;

/// <summary>
/// Encoder for lossy VP8 key frames. Each macroblock picks between 16x16 and 4x4 luma prediction and among the chroma
/// modes by rate-distortion cost, the coefficient probabilities are adapted to the image, and the loop filter strength is
/// chosen by measuring it on the reconstruction.
/// </summary>
/// <remarks>
/// Reconstruction runs exactly as the decoder's does, through the same predictors and inverse transforms, so predictions
/// are made from the pixels a decoder will actually have.
/// </remarks>
internal sealed class Vp8Encoder
{
    private const int Bps = Vp8Decoder.Bps;
    private const int YOffset = Vp8Decoder.YOffset;
    private const int UOffset = Vp8Decoder.UOffset;
    private const int VOffset = Vp8Decoder.VOffset;
    private const int MaxLevel = 2047;
    private const int BPredYMode = 4;

    // The cost of coding a 0 or a 1 at each probability (of a 0, out of 256), in 1/256 bits.
    internal static readonly int[] Cost0 = BuildCosts(zero: true);
    internal static readonly int[] Cost1 = BuildCosts(zero: false);
    private static readonly int[] YModeCost = BuildYModeCosts();
    private static readonly int[] UVModeCost = BuildUVModeCosts();
    private static readonly int[] BModeCost = BuildBModeCosts();
    private static readonly (int Node, bool Bit)[][] BModePaths = BuildBModePaths();

    private readonly ImageBuffer _image;
    private readonly CancellationToken _ct;
    private readonly int _width, _height, _mbWidth, _mbHeight;
    private readonly Vp8Frame _source;
    private readonly Vp8Frame _recon;
    private readonly bool[] _transparentMb;
    private readonly int _qIndex;
    private readonly Vp8Decoder.Quant _q;
    private readonly QuantStep _y1, _y2, _uv;
    private readonly long _lambda, _lambdaUV;
    private readonly float _lambdaSad;

    // Decisions, per macroblock.
    private readonly bool[] _is4x4;
    private readonly byte[] _yMode, _uvMode, _bModes;
    private readonly bool[] _skip, _inner;
    private readonly int[] _storeOffset;
    private short[] _store = new short[1 << 16];
    private int _storeLength;

    private readonly byte[] _probas = Vp8Tables.DefaultCoeffProbas.ToArray();
    private readonly bool[] _probaUpdated = new bool[Vp8Tables.DefaultCoeffProbas.Length];

    // Scratch.
    private readonly byte[] _srcY = new byte[256], _srcU = new byte[64], _srcV = new byte[64];
    private readonly float[] _coeffs = new float[16 * 16];
    private readonly short[] _levels = new short[25 * 16], _bestLevels16 = new short[17 * 16], _levels4 = new short[16 * 16];
    private readonly short[] _uvLevels = new short[8 * 16], _bestUVLevels = new short[8 * 16];
    private readonly short[] _raster = new short[16 * 16];
    private readonly byte[] _bestRecon16 = new byte[256], _bestReconUV = new byte[128], _bestRecon4 = new byte[16];
    private readonly byte[] _modes4 = new byte[16];
    private int _mbx, _mby;

    private Vp8Encoder(ImageBuffer image, double quality, CancellationToken ct)
    {
        _image = image;
        _ct = ct;
        _width = image.Width;
        _height = image.Height;
        _source = new Vp8Frame(_width, _height);
        _recon = new Vp8Frame(_width, _height);
        _mbWidth = _source.MbWidth;
        _mbHeight = _source.MbHeight;
        var count = _mbWidth * _mbHeight;
        _transparentMb = new bool[count];
        _is4x4 = new bool[count];
        _yMode = new byte[count];
        _uvMode = new byte[count];
        _bModes = new byte[count * 16];
        _skip = new bool[count];
        _inner = new bool[count];
        _storeOffset = new int[count];

        _qIndex = QualityToIndex(quality);
        _q = Vp8Decoder.Quant.Create(_qIndex, 0, 0, 0, 0, 0);
        // Rounding below one half leaves a dead zone around zero, where most of the bits are saved.
        _y1 = new QuantStep(_q.Y1Dc, _q.Y1Ac, 0.40f, 0.34f);
        _y2 = new QuantStep(_q.Y2Dc, _q.Y2Ac, 0.40f, 0.34f);
        _uv = new QuantStep(_q.UvDc, _q.UvAc, 0.40f, 0.34f);
        // Distortion is a sum of squared errors, so trading it against bits scales with the square of the step.
        _lambda = Math.Max(1, (long)(0.14 * _q.Y1Ac * _q.Y1Ac));
        _lambdaUV = Math.Max(1, (long)(0.14 * _q.UvAc * _q.UvAc));
        _lambdaSad = MathF.Sqrt(_lambda);
    }

    /// <summary>
    /// Maps a quality in [0, 1] to a quantizer index, so that equal steps of quality look like roughly equal steps of
    /// compression: the index falls with the cube root of a quality that is stretched above three quarters.
    /// </summary>
    internal static int QualityToIndex(double quality)
    {
        var c = Math.Clamp(quality, 0, 1);
        var linear = c < 0.75 ? c * (2.0 / 3.0) : 2 * c - 1;
        return Math.Clamp((int)Math.Round(127 * (1 - Math.Cbrt(linear))), 0, 127);
    }

    /// <summary>Encodes the image's colour as a 'VP8 ' chunk payload; alpha, if any, is the caller's to store.</summary>
    public static byte[] Encode(ImageBuffer image, double quality, CancellationToken ct)
    {
        var encoder = new Vp8Encoder(image, quality, ct);
        encoder.ConvertToYuv();
        encoder.DecideMacroblocks();
        var filterLevel = encoder.ChooseFilterLevel();
        encoder.UpdateProbabilities();
        return encoder.WriteFrame(filterLevel);
    }

    // ------------------------------------------------------------------ colour conversion

    // BT.601 with luma in 16..235 and chroma in 16..240, the inverse of the decoder's conversion, in 16 bit fixed point.
    private const int YR = 16829, YG = 33039, YB = 6416;
    private const int UR = -9714, UG = -19070, UB = 28784;
    private const int VR = 28784, VG = -24103, VB = -4681;

    private void ConvertToYuv()
    {
        var hasAlpha = _image.HasTransparency();
        var ys = _source.YStride;
        for (var y = 0; y < _source.MbHeight * 16; y++)
        {
            var row = _image.GetRow(Math.Min(y, _height - 1));
            var dst = _source.Y.AsSpan(y * ys, ys);
            for (var x = 0; x < ys; x++)
            {
                var p = row[Math.Min(x, _width - 1)];
                dst[x] = (byte)((YR * p.R + YG * p.G + YB * p.B + (16 << 16) + 32768) >> 16);
            }
        }

        var uvs = _source.UVStride;
        var chromaWidth = (_width + 1) >> 1;
        var chromaHeight = (_height + 1) >> 1;
        for (var cy = 0; cy < _source.MbHeight * 8; cy++)
        {
            var y0 = Math.Min(2 * Math.Min(cy, chromaHeight - 1), _height - 1);
            var y1 = Math.Min(y0 + 1, _height - 1);
            var r0 = _image.GetRow(y0);
            var r1 = _image.GetRow(y1);
            for (var cx = 0; cx < uvs; cx++)
            {
                var x0 = Math.Min(2 * Math.Min(cx, chromaWidth - 1), _width - 1);
                var x1 = Math.Min(x0 + 1, _width - 1);
                Rgba32 a = r0[x0], b = r0[x1], c = r1[x0], d = r1[x1];
                int sr, sg, sb;
                var weight = a.A + b.A + c.A + d.A;
                if (!hasAlpha || weight == 4 * 255 || weight == 0)
                {
                    sr = a.R + b.R + c.R + d.R;
                    sg = a.G + b.G + c.G + d.G;
                    sb = a.B + b.B + c.B + d.B;
                }
                else
                {
                    // Weighting by alpha keeps the colour of invisible pixels from bleeding into visible ones.
                    sr = (a.R * a.A + b.R * b.A + c.R * c.A + d.R * d.A) * 4 / weight;
                    sg = (a.G * a.A + b.G * b.A + c.G * c.A + d.G * d.A) * 4 / weight;
                    sb = (a.B * a.A + b.B * b.A + c.B * c.A + d.B * d.A) * 4 / weight;
                }
                _source.U[cy * uvs + cx] = Vp8Decoder.Clip8((UR * sr + UG * sg + UB * sb + (128 << 18) + (1 << 17)) >> 18);
                _source.V[cy * uvs + cx] = Vp8Decoder.Clip8((VR * sr + VG * sg + VB * sb + (128 << 18) + (1 << 17)) >> 18);
            }
        }

        if (hasAlpha) FlattenTransparentAreas();
    }

    /// <summary>
    /// Replaces the values under fully transparent pixels, which no one will see, with the average of the visible ones in
    /// the same macroblock, so they cost no detail to code. Wholly transparent macroblocks are marked and later coded as
    /// their own prediction, which costs next to nothing.
    /// </summary>
    private void FlattenTransparentAreas()
    {
        for (var mby = 0; mby < _mbHeight; mby++)
        {
            for (var mbx = 0; mbx < _mbWidth; mbx++)
            {
                long sumY = 0, sumU = 0, sumV = 0;
                int countY = 0, countUV = 0;
                for (var y = mby * 16; y < Math.Min(_height, mby * 16 + 16); y++)
                {
                    var row = _image.GetRow(y);
                    for (var x = mbx * 16; x < Math.Min(_width, mbx * 16 + 16); x++)
                    {
                        if (row[x].A == 0) continue;
                        sumY += _source.Y[y * _source.YStride + x];
                        countY++;
                        var ci = (y >> 1) * _source.UVStride + (x >> 1);
                        sumU += _source.U[ci];
                        sumV += _source.V[ci];
                        countUV++;
                    }
                }
                if (countY == 0)
                {
                    _transparentMb[mby * _mbWidth + mbx] = true;
                    continue;
                }
                if (countY == 256) continue;
                var meanY = (byte)(sumY / countY);
                var meanU = (byte)(sumU / countUV);
                var meanV = (byte)(sumV / countUV);
                for (var y = mby * 16; y < mby * 16 + 16; y++)
                {
                    var row = _image.GetRow(Math.Min(y, _height - 1));
                    for (var x = mbx * 16; x < mbx * 16 + 16; x++)
                        if (y >= _height || x >= _width || row[x].A == 0) _source.Y[y * _source.YStride + x] = meanY;
                }
                for (var cy = mby * 8; cy < mby * 8 + 8; cy++)
                {
                    for (var cx = mbx * 8; cx < mbx * 8 + 8; cx++)
                    {
                        var visible = false;
                        for (var k = 0; k < 4 && !visible; k++)
                        {
                            var y = 2 * cy + (k >> 1);
                            var x = 2 * cx + (k & 1);
                            visible = y < _height && x < _width && _image.GetRow(y)[x].A != 0;
                        }
                        if (visible) continue;
                        _source.U[cy * _source.UVStride + cx] = meanU;
                        _source.V[cy * _source.UVStride + cx] = meanV;
                    }
                }
            }
        }
    }

    // ------------------------------------------------------------------ mode decision

    private void DecideMacroblocks()
    {
        var nzTop = new byte[_mbWidth * 9];
        var nzLeft = new byte[9];
        var modesTop = new byte[_mbWidth * 4];
        var modesLeft = new byte[4];
        var work = new byte[Vp8Decoder.WorkSize];
        for (var mby = 0; mby < _mbHeight; mby++)
        {
            _ct.ThrowIfCancellationRequested();
            Array.Clear(nzLeft);
            Array.Clear(modesLeft);
            for (var mbx = 0; mbx < _mbWidth; mbx++)
            {
                _mbx = mbx;
                _mby = mby;
                EncodeMacroblock(mby * _mbWidth + mbx, work, nzTop.AsSpan(mbx * 9, 9), nzLeft, modesTop.AsSpan(mbx * 4, 4), modesLeft);
                Vp8Decoder.StoreMacroblock(_recon, mbx, mby, work);
            }
        }
    }

    private void EncodeMacroblock(int mb, Span<byte> work, Span<byte> nzTop, Span<byte> nzLeft, Span<byte> modesTop, Span<byte> modesLeft)
    {
        Vp8Decoder.LoadEdges(_recon, _mbx, _mby, work);
        for (var r = 3; r < 12; r += 4) work.Slice(YOffset - Bps + 16, 4).CopyTo(work[(YOffset + r * Bps + 16)..]);
        LoadSource(work, _transparentMb[mb]);

        // 16x16 luma: try every mode and keep the best reconstruction.
        var bestI16 = long.MaxValue;
        var bestMode = 0;
        for (var mode = 0; mode < 4; mode++)
        {
            var score = TryI16(mode, work, nzTop, nzLeft);
            if (score >= bestI16) continue;
            bestI16 = score;
            bestMode = mode;
            CopyBlock(work, YOffset, Bps, _bestRecon16, 0, 16, 16);
            _levels.AsSpan(0, 17 * 16).CopyTo(_bestLevels16);
        }

        // 4x4 luma, given up as soon as it costs more than the best 16x16 mode.
        var is4x4 = !_transparentMb[mb] && TryI4(work, nzTop, nzLeft, modesTop, modesLeft, bestI16);
        if (!is4x4)
        {
            CopyBlock(_bestRecon16, 0, 16, work, YOffset, Bps, 16);
            _bestLevels16.CopyTo(_levels, 0);
        }
        else
        {
            _levels4.CopyTo(_levels, 0);
            _levels.AsSpan(16 * 16, 16).Clear();
        }

        // Chroma.
        var bestUV = long.MaxValue;
        var uvMode = 0;
        for (var mode = 0; mode < 4; mode++)
        {
            var score = TryUV(mode, work, nzTop, nzLeft);
            if (score >= bestUV) continue;
            bestUV = score;
            uvMode = mode;
            CopyBlock(work, UOffset, Bps, _bestReconUV, 0, 8, 8);
            CopyBlock(work, VOffset, Bps, _bestReconUV, 64, 8, 8);
            _uvLevels.CopyTo(_bestUVLevels, 0);
        }
        CopyBlock(_bestReconUV, 0, 8, work, UOffset, Bps, 8);
        CopyBlock(_bestReconUV, 64, 8, work, VOffset, Bps, 8);
        _bestUVLevels.CopyTo(_levels, 17 * 16);

        // Record the decision and update the contexts as a decoder will.
        _is4x4[mb] = is4x4;
        _yMode[mb] = (byte)bestMode;
        _uvMode[mb] = (byte)uvMode;
        if (is4x4)
        {
            _modes4.CopyTo(_bModes, mb * 16);
            for (var x = 0; x < 4; x++) modesTop[x] = _modes4[12 + x];
            for (var y = 0; y < 4; y++) modesLeft[y] = _modes4[y * 4 + 3];
        }
        else
        {
            _bModes.AsSpan(mb * 16, 16).Fill((byte)bestMode);
            modesTop.Fill((byte)bestMode);
            modesLeft.Fill((byte)bestMode);
        }

        var first = is4x4 ? 0 : 1;
        var any = false;
        if (!is4x4)
        {
            var nz = (byte)(LastNonZero(_levels.AsSpan(16 * 16, 16), 0) >= 0 ? 1 : 0);
            nzTop[8] = nzLeft[8] = nz;
            any |= nz != 0;
        }
        for (var b = 0; b < 16; b++)
        {
            var nz = (byte)(LastNonZero(_levels.AsSpan(b * 16, 16), first) >= first ? 1 : 0);
            nzTop[b & 3] = nzLeft[b >> 2] = nz;
            any |= nz != 0;
        }
        for (var b = 0; b < 8; b++)
        {
            var nz = (byte)(LastNonZero(_levels.AsSpan((17 + b) * 16, 16), 0) >= 0 ? 1 : 0);
            var plane = b >> 2;
            nzTop[4 + plane * 2 + (b & 1)] = nzLeft[4 + plane * 2 + ((b >> 1) & 1)] = nz;
            any |= nz != 0;
        }
        _skip[mb] = !any;
        _inner[mb] = is4x4 || HasDecodedCoefficients(is4x4);
        _storeOffset[mb] = _storeLength;
        if (any) Store(is4x4);
    }

    /// <summary>
    /// Whether a decoder will see a non-zero coefficient, which is what makes it filter the inner edges: an AC level, or a
    /// DC that the Walsh-Hadamard transform left non-zero.
    /// </summary>
    private bool HasDecodedCoefficients(bool is4x4)
    {
        for (var b = 0; b < 16; b++)
            if (LastNonZero(_levels.AsSpan(b * 16, 16), is4x4 ? 0 : 1) >= (is4x4 ? 0 : 1)) return true;
        for (var b = 17; b < 25; b++)
            if (LastNonZero(_levels.AsSpan(b * 16, 16), 0) >= 0) return true;
        if (is4x4) return false;
        Span<short> dq = stackalloc short[16];
        Dequantize(_levels.AsSpan(16 * 16, 16), dq, _q.Y2Dc, _q.Y2Ac, 0);
        var raster = _raster.AsSpan();
        raster.Clear();
        Vp8Decoder.InverseWalsh(dq, raster);
        for (var b = 0; b < 16; b++) if (raster[b * 16] != 0) return true;
        return false;
    }

    private void LoadSource(Span<byte> work, bool transparent)
    {
        if (transparent)
        {
            // Nothing here is visible, so the source is made to be what DC prediction gives, which then codes for free.
            Vp8Predict.Luma16(Vp8Decoder.BDc, work, YOffset, _mbx > 0, _mby > 0);
            Vp8Predict.Chroma8(Vp8Decoder.BDc, work, UOffset, _mbx > 0, _mby > 0);
            Vp8Predict.Chroma8(Vp8Decoder.BDc, work, VOffset, _mbx > 0, _mby > 0);
            CopyBlock(work, YOffset, Bps, _srcY, 0, 16, 16);
            CopyBlock(work, UOffset, Bps, _srcU, 0, 8, 8);
            CopyBlock(work, VOffset, Bps, _srcV, 0, 8, 8);
            return;
        }
        CopyBlock(_source.Y, (_mby * 16) * _source.YStride + _mbx * 16, _source.YStride, _srcY, 0, 16, 16);
        CopyBlock(_source.U, (_mby * 8) * _source.UVStride + _mbx * 8, _source.UVStride, _srcU, 0, 8, 8);
        CopyBlock(_source.V, (_mby * 8) * _source.UVStride + _mbx * 8, _source.UVStride, _srcV, 0, 8, 8);
    }

    private long TryI16(int mode, Span<byte> work, ReadOnlySpan<byte> nzTop, ReadOnlySpan<byte> nzLeft)
    {
        Vp8Predict.Luma16(mode, work, YOffset, _mbx > 0, _mby > 0);
        var coeffs = _coeffs.AsSpan();
        Span<float> dc = stackalloc float[16];
        for (var b = 0; b < 16; b++)
        {
            ForwardDct(_srcY, (b >> 2) * 64 + (b & 3) * 4, 16, work, BlockOffset(b), coeffs.Slice(b * 16, 16));
            dc[b] = coeffs[b * 16];
        }
        Span<float> y2 = stackalloc float[16];
        ForwardWalsh(dc, y2);

        var levels = _levels.AsSpan();
        var y2Levels = levels.Slice(16 * 16, 16);
        Quantize(y2, y2Levels, 0, _y2);
        var cost = new CostSink();
        PutCoeffs(ref cost, _probas, 1, nzTop[8] + nzLeft[8], 0, y2Levels);

        var raster = _raster.AsSpan();
        raster.Clear();
        Span<short> dq = stackalloc short[16];
        Dequantize(y2Levels, dq, _q.Y2Dc, _q.Y2Ac, 0);
        Vp8Decoder.InverseWalsh(dq, raster);

        Span<byte> top = stackalloc byte[4];
        Span<byte> left = stackalloc byte[4];
        nzTop[..4].CopyTo(top);
        nzLeft[..4].CopyTo(left);
        for (var b = 0; b < 16; b++)
        {
            var block = levels.Slice(b * 16, 16);
            var last = Quantize(coeffs.Slice(b * 16, 16), block, 1, _y1);
            PutCoeffs(ref cost, _probas, 0, top[b & 3] + left[b >> 2], 1, block);
            top[b & 3] = left[b >> 2] = (byte)(last >= 1 ? 1 : 0);
            Dequantize(block, raster.Slice(b * 16, 16), _q.Y1Dc, _q.Y1Ac, 1);
        }
        for (var b = 0; b < 16; b++) Vp8Decoder.AddResidual(raster.Slice(b * 16, 16), work, BlockOffset(b));
        long distortion = Sse(work, YOffset, _srcY, 0, 16, 16);
        return distortion * 256 + _lambda * (cost.Total + YModeCost[mode]);
    }

    /// <summary>Tries 4x4 prediction for the macroblock, block by block, keeping it only if it beats <paramref name="budget"/>.</summary>
    private bool TryI4(Span<byte> work, ReadOnlySpan<byte> nzTop, ReadOnlySpan<byte> nzLeft, ReadOnlySpan<byte> modesTop, ReadOnlySpan<byte> modesLeft, long budget)
    {
        Span<byte> top = stackalloc byte[4];
        Span<byte> left = stackalloc byte[4];
        nzTop[..4].CopyTo(top);
        nzLeft[..4].CopyTo(left);
        Span<int> candidates = stackalloc int[10];
        Span<float> estimates = stackalloc float[10];
        Span<float> coeffs = stackalloc float[16];
        Span<short> levels = stackalloc short[16];
        Span<short> raster = stackalloc short[16];
        long total = _lambda * YModeCost[BPredYMode];
        for (var n = 0; n < 16; n++)
        {
            var offset = BlockOffset(n);
            var srcOffset = (n >> 2) * 64 + (n & 3) * 4;
            int above = n < 4 ? modesTop[n] : _modes4[n - 4];
            int leftMode = (n & 3) == 0 ? modesLeft[n >> 2] : _modes4[n - 1];
            var modeCosts = BModeCost.AsSpan((above * 10 + leftMode) * 10, 10);

            // Rank the modes by a cheap estimate, the transformed prediction error, and code only the most promising.
            for (var mode = 0; mode < 10; mode++)
            {
                Vp8Predict.Luma4(mode, work, offset);
                estimates[mode] = Satd(_srcY, srcOffset, work, offset) + _lambdaSad * modeCosts[mode] / 256f;
                candidates[mode] = mode;
            }
            for (var i = 1; i < 10; i++)
                for (var j = i; j > 0 && estimates[candidates[j]] < estimates[candidates[j - 1]]; j--)
                    (candidates[j], candidates[j - 1]) = (candidates[j - 1], candidates[j]);

            var best = long.MaxValue;
            var bestMode = 0;
            var bestNz = 0;
            var ctx = top[n & 3] + left[n >> 2];
            for (var c = 0; c < 4; c++)
            {
                var mode = candidates[c];
                Vp8Predict.Luma4(mode, work, offset);
                ForwardDct(_srcY, srcOffset, 16, work, offset, coeffs);
                var last = Quantize(coeffs, levels, 0, _y1);
                var cost = new CostSink();
                PutCoeffs(ref cost, _probas, 3, ctx, 0, levels);
                raster.Clear();
                Dequantize(levels, raster, _q.Y1Dc, _q.Y1Ac, 0);
                Vp8Decoder.AddResidual(raster, work, offset);
                var score = Sse(work, offset, _srcY, srcOffset, 4, 16) * 256L + _lambda * (cost.Total + modeCosts[mode]);
                if (score >= best) continue;
                best = score;
                bestMode = mode;
                bestNz = last >= 0 ? 1 : 0;
                CopyBlock(work, offset, Bps, _bestRecon4, 0, 4, 4);
                levels.CopyTo(_levels4.AsSpan(n * 16, 16));
            }
            CopyBlock(_bestRecon4, 0, 4, work, offset, Bps, 4);
            _modes4[n] = (byte)bestMode;
            top[n & 3] = left[n >> 2] = (byte)bestNz;
            total += best;
            if (total >= budget) return false;
        }
        return true;
    }

    private long TryUV(int mode, Span<byte> work, ReadOnlySpan<byte> nzTop, ReadOnlySpan<byte> nzLeft)
    {
        Vp8Predict.Chroma8(mode, work, UOffset, _mbx > 0, _mby > 0);
        Vp8Predict.Chroma8(mode, work, VOffset, _mbx > 0, _mby > 0);
        Span<byte> top = stackalloc byte[4];
        Span<byte> left = stackalloc byte[4];
        nzTop[4..8].CopyTo(top);
        nzLeft[4..8].CopyTo(left);
        Span<float> coeffs = stackalloc float[16];
        Span<short> raster = stackalloc short[16];
        var cost = new CostSink();
        long distortion = 0;
        for (var b = 0; b < 8; b++)
        {
            var plane = b >> 2;
            var x = b & 1;
            var y = (b >> 1) & 1;
            var src = plane == 0 ? _srcU : _srcV;
            var offset = (plane == 0 ? UOffset : VOffset) + y * 4 * Bps + x * 4;
            ForwardDct(src, y * 32 + x * 4, 8, work, offset, coeffs);
            var levels = _uvLevels.AsSpan(b * 16, 16);
            var last = Quantize(coeffs, levels, 0, _uv);
            PutCoeffs(ref cost, _probas, 2, top[plane * 2 + x] + left[plane * 2 + y], 0, levels);
            top[plane * 2 + x] = left[plane * 2 + y] = (byte)(last >= 0 ? 1 : 0);
            raster.Clear();
            Dequantize(levels, raster, _q.UvDc, _q.UvAc, 0);
            Vp8Decoder.AddResidual(raster, work, offset);
        }
        distortion += Sse(work, UOffset, _srcU, 0, 8, 8);
        distortion += Sse(work, VOffset, _srcV, 0, 8, 8);
        return distortion * 256 + _lambdaUV * (cost.Total + UVModeCost[mode]);
    }

    private static int BlockOffset(int n) => YOffset + (n >> 2) * 4 * Bps + (n & 3) * 4;

    /// <summary>
    /// Appends a macroblock's levels in the order they are coded, the DC block first when there is one: for each block,
    /// the count up to its last non-zero level, then those levels.
    /// </summary>
    private void Store(bool is4x4)
    {
        if (_storeLength + 25 * 17 > _store.Length) Array.Resize(ref _store, _store.Length * 2);
        for (var i = is4x4 ? 1 : 0; i < 25; i++)
        {
            var b = i == 0 ? 16 : i <= 16 ? i - 1 : i;
            var block = _levels.AsSpan(b * 16, 16);
            var count = LastNonZero(block, 0) + 1;
            _store[_storeLength++] = (short)count;
            block[..count].CopyTo(_store.AsSpan(_storeLength));
            _storeLength += count;
        }
    }

    // ------------------------------------------------------------------ transforms and quantization

    private static readonly float CosA = (float)(Math.Sqrt(0.5) * Math.Cos(Math.PI / 8));
    private static readonly float CosB = (float)(Math.Sqrt(0.5) * Math.Cos(3 * Math.PI / 8));

    /// <summary>
    /// The forward transform matching the decoder's inverse DCT: twice the orthonormal 4x4 DCT of the residual, in raster
    /// order with the vertical frequency in the row.
    /// </summary>
    private static void ForwardDct(ReadOnlySpan<byte> src, int srcOffset, int srcStride, ReadOnlySpan<byte> pred, int predOffset, Span<float> output)
    {
        Span<float> t = stackalloc float[16];
        for (var y = 0; y < 4; y++)
        {
            var s = srcOffset + y * srcStride;
            var p = predOffset + y * Bps;
            float r0 = src[s] - pred[p], r1 = src[s + 1] - pred[p + 1], r2 = src[s + 2] - pred[p + 2], r3 = src[s + 3] - pred[p + 3];
            float a = r0 + r3, b = r1 + r2, c = r1 - r2, d = r0 - r3;
            t[y * 4] = 0.5f * (a + b);
            t[y * 4 + 1] = CosA * d + CosB * c;
            t[y * 4 + 2] = 0.5f * (a - b);
            t[y * 4 + 3] = CosB * d - CosA * c;
        }
        for (var x = 0; x < 4; x++)
        {
            float r0 = t[x], r1 = t[4 + x], r2 = t[8 + x], r3 = t[12 + x];
            float a = r0 + r3, b = r1 + r2, c = r1 - r2, d = r0 - r3;
            output[x] = a + b;
            output[4 + x] = 2 * (CosA * d + CosB * c);
            output[8 + x] = a - b;
            output[12 + x] = 2 * (CosB * d - CosA * c);
        }
    }

    /// <summary>The forward Walsh-Hadamard transform matching the decoder's inverse: half the Hadamard transform of the DCs.</summary>
    private static void ForwardWalsh(ReadOnlySpan<float> input, Span<float> output)
    {
        Span<float> t = stackalloc float[16];
        for (var i = 0; i < 4; i++)
        {
            float a = input[i * 4], b = input[i * 4 + 1], c = input[i * 4 + 2], d = input[i * 4 + 3];
            t[i * 4] = a + b + c + d;
            t[i * 4 + 1] = a + b - c - d;
            t[i * 4 + 2] = a - b - c + d;
            t[i * 4 + 3] = a - b + c - d;
        }
        for (var i = 0; i < 4; i++)
        {
            float a = t[i], b = t[4 + i], c = t[8 + i], d = t[12 + i];
            output[i] = 0.5f * (a + b + c + d);
            output[4 + i] = 0.5f * (a + b - c - d);
            output[8 + i] = 0.5f * (a - b - c + d);
            output[12 + i] = 0.5f * (a - b + c - d);
        }
    }

    /// <summary>Quantizes raster coefficients into levels in coding order from <paramref name="first"/>. Returns the last non-zero position, or -1.</summary>
    private static int Quantize(ReadOnlySpan<float> raster, Span<short> levels, int first, QuantStep q)
    {
        var zigzag = Vp8Tables.Zigzag;
        var last = -1;
        for (var n = 0; n < 16; n++)
        {
            if (n < first) { levels[n] = 0; continue; }
            var c = raster[zigzag[n]];
            var a = n == 0 ? MathF.Abs(c) * q.DcInverse + q.DcBias : MathF.Abs(c) * q.AcInverse + q.AcBias;
            var level = a >= MaxLevel ? MaxLevel : (int)a;
            levels[n] = (short)(c < 0 ? -level : level);
            if (level != 0) last = n;
        }
        return last;
    }

    /// <summary>Turns levels in coding order back into coefficients in raster order, as a decoder does.</summary>
    private static void Dequantize(ReadOnlySpan<short> levels, Span<short> raster, int dc, int ac, int first)
    {
        var zigzag = Vp8Tables.Zigzag;
        for (var n = first; n < 16; n++) raster[zigzag[n]] = (short)(levels[n] * (n > 0 ? ac : dc));
    }

    private static int LastNonZero(ReadOnlySpan<short> levels, int first)
    {
        for (var n = 15; n >= first; n--) if (levels[n] != 0) return n;
        return -1;
    }

    private static long Sse(ReadOnlySpan<byte> a, int aOffset, ReadOnlySpan<byte> b, int bOffset, int size, int bStride)
    {
        long sum = 0;
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = a[aOffset + y * Bps + x] - b[bOffset + y * bStride + x];
                sum += d * d;
            }
        return sum;
    }

    /// <summary>The sum of absolute Hadamard transformed differences of a 4x4 block, a cheap stand-in for its coding cost.</summary>
    private static float Satd(ReadOnlySpan<byte> src, int srcOffset, ReadOnlySpan<byte> pred, int predOffset)
    {
        Span<int> t = stackalloc int[16];
        for (var y = 0; y < 4; y++)
        {
            int d0 = src[srcOffset + y * 16] - pred[predOffset + y * Bps], d1 = src[srcOffset + y * 16 + 1] - pred[predOffset + y * Bps + 1];
            int d2 = src[srcOffset + y * 16 + 2] - pred[predOffset + y * Bps + 2], d3 = src[srcOffset + y * 16 + 3] - pred[predOffset + y * Bps + 3];
            int a = d0 + d3, b = d1 + d2, c = d1 - d2, d = d0 - d3;
            t[y * 4] = a + b; t[y * 4 + 1] = c + d; t[y * 4 + 2] = a - b; t[y * 4 + 3] = d - c;
        }
        var sum = 0;
        for (var x = 0; x < 4; x++)
        {
            int a = t[x] + t[12 + x], b = t[4 + x] + t[8 + x], c = t[4 + x] - t[8 + x], d = t[x] - t[12 + x];
            sum += Math.Abs(a + b) + Math.Abs(c + d) + Math.Abs(a - b) + Math.Abs(d - c);
        }
        return sum / 2f;
    }

    private static void CopyBlock(ReadOnlySpan<byte> src, int srcOffset, int srcStride, Span<byte> dst, int dstOffset, int dstStride, int size)
    {
        for (var y = 0; y < size; y++) src.Slice(srcOffset + y * srcStride, size).CopyTo(dst[(dstOffset + y * dstStride)..]);
    }

    private readonly record struct QuantStep(float DcInverse, float AcInverse, float DcBias, float AcBias)
    {
        public QuantStep(int dc, int ac, float dcBias, float acBias) : this(1f / dc, 1f / ac, dcBias, acBias) { }
    }

    // ------------------------------------------------------------------ loop filter strength

    /// <summary>
    /// Picks the loop filter level that brings the reconstruction closest to the source, among a few around the strength
    /// the quantizer step suggests.
    /// </summary>
    private int ChooseFilterLevel()
    {
        var suggested = Math.Clamp((int)Math.Round(0.4 * _q.Y1Ac), 0, 63);
        if (suggested == 0) return 0;
        Span<int> candidates = [suggested * 3 / 5, suggested, Math.Min(63, suggested * 7 / 5)];
        var trial = new Vp8Frame(_width, _height);
        var levels = new byte[_is4x4.Length];
        var best = 0;
        var bestError = FrameError(_recon);
        foreach (var level in candidates)
        {
            if (level == 0) continue;
            _ct.ThrowIfCancellationRequested();
            _recon.Y.CopyTo(trial.Y, 0);
            _recon.U.CopyTo(trial.U, 0);
            _recon.V.CopyTo(trial.V, 0);
            levels.AsSpan().Fill((byte)level);
            Vp8Decoder.LoopFilter(trial, levels, _inner, simple: false, sharpness: 0, _ct);
            var error = FrameError(trial);
            if (error < bestError) { bestError = error; best = level; }
        }
        return best;
    }

    private long FrameError(Vp8Frame f)
    {
        long sum = 0;
        for (var y = 0; y < _height; y++)
        {
            var a = f.Y.AsSpan(y * f.YStride, _width);
            var b = _source.Y.AsSpan(y * f.YStride, _width);
            for (var x = 0; x < a.Length; x++) { var d = a[x] - b[x]; sum += d * d; }
        }
        var cw = (_width + 1) >> 1;
        for (var y = 0; y < (_height + 1) >> 1; y++)
        {
            for (var x = 0; x < cw; x++)
            {
                var i = y * f.UVStride + x;
                int du = f.U[i] - _source.U[i], dv = f.V[i] - _source.V[i];
                sum += du * du + dv * dv;
            }
        }
        return sum;
    }

    // ------------------------------------------------------------------ probabilities

    private void UpdateProbabilities()
    {
        var stats = new StatsSink(new int[_probas.Length * 2]);
        WalkTokens(ref stats);
        var update = Vp8Tables.CoeffUpdateProbas;
        for (var i = 0; i < _probas.Length; i++)
        {
            int n0 = stats.Counts[2 * i], n1 = stats.Counts[2 * i + 1];
            if (n0 + n1 == 0) continue;
            int old = _probas[i];
            var candidate = Math.Clamp((int)((255L * n0 + (n0 + n1) / 2) / (n0 + n1)), 1, 255);
            long oldCost = (long)n0 * Cost0[old] + (long)n1 * Cost1[old] + Cost0[update[i]];
            long newCost = (long)n0 * Cost0[candidate] + (long)n1 * Cost1[candidate] + Cost1[update[i]] + 8 * 256;
            if (newCost >= oldCost) continue;
            _probas[i] = (byte)candidate;
            _probaUpdated[i] = true;
        }
    }

    /// <summary>Feeds every coefficient token of the frame, with the contexts a decoder will use, to <paramref name="sink"/>.</summary>
    private void WalkTokens<TSink>(ref TSink sink) where TSink : struct, ITokenSink
    {
        var nzTop = new byte[_mbWidth * 9];
        Span<byte> nzLeft = stackalloc byte[9];
        Span<short> levels = stackalloc short[16];
        for (var mby = 0; mby < _mbHeight; mby++)
        {
            nzLeft.Clear();
            for (var mbx = 0; mbx < _mbWidth; mbx++)
            {
                var mb = mby * _mbWidth + mbx;
                var top = nzTop.AsSpan(mbx * 9, 9);
                var is4x4 = _is4x4[mb];
                if (_skip[mb])
                {
                    top[..8].Clear();
                    nzLeft[..8].Clear();
                    if (!is4x4) top[8] = nzLeft[8] = 0;
                    continue;
                }
                var pos = _storeOffset[mb];
                if (!is4x4)
                {
                    pos = Load(pos, levels);
                    PutCoeffs(ref sink, _probas, 1, top[8] + nzLeft[8], 0, levels);
                    top[8] = nzLeft[8] = (byte)(LastNonZero(levels, 0) >= 0 ? 1 : 0);
                }
                var first = is4x4 ? 0 : 1;
                var type = is4x4 ? 3 : 0;
                for (var b = 0; b < 16; b++)
                {
                    pos = Load(pos, levels);
                    PutCoeffs(ref sink, _probas, type, top[b & 3] + nzLeft[b >> 2], first, levels);
                    top[b & 3] = nzLeft[b >> 2] = (byte)(LastNonZero(levels, first) >= first ? 1 : 0);
                }
                for (var b = 0; b < 8; b++)
                {
                    pos = Load(pos, levels);
                    var plane = b >> 2;
                    var t = 4 + plane * 2 + (b & 1);
                    var l = 4 + plane * 2 + ((b >> 1) & 1);
                    PutCoeffs(ref sink, _probas, 2, top[t] + nzLeft[l], 0, levels);
                    top[t] = nzLeft[l] = (byte)(LastNonZero(levels, 0) >= 0 ? 1 : 0);
                }
            }
        }
    }

    private int Load(int pos, Span<short> levels)
    {
        int count = _store[pos++];
        levels.Clear();
        _store.AsSpan(pos, count).CopyTo(levels);
        return pos + count;
    }

    /// <summary>Codes one block's levels (in coding order, from <paramref name="first"/>) the way <see cref="Vp8Decoder"/> reads them.</summary>
    private static void PutCoeffs<TSink>(ref TSink sink, byte[] probas, int type, int ctx, int first, ReadOnlySpan<short> levels) where TSink : struct, ITokenSink
    {
        var bands = Vp8Tables.Bands;
        var last = LastNonZero(levels, first);
        var p = Vp8Tables.ProbaIndex(type, bands[first], ctx);
        var n = first;
        var afterZero = false;
        while (n <= last)
        {
            if (!afterZero) sink.Put(probas[p], true, p); // not the end of the block
            int v = levels[n];
            var abs = v < 0 ? -v : v;
            if (abs == 0)
            {
                sink.Put(probas[p + 1], false, p + 1);
                afterZero = true;
                n++;
                p = Vp8Tables.ProbaIndex(type, bands[n], 0);
                continue;
            }
            sink.Put(probas[p + 1], true, p + 1);
            int next;
            if (abs == 1)
            {
                sink.Put(probas[p + 2], false, p + 2);
                next = 1;
            }
            else
            {
                sink.Put(probas[p + 2], true, p + 2);
                PutLargeValue(ref sink, probas, p, abs);
                next = 2;
            }
            sink.Put(128, v < 0, -1);
            afterZero = false;
            n++;
            p = Vp8Tables.ProbaIndex(type, bands[n], next);
        }
        if (n < 16) sink.Put(probas[p], false, p); // end of block
    }

    private static void PutLargeValue<TSink>(ref TSink sink, byte[] probas, int p, int v) where TSink : struct, ITokenSink
    {
        if (v <= 4)
        {
            sink.Put(probas[p + 3], false, p + 3);
            if (v == 2) sink.Put(probas[p + 4], false, p + 4);
            else
            {
                sink.Put(probas[p + 4], true, p + 4);
                sink.Put(probas[p + 5], v == 4, p + 5);
            }
            return;
        }
        sink.Put(probas[p + 3], true, p + 3);
        if (v <= 10)
        {
            sink.Put(probas[p + 6], false, p + 6);
            if (v <= 6)
            {
                sink.Put(probas[p + 7], false, p + 7);
                sink.Put(159, v == 6, -1);
            }
            else
            {
                sink.Put(probas[p + 7], true, p + 7);
                var r = v - 7;
                sink.Put(165, r >= 2, -1);
                sink.Put(145, (r & 1) != 0, -1);
            }
            return;
        }
        sink.Put(probas[p + 6], true, p + 6);
        int cat, bits, baseValue;
        ReadOnlySpan<byte> table;
        if (v <= 18) { cat = 0; bits = 3; baseValue = 11; table = Vp8Tables.Cat3; }
        else if (v <= 34) { cat = 1; bits = 4; baseValue = 19; table = Vp8Tables.Cat4; }
        else if (v <= 66) { cat = 2; bits = 5; baseValue = 35; table = Vp8Tables.Cat5; }
        else { cat = 3; bits = 11; baseValue = 67; table = Vp8Tables.Cat6; }
        var bit1 = cat >> 1;
        sink.Put(probas[p + 8], bit1 != 0, p + 8);
        sink.Put(probas[p + 9 + bit1], (cat & 1) != 0, p + 9 + bit1);
        var rest = v - baseValue;
        for (var i = 0; i < bits; i++) sink.Put(table[i], ((rest >> (bits - 1 - i)) & 1) != 0, -1);
    }

    // ------------------------------------------------------------------ bitstream

    private byte[] WriteFrame(int filterLevel)
    {
        var count = _skip.Length;
        var skipped = 0;
        foreach (var s in _skip) if (s) skipped++;
        var useSkip = skipped > 0;
        var skipProba = useSkip ? Math.Clamp((int)((255L * (count - skipped) + count / 2) / count), 1, 255) : 0;

        var header = new Vp8BoolEncoder(Math.Max(1024, count / 2));
        header.PutLiteral(0, 1); // colour space: YUV
        header.PutLiteral(0, 1); // clamping: the decoder clamps
        header.PutFlag(false); // no segmentation
        header.PutFlag(false); // the normal loop filter
        header.PutLiteral(filterLevel, 6);
        header.PutLiteral(0, 3); // sharpness
        header.PutFlag(false); // no loop filter deltas
        header.PutLiteral(0, 2); // a single token partition
        header.PutLiteral(_qIndex, 7);
        for (var i = 0; i < 5; i++) header.PutFlag(false); // no quantizer deltas
        header.PutFlag(false); // refresh entropy probabilities: there is no next frame
        var update = Vp8Tables.CoeffUpdateProbas;
        for (var i = 0; i < _probas.Length; i++)
        {
            header.PutBit(update[i], _probaUpdated[i]);
            if (_probaUpdated[i]) header.PutLiteral(_probas[i], 8);
        }
        header.PutFlag(useSkip);
        if (useSkip) header.PutLiteral(skipProba, 8);

        var modesTop = new byte[_mbWidth * 4];
        Span<byte> modesLeft = stackalloc byte[4];
        var kf = Vp8Tables.KeyFrameBModeProbas;
        for (var mby = 0; mby < _mbHeight; mby++)
        {
            modesLeft.Clear();
            for (var mbx = 0; mbx < _mbWidth; mbx++)
            {
                var mb = mby * _mbWidth + mbx;
                if (useSkip) header.PutBit(skipProba, _skip[mb]);
                if (_is4x4[mb])
                {
                    header.PutBit(145, false);
                    for (var n = 0; n < 16; n++)
                    {
                        int mode = _bModes[mb * 16 + n];
                        var probas = kf.Slice((modesTop[mbx * 4 + (n & 3)] * 10 + modesLeft[n >> 2]) * 9, 9);
                        foreach (var (node, bit) in BModePaths[mode]) header.PutBit(probas[node >> 1], bit);
                        modesTop[mbx * 4 + (n & 3)] = modesLeft[n >> 2] = (byte)mode;
                    }
                }
                else
                {
                    int mode = _yMode[mb];
                    header.PutBit(145, true);
                    if (mode is Vp8Decoder.BTm or Vp8Decoder.BHe)
                    {
                        header.PutBit(156, true);
                        header.PutBit(128, mode == Vp8Decoder.BTm);
                    }
                    else
                    {
                        header.PutBit(156, false);
                        header.PutBit(163, mode == Vp8Decoder.BVe);
                    }
                    modesTop.AsSpan(mbx * 4, 4).Fill((byte)mode);
                    modesLeft.Fill((byte)mode);
                }
                int uv = _uvMode[mb];
                header.PutBit(142, uv != Vp8Decoder.BDc);
                if (uv != Vp8Decoder.BDc)
                {
                    header.PutBit(114, uv != Vp8Decoder.BVe);
                    if (uv != Vp8Decoder.BVe) header.PutBit(183, uv == Vp8Decoder.BTm);
                }
            }
        }
        var first = header.Finish();
        if (first.Length >= 1 << 19)
            throw new ImageEncodeException("The image is too large for a lossy WebP at this quality: its macroblock headers exceed the format's 512 KB limit. Lower the quality or the dimensions, or encode losslessly.");

        var tokens = new TokenWriter(new Vp8BoolEncoder(Math.Max(4096, _storeLength)));
        WalkTokens(ref tokens);
        var data = tokens.Encoder.Finish();

        var output = new byte[10 + first.Length + data.Length];
        var tag = (first.Length << 5) | 0x10; // key frame, version 0, shown
        output[0] = (byte)tag;
        output[1] = (byte)(tag >> 8);
        output[2] = (byte)(tag >> 16);
        output[3] = 0x9D;
        output[4] = 0x01;
        output[5] = 0x2A;
        output[6] = (byte)_width;
        output[7] = (byte)(_width >> 8);
        output[8] = (byte)_height;
        output[9] = (byte)(_height >> 8);
        first.CopyTo(output, 10);
        data.CopyTo(output, 10 + first.Length);
        return output;
    }

    // ------------------------------------------------------------------ cost tables

    private static int[] BuildCosts(bool zero)
    {
        var costs = new int[256];
        for (var p = 0; p < 256; p++)
        {
            var probability = zero ? Math.Max(p, 1) / 256.0 : (256 - p) / 256.0;
            costs[p] = (int)Math.Round(-Math.Log2(probability) * 256);
        }
        return costs;
    }

    private static int[] BuildYModeCosts()
    {
        // DC, TM, V, H, then 4x4 prediction.
        var notB = Cost1[145];
        return
        [
            notB + Cost0[156] + Cost0[163],
            notB + Cost1[156] + Cost1[128],
            notB + Cost0[156] + Cost1[163],
            notB + Cost1[156] + Cost0[128],
            Cost0[145],
        ];
    }

    private static int[] BuildUVModeCosts() =>
    [
        Cost0[142],
        Cost1[142] + Cost1[114] + Cost1[183],
        Cost1[142] + Cost0[114],
        Cost1[142] + Cost1[114] + Cost0[183],
    ];

    private static (int Node, bool Bit)[][] BuildBModePaths()
    {
        var tree = new sbyte[] { -Vp8Decoder.BDc, 2, -Vp8Decoder.BTm, 4, -Vp8Decoder.BVe, 6, 8, 12, -Vp8Decoder.BHe, 10, -Vp8Decoder.BRd, -Vp8Decoder.BVr, -Vp8Decoder.BLd, 14, -Vp8Decoder.BVl, 16, -Vp8Decoder.BHd, -Vp8Decoder.BHu };
        var paths = new (int, bool)[10][];
        void Walk(int node, List<(int, bool)> path)
        {
            for (var bit = 0; bit < 2; bit++)
            {
                var next = tree[node + bit];
                path.Add((node, bit == 1));
                if (next <= 0) paths[-next] = [.. path];
                else Walk(next, path);
                path.RemoveAt(path.Count - 1);
            }
        }
        Walk(0, []);
        return paths;
    }

    private static int[] BuildBModeCosts()
    {
        var paths = BuildBModePaths();
        var kf = Vp8Tables.KeyFrameBModeProbas;
        var costs = new int[10 * 10 * 10];
        for (var ctx = 0; ctx < 100; ctx++)
            for (var mode = 0; mode < 10; mode++)
            {
                var cost = 0;
                foreach (var (node, bit) in paths[mode])
                {
                    int p = kf[ctx * 9 + (node >> 1)];
                    cost += bit ? Cost1[p] : Cost0[p];
                }
                costs[ctx * 10 + mode] = cost;
            }
        return costs;
    }

    // ------------------------------------------------------------------ token sinks

    internal interface ITokenSink
    {
        /// <summary>One coded bit, at <paramref name="probability"/>; <paramref name="index"/> is its adaptable probability's position, or -1 for a fixed one.</summary>
        void Put(int probability, bool bit, int index);
    }

    private struct CostSink : ITokenSink
    {
        public int Total;
        public void Put(int probability, bool bit, int index) => Total += bit ? Cost1[probability] : Cost0[probability];
    }

    private readonly struct StatsSink(int[] counts) : ITokenSink
    {
        public int[] Counts { get; } = counts;
        public void Put(int probability, bool bit, int index)
        {
            if (index >= 0) Counts[2 * index + (bit ? 1 : 0)]++;
        }
    }

    private readonly struct TokenWriter(Vp8BoolEncoder encoder) : ITokenSink
    {
        public Vp8BoolEncoder Encoder { get; } = encoder;
        public void Put(int probability, bool bit, int index) => Encoder.PutBit(probability, bit);
    }
}

/// <summary>The VP8 boolean entropy encoder (RFC 6386 section 7.3).</summary>
internal sealed class Vp8BoolEncoder
{
    private byte[] _buffer;
    private int _length;
    private uint _range = 255;
    private uint _bottom;
    private int _bitCount = 24;

    public Vp8BoolEncoder(int capacity) => _buffer = new byte[capacity];

    public void PutBit(int probability, bool bit)
    {
        var split = 1 + (((_range - 1) * (uint)probability) >> 8);
        if (bit)
        {
            _bottom += split;
            _range -= split;
        }
        else _range = split;
        while (_range < 128)
        {
            _range <<= 1;
            if ((_bottom & (1u << 31)) != 0) AddOne();
            _bottom <<= 1;
            if (--_bitCount == 0)
            {
                Emit((byte)(_bottom >> 24));
                _bottom &= (1 << 24) - 1;
                _bitCount = 8;
            }
        }
    }

    public void PutFlag(bool value) => PutBit(128, value);

    public void PutLiteral(int value, int bits)
    {
        while (bits-- > 0) PutBit(128, ((value >> bits) & 1) != 0);
    }

    private void AddOne()
    {
        // A carry into bytes already written; the arithmetic guarantees it never runs off the front.
        var i = _length - 1;
        while (_buffer[i] == 255) _buffer[i--] = 0;
        _buffer[i]++;
    }

    private void Emit(byte b)
    {
        if (_length == _buffer.Length) Array.Resize(ref _buffer, _buffer.Length * 2);
        _buffer[_length++] = b;
    }

    public byte[] Finish()
    {
        var c = _bitCount;
        var v = _bottom;
        if ((v & (1u << (32 - c))) != 0) AddOne();
        v <<= c & 7;
        c >>= 3;
        while (--c >= 0) v <<= 8;
        for (var i = 0; i < 4; i++)
        {
            Emit((byte)(v >> 24));
            v <<= 8;
        }
        return _buffer.AsSpan(0, _length).ToArray();
    }
}
