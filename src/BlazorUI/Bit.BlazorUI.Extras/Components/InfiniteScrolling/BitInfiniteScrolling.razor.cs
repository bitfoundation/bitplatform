namespace Bit.BlazorUI;

/// <summary>
/// BitInfiniteScrolling is a container that enables scrolling through a list of items infinitely as long as
/// there are items to fetch and render. It loads the next page through an items provider as soon as the
/// sentinel element at the end of the list enters the scroll viewport, and it ships with loading, empty, end
/// and error states, a manual (Load more) mode, a reversed (chat) mode that keeps the scroll position stable
/// while older items get prepended, and a horizontal mode. The loaded items can be capped, keyed, driven from
/// code and reset from a token.
/// </summary>
public partial class BitInfiniteScrolling<TItem> : BitComponentBase
{
    private List<TItem> _items = [];
    private Exception? _error;
    private bool _hasMore = true;
    private bool _endedByCap;
    private bool _retrying;
    private int? _totalCount;
    private bool _initialized;
    private int _loadVersion;
    private object? _refreshToken;
    private bool _refreshTokenRead;
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

    // The button loads the next page in manual mode and retries the page that failed in any mode. It stays in
    // the DOM (disabled) while the load it started runs, rather than vanishing under the keyboard focus that
    // pressed it and dropping that focus back to the top of the page.
    private bool _showButton => IsEnabled && (_error is not null || _retrying || (Manual && _hasMore && _initialized));

    // The label of that button, which keeps saying "retry" for as long as the retry it started is running.
    private string? _buttonText => (_error is not null || _retrying) ? RetryText : LoadMoreText;



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
    /// Lays the list out along the horizontal axis, so the pages are fetched while scrolling sideways and
    /// every scroll operation of the component works on the horizontal axis of its scroll container.
    /// </summary>
    /// <remarks>
    /// The root element becomes a flex row in this mode and the sentinel element is given a width instead of
    /// a height, which is what the LastElementWidth parameter sizes.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool Horizontal { get; set; }

    /// <summary>
    /// The function that returns a stable key for each item, which is rendered as the @key of that item.
    /// A keyed item is matched by its key instead of by its position, so a page prepended above the rendered
    /// items inserts new nodes rather than rewriting the content of every node below it.
    /// </summary>
    /// <remarks>
    /// Each item gets a wrapper element to carry the key, which is laid out with display:contents so it adds
    /// nothing to the layout. The keys have to be unique among the loaded items.
    /// </remarks>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

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
    /// The width of the last element that triggers the loading, which is the size along the scroll axis in
    /// the horizontal mode.
    /// </summary>
    [Parameter] public string? LastElementWidth { get; set; }

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
    /// An arbitrary value that resets the component whenever it changes: the loaded items are thrown away and
    /// the first page is fetched again from the items provider.
    /// </summary>
    /// <remarks>
    /// A provider written as a lambda that captures a filter hands over the same method on every render, so a
    /// changed filter cannot be detected from the delegate itself. Passing the filter (or anything derived
    /// from it) as this token is what tells the component that its data source now answers differently.
    /// </remarks>
    [Parameter] public object? RefreshToken { get; set; }

    /// <summary>
    /// Prepends each loaded page before the already rendered items and moves the sentinel element to the top of
    /// the list, so scrolling up loads the older items of a chat or a log while the scroll position stays put.
    /// </summary>
    /// <remarks>
    /// The list starts out scrolled to its newest items, and the root element becomes a flex column in this
    /// mode, which is how the status blocks are moved above the items without a second copy of the markup.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool Reversed { get; set; }

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
    [Parameter, ResetClassBuilder] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the component.
    /// </summary>
    [Parameter] public BitInfiniteScrollingClassStyles? Styles { get; set; }

    /// <summary>
    /// The threshold parameter for the IntersectionObserver that specifies a ratio of intersection area to total bounding box area of the last element.
    /// It defaults to 0, which fires as soon as a single pixel of the last element shows up; a ratio only means
    /// something for a last element that was given a real size along the scroll axis.
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
    /// The total number of the items of the data source, as it was last reported by a
    /// <see cref="BitInfiniteScrollingItemsProviderResult{T}"/> returned from the items provider,
    /// or null while no provider result carried one.
    /// </summary>
    public int? TotalCount => _totalCount;



    /// <summary>
    /// Refreshes the items and re-renders them from scratch.
    /// </summary>
    public async Task RefreshDataAsync()
    {
        CancelLoad();

        _items = [];
        _error = null;
        _hasMore = true;
        _endedByCap = false;
        _totalCount = null;
        _initialized = false;
        _initialScrollDone = false;
        _pendingScrollAdjust = false;
        _pendingScrollToEnd = false;

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

        // The cap counts everything the list holds, whoever put it there, so the surplus is dropped from the
        // end that was just written and the items that were already on screen keep their place.
        TrimToMaxItems(fromStart: false);

        var ended = CapHasMore();

        _initialized = true;

        StateHasChanged();

        await ReobserveAsync();

        if (ended)
        {
            await OnEnd.InvokeAsync();
        }
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

        TrimToMaxItems(fromStart: true);

        var ended = CapHasMore();

        _initialized = true;
        _pendingScrollAdjust = true;

        StateHasChanged();

        if (ended)
        {
            await OnEnd.InvokeAsync();
        }
    }

    /// <summary>
    /// Replaces every loaded item with the provided ones, without calling the items provider. It is the way to
    /// filter, sort, deduplicate or patch the loaded items in place, and to seed the list with items that were
    /// fetched elsewhere.
    /// </summary>
    /// <remarks>
    /// The Skip of the next provider request is the number of the loaded items, so a set that is shorter (or
    /// longer) than what was loaded moves the paging window along with it.
    /// </remarks>
    public async Task SetItemsAsync(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        // Everything the list held is being replaced, so a page that is still on its way would land on top of
        // a set it knows nothing about.
        CancelLoad();

        _items = [.. items];

        TrimToMaxItems(fromStart: false);

        var ended = CapHasMore();

        _initialized = true;

        StateHasChanged();

        await ReobserveAsync();

        if (ended)
        {
            await OnEnd.InvokeAsync();
        }
    }

    /// <summary>
    /// Removes the first occurrence of the provided item from the loaded items, and reports whether it was
    /// found. The list keeps loading from where it stopped.
    /// </summary>
    /// <remarks>
    /// The Skip of the next provider request is the number of the loaded items, so removing an item moves the
    /// paging window back by one. A data source that is paged by an index rather than by a cursor may hand
    /// over an item twice because of it.
    /// </remarks>
    public async Task<bool> RemoveItemAsync(TItem item)
    {
        if (_items.Remove(item) is false) return false;

        // A list that had stopped only because it was full has room for another page again. One that had
        // reached the end of its data stays where it is: what was removed is not what the provider still has.
        if (_endedByCap && IsMaxItemsReached() is false)
        {
            _hasMore = true;
            _endedByCap = false;
        }

        StateHasChanged();

        await ReobserveAsync();

        return true;
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

    /// <summary>
    /// Scrolls the scroll container to the provided offset, in pixels, along the scroll axis of the component.
    /// Together with GetScrollOffsetAsync it is what restores the position of a list the user is coming back to.
    /// </summary>
    public async Task ScrollToOffsetAsync(double offset, bool smooth = false)
    {
        if (IsRendered is false || IsDisposed) return;

        await _js.BitInfiniteScrollingScrollToOffset(UniqueId, offset, smooth);
    }

    /// <summary>
    /// Returns the current offset of the scroll container, in pixels, along the scroll axis of the component.
    /// </summary>
    public async Task<double> GetScrollOffsetAsync()
    {
        if (IsRendered is false || IsDisposed) return 0;

        return await _js.BitInfiniteScrollingGetScrollOffset(UniqueId);
    }



    // The built-in button is the one control the user operates, so a load it starts is remembered: that is
    // what keeps the button (and the focus on it) in place, and its label from flipping mid-flight.
    private async Task HandleLoadMoreClick()
    {
        _retrying = _error is not null;

        await LoadMoreItemsAsync();

        // A click that found nothing left to load leaves nothing to keep the button in place for either.
        if (_retrying && _isLoading is false)
        {
            _retrying = false;
            StateHasChanged();
        }
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
        var providerChanged = ReferenceEquals(ItemsProvider, _itemsProvider) is false
                              && ItemsProvider?.Method != _itemsProvider?.Method;

        // That same lambda keeps its method when what it captured changes, which is exactly the case the
        // refresh token is for: the data source is the same delegate but it now answers differently.
        var tokenChanged = _refreshTokenRead && Equals(RefreshToken, _refreshToken) is false;

        _itemsProvider = ItemsProvider;
        _refreshToken = RefreshToken;
        _refreshTokenRead = true;

        if (providerChanged || tokenChanged)
        {
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
        ClassBuilder.Register(() => Horizontal ? "bit-isc-hor" : string.Empty);
        // A component that scrolls with something else must not clip (and scroll) its own content on top of it.
        ClassBuilder.Register(() => ScrollerSelector.HasValue() ? "bit-isc-ext" : string.Empty);
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
            // A list that can never load anything (a cap of zero, say) is still a list that has been through
            // its loading: leaving it uninitialized would hide the empty state along with the loading one.
            _endedByCap = _endedByCap || IsMaxItemsReached();
            _hasMore = false;
            _initialized = true;
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

            // A provider that answers with a BitInfiniteScrollingItemsProviderResult says for itself where the
            // data ends and how much of it there is, which is what a cursor-paged source cannot express
            // through the size of the page it returns.
            var providerResult = result as BitInfiniteScrollingItemsProviderResult<TItem>;

            newItems = result as TItem[] ?? [.. result ?? []];

            if (providerResult?.TotalCount is not null)
            {
                _totalCount = providerResult.TotalCount;
            }

            if (providerResult?.HasMore is bool providerHasMore)
            {
                _hasMore = providerHasMore;
            }
            else if (newItems.Length == 0 || (requestCount > 0 && newItems.Length < requestCount))
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

            // A provider that ignores the requested count can still overshoot the cap; the surplus is
            // dropped from the far end of the page that was just inserted, so the items that were already
            // on screen keep their place.
            TrimToMaxItems(fromStart: Reversed);

            if (IsMaxItemsReached())
            {
                _hasMore = false;
                _endedByCap = true;
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
                _retrying = false;
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

    // A list that is full stops loading, and remembers that the cap - rather than the data - is what stopped
    // it. It reports whether this call is what ended it, which is what the OnEnd callback is told about.
    private bool CapHasMore()
    {
        if (_hasMore is false || IsMaxItemsReached() is false) return false;

        _hasMore = false;
        _endedByCap = true;

        return true;
    }

    private void CancelLoad()
    {
        _loadVersion++;

        if (_cts is null) return;

        var cts = _cts;
        _cts = null;

        // The source is deliberately not disposed here: the load that owns it may still be inside the
        // provider, and a token whose source is already disposed throws instead of reporting a cancellation.
        // The finally block of that load is what disposes it.
        cts.Cancel();
    }

    private void TrimToMaxItems(bool fromStart)
    {
        if (MaxItems is null || _items.Count <= MaxItems.Value) return;

        var surplus = _items.Count - MaxItems.Value;

        _items.RemoveRange(fromStart ? 0 : _items.Count - surplus, surplus);
    }

    private string BuildJsSetupKey() => $"{ScrollerSelector}|{Threshold}|{RootMargin}|{Reversed}|{Horizontal}|{Manual}|{IsEnabled}";

    private async Task SetupJsAsync()
    {
        if (IsDisposed || _dotnetObj is null) return;

        // A pending scroll correction observes the sentinel itself once the scroll is where it belongs, so the
        // observer must not start out watching a sentinel that is still sitting in the viewport.
        var autoLoad = _canAutoLoad && _pendingScrollToEnd is false && _pendingScrollAdjust is false;

        await _js.BitInfiniteScrollingSetup(UniqueId, ScrollerSelector, RootElement, _lastElementRef,
                                            Threshold, RootMargin, Horizontal, autoLoad, _dotnetObj);
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

        // The sentinel spans the cross axis of the list and is one pixel thick along the scroll axis, which
        // is the axis LastElementHeight (or, in the horizontal mode, LastElementWidth) thickens. Across a
        // horizontal row that cross size is left to the flex line to stretch: a percentage of the row, whose
        // own height is its content, would resolve to nothing and leave the observer an element with no area.
        var style = Horizontal
            ? $"width:{LastElementWidth ?? "1px"};height:{LastElementHeight ?? "auto"}"
            : $"height:{LastElementHeight ?? "1px"};width:{LastElementWidth ?? "100%"}";

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
        catch (OperationCanceledException) { } // the renderer is being torn down along with the component
        catch (ObjectDisposedException) { } // the JS runtime of a WebAssembly host may go first

        await base.DisposeAsync(disposing);
    }
}
