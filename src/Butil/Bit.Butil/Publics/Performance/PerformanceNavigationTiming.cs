namespace Bit.Butil;

/// <summary>
/// The document's own load, as a resource timing plus the document milestones - unload, DOM
/// interactive, DOMContentLoaded, load. There is one of these per document, which is what makes it
/// the entry to read for "how long did this page take".
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceNavigationTiming">https://developer.mozilla.org/en-US/docs/Web/API/PerformanceNavigationTiming</see>
/// </summary>
/// <remarks>
/// Under a Blazor WebAssembly app these milestones describe the <i>host page</i>: <c>load</c> fires
/// once the boot script and the framework files are down, before .NET has rendered anything. The
/// time your app became usable is a <see cref="Performance.Mark"/> you place yourself.
/// </remarks>
public class PerformanceNavigationTiming : PerformanceResourceTiming
{
    /// <summary>How the document was reached: <c>"navigate"</c>, <c>"reload"</c>, <c>"back_forward"</c> or <c>"prerender"</c>.</summary>
    public string? Type { get; set; }

    /// <summary>How many redirects the navigation went through.</summary>
    public int RedirectCount { get; set; }

    /// <summary>When the previous document's <c>unload</c> handler started; <c>0</c> unless it was same-origin.</summary>
    public double UnloadEventStart { get; set; }

    /// <summary>When the previous document's <c>unload</c> handler finished.</summary>
    public double UnloadEventEnd { get; set; }

    /// <summary>When parsing finished and the document became interactive.</summary>
    public double DomInteractive { get; set; }

    /// <summary>When the <c>DOMContentLoaded</c> handlers started.</summary>
    public double DomContentLoadedEventStart { get; set; }

    /// <summary>When the <c>DOMContentLoaded</c> handlers finished.</summary>
    public double DomContentLoadedEventEnd { get; set; }

    /// <summary>When the document and its subresources finished loading.</summary>
    public double DomComplete { get; set; }

    /// <summary>When the <c>load</c> handlers started.</summary>
    public double LoadEventStart { get; set; }

    /// <summary>When the <c>load</c> handlers finished.</summary>
    public double LoadEventEnd { get; set; }

    /// <summary>
    /// For a prerendered document, how long it had been prerendering when it was activated. Subtract
    /// it from the other timestamps to get times relative to activation rather than to prerender.
    /// </summary>
    public double ActivationStart { get; set; }
}
