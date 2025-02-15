namespace Bit.BlazorUI;

public sealed class BitInfiniteScrollingItemsProviderRequest
{
    public BitInfiniteScrollingItemsProviderRequest(int skip, CancellationToken cancellationToken)
    {
        Skip = skip;
        CancellationToken = cancellationToken;
    }

    public int Skip { get; }

    public CancellationToken CancellationToken { get; }
}
