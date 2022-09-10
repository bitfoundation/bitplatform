namespace Bit.BlazorUI;

public struct BitDropDownItemsProviderRequest<TDropDownItem>
{
    public int StartIndex { get; }

    public int Count { get; }

    public string? Search { get; }

    public CancellationToken CancellationToken { get; }

    internal BitDropDownItemsProviderRequest(int startIndex, int count, string? search, CancellationToken cancellationToken)
    {
        StartIndex = startIndex;
        Count = count;
        Search = search;
        CancellationToken = cancellationToken;
    }
}
