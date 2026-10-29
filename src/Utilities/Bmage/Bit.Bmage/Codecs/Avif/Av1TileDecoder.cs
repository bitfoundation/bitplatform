namespace Bit.Bmage.Codecs.Avif;

/// <summary>
/// Decodes the tiles of an intra frame: the block syntax of spec section 5.11 with the CDF selection of 8.3, followed by
/// prediction and reconstruction as each transform block is read. Method and variable names follow the spec so the two
/// can be read side by side.
/// </summary>
internal sealed partial class Av1TileDecoder
{
    private const int DcPred = 0, VPred = 1, D67Pred = 8, SmoothPred = 9, SmoothVPred = 10, SmoothHPred = 11, UvCflPred = 13;
    private const int Block4x4 = 0, Block8x8 = 3, Block64x64 = 12, Block128x128 = 15;
    private const int PartitionNone = 0, PartitionHorz = 1, PartitionVert = 2, PartitionSplit = 3, PartitionHorzA = 4, PartitionHorzB = 5,
        PartitionVertA = 6, PartitionVertB = 7, PartitionHorz4 = 8, PartitionVert4 = 9;
    private const int Tx4x4 = 0, Tx16x16 = 2, Tx32x32 = 3, Tx64x64 = 4;

    private readonly Av1Frame _f;
    private readonly Av1SequenceHeader _seq;
    private readonly Av1FrameHeader _hdr;
    private readonly ushort[] _cdf = new ushort[Av1Cdf.Size];
    private readonly Av1SymbolReader _sr = new();
    private readonly int _sbSize4;
    private readonly int _sbMask;

    private int _miRowStart, _miRowEnd, _miColStart, _miColEnd;
    private int _currentQIndex;
    private bool _readDeltas;
    private readonly int[] _deltaLF = new int[4];
    private readonly int[,] _refSgrXqd = new int[3, 2];
    private readonly int[,,] _refLrWiener = new int[3, 2, 3];

    private readonly byte[][] _aboveLevel = new byte[3][], _aboveDc = new byte[3][], _leftLevel = new byte[3][], _leftDc = new byte[3][];
    private readonly byte[][] _blockDecoded = new byte[3][];
    private readonly int _bdStride;

    // Palette colours of the current superblock row (the cache never looks further up), per 4x4 position.
    private readonly ushort[] _bandPaletteY;
    private readonly ushort[] _bandPaletteU;

    // The block being decoded.
    private int _miRow, _miCol, _miSize, _bw4, _bh4;
    private bool _hasChroma, _availU, _availL, _availUChroma, _availLChroma;
    private bool _skip, _lossless, _useIntrabc, _isInter, _useFilterIntra;
    private int _segmentId, _yMode, _uvMode, _angleDeltaY, _angleDeltaUV, _cflAlphaU, _cflAlphaV, _filterIntraMode;
    private int _paletteSizeY, _paletteSizeUV;
    private readonly int[] _paletteY = new int[8], _paletteU = new int[8], _paletteV = new int[8];
    private readonly byte[] _colorMapY = new byte[64 * 64], _colorMapUV = new byte[64 * 64];
    private int _txSize;
    private int _maxLumaW, _maxLumaH;
    private int _mvRow, _mvCol;

    public Av1TileDecoder(Av1Frame frame)
    {
        _f = frame;
        _seq = frame.Seq;
        _hdr = frame.Hdr;
        _sbSize4 = _seq.Use128x128Superblock ? 32 : 16;
        _sbMask = _sbSize4 - 1;
        var aboveLen = frame.Planes[0].Stride / 4 + 32;
        var leftLen = frame.Planes[0].Height / 4 + 32;
        for (var p = 0; p < frame.NumPlanes; p++)
        {
            _aboveLevel[p] = new byte[aboveLen];
            _aboveDc[p] = new byte[aboveLen];
            _leftLevel[p] = new byte[leftLen];
            _leftDc[p] = new byte[leftLen];
        }
        _bdStride = _sbSize4 + 3;
        for (var p = 0; p < frame.NumPlanes; p++) _blockDecoded[p] = new byte[_bdStride * _bdStride];
        _bandPaletteY = new ushort[32 * _f.MiCols * 8];
        _bandPaletteU = new ushort[32 * _f.MiCols * 8];
    }

    private static int Clip3(int lo, int hi, int x) => x < lo ? lo : x > hi ? hi : x;

    private int Clip1(int x)
    {
        var max = (1 << _f.BitDepth) - 1;
        return x < 0 ? 0 : x > max ? max : x;
    }

    private static int Round2(int x, int n) => n == 0 ? x : (x + (1 << (n - 1))) >> n;

    private static int Round2Signed(int x, int n) => x >= 0 ? Round2(x, n) : -Round2(-x, n);

    private static int BlockWidth(int bsize) => Av1Tables.Num4x4BlocksWide[bsize] << 2;

    private static int BlockHeight(int bsize) => Av1Tables.Num4x4BlocksHigh[bsize] << 2;

    private bool IsInside(int r, int c) => c >= _miColStart && c < _miColEnd && r >= _miRowStart && r < _miRowEnd;

    private int Mi(int r, int c) => r * _f.MiCols + c;

    public void DecodeTile(byte[] data, int start, int size, int tileRow, int tileCol, CancellationToken cancellationToken)
    {
        _miRowStart = _hdr.MiRowStarts[tileRow];
        _miRowEnd = _hdr.MiRowStarts[tileRow + 1];
        _miColStart = _hdr.MiColStarts[tileCol];
        _miColEnd = _hdr.MiColStarts[tileCol + 1];
        _currentQIndex = _hdr.BaseQIdx;

        // init_symbol: every tile of an intra frame starts from the default CDFs, with the coefficient set chosen by
        // base_q_idx.
        Av1Cdf.DefaultNonCoeff.CopyTo(_cdf);
        var qctx = _hdr.BaseQIdx <= 20 ? 0 : _hdr.BaseQIdx <= 60 ? 1 : _hdr.BaseQIdx <= 120 ? 2 : 3;
        Av1Cdf.DefaultCoeff.Slice(qctx * Av1Cdf.CoeffSize, Av1Cdf.CoeffSize).CopyTo(_cdf.AsSpan(Av1Cdf.CoeffStart));
        _sr.Init(data, start, size, _hdr.DisableCdfUpdate);

        for (var p = 0; p < _f.NumPlanes; p++)
        {
            Array.Clear(_aboveLevel[p]);
            Array.Clear(_aboveDc[p]);
        }
        Array.Clear(_deltaLF);
        for (var p = 0; p < _f.NumPlanes; p++)
        {
            for (var pass = 0; pass < 2; pass++)
            {
                _refSgrXqd[p, pass] = Av1Tables.SgrprojXqdMid[pass];
                for (var i = 0; i < 3; i++) _refLrWiener[p, pass, i] = Av1Tables.WienerTapsMid[i];
            }
        }
        var sbSize = _seq.Use128x128Superblock ? Block128x128 : Block64x64;
        for (var r = _miRowStart; r < _miRowEnd; r += _sbSize4)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var p = 0; p < _f.NumPlanes; p++)
            {
                Array.Clear(_leftLevel[p]);
                Array.Clear(_leftDc[p]);
            }
            for (var c = _miColStart; c < _miColEnd; c += _sbSize4)
            {
                _readDeltas = _hdr.DeltaQPresent;
                // clear_cdef has nothing to do: every 64x64 cdef_idx starts at -1 and belongs to one superblock.
                ClearBlockDecodedFlags(r, c);
                ReadLr(r, c, sbSize);
                DecodePartition(r, c, sbSize);
            }
        }
    }

    private void ClearBlockDecodedFlags(int r, int c)
    {
        for (var plane = 0; plane < _f.NumPlanes; plane++)
        {
            var subX = plane > 0 ? _f.SubX : 0;
            var subY = plane > 0 ? _f.SubY : 0;
            var sbWidth4 = (_miColEnd - c) >> subX;
            var sbHeight4 = (_miRowEnd - r) >> subY;
            var bd = _blockDecoded[plane];
            for (var y = -1; y <= (_sbSize4 >> subY); y++)
            {
                for (var x = -1; x <= (_sbSize4 >> subX); x++)
                {
                    byte v = 0;
                    if (y < 0 && x < sbWidth4) v = 1;
                    else if (x < 0 && y < sbHeight4) v = 1;
                    bd[(y + 1) * _bdStride + x + 1] = v;
                }
            }
            bd[((_sbSize4 >> subY) + 1) * _bdStride] = 0;
        }
    }

    private bool BlockDecoded(int plane, int y, int x) => _blockDecoded[plane][(y + 1) * _bdStride + x + 1] != 0;

    // ------------------------------------------------------------------ partitions

    private void DecodePartition(int r, int c, int bSize)
    {
        if (r >= _f.MiRows || c >= _f.MiCols) return;
        var availU = IsInside(r - 1, c);
        var availL = IsInside(r, c - 1);
        var num4x4 = Av1Tables.Num4x4BlocksWide[bSize];
        var halfBlock4x4 = num4x4 >> 1;
        var quarterBlock4x4 = halfBlock4x4 >> 1;
        var hasRows = r + halfBlock4x4 < _f.MiRows;
        var hasCols = c + halfBlock4x4 < _f.MiCols;
        int partition;
        if (bSize < Block8x8)
        {
            partition = PartitionNone;
        }
        else
        {
            var bsl = Av1Tables.MiWidthLog2[bSize];
            var above = availU && Av1Tables.MiWidthLog2[_f.MiSizes[Mi(r - 1, c)]] < bsl ? 1 : 0;
            var left = availL && Av1Tables.MiHeightLog2[_f.MiSizes[Mi(r, c - 1)]] < bsl ? 1 : 0;
            var ctx = left * 2 + above;
            int cdfOffset, n;
            switch (bsl)
            {
                case 1: cdfOffset = Av1Cdf.PartitionW8 + ctx * 5; n = 4; break;
                case 2: cdfOffset = Av1Cdf.PartitionW16 + ctx * 11; n = 10; break;
                case 3: cdfOffset = Av1Cdf.PartitionW32 + ctx * 11; n = 10; break;
                case 4: cdfOffset = Av1Cdf.PartitionW64 + ctx * 11; n = 10; break;
                default: cdfOffset = Av1Cdf.PartitionW128 + ctx * 9; n = 8; break;
            }
            if (hasRows && hasCols)
            {
                partition = _sr.Read(_cdf, cdfOffset, n);
            }
            else if (hasCols)
            {
                var psum = P(cdfOffset, PartitionVert) + P(cdfOffset, PartitionSplit) + P(cdfOffset, PartitionHorzA)
                    + P(cdfOffset, PartitionVertA) + P(cdfOffset, PartitionVertB);
                if (bSize != Block128x128) psum += P(cdfOffset, PartitionVert4);
                partition = _sr.ReadBoolWithProbability((uint)(32768 - psum)) == 1 ? PartitionSplit : PartitionHorz;
            }
            else if (hasRows)
            {
                var psum = P(cdfOffset, PartitionHorz) + P(cdfOffset, PartitionSplit) + P(cdfOffset, PartitionHorzA)
                    + P(cdfOffset, PartitionHorzB) + P(cdfOffset, PartitionVertA);
                if (bSize != Block128x128) psum += P(cdfOffset, PartitionHorz4);
                partition = _sr.ReadBoolWithProbability((uint)(32768 - psum)) == 1 ? PartitionSplit : PartitionVert;
            }
            else
            {
                partition = PartitionSplit;
            }
        }
        var subSize = Av1Tables.PartitionSubsize[partition * 22 + bSize];
        var splitSize = Av1Tables.PartitionSubsize[PartitionSplit * 22 + bSize];
        if (subSize >= 22) throw new ImageDecodeException("Invalid AV1 partition.");
        switch (partition)
        {
            case PartitionNone:
                DecodeBlock(r, c, subSize);
                break;
            case PartitionHorz:
                DecodeBlock(r, c, subSize);
                if (hasRows) DecodeBlock(r + halfBlock4x4, c, subSize);
                break;
            case PartitionVert:
                DecodeBlock(r, c, subSize);
                if (hasCols) DecodeBlock(r, c + halfBlock4x4, subSize);
                break;
            case PartitionSplit:
                DecodePartition(r, c, subSize);
                DecodePartition(r, c + halfBlock4x4, subSize);
                DecodePartition(r + halfBlock4x4, c, subSize);
                DecodePartition(r + halfBlock4x4, c + halfBlock4x4, subSize);
                break;
            case PartitionHorzA:
                DecodeBlock(r, c, splitSize);
                DecodeBlock(r, c + halfBlock4x4, splitSize);
                DecodeBlock(r + halfBlock4x4, c, subSize);
                break;
            case PartitionHorzB:
                DecodeBlock(r, c, subSize);
                DecodeBlock(r + halfBlock4x4, c, splitSize);
                DecodeBlock(r + halfBlock4x4, c + halfBlock4x4, splitSize);
                break;
            case PartitionVertA:
                DecodeBlock(r, c, splitSize);
                DecodeBlock(r + halfBlock4x4, c, splitSize);
                DecodeBlock(r, c + halfBlock4x4, subSize);
                break;
            case PartitionVertB:
                DecodeBlock(r, c, subSize);
                DecodeBlock(r, c + halfBlock4x4, splitSize);
                DecodeBlock(r + halfBlock4x4, c + halfBlock4x4, splitSize);
                break;
            case PartitionHorz4:
                DecodeBlock(r, c, subSize);
                DecodeBlock(r + quarterBlock4x4, c, subSize);
                DecodeBlock(r + quarterBlock4x4 * 2, c, subSize);
                if (r + quarterBlock4x4 * 3 < _f.MiRows) DecodeBlock(r + quarterBlock4x4 * 3, c, subSize);
                break;
            default:
                DecodeBlock(r, c, subSize);
                DecodeBlock(r, c + quarterBlock4x4, subSize);
                DecodeBlock(r, c + quarterBlock4x4 * 2, subSize);
                if (c + quarterBlock4x4 * 3 < _f.MiCols) DecodeBlock(r, c + quarterBlock4x4 * 3, subSize);
                break;
        }
    }

    /// <summary>The probability of one partition symbol, out of 32768, from the partition CDF.</summary>
    private int P(int cdfOffset, int symbol) => _cdf[cdfOffset + symbol] - (symbol > 0 ? _cdf[cdfOffset + symbol - 1] : 0);

    // ------------------------------------------------------------------ blocks

    private void DecodeBlock(int r, int c, int subSize)
    {
        _miRow = r;
        _miCol = c;
        _miSize = subSize;
        var bw4 = _bw4 = Av1Tables.Num4x4BlocksWide[subSize];
        var bh4 = _bh4 = Av1Tables.Num4x4BlocksHigh[subSize];
        if (bh4 == 1 && _f.SubY == 1 && (r & 1) == 0) _hasChroma = false;
        else if (bw4 == 1 && _f.SubX == 1 && (c & 1) == 0) _hasChroma = false;
        else _hasChroma = _f.NumPlanes > 1;
        _availU = IsInside(r - 1, c);
        _availL = IsInside(r, c - 1);
        _availUChroma = _availU;
        _availLChroma = _availL;
        if (_hasChroma)
        {
            if (_f.SubY == 1 && bh4 == 1) _availUChroma = IsInside(r - 2, c);
            if (_f.SubX == 1 && bw4 == 1) _availLChroma = IsInside(r, c - 2);
        }
        else
        {
            _availUChroma = false;
            _availLChroma = false;
        }

        IntraFrameModeInfo();
        PaletteTokens();
        ReadBlockTxSize();
        if (_skip) ResetBlockContext(bw4, bh4);

        var rowEnd = Math.Min(_f.MiRows, r + bh4);
        var colEnd = Math.Min(_f.MiCols, c + bw4);
        for (var y = r; y < rowEnd; y++)
        {
            var row = y * _f.MiCols;
            for (var x = c; x < colEnd; x++)
            {
                _f.YModes[row + x] = (byte)_yMode;
                if (_hasChroma) _f.UVModes[row + x] = (byte)_uvMode;
                _f.RefFrames0[row + x] = 0;
                if (_isInter)
                {
                    _f.MvRows![row + x] = (short)_mvRow;
                    _f.MvCols![row + x] = (short)_mvCol;
                }
            }
        }

        ComputePrediction();
        Residual();

        for (var y = r; y < rowEnd; y++)
        {
            var row = y * _f.MiCols;
            for (var x = c; x < colEnd; x++)
            {
                _f.IsInters[row + x] = (byte)(_isInter ? 1 : 0);
                _f.Skips[row + x] = (byte)(_skip ? 1 : 0);
                _f.MiSizes[row + x] = (byte)_miSize;
                _f.SegmentIds[row + x] = (byte)_segmentId;
                _f.PaletteSizesY[row + x] = (byte)_paletteSizeY;
                _f.PaletteSizesUV[row + x] = (byte)_paletteSizeUV;
                if (_f.DeltaLFs is { } deltaLFs)
                    for (var i = 0; i < 4; i++) deltaLFs[(row + x) * 4 + i] = (sbyte)_deltaLF[i];
            }
            var band = (y & 31) * _f.MiCols;
            for (var x = c; x < colEnd; x++)
            {
                for (var i = 0; i < _paletteSizeY; i++) _bandPaletteY[(band + x) * 8 + i] = (ushort)_paletteY[i];
                for (var i = 0; i < _paletteSizeUV; i++) _bandPaletteU[(band + x) * 8 + i] = (ushort)_paletteU[i];
            }
        }
    }

    private void ResetBlockContext(int bw4, int bh4)
    {
        for (var plane = 0; plane < 1 + (_hasChroma ? 2 : 0); plane++)
        {
            var subX = plane > 0 ? _f.SubX : 0;
            var subY = plane > 0 ? _f.SubY : 0;
            for (var i = _miCol >> subX; i < (_miCol + bw4) >> subX; i++)
            {
                _aboveLevel[plane][i] = 0;
                _aboveDc[plane][i] = 0;
            }
            for (var i = _miRow >> subY; i < (_miRow + bh4) >> subY; i++)
            {
                _leftLevel[plane][i] = 0;
                _leftDc[plane][i] = 0;
            }
        }
    }

    private void IntraFrameModeInfo()
    {
        _skip = false;
        if (_hdr.SegIdPreSkip) IntraSegmentId();
        ReadSkip();
        if (!_hdr.SegIdPreSkip) IntraSegmentId();
        ReadCdef();
        ReadDeltaQIndex();
        ReadDeltaLf();
        _readDeltas = false;
        _useIntrabc = _hdr.AllowIntrabc && _sr.Read(_cdf, Av1Cdf.Intrabc, 2) == 1;
        _paletteSizeY = 0;
        _paletteSizeUV = 0;
        _useFilterIntra = false;
        _angleDeltaY = 0;
        _angleDeltaUV = 0;
        if (_useIntrabc)
        {
            _isInter = true;
            _yMode = DcPred;
            _uvMode = DcPred;
            FindMvStackAndAssignMv();
            return;
        }
        _isInter = false;
        var aboveMode = Av1Tables.IntraModeContext[_availU ? _f.YModes[Mi(_miRow - 1, _miCol)] : DcPred];
        var leftMode = Av1Tables.IntraModeContext[_availL ? _f.YModes[Mi(_miRow, _miCol - 1)] : DcPred];
        _yMode = _sr.Read(_cdf, Av1Cdf.IntraFrameYMode + (aboveMode * 5 + leftMode) * 14, 13);
        IntraAngleInfoY();
        if (_hasChroma)
        {
            var cflAllowed = _lossless && GetPlaneResidualSize(_miSize, 1) == Block4x4
                || !_lossless && Math.Max(BlockWidth(_miSize), BlockHeight(_miSize)) <= 32;
            _uvMode = cflAllowed
                ? _sr.Read(_cdf, Av1Cdf.UvModeCflAllowed + _yMode * 15, 14)
                : _sr.Read(_cdf, Av1Cdf.UvModeCflNotAllowed + _yMode * 14, 13);
            if (_uvMode == UvCflPred) ReadCflAlphas();
            IntraAngleInfoUv();
        }
        else
        {
            _uvMode = DcPred;
        }
        if (_miSize >= Block8x8 && BlockWidth(_miSize) <= 64 && BlockHeight(_miSize) <= 64 && _hdr.AllowScreenContentTools)
            PaletteModeInfo();
        FilterIntraModeInfo();
    }

    private void IntraSegmentId()
    {
        if (_hdr.SegmentationEnabled) ReadSegmentId();
        else _segmentId = 0;
        _lossless = _hdr.LosslessArray[_segmentId];
    }

    private void ReadSegmentId()
    {
        var prevUL = _availU && _availL ? _f.SegmentIds[Mi(_miRow - 1, _miCol - 1)] : -1;
        var prevU = _availU ? _f.SegmentIds[Mi(_miRow - 1, _miCol)] : -1;
        var prevL = _availL ? _f.SegmentIds[Mi(_miRow, _miCol - 1)] : -1;
        int pred;
        if (prevU == -1) pred = prevL == -1 ? 0 : prevL;
        else if (prevL == -1) pred = prevU;
        else pred = prevUL == prevU ? prevU : prevL;
        if (_skip)
        {
            _segmentId = pred;
            return;
        }
        int ctx;
        if (prevUL < 0) ctx = 0;
        else if (prevUL == prevU && prevUL == prevL) ctx = 2;
        else if (prevUL == prevU || prevUL == prevL || prevU == prevL) ctx = 1;
        else ctx = 0;
        var segmentId = _sr.Read(_cdf, Av1Cdf.SegmentId + ctx * 9, 8);
        _segmentId = Math.Clamp(NegDeinterleave(segmentId, pred, _hdr.LastActiveSegId + 1), 0, 7);
    }

    private static int NegDeinterleave(int diff, int reference, int max)
    {
        if (reference == 0) return diff;
        if (reference >= max - 1) return max - diff - 1;
        if (2 * reference < max)
        {
            if (diff <= 2 * reference)
                return (diff & 1) != 0 ? reference + ((diff + 1) >> 1) : reference - (diff >> 1);
            return diff;
        }
        if (diff <= 2 * (max - reference - 1))
            return (diff & 1) != 0 ? reference + ((diff + 1) >> 1) : reference - (diff >> 1);
        return max - (diff + 1);
    }

    private bool SegFeatureActive(int feature) => _hdr.SegmentationEnabled && _hdr.FeatureEnabled[_segmentId, feature];

    private void ReadSkip()
    {
        if (_hdr.SegIdPreSkip && SegFeatureActive(6))
        {
            _skip = true;
            return;
        }
        var ctx = 0;
        if (_availU) ctx += _f.Skips[Mi(_miRow - 1, _miCol)];
        if (_availL) ctx += _f.Skips[Mi(_miRow, _miCol - 1)];
        _skip = _sr.Read(_cdf, Av1Cdf.Skip + ctx * 3, 2) == 1;
    }

    private void ReadCdef()
    {
        if (_skip || _hdr.CodedLossless || !_seq.EnableCdef || _hdr.AllowIntrabc) return;
        var r = _miRow & ~15;
        var c = _miCol & ~15;
        var idx = (r >> 4) * _f.CdefStride + (c >> 4);
        if (_f.CdefIdx[idx] != -1) return;
        var value = (sbyte)_sr.ReadLiteral(_hdr.CdefBits);
        for (var i = r; i < r + _bh4 && i < _f.MiRows; i += 16)
            for (var j = c; j < c + _bw4 && j < _f.MiCols; j += 16)
                _f.CdefIdx[(i >> 4) * _f.CdefStride + (j >> 4)] = value;
    }

    private void ReadDeltaQIndex()
    {
        var sbSize = _seq.Use128x128Superblock ? Block128x128 : Block64x64;
        if (_miSize == sbSize && _skip) return;
        if (!_readDeltas) return;
        var deltaQAbs = _sr.Read(_cdf, Av1Cdf.DeltaQ, 4);
        if (deltaQAbs == 3)
        {
            var remBits = _sr.ReadLiteral(3) + 1;
            deltaQAbs = _sr.ReadLiteral(remBits) + (1 << remBits) + 1;
        }
        if (deltaQAbs != 0)
        {
            var reduced = _sr.ReadBool() == 1 ? -deltaQAbs : deltaQAbs;
            _currentQIndex = Clip3(1, 255, _currentQIndex + (reduced << _hdr.DeltaQRes));
        }
    }

    private void ReadDeltaLf()
    {
        var sbSize = _seq.Use128x128Superblock ? Block128x128 : Block64x64;
        if (_miSize == sbSize && _skip) return;
        if (!_readDeltas || !_hdr.DeltaLfPresent) return;
        var frameLfCount = 1;
        if (_hdr.DeltaLfMulti) frameLfCount = _f.NumPlanes > 1 ? 4 : 2;
        for (var i = 0; i < frameLfCount; i++)
        {
            var cdf = _hdr.DeltaLfMulti ? Av1Cdf.DeltaLfMulti + i * 5 : Av1Cdf.DeltaLf;
            var deltaLfAbs = _sr.Read(_cdf, cdf, 4);
            if (deltaLfAbs == 3)
            {
                var n = _sr.ReadLiteral(3) + 1;
                deltaLfAbs = _sr.ReadLiteral(n) + (1 << n) + 1;
            }
            if (deltaLfAbs != 0)
            {
                var reduced = _sr.ReadBool() == 1 ? -deltaLfAbs : deltaLfAbs;
                _deltaLF[i] = Clip3(-63, 63, _deltaLF[i] + (reduced << _hdr.DeltaLfRes));
            }
        }
    }

    private static bool IsDirectionalMode(int mode) => mode >= VPred && mode <= D67Pred;

    private void IntraAngleInfoY()
    {
        if (_miSize >= Block8x8 && IsDirectionalMode(_yMode))
            _angleDeltaY = _sr.Read(_cdf, Av1Cdf.AngleDelta + (_yMode - VPred) * 8, 7) - 3;
    }

    private void IntraAngleInfoUv()
    {
        if (_miSize >= Block8x8 && IsDirectionalMode(_uvMode))
            _angleDeltaUV = _sr.Read(_cdf, Av1Cdf.AngleDelta + (_uvMode - VPred) * 8, 7) - 3;
    }

    private void ReadCflAlphas()
    {
        var signs = _sr.Read(_cdf, Av1Cdf.CflSign, 8);
        var signU = (signs + 1) / 3;
        var signV = (signs + 1) % 3;
        if (signU != 0)
        {
            var ctx = (signU - 1) * 3 + signV;
            _cflAlphaU = 1 + _sr.Read(_cdf, Av1Cdf.CflAlpha + ctx * 17, 16);
            if (signU == 1) _cflAlphaU = -_cflAlphaU;
        }
        else
        {
            _cflAlphaU = 0;
        }
        if (signV != 0)
        {
            var ctx = (signV - 1) * 3 + signU;
            _cflAlphaV = 1 + _sr.Read(_cdf, Av1Cdf.CflAlpha + ctx * 17, 16);
            if (signV == 1) _cflAlphaV = -_cflAlphaV;
        }
        else
        {
            _cflAlphaV = 0;
        }
    }

    private void FilterIntraModeInfo()
    {
        _useFilterIntra = false;
        if (_seq.EnableFilterIntra && _yMode == DcPred && _paletteSizeY == 0 && Math.Max(BlockWidth(_miSize), BlockHeight(_miSize)) <= 32)
        {
            _useFilterIntra = _sr.Read(_cdf, Av1Cdf.FilterIntra + _miSize * 3, 2) == 1;
            if (_useFilterIntra) _filterIntraMode = _sr.Read(_cdf, Av1Cdf.FilterIntraMode, 5);
        }
    }

    // ------------------------------------------------------------------ palette

    private void PaletteModeInfo()
    {
        var bsizeCtx = Av1Tables.MiWidthLog2[_miSize] + Av1Tables.MiHeightLog2[_miSize] - 2;
        var bitDepth = _f.BitDepth;
        Span<int> cache = stackalloc int[16];
        if (_yMode == DcPred)
        {
            var ctx = 0;
            if (_availU && _f.PaletteSizesY[Mi(_miRow - 1, _miCol)] > 0) ctx++;
            if (_availL && _f.PaletteSizesY[Mi(_miRow, _miCol - 1)] > 0) ctx++;
            if (_sr.Read(_cdf, Av1Cdf.PaletteYMode + (bsizeCtx * 3 + ctx) * 3, 2) == 1)
            {
                _paletteSizeY = _sr.Read(_cdf, Av1Cdf.PaletteYSize + bsizeCtx * 8, 7) + 2;
                var cacheN = GetPaletteCache(0, cache);
                var idx = 0;
                for (var i = 0; i < cacheN && idx < _paletteSizeY; i++)
                    if (_sr.ReadBool() == 1) _paletteY[idx++] = cache[i];
                if (idx < _paletteSizeY) _paletteY[idx++] = _sr.ReadLiteral(bitDepth);
                var paletteBits = 0;
                if (idx < _paletteSizeY) paletteBits = bitDepth - 3 + _sr.ReadLiteral(2);
                while (idx < _paletteSizeY)
                {
                    var delta = _sr.ReadLiteral(paletteBits) + 1;
                    _paletteY[idx] = Clip1(_paletteY[idx - 1] + delta);
                    var range = (1 << bitDepth) - _paletteY[idx] - 1;
                    paletteBits = Math.Min(paletteBits, CeilLog2(range));
                    idx++;
                }
                Array.Sort(_paletteY, 0, _paletteSizeY);
            }
        }
        if (_hasChroma && _uvMode == DcPred)
        {
            var ctx = _paletteSizeY > 0 ? 1 : 0;
            if (_sr.Read(_cdf, Av1Cdf.PaletteUvMode + ctx * 3, 2) == 1)
            {
                _paletteSizeUV = _sr.Read(_cdf, Av1Cdf.PaletteUvSize + bsizeCtx * 8, 7) + 2;
                var cacheN = GetPaletteCache(1, cache);
                var idx = 0;
                for (var i = 0; i < cacheN && idx < _paletteSizeUV; i++)
                    if (_sr.ReadBool() == 1) _paletteU[idx++] = cache[i];
                if (idx < _paletteSizeUV) _paletteU[idx++] = _sr.ReadLiteral(bitDepth);
                var paletteBits = 0;
                if (idx < _paletteSizeUV) paletteBits = bitDepth - 3 + _sr.ReadLiteral(2);
                while (idx < _paletteSizeUV)
                {
                    var delta = _sr.ReadLiteral(paletteBits);
                    _paletteU[idx] = Clip1(_paletteU[idx - 1] + delta);
                    var range = (1 << bitDepth) - _paletteU[idx];
                    paletteBits = Math.Min(paletteBits, CeilLog2(range));
                    idx++;
                }
                Array.Sort(_paletteU, 0, _paletteSizeUV);
                if (_sr.ReadBool() == 1)
                {
                    var minBits = bitDepth - 4;
                    var maxVal = 1 << bitDepth;
                    var bits = minBits + _sr.ReadLiteral(2);
                    _paletteV[0] = _sr.ReadLiteral(bitDepth);
                    for (idx = 1; idx < _paletteSizeUV; idx++)
                    {
                        var delta = _sr.ReadLiteral(bits);
                        if (delta != 0 && _sr.ReadBool() == 1) delta = -delta;
                        var val = _paletteV[idx - 1] + delta;
                        if (val < 0) val += maxVal;
                        if (val >= maxVal) val -= maxVal;
                        _paletteV[idx] = Clip1(val);
                    }
                }
                else
                {
                    for (idx = 0; idx < _paletteSizeUV; idx++) _paletteV[idx] = _sr.ReadLiteral(bitDepth);
                }
            }
        }
    }

    private static int CeilLog2(int x)
    {
        if (x < 2) return 0;
        var i = 1;
        var p = 2;
        while (p < x)
        {
            i++;
            p <<= 1;
        }
        return i;
    }

    private int GetPaletteCache(int plane, Span<int> cache)
    {
        var sizes = plane == 0 ? _f.PaletteSizesY : _f.PaletteSizesUV;
        var colors = plane == 0 ? _bandPaletteY : _bandPaletteU;
        var aboveN = (_miRow * 4) % 64 != 0 ? sizes[Mi(_miRow - 1, _miCol)] : 0;
        var leftN = _availL ? sizes[Mi(_miRow, _miCol - 1)] : 0;
        var aboveBase = aboveN > 0 ? (((_miRow - 1) & 31) * _f.MiCols + _miCol) * 8 : 0;
        var leftBase = leftN > 0 ? ((_miRow & 31) * _f.MiCols + _miCol - 1) * 8 : 0;
        int aboveIdx = 0, leftIdx = 0, n = 0;
        while (aboveIdx < aboveN && leftIdx < leftN)
        {
            int aboveC = colors[aboveBase + aboveIdx];
            int leftC = colors[leftBase + leftIdx];
            if (leftC < aboveC)
            {
                if (n == 0 || leftC != cache[n - 1]) cache[n++] = leftC;
                leftIdx++;
            }
            else
            {
                if (n == 0 || aboveC != cache[n - 1]) cache[n++] = aboveC;
                aboveIdx++;
                if (leftC == aboveC) leftIdx++;
            }
        }
        while (aboveIdx < aboveN)
        {
            int val = colors[aboveBase + aboveIdx++];
            if (n == 0 || val != cache[n - 1]) cache[n++] = val;
        }
        while (leftIdx < leftN)
        {
            int val = colors[leftBase + leftIdx++];
            if (n == 0 || val != cache[n - 1]) cache[n++] = val;
        }
        return n;
    }

    private void PaletteTokens()
    {
        var blockHeight = BlockHeight(_miSize);
        var blockWidth = BlockWidth(_miSize);
        var onscreenHeight = Math.Min(blockHeight, (_f.MiRows - _miRow) * 4);
        var onscreenWidth = Math.Min(blockWidth, (_f.MiCols - _miCol) * 4);
        if (_paletteSizeY > 0)
        {
            ReadColorMap(_colorMapY, _paletteSizeY, blockWidth, blockHeight, onscreenWidth, onscreenHeight, Av1Cdf.PaletteSize2YColor);
        }
        if (_paletteSizeUV > 0)
        {
            blockHeight >>= _f.SubY;
            blockWidth >>= _f.SubX;
            onscreenHeight >>= _f.SubY;
            onscreenWidth >>= _f.SubX;
            if (blockWidth < 4)
            {
                blockWidth += 2;
                onscreenWidth += 2;
            }
            if (blockHeight < 4)
            {
                blockHeight += 2;
                onscreenHeight += 2;
            }
            ReadColorMap(_colorMapUV, _paletteSizeUV, blockWidth, blockHeight, onscreenWidth, onscreenHeight, Av1Cdf.PaletteSize2UvColor);
        }
    }

    // The colour CDFs for palette sizes 2..8 follow one another, each [5][n + 1].
    private static int ColorCdfOffset(int size2Offset, int n)
    {
        var offset = size2Offset;
        for (var s = 2; s < n; s++) offset += 5 * (s + 1);
        return offset;
    }

    private void ReadColorMap(byte[] map, int n, int blockWidth, int blockHeight, int onscreenWidth, int onscreenHeight, int size2Offset)
    {
        const int stride = 64;
        var cdfBase = ColorCdfOffset(size2Offset, n);
        map[0] = (byte)_sr.ReadNs(n);
        Span<int> colorOrder = stackalloc int[8];
        Span<int> scores = stackalloc int[8];
        for (var i = 1; i < onscreenHeight + onscreenWidth - 1; i++)
        {
            for (var j = Math.Min(i, onscreenWidth - 1); j >= Math.Max(0, i - onscreenHeight + 1); j--)
            {
                var r = i - j;
                var hash = GetPaletteColorContext(map, stride, r, j, n, colorOrder, scores);
                var ctx = Av1Tables.PaletteColorContext[hash];
                if (ctx < 0) throw new ImageDecodeException("Invalid AV1 palette colour context.");
                var idx = _sr.Read(_cdf, cdfBase + ctx * (n + 1), n);
                map[r * stride + j] = (byte)colorOrder[idx];
            }
        }
        for (var i = 0; i < onscreenHeight; i++)
            for (var j = onscreenWidth; j < blockWidth; j++)
                map[i * stride + j] = map[i * stride + onscreenWidth - 1];
        for (var i = onscreenHeight; i < blockHeight; i++)
            for (var j = 0; j < blockWidth; j++)
                map[i * stride + j] = map[(onscreenHeight - 1) * stride + j];
    }

    private static int GetPaletteColorContext(byte[] map, int stride, int r, int c, int n, Span<int> colorOrder, Span<int> scores)
    {
        for (var i = 0; i < 8; i++)
        {
            scores[i] = 0;
            colorOrder[i] = i;
        }
        if (c > 0) scores[map[r * stride + c - 1]] += 2;
        if (r > 0 && c > 0) scores[map[(r - 1) * stride + c - 1]] += 1;
        if (r > 0) scores[map[(r - 1) * stride + c]] += 2;
        for (var i = 0; i < 3; i++)
        {
            var maxScore = scores[i];
            var maxIdx = i;
            for (var j = i + 1; j < n; j++)
            {
                if (scores[j] > maxScore)
                {
                    maxScore = scores[j];
                    maxIdx = j;
                }
            }
            if (maxIdx != i)
            {
                maxScore = scores[maxIdx];
                var maxColorOrder = colorOrder[maxIdx];
                for (var k = maxIdx; k > i; k--)
                {
                    scores[k] = scores[k - 1];
                    colorOrder[k] = colorOrder[k - 1];
                }
                scores[i] = maxScore;
                colorOrder[i] = maxColorOrder;
            }
        }
        return scores[0] * 1 + scores[1] * 2 + scores[2] * 2;
    }

    // ------------------------------------------------------------------ transform sizes

    private void ReadBlockTxSize()
    {
        var bw4 = _bw4;
        var bh4 = _bh4;
        if (_hdr.TxMode == Av1FrameHeader.TxModeSelect && _miSize > Block4x4 && _isInter && !_skip && !_lossless)
        {
            var maxTxSz = Av1Tables.MaxTxSizeRect[_miSize];
            var txW4 = Av1Tables.TxWidth[maxTxSz] / 4;
            var txH4 = Av1Tables.TxHeight[maxTxSz] / 4;
            for (var row = _miRow; row < _miRow + bh4; row += txH4)
                for (var col = _miCol; col < _miCol + bw4; col += txW4)
                    ReadVarTxSize(row, col, maxTxSz, 0);
        }
        else
        {
            ReadTxSize(!_skip || !_isInter);
            var rowEnd = Math.Min(_f.MiRows, _miRow + bh4);
            var colEnd = Math.Min(_f.MiCols, _miCol + bw4);
            for (var row = _miRow; row < rowEnd; row++)
                _f.InterTxSizes.AsSpan(row * _f.MiCols + _miCol, colEnd - _miCol).Fill((byte)_txSize);
        }
    }

    private void ReadTxSize(bool allowSelect)
    {
        if (_lossless)
        {
            _txSize = Tx4x4;
            return;
        }
        var maxRectTxSize = Av1Tables.MaxTxSizeRect[_miSize];
        var maxTxDepth = Av1Tables.MaxTxDepth[_miSize];
        _txSize = maxRectTxSize;
        if (_miSize > Block4x4 && allowSelect && _hdr.TxMode == Av1FrameHeader.TxModeSelect)
        {
            var maxTxWidth = Av1Tables.TxWidth[maxRectTxSize];
            var maxTxHeight = Av1Tables.TxHeight[maxRectTxSize];
            int aboveW, leftH;
            if (_availU && _f.IsInters[Mi(_miRow - 1, _miCol)] != 0) aboveW = BlockWidth(_f.MiSizes[Mi(_miRow - 1, _miCol)]);
            else if (_availU) aboveW = GetAboveTxWidth(_miRow, _miCol);
            else aboveW = 0;
            if (_availL && _f.IsInters[Mi(_miRow, _miCol - 1)] != 0) leftH = BlockHeight(_f.MiSizes[Mi(_miRow, _miCol - 1)]);
            else if (_availL) leftH = GetLeftTxHeight(_miRow, _miCol);
            else leftH = 0;
            var ctx = (aboveW >= maxTxWidth ? 1 : 0) + (leftH >= maxTxHeight ? 1 : 0);
            int txDepth;
            if (maxTxDepth == 4) txDepth = _sr.Read(_cdf, Av1Cdf.Tx64x64 + ctx * 4, 3);
            else if (maxTxDepth == 3) txDepth = _sr.Read(_cdf, Av1Cdf.Tx32x32 + ctx * 4, 3);
            else if (maxTxDepth == 2) txDepth = _sr.Read(_cdf, Av1Cdf.Tx16x16 + ctx * 4, 3);
            else txDepth = _sr.Read(_cdf, Av1Cdf.Tx8x8 + ctx * 3, 2);
            for (var i = 0; i < txDepth; i++) _txSize = Av1Tables.SplitTxSize[_txSize];
        }
    }

    private int GetAboveTxWidth(int row, int col)
    {
        if (row == _miRow)
        {
            if (!_availU) return 64;
            var above = Mi(row - 1, col);
            if (_f.Skips[above] != 0 && _f.IsInters[above] != 0) return BlockWidth(_f.MiSizes[above]);
        }
        return Av1Tables.TxWidth[_f.InterTxSizes[Mi(row - 1, col)]];
    }

    private int GetLeftTxHeight(int row, int col)
    {
        if (col == _miCol)
        {
            if (!_availL) return 64;
            var left = Mi(row, col - 1);
            if (_f.Skips[left] != 0 && _f.IsInters[left] != 0) return BlockHeight(_f.MiSizes[left]);
        }
        return Av1Tables.TxHeight[_f.InterTxSizes[Mi(row, col - 1)]];
    }

    private static int FindTxSize(int w, int h)
    {
        int txSz;
        for (txSz = 0; txSz < 19; txSz++)
            if (Av1Tables.TxWidth[txSz] == w && Av1Tables.TxHeight[txSz] == h) break;
        return txSz;
    }

    private void ReadVarTxSize(int row, int col, int txSz, int depth)
    {
        if (row >= _f.MiRows || col >= _f.MiCols) return;
        bool txfmSplit;
        if (txSz == Tx4x4 || depth == 2)
        {
            txfmSplit = false;
        }
        else
        {
            var above = GetAboveTxWidth(row, col) < Av1Tables.TxWidth[txSz] ? 1 : 0;
            var left = GetLeftTxHeight(row, col) < Av1Tables.TxHeight[txSz] ? 1 : 0;
            var size = Math.Min(64, Math.Max(BlockWidth(_miSize), BlockHeight(_miSize)));
            var maxTxSz = FindTxSize(size, size);
            var txSzSqrUp = Av1Tables.TxSizeSqrUp[txSz];
            var ctx = (txSzSqrUp != maxTxSz ? 1 : 0) * 3 + (4 - maxTxSz) * 6 + above + left;
            txfmSplit = _sr.Read(_cdf, Av1Cdf.TxfmSplit + ctx * 3, 2) == 1;
        }
        var w4 = Av1Tables.TxWidth[txSz] / 4;
        var h4 = Av1Tables.TxHeight[txSz] / 4;
        if (txfmSplit)
        {
            var subTxSz = Av1Tables.SplitTxSize[txSz];
            var stepW = Av1Tables.TxWidth[subTxSz] / 4;
            var stepH = Av1Tables.TxHeight[subTxSz] / 4;
            for (var i = 0; i < h4; i += stepH)
                for (var j = 0; j < w4; j += stepW)
                    ReadVarTxSize(row + i, col + j, subTxSz, depth + 1);
        }
        else
        {
            for (var i = 0; i < h4 && row + i < _f.MiRows; i++)
                for (var j = 0; j < w4 && col + j < _f.MiCols; j++)
                    _f.InterTxSizes[Mi(row + i, col + j)] = (byte)txSz;
            _txSize = txSz;
        }
    }

    private int GetPlaneResidualSize(int subsize, int plane)
    {
        var subx = plane > 0 ? _f.SubX : 0;
        var suby = plane > 0 ? _f.SubY : 0;
        return Av1Tables.SubsampledSize[(subsize * 2 + subx) * 2 + suby];
    }

    // ------------------------------------------------------------------ intra block copy motion vectors

    private readonly int[] _stackRow = new int[8], _stackCol = new int[8], _weightStack = new int[8];
    private int _numMvFound;

    /// <summary>
    /// find_mv_stack( 0 ) and assign_mv( 0 ) as they apply to intra block copy: the only candidates in an intra frame
    /// are other intra block copy vectors, there are no temporal or global ones, and vectors are whole samples.
    /// </summary>
    private void FindMvStackAndAssignMv()
    {
        _numMvFound = 0;
        var bw4 = _bw4;
        var bh4 = _bh4;
        ScanRow(-1);
        ScanCol(-1);
        if (Math.Max(bw4, bh4) <= 16) ScanPoint(-1, bw4);
        var numNearest = _numMvFound;
        for (var i = 0; i < numNearest; i++) _weightStack[i] += 640;
        ScanPoint(-1, -1);
        ScanRow(-3);
        ScanCol(-3);
        if (bh4 > 1) ScanRow(-5);
        if (bw4 > 1) ScanCol(-5);
        SortStack(0, numNearest);
        SortStack(numNearest, _numMvFound);
        // Extra search: neighbours are all intra (INTRA_FRAME is not > INTRA_FRAME), so only the global (zero)
        // candidates are added, without counting them.
        for (var idx = _numMvFound; idx < 2; idx++)
        {
            _stackRow[idx] = 0;
            _stackCol[idx] = 0;
        }
        // Context and clamping.
        var bw = BlockWidth(_miSize);
        var bh = BlockHeight(_miSize);
        for (var idx = 0; idx < _numMvFound; idx++)
        {
            _stackRow[idx] = ClampMvRow(_stackRow[idx], 128 + bh * 8);
            _stackCol[idx] = ClampMvCol(_stackCol[idx], 128 + bw * 8);
        }

        int predRow = _stackRow[0], predCol = _stackCol[0];
        if (predRow == 0 && predCol == 0)
        {
            predRow = _stackRow[1];
            predCol = _stackCol[1];
        }
        if (predRow == 0 && predCol == 0)
        {
            var sbSize4 = _sbSize4;
            if (_miRow - sbSize4 < _miRowStart)
            {
                predRow = 0;
                predCol = -(sbSize4 * 4 + 256) * 8;
            }
            else
            {
                predRow = -(sbSize4 * 4 * 8);
                predCol = 0;
            }
        }
        ReadMv(predRow, predCol);
    }

    private int ClampMvRow(int mvec, int border)
    {
        var mbToTopEdge = -((_miRow * 4) * 8);
        var mbToBottomEdge = ((_f.MiRows - _bh4 - _miRow) * 4) * 8;
        return Clip3(mbToTopEdge - border, mbToBottomEdge + border, mvec);
    }

    private int ClampMvCol(int mvec, int border)
    {
        var mbToLeftEdge = -((_miCol * 4) * 8);
        var mbToRightEdge = ((_f.MiCols - _bw4 - _miCol) * 4) * 8;
        return Clip3(mbToLeftEdge - border, mbToRightEdge + border, mvec);
    }

    private void ScanRow(int deltaRow)
    {
        var bw4 = _bw4;
        var end4 = Math.Min(Math.Min(bw4, _f.MiCols - _miCol), 16);
        var deltaCol = 0;
        var useStep16 = bw4 >= 16;
        if (Math.Abs(deltaRow) > 1)
        {
            deltaRow += _miRow & 1;
            deltaCol = 1 - (_miCol & 1);
        }
        var i = 0;
        while (i < end4)
        {
            var mvRow = _miRow + deltaRow;
            var mvCol = _miCol + deltaCol + i;
            if (!IsInside(mvRow, mvCol)) break;
            var len = Math.Min(bw4, Av1Tables.Num4x4BlocksWide[_f.MiSizes[Mi(mvRow, mvCol)]]);
            if (Math.Abs(deltaRow) > 1) len = Math.Max(2, len);
            if (useStep16) len = Math.Max(4, len);
            AddRefMvCandidate(mvRow, mvCol, len * 2);
            i += len;
        }
    }

    private void ScanCol(int deltaCol)
    {
        var bh4 = _bh4;
        var end4 = Math.Min(Math.Min(bh4, _f.MiRows - _miRow), 16);
        var deltaRow = 0;
        var useStep16 = bh4 >= 16;
        if (Math.Abs(deltaCol) > 1)
        {
            deltaRow = 1 - (_miRow & 1);
            deltaCol += _miCol & 1;
        }
        var i = 0;
        while (i < end4)
        {
            var mvRow = _miRow + deltaRow + i;
            var mvCol = _miCol + deltaCol;
            if (!IsInside(mvRow, mvCol)) break;
            var len = Math.Min(bh4, Av1Tables.Num4x4BlocksHigh[_f.MiSizes[Mi(mvRow, mvCol)]]);
            if (Math.Abs(deltaCol) > 1) len = Math.Max(2, len);
            if (useStep16) len = Math.Max(4, len);
            AddRefMvCandidate(mvRow, mvCol, len * 2);
            i += len;
        }
    }

    private void ScanPoint(int deltaRow, int deltaCol)
    {
        var mvRow = _miRow + deltaRow;
        var mvCol = _miCol + deltaCol;
        if (IsInside(mvRow, mvCol) && _f.RefFrames0[Mi(mvRow, mvCol)] != -1) AddRefMvCandidate(mvRow, mvCol, 4);
    }

    private void AddRefMvCandidate(int mvRow, int mvCol, int weight)
    {
        var mi = Mi(mvRow, mvCol);
        if (_f.IsInters[mi] == 0) return;
        // RefFrames[ 0 ] is INTRA_FRAME, matching RefFrame[ 0 ]; RefFrames[ 1 ] is NONE and never matches.
        var candRow = LowerMvPrecision(_f.MvRows![mi]);
        var candCol = LowerMvPrecision(_f.MvCols![mi]);
        for (var idx = 0; idx < _numMvFound; idx++)
        {
            if (_stackRow[idx] == candRow && _stackCol[idx] == candCol)
            {
                _weightStack[idx] += weight;
                return;
            }
        }
        if (_numMvFound < 8)
        {
            _stackRow[_numMvFound] = candRow;
            _stackCol[_numMvFound] = candCol;
            _weightStack[_numMvFound] = weight;
            _numMvFound++;
        }
    }

    private static int LowerMvPrecision(int v)
    {
        // force_integer_mv is always 1 in intra frames.
        var aInt = (Math.Abs(v) + 3) >> 3;
        return v > 0 ? aInt << 3 : -(aInt << 3);
    }

    private void SortStack(int start, int end)
    {
        while (end > start)
        {
            var newEnd = start;
            for (var idx = start + 1; idx < end; idx++)
            {
                if (_weightStack[idx - 1] < _weightStack[idx])
                {
                    (_weightStack[idx - 1], _weightStack[idx]) = (_weightStack[idx], _weightStack[idx - 1]);
                    (_stackRow[idx - 1], _stackRow[idx]) = (_stackRow[idx], _stackRow[idx - 1]);
                    (_stackCol[idx - 1], _stackCol[idx]) = (_stackCol[idx], _stackCol[idx - 1]);
                    newEnd = idx;
                }
            }
            end = newEnd;
        }
    }

    private void ReadMv(int predRow, int predCol)
    {
        const int mvCtx = 1; // MV_INTRABC_CONTEXT
        var diffRow = 0;
        var diffCol = 0;
        var mvJoint = _sr.Read(_cdf, Av1Cdf.MvJoint + mvCtx * 5, 4);
        if (mvJoint is 2 or 3) diffRow = ReadMvComponent(mvCtx, 0);
        if (mvJoint is 1 or 3) diffCol = ReadMvComponent(mvCtx, 1);
        _mvRow = predRow + diffRow;
        _mvCol = predCol + diffCol;
        if (Math.Abs(_mvRow) >= 1 << 14 || Math.Abs(_mvCol) >= 1 << 14)
            throw new ImageDecodeException("Invalid AV1 intra block copy vector.");
    }

    private int ReadMvComponent(int ctx, int comp)
    {
        var cc = ctx * 2 + comp;
        var sign = _sr.Read(_cdf, Av1Cdf.MvSign + cc * 3, 2);
        var mvClass = _sr.Read(_cdf, Av1Cdf.MvClass + cc * 12, 11);
        int mag;
        if (mvClass == 0)
        {
            var class0Bit = _sr.Read(_cdf, Av1Cdf.MvClass0Bit + cc * 3, 2);
            // force_integer_mv: the fraction is 3 and the high precision bit 1, neither coded.
            mag = ((class0Bit << 3) | (3 << 1) | 1) + 1;
        }
        else
        {
            var d = 0;
            for (var i = 0; i < mvClass; i++)
                d |= _sr.Read(_cdf, Av1Cdf.MvBit + (cc * 10 + i) * 3, 2) << i;
            mag = 2 << (mvClass + 2);
            mag += ((d << 3) | (3 << 1) | 1) + 1;
        }
        return sign == 1 ? -mag : mag;
    }

    // ------------------------------------------------------------------ loop restoration coefficients

    private void ReadLr(int r, int c, int bSize)
    {
        if (_hdr.AllowIntrabc || !_hdr.UsesLr) return;
        var w = Av1Tables.Num4x4BlocksWide[bSize];
        var h = Av1Tables.Num4x4BlocksHigh[bSize];
        for (var plane = 0; plane < _f.NumPlanes; plane++)
        {
            if (_hdr.FrameRestorationType[plane] == Av1FrameHeader.RestoreNone) continue;
            var subX = plane == 0 ? 0 : _f.SubX;
            var subY = plane == 0 ? 0 : _f.SubY;
            var unitSize = _hdr.LoopRestorationSize[plane];
            var unitRows = _f.LrUnitRows[plane];
            var unitCols = _f.LrUnitCols[plane];
            var unitRowStart = (r * (4 >> subY) + unitSize - 1) / unitSize;
            var unitRowEnd = Math.Min(unitRows, ((r + h) * (4 >> subY) + unitSize - 1) / unitSize);
            int numerator, denominator;
            if (_hdr.UseSuperres)
            {
                numerator = (4 >> subX) * _hdr.SuperresDenom;
                denominator = unitSize * 8;
            }
            else
            {
                numerator = 4 >> subX;
                denominator = unitSize;
            }
            var unitColStart = (c * numerator + denominator - 1) / denominator;
            var unitColEnd = Math.Min(unitCols, ((c + w) * numerator + denominator - 1) / denominator);
            for (var unitRow = unitRowStart; unitRow < unitRowEnd; unitRow++)
                for (var unitCol = unitColStart; unitCol < unitColEnd; unitCol++)
                    ReadLrUnit(plane, unitRow * unitCols + unitCol);
        }
    }

    private void ReadLrUnit(int plane, int unit)
    {
        int restorationType;
        var frameType = _hdr.FrameRestorationType[plane];
        if (frameType == Av1FrameHeader.RestoreWiener)
            restorationType = _sr.Read(_cdf, Av1Cdf.UseWiener, 2) == 1 ? Av1FrameHeader.RestoreWiener : Av1FrameHeader.RestoreNone;
        else if (frameType == Av1FrameHeader.RestoreSgrproj)
            restorationType = _sr.Read(_cdf, Av1Cdf.UseSgrproj, 2) == 1 ? Av1FrameHeader.RestoreSgrproj : Av1FrameHeader.RestoreNone;
        else
            restorationType = _sr.Read(_cdf, Av1Cdf.RestorationType, 3);
        _f.LrType[plane][unit] = (byte)restorationType;
        if (restorationType == Av1FrameHeader.RestoreWiener)
        {
            for (var pass = 0; pass < 2; pass++)
            {
                int firstCoeff;
                if (plane > 0)
                {
                    firstCoeff = 1;
                    _f.LrWiener[plane][unit * 6 + pass * 3] = 0;
                }
                else
                {
                    firstCoeff = 0;
                }
                for (var j = firstCoeff; j < 3; j++)
                {
                    int min = Av1Tables.WienerTapsMin[j];
                    int max = Av1Tables.WienerTapsMax[j];
                    int k = Av1Tables.WienerTapsK[j];
                    var v = DecodeSignedSubexpWithRefBool(min, max + 1, k, _refLrWiener[plane, pass, j]);
                    _f.LrWiener[plane][unit * 6 + pass * 3 + j] = (sbyte)v;
                    _refLrWiener[plane, pass, j] = v;
                }
            }
        }
        else if (restorationType == Av1FrameHeader.RestoreSgrproj)
        {
            var set = _sr.ReadLiteral(4);
            _f.LrSgrSet[plane][unit] = (byte)set;
            for (var i = 0; i < 2; i++)
            {
                var radius = Av1Tables.SgrParams[set * 4 + i * 2];
                int min = Av1Tables.SgrprojXqdMin[i];
                int max = Av1Tables.SgrprojXqdMax[i];
                int v;
                if (radius != 0)
                {
                    v = DecodeSignedSubexpWithRefBool(min, max + 1, 4, _refSgrXqd[plane, i]);
                }
                else
                {
                    v = 0;
                    if (i == 1) v = Clip3(min, max, (1 << 7) - _refSgrXqd[plane, 0]);
                }
                _f.LrSgrXqd[plane][unit * 2 + i] = (short)v;
                _refSgrXqd[plane, i] = v;
            }
        }
    }

    private int DecodeSignedSubexpWithRefBool(int low, int high, int k, int r)
    {
        var x = DecodeUnsignedSubexpWithRefBool(high - low, k, r - low);
        return x + low;
    }

    private int DecodeUnsignedSubexpWithRefBool(int mx, int k, int r)
    {
        var v = DecodeSubexpBool(mx, k);
        return (r << 1) <= mx ? InverseRecenter(r, v) : mx - 1 - InverseRecenter(mx - 1 - r, v);
    }

    private int DecodeSubexpBool(int numSyms, int k)
    {
        var i = 0;
        var mk = 0;
        while (true)
        {
            var b2 = i != 0 ? k + i - 1 : k;
            var a = 1 << b2;
            if (numSyms <= mk + 3 * a) return _sr.ReadNs(numSyms - mk) + mk;
            if (_sr.ReadBool() == 0) return _sr.ReadLiteral(b2) + mk;
            i++;
            mk += a;
            if (i > 24) throw new ImageDecodeException("Invalid AV1 loop restoration coefficient.");
        }
    }

    private static int InverseRecenter(int r, int v)
    {
        if (v > 2 * r) return v;
        if ((v & 1) != 0) return r - ((v + 1) >> 1);
        return r + (v >> 1);
    }
}
