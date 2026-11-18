namespace Bit.Bmage;

/// <summary>A single colour paint.</summary>
public sealed class BmageSolidPaint : IBmagePaint
{
    public BmageSolidPaint(BmageRgba32 color) => Color = color;
    public BmageRgba32 Color { get; }
    public BmageRgba32 GetColor(int x, int y) => Color;
}
