namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/PannerNode/distanceModel">PannerNode.distanceModel</see>:
/// how quickly a sound fades as it gets further from the listener.
/// </summary>
public enum AudioDistanceModel
{
    /// <summary>Fades linearly to silence at the maximum distance. Not physical, but predictable and easy to design around.</summary>
    Linear,

    /// <summary>Fades with the inverse of distance - how sound actually behaves, and the default.</summary>
    Inverse,

    /// <summary>Fades with distance raised to the rolloff factor, for a sharper drop than inverse gives.</summary>
    Exponential
}
