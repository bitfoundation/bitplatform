using System.Buffers.Binary;
using System.Diagnostics;

namespace Bit.Belemetry;

/// <summary>The trace-id-ratio sampling decision, as the OpenTelemetry <c>TraceIdRatioBased</c> sampler makes it.</summary>
internal static class BelemetrySampler
{
    /// <summary>
    /// Whether a trace is sampled at <paramref name="ratio"/>. Deterministic in the trace id, so every service in a
    /// trace that samples at the same ratio makes the same decision.
    /// </summary>
    public static bool ShouldSample(ActivityTraceId traceId, double ratio)
    {
        if (ratio >= 1) return true;
        if (ratio <= 0) return false;

        Span<byte> bytes = stackalloc byte[16];
        traceId.CopyTo(bytes);
        return ShouldSample(bytes, ratio);
    }

    /// <summary>Same as <see cref="ShouldSample(ActivityTraceId, double)"/> for a hex trace id.</summary>
    public static bool ShouldSample(string traceIdHex, double ratio)
    {
        if (ratio >= 1) return true;
        if (ratio <= 0) return false;
        if (traceIdHex.Length != 32) return true;

        Span<byte> bytes = stackalloc byte[16];
        return BelemetryHex.TryDecode(traceIdHex, bytes) is false || ShouldSample(bytes, ratio);
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
