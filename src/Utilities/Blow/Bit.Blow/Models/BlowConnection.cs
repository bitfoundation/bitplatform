namespace Bit.Blow;

/// <summary>
/// A pending or completed connection between two handles, produced while the
/// user drags from a handle.
/// </summary>
public readonly record struct BlowConnection(
    string? Source,
    string? Target,
    string? SourceHandle,
    string? TargetHandle);
