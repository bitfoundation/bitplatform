namespace Bit.Blow;

/// <summary>
/// An axis-aligned rectangle in flow coordinate space.
/// </summary>
public readonly record struct BlowRect(double X, double Y, double Width, double Height)
{
    /// <summary>The rectangle's smaller X coordinate.</summary>
    public double Left => X;

    /// <summary>The rectangle's smaller Y coordinate.</summary>
    public double Top => Y;

    /// <summary>The rectangle's larger X coordinate.</summary>
    public double Right => X + Width;

    /// <summary>The rectangle's larger Y coordinate.</summary>
    public double Bottom => Y + Height;
}
