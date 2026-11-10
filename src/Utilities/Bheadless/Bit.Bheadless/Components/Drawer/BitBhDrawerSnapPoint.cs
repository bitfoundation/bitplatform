using System.Globalization;

namespace Bit.Bheadless;

/// <summary>
/// A size a <see cref="BitBhDrawer"/> can rest at (<see cref="BitBhDrawer.SnapPoints"/>): how much of the panel shows,
/// measured from the edge the drawer is attached to - a fraction of the viewport (its height for a top or bottom
/// drawer, its width for a left or right one) or a number of pixels. A panel smaller than the size shows whole.
/// <code>
/// SnapPoints="@([0.25, "400px", 1])"     @* a number is a fraction, a string "…px" pixels or "…%" a percentage *@
/// </code>
/// Two snap points are equal when they have the same <see cref="Value"/> and unit (<c>"50%"</c> equals <c>0.5</c>);
/// <see cref="ToString"/> gives the form <c>data-snap-point</c> carries: <c>0.5</c>, <c>400px</c>.
/// </summary>
public readonly record struct BitBhDrawerSnapPoint
{
    private BitBhDrawerSnapPoint(double value, bool isPixels)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentOutOfRangeException(nameof(value), value, "A snap point must be a finite number.");
        Value = value;
        IsPixels = isPixels;
    }

    /// <summary>The size: a fraction of the viewport (0-1), or pixels when <see cref="IsPixels"/>.</summary>
    public double Value { get; }

    /// <summary>Whether <see cref="Value"/> is in pixels rather than a fraction of the viewport.</summary>
    public bool IsPixels { get; }

    /// <summary>A fraction (0-1) of the viewport's height (top / bottom drawers) or width (left / right drawers).</summary>
    public static BitBhDrawerSnapPoint Fraction(double fraction) => new(fraction, false);

    /// <summary>A size in CSS pixels.</summary>
    public static BitBhDrawerSnapPoint Pixels(double pixels) => new(pixels, true);

    /// <summary>
    /// Reads <c>"400px"</c> (pixels), <c>"50%"</c> (a percentage) or <c>"0.5"</c> (a fraction), with invariant-culture numbers.
    /// </summary>
    /// <exception cref="FormatException">The text is none of these.</exception>
    public static BitBhDrawerSnapPoint Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var text = value.Trim();
        var unit = text.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? "px" : text.EndsWith('%') ? "%" : "";
        var number = text[..^unit.Length].Trim();
        if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || double.IsNaN(parsed) || double.IsInfinity(parsed))
            throw new FormatException($"'{value}' is not a snap point: use a fraction (0.5), a percentage (50%) or pixels (400px).");
        return unit switch
        {
            "px" => Pixels(parsed),
            "%" => Fraction(parsed / 100),
            _ => Fraction(parsed),
        };
    }

    /// <summary>A fraction of the viewport: <c>SnapPoints="@([0.5, 1])"</c>.</summary>
    public static implicit operator BitBhDrawerSnapPoint(double fraction) => Fraction(fraction);

    /// <summary>Pixels or a percentage: <c>SnapPoints="@(["160px", "50%", 1])"</c> (see <see cref="Parse"/>).</summary>
    public static implicit operator BitBhDrawerSnapPoint(string value) => Parse(value);

    /// <summary>The invariant form: <c>0.5</c> for a fraction, <c>400px</c> for pixels.</summary>
    public override string ToString()
        => IsPixels
            ? Value.ToString("R", CultureInfo.InvariantCulture) + "px"
            : Value.ToString("R", CultureInfo.InvariantCulture);
}
