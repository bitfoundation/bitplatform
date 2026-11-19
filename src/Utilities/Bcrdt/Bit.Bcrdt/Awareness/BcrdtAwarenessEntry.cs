namespace Bit.Bcrdt;

/// <summary>What one client is currently broadcasting about itself.</summary>
/// <param name="ClientId">The client session the entry belongs to.</param>
/// <param name="Clock">That client's awareness counter, used to order its own updates.</param>
/// <param name="State">The published state, or <see langword="null"/> if the client has gone.</param>
/// <param name="LastSeen">When this replica last heard from the client.</param>
public sealed record BcrdtAwarenessEntry(
    ulong ClientId,
    long Clock,
    IReadOnlyDictionary<string, object?>? State,
    DateTimeOffset LastSeen);
