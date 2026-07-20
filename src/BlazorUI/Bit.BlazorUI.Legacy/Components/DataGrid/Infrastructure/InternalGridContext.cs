namespace Bit.BlazorUI.Legacy;

// The grid cascades this so that descendant columns can talk back to it. It's an internal type
// so that it doesn't show up by mistake in unrelated components.
internal class InternalGridContext<TGridItem>
{
    public BitDataGridLegacy<TGridItem> Grid { get; }
    public EventCallbackSubscribable<object?> ColumnsFirstCollected { get; } = new();

    public InternalGridContext(BitDataGridLegacy<TGridItem> grid)
    {
        Grid = grid;
    }
}
