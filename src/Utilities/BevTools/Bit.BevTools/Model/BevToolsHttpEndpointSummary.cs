namespace Bit.BevTools;

/// <summary>
/// One endpoint's requests in the buffer. Durations are of the requests that completed (failed ones included, cancelled
/// and pending ones not); sizes are response bodies as the transport reported them, null unless every response did.
/// </summary>
internal sealed record BevToolsHttpEndpointSummary(
    string Endpoint,
    int Requests,
    int Failed,
    int Cancelled,
    int Pending,
    double? P50Ms,
    double? P95Ms,
    double? MaxMs,
    long? ResponseBytes,
    DateTimeOffset LastAt,
    int? LastStatus,
    int DistinctUrls)
{
    /// <summary>The component methods that sent its requests, most first, with how many each: the first five.</summary>
    public IReadOnlyList<(string Component, int Requests)> Callers { get; init; } = [];

    /// <summary>How many different component methods sent its requests, all of them.</summary>
    public int CallerCount { get; init; }
}
