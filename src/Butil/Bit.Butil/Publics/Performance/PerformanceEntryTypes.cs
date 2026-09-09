namespace Bit.Butil;

/// <summary>
/// The <c>entryType</c> strings the timeline uses, so subscribing to one is a compile-time name
/// rather than a string that fails silently when it is misspelled - an unknown entry type is not an
/// error to <c>PerformanceObserver</c>, it simply never reports.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceEntry/entryType">https://developer.mozilla.org/en-US/docs/Web/API/PerformanceEntry/entryType</see>
/// </summary>
public static class PerformanceEntryTypes
{
    /// <summary>The document's own load - <see cref="PerformanceNavigationTiming"/>.</summary>
    public const string Navigation = "navigation";

    /// <summary>Every subresource fetch - <see cref="PerformanceResourceTiming"/>.</summary>
    public const string Resource = "resource";

    /// <summary>Marks placed with <see cref="Performance.Mark"/> - <see cref="PerformanceEntry"/>.</summary>
    public const string Mark = "mark";

    /// <summary>Measures placed with <see cref="Performance.Measure"/> - <see cref="PerformanceEntry"/>.</summary>
    public const string Measure = "measure";

    /// <summary>First paint and first contentful paint - <see cref="PerformanceEntry"/>.</summary>
    public const string Paint = "paint";

    /// <summary>Tasks over 50 ms - <see cref="PerformanceLongTaskTiming"/>.</summary>
    public const string LongTask = "longtask";

    /// <summary>Frames over 50 ms - <see cref="PerformanceLongAnimationFrameTiming"/>.</summary>
    public const string LongAnimationFrame = "long-animation-frame";

    /// <summary>LCP candidates - <see cref="LargestContentfulPaint"/>.</summary>
    public const string LargestContentfulPaint = "largest-contentful-paint";

    /// <summary>Layout shifts - <see cref="LayoutShift"/>.</summary>
    public const string LayoutShift = "layout-shift";

    /// <summary>Slow interactions - <see cref="PerformanceEventTiming"/>.</summary>
    public const string Event = "event";

    /// <summary>The page's first interaction only - <see cref="PerformanceEventTiming"/>.</summary>
    public const string FirstInput = "first-input";

    /// <summary>Element timings for elements marked with the <c>elementtiming</c> attribute.</summary>
    public const string Element = "element";

    /// <summary>Back/forward cache restores and other visibility state changes.</summary>
    public const string VisibilityState = "visibility-state";
}
