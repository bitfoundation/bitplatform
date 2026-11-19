using System.Buffers;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A linear gradient between two points.</summary>
public sealed class BmageLinearGradientPaint : IBmagePaint
{
    private readonly Vector2 _start;
    private readonly Vector2 _axis;
    private readonly float _lengthSquared;
    private readonly (float Offset, BmageRgba32 Color)[] _stops;

    public BmageLinearGradientPaint(Vector2 start, Vector2 end, IReadOnlyList<(float Offset, BmageRgba32 Color)> stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Count < 2) throw new ArgumentException("A gradient needs at least two stops.", nameof(stops));
        _start = start;
        _axis = end - start;
        _lengthSquared = _axis.LengthSquared();
        _stops = stops.OrderBy(s => s.Offset).ToArray();
    }

    public BmageRgba32 GetColor(int x, int y)
    {
        if (_lengthSquared <= 0) return _stops[0].Color;
        var t = Vector2.Dot(new Vector2(x + 0.5f, y + 0.5f) - _start, _axis) / _lengthSquared;
        return Sample(_stops, Math.Clamp(t, 0f, 1f));
    }

    internal static BmageRgba32 Sample((float Offset, BmageRgba32 Color)[] stops, float t)
    {
        if (t <= stops[0].Offset) return stops[0].Color;
        if (t >= stops[^1].Offset) return stops[^1].Color;
        for (var i = 1; i < stops.Length; i++)
        {
            if (t > stops[i].Offset) continue;
            var a = stops[i - 1];
            var b = stops[i];
            var span = b.Offset - a.Offset;
            var f = span <= 0 ? 0 : (t - a.Offset) / span;
            return new BmageRgba32(
                (byte)(a.Color.R + (b.Color.R - a.Color.R) * f),
                (byte)(a.Color.G + (b.Color.G - a.Color.G) * f),
                (byte)(a.Color.B + (b.Color.B - a.Color.B) * f),
                (byte)(a.Color.A + (b.Color.A - a.Color.A) * f));
        }
        return stops[^1].Color;
    }
}
