namespace Bit.Bmotion;

/// <summary>
/// Tracks the last known bounding rect of every <c>LayoutId</c> so a newly mounted element can
/// FLIP from where its predecessor was - shared-element transitions (motion.dev's
/// <c>layoutId</c>). One instance per DI scope, shared by all Bmotion components.
/// </summary>
internal sealed class BmotionLayoutRegistry
{
    private readonly Dictionary<string, BmotionBoundingRect> _rects = new(StringComparer.Ordinal);

    /// <summary>Records the rect an element with this layout id currently occupies.</summary>
    internal void Record(string layoutId, BmotionBoundingRect rect) => _rects[layoutId] = rect;

    /// <summary>The last recorded rect for a layout id, or null.</summary>
    internal BmotionBoundingRect? Get(string layoutId) => _rects.GetValueOrDefault(layoutId);
}
