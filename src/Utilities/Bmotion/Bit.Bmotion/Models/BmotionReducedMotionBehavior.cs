namespace Bit.Bmotion;

/// <summary>
/// What one element does when motion is reduced - set on <see cref="Bmotion.ReducedMotion"/> or a
/// <see cref="BmotionPreset.ReducedMotion"/>. Whether motion is reduced at all is the user's and the app's decision
/// (<see cref="BmotionReducedMotionMode"/>, <see cref="BmotionConfig"/>); this says how an element answers it, since a
/// decorative flourish and a progress bar shouldn't answer the same way. Reducing motion is about vestibular comfort:
/// the motion that triggers discomfort is movement, scaling and parallax, not changes of colour or opacity.
/// </summary>
public enum BmotionReducedMotionBehavior
{
    /// <summary>
    /// The default: movement, scaling, rotation and size changes land at once, while opacity, colours, filters and
    /// shadows keep their timing - an entrance becomes a fade, and a shake does nothing.
    /// </summary>
    Reduce = 0,

    /// <summary>
    /// Everything lands at once, fades included - for decorative motion that adds nothing once it is calm (an ambient
    /// loop, a hero crossfade), or content that must simply be there.
    /// </summary>
    Instant = 1,

    /// <summary>
    /// The animation plays in full - for motion that carries information and is calm already: a progress bar
    /// filling, a counter, an element settling where a drag left it. Use it sparingly, and never for motion that
    /// travels far, zooms or spins.
    /// </summary>
    Full = 2,
}
