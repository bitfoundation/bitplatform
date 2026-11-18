namespace Bit.BevTools;

/// <summary>Thresholds used by the built-in diagnostics rules. All are evidence-based; raise them for noisy apps.</summary>
public sealed class BevToolsDiagnosticsOptions
{
    /// <summary>Renders of a single component within <see cref="RenderStormWindowMs"/> that count as a render storm.</summary>
    public int RenderStormCount { get; set; } = 30;

    public int RenderStormWindowMs { get; set; } = 2000;

    /// <summary>
    /// Renders within 10 seconds, all caused by an ancestor re-rendering, that count as an avoidable render pattern.
    /// Deliberately much lower than <see cref="RenderStormCount"/>: the evidence here is the ratio, not the rate.
    /// </summary>
    public int ParentDrivenRenderCount { get; set; } = 10;

    /// <summary>Number of components rendered in one batch that counts as a render cascade.</summary>
    public int RenderCascadeSize { get; set; } = 25;

    /// <summary>Single render (BuildRenderTree) duration considered slow, in milliseconds.</summary>
    public double SlowRenderMs { get; set; } = 16;

    /// <summary>Identical HTTP requests (method + URL) within <see cref="DuplicateHttpWindowMs"/> that count as duplicates.</summary>
    public int DuplicateHttpCount { get; set; } = 4;

    public int DuplicateHttpWindowMs { get; set; } = 1000;

    /// <summary>
    /// Requests to one endpoint that differ only in their query values (a search box, a filter) within
    /// <see cref="HttpBurstWindowMs"/> that count as a burst: the pattern of a request per keystroke.
    /// </summary>
    public int HttpBurstCount { get; set; } = 4;

    public int HttpBurstWindowMs { get; set; } = 2000;

    /// <summary>
    /// Requests to one endpoint, each for a different id in its path (a number, GUID or hash segment), within
    /// <see cref="HttpBurstWindowMs"/> that count as one request per item: the N+1 pattern of a list whose rows each
    /// load their own data.
    /// </summary>
    public int HttpPerItemCount { get; set; } = 5;

    public double SlowHttpMs { get; set; } = 2000;

    public double SlowJsInteropMs { get; set; } = 250;

    /// <summary>JS interop calls to the same identifier within one second that count as excessive.</summary>
    public int ExcessiveJsInteropCount { get; set; } = 50;

    /// <summary>Event handler duration considered slow (it blocks the renderer), in milliseconds.</summary>
    public double SlowEventHandlerMs { get; set; } = 100;

    /// <summary>Occurrences of the same error that count as a repeated error.</summary>
    public int RepeatedErrorCount { get; set; } = 3;

    /// <summary>
    /// Log entries with one message template (whatever their values), from one category, within ten seconds that count
    /// as a log flood: logging on a hot path, such as a render or a list item.
    /// </summary>
    public int LogFloodCount { get; set; } = 100;

    /// <summary>
    /// Bytes a component's render fragment allocates per render, on average over its renders of the last 30 seconds, that
    /// count as a heavy render (rule <c>render.allocations</c>). Where the runtime counts allocations per thread.
    /// </summary>
    public long HeavyRenderAllocationBytes { get; set; } = 256 * 1024;

    /// <summary>
    /// A user interaction's time to the next paint that counts as slow (rule <c>browser.slow-interaction</c>): 200 ms is
    /// where the Web Vitals' Interaction to Next Paint stops being good.
    /// </summary>
    public double SlowInteractionMs { get; set; } = 200;

    /// <summary>
    /// The blocking time of one frame (the part beyond 50 ms per task, input kept waiting) that counts as a blocked main
    /// thread (rule <c>browser.long-frame</c>).
    /// </summary>
    public double LongFrameBlockingMs { get; set; } = 150;

    /// <summary>
    /// Executions of one SQL command (Entity Framework Core, read from its logs) within two seconds that count as one
    /// query per item (rule <c>db.per-item</c>): the N+1 pattern of a list whose rows each load their own data.
    /// </summary>
    public int DbPerItemCount { get; set; } = 10;

    /// <summary>A database command's elapsed time, as Entity Framework Core reported it, that counts as slow (rule <c>db.slow</c>).</summary>
    public double SlowDbCommandMs { get; set; } = 500;

    /// <summary>Rule ids that are disabled.</summary>
    public HashSet<string> DisabledRules { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the rules as data arrives (at most once a second) while the panel is closed too. Off by default: they then
    /// run while the panel is open, when a frontend reads the session and when it is exported - and most of them look
    /// back over a window of 10 to 60 seconds, so an export made after a longer quiet spell misses what they would have
    /// found in the activity before it. Turn it on for a run whose export a CI gate checks (<see cref="BevToolsSessionCheck"/>);
    /// it needs the panel component on the page, open or not.
    /// </summary>
    public bool EvaluateWhileClosed { get; set; }
}
