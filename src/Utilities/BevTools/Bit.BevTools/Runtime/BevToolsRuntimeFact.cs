namespace Bit.BevTools;

/// <summary>One fact about the runtime a session runs on, with what it means when it needs a reading.</summary>
public sealed record BevToolsRuntimeFact(string Name, string Value, string? Hint = null, BevToolsSeverity Severity = BevToolsSeverity.Info);
