namespace Bit.BevTools;

public sealed record BevToolsPrerenderRequest(string Method, string Url, int? StatusCode, double? DurationMs, string? Error);
