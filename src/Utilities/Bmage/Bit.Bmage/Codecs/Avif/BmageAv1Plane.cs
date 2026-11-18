using System.Buffers;

namespace Bit.Bmage;

/// <summary>One plane of samples, 16 bits each whatever the bit depth, so one code path serves 8, 10 and 12 bits.</summary>
internal sealed class BmageAv1Plane : IDisposable
{
    public ushort[] Data;
    public readonly int Stride;
    public readonly int Height;

    public BmageAv1Plane(int stride, int height)
    {
        Stride = stride;
        Height = height;
        Data = ArrayPool<ushort>.Shared.Rent(stride * height);
    }

    public BmageAv1Plane Clone()
    {
        var copy = new BmageAv1Plane(Stride, Height);
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
