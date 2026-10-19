namespace Bit.Bdnd;

/// <summary>
/// Cascaded by every <see cref="BdndDropTarget{TItem}"/> to its content so nested drop
/// targets know their enclosing target (needed for shallow <c>IsOver</c>).
/// </summary>
internal sealed class BdndDropTargetScope
{
    public BdndDropTargetScope(BdndDragDropManager manager, string handlerId, BdndDropTargetScope? parent)
    {
        Manager = manager;
        HandlerId = handlerId;
        Parent = parent;
    }

    public BdndDragDropManager Manager { get; }
    public string HandlerId { get; }
    public BdndDropTargetScope? Parent { get; }

    /// <summary>
    /// The handler id of the nearest target in this scope chain (this one included) that belongs
    /// to <paramref name="manager"/>; targets of nested providers are skipped.
    /// </summary>
    public string? FindTargetId(BdndDragDropManager manager)
    {
        for (var scope = this; scope is not null; scope = scope.Parent)
        {
            if (ReferenceEquals(scope.Manager, manager)) return scope.HandlerId;
        }
        return null;
    }
}
