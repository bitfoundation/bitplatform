namespace Bit.BevTools;

/// <summary>A lazily expandable view over one value. Children are resolved on demand by re-walking <see cref="Path"/> from the root object.</summary>
public sealed record BevToolsInspectedNode(string Name, string? TypeName, string Display, BevToolsNodeKind Kind, string Path, bool HasChildren, int? Count = null)
{
    public bool IsExpandable => HasChildren && Kind is not (BevToolsNodeKind.Circular or BevToolsNodeKind.Redacted or BevToolsNodeKind.Error);
}
