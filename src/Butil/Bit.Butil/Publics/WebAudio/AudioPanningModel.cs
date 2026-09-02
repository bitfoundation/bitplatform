namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/PannerNode/panningModel">PannerNode.panningModel</see>:
/// how a position in space is turned into what the two ears hear.
/// </summary>
public enum AudioPanningModel
{
    /// <summary>
    /// Cheap left/right balance with a distance-based volume. Fine for a 2D scene, and it is what a
    /// panner uses unless told otherwise.
    /// </summary>
    EqualPower,

    /// <summary>
    /// Convolves with a head-related transfer function, so sounds are placed above, behind and around
    /// the listener rather than merely left or right. Convincing on headphones and noticeably more
    /// expensive - the model a 3D scene or a VR session wants.
    /// </summary>
    Hrtf
}
