using System.Buffers;

namespace Bit.Bmage.Codecs.Avif;

/// <summary>One plane of samples, 16 bits each whatever the bit depth, so one code path serves 8, 10 and 12 bits.</summary>
internal sealed class Av1Plane : IDisposable
{
    public ushort[] Data;
    public readonly int Stride;
    public readonly int Height;

    public Av1Plane(int stride, int height)
    {
        Stride = stride;
        Height = height;
        Data = ArrayPool<ushort>.Shared.Rent(stride * height);
    }

    public Av1Plane Clone()
    {
        var copy = new Av1Plane(Stride, Height);
        Data.AsSpan(0, Stride * Height).CopyTo(copy.Data);
        return copy;
    }

    public void Dispose()
    {
        var data = Data;
        Data = [];
        if (data.Length > 0) ArrayPool<ushort>.Shared.Return(data);
    }
}

/// <summary>
/// The state of the frame being decoded: its sample planes, padded to whole superblocks so that blocks overhanging the
/// right and bottom edges never need bounds checks, and the per 4x4 mode information later blocks and the loop filters
/// read back.
/// </summary>
internal sealed class Av1Frame : IDisposable
{
    public readonly Av1SequenceHeader Seq;
    public readonly Av1FrameHeader Hdr;
    public readonly int MiRows, MiCols;
    public readonly int SubX, SubY;
    public readonly int BitDepth;
    public readonly int NumPlanes;
    public readonly Av1Plane[] Planes;

    // Mode info, one entry per 4x4 luma block, MiCols to a row.
    public readonly byte[] MiSizes;
    public readonly byte[] YModes;
    public readonly byte[] UVModes;
    public readonly sbyte[] RefFrames0; // -1 until the block is decoded, INTRA_FRAME (0) after
    public readonly byte[] IsInters;
    public readonly byte[] Skips;
    public readonly byte[] InterTxSizes;
    public readonly byte[] SegmentIds;
    public readonly byte[] PaletteSizesY;
    public readonly byte[] PaletteSizesUV;
    public readonly byte[] TxTypes;
    public readonly short[]? MvRows;
    public readonly short[]? MvCols;
    public readonly sbyte[]? DeltaLFs; // four per block, only when delta_lf_present
    public readonly byte[][] LoopfilterTxSizes; // per plane, in that plane's 4x4 units
    public readonly int[] LfStride;
    public readonly sbyte[] CdefIdx; // per 64x64 block, -1 until read
    public readonly int CdefStride;

    // Loop restoration units per plane.
    public readonly int[] LrUnitRows = new int[3];
    public readonly int[] LrUnitCols = new int[3];
    public readonly byte[][] LrType = new byte[3][];
    public readonly sbyte[][] LrWiener = new sbyte[3][]; // [unit][pass][3]
    public readonly byte[][] LrSgrSet = new byte[3][];
    public readonly short[][] LrSgrXqd = new short[3][]; // [unit][2]

    public Av1Frame(Av1SequenceHeader seq, Av1FrameHeader hdr)
    {
        Seq = seq;
        Hdr = hdr;
        MiRows = hdr.MiRows;
        MiCols = hdr.MiCols;
        SubX = seq.SubsamplingX;
        SubY = seq.SubsamplingY;
        BitDepth = seq.BitDepth;
        NumPlanes = seq.NumPlanes;

        var sbSize = seq.Use128x128Superblock ? 128 : 64;
        var lumaW = (MiCols * 4 + sbSize - 1) / sbSize * sbSize;
        var lumaH = (MiRows * 4 + sbSize - 1) / sbSize * sbSize;
        Planes = new Av1Plane[NumPlanes];
        try
        {
            Planes[0] = new Av1Plane(lumaW, lumaH);
            for (var p = 1; p < NumPlanes; p++) Planes[p] = new Av1Plane(lumaW >> SubX, lumaH >> SubY);
        }
        catch
        {
            foreach (var plane in Planes) plane?.Dispose();
            throw;
        }

        var mi = MiRows * MiCols;
        MiSizes = new byte[mi];
        YModes = new byte[mi];
        UVModes = new byte[mi];
        RefFrames0 = new sbyte[mi];
        RefFrames0.AsSpan().Fill(-1);
        IsInters = new byte[mi];
        Skips = new byte[mi];
        InterTxSizes = new byte[mi];
        SegmentIds = new byte[mi];
        PaletteSizesY = new byte[mi];
        PaletteSizesUV = new byte[mi];
        TxTypes = new byte[mi];
        if (hdr.AllowIntrabc)
        {
            MvRows = new short[mi];
            MvCols = new short[mi];
        }
        if (hdr.DeltaLfPresent) DeltaLFs = new sbyte[mi * 4];
        LoopfilterTxSizes = new byte[NumPlanes][];
        LfStride = new int[NumPlanes];
        for (var p = 0; p < NumPlanes; p++)
        {
            var sx = p == 0 ? 0 : SubX;
            var sy = p == 0 ? 0 : SubY;
            LfStride[p] = (MiCols + sx) >> sx;
            LoopfilterTxSizes[p] = new byte[LfStride[p] * ((MiRows + sy) >> sy)];
        }
        CdefStride = (MiCols + 15) >> 4;
        CdefIdx = new sbyte[CdefStride * ((MiRows + 15) >> 4)];
        CdefIdx.AsSpan().Fill(-1);

        if (hdr.UsesLr)
        {
            for (var p = 0; p < NumPlanes; p++)
            {
                if (hdr.FrameRestorationType[p] == Av1FrameHeader.RestoreNone) continue;
                var sx = p == 0 ? 0 : SubX;
                var sy = p == 0 ? 0 : SubY;
                var unitSize = hdr.LoopRestorationSize[p];
                LrUnitRows[p] = CountUnits(unitSize, Round2(hdr.FrameHeight, sy));
                LrUnitCols[p] = CountUnits(unitSize, Round2(hdr.UpscaledWidth, sx));
                var units = LrUnitRows[p] * LrUnitCols[p];
                LrType[p] = new byte[units];
                LrWiener[p] = new sbyte[units * 6];
                LrSgrSet[p] = new byte[units];
                LrSgrXqd[p] = new short[units * 2];
            }
        }
    }

    public static int CountUnits(int unitSize, int frameSize) => Math.Max((frameSize + (unitSize >> 1)) / unitSize, 1);

    public static int Round2(int x, int n) => n == 0 ? x : (x + (1 << (n - 1))) >> n;

    public void Dispose()
    {
        foreach (var plane in Planes) plane?.Dispose();
    }
}
