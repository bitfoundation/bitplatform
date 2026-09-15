namespace ButilTests.Benchmarks;

/// <summary>
/// Every number the suite fails on, in one place.
/// </summary>
/// <remarks>
/// Two kinds of budget live here, and they are not equally trustworthy.
/// <list type="bullet">
/// <item>
/// The <b>weight</b> budgets are deterministic: the same sources minify to the same bytes on every
/// machine, so these are set close to the measured figures and a regression is real. Raising one is
/// a decision to ship more JavaScript to every app that touches the module, and should be made
/// deliberately - not to make a red run green.
/// </item>
/// <item>
/// The <b>timing</b> budgets are not. They are measured through a real browser on whatever hardware
/// the run happens to have, so they are set as loose ceilings that only a genuine order-of-magnitude
/// regression can cross - a per-call cost drifting from 60 to 90 microseconds is noise, one drifting
/// to 900 is a round trip that stopped being a round trip. The sharp assertions in this suite are
/// the <b>ratios</b> instead (see <see cref="MinEventGateReduction"/>), because a ratio measured in one
/// run on one machine cancels the machine out.
/// </item>
/// </list>
/// </remarks>
internal static class Budgets
{
    // --- module weight (bytes, minified then brotli-compressed) --------------------------------

    /// <summary>
    /// The heaviest a single module's download may be - the module plus the dependencies its
    /// lazy-loaded file inlines. Held against the worst module rather than an average because the
    /// average hides exactly the case that matters: one module nobody noticed growing.
    /// <br/>
    /// It was 4,600 while the user-agent parser was a port of platform.js; the table-driven parser
    /// that replaced it moved the worst module to the WebAudio family, and the ceiling came down
    /// with it. A budget left at the old figure would have quietly stopped measuring anything.
    /// </summary>
    internal const int MaxModuleClosureBrotli = 3_700;

    /// <summary>
    /// The ninetieth percentile of the same figure. This is the one that catches a shared
    /// dependency quietly gaining weight: a few bytes added to <c>utils</c> move nearly every
    /// module at once, which the maximum above would not notice.
    /// </summary>
    internal const int MaxP90ModuleClosureBrotli = 2_600;

    /// <summary>
    /// The whole bundle, which is what an app that has not opted into lazy scripts downloads.
    /// </summary>
    internal const int MaxBundleBrotli = 80_000;

    /// <summary>The same bundle uncompressed, which is what the browser has to parse.</summary>
    internal const int MaxBundleMinified = 360_000;

    // --- interop timing (microseconds per operation) --------------------------------------------

    /// <summary>
    /// A value-returning round trip: .NET to JavaScript and back, with a serialized result. The
    /// ceiling is deliberately far above the figure a healthy run produces - it is here to catch a
    /// call shape that stopped being a single round trip, not to police scheduling jitter.
    /// </summary>
    internal const double MaxValueCallUs = 1_500;

    /// <summary>A void round trip - no result to deserialize.</summary>
    internal const double MaxVoidCallUs = 1_500;

    /// <summary>
    /// A read through an <see cref="Microsoft.AspNetCore.Components.ElementReference"/>, which
    /// additionally marshals the element. Higher than a plain call because it does more, not
    /// because it is allowed to be slow.
    /// </summary>
    internal const double MaxElementReadUs = 2_000;

    /// <summary>
    /// A megabyte of bytes coming back from JavaScript. Stated as throughput rather than a per-call
    /// ceiling because that is the number that tells you whether the payload path is copying more
    /// times than it should.
    /// </summary>
    internal const double MinPayloadThroughputMbPerSecond = 5;

    // --- rate limiting ---------------------------------------------------------------------------

    /// <summary>
    /// How much less traffic a gated DOM-event subscription must produce than an ungated one over
    /// the same burst. This is the assertion the whole rate-limiting feature rests on, and unlike a
    /// timing ceiling it is machine-independent: both halves are measured in the same run, on the
    /// same box, from the same burst.
    /// </summary>
    /// <remarks>
    /// An event burst is not frame-bound - a page can dispatch a thousand <c>mousemove</c>s inside
    /// one frame, and every one of them is a round trip - so the reduction here is bounded only by
    /// how long the burst takes, and comes out in the hundreds. Five is a floor with room for a slow
    /// machine to spread the same burst over more intervals, not an expected value.
    /// </remarks>
    internal const double MinEventGateReduction = 5;

    /// <summary>
    /// The same idea for an observer, whose ceiling is set by the browser's frame rate rather than
    /// by the burst - so it is held as a fraction of that ceiling, not as a fixed ratio.
    /// </summary>
    /// <remarks>
    /// A <c>ResizeObserver</c> already delivers at most once per frame, so an ungated run reports
    /// one batch per frame and the best a gate can do is <c>interval / frameTime</c> - about three
    /// for a 50 ms gate at 60 Hz, more on a machine pacing frames faster, and less on a loaded CI
    /// runner or a software-rendered headless browser that manages only 30. A fixed floor would fail
    /// on the slow machine for no fault of the gate, so the runner measures the frame time of the
    /// same burst and asks for this share of what that frame time allows. Three times fewer round
    /// trips through a whole window drag is the difference between a smooth resize and a Blazor
    /// Server circuit falling behind; seven tenths of it leaves room for the leading and trailing
    /// sends at either end of the burst.
    /// </remarks>
    internal const double MinObserverGateEfficiency = 0.7;

    /// <summary>
    /// The gate must not silence a subscription. A gated run that delivers nothing has not
    /// rate-limited anything - it has broken the subscription, and the trailing send exists
    /// precisely so this cannot happen.
    /// </summary>
    internal const int MinGatedDeliveries = 1;
}
