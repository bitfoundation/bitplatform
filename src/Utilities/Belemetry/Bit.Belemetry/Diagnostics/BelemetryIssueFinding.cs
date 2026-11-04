namespace Bit.Belemetry;

/// <summary>One statement of an issue report, with how much it is worth and the timeline events it rests on.</summary>
/// <param name="Kind">Observed, inferred or user-provided.</param>
/// <param name="Text">The statement.</param>
/// <param name="Evidence">The timeline events (<see cref="BelemetryDiagnosticEvent.Sequence"/>, prefixed <c>b</c> for browser events) it rests on.</param>
public sealed record BelemetryIssueFinding(BelemetryIssueFindingKind Kind, string Text, IReadOnlyList<string> Evidence);
