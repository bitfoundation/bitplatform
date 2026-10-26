using System.Numerics;

namespace Bit.BbScanner;

/// <summary>
/// Where a part of a symbol started on the lines it was read on, measured from where it started on the first, summed
/// for a fit of the line through them. Wherever a line crosses a part of a linear symbol, at whatever angle, it starts
/// on the part's leading edge, which runs square to the symbol's own axis: so the starts of a few lines trace that edge,
/// and the edge gives the axis - which the lines themselves, run off it by up to as far as they can still cross the
/// part whole, do not.
/// </summary>
internal readonly record struct BbEdgeFit(int Count, float X, float Y, float XX, float XY, float YY)
{
    /// <summary>
    /// How far apart along an edge, in squared modules summed over the starts, the starts an axis is fitted to have to
    /// lie: a start read a third of a module out then turns the axis three degrees at most.
    /// </summary>
    private const float MinSpread = 40;

    /// <summary>How far off the fitted edge, in squared modules on average, the starts may lie.</summary>
    private const float MaxScatter = 0.36f;

    /// <summary>A fit of the first start alone, the origin.</summary>
    public static BbEdgeFit First => default(BbEdgeFit).Add(Vector2.Zero);

    /// <summary>The fit with one more start, measured from the first.</summary>
    public BbEdgeFit Add(Vector2 point) =>
        new(Count + 1, X + point.X, Y + point.Y, XX + (point.X * point.X), XY + (point.X * point.Y), YY + (point.Y * point.Y));

    /// <summary>
    /// The symbol's own axis, pointing the way <paramref name="reading"/> runs, from the scatter of these starts about
    /// their edge and those of <paramref name="other"/>, a parallel edge of the same symbol; <see langword="null"/> until
    /// the starts lie far enough apart along an edge to give its angle within a few degrees, or when they do not lie
    /// along one.
    /// </summary>
    public Vector2? Axis(in BbEdgeFit other, float module, Vector2 reading)
    {
        var xx = CentredXX + other.CentredXX;
        var xy = CentredXY + other.CentredXY;
        var yy = CentredYY + other.CentredYY;
        var count = Count + other.Count;
        var mean = (xx + yy) / 2;
        var root = MathF.Sqrt((((xx - yy) / 2) * ((xx - yy) / 2)) + (xy * xy));
        if (mean + root < MinSpread * module * module || mean - root > count * MaxScatter * module * module)
        {
            return null;
        }

        var edge = MathF.Atan2(2 * xy, xx - yy) / 2;
        var axis = new Vector2(-MathF.Sin(edge), MathF.Cos(edge));
        return Vector2.Dot(axis, reading) < 0 ? -axis : axis;
    }

    private float CentredXX => Count == 0 ? 0 : XX - (X * X / Count);

    private float CentredXY => Count == 0 ? 0 : XY - (X * Y / Count);

    private float CentredYY => Count == 0 ? 0 : YY - (Y * Y / Count);
}
