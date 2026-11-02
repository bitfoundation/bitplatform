namespace Bit.Bmotion;

/// <summary>
/// The rejections CSS safe mode's Warn has reported for one <see cref="Bmotion"/> element, or for one
/// <see cref="BmotionAnimateService"/>: a target is checked every time it is resolved - several times a render - and a
/// rejected declaration is worth one warning, not one per check.
/// <para>
/// It remembers at most <see cref="Max"/> of them, by hash rather than whole (a value can be megabytes long). Past that
/// it says once that it stops reporting and then stays quiet: starting over instead would report a target carrying more
/// rejected declarations than it remembers all over again on every check, and a value bound to input can be a new
/// rejected one on every keystroke.
/// </para>
/// </summary>
internal sealed class BmotionCssWarnings
{
    /// <summary>The most rejections reported.</summary>
    internal const int Max = 64;

    private readonly HashSet<int> _reported = [];
    private bool _full;

    /// <summary>
    /// Whether to report this rejection: true the first time it is seen, false once it has been reported, or once
    /// <see cref="Max"/> have been. <paramref name="stopping"/> is true only for the first rejection that finds the
    /// record full - the time to say that reporting stops.
    /// </summary>
    internal bool ShouldReport(string? target, string prop, string? value, out bool stopping)
    {
        stopping = false;
        int key = HashCode.Combine(target, prop, value);
        lock (_reported)
        {
            if (_full || _reported.Contains(key)) return false;
            if (_reported.Count == Max)
            {
                _full = stopping = true;
                return false;
            }
            _reported.Add(key);
            return true;
        }
    }
}
