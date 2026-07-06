namespace Bit.Bmotion;

/// <summary>The animation driver (physics or interpolation model) used for a transition.</summary>
internal enum BmotionTransitionType
{
    /// <summary>Duration- and easing-based interpolation between start and end values.</summary>
    Tween,
    /// <summary>Physics-based spring driven by stiffness, damping and mass.</summary>
    Spring,
    /// <summary>Velocity-based deceleration that coasts to a stop (e.g. momentum after a drag).</summary>
    Inertia,
    /// <summary>Interpolation across an array of keyframe values rather than a single target.</summary>
    Keyframes,
}
