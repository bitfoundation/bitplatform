using System.Globalization;
using System.Text.Json.Serialization;

namespace Bit.Bonboarding;

/// <summary>An axis-aligned rectangle in CSS viewport pixels.</summary>
public readonly record struct BonboardingRect(double X, double Y, double Width, double Height)
{
    public static readonly BonboardingRect Empty = default;

    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public double CenterX => X + (Width / 2);
    public double CenterY => Y + (Height / 2);
    public double Area => Width * Height;

    [JsonIgnore]
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public static BonboardingRect FromEdges(double left, double top, double right, double bottom)
        => new(left, top, right - left, bottom - top);

    /// <summary>Grows the rectangle by <paramref name="amount"/> on every side (negative shrinks).</summary>
    public BonboardingRect Inflate(double amount) => Inflate(amount, amount);

    /// <summary>Grows the rectangle horizontally and vertically (negative shrinks).</summary>
    public BonboardingRect Inflate(double x, double y) => new(X - x, Y - y, Width + (2 * x), Height + (2 * y));

    /// <summary>Translates the rectangle.</summary>
    public BonboardingRect Offset(double dx, double dy) => new(X + dx, Y + dy, Width, Height);

    public bool Contains(double px, double py) => px >= Left && px <= Right && py >= Top && py <= Bottom;

    public bool Contains(BonboardingRect other) => other.Left >= Left && other.Top >= Top && other.Right <= Right && other.Bottom <= Bottom;

    public bool IntersectsWith(BonboardingRect other) => other.Left < Right && other.Right > Left && other.Top < Bottom && other.Bottom > Top;

    /// <summary>The overlapping region, or <see cref="Empty"/> when the rectangles are disjoint.</summary>
    public BonboardingRect Intersect(BonboardingRect other)
    {
        var left = Math.Max(Left, other.Left);
        var top = Math.Max(Top, other.Top);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);
        return right <= left || bottom <= top ? Empty : FromEdges(left, top, right, bottom);
    }

    /// <summary>The smallest rectangle containing both.</summary>
    public BonboardingRect Union(BonboardingRect other)
    {
        if (IsEmpty) return other;
        if (other.IsEmpty) return this;
        return FromEdges(Math.Min(Left, other.Left), Math.Min(Top, other.Top), Math.Max(Right, other.Right), Math.Max(Bottom, other.Bottom));
    }

    /// <summary>True when both rectangles agree to within <paramref name="tolerance"/> device pixels on every edge.</summary>
    public bool ApproximatelyEquals(BonboardingRect other, double tolerance = 0.5)
        => Math.Abs(X - other.X) <= tolerance
        && Math.Abs(Y - other.Y) <= tolerance
        && Math.Abs(Width - other.Width) <= tolerance
        && Math.Abs(Height - other.Height) <= tolerance;

    /// <summary>Rounds every component to <paramref name="digits"/> decimals; keeps generated CSS stable.</summary>
    public BonboardingRect Round(int digits = 2)
        => new(Math.Round(X, digits), Math.Round(Y, digits), Math.Round(Width, digits), Math.Round(Height, digits));

    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"BonboardingRect({X:0.##}, {Y:0.##}, {Width:0.##}x{Height:0.##})");
}
