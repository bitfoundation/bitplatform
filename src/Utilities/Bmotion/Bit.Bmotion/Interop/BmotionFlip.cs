namespace Bit.Bmotion;

/// <summary>
/// The motion of a FLIP layout animation (see <see cref="IBmotionInterop.PlayWaapiFlipAsync"/> and
/// <see cref="IBmotionInterop.PlayLayoutCrossfadeAsync"/>): the offset and scale the element plays back from, about which
/// anchor, for how long and along which curve.
/// </summary>
/// <remarks>
/// One object rather than a parameter each: an interface method with more than eight parameters needs a bridge built
/// for its exact signature when an app compiled with profile-guided AOT reaches it on the interpreter, and none exists.
/// </remarks>
public sealed class BmotionFlip
{
    /// <summary>The horizontal offset the element starts from, in CSS pixels.</summary>
    public double Dx { get; init; }

    /// <summary>The vertical offset the element starts from, in CSS pixels.</summary>
    public double Dy { get; init; }

    /// <summary>The horizontal scale the element starts from; 1 is its own width.</summary>
    public double Sx { get; init; } = 1;

    /// <summary>The vertical scale the element starts from; 1 is its own height.</summary>
    public double Sy { get; init; } = 1;

    /// <summary>How long the animation plays, in milliseconds.</summary>
    public double DurationMs { get; init; }

    /// <summary>The CSS easing the animation plays along.</summary>
    public string Easing { get; init; } = "ease";

    /// <summary>The horizontal projection anchor, 0-1 of the element's box, the offset was measured from.</summary>
    public double OriginX { get; init; }

    /// <summary>The vertical projection anchor, 0-1 of the element's box, the offset was measured from.</summary>
    public double OriginY { get; init; }
}
