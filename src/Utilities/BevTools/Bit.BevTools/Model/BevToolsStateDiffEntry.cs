namespace Bit.BevTools;

public sealed record BevToolsStateDiffEntry(string Path, string? Before, string? After)
{
    public string Kind => Before is null ? "added" : After is null ? "removed" : "changed";
}
