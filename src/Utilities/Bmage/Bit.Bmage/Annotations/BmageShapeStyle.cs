namespace Bit.Bmage;

/// <summary>Shared styling for shape annotations.</summary>
public sealed record BmageShapeStyle
{
    public static BmageShapeStyle Default { get; } = new();

    /// <summary>Outline colour, or null for no outline.</summary>
    public BmageRgba32? StrokeColor { get; init; } = new BmageRgba32(230, 40, 40);

    /// <summary>Interior colour, or null for no fill.</summary>
    public BmageRgba32? FillColor { get; init; }

    /// <summary>Outline width and dashes.</summary>
    public BmageStrokeStyle Stroke { get; init; } = new() { Width = 3f };

    /// <summary>Returns a style scaled for a proxy image.</summary>
    public BmageShapeStyle ForScale(double scale) => scale == 1 ? this : this with { Stroke = Stroke.ForScale(scale) };
}
