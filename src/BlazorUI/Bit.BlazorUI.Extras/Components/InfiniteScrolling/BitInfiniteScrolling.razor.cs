namespace Bit.BlazorUI;

/// <summary>
/// BitInfiniteScrolling is a container that enables scrolling through a list of items infinitely as long as there are items to fetch and render.
/// </summary>
public partial class BitInfiniteScrolling<TItem> : BitComponentBase, IAsyncDisposable
{
    private List<TItem> _items = [];
    private bool _enumerationCompleted;
    private CancellationTokenSource? _loadItemsCts;
    private ElementReference _lastElementRef = default!;
    private DotNetObjectReference<BitInfiniteScrolling<TItem>>? _dotnetObj;
    private BitInfiniteScrollingItemsProviderRequestDelegate<TItem>? _itemsProvider;

    private bool IsLoading => _loadItemsCts != null;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    [Parameter] public BitInfiniteScrollingItemsProviderRequestDelegate<TItem>? ItemsProvider { get; set; }

    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    [Parameter] public RenderFragment? LoadingTemplate { get; set; }



    public async Task RefreshDataAsync()
    {
        _loadItemsCts?.Cancel();
        _loadItemsCts = null;

        _items = [];
        _enumerationCompleted = false;
        await LoadMoreItems();
    }



    [JSInvokable("Load")]
    public async Task _LoadMoreItems()
    {
        await LoadMoreItems();
    }



    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (ItemsProvider != _itemsProvider)
        {
            _items = [];
            _enumerationCompleted = false;
        }

        _itemsProvider = ItemsProvider;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
            await _js.BitInfiniteScrollingSetup(_Id, RootElement, _lastElementRef, _dotnetObj);
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    protected override string RootElementClass => "bit-isc";

    protected override void RegisterCssClasses()
    {

    }


    private async Task LoadMoreItems()
    {
        if (_loadItemsCts != null) return;

        if (ItemsProvider == null) return;

        var items = _items;
        var cts = new CancellationTokenSource();
        _loadItemsCts = cts;

        try
        {
            StateHasChanged(); // Allow the UI to display the loading indicator
            try
            {
                var newItems = await ItemsProvider(new(items.Count, cts.Token));
                if (cts.IsCancellationRequested is false)
                {
                    var length = items.Count;
                    items.AddRange(newItems);

                    if (items.Count == length)
                    {
                        _enumerationCompleted = true;
                    }
                    else
                    {
                        await _js.BitInfiniteScrollingReobserve(_Id, _lastElementRef);
                    }
                }
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == cts.Token)
            {
                // No-op; we canceled the operation, so it's fine to suppress this exception.
            }
        }
        finally
        {
            _loadItemsCts = null;
            cts.Dispose();
        }

        StateHasChanged(); // Display the new items and hide the loading indicator
    }



    public async ValueTask DisposeAsync()
    {
        _dotnetObj?.Dispose();

        if (_loadItemsCts is not null)
        {
            _loadItemsCts.Dispose();
            _loadItemsCts = null;
        }


        _dotnetObj?.Dispose();
    }
}
