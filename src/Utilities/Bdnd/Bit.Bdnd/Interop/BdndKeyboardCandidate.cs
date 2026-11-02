namespace Bit.Bdnd;

/// <summary>A drop target a keyboard drag may move to, as the module is told about it.</summary>
internal sealed class BdndKeyboardCandidate
{
    /// <summary>The target's handler id.</summary>
    public string Id { get; set; } = "";

    /// <summary>The handler id of the target of the same provider around it, if any.</summary>
    public string? ParentId { get; set; }

    /// <summary>The target's <see cref="BdndEdges"/> flags: a target with edges is two places, before and after it.</summary>
    public int Edges { get; set; }
}
