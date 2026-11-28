using System.Globalization;
using System.Text;

namespace Bit.Belemetry;

/// <summary>
/// The sampling ratio a trace was recorded at, carried in its W3C <c>tracestate</c> as the <c>belemetry</c> entry
/// (<c>belemetry=sr:0.25</c>). The activity collector writes it when <see cref="BelemetryTracingOptions.SamplingRatio"/>
/// keeps a new trace. From there it flows with the trace like any other entry: to the local children, to an explicit
/// parent context (an in-process queue's consumer), to a Hangfire job, and to the downstream services and the server a
/// WebAssembly <c>HttpClient</c> calls. Every span of the trace then knows how many like it the ratio dropped
/// (<see cref="BelemetrySpan.SamplingRatio"/>). It is a Belemetry entry and not OpenTelemetry's <c>ot=th:</c>
/// threshold: that one belongs to the consistent probability sampler, which decides on other bits of the trace id.
/// </summary>
internal static class BelemetryTraceState
{
    /// <summary>The entry's key.</summary>
    public const string Key = "belemetry";

    private const string KeyPrefix = Key + "=";
    private const string RatioField = "sr:";
    private const int MaxMembers = 32;

    /// <summary>
    /// <paramref name="traceState"/> with an entry recording <paramref name="ratio"/> first. The entry replaces one the
    /// state already had (W3C: an updated entry moves to the front), and members past the 32 W3C allows are left out.
    /// </summary>
    public static string WithSamplingRatio(string? traceState, double ratio)
    {
        var entry = KeyPrefix + RatioField + ratio.ToString("R", CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(traceState)) return entry;

        var builder = new StringBuilder(entry, entry.Length + traceState.Length + 1);
        var members = 1;
        foreach (var range in traceState.AsSpan().Split(','))
        {
            var member = traceState.AsSpan(range).Trim();
            if (member.IsEmpty || member.StartsWith(KeyPrefix, StringComparison.Ordinal)) continue;
            if (members++ == MaxMembers) break;
            builder.Append(',').Append(member);
        }

        return builder.ToString();
    }

    /// <summary>
    /// The ratio <paramref name="traceState"/> records (<see cref="WithSamplingRatio"/>), above 0 and below 1. 1 when
    /// it records none, or a value that is not such a ratio.
    /// </summary>
    public static double SamplingRatio(string? traceState)
    {
        if (string.IsNullOrEmpty(traceState)) return 1;

        foreach (var range in traceState.AsSpan().Split(','))
        {
            var member = traceState.AsSpan(range).Trim();
            if (member.StartsWith(KeyPrefix, StringComparison.Ordinal) is false) continue;

            var value = member[KeyPrefix.Length..];
            foreach (var fieldRange in value.Split(';'))
            {
                var field = value[fieldRange];
                if (field.StartsWith(RatioField, StringComparison.Ordinal)
                    && double.TryParse(field[RatioField.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out var ratio)
                    && ratio > 0 && ratio < 1)
                {
                    return ratio;
                }
            }

            return 1;
        }

        return 1;
    }

    /// <summary>The key of OpenTelemetry's own tracestate entry.</summary>
    public const string OtelKey = "ot";

    private const string OtelKeyPrefix = OtelKey + "=";
    private const string ThresholdField = "th";
    private const string RandomnessField = "rv";
    private const int MaxOtelEntryLength = 256;
    private const long MaxAdjustedCount = 1L << 56;

    /// <summary>
    /// The tracestate an OTLP exporter sends for a span of trace <paramref name="traceIdHex"/> kept at
    /// <paramref name="ratio"/> (above 0, below 1): <paramref name="traceState"/> with OpenTelemetry's <c>ot</c> entry first,
    /// carrying the rejection threshold of that ratio (<c>th</c>) and the randomness it was compared against (<c>rv</c>),
    /// its other values kept. Null when the threshold cannot be written consistently, and the ratio goes elsewhere.
    /// </summary>
    /// <remarks>
    /// OpenTelemetry's probability sampling keeps a span when its randomness is at least the threshold, and a consumer
    /// erases a threshold the randomness contradicts. <see cref="BelemetrySampler"/> decides on the first 8 bytes of the
    /// trace id, as OpenTelemetry .NET's <c>TraceIdRatioBased</c> sampler does, while the default randomness is the last 7:
    /// a bare <c>th</c> would contradict them for most kept spans. So the randomness is written too, derived from the bytes
    /// the decision read (<see cref="Randomness"/>): every span <see cref="BelemetrySampler.ShouldSample(string, double)"/>
    /// keeps at a ratio satisfies its threshold, the head decision and a tail sampler's alike. The value is the same for
    /// every span of the trace. A state whose <c>ot</c> entry already has another randomness (a root in a service sampling
    /// with its own) does not get a threshold this decision did not make: null.
    /// </remarks>
    public static string? WithOtelThreshold(string? traceState, string traceIdHex, double ratio)
    {
        if ((ratio > 0 && ratio < 1) is false || Randomness(traceIdHex) is not { } randomness) return null;

        var threshold = Threshold(ratio);
        var entry = new StringBuilder(OtelKeyPrefix, MaxOtelEntryLength + 8);
        entry.Append(RandomnessField).Append(':').Append(randomness).Append(';').Append(ThresholdField).Append(':').Append(threshold);

        var rest = new StringBuilder();
        var members = 1;
        if (string.IsNullOrWhiteSpace(traceState) is false)
        {
            foreach (var range in traceState.AsSpan().Split(','))
            {
                var member = traceState.AsSpan(range).Trim();
                if (member.IsEmpty) continue;

                if (member.StartsWith(OtelKeyPrefix, StringComparison.Ordinal))
                {
                    var value = member[OtelKeyPrefix.Length..];
                    foreach (var fieldRange in value.Split(';'))
                    {
                        var field = value[fieldRange].Trim();
                        if (field.IsEmpty) continue;

                        var colon = field.IndexOf(':');
                        var key = colon < 0 ? field : field[..colon];
                        if (key.SequenceEqual(RandomnessField))
                        {
                            if (field[(colon + 1)..].SequenceEqual(randomness) is false) return null;
                        }
                        else if (key.SequenceEqual(ThresholdField) is false)
                        {
                            entry.Append(';').Append(field);
                        }
                    }
                    continue;
                }

                if (members++ == MaxMembers) break;
                rest.Append(',').Append(member);
            }
        }

        // The specification's limit on the entry; past it the threshold is not written rather than other values lost.
        if (entry.Length - OtelKeyPrefix.Length > MaxOtelEntryLength) return null;

        return entry.Append(rest).ToString();
    }

    /// <summary>
    /// The rejection threshold of <paramref name="ratio"/> as OpenTelemetry encodes it: <c>2^56 - ceil(ratio * 2^56)</c>,
    /// 14 lowercase hex digits without the trailing zeros (<c>c</c> for 0.25). Rounded down, never up: a span the ratio
    /// keeps must never fall below its threshold.
    /// </summary>
    internal static string Threshold(double ratio)
    {
        // Multiplying by a power of two is exact, and so is the ceiling of the product.
        var threshold = MaxAdjustedCount - (long)Math.Ceiling(ratio * MaxAdjustedCount);
        var hex = threshold.ToString("x14", CultureInfo.InvariantCulture).TrimEnd('0');
        return hex.Length == 0 ? "0" : hex;
    }

    /// <summary>
    /// The 56-bit randomness <see cref="BelemetrySampler"/>'s decision on <paramref name="traceIdHex"/> amounts to, as 14
    /// lowercase hex digits: <c>2^56 - 1 - (|first 8 bytes| &gt;&gt; 7)</c>. The sampler keeps a trace at a ratio when that
    /// absolute value is below <c>ratio * 2^63</c>, so the randomness is then at least <see cref="Threshold"/> of the ratio.
    /// Null for an id that is not 32 hex digits.
    /// </summary>
    internal static string? Randomness(string traceIdHex)
    {
        if (traceIdHex is null || traceIdHex.Length != 32) return null;

        Span<byte> bytes = stackalloc byte[16];
        if (BelemetryHex.TryDecode(traceIdHex, bytes) is false) return null;

        var value = System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(bytes);
        var abs = value == long.MinValue ? long.MaxValue : Math.Abs(value);
        return (MaxAdjustedCount - 1 - (abs >> 7)).ToString("x14", CultureInfo.InvariantCulture);
    }
}
