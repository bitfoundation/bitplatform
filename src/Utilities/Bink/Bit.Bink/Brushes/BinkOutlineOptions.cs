namespace Bit.Bink;

/// <summary>Options for <see cref="BinkStrokeOutliner"/>.</summary>
public readonly record struct BinkOutlineOptions
{
    /// <summary>Maximum stroke width (diameter) in document units.</summary>
    public float Width { get; init; }

    /// <summary>Pressure to width-factor mapping.</summary>
    public BinkPressureCurve PressureCurve { get; init; }

    /// <summary>Taper-in length at the start.</summary>
    public float TaperStart { get; init; }

    /// <summary>Taper-out length at the end.</summary>
    public float TaperEnd { get; init; }

    /// <summary>Whether to draw round caps (false gives flat caps).</summary>
    public bool RoundCaps { get; init; }

    /// <summary>Whether to emit a smooth (quadratic) outline instead of straight polygon edges.</summary>
    public bool Smooth { get; init; }

    /// <summary>Creates default options for a width.</summary>
    public BinkOutlineOptions(float width)
    {
        Width = width;
        PressureCurve = BinkPressureCurve.Linear;
        RoundCaps = true;
        Smooth = true;
    }
}
