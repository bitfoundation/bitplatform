namespace Bit.BlazorUI;

/// <summary>
/// BitInfiniteScrolling is a container that enables scrolling through a list of items infinitely as long as
/// there are items to fetch and render. It loads the next page through an items provider as soon as the
/// sentinel element at the end of the list enters the scroll viewport, and it ships with loading, empty, end
/// and error states, a manual (Load more) mode, and a reversed (chat) mode that keeps the scroll position
/// stable while older items get prepended.
/// </summary>
public partial class BitInfiniteScrolling<TItem> : BitComponentBase
{
    private List<TItem> _items = [];
    private Exception? _error;
    private bool _hasMore = true;
    private bool _initialized;
    private int _loadVersion;
    private bool _pendingScrollAdjust;
    private bool _pendingScrollToEnd;
    private bool _initialScrollDone;
    private string? _jsSetupKey;
    private CancellationTokenSource? _cts;
    private ElementReference _lastElementRef = default!;
    private BitInfiniteScrollingItemsProvider<TItem>? _itemsProvider;
    private DotNetObjectReference<BitInfiniteScrolling<TItem>>? _dotnetObj;



    private bool _isLoading => _cts is not null;

    // The sentinel is only worth observing while another page can actually arrive: a failed load waits for a
    // retry, the manual mode waits for the button, and a disabled component (or one with no provider) loads
    // nothing at all. Observing it in any of those states would make the browser ask for a page that the
    // component immediately declines, over and over.
    private bool _canAutoLoad => IsEnabled && ItemsProvider is not null && Manual is false && _hasMore && _error is null;

    private bool _showEmpty => _initialized && _isLoading is false && _error is null && _items.Count == 0;

    private bool _showEnd => _initialized && _isLoading is false && _error is null && _hasMore is false && _items.Count > 0
                             && (EndTemplate is not null || EndMessage.HasValue());

    // The button loads the next page in manual mode and retries the page that failed in any mode.
    private bool _showButton => IsEnabled && _isLoading is false && (_error is not null || (Manual && _hasMore && _initialized));



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The custom template to render each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the component.
    /// </summary>
    [Parameter] public BitInfiniteScrollingClassStyles? Classes { get; set; }

    /// <summary>
    /// The message to render when there is no item available and no EmptyTemplate is provided.
    /// </summary>
    [Parameter] public string EmptyMessage { get; set; } = "There is no item";

    /// <summary>
    /// The custom template to render when there is no item available.
    /// </summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// The message to render after the last page, when there is no more item to fetch.
    /// Nothing is rendered while both this parameter and the EndTemplate are empty.
    /// </summary>
    [Parameter] public string? EndMessage { get; set; }

    /// <summary>
    /// The custom template to render after the last page, when there is no more item to fetch.
    /// </summary>
    [Parameter] public RenderFragment? EndTemplate { get; set; }

    /// <summary>
    /// The message to render when the items provider throws and no ErrorTemplate is provided.
    /// </summary>
    [Parameter] public string ErrorMessage { get; set; } = "Failed to load the items.";

    /// <summary>
    /// The custom template to render when the items provider throws, receiving the thrown exception as its context.
    /// Providing this template replaces the default error message and its retry button, so the template itself
    /// should offer a way to call the RefreshDataAsync or LoadMoreAsync methods of the component.
    /// </summary>
    [Parameter] public RenderFragment<Exception>? ErrorTemplate { get; set; }

    /// <summary>
    /// The item provider function that will be called when scrolling ends.
    /// </summary>
    [Parameter] public BitInfiniteScrollingItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>
    /// Alias for ChildContent.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The CSS class of the last element that triggers the loading.
    /// </summary>
    [Parameter] public string? LastElementClass { get; set; }

    /// <summary>
    /// The height of the last element that triggers the loading.
    /// </summary>
    [Parameter] public string? LastElementHeight { get; set; }

    /// <summary>
    /// The CSS style of the last element that triggers the loading.
    /// </summary>
    [Parameter] public string? LastElementStyle { get; set; }

    /// <summary>
    /// The message to render while loading the new items and no LoadingTemplate is provided.
    /// </summary>
    [Parameter] public string LoadingMessage { get; set; } = "Loading...";

    /// <summary>
    /// The custom template to render while loading the new items.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// The custom template of the button that loads the next page in the manual mode and retries a failed load.
    /// </summary>
    [Parameter] public RenderFragment? LoadMoreTemplate { get; set; }

    /// <summary>
    /// The text of the button that loads the next page in the manual mode.
    /// </summary>
    [Parameter] public string LoadMoreText { get; set; } = "Load more";

    /// <summary>
    /// Replaces the automatic loading with an explicit button, so each page is fetched only when the user asks for it.
    /// This is the accessible fallback of an infinite list, and it also keeps the footer of a page reachable.
    /// </summary>
    [Parameter] public bool Manual { get; set; }

    /// <summary>
    /// The maximum number of items to load. The component stops fetching new pages as soon as the number of
    /// the loaded items reaches this value, and the last page it requests is narrowed down to what is still
    /// missing, so the list never grows beyond the cap.
    /// </summary>
    [Parameter] public int? MaxItems { get; set; }

    /// <summary>
    /// The callback that is invoked when the last page is loaded and there is no more item to fetch.
    /// </summary>
    [Parameter] public EventCallback OnEnd { get; set; }

    /// <summary>
    /// The callback that is invoked when the items provider throws, receiving the thrown exception.
    /// </summary>
    [Parameter] public EventCallback<Exception> OnError { get; set; }

    /// <summary>
    /// The callback that is invoked after each successful load, receiving the newly loaded items of that page.
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> OnItemsLoaded { get; set; }

    /// <summary>
    /// The number of the items to request in each page, which is sent to the items provider as the Count of its request.
    /// A provider returning fewer items than this value is considered the last page, which saves the extra
    /// round trip an empty page would otherwise cost.
    /// </summary>
    [Parameter] public int PageSize { get; set; }

    /// <summary>
    /// Pre-loads the data at the initialization of the component. Useful in prerendering mode.
    /// </summary>
    [Parameter] public bool Preload { get; set; }

    /// <summary>
    /// Prepends each loaded page before the already rendered items and moves the sentinel element to the top of
    /// the list, so scrolling up loads the older items of a chat or a log while the scroll position stays put.
    /// </summary>
    /// <remarks>
    /// The list starts out scrolled to its newest items, and the root element becomes a flex column in this
    /// mode, which is how the status blocks are moved above the items without a second copy of the markup.
    /// </remarks>
    [Parameter] public bool Reversed { get; set; }

    /// <summary>
    /// The rootMargin parameter of the IntersectionObserver, which grows (or shrinks) the area around the scroll
    /// viewport that the last element is checked against. A value like "200px" starts loading the next page 200
    /// pixels before the end of the list becomes visible.
    /// </summary>
    [Parameter] public string? RootMargin { get; set; }

    /// <summary>
    /// The text of the button that retries the failed load.
    /// </summary>
    [Parameter] public string RetryText { get; set; } = "Retry";

    /// <summary>
    /// The CSS selector of the scroll container, by default the root element of the component is selected for this purpose.
    /// The "window", "document", "body" and "html" values all select the viewport of the page itself.
    /// </summary>
    [Parameter] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the component.
    /// </summary>
    [Parameter] public BitInfiniteScrollingClassStyles? Styles { get; set; }

    /// <summary>
    /// The threshold parameter for the IntersectionObserver that specifies a ratio of intersection area to total bounding box area of the last element.
    /// It defaults to 0, which fires as soon as a single pixel of the last element shows up; a ratio only means
    /// something for a last element that was given a real height.
    /// </summary>
    [Parameter] public decimal? Threshold { get; set; }



    /// <summary>
    /// The exception of the last failed load, or null while the last load succeeded.
    /// </summary>
    public Exception? Error => _error;

    /// <summary>
    /// Determines whether another page can still be fetched from the items provider.
    /// </summary>
    public bool HasMore => _hasMore;

    /// <summary>
    /// Determines whether a page is currently being fetched from the items provider.
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// The items loaded so far, in the order they are rendered.
    /// </summary>
    public IReadOnlyList<TItem> Items => _items;



    /// <summary>
    /// Refreshes the items and re-renders them from scratch.
    /// </summary>
    public async Task RefreshDataAsync()
    {
        CancelLoad();

        _items = [];
        _error = null;
        _hasMore = true;
        _initialized = false;
        _initialScrollDone = false;

        StateHasChanged();

        await LoadMoreItemsAsync();
    }

    /// <summary>
    /// Loads the next page of the items, the same way reaching the end of the list does.
    /// It is the way to load the next page in the manual mode, and to retry a failed load.
    /// </summary>
    public Task LoadMoreAsync() => LoadMoreItemsAsync();

    /// <summary>
    /// Appends the provided items to the end of the already loaded items, without calling the items provider.
    /// Useful for pushing newly arrived items into the list.
    /// </summary>
    /// <remarks>
    /// The Skip of the next provider request is the number of the loaded items, so it grows along with what
    /// is pushed in here.
    /// </remarks>
    public async Task AppendItemsAsync(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _items.AddRange(items);
        _initialized = true;

        StateHasChanged();

        await ReobserveAsync();
    }

    /// <summary>
    /// Prepends the provided items to the beginning of the already loaded items, without calling the items provider.
    /// The scroll position is kept stable, so the visible items do not jump away.
    /// </summary>
    /// <remarks>
    /// The Skip of the next provider request is the number of the loaded items, so it grows along with what
    /// is pushed in here.
    /// </remarks>
    public async Task PrependItemsAsync(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        await PrepareScrollAsync();

        _items.InsertRange(0, items);
        _initialized = true;
        _pendingScrollAdjust = true;

        StateHasChanged();
    }

    /// <summary>
    /// Scrolls the scroll container to its top.
    /// </summary>
    public async Task ScrollToTopAsync(bool smooth = false)
    {
        if (IsRendered is false || IsDisposed) return;

        await _js.BitInfiniteScrollingScrollTo(UniqueId, false, smooth);
    }

    /// <summary>
    /// Scrolls the scroll container to its bottom. Useful in the reversed (chat) mode.
    /// </summary>
    public async Task ScrollToBottomAsync(bool smooth = false)
    {
        if (IsRendered is false || IsDisposed) return;

        await _js.BitInfiniteScrollingScrollTo(UniqueId, true, smooth);
    }



    [JSInvokable("Load")]
    public async Task _LoadMoreItems()
    {
        if (IsDisposed) return;

        await LoadMoreItemsAsync();
    }



    protected override async Task OnInitializedAsync()
    {
        _itemsProvider = ItemsProvider;

        if (Preload)
        {
            await LoadMoreItemsAsync();
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        // A parent that writes its provider as a lambda hands over a new delegate instance on every render,
        // so only a change of the underlying method counts as a new data source. Comparing the delegates
        // themselves would otherwise wipe the loaded items on every single render of the parent.
        if (ReferenceEquals(ItemsProvider, _itemsProvider) is false && ItemsProvider?.Method != _itemsProvider?.Method)
        {
            _itemsProvider = ItemsProvider;

            if (_initialized || _items.Count > 0)
            {
                await RefreshDataAsync();
            }
            else if (Manual)
            {
                // Nothing was loaded yet and there is no observer to fire in the manual mode, so the first
                // page of the provider that just arrived is fetched the way the first render would have.
                await LoadMoreItemsAsync();
            }
            else
            {
                // A provider that arrives after the first render finds the sentinel unobserved, since there
                // was nothing to load it for.
                await ReobserveAsync();
            }
        }
        else
        {
            _itemsProvider = ItemsProvider;
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
            _jsSetupKey = BuildJsSetupKey();
            await SetupJsAsync();
            await LoadFirstManualPageAsync();
        }
        else
        {
            // The observer is created once with its scroller, threshold and margin baked in, so a change of
            // any of them has to rebuild it rather than silently keep observing with the previous options.
            var key = BuildJsSetupKey();
            if (key != _jsSetupKey)
            {
                _jsSetupKey = key;
                await SetupJsAsync();
                await LoadFirstManualPageAsync();
            }
        }

        // A reversed list corrects its scroll position after the render that inserted the items, and only then
        // starts watching the sentinel again: the sentinel sits at the top, so observing it before the
        // correction would ask for the next page while it is still in view.
        if (_pendingScrollToEnd)
        {
            _pendingScrollToEnd = false;
            _pendingScrollAdjust = false;
            await ScrollToBottomAsync();
            await ReobserveAsync();
        }
        else if (_pendingScrollAdjust)
        {
            _pendingScrollAdjust = false;
            await RestoreScrollAsync();
            await ReobserveAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
        ClassBuilder.Register(() => Reversed ? "bit-isc-rev" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }



    protected override string RootElementClass => "bit-isc";



    private async Task LoadMoreItemsAsync()
    {
        // A load that is already running re-observes the sentinel when it finishes, so this call has nothing
        // left to arrange.
        if (IsDisposed || _isLoading) return;

        if (ItemsProvider is null || IsEnabled is false) return;

        if (_hasMore is false || IsMaxItemsReached())
        {
            _hasMore = false;
            await UnobserveAsync();
            return;
        }

        // Each load carries a version so that a refresh (or a disposal) that happens while the provider is
        // still running can tell the stale completion apart from the current one, instead of letting it
        // clear the state the newer load has already installed.
        var version = ++_loadVersion;
        var cts = new CancellationTokenSource();

        _cts = cts;
        _error = null;

        StateHasChanged();

        TItem[]? newItems = null;

        // MaxItems narrows the page down to what is still allowed, so the provider is never asked for items
        // the component would have to throw away.
        var requestCount = Math.Max(0, PageSize);
        if (MaxItems is not null)
        {
            var allowed = MaxItems.Value - _items.Count;
            requestCount = requestCount == 0 ? allowed : Math.Min(requestCount, allowed);
        }

        try
        {
            var result = await ItemsProvider(new BitInfiniteScrollingItemsProviderRequest(_items.Count, requestCount, cts.Token));

            if (version != _loadVersion || cts.IsCancellationRequested || IsDisposed) return;

            newItems = result as TItem[] ?? [.. result ?? []];

            if (newItems.Length == 0 || (requestCount > 0 && newItems.Length < requestCount))
            {
                _hasMore = false;
            }

            if (newItems.Length > 0)
            {
                if (Reversed && _initialScrollDone is false)
                {
                    // The sentinel of a reversed list sits at its top, which is exactly where a fresh scroll
                    // container starts out. Landing on the newest items (the way a chat opens) both shows the
                    // right end of the data and moves the sentinel out of view, so the next page waits for
                    // the user to scroll up instead of being requested immediately.
                    _initialScrollDone = true;
                    _pendingScrollToEnd = true;
                    _items.InsertRange(0, newItems);
                }
                else if (Reversed)
                {
                    // Record the scroll geometry before the DOM grows at the top, so the correction that runs
                    // after the render can put the viewport back on the item the user was looking at.
                    await PrepareScrollAsync();
                    _pendingScrollAdjust = true;
                    _items.InsertRange(0, newItems);
                }
                else
                {
                    _items.AddRange(newItems);
                }
            }

            if (MaxItems is not null && _items.Count > MaxItems.Value)
            {
                // A provider that ignores the requested count can still overshoot the cap; the surplus is
                // dropped from the far end of the page that was just inserted, so the items that were
                // already on screen keep their place.
                var surplus = _items.Count - MaxItems.Value;
                _items.RemoveRange(Reversed ? 0 : _items.Count - surplus, surplus);
            }

            if (IsMaxItemsReached())
            {
                _hasMore = false;
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // The load was superseded by a refresh or by the disposal of the component.
        }
        catch (Exception ex)
        {
            if (version == _loadVersion)
            {
                _error = ex;
            }
        }
        finally
        {
            if (version == _loadVersion)
            {
                _cts = null;
                _initialized = true;
            }

            cts.Dispose();
        }

        if (version != _loadVersion || IsDisposed) return;

        StateHasChanged();

        if (_error is not null)
        {
            // A failed page keeps the sentinel unobserved, otherwise a server that is down turns the list
            // into a retry storm; the retry button (or LoadMoreAsync) is what resumes the loading.
            await UnobserveAsync();
            await OnError.InvokeAsync(_error);
            return;
        }

        if (_pendingScrollToEnd is false && _pendingScrollAdjust is false)
        {
            await ReobserveAsync();
        }

        if (newItems is { Length: > 0 })
        {
            await OnItemsLoaded.InvokeAsync(newItems);
        }

        if (_hasMore is false)
        {
            await OnEnd.InvokeAsync();
        }
    }

    private bool IsMaxItemsReached() => MaxItems is not null && _items.Count >= MaxItems.Value;

    private void CancelLoad()
    {
        _loadVersion++;

        if (_cts is null) return;

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private string BuildJsSetupKey() => $"{ScrollerSelector}|{Threshold}|{RootMargin}|{Reversed}|{Manual}|{IsEnabled}";

    private async Task SetupJsAsync()
    {
        if (IsDisposed || _dotnetObj is null) return;

        // A pending scroll correction observes the sentinel itself once the scroll is where it belongs, so the
        // observer must not start out watching a sentinel that is still sitting in the viewport.
        var autoLoad = _canAutoLoad && _pendingScrollToEnd is false && _pendingScrollAdjust is false;

        await _js.BitInfiniteScrollingSetup(UniqueId, ScrollerSelector, RootElement, _lastElementRef,
                                            Threshold, RootMargin, autoLoad, _dotnetObj);
    }

    // In the automatic mode the first page arrives because the sentinel starts out visible; the manual mode has
    // no observer to fire, so its first page is fetched here and every next one by the button.
    private async Task LoadFirstManualPageAsync()
    {
        if (Manual is false || _initialized || _isLoading || ItemsProvider is null) return;

        await LoadMoreItemsAsync();
    }

    private async Task ReobserveAsync()
    {
        if (IsRendered is false || IsDisposed) return;

        if (_canAutoLoad is false)
        {
            await UnobserveAsync();
            return;
        }

        await _js.BitInfiniteScrollingReobserve(UniqueId, _lastElementRef);
    }

    private async Task UnobserveAsync()
    {
        if (IsRendered is false || IsDisposed) return;

        await _js.BitInfiniteScrollingUnobserve(UniqueId);
    }

    private async Task PrepareScrollAsync()
    {
        if (IsRendered is false || IsDisposed) return;

        await _js.BitInfiniteScrollingPrepareScroll(UniqueId);
    }

    private async Task RestoreScrollAsync()
    {
        if (IsRendered is false || IsDisposed) return;

        await _js.BitInfiniteScrollingRestoreScroll(UniqueId);
    }

    private string GetLastElementStyle()
    {
        // The sentinel only exists to be watched: while it cannot trigger anything (loading, manual mode,
        // an error, or the end of the data) it is taken out of the layout so it neither adds height nor
        // keeps intersecting the viewport.
        if (_canAutoLoad is false || _isLoading) return "display:none";

        var style = $"height:{LastElementHeight ?? "1px"};width:100%";

        return JoinStyles(JoinStyles(style, LastElementStyle), Styles?.LastElement)!;
    }

    private string? GetLastElementClass() => JoinClasses(JoinClasses("bit-isc-lst", LastElementClass), Classes?.LastElement);

    // What the live region announces. The error has its own alert element, which is announced by being
    // inserted, so it is deliberately not repeated here.
    private string? GetStatusMessage()
    {
        if (_isLoading) return LoadingMessage;

        if (_showEmpty) return EmptyMessage;

        if (_showEnd) return EndMessage;

        return null;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        CancelLoad();

        _dotnetObj?.Dispose();
        _dotnetObj = null;

        try
        {
            await _js.BitInfiniteScrollingDispose(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
