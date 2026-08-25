namespace Bit.BlazorUI;

/// <summary>
/// The request the <see cref="BitBasicList{TItem}"/> makes to its items provider for a region of its items.
/// </summary>
/// <typeparam name="TItem">The type of the items of the list.</typeparam>
public readonly struct BitBasicListItemsProviderRequest<TItem>
{
    /// <summary>
    /// The zero based index of the first item to be supplied.
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// The number of items to be supplied.
    /// </summary>
    /// <remarks>
    /// A list that neither virtualizes nor pages its items asks for the whole set at once, which it does
    /// with a count of <see cref="int.MaxValue"/>.
    /// </remarks>
    public int Count { get; }

    /// <summary>
    /// The token that is cancelled once the requested items are no longer needed, which happens where the
    /// scrolling moved on to another region or the list was disposed of before the request was answered.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    internal BitBasicListItemsProviderRequest(int startIndex, int count, CancellationToken cancellationToken)
    {
        StartIndex = startIndex;
        Count = count;
        CancellationToken = cancellationToken;
    }
}
