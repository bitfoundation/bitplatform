using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bit.Bink;

/// <summary>
/// Allocation-free 2D geometry helpers used by stroke generation, hit testing and erasing.
/// </summary>
public static class BinkGeometryMath
{
    /// <summary>A small epsilon used to detect degenerate segments.</summary>
    public const float Epsilon = 1e-5f;

    /// <summary>Squared distance from a point to a line segment.</summary>
    public static float DistanceToSegmentSquared(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.LengthSquared();
        if (len2 < Epsilon * Epsilon) return (p - a).LengthSquared();
        float t = Math.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f);
        Vector2 proj = a + ab * t;
        return (p - proj).LengthSquared();
    }

    /// <summary>Distance from a point to a line segment.</summary>
    public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b) => MathF.Sqrt(DistanceToSegmentSquared(p, a, b));

    /// <summary>
    /// Whether a transform stretches every direction by the same factor: any combination of rotation, reflection,
    /// even scaling and translation. Under such a transform a distance converts between local and document units by
    /// one factor; under any other (stretching a selection by an edge handle produces one) it does not.
    /// </summary>
    public static bool IsConformal(Matrix3x2 m)
    {
        float tolerance = 1e-4f * (MathF.Abs(m.M11) + MathF.Abs(m.M12) + MathF.Abs(m.M21) + MathF.Abs(m.M22)) + 1e-9f;
        return (MathF.Abs(m.M11 - m.M22) <= tolerance && MathF.Abs(m.M12 + m.M21) <= tolerance)
            || (MathF.Abs(m.M11 + m.M22) <= tolerance && MathF.Abs(m.M12 - m.M21) <= tolerance);
    }

    /// <summary>The most a transform stretches any direction: its largest singular value.</summary>
    public static float MaxScale(Matrix3x2 m)
    {
        float sum = m.M11 * m.M11 + m.M12 * m.M12 + m.M21 * m.M21 + m.M22 * m.M22;
        float determinant = m.GetDeterminant();
        float spread = MathF.Sqrt(MathF.Max(sum * sum - 4f * determinant * determinant, 0f));
        return MathF.Sqrt((sum + spread) / 2f);
    }

    /// <summary>
    /// How far a band of half-width <paramref name="halfWidth"/> around the local segment <paramref name="a"/> to
    /// <paramref name="b"/> reaches either side of it in document units under <paramref name="m"/>. The band's sides
    /// stay parallel under the transform, at the half-width times the determinant over the segment's stretch: an even
    /// scale multiplies it by the scale, a stretch along the segment leaves it alone, a stretch across it widens it by
    /// the full factor. A degenerate segment is a dot, given its mean radius.
    /// </summary>
    public static float TransformedHalfWidth(Vector2 a, Vector2 b, Matrix3x2 m, float halfWidth)
    {
        if (m.IsIdentity) return halfWidth;
        float determinant = MathF.Abs(m.GetDeterminant());
        Vector2 local = b - a;
        float documentLength = Vector2.TransformNormal(local, m).Length();
        if (documentLength < Epsilon) return halfWidth * MathF.Sqrt(determinant);
        return halfWidth * determinant * local.Length() / documentLength;
    }

    /// <summary>
    /// Whether a document point lies within <paramref name="tolerance"/> of a round-ended band of local half-width
    /// <paramref name="halfWidth"/> around the local segment <paramref name="localA"/> to <paramref name="localB"/>,
    /// drawn under <paramref name="m"/>. Beside the segment the band reaches <see cref="TransformedHalfWidth"/>; at
    /// its ends, the round cap becomes an ellipse, measured along the direction to the point.
    /// </summary>
    internal static bool NearTransformedSegment(Vector2 point, Vector2 localA, Vector2 localB, Matrix3x2 m, Matrix3x2 inverse, float halfWidth, float tolerance)
    {
        Vector2 a = Vector2.Transform(localA, m);
        Vector2 b = Vector2.Transform(localB, m);
        Vector2 ab = b - a;
        float length2 = ab.LengthSquared();
        float t = length2 < Epsilon * Epsilon ? 0f : Vector2.Dot(point - a, ab) / length2;
        if (t > 0f && t < 1f)
        {
            float reach = TransformedHalfWidth(localA, localB, m, halfWidth) + tolerance;
            return (point - (a + ab * t)).LengthSquared() <= reach * reach;
        }
        return WithinTransformedDot(point - (t <= 0f ? a : b), inverse, halfWidth, tolerance);
    }

    /// <summary>
    /// Whether an offset from a dot's centre, in document units, lies within <paramref name="tolerance"/> of a dot of
    /// local radius <paramref name="radius"/> drawn under the transform whose inverse is <paramref name="inverse"/>.
    /// The dot is an ellipse in the document, reaching <c>radius / |inverse · u|</c> along a unit direction u.
    /// </summary>
    internal static bool WithinTransformedDot(Vector2 offset, Matrix3x2 inverse, float radius, float tolerance)
    {
        float distance = offset.Length();
        if (distance <= tolerance) return true;
        float shrink = Vector2.TransformNormal(offset / distance, inverse).Length();
        float reach = shrink > Epsilon ? radius / shrink : radius;
        return distance <= reach + tolerance;
    }

    /// <summary>
    /// Parameter <c>t</c> in [0,1] of the point on segment <paramref name="a"/>-<paramref name="b"/> closest to <paramref name="p"/>.
    /// </summary>
    public static float ProjectOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.LengthSquared();
        if (len2 < Epsilon * Epsilon) return 0f;
        return Math.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f);
    }

    /// <summary>Shortest distance between two segments.</summary>
    public static float SegmentToSegmentDistance(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        if (SegmentsIntersect(a1, a2, b1, b2)) return 0f;
        float d = DistanceToSegmentSquared(a1, b1, b2);
        d = MathF.Min(d, DistanceToSegmentSquared(a2, b1, b2));
        d = MathF.Min(d, DistanceToSegmentSquared(b1, a1, a2));
        d = MathF.Min(d, DistanceToSegmentSquared(b2, a1, a2));
        return MathF.Sqrt(d);
    }

    /// <summary>True when segments a1-a2 and b1-b2 intersect (including touching).</summary>
    public static bool SegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        float d1 = Cross(b2 - b1, a1 - b1);
        float d2 = Cross(b2 - b1, a2 - b1);
        float d3 = Cross(a2 - a1, b1 - a1);
        float d4 = Cross(a2 - a1, b2 - a1);
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) return true;
        if (d1 == 0 && OnSegment(b1, b2, a1)) return true;
        if (d2 == 0 && OnSegment(b1, b2, a2)) return true;
        if (d3 == 0 && OnSegment(a1, a2, b1)) return true;
        if (d4 == 0 && OnSegment(a1, a2, b2)) return true;
        return false;
    }

    private static bool OnSegment(Vector2 a, Vector2 b, Vector2 p)
        => p.X >= MathF.Min(a.X, b.X) - Epsilon && p.X <= MathF.Max(a.X, b.X) + Epsilon
        && p.Y >= MathF.Min(a.Y, b.Y) - Epsilon && p.Y <= MathF.Max(a.Y, b.Y) + Epsilon;

    /// <summary>True when a segment intersects a rectangle (or lies inside it).</summary>
    public static bool SegmentIntersectsRect(Vector2 a, Vector2 b, BinkRect rect)
    {
        if (rect.Contains(a) || rect.Contains(b)) return true;
        var tl = new Vector2(rect.Left, rect.Top);
        var tr = new Vector2(rect.Right, rect.Top);
        var br = new Vector2(rect.Right, rect.Bottom);
        var bl = new Vector2(rect.Left, rect.Bottom);
        return SegmentsIntersect(a, b, tl, tr) || SegmentsIntersect(a, b, tr, br) || SegmentsIntersect(a, b, br, bl) || SegmentsIntersect(a, b, bl, tl);
    }

    /// <summary>2D cross product (z component of the 3D cross product).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>Returns the vector rotated 90 degrees (x,y) -> (-y,x).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Perpendicular(Vector2 v) => new(-v.Y, v.X);

    /// <summary>Rotates a vector by an angle in radians.</summary>
    public static Vector2 Rotate(Vector2 v, float radians)
    {
        float c = MathF.Cos(radians);
        float s = MathF.Sin(radians);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    /// <summary>Rotates a point around a pivot.</summary>
    public static Vector2 RotateAround(Vector2 point, Vector2 pivot, float radians) => pivot + Rotate(point - pivot, radians);

    /// <summary>Returns the normalized vector, or zero when the input is (near) zero.</summary>
    public static Vector2 SafeNormalize(Vector2 v)
    {
        float len = v.Length();
        return len < Epsilon ? Vector2.Zero : v / len;
    }

    /// <summary>Linear interpolation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>Angle of a vector in radians.</summary>
    public static float Angle(Vector2 v) => MathF.Atan2(v.Y, v.X);

    /// <summary>True when point is inside a polygon (nonzero winding rule).</summary>
    public static bool PointInPolygon(Vector2 p, ReadOnlySpan<Vector2> polygon) => WindingNumber(p, polygon) != 0;

    /// <summary>
    /// How many times a polygon, closed implicitly, winds around a point, signed by its direction. Summed over several
    /// polygons, it decides whether the point is inside their combined fill: under the non-zero rule when the sum is
    /// not zero, under the even-odd rule when it is odd.
    /// </summary>
    internal static int WindingNumber(Vector2 p, ReadOnlySpan<Vector2> polygon)
    {
        int winding = 0;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[j];
            Vector2 b = polygon[i];
            if (a.Y <= p.Y)
            {
                if (b.Y > p.Y && Cross(b - a, p - a) > 0) winding++;
            }
            else if (b.Y <= p.Y && Cross(b - a, p - a) < 0)
            {
                winding--;
            }
        }
        return winding;
    }

    /// <summary>Computes the bounding rectangle of a set of points.</summary>
    public static BinkRect Bounds(ReadOnlySpan<Vector2> points)
    {
        if (points.IsEmpty) return BinkRect.Empty;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (Vector2 p in points)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }
        return BinkRect.FromLTRB(minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Ramer-Douglas-Peucker polyline simplification. Returns indices of the retained points (always includes first and last).
    /// </summary>
    /// <param name="xs">X coordinates.</param>
    /// <param name="ys">Y coordinates.</param>
    /// <param name="tolerance">Maximum allowed deviation from the original polyline, in the same units as the coordinates.</param>
    /// <param name="keep">Receives the retained indices in increasing order. Cleared first.</param>
    public static void Simplify(ReadOnlySpan<float> xs, ReadOnlySpan<float> ys, float tolerance, List<int> keep)
    {
        keep.Clear();
        int n = Math.Min(xs.Length, ys.Length);
        if (n == 0) return;
        if (n <= 2 || tolerance <= 0)
        {
            for (int i = 0; i < n; i++) keep.Add(i);
            return;
        }

        float tol2 = tolerance * tolerance;
        bool[] retained = new bool[n];
        retained[0] = true;
        retained[n - 1] = true;

        // Explicit stack instead of recursion so very long strokes cannot overflow the call stack.
        var stack = new Stack<(int Start, int End)>();
        stack.Push((0, n - 1));
        while (stack.Count > 0)
        {
            (int start, int end) = stack.Pop();
            float maxDist = 0f;
            int index = -1;
            var a = new Vector2(xs[start], ys[start]);
            var b = new Vector2(xs[end], ys[end]);
            for (int i = start + 1; i < end; i++)
            {
                float d = DistanceToSegmentSquared(new Vector2(xs[i], ys[i]), a, b);
                if (d > maxDist) { maxDist = d; index = i; }
            }
            if (index >= 0 && maxDist > tol2)
            {
                retained[index] = true;
                stack.Push((start, index));
                stack.Push((index, end));
            }
        }

        for (int i = 0; i < n; i++) if (retained[i]) keep.Add(i);
    }
}
