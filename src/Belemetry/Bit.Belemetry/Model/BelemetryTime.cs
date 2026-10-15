using System.Diagnostics;

namespace Bit.Belemetry;

/// <summary>Conversions between .NET time types and the Unix-epoch nanoseconds OTLP uses.</summary>
public static class BelemetryTime
{
    private const long NanosPerTick = 100;
    private static readonly long _unixEpochTicks = DateTime.UnixEpoch.Ticks;

    // DateTime.UtcNow has ~1-16 ms resolution on some platforms (and in the browser it is Date.now()), which
    // would collapse the start/end of short operations onto the same value. Anchoring once to UtcNow and
    // advancing with the monotonic Stopwatch gives sub-millisecond resolution and immunity to wall-clock jumps.
    private static readonly long _anchorUnixNano = (DateTime.UtcNow.Ticks - _unixEpochTicks) * NanosPerTick;
    private static readonly long _anchorTimestamp = Stopwatch.GetTimestamp();

    /// <summary>The current time, in nanoseconds since the Unix epoch.</summary>
    public static long NowUnixNano()
    {
        var elapsed = Stopwatch.GetTimestamp() - _anchorTimestamp;
        return _anchorUnixNano + (long)(elapsed * (1_000_000_000.0 / Stopwatch.Frequency));
    }

    /// <summary>Converts a <see cref="DateTime"/> (local times are converted to UTC) to Unix-epoch nanoseconds.</summary>
    public static long ToUnixNano(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
        return (utc.Ticks - _unixEpochTicks) * NanosPerTick;
    }

    /// <summary>Converts a <see cref="DateTimeOffset"/> to Unix-epoch nanoseconds.</summary>
    public static long ToUnixNano(DateTimeOffset value) => (value.UtcTicks - _unixEpochTicks) * NanosPerTick;

    /// <summary>Converts Unix-epoch milliseconds (as the browser reports them, possibly fractional) to Unix-epoch nanoseconds.</summary>
    public static long FromUnixMilliseconds(double milliseconds)
    {
        // Multiplying the whole value by 1e6 lands around 1.7e18, past the 2^53 a double holds exactly, which would
        // cost up to ~256 ns; scaling only the fractional part keeps every representable input exact.
        var whole = Math.Floor(milliseconds);
        return ((long)whole * 1_000_000L) + (long)Math.Round((milliseconds - whole) * 1_000_000d);
    }

    /// <summary>Converts Unix-epoch nanoseconds to a UTC <see cref="DateTime"/>.</summary>
    public static DateTime ToDateTime(long unixNano) => new(_unixEpochTicks + (unixNano / NanosPerTick), DateTimeKind.Utc);
}
