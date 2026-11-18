namespace Bit.Bdnd;

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
