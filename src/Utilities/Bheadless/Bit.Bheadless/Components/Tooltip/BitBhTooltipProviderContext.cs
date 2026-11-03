namespace Bit.Bheadless;

/// <summary>
/// Cascading context provided by <see cref="BitBhTooltipProvider"/> to coordinate
/// timing across all enclosed <see cref="BitBhTooltip"/> instances.
///
/// <para>
/// The provider tracks which tooltips are open and when the last one closed, so that
/// a subsequent tooltip can open without delay while another is still showing (its
/// close waits a short grace period) or if the user is moving quickly between
/// triggers — matching Radix UI's <c>skipDelayDuration</c> behaviour.
/// </para>
/// </summary>
public sealed class BitBhTooltipProviderContext
{
    /// <summary>
    /// Default open delay (in milliseconds) before a tooltip becomes visible
    /// after the trigger is hovered or focused.
    /// </summary>
    public int DelayDuration { get; internal set; } = 700;

    /// <summary>
    /// How quickly (in milliseconds) the user has to move between triggers for
    /// the next tooltip to skip its open delay and appear instantly.
    /// </summary>
    public int SkipDelayDuration { get; internal set; } = 300;

    /// <summary>
    /// When <c>true</c>, the tooltip content cannot be hovered without closing.
    /// Defaults to <c>false</c>: pointer can move from trigger into content
    /// while the tooltip stays open.
    /// </summary>
    public bool DisableHoverableContent { get; internal set; }

    private DateTime? _lastClosedAt;
    private readonly HashSet<BitBhTooltip> _open = new();

    internal void NotifyOpened(BitBhTooltip tooltip) => _open.Add(tooltip);

    internal void NotifyDisposed(BitBhTooltip tooltip) => _open.Remove(tooltip);

    internal void NotifyClosed(BitBhTooltip tooltip)
    {
        _open.Remove(tooltip);
        _lastClosedAt = DateTime.UtcNow;
    }

    /// <summary>Whether a tooltip opening now skips its delay: another one is open, or closed a moment ago.</summary>
    internal bool SkipsDelay() => _open.Count > 0 || IsRecentlyClosed();

    internal bool IsRecentlyClosed()
    {
        if (_lastClosedAt is null) return false;
        return (DateTime.UtcNow - _lastClosedAt.Value).TotalMilliseconds < SkipDelayDuration;
    }
}
