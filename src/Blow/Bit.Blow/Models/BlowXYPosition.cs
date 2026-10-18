namespace Bit.Blow;

/// <summary>
/// A point in flow coordinate space (pre-viewport-transform).
/// </summary>
public readonly record struct BlowXYPosition(double X, double Y)
{
    /// <summary>The flow-space origin.</summary>
    public static readonly BlowXYPosition Zero = new(0, 0);

    /// <summary>Returns this point translated by <paramref name="other"/>.</summary>
    public BlowXYPosition Add(BlowXYPosition other) => new(X + other.X, Y + other.Y);

    /// <summary>Returns this point translated by the negation of <paramref name="other"/>.</summary>
    public BlowXYPosition Subtract(BlowXYPosition other) => new(X - other.X, Y - other.Y);

    /// <summary>Renders the point as <c>(x, y)</c>, rounded for reading rather than for round-tripping.</summary>
    /// <remarks>
    /// Formatted for the current culture, since this is what gets read out to the user. Where the
    /// decimal separator is a comma the coordinates are separated by a semicolon instead, so
    /// <c>(1,5; 2,5)</c> cannot be mistaken for four numbers.
    /// </remarks>
    public override string ToString()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var separator = culture.NumberFormat.NumberDecimalSeparator == "," ? "; " : ", ";
        return string.Format(culture, "({0:0.##}{1}{2:0.##})", X, separator, Y);
    }
}

/// <summary>
/// A measured width/height in pixels (at zoom = 1).
/// </summary>
public readonly record struct BlowDimensions(double Width, double Height)
{
    /// <summary>A zero size, the value a node carries until it has been measured.</summary>
    public static readonly BlowDimensions Empty = new(0, 0);

    /// <summary>True when either dimension is zero or negative, i.e. nothing has been measured yet.</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

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
