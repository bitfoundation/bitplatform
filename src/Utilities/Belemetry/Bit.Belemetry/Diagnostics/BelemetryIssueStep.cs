namespace Bit.Belemetry;

/// <summary>One step of an issue report's sequences (navigation, requests, interop failures, circuit transitions).</summary>
/// <param name="Time">When it happened.</param>
/// <param name="Text">What happened, sanitized (<c>/orders/{id}</c>, <c>GET https://api.example.com/orders/{n} 500 (120 ms)</c>).</param>
/// <param name="TraceId">Its trace, when it has one.</param>
/// <param name="Evidence">The timeline event it comes from.</param>
public sealed record BelemetryIssueStep(DateTimeOffset Time, string Text, string? TraceId, string? Evidence);
