namespace Bit.Belemetry;

/// <summary>
/// Protection against error floods: an error thrown in a loop (a timer, a render loop, a retry) would otherwise fill
/// the queue, evict every other record and cost the backend a record per occurrence from every client. Applied by the
/// browser script to its errors, and by the .NET pipeline of WebAssembly and Hybrid apps to theirs (the server
/// pipeline serves every user at once and is not capped).
/// </summary>
public sealed class BelemetryErrorOptions
{
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
