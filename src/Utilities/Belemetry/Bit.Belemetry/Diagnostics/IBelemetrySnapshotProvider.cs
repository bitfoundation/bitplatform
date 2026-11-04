namespace Bit.Belemetry;

/// <summary>
/// Application-defined diagnostic state: a named provider returning a small, structured, serializable description of
/// state that helps reproduce a failure (a cart's item count, a wizard's step, a feature flag set) - never the state
/// objects themselves. Register it as a scoped or singleton service
/// (<c>services.AddBitBelemetrySnapshotProvider&lt;MyProvider&gt;()</c>); snapshots are only taken when a capture asks
/// for them (<see cref="BelemetryDiagnosticCaptureOptions.IncludeSnapshots"/>) or the app records one
/// (<see cref="IBelemetryDiagnostics.RecordSnapshotAsync"/>).
/// </summary>
/// <remarks>
/// <para>
/// The data may hold strings, numbers, booleans, dates, enums, <see cref="Guid"/>s, nested
/// <see cref="IReadOnlyDictionary{TKey, TValue}"/> (string keys) and lists of those. Anything else is replaced with a
/// marker rather than walked: Belemetry never reflects over an object. Every value goes through the key redaction,
/// the application's <see cref="IBelemetryDiagnosticSanitizer"/>s, the privacy patterns and
/// <see cref="BelemetrySnapshotOptions"/>'s limits, and what was redacted, cut or left out is reported.
/// </para>
/// <para>
/// A snapshot is a description, not a restorable state. A provider that throws or exceeds
/// <see cref="BelemetrySnapshotOptions.Timeout"/> is reported as failed and does not keep the others from being taken.
/// </para>
/// </remarks>
public interface IBelemetrySnapshotProvider
{
    /// <summary>The snapshot's name, unique in the app (<c>cart</c>, <c>checkout-wizard</c>).</summary>
    string Name { get; }

    /// <summary>Describes the state now; <see langword="null"/> when there is nothing to describe.</summary>
    ValueTask<IReadOnlyDictionary<string, object?>?> CaptureAsync(BelemetrySnapshotContext context, CancellationToken cancellationToken);
}
