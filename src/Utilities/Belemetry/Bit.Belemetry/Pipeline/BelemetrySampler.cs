using System.Buffers.Binary;
using System.Diagnostics;

namespace Bit.Belemetry;

/// <summary>
/// The trace-id-ratio sampling decision, as the OpenTelemetry <c>TraceIdRatioBased</c> sampler makes it. Belemetry
/// applies it to new traces with <see cref="BelemetryTracingOptions.SamplingRatio"/>; it is public so a processor
/// that samples at the end of a span (keep every failure, a fraction of the rest) can make the same deterministic,
/// trace-wide decision instead of a random one.
/// </summary>
public static class BelemetrySampler
{
    /// <summary>
    /// Whether a trace is sampled at <paramref name="ratio"/>. Deterministic in the trace id, so every service in a
    /// trace that samples at the same ratio makes the same decision.
    /// </summary>
    /// <param name="traceId">The trace id.</param>
    /// <param name="ratio">The fraction of traces kept, 0 to 1.</param>
    public static bool ShouldSample(ActivityTraceId traceId, double ratio)
    {
        if (ratio >= 1) return true;
        if (ratio <= 0) return false;

        Span<byte> bytes = stackalloc byte[16];
        traceId.CopyTo(bytes);
        return ShouldSample(bytes, ratio);
    }

    /// <summary>Same as <see cref="ShouldSample(ActivityTraceId, double)"/> for a hex trace id, such as <see cref="BelemetrySpan.TraceId"/>.</summary>
    /// <param name="traceIdHex">The 32-character hex trace id; anything else is kept.</param>
    /// <param name="ratio">The fraction of traces kept, 0 to 1.</param>
    public static bool ShouldSample(string traceIdHex, double ratio)
    {
        if (ratio >= 1) return true;
        if (ratio <= 0) return false;
        if (traceIdHex is null || traceIdHex.Length != 32) return true;

        Span<byte> bytes = stackalloc byte[16];
        return BelemetryHex.TryDecode(traceIdHex, bytes) is false || ShouldSample(bytes, ratio);
    }

    /// <summary>
    /// Whether the browser script keeps a session at <paramref name="ratio"/>, as its <c>isSessionSampled</c> decides
    /// (<c>Scripts/session.ts</c>): the leading hex digits of the first 8 characters of the (random) id as a fraction of
    /// 2^32, below the ratio. An id without any is never kept.
    /// </summary>
    internal static bool IsSessionSampled(string sessionId, double ratio)
    {
        if ((ratio < 1) is false) return true;
        if ((ratio > 0) is false) return false;

        // parseInt(id.slice(0, 8), 16): the digits up to the first that is not one.
        long bucket = 0;
        var digits = 0;
        foreach (var c in sessionId.AsSpan(0, Math.Min(8, sessionId.Length)))
        {
            var digit = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 10,
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => -1,
            };
            if (digit < 0) break;
            bucket = (bucket << 4) | (long)digit;
            digits++;
        }

        return digits > 0 && bucket / 4294967296.0 < ratio;
    }

    private static bool ShouldSample(ReadOnlySpan<byte> traceId, double ratio)
    {
        // The OpenTelemetry .NET sampler compares the absolute value of the first 8 bytes (big-endian) against
        // ratio * long.MaxValue; doing the same keeps decisions consistent with services using that SDK.
        var value = BinaryPrimitives.ReadInt64BigEndian(traceId);
        var abs = value == long.MinValue ? long.MaxValue : Math.Abs(value);
        return abs < (long)(ratio * long.MaxValue);
    }
}
