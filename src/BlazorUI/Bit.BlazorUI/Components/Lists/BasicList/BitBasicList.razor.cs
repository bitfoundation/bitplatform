namespace Bit.BlazorUI;

/// <summary>
/// BitBasicList provides a base component for rendering large sets of items. It’s agnostic of layout, the tile component used, and selection management.
/// </summary>
public partial class BitBasicList<TItem> : BitComponentBase
{
    private _BitBasicListVirtualize<TItem>? _bitBasicListVirtualizeRef;



    /// <summary>
    /// The custom content that gets rendered when there is no item to show.
    /// </summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>
    /// Enables virtualization in rendering the list.
    /// </summary>
    [Parameter] public bool EnableVirtualization { get; set; }

    /// <summary>
    /// Gets or sets the list of items to render.
    /// </summary>
    [Parameter] public ICollection<TItem> Items { get; set; } = Array.Empty<TItem>();

    /// <summary>
    /// Gets the size of each item in pixels. Defaults to 50px.
    /// </summary>
    [Parameter] public float ItemSize { get; set; } = 50f;

    /// <summary>
    /// Gets or sets a value that determines how many additional items will be rendered before and after the visible region.
    /// </summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>
    /// Gets or set the role attribute of the BasicList html element.
    /// </summary>
    [Parameter] public string Role { get; set; } = "list";

    /// <summary>
    /// Gets or sets the Template to render each row.
    /// </summary>
    [Parameter] public RenderFragment<TItem> RowTemplate { get; set; } = default!;

    /// <summary>
    /// The function providing items to the list
    /// </summary>
    [Parameter] public BitBasicListItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>
    /// The template for items that have not yet been loaded in memory.
    /// </summary>
    [Parameter] public RenderFragment<PlaceholderContext>? VirtualizePlaceholder { get; set; }



    public async Task RefreshDataAsync()
    {
        if (ItemsProvider is null) return;
        if (_bitBasicListVirtualizeRef is null) return;

        await _bitBasicListVirtualizeRef.RefreshDataAsync();
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-bsl";

    // Gets called both by RefreshDataCoreAsync and directly by the Virtualize child component during scrolling
    private async ValueTask<ItemsProviderResult<TItem>> ProvideVirtualizedItems(ItemsProviderRequest request)
    {
        if (ItemsProvider is null) return default;

        // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
        // TODO: Consider making this configurable, or smarter (e.g., doesn't delay on first call in a batch, then the amount
        // of delay increases if you rapidly issue repeated requests, such as when scrolling a long way)
        await Task.Delay(100);
        if (request.CancellationToken.IsCancellationRequested) return default;

        var providerRequest = new BitBasicListItemsProviderRequest<TItem>(request.StartIndex, request.Count, request.CancellationToken);
        var providerResult = await ItemsProvider(providerRequest);

        if (request.CancellationToken.IsCancellationRequested) return default;

        return new ItemsProviderResult<TItem>(providerResult.Items, providerResult.TotalItemCount);
    }
}
