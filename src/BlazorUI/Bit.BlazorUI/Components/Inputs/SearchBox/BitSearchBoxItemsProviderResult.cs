namespace Bit.BlazorUI;

public struct BitSearchBoxItemsProviderResult<TItem>
{
    public ICollection<TItem> Items { get; set; }
    public int TotalItemCount { get; set; }
    public BitSearchBoxItemsProviderResult(ICollection<TItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }
}

public static class BitSearchBoxItemsProviderResult
{
    public static BitSearchBoxItemsProviderResult<TItem> From<TItem>(ICollection<TItem> items, int totalItemCount) => new BitSearchBoxItemsProviderResult<TItem>(items, totalItemCount);
}
