namespace Bit.BevTools;

/// <summary>A group of facts: how this session renders, the host it renders on, the .NET runtime underneath.</summary>
public sealed record BevToolsRuntimeSection(string Title, string? Scope, IReadOnlyList<BevToolsRuntimeFact> Facts);
