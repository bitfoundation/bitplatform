using System.Buffers;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A radial gradient from a centre point out to a radius.</summary>
public sealed class BmageRadialGradientPaint : IBmagePaint
{
    private readonly Vector2 _center;
    private readonly float _radius;
    private readonly (float Offset, BmageRgba32 Color)[] _stops;

    public BmageRadialGradientPaint(Vector2 center, float radius, IReadOnlyList<(float Offset, BmageRgba32 Color)> stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Count < 2) throw new ArgumentException("A gradient needs at least two stops.", nameof(stops));
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        _center = center;
        _radius = radius;
        _stops = stops.OrderBy(s => s.Offset).ToArray();
    }

    public BmageRgba32 GetColor(int x, int y)
    {
        var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), _center) / _radius;
        return BmageLinearGradientPaint.Sample(_stops, Math.Clamp(d, 0f, 1f));
    }
}
