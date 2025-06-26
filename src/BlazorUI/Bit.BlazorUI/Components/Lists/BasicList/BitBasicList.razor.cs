namespace Bit.BlazorUI;

/// <summary>
/// BitBasicList provides a base component for rendering large sets of items. It’s agnostic of layout, the tile component used, and selection management.
/// </summary>
public partial class BitBasicList<TItem> : BitComponentBase
{
    private int _loadMoreSkip = 0;
    private bool _loadMoreFinished;
    private ICollection<TItem> _viewItems = [];
    private CancellationTokenSource? _globalCts;
    private ICollection<TItem>? _internalItems = null;
    private _BitBasicListVirtualize<TItem>? _bitBasicListVirtualizeRef;
    private BitBasicListItemsProvider<TItem>? _internalItemsProvider = null;



    private bool _isLoadingMore => _globalCts is not null;



    /// <summary>
    /// The custom content that will be rendered when there is no item to show.
    /// </summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>
    /// Sets the height of the list to fit its content.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FitHeight { get; set; }

    /// <summary>
    /// Sets the width and height of the list to fit its content.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FitSize { get; set; }

    /// <summary>
    /// Sets the width of the list to fit its content.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FitWidth { get; set; }

    /// <summary>
    /// Sets the height of the list to 100%.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Sets the width and height of the list to 100%.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FullSize { get; set; }

    /// <summary>
    /// Sets the width of the list to 100%.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The list of items to render.
    /// </summary>
    [Parameter] public ICollection<TItem>? Items { get; set; }

    /// <summary>
    /// Size of each item in pixels. Defaults to 50px.
    /// </summary>
    [Parameter] public float ItemSize { get; set; } = 50f;

    /// <summary>
    /// The function providing items to the list.
    /// </summary>
    [Parameter] public BitBasicListItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>
    /// Enables the LoadMore mode for the list.
    /// </summary>
    [Parameter] public bool LoadMore { get; set; }

    /// <summary>
    /// The number of items to be loaded and rendered after the LoadMore button is clicked. Defaults to 20.
    /// </summary>
    [Parameter] public int LoadMoreSize { get; set; } = 20;

    /// <summary>
    /// The template of the LoadMore button.
    /// </summary>
    [Parameter] public RenderFragment<bool>? LoadMoreTemplate { get; set; }

    /// <summary>
    /// The custom text of the default LoadMore button. Defaults to "LoadMore".
    /// </summary>
    [Parameter] public string? LoadMoreText { get; set; } = "LoadMore";

    /// <summary>
    /// A value that determines how many additional items will be rendered before and after the visible region in Virtualize mode.
    /// </summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>
    /// The role attribute of the html element of the list.
    /// </summary>
    [Parameter] public string Role { get; set; } = "list";

    /// <summary>
    /// The template to render each row.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? RowTemplate { get; set; }

    /// <summary>
    /// Enables virtualization in rendering the list.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>
    /// The template for items that have not yet rendered.
    /// </summary>
    [Parameter] public RenderFragment<PlaceholderContext>? VirtualizePlaceholder { get; set; }



    public async Task RefreshDataAsync()
    {
        _globalCts?.Cancel();
        _globalCts = null;

        if (ItemsProvider is null) return;
        if (_bitBasicListVirtualizeRef is null) return;

        await _bitBasicListVirtualizeRef.RefreshDataAsync();
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-bsl";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => (FullSize || FullWidth) ? "width:100%" : string.Empty);
        StyleBuilder.Register(() => (FullSize || FullHeight) ? "height:100%" : string.Empty);

        StyleBuilder.Register(() => (FitSize || FitWidth) ? "width:fit-content" : string.Empty);
        StyleBuilder.Register(() => (FitSize || FitHeight) ? "height:fit-content" : string.Empty);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_internalItems != Items)
        {
            _internalItems = Items;

            if (ItemsProvider is null) // ItemsProvider always has priority over Items
            {
                _viewItems = Items ?? [];

                if (LoadMore)
                {
                    await PerformLoadMore(true);
                }
            }
        }

        if (_internalItemsProvider != ItemsProvider)
        {
            _internalItemsProvider = ItemsProvider;

            if (LoadMore && ItemsProvider is not null)
            {
                await PerformLoadMore(true);
            }
        }

        await base.OnParametersSetAsync();
    }


    private async Task PerformLoadMore(bool reset)
    {
        if (reset)
        {
            _viewItems = [];
            _loadMoreSkip = 0;
            _loadMoreFinished = false;
        }

        if (LoadMore is false || _globalCts is not null) return;

        var localCts = new CancellationTokenSource();
        _globalCts = localCts;

        try
        {
            StateHasChanged();

            try
            {
                if (ItemsProvider is null)
                {
                    var items = Items ?? [];

                    _viewItems = [.. _viewItems, .. items.Skip(_loadMoreSkip).Take(LoadMoreSize)];

                    _loadMoreFinished = _viewItems.Count >= items.Count;
                }
                else
                {
                    var result = await ProvideVirtualizedItems(new(_loadMoreSkip, LoadMoreSize, localCts.Token));

                    if (localCts.IsCancellationRequested is false)
                    {
                        _viewItems = [.. _viewItems, .. result.Items];

                        //_loadMoreFinished = _viewItems.Count >= result.TotalItemCount; // for performance purposes we won't use TotalItemCount here!
                        _loadMoreFinished = result.Items.Any() is false;
                    }
                }

                _loadMoreSkip += LoadMoreSize;
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == localCts.Token) { }
        }
        finally
        {
            _globalCts = null;
            localCts.Dispose();
        }

        StateHasChanged();
    }

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



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_globalCts is not null)
        {
            _globalCts.Dispose();
            _globalCts = null;
        }

        await base.DisposeAsync(disposing);
    }
}
