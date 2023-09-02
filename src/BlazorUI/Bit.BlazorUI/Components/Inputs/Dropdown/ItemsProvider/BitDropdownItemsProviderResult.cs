namespace Bit.BlazorUI;

public struct BitDropdownItemsProviderResult<TDropdownItem>
{
    public ICollection<TDropdownItem> Items { get; set; }
    public int TotalItemCount { get; set; }
    public BitDropdownItemsProviderResult(ICollection<TDropdownItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }
}

public static class BitDropdownItemsProviderResult
{
    public static BitDropdownItemsProviderResult<TDropdownItem> From<TDropdownItem>(ICollection<TDropdownItem> items, int totalItemCount)
        => new BitDropdownItemsProviderResult<TDropdownItem>(items, totalItemCount);
}
