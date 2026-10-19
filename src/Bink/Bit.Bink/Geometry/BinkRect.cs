using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// An axis-aligned rectangle in document units. Immutable.
/// </summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="Width">Width (never negative for a normalized rectangle).</param>
/// <param name="Height">Height (never negative for a normalized rectangle).</param>
public readonly record struct BinkRect(float X, float Y, float Width, float Height)
{
    /// <summary>
    /// The "no rectangle" value, used as the starting point when accumulating bounds and returned when there is
    /// nothing to measure. It is the default value of the type, so a zero-size rectangle exactly at the origin is
    /// indistinguishable from it.
    /// </summary>
    public static readonly BinkRect Empty;

    /// <summary>Left edge.</summary>
    public float Left => X;

    /// <summary>Top edge.</summary>
    public float Top => Y;

    /// <summary>Right edge.</summary>
    public float Right => X + Width;

    /// <summary>Bottom edge.</summary>
    public float Bottom => Y + Height;

    /// <summary>Center point.</summary>
    public Vector2 Center => new(X + Width / 2f, Y + Height / 2f);

    /// <summary>Top-left corner.</summary>
    public Vector2 TopLeft => new(X, Y);

    /// <summary>Size as a vector.</summary>
    public Vector2 Size => new(Width, Height);

    /// <summary>True when the rectangle has no area (including the <see cref="Empty"/> value).</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>True for the <see cref="Empty"/> sentinel: bounds accumulation treats this as "nothing yet".</summary>
    public bool IsUnset => this == Empty;

    /// <summary>Creates a rectangle from two corner points in any order.</summary>
    public static BinkRect FromPoints(Vector2 a, Vector2 b)
    {
        float minX = MathF.Min(a.X, b.X);
        float minY = MathF.Min(a.Y, b.Y);
        return new BinkRect(minX, minY, MathF.Abs(a.X - b.X), MathF.Abs(a.Y - b.Y));
    }

    /// <summary>Creates a rectangle from edges.</summary>
    public static BinkRect FromLTRB(float left, float top, float right, float bottom)
        => new(left, top, right - left, bottom - top);

    /// <summary>Creates a rectangle centered on a point.</summary>
    public static BinkRect FromCenter(Vector2 center, float width, float height)
        => new(center.X - width / 2f, center.Y - height / 2f, width, height);

    /// <summary>Returns the smallest rectangle containing both rectangles. The <see cref="Empty"/> sentinel is ignored.</summary>
    public BinkRect Union(BinkRect other)
    {
        if (IsUnset) return other;
        if (other.IsUnset) return this;
        return FromLTRB(MathF.Min(Left, other.Left), MathF.Min(Top, other.Top), MathF.Max(Right, other.Right), MathF.Max(Bottom, other.Bottom));
    }

    /// <summary>Returns the smallest rectangle containing this rectangle and a point.</summary>
    public BinkRect Union(Vector2 point)
    {
        if (IsUnset) return new BinkRect(point.X, point.Y, 0, 0);
        return FromLTRB(MathF.Min(Left, point.X), MathF.Min(Top, point.Y), MathF.Max(Right, point.X), MathF.Max(Bottom, point.Y));
    }

    /// <summary>Returns the intersection of two rectangles, or <see cref="Empty"/> when they do not overlap.</summary>
    public BinkRect Intersect(BinkRect other)
    {
        float l = MathF.Max(Left, other.Left);
        float t = MathF.Max(Top, other.Top);
        float r = MathF.Min(Right, other.Right);
        float b = MathF.Min(Bottom, other.Bottom);
        return r <= l || b <= t ? Empty : FromLTRB(l, t, r, b);
    }

    /// <summary>
    /// True when the rectangles overlap, with touching edges counting as overlapping. A zero-area rectangle is a
    /// point or a line, which can still overlap: only the <see cref="Empty"/> sentinel intersects nothing.
    /// </summary>
    public bool Intersects(BinkRect other)
        => !IsUnset && !other.IsUnset && other.Left <= Right && other.Right >= Left && other.Top <= Bottom && other.Bottom >= Top;

    /// <summary>True when the point lies inside or on the edge.</summary>
    public bool Contains(Vector2 point)
        => point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

    /// <summary>True when <paramref name="other"/> lies entirely inside this rectangle.</summary>
    public bool Contains(BinkRect other)
        => other.Left >= Left && other.Right <= Right && other.Top >= Top && other.Bottom <= Bottom;

    /// <summary>Grows the rectangle by the given amount on every side (negative shrinks).</summary>
    public BinkRect Inflate(float amount) => Inflate(amount, amount);

    /// <summary>Grows the rectangle by the given amounts (negative shrinks).</summary>
    public BinkRect Inflate(float dx, float dy) => new(X - dx, Y - dy, Width + 2 * dx, Height + 2 * dy);

    /// <summary>Translates the rectangle.</summary>
    public BinkRect Offset(Vector2 delta) => new(X + delta.X, Y + delta.Y, Width, Height);

    /// <summary>Returns the axis-aligned bounding box of the transformed rectangle.</summary>
    public BinkRect Transform(Matrix3x2 matrix)
    {
        if (matrix.IsIdentity) return this;
        Vector2 a = Vector2.Transform(new Vector2(Left, Top), matrix);
        Vector2 b = Vector2.Transform(new Vector2(Right, Top), matrix);
        Vector2 c = Vector2.Transform(new Vector2(Right, Bottom), matrix);
        Vector2 d = Vector2.Transform(new Vector2(Left, Bottom), matrix);
        float minX = MathF.Min(MathF.Min(a.X, b.X), MathF.Min(c.X, d.X));
        float minY = MathF.Min(MathF.Min(a.Y, b.Y), MathF.Min(c.Y, d.Y));
        float maxX = MathF.Max(MathF.Max(a.X, b.X), MathF.Max(c.X, d.X));
        float maxY = MathF.Max(MathF.Max(a.Y, b.Y), MathF.Max(c.Y, d.Y));
        return FromLTRB(minX, minY, maxX, maxY);
    }

    /// <summary>Rounds the rectangle outwards to integer coordinates.</summary>
    public BinkRect RoundOut()
    {
        float l = MathF.Floor(Left);
        float t = MathF.Floor(Top);
        return FromLTRB(l, t, MathF.Ceiling(Right), MathF.Ceiling(Bottom));
    }

    /// <summary>Returns a rectangle with non-negative width and height.</summary>
    public BinkRect Normalize()
        => Width >= 0 && Height >= 0 ? this : FromLTRB(MathF.Min(Left, Right), MathF.Min(Top, Bottom), MathF.Max(Left, Right), MathF.Max(Top, Bottom));

    /// <inheritdoc />
    public override string ToString() => FormattableString.Invariant($"BinkRect({X}, {Y}, {Width}, {Height})");
}
