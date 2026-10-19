namespace Bit.Bmotion;
/// <summary>
/// Carries one key of an interrupted compositor (WAAPI) animation on along the very curve the
/// browser was playing it on, from the point the browser had reached. A compositor animation owns
/// the whole <c>transform</c> property, so a new animation on <c>y</c> has to cancel one running on
/// <c>x</c>; this driver is what keeps that <c>x</c> going instead of freezing it mid-flight.
/// </summary>
internal sealed class BmotionWaapiContinuationDriver : IBmotionAnimationDriver
{
    private readonly BmotionElementAnimationState.WaapiPlan _plan;
    private readonly string _key;
    private readonly double _playedAtHandoff;
    private readonly Action<double> _apply;
    private double _startTs = -1;
    private bool _cancelled;

    public BmotionWaapiContinuationDriver(
        BmotionElementAnimationState.WaapiPlan plan, string key, double playedAtHandoffMs, Action<double> apply)
    {
        _plan = plan;
        _key = key;
        _playedAtHandoff = playedAtHandoffMs;
        _apply = apply;
    }

    public bool Tick(double timestamp)
    {
        if (_cancelled) return true;
        if (_startTs < 0) _startTs = timestamp;

        // The element's clock already runs at its playback rate, so its time is animation time.
        double played = _playedAtHandoff + (timestamp - _startTs);
        if (played >= _plan.TotalMs)
        {
            Complete();
            return true;
        }
        _apply(_plan.ValueAt(_key, played));
        return false;
    }

    public void Cancel() => _cancelled = true;

    public void Complete()
    {
        // Past the end the plan samples its resting value (the last iteration's end, mirror included).
        if (!double.IsInfinity(_plan.TotalMs)) _apply(_plan.ValueAt(_key, _plan.TotalMs + 1));
    }
}
