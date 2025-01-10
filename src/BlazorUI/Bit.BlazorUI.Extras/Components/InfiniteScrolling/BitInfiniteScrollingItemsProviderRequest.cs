namespace Bit.BlazorUI;

public sealed class BitInfiniteScrollingItemsProviderRequest
{
    public BitInfiniteScrollingItemsProviderRequest(int startIndex, CancellationToken cancellationToken)
    {
        StartIndex = startIndex;
        CancellationToken = cancellationToken;
    }

    public int StartIndex { get; }

    public CancellationToken CancellationToken { get; }
}
