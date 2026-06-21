namespace Bit.BlazorUI;

/// <summary>Result returned from a grid's <c>OnRead</c> callback.</summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed class BitDataGridReadResult<TItem>
{
    public BitDataGridReadResult(IReadOnlyList<TItem> items, int totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }

    /// <summary>The items for the current page/window.</summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>The total number of items matching the current filters (across all pages).</summary>
    public int TotalCount { get; }
}
