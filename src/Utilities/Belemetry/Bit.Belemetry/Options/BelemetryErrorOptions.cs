namespace Bit.Belemetry;

/// <summary>
/// The .NET exceptions nothing caught, and protection against error floods: an error thrown in a loop (a timer, a
/// render loop, a retry) would otherwise fill the queue, evict every other record and cost the backend a record per
/// occurrence from every client. The flood limits are applied by the browser script to its errors, and by the .NET
/// pipeline of WebAssembly and Hybrid apps to theirs (the server pipeline serves every user at once and is not capped).
/// </summary>
public sealed class BelemetryErrorOptions
{
    /// <summary>
    /// Whether the .NET pipeline records the exceptions nothing caught (<c>AppDomain.UnhandledException</c>, which ends
    /// the process) and the exceptions of faulted tasks nothing observed (<c>TaskScheduler.UnobservedTaskException</c>),
    /// as <see cref="BelemetryFeatures.Errors"/> records. A process that is ending also saves its queued records for the
    /// next launch to send: to files on .NET hosts, with <see cref="BelemetryFeatures.Persistence"/>; in WebAssembly it
    /// hands them to the page, which outlives the .NET runtime. Defaults to <see langword="true"/>.
    /// </summary>
    public bool CaptureUnhandledExceptions { get; set; } = true;

    /// <summary>
    /// How long an error identical to one just recorded - same scope, type, message and first stack frame - is
    /// suppressed. A loop that keeps throwing it is then recorded once per window. Defaults to 5 seconds; zero or
    /// less records every occurrence.
    /// </summary>
    public TimeSpan DuplicateWindow { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The most error records per minute (on average, with a burst of as many); the excess is dropped. Defaults to
    /// 100; zero or less is unlimited.
    /// </summary>
    public int MaxPerMinute { get; set; } = 100;
}
