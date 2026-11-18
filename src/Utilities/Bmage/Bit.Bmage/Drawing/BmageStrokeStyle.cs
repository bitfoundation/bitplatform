using System.Numerics;

namespace Bit.Bmage;

/// <summary>Stroke appearance: width, caps, joins and an optional dash pattern.</summary>
public sealed record BmageStrokeStyle
{
    /// <summary>A 1px solid stroke with round caps and joins.</summary>
    public static BmageStrokeStyle Default { get; } = new();

    /// <summary>Stroke width in pixels. Must be positive.</summary>
    public float Width { get; init; } = 1f;

    public BmageLineCap Cap { get; init; } = BmageLineCap.Round;

    public BmageLineJoin Join { get; init; } = BmageLineJoin.Round;

    /// <summary>Maximum ratio of miter length to stroke width before falling back to a bevel.</summary>
    public float MiterLimit { get; init; } = 4f;

    /// <summary>
    /// Dash lengths in pixels, alternating on and off. Null or empty means a solid line. Values are used cyclically,
    /// so a single value produces equal dashes and gaps.
    /// </summary>
    public IReadOnlyList<float>? DashPattern { get; init; }

    /// <summary>Distance into the dash pattern at which to start.</summary>
    public float DashOffset { get; init; }

    /// <summary>Creates a dashed variant of this style.</summary>
    public BmageStrokeStyle WithDashes(params float[] pattern) => this with { DashPattern = pattern };

    /// <summary>Returns a style scaled for a proxy image.</summary>
    public BmageStrokeStyle ForScale(double scale)
    {
        if (scale == 1) return this;
        return this with
        {
            Width = (float)Math.Max(0.1, Width * scale),
            DashPattern = DashPattern?.Select(d => (float)(d * scale)).ToArray(),
            DashOffset = (float)(DashOffset * scale),
        };
    }
}
