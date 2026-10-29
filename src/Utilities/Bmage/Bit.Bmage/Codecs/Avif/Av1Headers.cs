namespace Bit.Bmage.Codecs.Avif;

/// <summary>The fields of an AV1 sequence header OBU (spec 5.5) that decoding a still image needs.</summary>
internal sealed class Av1SequenceHeader
{
    public int Profile;
    public bool StillPicture;
    public bool ReducedStillPictureHeader;
    public int OperatingPointIdc;
    public bool DecoderModelInfoPresent;
    public bool EqualPictureInterval;
    public int BufferRemovalTimeLength;
    public int FramePresentationTimeLength;
    public int OperatingPointsCount;
    public int[] OperatingPointIdcs = [];
    public bool[] DecoderModelPresentForOp = [];
    public int FrameWidthBits;
    public int FrameHeightBits;
    public int MaxFrameWidth;
    public int MaxFrameHeight;
    public bool FrameIdNumbersPresent;
    public int DeltaFrameIdLength;
    public int AdditionalFrameIdLength;
    public bool Use128x128Superblock;
    public bool EnableFilterIntra;
    public bool EnableIntraEdgeFilter;
    public bool EnableOrderHint;
    public int OrderHintBits;
    public int ForceScreenContentTools;
    public int ForceIntegerMv;
    public bool EnableSuperres;
    public bool EnableCdef;
    public bool EnableRestoration;
    public int BitDepth;
    public bool MonoChrome;
    public int NumPlanes;
    public int ColorPrimaries = 2;
    public int TransferCharacteristics = 2;
    public int MatrixCoefficients = 2;
    public bool ColorDescriptionPresent;
    public bool FullRange;
    public int SubsamplingX;
    public int SubsamplingY;
    public int ChromaSamplePosition;
    public bool SeparateUvDeltaQ;
    public bool FilmGrainParamsPresent;

    public static Av1SequenceHeader Parse(ReadOnlySpan<byte> payload)
    {
        var r = new Av1BitReader(payload);
        var s = new Av1SequenceHeader();
        s.Profile = r.Read(3);
        if (s.Profile > 2) throw new ImageDecodeException($"Unsupported AV1 profile {s.Profile}.");
        s.StillPicture = r.ReadFlag();
        s.ReducedStillPictureHeader = r.ReadFlag();
        var bufferDelayLength = 0;
        if (s.ReducedStillPictureHeader)
        {
            s.OperatingPointsCount = 1;
            s.OperatingPointIdcs = [0];
            s.DecoderModelPresentForOp = [false];
            r.Read(5); // seq_level_idx[0]
        }
        else
        {
            var timingInfoPresent = r.ReadFlag();
            if (timingInfoPresent)
            {
                r.ReadBits(32); // num_units_in_display_tick
                r.ReadBits(32); // time_scale
                s.EqualPictureInterval = r.ReadFlag();
                if (s.EqualPictureInterval) r.ReadUvlc();
                s.DecoderModelInfoPresent = r.ReadFlag();
                if (s.DecoderModelInfoPresent)
                {
                    bufferDelayLength = r.Read(5) + 1;
                    r.ReadBits(32); // num_units_in_decoding_tick
                    s.BufferRemovalTimeLength = r.Read(5) + 1;
                    s.FramePresentationTimeLength = r.Read(5) + 1;
                }
            }
            var initialDisplayDelayPresent = r.ReadFlag();
            s.OperatingPointsCount = r.Read(5) + 1;
            s.OperatingPointIdcs = new int[s.OperatingPointsCount];
            s.DecoderModelPresentForOp = new bool[s.OperatingPointsCount];
            for (var i = 0; i < s.OperatingPointsCount; i++)
            {
                s.OperatingPointIdcs[i] = r.Read(12);
                var levelIdx = r.Read(5);
                if (levelIdx > 7) r.ReadBit(); // seq_tier
                if (s.DecoderModelInfoPresent)
                {
                    s.DecoderModelPresentForOp[i] = r.ReadFlag();
                    if (s.DecoderModelPresentForOp[i])
                    {
                        r.ReadBits(bufferDelayLength); // decoder_buffer_delay
                        r.ReadBits(bufferDelayLength); // encoder_buffer_delay
                        r.ReadBit(); // low_delay_mode_flag
                    }
                }
                if (initialDisplayDelayPresent && r.ReadFlag()) r.Read(4);
            }
        }
        // Operating point 0, the one every decoder is expected to choose without other instructions.
        s.OperatingPointIdc = s.OperatingPointIdcs[0];
        s.FrameWidthBits = r.Read(4) + 1;
        s.FrameHeightBits = r.Read(4) + 1;
        s.MaxFrameWidth = (int)r.ReadBits(s.FrameWidthBits) + 1;
        s.MaxFrameHeight = (int)r.ReadBits(s.FrameHeightBits) + 1;
        s.FrameIdNumbersPresent = !s.ReducedStillPictureHeader && r.ReadFlag();
        if (s.FrameIdNumbersPresent)
        {
            s.DeltaFrameIdLength = r.Read(4) + 2;
            s.AdditionalFrameIdLength = r.Read(3) + 1;
        }
        s.Use128x128Superblock = r.ReadFlag();
        s.EnableFilterIntra = r.ReadFlag();
        s.EnableIntraEdgeFilter = r.ReadFlag();
        if (s.ReducedStillPictureHeader)
        {
            s.ForceScreenContentTools = 2;
            s.ForceIntegerMv = 2;
        }
        else
        {
            r.ReadBit(); // enable_interintra_compound
            r.ReadBit(); // enable_masked_compound
            r.ReadBit(); // enable_warped_motion
            r.ReadBit(); // enable_dual_filter
            s.EnableOrderHint = r.ReadFlag();
            if (s.EnableOrderHint)
            {
                r.ReadBit(); // enable_jnt_comp
                r.ReadBit(); // enable_ref_frame_mvs
            }
            s.ForceScreenContentTools = r.ReadFlag() ? 2 : r.Read(1);
            if (s.ForceScreenContentTools > 0) s.ForceIntegerMv = r.ReadFlag() ? 2 : r.Read(1);
            else s.ForceIntegerMv = 2;
            if (s.EnableOrderHint) s.OrderHintBits = r.Read(3) + 1;
        }
        s.EnableSuperres = r.ReadFlag();
        s.EnableCdef = r.ReadFlag();
        s.EnableRestoration = r.ReadFlag();
        ParseColorConfig(ref r, s);
        s.FilmGrainParamsPresent = r.ReadFlag();
        return s;
    }

    private static void ParseColorConfig(ref Av1BitReader r, Av1SequenceHeader s)
    {
        var highBitdepth = r.ReadFlag();
        if (s.Profile == 2 && highBitdepth) s.BitDepth = r.ReadFlag() ? 12 : 10;
        else s.BitDepth = highBitdepth ? 10 : 8;
        s.MonoChrome = s.Profile != 1 && r.ReadFlag();
        s.NumPlanes = s.MonoChrome ? 1 : 3;
        s.ColorDescriptionPresent = r.ReadFlag();
        if (s.ColorDescriptionPresent)
        {
            s.ColorPrimaries = r.Read(8);
            s.TransferCharacteristics = r.Read(8);
            s.MatrixCoefficients = r.Read(8);
        }
        if (s.MonoChrome)
        {
            s.FullRange = r.ReadFlag();
            s.SubsamplingX = 1;
            s.SubsamplingY = 1;
            return;
        }
        if (s.ColorPrimaries == 1 && s.TransferCharacteristics == 13 && s.MatrixCoefficients == 0)
        {
            s.FullRange = true;
        }
        else
        {
            s.FullRange = r.ReadFlag();
            if (s.Profile == 0)
            {
                s.SubsamplingX = 1;
                s.SubsamplingY = 1;
            }
            else if (s.Profile == 2)
            {
                if (s.BitDepth == 12)
                {
                    s.SubsamplingX = r.Read(1);
                    s.SubsamplingY = s.SubsamplingX == 1 ? r.Read(1) : 0;
                }
                else
                {
                    s.SubsamplingX = 1;
                }
            }
            if (s.SubsamplingX == 1 && s.SubsamplingY == 1) s.ChromaSamplePosition = r.Read(2);
        }
        s.SeparateUvDeltaQ = r.ReadFlag();
    }
}

/// <summary>Film grain parameters (spec 5.9.30).</summary>
internal sealed class Av1FilmGrain
{
    public bool ApplyGrain;
    public int GrainSeed;
    public int NumYPoints;
    public readonly int[] PointYValue = new int[16];
    public readonly int[] PointYScaling = new int[16];
    public bool ChromaScalingFromLuma;
    public int NumCbPoints;
    public readonly int[] PointCbValue = new int[16];
    public readonly int[] PointCbScaling = new int[16];
    public int NumCrPoints;
    public readonly int[] PointCrValue = new int[16];
    public readonly int[] PointCrScaling = new int[16];
    public int GrainScalingMinus8;
    public int ArCoeffLag;
    public readonly int[] ArCoeffsYPlus128 = new int[24];
    public readonly int[] ArCoeffsCbPlus128 = new int[25];
    public readonly int[] ArCoeffsCrPlus128 = new int[25];
    public int ArCoeffShiftMinus6;
    public int GrainScaleShift;
    public int CbMult, CbLumaMult, CbOffset;
    public int CrMult, CrLumaMult, CrOffset;
    public bool OverlapFlag;
    public bool ClipToRestrictedRange;
}

/// <summary>The uncompressed header of an intra frame (spec 5.9) and the values derived from it.</summary>
internal sealed class Av1FrameHeader
{
    public const int KeyFrame = 0, InterFrame = 1, IntraOnlyFrame = 2, SwitchFrame = 3;
    public const int RestoreNone = 0, RestoreWiener = 1, RestoreSgrproj = 2, RestoreSwitchable = 3;
    public const int TxModeOnly4x4 = 0, TxModeLargest = 1, TxModeSelect = 2;

    public bool ShowExistingFrame;
    public int FrameToShowMapIdx;
    public int FrameType;
    public bool ShowFrame;
    public bool ShowableFrame;
    public bool ErrorResilientMode;
    public bool DisableCdfUpdate;
    public bool AllowScreenContentTools;
    public bool ForceIntegerMv;
    public int RefreshFrameFlags;
    public int FrameWidth;
    public int FrameHeight;
    public int UpscaledWidth;
    public int RenderWidth;
    public int RenderHeight;
    public bool UseSuperres;
    public int SuperresDenom = 8;
    public int MiCols;
    public int MiRows;
    public bool AllowIntrabc;
    public bool DisableFrameEndUpdateCdf;

    // Tiles.
    public int TileCols;
    public int TileRows;
    public int TileColsLog2;
    public int TileRowsLog2;
    public int[] MiColStarts = new int[65];
    public int[] MiRowStarts = new int[65];
    public int ContextUpdateTileId;
    public int TileSizeBytes = 4;

    // Quantization.
    public int BaseQIdx;
    public int DeltaQYDc, DeltaQUDc, DeltaQUAc, DeltaQVDc, DeltaQVAc;
    public bool UsingQmatrix;
    public int QmY, QmU, QmV;

    // Segmentation.
    public bool SegmentationEnabled;
    public bool SegmentationUpdateMap;
    public readonly bool[,] FeatureEnabled = new bool[8, 8];
    public readonly int[,] FeatureData = new int[8, 8];
    public bool SegIdPreSkip;
    public int LastActiveSegId;

    // Delta q / lf.
    public bool DeltaQPresent;
    public int DeltaQRes;
    public bool DeltaLfPresent;
    public int DeltaLfRes;
    public bool DeltaLfMulti;

    public bool CodedLossless;
    public bool AllLossless;
    public readonly bool[] LosslessArray = new bool[8];
    public readonly int[,] SegQMLevel = new int[3, 8];

    // Loop filter.
    public readonly int[] LoopFilterLevel = new int[4];
    public int LoopFilterSharpness;
    public bool LoopFilterDeltaEnabled;
    public readonly int[] LoopFilterRefDeltas = [1, 0, 0, 0, -1, 0, -1, -1];
    public readonly int[] LoopFilterModeDeltas = new int[2];

    // CDEF.
    public int CdefDamping = 3;
    public int CdefBits;
    public readonly int[] CdefYPriStrength = new int[8];
    public readonly int[] CdefYSecStrength = new int[8];
    public readonly int[] CdefUvPriStrength = new int[8];
    public readonly int[] CdefUvSecStrength = new int[8];

    // Loop restoration.
    public readonly int[] FrameRestorationType = new int[3];
    public readonly int[] LoopRestorationSize = new int[3];
    public bool UsesLr;

    public int TxMode;
    public bool ReducedTxSet;
    public readonly Av1FilmGrain FilmGrain = new();

    public bool FrameIsIntra => FrameType is KeyFrame or IntraOnlyFrame;

    /// <summary>
    /// Parses the uncompressed header. For an inter frame it stops after the frame type: decoding one would need the
    /// inter prediction tools this decoder leaves out, so the caller goes no further.
    /// </summary>
    public static Av1FrameHeader Parse(ref Av1BitReader r, Av1SequenceHeader seq, int temporalId, int spatialId)
    {
        var h = new Av1FrameHeader();
        var idLen = seq.FrameIdNumbersPresent ? seq.AdditionalFrameIdLength + seq.DeltaFrameIdLength : 0;
        const int allFrames = 0xFF;
        if (seq.ReducedStillPictureHeader)
        {
            h.FrameType = KeyFrame;
            h.ShowFrame = true;
        }
        else
        {
            h.ShowExistingFrame = r.ReadFlag();
            if (h.ShowExistingFrame)
            {
                h.FrameToShowMapIdx = r.Read(3);
                if (seq.DecoderModelInfoPresent && !seq.EqualPictureInterval) r.ReadBits(seq.FramePresentationTimeLength);
                if (seq.FrameIdNumbersPresent) r.ReadBits(idLen);
                return h;
            }
            h.FrameType = r.Read(2);
            h.ShowFrame = r.ReadFlag();
            if (h.ShowFrame && seq.DecoderModelInfoPresent && !seq.EqualPictureInterval) r.ReadBits(seq.FramePresentationTimeLength);
            h.ShowableFrame = h.ShowFrame ? h.FrameType != KeyFrame : r.ReadFlag();
            h.ErrorResilientMode = h.FrameType == SwitchFrame || (h.FrameType == KeyFrame && h.ShowFrame) || r.ReadFlag();
        }
        if (!h.FrameIsIntra) return h; // the caller stops here
        h.DisableCdfUpdate = r.ReadFlag();
        h.AllowScreenContentTools = seq.ForceScreenContentTools == 2 ? r.ReadFlag() : seq.ForceScreenContentTools == 1;
        if (h.AllowScreenContentTools && seq.ForceIntegerMv == 2) r.ReadBit();
        h.ForceIntegerMv = true;
        if (seq.FrameIdNumbersPresent) r.ReadBits(idLen); // current_frame_id
        var frameSizeOverride = !seq.ReducedStillPictureHeader && r.ReadFlag();
        r.ReadBits(seq.OrderHintBits); // order_hint
        if (seq.DecoderModelInfoPresent && r.ReadFlag())
        {
            for (var op = 0; op < seq.OperatingPointsCount; op++)
            {
                if (!seq.DecoderModelPresentForOp[op]) continue;
                var idc = seq.OperatingPointIdcs[op];
                var inTemporal = (idc >> temporalId) & 1;
                var inSpatial = (idc >> (spatialId + 8)) & 1;
                if (idc == 0 || (inTemporal == 1 && inSpatial == 1)) r.ReadBits(seq.BufferRemovalTimeLength);
            }
        }
        h.RefreshFrameFlags = h.FrameType == KeyFrame && h.ShowFrame ? allFrames : r.Read(8);
        if (h.RefreshFrameFlags != allFrames && h.ErrorResilientMode && seq.EnableOrderHint)
            for (var i = 0; i < 8; i++) r.ReadBits(seq.OrderHintBits); // ref_order_hint
        // frame_size()
        if (frameSizeOverride)
        {
            h.FrameWidth = (int)r.ReadBits(seq.FrameWidthBits) + 1;
            h.FrameHeight = (int)r.ReadBits(seq.FrameHeightBits) + 1;
        }
        else
        {
            h.FrameWidth = seq.MaxFrameWidth;
            h.FrameHeight = seq.MaxFrameHeight;
        }
        // superres_params()
        h.UseSuperres = seq.EnableSuperres && r.ReadFlag();
        h.SuperresDenom = h.UseSuperres ? r.Read(3) + 9 : 8;
        h.UpscaledWidth = h.FrameWidth;
        h.FrameWidth = (h.UpscaledWidth * 8 + h.SuperresDenom / 2) / h.SuperresDenom;
        h.MiCols = 2 * ((h.FrameWidth + 7) >> 3);
        h.MiRows = 2 * ((h.FrameHeight + 7) >> 3);
        // render_size()
        if (r.ReadFlag())
        {
            h.RenderWidth = r.Read(16) + 1;
            h.RenderHeight = r.Read(16) + 1;
        }
        else
        {
            h.RenderWidth = h.UpscaledWidth;
            h.RenderHeight = h.FrameHeight;
        }
        if (h.AllowScreenContentTools && h.UpscaledWidth == h.FrameWidth) h.AllowIntrabc = r.ReadFlag();
        h.DisableFrameEndUpdateCdf = seq.ReducedStillPictureHeader || h.DisableCdfUpdate || r.ReadFlag();

        ParseTileInfo(ref r, seq, h);
        ParseQuantization(ref r, seq, h);
        ParseSegmentation(ref r, h);
        if (h.BaseQIdx > 0) h.DeltaQPresent = r.ReadFlag();
        if (h.DeltaQPresent) h.DeltaQRes = r.Read(2);
        if (h.DeltaQPresent)
        {
            if (!h.AllowIntrabc) h.DeltaLfPresent = r.ReadFlag();
            if (h.DeltaLfPresent)
            {
                h.DeltaLfRes = r.Read(2);
                h.DeltaLfMulti = r.ReadFlag();
            }
        }
        h.CodedLossless = true;
        for (var segmentId = 0; segmentId < 8; segmentId++)
        {
            var qindex = h.GetQIndex(true, segmentId, 0);
            h.LosslessArray[segmentId] = qindex == 0 && h.DeltaQYDc == 0 && h.DeltaQUAc == 0 && h.DeltaQUDc == 0 && h.DeltaQVAc == 0 && h.DeltaQVDc == 0;
            if (!h.LosslessArray[segmentId]) h.CodedLossless = false;
            if (h.UsingQmatrix)
            {
                var lossless = h.LosslessArray[segmentId];
                h.SegQMLevel[0, segmentId] = lossless ? 15 : h.QmY;
                h.SegQMLevel[1, segmentId] = lossless ? 15 : h.QmU;
                h.SegQMLevel[2, segmentId] = lossless ? 15 : h.QmV;
            }
        }
        h.AllLossless = h.CodedLossless && h.FrameWidth == h.UpscaledWidth;
        ParseLoopFilter(ref r, seq, h);
        ParseCdef(ref r, seq, h);
        ParseLoopRestoration(ref r, seq, h);
        h.TxMode = h.CodedLossless ? TxModeOnly4x4 : r.ReadFlag() ? TxModeSelect : TxModeLargest;
        // frame_reference_mode, skip_mode_params, allow_warped_motion and global_motion_params read nothing for intra frames.
        h.ReducedTxSet = r.ReadFlag();
        ParseFilmGrain(ref r, seq, h);
        return h;
    }

    /// <summary>get_qindex (spec 7.12.2).</summary>
    public int GetQIndex(bool ignoreDeltaQ, int segmentId, int currentQIndex)
    {
        if (SegmentationEnabled && FeatureEnabled[segmentId, 0])
        {
            var data = FeatureData[segmentId, 0];
            var qindex = BaseQIdx + data;
            if (!ignoreDeltaQ && DeltaQPresent) qindex = currentQIndex + data;
            return Math.Clamp(qindex, 0, 255);
        }
        if (!ignoreDeltaQ && DeltaQPresent) return currentQIndex;
        return BaseQIdx;
    }

    private static int TileLog2(int blkSize, int target)
    {
        var k = 0;
        while ((blkSize << k) < target) k++;
        return k;
    }

    private static void ParseTileInfo(ref Av1BitReader r, Av1SequenceHeader seq, Av1FrameHeader h)
    {
        var sbCols = seq.Use128x128Superblock ? (h.MiCols + 31) >> 5 : (h.MiCols + 15) >> 4;
        var sbRows = seq.Use128x128Superblock ? (h.MiRows + 31) >> 5 : (h.MiRows + 15) >> 4;
        var sbShift = seq.Use128x128Superblock ? 5 : 4;
        var sbSize = sbShift + 2;
        var maxTileWidthSb = 4096 >> sbSize;
        var maxTileAreaSb = (4096 * 2304) >> (2 * sbSize);
        var minLog2TileCols = TileLog2(maxTileWidthSb, sbCols);
        var maxLog2TileCols = TileLog2(1, Math.Min(sbCols, 64));
        var maxLog2TileRows = TileLog2(1, Math.Min(sbRows, 64));
        var minLog2Tiles = Math.Max(minLog2TileCols, TileLog2(maxTileAreaSb, sbRows * sbCols));
        if (r.ReadFlag())
        {
            h.TileColsLog2 = minLog2TileCols;
            while (h.TileColsLog2 < maxLog2TileCols && r.ReadFlag()) h.TileColsLog2++;
            var tileWidthSb = (sbCols + (1 << h.TileColsLog2) - 1) >> h.TileColsLog2;
            var i = 0;
            for (var startSb = 0; startSb < sbCols; startSb += tileWidthSb) h.MiColStarts[i++] = startSb << sbShift;
            h.MiColStarts[i] = h.MiCols;
            h.TileCols = i;
            var minLog2TileRows = Math.Max(minLog2Tiles - h.TileColsLog2, 0);
            h.TileRowsLog2 = minLog2TileRows;
            while (h.TileRowsLog2 < maxLog2TileRows && r.ReadFlag()) h.TileRowsLog2++;
            var tileHeightSb = (sbRows + (1 << h.TileRowsLog2) - 1) >> h.TileRowsLog2;
            i = 0;
            for (var startSb = 0; startSb < sbRows; startSb += tileHeightSb) h.MiRowStarts[i++] = startSb << sbShift;
            h.MiRowStarts[i] = h.MiRows;
            h.TileRows = i;
        }
        else
        {
            var widestTileSb = 0;
            var startSb = 0;
            var i = 0;
            for (; startSb < sbCols; i++)
            {
                if (i >= 64) throw new ImageDecodeException("Too many AV1 tile columns.");
                h.MiColStarts[i] = startSb << sbShift;
                var maxWidth = Math.Min(sbCols - startSb, maxTileWidthSb);
                var sizeSb = r.ReadNs(maxWidth) + 1;
                widestTileSb = Math.Max(sizeSb, widestTileSb);
                startSb += sizeSb;
            }
            h.MiColStarts[i] = h.MiCols;
            h.TileCols = i;
            h.TileColsLog2 = TileLog2(1, h.TileCols);
            maxTileAreaSb = minLog2Tiles > 0 ? (sbRows * sbCols) >> (minLog2Tiles + 1) : sbRows * sbCols;
            var maxTileHeightSb = Math.Max(maxTileAreaSb / widestTileSb, 1);
            startSb = 0;
            for (i = 0; startSb < sbRows; i++)
            {
                if (i >= 64) throw new ImageDecodeException("Too many AV1 tile rows.");
                h.MiRowStarts[i] = startSb << sbShift;
                var maxHeight = Math.Min(sbRows - startSb, maxTileHeightSb);
                startSb += r.ReadNs(maxHeight) + 1;
            }
            h.MiRowStarts[i] = h.MiRows;
            h.TileRows = i;
            h.TileRowsLog2 = TileLog2(1, h.TileRows);
        }
        if (h.TileColsLog2 > 0 || h.TileRowsLog2 > 0)
        {
            h.ContextUpdateTileId = r.Read(h.TileRowsLog2 + h.TileColsLog2);
            h.TileSizeBytes = r.Read(2) + 1;
        }
    }

    private static int ReadDeltaQ(ref Av1BitReader r) => r.ReadFlag() ? r.ReadSigned(7) : 0;

    private static void ParseQuantization(ref Av1BitReader r, Av1SequenceHeader seq, Av1FrameHeader h)
    {
        h.BaseQIdx = r.Read(8);
        h.DeltaQYDc = ReadDeltaQ(ref r);
        if (seq.NumPlanes > 1)
        {
            var diffUvDelta = seq.SeparateUvDeltaQ && r.ReadFlag();
            h.DeltaQUDc = ReadDeltaQ(ref r);
            h.DeltaQUAc = ReadDeltaQ(ref r);
            if (diffUvDelta)
            {
                h.DeltaQVDc = ReadDeltaQ(ref r);
                h.DeltaQVAc = ReadDeltaQ(ref r);
            }
            else
            {
                h.DeltaQVDc = h.DeltaQUDc;
                h.DeltaQVAc = h.DeltaQUAc;
            }
        }
        h.UsingQmatrix = r.ReadFlag();
        if (h.UsingQmatrix)
        {
            h.QmY = r.Read(4);
            h.QmU = r.Read(4);
            h.QmV = seq.SeparateUvDeltaQ ? r.Read(4) : h.QmU;
        }
    }

    private static void ParseSegmentation(ref Av1BitReader r, Av1FrameHeader h)
    {
        h.SegmentationEnabled = r.ReadFlag();
        if (h.SegmentationEnabled)
        {
            // Intra frames have no primary reference frame, so the map and data are always sent.
            h.SegmentationUpdateMap = true;
            for (var i = 0; i < 8; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    var enabled = r.ReadFlag();
                    h.FeatureEnabled[i, j] = enabled;
                    var clipped = 0;
                    if (enabled)
                    {
                        var bits = Av1Tables.SegmentationFeatureBits[j];
                        var limit = Av1Tables.SegmentationFeatureMax[j];
                        clipped = Av1Tables.SegmentationFeatureSigned[j] == 1
                            ? Math.Clamp(r.ReadSigned(1 + bits), -limit, limit)
                            : Math.Clamp(r.Read(bits), 0, limit);
                    }
                    h.FeatureData[i, j] = clipped;
                }
            }
        }
        for (var i = 0; i < 8; i++)
        {
            for (var j = 0; j < 8; j++)
            {
                if (!h.FeatureEnabled[i, j]) continue;
                h.LastActiveSegId = i;
                if (j >= 5) h.SegIdPreSkip = true;
            }
        }
    }

    private static void ParseLoopFilter(ref Av1BitReader r, Av1SequenceHeader seq, Av1FrameHeader h)
    {
        if (h.CodedLossless || h.AllowIntrabc) return;
        h.LoopFilterLevel[0] = r.Read(6);
        h.LoopFilterLevel[1] = r.Read(6);
        if (seq.NumPlanes > 1 && (h.LoopFilterLevel[0] != 0 || h.LoopFilterLevel[1] != 0))
        {
            h.LoopFilterLevel[2] = r.Read(6);
            h.LoopFilterLevel[3] = r.Read(6);
        }
        h.LoopFilterSharpness = r.Read(3);
        h.LoopFilterDeltaEnabled = r.ReadFlag();
        if (h.LoopFilterDeltaEnabled && r.ReadFlag())
        {
            for (var i = 0; i < 8; i++)
                if (r.ReadFlag()) h.LoopFilterRefDeltas[i] = r.ReadSigned(7);
            for (var i = 0; i < 2; i++)
                if (r.ReadFlag()) h.LoopFilterModeDeltas[i] = r.ReadSigned(7);
        }
    }

    private static void ParseCdef(ref Av1BitReader r, Av1SequenceHeader seq, Av1FrameHeader h)
    {
        if (h.CodedLossless || h.AllowIntrabc || !seq.EnableCdef) return;
        h.CdefDamping = r.Read(2) + 3;
        h.CdefBits = r.Read(2);
        for (var i = 0; i < 1 << h.CdefBits; i++)
        {
            h.CdefYPriStrength[i] = r.Read(4);
            h.CdefYSecStrength[i] = r.Read(2);
            if (h.CdefYSecStrength[i] == 3) h.CdefYSecStrength[i]++;
            if (seq.NumPlanes > 1)
            {
                h.CdefUvPriStrength[i] = r.Read(4);
                h.CdefUvSecStrength[i] = r.Read(2);
                if (h.CdefUvSecStrength[i] == 3) h.CdefUvSecStrength[i]++;
            }
        }
    }

    private static void ParseLoopRestoration(ref Av1BitReader r, Av1SequenceHeader seq, Av1FrameHeader h)
    {
        if (h.AllLossless || h.AllowIntrabc || !seq.EnableRestoration) return;
        var usesChromaLr = false;
        for (var i = 0; i < seq.NumPlanes; i++)
        {
            h.FrameRestorationType[i] = Av1Tables.RemapLrType[r.Read(2)];
            if (h.FrameRestorationType[i] != RestoreNone)
            {
                h.UsesLr = true;
                if (i > 0) usesChromaLr = true;
            }
        }
        if (!h.UsesLr) return;
        int lrUnitShift;
        if (seq.Use128x128Superblock)
        {
            lrUnitShift = r.Read(1) + 1;
        }
        else
        {
            lrUnitShift = r.Read(1);
            if (lrUnitShift != 0) lrUnitShift += r.Read(1);
        }
        h.LoopRestorationSize[0] = 256 >> (2 - lrUnitShift);
        var lrUvShift = seq.SubsamplingX == 1 && seq.SubsamplingY == 1 && usesChromaLr ? r.Read(1) : 0;
        h.LoopRestorationSize[1] = h.LoopRestorationSize[0] >> lrUvShift;
        h.LoopRestorationSize[2] = h.LoopRestorationSize[0] >> lrUvShift;
    }

    private static void ParseFilmGrain(ref Av1BitReader r, Av1SequenceHeader seq, Av1FrameHeader h)
    {
        var g = h.FilmGrain;
        if (!seq.FilmGrainParamsPresent || (!h.ShowFrame && !h.ShowableFrame)) return;
        g.ApplyGrain = r.ReadFlag();
        if (!g.ApplyGrain) return;
        g.GrainSeed = r.Read(16);
        // update_grain is only coded for inter frames; intra frames always send their parameters.
        g.NumYPoints = r.Read(4);
        if (g.NumYPoints > 14) throw new ImageDecodeException("Invalid AV1 film grain parameters.");
        for (var i = 0; i < g.NumYPoints; i++)
        {
            g.PointYValue[i] = r.Read(8);
            g.PointYScaling[i] = r.Read(8);
        }
        g.ChromaScalingFromLuma = !seq.MonoChrome && r.ReadFlag();
        if (seq.MonoChrome || g.ChromaScalingFromLuma || (seq.SubsamplingX == 1 && seq.SubsamplingY == 1 && g.NumYPoints == 0))
        {
            g.NumCbPoints = 0;
            g.NumCrPoints = 0;
        }
        else
        {
            g.NumCbPoints = r.Read(4);
            if (g.NumCbPoints > 10) throw new ImageDecodeException("Invalid AV1 film grain parameters.");
            for (var i = 0; i < g.NumCbPoints; i++)
            {
                g.PointCbValue[i] = r.Read(8);
                g.PointCbScaling[i] = r.Read(8);
            }
            g.NumCrPoints = r.Read(4);
            if (g.NumCrPoints > 10) throw new ImageDecodeException("Invalid AV1 film grain parameters.");
            for (var i = 0; i < g.NumCrPoints; i++)
            {
                g.PointCrValue[i] = r.Read(8);
                g.PointCrScaling[i] = r.Read(8);
            }
        }
        g.GrainScalingMinus8 = r.Read(2);
        g.ArCoeffLag = r.Read(2);
        var numPosLuma = 2 * g.ArCoeffLag * (g.ArCoeffLag + 1);
        int numPosChroma;
        if (g.NumYPoints > 0)
        {
            numPosChroma = numPosLuma + 1;
            for (var i = 0; i < numPosLuma; i++) g.ArCoeffsYPlus128[i] = r.Read(8);
        }
        else
        {
            numPosChroma = numPosLuma;
        }
        if (g.ChromaScalingFromLuma || g.NumCbPoints > 0)
            for (var i = 0; i < numPosChroma; i++) g.ArCoeffsCbPlus128[i] = r.Read(8);
        if (g.ChromaScalingFromLuma || g.NumCrPoints > 0)
            for (var i = 0; i < numPosChroma; i++) g.ArCoeffsCrPlus128[i] = r.Read(8);
        g.ArCoeffShiftMinus6 = r.Read(2);
        g.GrainScaleShift = r.Read(2);
        if (g.NumCbPoints > 0)
        {
            g.CbMult = r.Read(8);
            g.CbLumaMult = r.Read(8);
            g.CbOffset = r.Read(9);
        }
        if (g.NumCrPoints > 0)
        {
            g.CrMult = r.Read(8);
            g.CrLumaMult = r.Read(8);
            g.CrOffset = r.Read(9);
        }
        g.OverlapFlag = r.ReadFlag();
        g.ClipToRestrictedRange = r.ReadFlag();
    }
}
