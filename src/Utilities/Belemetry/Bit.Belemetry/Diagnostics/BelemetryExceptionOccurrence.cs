namespace Bit.Belemetry;

/// <summary>One occurrence of an exception group: when, in which release, and where.</summary>
/// <param name="Sequence">The timeline event, when it is still on the timeline.</param>
/// <param name="Time">When it happened.</param>
/// <param name="Release">The release (service version) it happened in.</param>
/// <param name="TraceId">The trace it happened in, when there was one.</param>
/// <param name="Route">The route template it happened on, when known.</param>
/// <param name="ComponentType">The component it is associated with, when known.</param>
/// <param name="Message">The sanitized message of this occurrence.</param>
/// <param name="Origin">Where it was observed (<see cref="BelemetryDiagnosticException.Origin"/>).</param>
public sealed record BelemetryExceptionOccurrence(long? Sequence, DateTimeOffset Time, string? Release, string? TraceId, string? Route, string? ComponentType, string? Message, string Origin);
