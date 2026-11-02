namespace Bit.Bheadless;

/// <summary>
/// Cascading context provided by <see cref="BitBhTooltipProvider"/> to coordinate
/// timing across all enclosed <see cref="BitBhTooltip"/> instances.
///
/// <para>
/// The provider tracks when the last tooltip closed so that a subsequent
/// tooltip can open without delay if the user is moving quickly between
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

    internal void NotifyClosed()
    {
        _lastClosedAt = DateTime.UtcNow;
    }

    internal bool IsRecentlyClosed()
    {
        if (_lastClosedAt is null) return false;
        return (DateTime.UtcNow - _lastClosedAt.Value).TotalMilliseconds < SkipDelayDuration;
    }
}
