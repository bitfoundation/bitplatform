namespace Bit.Belemetry;

/// <summary><see cref="System.Diagnostics.Activity"/> collection.</summary>
public sealed class BelemetryTracingOptions
{
    /// <summary>
    /// The <see cref="System.Diagnostics.ActivitySource"/> names collected; a trailing <c>*</c> matches a prefix.
    /// Defaults to <c>System.Net.Http</c>, <c>Microsoft.AspNetCore.Components*</c> and <c>Bit.Belemetry.*</c> (the
    /// instrumentation packages, such as <c>Bit.Belemetry.EntityFrameworkCore</c> and <c>Bit.Belemetry.Hangfire</c>),
    /// plus <c>Microsoft.AspNetCore</c> for the server pipeline. Add your own sources here.
    /// </summary>
    public List<string> Sources { get; } = ["System.Net.Http", "Microsoft.AspNetCore.Components*", "Bit.Belemetry.*"];

    /// <summary>
    /// Activity names (<see cref="System.Diagnostics.Activity.OperationName"/>; a trailing <c>*</c> matches a prefix)
    /// never recorded, decided when the activity is created so it costs nothing - e.g.
    /// <c>Microsoft.AspNetCore.Components.HandleEvent</c> to drop Blazor's per-event spans. The children of an ignored
    /// activity attach to its parent. Empty by default.
    /// </summary>
    public List<string> IgnoredActivityNames { get; } = [];

    /// <summary>
    /// The fraction of new traces recorded (parent-based: a child follows its parent's decision). Defaults to 1.0.
    /// </summary>
    public double SamplingRatio { get; set; } = 1.0;

    /// <summary>The maximum number of events kept per span. Defaults to 128.</summary>
    public int MaxEventsPerSpan { get; set; } = 128;

    /// <summary>The maximum number of links kept per span. Defaults to 128.</summary>
    public int MaxLinksPerSpan { get; set; } = 128;
}
