namespace Bit.BlazorUI;

/// <summary>
/// BitInfiniteScrolling is a container that enables scrolling through a list of items infinitely as long as there are items to fetch and render.
/// </summary>
public partial class BitInfiniteScrolling<TItem> : BitComponentBase, IAsyncDisposable
{
    private List<TItem> _currentItems = [];
    private CancellationTokenSource? _globalCts;
    private ElementReference _lastElementRef = default!;
    private BitInfiniteScrollingItemsProvider<TItem>? _itemsProvider;
    private DotNetObjectReference<BitInfiniteScrolling<TItem>>? _dotnetObj;

    private bool _isLoading => _globalCts is not null;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The custom template to render each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

    /// <summary>
    /// The item provider function that will be called when scrolling ends.
    /// </summary>
    [Parameter] public BitInfiniteScrollingItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>
    /// Alias for ChildContent.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The height of the last element that triggers the loading.
    /// </summary>
    [Parameter] public string? LastElementHeight { get; set; }

    /// <summary>
    /// The custom template to render while loading the new items.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Pre-loads the data at the initialization of the component. Useful in prerendering mode.
    /// </summary>
    [Parameter] public bool Preload { get; set; }

    /// <summary>
    /// The CSS selector of the scroll container, by default the root element of the component is selected for this purpose.
    /// </summary>
    [Parameter] public string? ScrollerSelector { get; set; }




    /// <summary>
    /// Refreshes the items and re-renders them from scratch.
    /// </summary>
    public async Task RefreshDataAsync()
    {
        _globalCts?.Cancel();
        _globalCts = null;

        _currentItems = [];
        await LoadMoreItems();
    }



    [JSInvokable("Load")]
    public async Task _LoadMoreItems()
    {
        await LoadMoreItems();
    }



    protected override async Task OnInitializedAsync()
    {
        if (Preload && ItemsProvider is not null)
        {
            var items = await ItemsProvider(new(0, CancellationToken.None));
            _currentItems.AddRange(items);
        }

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (ItemsProvider != _itemsProvider && (_itemsProvider is not null || _currentItems.Count == 0))
        {
            _currentItems = [];
        }

        _itemsProvider = ItemsProvider;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
            await _js.BitInfiniteScrollingSetup(_Id, ScrollerSelector, RootElement, _lastElementRef, _dotnetObj);
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    protected override string RootElementClass => "bit-isc";



    private async Task LoadMoreItems()
    {
        if (ItemsProvider is null || _globalCts is not null) return;

        var items = _currentItems;
        var localCts = new CancellationTokenSource();

        _globalCts = localCts;

        try
        {
            StateHasChanged();

            try
            {
                var newItems = await ItemsProvider(new(items.Count, localCts.Token));

                if (localCts.IsCancellationRequested is false)
                {
                    var length = items.Count;
                    items.AddRange(newItems);

                    if (items.Count != length)
                    {
                        await _js.BitInfiniteScrollingReobserve(_Id, _lastElementRef);
                    }
                }
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



    public async ValueTask DisposeAsync()
    {
        if (_globalCts is not null)
        {
            _globalCts.Dispose();
            _globalCts = null;
        }

        _dotnetObj?.Dispose();
        await _js.BitInfiniteScrollingDispose(_Id);
    }
}
