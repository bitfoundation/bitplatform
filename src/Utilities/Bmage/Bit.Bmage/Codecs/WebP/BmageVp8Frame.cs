namespace Bit.Bmage;

/// <summary>A VP8 key frame's 8-bit Y, U and V planes, chroma at half resolution, padded to whole macroblocks.</summary>
internal sealed class BmageVp8Frame
{
    public BmageVp8Frame(int width, int height)
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
