using System.Numerics;

namespace Bit.Bmage;

/// <summary>One stroke of a mask brush.</summary>
public sealed record BmageBrushStroke
{
    private readonly Vector2[] _points = [];

    /// <param name="points">The path the brush followed, in the mask's units.</param>
    /// <param name="width">The brush diameter, in the mask's units.</param>
    public BmageBrushStroke(IReadOnlyList<Vector2> points, float width)
    {
        Points = points;
        Width = width;
    }

    /// <summary>The path the brush followed. Drawn smoothed, as freehand annotations are.</summary>
    public IReadOnlyList<Vector2> Points
    {
        get => _points;
        init => _points = value?.ToArray() ?? throw new ArgumentNullException(nameof(Points));
    }

    /// <summary>The brush diameter.</summary>
    public float Width
    {
        get;
        init => field = value > 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Width), "A brush needs a positive width.");
    }

    /// <summary>How much coverage the stroke adds (or removes, when erasing), in [0,1].</summary>
    public float Opacity
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 1f;

    /// <summary>Removes coverage instead of adding it, like painting a mask with black.</summary>
    public bool Erase { get; init; }

    internal BmageBrushStroke Scale(double scale) => this with { Points = [.. _points.Select(p => p * (float)scale)], Width = (float)(Width * scale) };
}
