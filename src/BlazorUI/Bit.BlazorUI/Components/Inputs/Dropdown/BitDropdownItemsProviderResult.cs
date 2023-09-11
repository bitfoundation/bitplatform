namespace Bit.BlazorUI;

public struct BitDropdownItemsProviderResult<TItem>
{
    public ICollection<TItem> Items { get; set; }
    public int TotalItemCount { get; set; }
    public BitDropdownItemsProviderResult(ICollection<TItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }
}

public static class BitDropdownItemsProviderResult
{
    public static BitDropdownItemsProviderResult<TItem> From<TItem>(ICollection<TItem> items, int totalItemCount) => new BitDropdownItemsProviderResult<TItem>(items, totalItemCount);
}
