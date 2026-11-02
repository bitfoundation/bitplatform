namespace Bit.Bmotion;

/// <summary>
/// Tracks the last known bounding rect of every <c>LayoutId</c> so a newly mounted element can
/// FLIP from where its predecessor was - shared-element transitions (motion.dev's
/// <c>layoutId</c>) - and which mounted element holds each id, so a new holder can crossfade with
/// one that is still on its way out. One instance per DI scope, shared by all Bmotion components.
/// </summary>
internal sealed class BmotionLayoutRegistry
{
    private readonly Dictionary<string, BmotionBoundingRect> _rects = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Bmotion> _holders = new(StringComparer.Ordinal);

    /// <summary>Records the rect an element with this layout id currently occupies.</summary>
    internal void Record(string layoutId, BmotionBoundingRect rect) => _rects[layoutId] = rect;

    /// <summary>The last recorded rect for a layout id, or null.</summary>
    internal BmotionBoundingRect? Get(string layoutId) => _rects.GetValueOrDefault(layoutId);

    /// <summary>The mounted element that holds a layout id (the last to mount with it), or null.</summary>
    internal Bmotion? HolderOf(string layoutId) => _holders.GetValueOrDefault(layoutId);

    /// <summary>Makes <paramref name="holder"/> the element holding <paramref name="layoutId"/>.</summary>
    internal void Hold(string layoutId, Bmotion holder) => _holders[layoutId] = holder;

    /// <summary>Forgets <paramref name="holder"/> as a layout id's holder - unless a newer one has taken it over.</summary>
    internal void Release(string layoutId, Bmotion holder)
    {
        if (_holders.TryGetValue(layoutId, out var current) && ReferenceEquals(current, holder))
            _holders.Remove(layoutId);
    }
}
