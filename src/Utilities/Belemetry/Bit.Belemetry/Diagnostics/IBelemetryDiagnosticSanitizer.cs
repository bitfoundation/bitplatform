namespace Bit.Belemetry;

/// <summary>
/// Application-defined sanitization of diagnostic values: the attributes an event keeps and every value a snapshot
/// provider returns. Register it as a singleton (<c>services.AddSingleton&lt;IBelemetryDiagnosticSanitizer, MySanitizer&gt;()</c>);
/// every registered sanitizer runs, in registration order.
/// </summary>
/// <remarks>
/// It runs after the built-in key redaction (a value whose key is redacted never reaches it) and before the built-in
/// pattern scrubbing and size limits, which also apply to what it returns. A sanitizer that throws makes the value
/// excluded - never kept as it was - and the bundle's manifest counts it.
/// </remarks>
public interface IBelemetryDiagnosticSanitizer
{
    /// <summary>Sanitizes one value.</summary>
    /// <param name="path">The value's key, or its dotted path inside a snapshot (<c>cart.items.0.name</c>).</param>
    /// <param name="value">The value.</param>
    /// <param name="sanitized">The value to keep instead; <see langword="null"/> removes it.</param>
    /// <returns><see langword="true"/> when the sanitizer decided (then <paramref name="sanitized"/> is used); <see langword="false"/> to keep the value as it is.</returns>
    bool TrySanitize(string path, object? value, out object? sanitized);
}
