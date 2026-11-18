using System.Numerics;

namespace Bit.Bink;

/// <summary>Visual style shared by shape annotations.</summary>
public sealed record BinkShapeStyle
{
    /// <summary>A 2-unit black outline with no fill.</summary>
    public static readonly BinkShapeStyle Default = new();

    /// <summary>Outline color.</summary>
    public BinkColor StrokeColor { get; init; } = BinkColor.Black;

    /// <summary>Outline width in local units; 0 disables the outline.</summary>
    public float StrokeWidth { get; init; } = 2f;

    /// <summary>Fill color, or null for no fill.</summary>
    public BinkColor? FillColor { get; init; }

    /// <summary>Opacity 0..1 applied to the whole shape.</summary>
    public float Opacity { get; init; } = 1f;

    /// <summary>Dash pattern (on, off, ...) in local units, or null for a solid line.</summary>
    public float[]? Dash { get; init; }

    /// <summary>Blend mode.</summary>
    public BinkBlendMode BlendMode { get; init; } = BinkBlendMode.Normal;

    /// <summary>How the ends of open outlines are drawn. Round by default, which is what hand-drawn annotation looks like.</summary>
    public BinkLineCap LineCap { get; init; } = BinkLineCap.Round;

    /// <summary>How corners are drawn. Round by default; <see cref="BinkLineJoin.Miter"/> keeps a rectangle's corners sharp.</summary>
    public BinkLineJoin LineJoin { get; init; } = BinkLineJoin.Round;

    /// <summary>Paint for the outline.</summary>
    public BinkStrokePaint ToStrokePaint() => new(StrokeColor, StrokeWidth, Opacity, BlendMode, LineCap, LineJoin, Dash);

    /// <summary>Paint for the fill.</summary>
    public BinkFillPaint ToFillPaint() => new(FillColor ?? BinkColor.Transparent, Opacity, BlendMode);

    /// <inheritdoc />
    public bool Equals(BinkShapeStyle? other)
        => other is not null && StrokeColor == other.StrokeColor && StrokeWidth == other.StrokeWidth && FillColor == other.FillColor
           && Opacity == other.Opacity && BlendMode == other.BlendMode && LineCap == other.LineCap && LineJoin == other.LineJoin
           && (Dash ?? []).AsSpan().SequenceEqual(other.Dash ?? []);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(StrokeColor, StrokeWidth, FillColor, Opacity, BlendMode, LineCap, LineJoin);
}
