namespace Bit.Bmotion;

/// <summary>The axis a scroll-driven timeline reads its progress from.</summary>
public enum BmScrollAxis
{
    /// <summary>The block (usually vertical) axis.</summary>
    Y,

    /// <summary>The inline (usually horizontal) axis.</summary>
    X,
}

/// <summary>
/// Binds an element's animation to <b>scroll position instead of time</b>, driven by the browser's
/// native <c>ScrollTimeline</c> / <c>ViewTimeline</c> where available - so the whole animation runs
/// on the compositor with no scroll handler, no per-frame interop and no .NET round trip.
/// <code>
/// @* a reading-progress bar *@
/// &lt;Bmotion Timeline="BmScrollTimeline.Page()" Animate="Bm.To(scaleX: [0, 1])"&gt;
///     &lt;div class="progress-bar" style="transform-origin:0 50%;" /&gt;
/// &lt;/Bmotion&gt;
///
/// @* an element animating across its own journey through the viewport *@
/// &lt;Bmotion Timeline="BmScrollTimeline.View()" Animate="Bm.To(opacity: [0, 1, 1, 0], y: [40, 0, 0, -40])"&gt;
///     &lt;div class="card" /&gt;
/// &lt;/Bmotion&gt;
/// </code>
/// <para>
/// On browsers without native scroll timelines the bridge falls back to scrubbing the same Web
/// Animation from a passive scroll listener - still no .NET interop per frame, and still the
/// browser interpolating the values. Custom <see cref="Range"/> strings need the native API and are
/// ignored by the fallback, which always covers the element's full journey.
/// </para>
/// <para>
/// A timeline-driven animation <b>owns the properties it animates</b> for as long as it is
/// attached: they are driven by the browser from the scroll position, so a gesture or a second
/// animation targeting the same properties will fight it. Animate different properties, or drop
/// the timeline.
/// </para>
/// </summary>
public sealed class BmScrollTimeline
{
    private BmScrollTimeline() { }

    /// <summary><c>true</c> for a <c>ViewTimeline</c> (an element's journey through the scrollport).</summary>
    public bool IsView { get; private init; }

    /// <summary>
    /// CSS selector of the scroll container (for a scroll timeline) or of the tracked subject
    /// (for a view timeline). <c>null</c> means the document scroller / the animated element itself.
    /// </summary>
    public string? Selector { get; private init; }

    /// <summary>The axis progress is read from.</summary>
    public BmScrollAxis Axis { get; private init; }

    /// <summary>
    /// Optional native range, in CSS <c>animation-range</c> syntax - e.g. <c>"entry 0% cover 50%"</c>
    /// or <c>"cover 25% cover 75%"</c>. Applies to view timelines; needs native support.
    /// </summary>
    public string? Range { get; private init; }

    /// <summary>Progress across the whole document scroll - the reading-progress-bar timeline.</summary>
    public static BmScrollTimeline Page(BmScrollAxis axis = BmScrollAxis.Y)
        => new() { Axis = axis };

    /// <summary>Progress across the scroll of the element matching <paramref name="selector"/>.</summary>
    public static BmScrollTimeline Container(string selector, BmScrollAxis axis = BmScrollAxis.Y)
    {
        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentException("Selector must not be null or whitespace.", nameof(selector));
        return new() { Selector = selector, Axis = axis };
    }

    /// <summary>
    /// Progress of the <b>animated element itself</b> through the scrollport: 0 as it enters, 1 as
    /// it leaves. The scroll-linked reveal, without a scroll handler.
    /// </summary>
    /// <param name="axis">The axis progress is read from.</param>
    /// <param name="range">Optional native <c>animation-range</c>, e.g. <c>"entry 0% cover 50%"</c>.</param>
    public static BmScrollTimeline View(BmScrollAxis axis = BmScrollAxis.Y, string? range = null)
        => new() { IsView = true, Axis = axis, Range = range };

    /// <summary>
    /// Progress of the element matching <paramref name="selector"/> through the scrollport, driving
    /// the animation on <em>this</em> element - the "animate the header as the hero scrolls past" idiom.
    /// </summary>
    public static BmScrollTimeline ViewOf(string selector, BmScrollAxis axis = BmScrollAxis.Y, string? range = null)
    {
        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentException("Selector must not be null or whitespace.", nameof(selector));
        return new() { IsView = true, Selector = selector, Axis = axis, Range = range };
    }

    /// <summary>Lowers this spec into the flat object the JS bridge consumes.</summary>
    internal Dictionary<string, object?> ToJsObject() => new()
    {
        ["view"] = IsView,
        ["selector"] = Selector,
        // The bridge speaks the CSS axis names so it can hand them straight to the timeline
        // constructors without a second translation table.
        ["axis"] = Axis == BmScrollAxis.X ? "inline" : "block",
        ["range"] = Range,
    };

    /// <summary>
    /// Structural comparison, so a spec recreated inline on every render
    /// (<c>Timeline="BmScrollTimeline.Page()"</c>) doesn't read as a parameter change.
    /// </summary>
    internal static bool AreEquivalent(BmScrollTimeline? a, BmScrollTimeline? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.IsView == b.IsView
            && a.Axis == b.Axis
            && string.Equals(a.Selector, b.Selector, StringComparison.Ordinal)
            && string.Equals(a.Range, b.Range, StringComparison.Ordinal);
    }
}
