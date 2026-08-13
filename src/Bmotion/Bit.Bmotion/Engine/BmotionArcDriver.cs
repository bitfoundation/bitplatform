namespace Bit.Bmotion;

/// <summary>
/// Drives <c>x</c> and <c>y</c> (and optionally <c>rotate</c>) together along a curve - the engine
/// side of <see cref="BmArc"/>.
/// <para>
/// Every other driver owns a single property, which is exactly why an arc needs its own: two
/// independent drivers interpolating x and y can only ever describe the straight line between the
/// endpoints, no matter how they are eased. This one runs a single 0→1 progress driver and maps
/// that progress onto the curve, so the pair stays on the arc for the whole journey - and so a
/// spring's overshoot travels along the curve rather than past the corner of it.
/// </para>
/// </summary>
internal sealed class BmotionArcDriver : IBmotionAnimationDriver
{
    private readonly BmArcCurve _curve;
    private readonly IBmotionAnimationDriver _progress;
    private readonly Action<double, double, double?> _apply;

    /// <param name="curve">The path to follow, already resolved from the element's current position.</param>
    /// <param name="config">Timing/physics for the journey; drives a 0→1 progress value.</param>
    /// <param name="apply">Receives <c>(x, y, rotateDegrees)</c>; rotation is null when the arc doesn't turn the element.</param>
    public BmotionArcDriver(BmArcCurve curve, BmotionTransitionConfig config, Action<double, double, double?> apply)
    {
        _curve = curve;
        _apply = apply;
        // Progress runs 0→1 and the curve turns it into positions, so the transition's easing,
        // spring physics and repeat behaviour all apply to the journey along the arc. Inertia has
        // no target to run to, so it degrades to the tween it would otherwise fight.
        _progress = config.Type == BmotionTransitionType.Spring
            ? new BmotionSpringDriver(0, 1, config, Emit)
            : new BmotionTweenDriver(0, 1, config, Emit);
    }

    private void Emit(double t)
    {
        var (x, y) = _curve.PointAt(t);
        _apply(x, y, _curve.Rotate > 0 ? _curve.RotationAt(t) : null);
    }

    public bool Tick(double timestamp) => _progress.Tick(timestamp);

    public void Cancel() => _progress.Cancel();

    public void Complete() => _progress.Complete();
}
