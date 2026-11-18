namespace Bit.Bcrdt;

/// <summary>What one change from the shared state did to the lists.</summary>
/// <param name="Nodes">The ids of the nodes it added, changed or removed.</param>
/// <param name="Edges">The ids of the edges it added, changed or removed.</param>
/// <param name="Reordered">Whether it changed the order of either list.</param>
public sealed record BcrdtBlowRemoteChange(IReadOnlyCollection<string> Nodes, IReadOnlyCollection<string> Edges, bool Reordered);
