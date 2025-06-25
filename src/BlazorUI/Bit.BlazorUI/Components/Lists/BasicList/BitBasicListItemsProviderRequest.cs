namespace Bit.BlazorUI;

public struct BitBasicListItemsProviderRequest<TItem>
{
    public int StartIndex { get; }

    public int Count { get; }

    public CancellationToken CancellationToken { get; }

    internal BitBasicListItemsProviderRequest(int startIndex, int count, CancellationToken cancellationToken)
    {
        StartIndex = startIndex;
        Count = count;
        CancellationToken = cancellationToken;
    }
}
