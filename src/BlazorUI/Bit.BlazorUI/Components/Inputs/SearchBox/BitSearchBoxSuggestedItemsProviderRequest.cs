namespace Bit.BlazorUI;

public struct BitSearchBoxSuggestedItemsProviderRequest<TItem>
{
    public int StartIndex { get; }

    public int Count { get; }

    public string? Search { get; }

    public CancellationToken CancellationToken { get; }

    internal BitSearchBoxSuggestedItemsProviderRequest(int startIndex, int count, string? search, CancellationToken cancellationToken)
    {
        StartIndex = startIndex;
        Count = count;
        Search = search;
        CancellationToken = cancellationToken;
    }
}
