namespace Bit.BlazorUI;

public struct BitDropDownItemsProviderResult<TDropDownItem>
{
    public ICollection<TDropDownItem> Items { get; set; }
    public int TotalItemCount { get; set; }
    public BitDropDownItemsProviderResult(ICollection<TDropDownItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }
}

public static class BitDropDownItemsProviderResult
{
    public static BitDropDownItemsProviderResult<TDropDownItem> From<TDropDownItem>(ICollection<TDropDownItem> items, int totalItemCount)
        => new BitDropDownItemsProviderResult<TDropDownItem>(items, totalItemCount);
}
