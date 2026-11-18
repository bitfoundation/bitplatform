namespace Bit.Bink;

/// <summary>Shape of the mapping from input pressure to width.</summary>
public enum BinkPressureCurveKind : byte
{
    /// <summary>Width grows linearly with pressure.</summary>
    Linear = 0,
    /// <summary>Slow start, fast finish (light touches stay thin).</summary>
    EaseIn = 1,
    /// <summary>Fast start, slow finish (light touches already draw fairly wide).</summary>
    EaseOut = 2,
    /// <summary>Smooth S-curve.</summary>
    EaseInOut = 3,
    /// <summary>Ignores pressure: always the maximum width.</summary>
    Constant = 4,
    /// <summary>Custom lookup table (see <see cref="BinkPressureCurve.Samples"/>).</summary>
    Custom = 5,
}
