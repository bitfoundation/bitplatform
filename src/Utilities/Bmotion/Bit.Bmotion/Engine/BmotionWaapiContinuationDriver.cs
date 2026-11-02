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
    private readonly Action<double>? _apply;
    private readonly Action<string>? _applyString;
    private double _startTs = double.NaN; // NaN = not started (timestamps near clock 0 can be negative)
    private bool _cancelled;

    /// <param name="plan">The interrupted plan.</param>
    /// <param name="key">The key it carries on.</param>
    /// <param name="playedAtHandoffMs">How far the plan had played when it was interrupted.</param>
    /// <param name="apply">Applies a numeric key's value.</param>
    /// <param name="applyString">Applies a string key's value (a colour, a length) instead.</param>
    public BmotionWaapiContinuationDriver(
        BmotionElementAnimationState.WaapiPlan plan, string key, double playedAtHandoffMs,
        Action<double>? apply = null, Action<string>? applyString = null)
    {
        _plan = plan;
        _key = key;
        _playedAtHandoff = playedAtHandoffMs;
        _apply = apply;
        _applyString = applyString;
    }

    public bool Tick(double timestamp)
    {
        if (_cancelled) return true;
        if (double.IsNaN(_startTs)) _startTs = timestamp;

        // The element's clock already runs at its playback rate, so its time is animation time.
        double played = _playedAtHandoff + (timestamp - _startTs);
        if (played >= _plan.TotalMs)
        {
            Complete();
            return true;
        }
        Apply(played);
        return false;
    }

    public void Cancel() => _cancelled = true;

    public void Complete()
    {
        // Past the end the plan samples its resting value (the last iteration's end, mirror included).
        if (!double.IsInfinity(_plan.TotalMs)) Apply(_plan.TotalMs + 1);
    }

    private void Apply(double played)
    {
        if (_applyString is not null) _applyString(_plan.StringAt(_key, played));
        else _apply?.Invoke(_plan.ValueAt(_key, played));
    }
}
