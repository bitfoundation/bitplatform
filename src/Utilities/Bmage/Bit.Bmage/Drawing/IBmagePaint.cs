namespace Bit.Bmage;

/// <summary>Supplies a colour per pixel; implemented by solid colours and gradients.</summary>
public interface IBmagePaint
{
    /// <summary>Returns the colour to paint at the given pixel.</summary>
    BmageRgba32 GetColor(int x, int y);
}
