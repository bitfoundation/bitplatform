namespace Bit.Bortable;

/// <summary>
/// The per-scope coordinator that holds the active drag, so that several <see cref="Bortable{TItem}"/>
/// lists in the same group can exchange items. Scoped, so every Blazor Server circuit has a drag state
/// of its own. Registered by <see cref="BitBortable.AddBitBortableServices"/>.
/// </summary>
internal sealed class BortableService
{
    internal BortableDragContext? Context { get; set; }

    /// <summary>
    /// Serializes the drag handlers of all lists. Each handler awaits JS interop, so without it a
    /// <c>dragover</c> could interleave with another one (or with the <c>drop</c>/<c>dragend</c> that
    /// finishes the drag) and act on stale indices.
    /// </summary>
    internal SemaphoreSlim Gate { get; } = new(1, 1);
}
