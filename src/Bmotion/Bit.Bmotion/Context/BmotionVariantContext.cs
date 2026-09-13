
namespace Bit.Bmotion;
/// <summary>
/// Cascaded by a parent Bmotion component to propagate the active variant name,
/// shared variants dictionary, and stagger configuration to descendant Bmotion components.
/// </summary>
internal class BmotionVariantContext
{
    // Children claim their slot during their own OnInitialized - i.e. inside the render pass, which
    // completes before any component's OnAfterRenderAsync runs. That ordering is what makes
    // TotalChildren trustworthy by the time a delay is computed, which in turn is what lets a
    // stagger radiate from the last or centre element (both need to know how many there are) and
    // lets an afterChildren parent wait for the real cascade.
    private readonly List<Func<double>> _childDurations = new();

    /// <summary>The currently active variant name selected by the nearest ancestor.</summary>
    public string? ActiveVariant { get; internal set; }

    /// <summary>The initial variant name provided by the nearest ancestor.</summary>
    public string? InitialVariant { get; internal set; }

    /// <summary>Shared variants dictionary from the nearest ancestor that defined variants.</summary>
    public BmVariants? Variants { get; internal set; }

    /// <summary>Seconds to stagger each child's animation start.</summary>
    public double StaggerChildren { get; internal set; }

    /// <summary>Seconds to delay the first child's animation start.</summary>
    public double DelayChildren { get; internal set; }

    /// <summary>
    /// Full stagger generator for the children, when the parent supplied one. It wins over the flat
    /// <see cref="StaggerChildren"/> interval and is what enables <c>from</c> origins and grids.
    /// </summary>
    public BmStagger? ChildStagger { get; internal set; }

    // A child that reports no duration contributes only its stagger delay to the cascade's length.
    private static readonly Func<double> _noDuration = static () => 0;

    /// <summary>
    /// Called by a child Bmotion component once, during its own initialisation, to claim a stable
    /// position in the stagger sequence. Returns the child's index.
    /// </summary>
    /// <param name="estimatedDuration">
    /// Reports how long this child's animation takes, evaluated lazily so it reflects the child's
    /// resolved variant rather than whatever was configured at registration time. Used only by
    /// <see cref="MaxChildFinishSeconds"/>.
    /// </param>
    internal int RegisterChild(Func<double>? estimatedDuration = null)
    {
        _childDurations.Add(estimatedDuration ?? _noDuration);
        return _childDurations.Count - 1;
    }

    /// <summary>How many children have claimed a stagger slot - the stagger's <c>total</c>.</summary>
    internal int TotalChildren => _childDurations.Count;

    /// <summary>The next child index that would be handed out. Used to carry the counter across
    /// context instances so children registering after a variant change keep stable indices.</summary>
    internal int NextChildIndex => _childDurations.Count;

    /// <summary>
    /// Adopts <paramref name="previous"/>'s registered children when a fresh context instance
    /// replaces a prior one (which happens on every variant switch, because descendants are only
    /// re-notified by a changed reference). Children don't re-register, so without this the new
    /// context would believe it had none - collapsing every stagger delay to zero and making an
    /// afterChildren parent think there was nothing to wait for.
    /// </summary>
    internal void SeedFrom(BmotionVariantContext previous)
    {
        _childDurations.Clear();
        _childDurations.AddRange(previous._childDurations);
    }

    /// <summary>Returns the stagger delay in seconds for a child at the given index.</summary>
    public double GetChildDelay(int childIndex)
        => ChildStagger is { } stagger
            // BmStagger carries its own start delay; DelayChildren adds on top of it.
            ? DelayChildren + stagger.DelayFor(childIndex, Math.Max(TotalChildren, childIndex + 1))
            : DelayChildren + childIndex * StaggerChildren;

    /// <summary>
    /// When the whole cascade will have finished, in seconds from the moment it starts: the latest
    /// (child delay + that child's own animation duration) across every registered child. This is
    /// what a <see cref="BmWhen.AfterChildren"/> parent waits for.
    /// </summary>
    internal double MaxChildFinishSeconds()
    {
        double max = 0;
        for (int i = 0; i < _childDurations.Count; i++)
        {
            double duration;
            // A child's duration provider resolves its variant; a throw there must not take out the
            // parent's animation, so fall back to contributing just the child's delay.
            try { duration = _childDurations[i](); }
            catch { duration = 0; }
            if (!double.IsFinite(duration) || duration < 0) duration = 0;
            double finish = GetChildDelay(i) + duration;
            if (finish > max) max = finish;
        }
        return max;
    }
}
