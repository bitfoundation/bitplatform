namespace Bit.Belemetry;

/// <summary><see cref="System.Diagnostics.Activity"/> collection.</summary>
public sealed class BelemetryTracingOptions
{
    /// <summary>
    /// The <see cref="System.Diagnostics.ActivitySource"/> names collected; a trailing <c>*</c> matches a prefix.
    /// Defaults to <c>System.Net.Http</c> and <c>Microsoft.AspNetCore.Components*</c> (plus <c>Microsoft.AspNetCore</c>
    /// for the server pipeline). Add your own sources here.
    /// </summary>
    public List<string> Sources { get; } = ["System.Net.Http", "Microsoft.AspNetCore.Components*"];

    /// <summary>
    /// The fraction of new traces recorded (parent-based: a child follows its parent's decision). Defaults to 1.0.
    /// </summary>
    public double SamplingRatio { get; set; } = 1.0;

    /// <summary>The maximum number of events kept per span. Defaults to 128.</summary>
    public int MaxEventsPerSpan { get; set; } = 128;

    /// <summary>The maximum number of links kept per span. Defaults to 128.</summary>
    public int MaxLinksPerSpan { get; set; } = 128;
}
