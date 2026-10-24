namespace Bit.Belemetry;

/// <summary>
/// <see cref="BelemetryOptions.MaxValueLength"/>: cuts long strings in attributes, bodies and status messages. The
/// same rule is implemented in <c>Scripts/limits.ts</c>; keep both in step.
/// </summary>
internal static class BelemetryValueLimit
{
    public static void Apply(BelemetrySpan span, int max)
    {
        if (max <= 0) return;

        Apply(span.Attributes, max);
        if (span.StatusMessage is not null) span.StatusMessage = Truncate(span.StatusMessage, max);
        foreach (var e in span.Events) Apply(e.Attributes, max);
    }

    public static void Apply(BelemetryLogRecord log, int max)
    {
        if (max <= 0) return;

        Apply(log.Attributes, max);
        if (log.Body is not null) log.Body = Truncate(log.Body, max);
    }

    /// <summary>The first <paramref name="max"/> characters, one fewer when the cut would split a surrogate pair.</summary>
    internal static string Truncate(string value, int max)
    {
        if (max <= 0 || value.Length <= max) return value;
        return value[..(char.IsHighSurrogate(value[max - 1]) ? max - 1 : max)];
    }

    private static void Apply(BelemetryAttributes attributes, int max)
    {
        List<KeyValuePair<string, object>>? changed = null;
        foreach (var (key, value) in attributes)
        {
            object? next = value switch
            {
                string s when s.Length > max => Truncate(s, max),
                // An IEnumerable<string> attribute can hold nulls.
                string[] a when a.Any(s => s?.Length > max) => a.Select(s => s is null ? s : Truncate(s, max)).ToArray(),
                _ => null,
            };

            // Collected and set afterwards: the attributes cannot change while they are being enumerated.
            if (next is not null) (changed ??= []).Add(new(key, next));
        }

        if (changed is null) return;
        foreach (var (key, value) in changed) attributes.Set(key, value);
    }
}
