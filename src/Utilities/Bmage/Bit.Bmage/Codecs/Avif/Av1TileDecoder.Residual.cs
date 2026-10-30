namespace Bit.Bmage.Codecs.Avif;

// Residual syntax (spec 5.11.34-5.11.39) and reconstruction (7.12).
internal sealed partial class Av1TileDecoder
{
    private const int DctDct = 0, Idtx = 9, VDct = 10, HDct = 11, VAdst = 12, HAdst = 13, VFlipadst = 14, HFlipadst = 15;
    private const int TxClass2D = 0, TxClassHoriz = 1, TxClassVert = 2;

    private readonly int[] _quant = new int[1024];
    private int _planeTxType;
    private int _maxQuantRow, _maxQuantCol;

    private void Residual()
    {
        var widthChunks = Math.Max(1, BlockWidth(_miSize) >> 6);
        var heightChunks = Math.Max(1, BlockHeight(_miSize) >> 6);
        var miSizeChunk = widthChunks > 1 || heightChunks > 1 ? Block64x64 : _miSize;
        for (var chunkY = 0; chunkY < heightChunks; chunkY++)
        {
            for (var chunkX = 0; chunkX < widthChunks; chunkX++)
            {
                var miRowChunk = _miRow + (chunkY << 4);
                var miColChunk = _miCol + (chunkX << 4);
                for (var plane = 0; plane < 1 + (_hasChroma ? 2 : 0); plane++)
                {
                    var txSz = _lossless ? Tx4x4 : GetTxSize(plane, _txSize);
                    var stepX = Av1Tables.TxWidth[txSz] >> 2;
                    var stepY = Av1Tables.TxHeight[txSz] >> 2;
                    var planeSz = GetPlaneResidualSize(miSizeChunk, plane);
                    var num4x4W = Av1Tables.Num4x4BlocksWide[planeSz];
                    var num4x4H = Av1Tables.Num4x4BlocksHigh[planeSz];
                    var subX = plane > 0 ? _f.SubX : 0;
                    var subY = plane > 0 ? _f.SubY : 0;
                    if (_isInter && !_lossless && plane == 0)
                    {
                        var baseX = (miColChunk >> subX) * 4;
                        var baseY = (miRowChunk >> subY) * 4;
                        TransformTree(baseX, baseY, num4x4W * 4, num4x4H * 4);
                    }
                    else
                    {
                        var baseXBlock = (_miCol >> subX) * 4;
                        var baseYBlock = (_miRow >> subY) * 4;
                        for (var y = 0; y < num4x4H; y += stepY)
                            for (var x = 0; x < num4x4W; x += stepX)
                                TransformBlock(plane, baseXBlock, baseYBlock, txSz, x + ((chunkX << 4) >> subX), y + ((chunkY << 4) >> subY));
                    }
                }
            }
        }
    }

    private void TransformTree(int startX, int startY, int w, int h)
    {
        var maxX = _f.MiCols * 4;
        var maxY = _f.MiRows * 4;
        if (startX >= maxX || startY >= maxY) return;
        var row = startY >> 2;
        var col = startX >> 2;
        var lumaTxSz = _f.InterTxSizes[Mi(row, col)];
        var lumaW = Av1Tables.TxWidth[lumaTxSz];
        var lumaH = Av1Tables.TxHeight[lumaTxSz];
        if (w <= lumaW && h <= lumaH)
        {
            var txSz = FindTxSize(w, h);
            TransformBlock(0, startX, startY, txSz, 0, 0);
        }
        else if (w > h)
        {
            TransformTree(startX, startY, w / 2, h);
            TransformTree(startX + w / 2, startY, w / 2, h);
        }
        else if (w < h)
        {
            TransformTree(startX, startY, w, h / 2);
            TransformTree(startX, startY + h / 2, w, h / 2);
        }
        else
        {
            TransformTree(startX, startY, w / 2, h / 2);
            TransformTree(startX + w / 2, startY, w / 2, h / 2);
            TransformTree(startX, startY + h / 2, w / 2, h / 2);
            TransformTree(startX + w / 2, startY + h / 2, w / 2, h / 2);
        }
    }

    private int GetTxSize(int plane, int txSz)
    {
        if (plane == 0) return txSz;
        var uvTx = Av1Tables.MaxTxSizeRect[GetPlaneResidualSize(_miSize, plane)];
        if (Av1Tables.TxWidth[uvTx] == 64 || Av1Tables.TxHeight[uvTx] == 64)
        {
            if (Av1Tables.TxWidth[uvTx] == 16) return 9; // TX_16X32
            if (Av1Tables.TxHeight[uvTx] == 16) return 10; // TX_32X16
            return Tx32x32;
        }
        return uvTx;
    }

    private void TransformBlock(int plane, int baseX, int baseY, int txSz, int x, int y)
    {
        var startX = baseX + 4 * x;
        var startY = baseY + 4 * y;
        var subX = plane > 0 ? _f.SubX : 0;
        var subY = plane > 0 ? _f.SubY : 0;
        var row = (startY << subY) >> 2;
        var col = (startX << subX) >> 2;
        var subBlockMiRow = row & _sbMask;
        var subBlockMiCol = col & _sbMask;
        var stepX = Av1Tables.TxWidth[txSz] >> 2;
        var stepY = Av1Tables.TxHeight[txSz] >> 2;
        var maxX = (_f.MiCols * 4) >> subX;
        var maxY = (_f.MiRows * 4) >> subY;
        if (startX >= maxX || startY >= maxY) return;
        if (!_isInter)
        {
            if ((plane == 0 && _paletteSizeY > 0) || (plane != 0 && _paletteSizeUV > 0))
            {
                PredictPalette(plane, startX, startY, x, y, txSz);
            }
            else
            {
                var isCfl = plane > 0 && _uvMode == UvCflPred;
                var mode = plane == 0 ? _yMode : isCfl ? DcPred : _uvMode;
                PredictIntra(plane, startX, startY,
                    (plane == 0 ? _availL : _availLChroma) || x > 0,
                    (plane == 0 ? _availU : _availUChroma) || y > 0,
                    BlockDecoded(plane, (subBlockMiRow >> subY) - 1, (subBlockMiCol >> subX) + stepX),
                    BlockDecoded(plane, (subBlockMiRow >> subY) + stepY, (subBlockMiCol >> subX) - 1),
                    mode, Av1Tables.TxWidthLog2[txSz], Av1Tables.TxHeightLog2[txSz]);
                if (isCfl) PredictChromaFromLuma(plane, startX, startY, txSz);
            }
            if (plane == 0)
            {
                _maxLumaW = startX + stepX * 4;
                _maxLumaH = startY + stepY * 4;
            }
        }
        if (!_skip)
        {
            var eob = Coeffs(plane, startX, startY, txSz);
            if (eob > 0) Reconstruct(plane, startX, startY, txSz);
        }
        var lf = _f.LoopfilterTxSizes[plane];
        var lfStride = _f.LfStride[plane];
        var lfRows = lf.Length / lfStride;
        var bd = _blockDecoded[plane];
        for (var i = 0; i < stepY; i++)
        {
            var lr = (row >> subY) + i;
            for (var j = 0; j < stepX; j++)
            {
                var lc = (col >> subX) + j;
                if (lr < lfRows && lc < lfStride) lf[lr * lfStride + lc] = (byte)txSz;
                bd[((subBlockMiRow >> subY) + i + 1) * _bdStride + (subBlockMiCol >> subX) + j + 1] = 1;
            }
        }
    }

    private int GetTxSet(int txSz)
    {
        var txSzSqr = Av1Tables.TxSizeSqr[txSz];
        var txSzSqrUp = Av1Tables.TxSizeSqrUp[txSz];
        if (txSzSqrUp > Tx32x32) return 0;
        if (_isInter)
        {
            if (_hdr.ReducedTxSet || txSzSqrUp == Tx32x32) return 3;
            if (txSzSqr == Tx16x16) return 2;
            return 1;
        }
        if (txSzSqrUp == Tx32x32) return 0;
        if (_hdr.ReducedTxSet) return 2;
        if (txSzSqr == Tx16x16) return 2;
        return 1;
    }

    private bool IsTxTypeInSet(int txSet, int txType)
        => (_isInter ? Av1Tables.TxTypeInSetInter[txSet * 16 + txType] : Av1Tables.TxTypeInSetIntra[txSet * 16 + txType]) != 0;

    private int ComputeTxType(int plane, int txSz, int blockX, int blockY)
    {
        var txSzSqrUp = Av1Tables.TxSizeSqrUp[txSz];
        if (_lossless || txSzSqrUp > Tx32x32) return DctDct;
        var txSet = GetTxSet(txSz);
        if (plane == 0) return _f.TxTypes[Mi(blockY, blockX)];
        int txType;
        if (_isInter)
        {
            var x4 = Math.Max(_miCol, blockX << _f.SubX);
            var y4 = Math.Max(_miRow, blockY << _f.SubY);
            txType = _f.TxTypes[Mi(y4, x4)];
        }
        else
        {
            txType = Av1Tables.ModeToTxfm[_uvMode];
        }
        return IsTxTypeInSet(txSet, txType) ? txType : DctDct;
    }

    private void SetTxTypes(int x4, int y4, int txSz, int txType)
    {
        var rows = Math.Min(Av1Tables.TxHeight[txSz] >> 2, _f.MiRows - y4);
        var cols = Math.Min(Av1Tables.TxWidth[txSz] >> 2, _f.MiCols - x4);
        for (var j = 0; j < rows; j++) _f.TxTypes.AsSpan(Mi(y4 + j, x4), cols).Fill((byte)txType);
    }

    private void TransformType(int x4, int y4, int txSz)
    {
        var set = GetTxSet(txSz);
        var txType = DctDct;
        var qindex = _hdr.SegmentationEnabled ? _hdr.GetQIndex(true, _segmentId, _currentQIndex) : _hdr.BaseQIdx;
        if (set > 0 && qindex > 0)
        {
            var txSzSqr = Av1Tables.TxSizeSqr[txSz];
            if (_isInter)
            {
                if (set == 1) txType = Av1Tables.TxTypeInterInvSet1[_sr.Read(_cdf, Av1Cdf.InterTxTypeSet1 + txSzSqr * 17, 16)];
                else if (set == 2) txType = Av1Tables.TxTypeInterInvSet2[_sr.Read(_cdf, Av1Cdf.InterTxTypeSet2, 12)];
                else txType = Av1Tables.TxTypeInterInvSet3[_sr.Read(_cdf, Av1Cdf.InterTxTypeSet3 + txSzSqr * 3, 2)];
            }
            else
            {
                var intraDir = _useFilterIntra ? Av1Tables.FilterIntraModeToIntraDir[_filterIntraMode] : _yMode;
                if (set == 1) txType = Av1Tables.TxTypeIntraInvSet1[_sr.Read(_cdf, Av1Cdf.IntraTxTypeSet1 + (txSzSqr * 13 + intraDir) * 8, 7)];
                else txType = Av1Tables.TxTypeIntraInvSet2[_sr.Read(_cdf, Av1Cdf.IntraTxTypeSet2 + (txSzSqr * 13 + intraDir) * 6, 5)];
            }
        }
        SetTxTypes(x4, y4, txSz, txType);
    }

    private static int GetTxClass(int txType) => txType switch
    {
        VDct or VAdst or VFlipadst => TxClassVert,
        HDct or HAdst or HFlipadst => TxClassHoriz,
        _ => TxClass2D,
    };

    private ReadOnlySpan<short> GetScan(int txSz)
    {
        if (txSz == 17) return Av1Tables.DefaultScan16x32;
        if (txSz == 18) return Av1Tables.DefaultScan32x16;
        if (Av1Tables.TxSizeSqrUp[txSz] == Tx64x64) return Av1Tables.DefaultScan32x32;
        if (_planeTxType == Idtx) return DefaultScan(txSz);
        var preferRow = _planeTxType is VDct or VAdst or VFlipadst;
        var preferCol = _planeTxType is HDct or HAdst or HFlipadst;
        if (preferRow) return MrowScan(txSz);
        if (preferCol) return McolScan(txSz);
        return DefaultScan(txSz);
    }

    // The byte scan tables are widened once so every scan reads as shorts.
    private static readonly short[][] ScanCache = BuildScanCache();

    private static short[][] BuildScanCache()
    {
        static short[] W(ReadOnlySpan<byte> s)
        {
            var r = new short[s.Length];
            for (var i = 0; i < s.Length; i++) r[i] = s[i];
            return r;
        }
        return
        [
            W(Av1Tables.DefaultScan4x4), W(Av1Tables.DefaultScan8x8), W(Av1Tables.DefaultScan16x16), W(Av1Tables.DefaultScan4x8), W(Av1Tables.DefaultScan8x4),
            W(Av1Tables.DefaultScan8x16), W(Av1Tables.DefaultScan16x8), W(Av1Tables.DefaultScan4x16), W(Av1Tables.DefaultScan16x4), W(Av1Tables.DefaultScan8x32),
            W(Av1Tables.DefaultScan32x8),
            W(Av1Tables.MrowScan4x4), W(Av1Tables.MrowScan4x8), W(Av1Tables.MrowScan8x4), W(Av1Tables.MrowScan8x8), W(Av1Tables.MrowScan8x16),
            W(Av1Tables.MrowScan16x8), W(Av1Tables.MrowScan16x16), W(Av1Tables.MrowScan4x16), W(Av1Tables.MrowScan16x4),
            W(Av1Tables.McolScan4x4), W(Av1Tables.McolScan4x8), W(Av1Tables.McolScan8x4), W(Av1Tables.McolScan8x8), W(Av1Tables.McolScan8x16),
            W(Av1Tables.McolScan16x8), W(Av1Tables.McolScan16x16), W(Av1Tables.McolScan4x16), W(Av1Tables.McolScan16x4),
        ];
    }

    private static ReadOnlySpan<short> DefaultScan(int txSz) => txSz switch
    {
        0 => ScanCache[0],
        5 => ScanCache[3],
        6 => ScanCache[4],
        1 => ScanCache[1],
        7 => ScanCache[5],
        8 => ScanCache[6],
        2 => ScanCache[2],
        9 => Av1Tables.DefaultScan16x32,
        10 => Av1Tables.DefaultScan32x16,
        13 => ScanCache[7],
        14 => ScanCache[8],
        15 => ScanCache[9],
        16 => ScanCache[10],
        _ => Av1Tables.DefaultScan32x32,
    };

    private static ReadOnlySpan<short> MrowScan(int txSz) => txSz switch
    {
        0 => ScanCache[11],
        5 => ScanCache[12],
        6 => ScanCache[13],
        1 => ScanCache[14],
        7 => ScanCache[15],
        8 => ScanCache[16],
        2 => ScanCache[17],
        13 => ScanCache[18],
        _ => ScanCache[19],
    };

    private static ReadOnlySpan<short> McolScan(int txSz) => txSz switch
    {
        0 => ScanCache[20],
        5 => ScanCache[21],
        6 => ScanCache[22],
        1 => ScanCache[23],
        7 => ScanCache[24],
        8 => ScanCache[25],
        2 => ScanCache[26],
        13 => ScanCache[27],
        _ => ScanCache[28],
    };

    private int Coeffs(int plane, int startX, int startY, int txSz)
    {
        var x4 = startX >> 2;
        var y4 = startY >> 2;
        var w4 = Av1Tables.TxWidth[txSz] >> 2;
        var h4 = Av1Tables.TxHeight[txSz] >> 2;
        var txSzCtx = (Av1Tables.TxSizeSqr[txSz] + Av1Tables.TxSizeSqrUp[txSz] + 1) >> 1;
        var ptype = plane > 0 ? 1 : 0;
        var segEob = txSz is 17 or 18 ? 512 : Math.Min(1024, Av1Tables.TxWidth[txSz] * Av1Tables.TxHeight[txSz]);
        var quant = _quant;
        Array.Clear(quant, 0, segEob);
        var eob = 0;
        var culLevel = 0;
        var dcCategory = 0;
        _maxQuantRow = 0;
        _maxQuantCol = 0;

        var allZero = _sr.Read(_cdf, Av1Cdf.TxbSkip + (txSzCtx * 13 + AllZeroContext(plane, txSz, x4, y4, w4, h4)) * 3, 2);
        if (allZero == 1)
        {
            if (plane == 0) SetTxTypes(x4, y4, txSz, DctDct);
        }
        else
        {
            if (plane == 0) TransformType(x4, y4, txSz);
            _planeTxType = ComputeTxType(plane, txSz, x4, y4);
            var scan = GetScan(txSz);
            var txClass = GetTxClass(_planeTxType);
            var eobMultisize = Math.Min((int)Av1Tables.TxWidthLog2[txSz], 5) + Math.Min((int)Av1Tables.TxHeightLog2[txSz], 5) - 4;
            var eobCtx = txClass == TxClass2D ? 0 : 1;
            var eobPt = eobMultisize switch
            {
                0 => _sr.Read(_cdf, Av1Cdf.EobPt16 + (ptype * 2 + eobCtx) * 6, 5),
                1 => _sr.Read(_cdf, Av1Cdf.EobPt32 + (ptype * 2 + eobCtx) * 7, 6),
                2 => _sr.Read(_cdf, Av1Cdf.EobPt64 + (ptype * 2 + eobCtx) * 8, 7),
                3 => _sr.Read(_cdf, Av1Cdf.EobPt128 + (ptype * 2 + eobCtx) * 9, 8),
                4 => _sr.Read(_cdf, Av1Cdf.EobPt256 + (ptype * 2 + eobCtx) * 10, 9),
                5 => _sr.Read(_cdf, Av1Cdf.EobPt512 + ptype * 11, 10),
                _ => _sr.Read(_cdf, Av1Cdf.EobPt1024 + ptype * 12, 11),
            } + 1;
            eob = eobPt < 2 ? eobPt : (1 << (eobPt - 2)) + 1;
            var eobShift = Math.Max(-1, eobPt - 3);
            if (eobShift >= 0)
            {
                if (_sr.Read(_cdf, Av1Cdf.EobExtra + ((txSzCtx * 2 + ptype) * 9 + eobPt - 3) * 3, 2) == 1) eob += 1 << eobShift;
                for (var i = 1; i < Math.Max(0, eobPt - 2); i++)
                {
                    eobShift = Math.Max(0, eobPt - 2) - 1 - i;
                    if (_sr.ReadBool() == 1) eob += 1 << eobShift;
                }
            }
            if (eob > segEob) throw new ImageDecodeException("Invalid AV1 end of block.");

            var adjTxSz = Av1Tables.AdjustedTxSize[txSz];
            var bwl = Av1Tables.TxWidthLog2[adjTxSz];
            var width = 1 << bwl;
            var height = Av1Tables.TxHeight[adjTxSz];
            var baseEobCdf = Av1Cdf.CoeffBaseEob + (txSzCtx * 2 + ptype) * 4 * 4;
            var baseCdf = Av1Cdf.CoeffBase + (txSzCtx * 2 + ptype) * 42 * 5;
            var brCdf = Av1Cdf.CoeffBr + (Math.Min(txSzCtx, Tx32x32) * 2 + ptype) * 21 * 5;
            for (var c = eob - 1; c >= 0; c--)
            {
                int pos = scan[c];
                int level;
                if (c == eob - 1)
                {
                    int ctx;
                    if (c == 0) ctx = 0;
                    else if (c <= (height << bwl) / 8) ctx = 1;
                    else if (c <= (height << bwl) / 4) ctx = 2;
                    else ctx = 3;
                    level = _sr.Read(_cdf, baseEobCdf + ctx * 4, 3) + 1;
                }
                else
                {
                    level = _sr.Read(_cdf, baseCdf + CoeffBaseContext(txSz, txClass, bwl, width, height, pos) * 5, 4);
                }
                if (level > 2)
                {
                    var ctx = CoeffBrContext(txClass, bwl, width, height, pos);
                    for (var idx = 0; idx < 4; idx++)
                    {
                        var coeffBr = _sr.Read(_cdf, brCdf + ctx * 5, 4);
                        level += coeffBr;
                        if (coeffBr < 3) break;
                    }
                }
                quant[pos] = level;
            }

            for (var c = 0; c < eob; c++)
            {
                int pos = scan[c];
                var sign = 0;
                if (quant[pos] != 0)
                {
                    if (c == 0) sign = _sr.Read(_cdf, Av1Cdf.DcSign + (ptype * 3 + DcSignContext(plane, x4, y4, w4, h4)) * 3, 2);
                    else sign = _sr.ReadBool();
                }
                if (quant[pos] > 14)
                {
                    var length = 0;
                    int bit;
                    do
                    {
                        length++;
                        bit = _sr.ReadBool();
                        if (length > 20) throw new ImageDecodeException("Invalid AV1 coefficient.");
                    }
                    while (bit == 0);
                    var x = 1;
                    for (var i = length - 2; i >= 0; i--) x = (x << 1) | _sr.ReadBool();
                    quant[pos] = x + 14;
                }
                if (pos == 0 && quant[pos] > 0) dcCategory = sign != 0 ? 1 : 2;
                quant[pos] &= 0xFFFFF;
                culLevel += quant[pos];
                if (quant[pos] != 0)
                {
                    var r = pos >> bwl;
                    var col = pos & (width - 1);
                    if (r > _maxQuantRow) _maxQuantRow = r;
                    if (col > _maxQuantCol) _maxQuantCol = col;
                }
                if (sign != 0) quant[pos] = -quant[pos];
            }
            culLevel = Math.Min(63, culLevel);
        }

        var aboveLevel = _aboveLevel[plane];
        var aboveDc = _aboveDc[plane];
        for (var i = 0; i < w4; i++)
        {
            aboveLevel[x4 + i] = (byte)culLevel;
            aboveDc[x4 + i] = (byte)dcCategory;
        }
        var leftLevel = _leftLevel[plane];
        var leftDc = _leftDc[plane];
        for (var i = 0; i < h4; i++)
        {
            leftLevel[y4 + i] = (byte)culLevel;
            leftDc[y4 + i] = (byte)dcCategory;
        }
        return eob;
    }

    private int AllZeroContext(int plane, int txSz, int x4, int y4, int w4, int h4)
    {
        var maxX4 = _f.MiCols;
        var maxY4 = _f.MiRows;
        if (plane > 0)
        {
            maxX4 >>= _f.SubX;
            maxY4 >>= _f.SubY;
        }
        var w = Av1Tables.TxWidth[txSz];
        var h = Av1Tables.TxHeight[txSz];
        var bsize = GetPlaneResidualSize(_miSize, plane);
        var bw = BlockWidth(bsize);
        var bh = BlockHeight(bsize);
        var aboveLevel = _aboveLevel[plane];
        var leftLevel = _leftLevel[plane];
        if (plane == 0)
        {
            var top = 0;
            var left = 0;
            for (var k = 0; k < w4; k++)
                if (x4 + k < maxX4) top = Math.Max(top, aboveLevel[x4 + k]);
            for (var k = 0; k < h4; k++)
                if (y4 + k < maxY4) left = Math.Max(left, leftLevel[y4 + k]);
            top = Math.Min(top, 255);
            left = Math.Min(left, 255);
            if (bw == w && bh == h) return 0;
            if (top == 0 && left == 0) return 1;
            if (top == 0 || left == 0) return 2 + (Math.Max(top, left) > 3 ? 1 : 0);
            if (Math.Max(top, left) <= 3) return 4;
            if (Math.Min(top, left) <= 3) return 5;
            return 6;
        }
        var aboveDc = _aboveDc[plane];
        var leftDc = _leftDc[plane];
        var above = 0;
        var l = 0;
        for (var i = 0; i < w4; i++)
            if (x4 + i < maxX4) above |= aboveLevel[x4 + i] | aboveDc[x4 + i];
        for (var i = 0; i < h4; i++)
            if (y4 + i < maxY4) l |= leftLevel[y4 + i] | leftDc[y4 + i];
        var ctx = (above != 0 ? 1 : 0) + (l != 0 ? 1 : 0) + 7;
        if (bw * bh > w * h) ctx += 3;
        return ctx;
    }

    private int DcSignContext(int plane, int x4, int y4, int w4, int h4)
    {
        var maxX4 = _f.MiCols;
        var maxY4 = _f.MiRows;
        if (plane > 0)
        {
            maxX4 >>= _f.SubX;
            maxY4 >>= _f.SubY;
        }
        var dcSign = 0;
        var aboveDc = _aboveDc[plane];
        var leftDc = _leftDc[plane];
        for (var k = 0; k < w4; k++)
        {
            if (x4 + k >= maxX4) continue;
            var sign = aboveDc[x4 + k];
            if (sign == 1) dcSign--;
            else if (sign == 2) dcSign++;
        }
        for (var k = 0; k < h4; k++)
        {
            if (y4 + k >= maxY4) continue;
            var sign = leftDc[y4 + k];
            if (sign == 1) dcSign--;
            else if (sign == 2) dcSign++;
        }
        return dcSign < 0 ? 1 : dcSign > 0 ? 2 : 0;
    }

    private int CoeffBaseContext(int txSz, int txClass, int bwl, int width, int height, int pos)
    {
        var quant = _quant;
        var row = pos >> bwl;
        var col = pos - (row << bwl);
        var mag = 0;
        var offsets = Av1Tables.SigRefDiffOffset;
        var ob = txClass * 10;
        for (var idx = 0; idx < 5; idx++)
        {
            var refRow = row + offsets[ob + idx * 2];
            var refCol = col + offsets[ob + idx * 2 + 1];
            if (refRow < height && refCol < width) mag += Math.Min(Math.Abs(quant[(refRow << bwl) + refCol]), 3);
        }
        var ctx = Math.Min((mag + 1) >> 1, 4);
        if (txClass == TxClass2D)
        {
            if (row == 0 && col == 0) return 0;
            return ctx + Av1Tables.CoeffBaseCtxOffset[(txSz * 5 + Math.Min(row, 4)) * 5 + Math.Min(col, 4)];
        }
        var i = txClass == TxClassVert ? row : col;
        return ctx + Av1Tables.CoeffBasePosCtxOffset[Math.Min(i, 2)];
    }

    private int CoeffBrContext(int txClass, int bwl, int txw, int txh, int pos)
    {
        var quant = _quant;
        var row = pos >> bwl;
        var col = pos - (row << bwl);
        var mag = 0;
        var offsets = Av1Tables.MagRefOffsetWithTxClass;
        var ob = txClass * 6;
        for (var idx = 0; idx < 3; idx++)
        {
            var refRow = row + offsets[ob + idx * 2];
            var refCol = col + offsets[ob + idx * 2 + 1];
            if (refRow < txh && refCol < (1 << bwl)) mag += Math.Min(quant[refRow * txw + refCol], 15);
        }
        mag = Math.Min((mag + 1) >> 1, 6);
        if (pos == 0) return mag;
        if (txClass == 0) return row < 2 && col < 2 ? mag + 7 : mag + 14;
        if (txClass == 1) return col == 0 ? mag + 7 : mag + 14;
        return row == 0 ? mag + 7 : mag + 14;
    }

    // ------------------------------------------------------------------ reconstruction

    private int DcQ(int b) => Av1Tables.DcQlookup[((_f.BitDepth - 8) >> 1) * 256 + Clip3(0, 255, b)];

    private int AcQ(int b) => Av1Tables.AcQlookup[((_f.BitDepth - 8) >> 1) * 256 + Clip3(0, 255, b)];

    private void Reconstruct(int plane, int x, int y, int txSz)
    {
        var dqDenom = txSz is 3 or 9 or 10 or 17 or 18 ? 2 : txSz is 4 or 11 or 12 ? 4 : 1;
        var log2W = Av1Tables.TxWidthLog2[txSz];
        var log2H = Av1Tables.TxHeightLog2[txSz];
        var w = 1 << log2W;
        var h = 1 << log2H;
        var tw = Math.Min(32, w);
        var th = Math.Min(32, h);
        var txType = _planeTxType;
        var flipUD = txType is 4 or 8 or 14 or 6;
        var flipLR = txType is 5 or 7 or 15 or 6;

        var qindex = _hdr.GetQIndex(false, _segmentId, _currentQIndex);
        int dcQ, acQ;
        if (plane == 0)
        {
            dcQ = DcQ(qindex + _hdr.DeltaQYDc);
            acQ = AcQ(qindex);
        }
        else if (plane == 1)
        {
            dcQ = DcQ(qindex + _hdr.DeltaQUDc);
            acQ = AcQ(qindex + _hdr.DeltaQUAc);
        }
        else
        {
            dcQ = DcQ(qindex + _hdr.DeltaQVDc);
            acQ = AcQ(qindex + _hdr.DeltaQVAc);
        }
        var qmLevel = _hdr.UsingQmatrix && txType < Idtx ? _hdr.SegQMLevel[plane, _segmentId] : 15;
        var qm = qmLevel < 15 ? Av1Tables.QuantizerMatrix.Slice((qmLevel * 2 + (plane > 0 ? 1 : 0)) * 3344 + Av1Tables.QmOffset[txSz]) : default;
        var maxCoeff = (1 << (7 + _f.BitDepth)) - 1;
        var minCoeff = -(1 << (7 + _f.BitDepth));

        var dequant = _dequant;
        var rows = Math.Min(th, _maxQuantRow + 1);
        var cols = Math.Min(tw, _maxQuantCol + 1);
        var quant = _quant;
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                var qv = quant[i * tw + j];
                if (qv == 0)
                {
                    dequant[i * tw + j] = 0;
                    continue;
                }
                long q = i == 0 && j == 0 ? dcQ : acQ;
                if (qmLevel < 15) q = (q * qm[i * tw + j] + 16) >> 5;
                var dq = qv * q;
                var mag = (Math.Abs(dq) & 0xFFFFFF) / dqDenom;
                var dq2 = dq < 0 ? -mag : mag;
                dequant[i * tw + j] = (int)Math.Clamp(dq2, minCoeff, maxCoeff);
            }
        }

        InverseTransform2D(txSz, txType, rows, cols, tw);

        var p = _f.Planes[plane];
        var data = p.Data;
        var stride = p.Stride;
        var residual = _residual;
        var maxValue = (1 << _f.BitDepth) - 1;
        for (var i = 0; i < h; i++)
        {
            var yy = flipUD ? h - i - 1 : i;
            var dst = (y + yy) * stride + x;
            var src = i * w;
            if (flipLR)
            {
                for (var j = 0; j < w; j++)
                {
                    var v = data[dst + w - j - 1] + residual[src + j];
                    data[dst + w - j - 1] = (ushort)(v < 0 ? 0 : v > maxValue ? maxValue : v);
                }
            }
            else
            {
                for (var j = 0; j < w; j++)
                {
                    var v = data[dst + j] + residual[src + j];
                    data[dst + j] = (ushort)(v < 0 ? 0 : v > maxValue ? maxValue : v);
                }
            }
        }
    }
}
