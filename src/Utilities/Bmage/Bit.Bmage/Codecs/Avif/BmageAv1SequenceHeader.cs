namespace Bit.Bmage;

/// <summary>The fields of an AV1 sequence header OBU (spec 5.5) that decoding a still image needs.</summary>
internal sealed class BmageAv1SequenceHeader
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

    public static BmageAv1SequenceHeader Parse(ReadOnlySpan<byte> payload)
    {
        var r = new BmageAv1BitReader(payload);
        var s = new BmageAv1SequenceHeader();
        s.Profile = r.Read(3);
        if (s.Profile > 2) throw new BmageImageDecodeException($"Unsupported AV1 profile {s.Profile}.");
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

    private static void ParseColorConfig(ref BmageAv1BitReader r, BmageAv1SequenceHeader s)
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
