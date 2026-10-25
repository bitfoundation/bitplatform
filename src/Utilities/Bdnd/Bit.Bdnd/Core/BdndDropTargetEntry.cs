namespace Bit.Bdnd;

/// <summary>Internal registration record for a drop target.</summary>
internal sealed class BdndDropTargetEntry
{
    public required string Id { get; init; }

    /// <summary>The id of the nearest enclosing drop target of the same manager, if any.</summary>
    public string? ParentId { get; init; }

    public string[] Accept { get; set; } = [];
    public Func<object?, IBdndMonitor, bool>? CanDrop { get; set; }

    /// <summary>The target's name for screen readers (its <c>Label</c>), if any.</summary>
    public string? Label { get; set; }

    /// <summary>Runs the target's hover and drop for drags the native drag-and-drop API does not make (the keyboard).</summary>
    public IBdndDropTargetHandler? Handler { get; set; }

    /// <summary>True while the pointer is over the target and the target accepts the item type.</summary>
    public bool IsOver { get; set; }

    /// <summary>The edges the target places drags against (its <c>Edges</c>).</summary>
    public BdndEdges Edges { get; set; }

    /// <summary>The edge the drag over the target is closest to, while it is over it and the target has edges.</summary>
    public BdndEdge? Edge { get; set; }

    /// <summary>Tells the target alone that its state changed (its edge), without a provider-wide state change.</summary>
    public Action? Changed { get; set; }

    /// <summary>The native type matched by the native (OS) drag hovering the target, or null.</summary>
    public string? NativeItemType { get; set; }
}

/// <summary>What the source of the drag in progress does when the manager has to end that drag itself.</summary>
internal interface IBdndDragSourceHandler
{
    /// <summary>
    /// Ends drag <paramref name="dragId"/> at once, as a cancel: <c>OnEnd</c>'s synchronous part runs and the drag
    /// state is cleared before this returns; the task is the rest of <c>OnEnd</c>.
    /// </summary>
    Task AbortAsync(long dragId);
}

/// <summary>
/// What a drop target does when a drag that has no native events of its own - a keyboard drag - hovers it or is
/// dropped on it: the same as its managed <c>dragover</c> and <c>drop</c> handlers do for a native drag.
/// </summary>
internal interface IBdndDropTargetHandler
{
    /// <summary>Runs <c>OnHover</c> if the item's type is accepted.</summary>
    Task HoverAsync(BdndXYCoord? pointer);

    /// <summary>Runs <c>OnDrop</c> and records the drop if the target can take the item.</summary>
    Task DropAsync(BdndXYCoord? pointer);
}
