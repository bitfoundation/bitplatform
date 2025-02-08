namespace Bit.BlazorUI;

public class BitSearchBoxSuggestItemsProviderRequest(string? searchTerm, int take, CancellationToken cancellationToken)
{
    public string? SearchTerm { get; } = searchTerm;

    public int Take { get; } = take;

    public CancellationToken CancellationToken { get; } = cancellationToken;
}
