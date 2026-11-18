namespace Bit.BbScanner;

/// <summary>
/// Part of a symbol one line read: where its middle lies in the image; how far apart, in pixels, the lines that could
/// hold the rest lie either side of it and how far out they go; and the symbol's own axis, as a unit vector, once the
/// lines read so far show it - zero until then.
/// </summary>
internal readonly record struct BbScannerPartialRead(float X, float Y, float Spacing, float Reach, float AxisX, float AxisY)
{
    public bool HasAxis => AxisX != 0 || AxisY != 0;
}
