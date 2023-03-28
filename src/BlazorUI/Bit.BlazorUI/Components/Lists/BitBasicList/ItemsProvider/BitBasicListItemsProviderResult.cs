namespace Bit.BlazorUI;

public struct BitBasicListItemsProviderResult<TItem>
{
    public ICollection<TItem> Items { get; set; }
    public int TotalItemCount { get; set; }
    public BitBasicListItemsProviderResult(ICollection<TItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }
}

public static class BitBasicListItemsProviderResult
{
    public static BitBasicListItemsProviderResult<TItem> From<TItem>(ICollection<TItem> items, int totalItemCount)
        => new BitBasicListItemsProviderResult<TItem>(items, totalItemCount);
}
