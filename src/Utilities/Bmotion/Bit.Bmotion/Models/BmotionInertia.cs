namespace Bit.Bmotion;

/// <summary>Velocity-based deceleration that coasts to a stop (e.g. momentum after a drag).</summary>
public sealed class BmotionInertia : BmotionTransition
{
    /// <summary>Velocity at the start of deceleration. Default: 0.</summary>
    public double Velocity { get; set; }

    /// <summary>Exponential decay time constant in milliseconds. Default: 700.</summary>
    public double TimeConstant { get; set; } = 700;

    /// <summary>Multiplier for the projected coast distance. Default: 0.8.</summary>
    public double Power { get; set; } = 0.8;

    /// <summary>Minimum distance from target that counts as at rest. Default: 0.5.</summary>
    public double RestDelta { get; set; } = 0.5;

    /// <summary>Optional lower bound for the coast target.</summary>
    public double? Min { get; set; }

    /// <summary>Optional upper bound for the coast target.</summary>
    public double? Max { get; set; }

    /// <summary>
    /// Stiffness of the spring that takes over when the coast reaches <see cref="Min"/>/<see cref="Max"/>: the
    /// element keeps the speed it was thrown with until it gets there, then bounces off the bound and settles on it
    /// (motion.dev's <c>bounceStiffness</c>). Higher is a harder wall. <c>null</c> (the default) is 500 - or, for a
    /// drag's momentum (<c>DragTransition</c>), the spring a drag released out of bounds snaps back with.
    /// </summary>
    public double? BounceStiffness { get; set; }

    /// <summary>
    /// Damping of the bounce spring at <see cref="Min"/>/<see cref="Max"/> (motion.dev's <c>bounceDamping</c>).
    /// Higher bounces less: from about 2·√<see cref="BounceStiffness"/> it doesn't bounce at all. <c>null</c> (the
    /// default) is 10 - or, for a drag's momentum, the drag's snap-back spring.
    /// </summary>
    public double? BounceDamping { get; set; }

    /// <summary>
    /// Adjusts where the coast comes to rest - motion.dev's <c>modifyTarget</c>. The projected
    /// resting position is passed in and the returned value is used instead, which is how a fling
    /// snaps to a grid, a page or a set of stops:
    /// <code>
    /// // release anywhere, settle on the nearest 100px stop
    /// Bm.Inertia(modifyTarget: v =&gt; Math.Round(v / 100) * 100)
    /// </code>
    /// Runs before <see cref="Min"/>/<see cref="Max"/> clamping, so a snapped target still respects
    /// the bounds. A non-finite return value is ignored.
    /// </summary>
    public Func<double, double>? ModifyTarget { get; set; }

    private protected override BmotionTransitionConfig CreateConfig() => new()
    {
        Type = BmotionTransitionType.Inertia,
        InertiaVelocity = Velocity,
        TimeConstant = TimeConstant,
        Power = Power,
        InertiaRestDelta = RestDelta,
        InertiaMin = Min,
        InertiaMax = Max,
        BounceStiffness = BounceStiffness,
        BounceDamping = BounceDamping,
        ModifyTarget = ModifyTarget,
    };

    private protected override BmotionTransition CloneTransition()
    {
        var copy = new BmotionInertia
        {
            Velocity = Velocity,
            TimeConstant = TimeConstant,
            Power = Power,
            RestDelta = RestDelta,
            Min = Min,
            Max = Max,
            BounceStiffness = BounceStiffness,
            BounceDamping = BounceDamping,
            ModifyTarget = ModifyTarget,
        };
        CopyBaseTo(copy);
        return copy;
    }

    internal override bool ValueEquals(BmotionTransition other)
        => other is BmotionInertia i
           && Velocity == i.Velocity && TimeConstant == i.TimeConstant && Power == i.Power
           && RestDelta == i.RestDelta && Min == i.Min && Max == i.Max
           && BounceStiffness == i.BounceStiffness && BounceDamping == i.BounceDamping
           // Like OnUpdate, an inline lambda is a fresh delegate every render; comparing it by
           // reference would report a transition change on every re-render. Compare by target
           // method so a stable named method still reads as unchanged.
           && Equals(ModifyTarget?.Method, i.ModifyTarget?.Method)
           && base.ValueEquals(other);
}
