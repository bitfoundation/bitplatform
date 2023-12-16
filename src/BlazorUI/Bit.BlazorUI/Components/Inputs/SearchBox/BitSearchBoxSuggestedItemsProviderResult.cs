namespace Bit.BlazorUI;

public struct BitSearchBoxSuggestedItemsProviderResult<TItem>
{
    public ICollection<TItem> Items { get; set; }
    public int TotalItemCount { get; set; }
    public BitSearchBoxSuggestedItemsProviderResult(ICollection<TItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }
}

public static class BitSearchBoxSuggestedItemsProviderResult
{
    public static BitSearchBoxSuggestedItemsProviderResult<TItem> From<TItem>(ICollection<TItem> items, int totalItemCount) => new BitSearchBoxSuggestedItemsProviderResult<TItem>(items, totalItemCount);
}
