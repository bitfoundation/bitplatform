namespace Bit.BlazorUI;

// The grid cascades this so that descendant columns can talk back to it. It's an internal type
// so that it doesn't show up by mistake in unrelated components.
internal class InternalGridContext<TGridItem>
{
    public BitDataGrid<TGridItem> Grid { get; }
    public EventCallbackSubscribable<object?> ColumnsFirstCollected { get; } = new();

    public InternalGridContext(BitDataGrid<TGridItem> grid)
    {
        Grid = grid;
    }
}
