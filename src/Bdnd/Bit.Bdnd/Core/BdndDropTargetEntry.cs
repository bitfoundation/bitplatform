namespace Bit.Bdnd;

/// <summary>Internal registration record for a drop target.</summary>
internal sealed class BdndDropTargetEntry
{
    public required string Id { get; init; }

    /// <summary>The id of the nearest enclosing drop target of the same manager, if any.</summary>
    public string? ParentId { get; init; }

    public string[] Accept { get; set; } = [];
    public Func<object?, IBdndMonitor, bool>? CanDrop { get; set; }

    /// <summary>True while the pointer is over the target and the target accepts the item type.</summary>
    public bool IsOver { get; set; }

    /// <summary>The native type matched by the native (OS) drag hovering the target, or null.</summary>
    public string? NativeItemType { get; set; }
}
