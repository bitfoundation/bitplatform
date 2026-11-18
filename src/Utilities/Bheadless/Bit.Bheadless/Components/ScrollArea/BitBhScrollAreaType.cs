namespace Bit.Bheadless;

/// <summary>When the scrollbars of a <see cref="BitBhScrollArea"/> are shown.</summary>
public enum BitBhScrollAreaType
{
    /// <summary>
    /// While the pointer is over the area or the content scrolls, when the content overflows on the scrollbar's
    /// axis; hidden <see cref="BitBhScrollArea.ScrollHideDelay"/> after the pointer leaves. The default.
    /// </summary>
    Hover,

    /// <summary>
    /// While the content scrolls (and while the pointer is on a scrollbar or drags its thumb); hidden
    /// <see cref="BitBhScrollArea.ScrollHideDelay"/> after scrolling stops.
    /// </summary>
    Scroll,

    /// <summary>Whenever the content overflows on the scrollbar's axis.</summary>
    Auto,

    /// <summary>Always, whether the content overflows or not.</summary>
    Always,
}
