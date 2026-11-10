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
}
