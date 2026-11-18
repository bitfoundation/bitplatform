namespace Bit.Bmotion;

/// <summary>How a spring's damping shapes its motion.</summary>
public enum BmotionSpringDamping
{
    /// <summary>Damping ratio below 1: it overshoots the target and oscillates around it before it rests.</summary>
    Underdamped,

    /// <summary>
    /// Damping ratio of 1 (within 0.1 %) - a classification of the ratio alone. Started from rest it is the fastest
    /// approach that doesn't overshoot; started with a velocity towards the target (a throw, an interrupted
    /// animation) it can still pass the target once.
    /// </summary>
    CriticallyDamped,

    /// <summary>
    /// Damping ratio above 1: it creeps onto the target, slower than critical damping - without overshoot when it
    /// starts from rest, though a strong enough initial velocity can still carry it past once.
    /// </summary>
    Overdamped,
}
