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
