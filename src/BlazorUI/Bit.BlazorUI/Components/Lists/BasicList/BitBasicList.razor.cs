namespace Bit.BlazorUI;

/// <summary>
/// BitBasicList provides a base component for rendering large sets of items. It’s agnostic of layout, the tile component used, and selection management.
/// </summary>
public partial class BitBasicList<TItem> : BitComponentBase
{
    private int _loadMoreSkip = 0;
    private long _lastFetchTime;
    private bool _isLoadingMore;
    private bool _loadMoreFinished;
    private bool _autoLoadRegistered;
    private bool _internalLoadMore;
    private bool _internalVirtualize;
    private int _internalLoadMoreSize = 20;
    private ICollection<TItem> _viewItems = [];
    private string? _autoLoadMargin = null;
    private CancellationTokenSource? _globalCts;
    private ICollection<TItem>? _internalItems = null;
    private ElementReference _sentinelElement = default!;
    private RenderFragment<TItem>? _defaultRowTemplate = null;
    private DotNetObjectReference<BitBasicList<TItem>>? _dotnetObj = null;
    private _BitBasicListVirtualize<TItem>? _bitBasicListVirtualizeRef;
    private BitBasicListItemsProvider<TItem>? _internalItemsProvider = null;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Renders each item as its own text when no <see cref="RowTemplate"/> is provided, so a list of
    /// plain values shows up without a template of its own.
    /// </summary>
    /// <remarks>
    /// The text is wrapped in an element of its own so that each item stays a single element child of the
    /// list, which is what <see cref="ScrollToIndexAsync"/> counts the items of the list by.
    /// </remarks>
    private RenderFragment<TItem> _EffectiveRowTemplate => RowTemplate
        ?? (_defaultRowTemplate ??= item => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, item?.ToString());
            builder.CloseElement();
        });

    private bool _ShowLoading => Loading || (_isLoadingMore && _viewItems.Count == 0);

    private bool _ShowSentinel => LoadMore && AutoLoad && IsEnabled && _loadMoreFinished is false;



    /// <summary>
    /// Loads the next page as soon as the end of the loaded items scrolls into view, turning the
    /// LoadMore button into an infinite scrolling list.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This parameter only has an effect while <see cref="LoadMore"/> is enabled, since the pages it
    /// loads are the pages of the LoadMore mode. The LoadMore button keeps being rendered so the list
    /// stays usable where the browser has no IntersectionObserver and so a page that failed to load
    /// can still be asked for by hand; render a <see cref="LoadMoreTemplate"/> of a loading indicator
    /// to show a spinner in its place instead.
    /// </remarks>
    [Parameter] public bool AutoLoad { get; set; }

    /// <summary>
    /// How many pixels before the end of the loaded items the next page starts loading in <see cref="AutoLoad"/> mode.
    /// <br />
    /// The default value is <strong>0</strong>.
    /// </summary>
    /// <remarks>
    /// A positive value asks for the next page while the end is still that many pixels away, which is
    /// what keeps a fast scroll from reaching an empty region before the page arrives. A value of zero
    /// only loads once the end is actually reached.
    /// </remarks>
    [Parameter] public int AutoLoadThreshold { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the list.
    /// </summary>
    [Parameter] public BitBasicListClassStyles? Classes { get; set; }

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
    /// The content rendered below the items of the list, inside its scrolling region.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

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
    /// The content rendered above the items of the list, inside its scrolling region.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Lays the items of the list out in a row and scrolls it sideways instead of down.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Virtualization is vertical only, so this parameter is ignored while <see cref="Virtualize"/> is enabled.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Horizontal { get; set; }

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
    /// <remarks>
    /// The provider always takes priority over <see cref="Items"/>. In <see cref="Virtualize"/> mode it is
    /// called for the region that is about to become visible, in <see cref="LoadMore"/> mode for one page at
    /// a time, and in the plain mode it is called once for the whole set (with a Count of
    /// <see cref="int.MaxValue"/>), since there is nothing there to page or window the request.
    /// </remarks>
    [Parameter] public BitBasicListItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>
    /// The number of milliseconds the list waits before calling the <see cref="ItemsProvider"/> in <see cref="Virtualize"/> mode.
    /// <br />
    /// The default value is <strong>100</strong>.
    /// </summary>
    /// <remarks>
    /// Scrolling asks for a new region far more often than the regions actually differ, so the requests are
    /// debounced by this delay: a request that is superseded before the delay elapses is dropped instead of
    /// being sent. Scrolling that never settles would drop every one of them, so the delay is skipped once
    /// the provider has been left unasked for longer than twice this value, which keeps items arriving even
    /// then. A value of zero turns the debouncing off, which suits an in-memory provider that answers
    /// instantly. This delay never applies to the pages of the <see cref="LoadMore"/> mode, since those are
    /// asked for one deliberate click at a time.
    /// </remarks>
    [Parameter] public int ItemsProviderDelay { get; set; } = 100;

    /// <summary>
    /// Shows the loading content of the list in place of its items.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The list also shows this content on its own while the first page of a <see cref="LoadMore"/> list or
    /// the items of a plain <see cref="ItemsProvider"/> list are still being fetched, so this parameter is
    /// only needed for the loading the surrounding page does itself.
    /// </remarks>
    [Parameter] public bool Loading { get; set; }

    /// <summary>
    /// The template rendered while the list is loading its items.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

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
    /// <remarks>
    /// The context of the template is whether a page is being loaded at that moment, which is what a template
    /// showing a spinner in place of its label reads. A template is rendered inside a plain clickable element
    /// rather than inside a button, so it is free to bring a button (or any other control) of its own along.
    /// </remarks>
    [Parameter] public RenderFragment<bool>? LoadMoreTemplate { get; set; }

    /// <summary>
    /// The custom text of the default LoadMore button. Defaults to "Load more".
    /// </summary>
    [Parameter] public string? LoadMoreText { get; set; } = "Load more";

    /// <summary>
    /// The callback that is invoked when the list starts and stops loading its items.
    /// </summary>
    [Parameter] public EventCallback<bool> OnLoadingChange { get; set; }

    /// <summary>
    /// The callback that is invoked after each page of the LoadMore mode has been appended, with the number
    /// of items the list holds at that point.
    /// </summary>
    [Parameter] public EventCallback<int> OnLoadMore { get; set; }

    /// <summary>
    /// A value that determines how many additional items will be rendered before and after the visible region in Virtualize mode.
    /// </summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>
    /// The role attribute of the html element of the list. Defaults to "list".
    /// </summary>
    /// <remarks>
    /// The rows of the list are the markup of the <see cref="RowTemplate"/>, so a role of "list" only describes
    /// the element correctly where that template renders a row of role "listitem" (an <c>li</c> element, for
    /// one). Set this to null to leave the role off altogether where the rows carry a structure of their own.
    /// </remarks>
    [Parameter] public string? Role { get; set; } = "list";

    /// <summary>
    /// The template to render each row.
    /// </summary>
    /// <remarks>
    /// Without a template each item is rendered as its own text.
    /// </remarks>
    [Parameter] public RenderFragment<TItem>? RowTemplate { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the list.
    /// </summary>
    [Parameter] public BitBasicListClassStyles? Styles { get; set; }

    /// <summary>
    /// Enables virtualization in rendering the list.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Virtualize { get; set; }

    /// <summary>
    /// The template for items that have not yet rendered.
    /// </summary>
    [Parameter] public RenderFragment<PlaceholderContext>? VirtualizePlaceholder { get; set; }



    /// <summary>
    /// Reloads the items of the list.
    /// </summary>
    /// <remarks>
    /// What that means depends on the mode of the list: a LoadMore list drops the pages it has loaded and loads
    /// the first one again, a virtualized list re-requests the region it currently shows from its
    /// <see cref="ItemsProvider"/>, a plain provider list fetches the whole set again, and a plain list of
    /// <see cref="Items"/> simply picks up the current contents of that collection, which is what a list bound
    /// to a collection that is mutated in place (rather than replaced) needs in order to notice.
    /// </remarks>
    // The rendering the reload ends in belongs to the renderer, so the whole of it is dispatched there:
    // the method is public, so the call can just as well come from a thread of the caller's own.
    public Task RefreshDataAsync() => InvokeAsync(async () =>
    {
        if (IsDisposed) return;

        if (LoadMore)
        {
            await LoadMoreItems(true);
            return;
        }

        if (ItemsProvider is not null)
        {
            if (Virtualize)
            {
                if (_bitBasicListVirtualizeRef is null) return;

                await _bitBasicListVirtualizeRef.RefreshDataAsync();
            }
            else
            {
                await LoadAllItems();
            }

            StateHasChanged();
            return;
        }

        _viewItems = Items ?? [];
        StateHasChanged();
    });

    /// <summary>
    /// Loads the next page of the LoadMore mode, the same way clicking the LoadMore button does.
    /// </summary>
    /// <remarks>
    /// The call is a no-op where the list is not in LoadMore mode, where every page has already been loaded,
    /// or where a page is being loaded at that moment.
    /// </remarks>
    public Task LoadMoreAsync() => InvokeAsync(() => LoadMoreItems(false));

    /// <summary>
    /// Scrolls the list to its start.
    /// </summary>
    /// <param name="smooth">Animates the scrolling instead of jumping to the offset.</param>
    public async Task ScrollToStartAsync(bool smooth = false) => await ScrollToOffsetAsync(0, smooth);

    /// <summary>
    /// Scrolls the list to its end.
    /// </summary>
    /// <param name="smooth">Animates the scrolling instead of jumping to the offset.</param>
    public async Task ScrollToEndAsync(bool smooth = false)
    {
        if (IsRendered is false || IsDisposed) return;

        await _js.BitUtilsScrollToEnd(RootElement, _IsHorizontal, smooth);
    }

    /// <summary>
    /// Scrolls the list to an absolute offset in pixels on its scrolling axis.
    /// </summary>
    /// <param name="offset">The offset in pixels to scroll to.</param>
    /// <param name="smooth">Animates the scrolling instead of jumping to the offset.</param>
    public async Task ScrollToOffsetAsync(double offset, bool smooth = false)
    {
        if (IsRendered is false || IsDisposed) return;

        await _js.BitUtilsScrollToOffset(RootElement, offset, _IsHorizontal, smooth);
    }

    /// <summary>
    /// Scrolls the list so that the item at the given index sits at its start edge.
    /// </summary>
    /// <param name="index">The zero based index of the item to scroll to.</param>
    /// <param name="smooth">Animates the scrolling instead of jumping to the offset.</param>
    /// <remarks>
    /// A virtualized list only holds the items around the visible region in the DOM, so the offset of the item
    /// is calculated from <see cref="ItemSize"/> there. Everywhere else the offset is measured off the rendered
    /// item itself, so items of differing sizes land accurately.
    /// </remarks>
    public async Task ScrollToIndexAsync(int index, bool smooth = false)
    {
        if (IsRendered is false || IsDisposed) return;
        if (index < 0) return;

        // The header is rendered as a single element before the items, so it shifts every one of them by
        // one child. Measuring off a child rather than off the container keeps the header (whose height is
        // its own content's business) out of the calculation.
        var headerOffset = HeaderTemplate is not null ? 1 : 0;

        if (Virtualize)
        {
            // The items start after the spacer the virtualization renders in their place, so that is the
            // child the offset of the item is measured from.
            await _js.BitUtilsScrollToChild(RootElement, headerOffset, index * ItemSize, _IsHorizontal, smooth);
        }
        else
        {
            await _js.BitUtilsScrollToChild(RootElement, headerOffset + index, 0, _IsHorizontal, smooth);
        }
    }



    [JSInvokable("OnIntersect")]
    public async Task _OnIntersect()
    {
        if (IsDisposed) return;
        if (_ShowSentinel is false) return;

        await InvokeAsync(async () => await LoadMoreItems(false));
    }



    protected override string RootElementClass => "bit-bsl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => _IsHorizontal ? "bit-bsl-hrz" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => (FullSize || FullWidth) ? "width:100%" : string.Empty);
        StyleBuilder.Register(() => (FullSize || FullHeight) ? "height:100%" : string.Empty);

        StyleBuilder.Register(() => (FitSize || FitWidth) ? "width:fit-content" : string.Empty);
        StyleBuilder.Register(() => (FitSize || FitHeight) ? "height:fit-content" : string.Empty);
    }

    protected override async Task OnParametersSetAsync()
    {
        // Every one of these decides where the rendered items come from, so a change in any of them has
        // to start the loading over rather than leave the list showing what the previous mode had loaded.
        var sourceChanged = _internalItems != Items
                         || _internalItemsProvider != ItemsProvider
                         || _internalLoadMore != LoadMore
                         || _internalLoadMoreSize != LoadMoreSize
                         || _internalVirtualize != Virtualize;

        if (sourceChanged)
        {
            _internalItems = Items;
            _internalLoadMore = LoadMore;
            _internalVirtualize = Virtualize;
            _internalLoadMoreSize = LoadMoreSize;
            _internalItemsProvider = ItemsProvider;

            if (LoadMore)
            {
                await LoadMoreItems(true);
            }
            else if (ItemsProvider is null) // ItemsProvider always has priority over Items
            {
                _viewItems = Items ?? [];
            }
            else if (Virtualize is false)
            {
                // Neither mode windows the request, so the whole set is fetched at once. Without this the
                // list would silently stay empty, since nothing else ever calls the provider here.
                await LoadAllItems();
            }
            else
            {
                // The virtualized provider mode renders straight off the provider, so nothing is held here.
                _viewItems = [];
            }
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsDisposed) return;

        if (_ShowSentinel)
        {
            var margin = $"{Math.Max(0, AutoLoadThreshold)}px";

            // A margin of its own is baked into the observer, so a changed threshold only takes effect by
            // registering again, which replaces the observer registered under the same id.
            if (_autoLoadRegistered is false || _autoLoadMargin != margin)
            {
                _autoLoadRegistered = true;
                _autoLoadMargin = margin;
                _dotnetObj ??= DotNetObjectReference.Create(this);

                await _js.BitObserversRegisterIntersection(_Id, _sentinelElement, _dotnetObj, margin);
            }
        }
        else if (_autoLoadRegistered)
        {
            _autoLoadRegistered = false;
            _autoLoadMargin = null;

            await _js.BitObserversUnregisterIntersection(_Id);
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    private bool _IsHorizontal => Horizontal && Virtualize is false;

    // The wrapper of a LoadMoreTemplate is not a real button, so the keys a button would be activated
    // by are handled here. The default action of the key is left alone so that neither Tab nor a key
    // typed into a control the template brought along is taken away from the browser.
    private async Task HandleLoadMoreKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not ("Enter" or " ")) return;

        await LoadMoreItems(false);
    }

    private async Task LoadMoreItems(bool reset)
    {
        if (reset)
        {
            // A load that is still in flight would append its page on top of the reset one, so it is
            // cancelled here and its result discarded by the cancellation checks below.
            _globalCts?.Cancel();
            _globalCts = null;

            _viewItems = [];
            _loadMoreSkip = 0;
            _loadMoreFinished = false;
        }

        if (LoadMore is false) return;
        if (_loadMoreFinished) return;
        if (_globalCts is not null) return;
        // A reset always loads the first page, since that is what the list would have shown anyway; only
        // the loading a disabled list would have been asked for by the user is turned down here.
        if (reset is false && IsEnabled is false) return;
        if (IsDisposed) return;

        var localCts = new CancellationTokenSource();

        _globalCts = localCts;

        var loaded = false;

        try
        {
            await SetLoading(true);

            try
            {
                if (ItemsProvider is null)
                {
                    var items = Items ?? [];
                    var page = items.Skip(_loadMoreSkip).Take(LoadMoreSize).ToArray();

                    if (localCts.IsCancellationRequested is false)
                    {
                        _viewItems = [.. _viewItems, .. page];

                        // The skip advances by what was actually appended, so a provider (or a collection)
                        // that returns a short page does not leave a hole in the next one.
                        _loadMoreSkip += page.Length;
                        _loadMoreFinished = _viewItems.Count >= items.Count;
                        loaded = true;
                    }
                }
                else
                {
                    var result = await FetchItems(_loadMoreSkip, LoadMoreSize, localCts.Token, false);

                    if (localCts.IsCancellationRequested is false)
                    {
                        var page = result.Items ?? [];

                        _viewItems = [.. _viewItems, .. page];

                        _loadMoreSkip += page.Count;

                        //_loadMoreFinished = _viewItems.Count >= result.TotalItemCount; // for performance purposes we won't use TotalItemCount here!
                        // A page shorter than the one asked for is the last one, which saves the extra
                        // round trip an empty page would have cost to discover the same thing.
                        _loadMoreFinished = page.Count < LoadMoreSize;
                        loaded = true;
                    }
                }
            }
            catch (OperationCanceledException) when (localCts.IsCancellationRequested) { }
        }
        finally
        {
            // A load that was superseded no longer owns the loading state, so it leaves the flag (and the
            // cancellation source) to the load that replaced it rather than clearing what that one set.
            var owned = _globalCts == localCts;

            if (owned)
            {
                _globalCts = null;
            }

            localCts.Dispose();

            if (owned)
            {
                await SetLoading(false);
            }
        }

        StateHasChanged();

        if (loaded && IsDisposed is false)
        {
            await OnLoadMore.InvokeAsync(_viewItems.Count);
        }
    }

    private async Task LoadAllItems()
    {
        if (ItemsProvider is null) return;
        if (IsDisposed) return;

        _globalCts?.Cancel();

        var localCts = new CancellationTokenSource();

        _globalCts = localCts;

        try
        {
            await SetLoading(true);

            try
            {
                var result = await FetchItems(0, int.MaxValue, localCts.Token, false);

                if (localCts.IsCancellationRequested is false)
                {
                    _viewItems = result.Items ?? [];
                }
            }
            catch (OperationCanceledException) when (localCts.IsCancellationRequested) { }
        }
        finally
        {
            var owned = _globalCts == localCts;

            if (owned)
            {
                _globalCts = null;
            }

            localCts.Dispose();

            if (owned)
            {
                await SetLoading(false);
            }
        }

        StateHasChanged();
    }

    private async Task SetLoading(bool value)
    {
        if (_isLoadingMore == value) return;

        _isLoadingMore = value;

        StateHasChanged();

        if (IsDisposed) return;

        await OnLoadingChange.InvokeAsync(value);
    }

    private async ValueTask<BitBasicListItemsProviderResult<TItem>> FetchItems(
        int startIndex, int count, CancellationToken cancellationToken, bool debounce)
    {
        if (ItemsProvider is null) return new([], 0);

        // Scrolling that keeps asking for a new region faster than the delay elapses would have every
        // request cancelled before it was ever sent, which leaves the list on its placeholders for as
        // long as it goes on. So the delay is skipped once the provider has been left unasked for longer
        // than twice the window: the debouncing turns into a throttling there, and items keep arriving
        // no matter how the scrolling goes.
        var starved = Environment.TickCount64 - _lastFetchTime > ItemsProviderDelay * 2L;

        if (debounce && ItemsProviderDelay > 0 && starved is false)
        {
            // Debounces the requests. This eliminates a lot of redundant queries at the cost of slight lag
            // after interactions: a request superseded within the delay is dropped before it is ever sent.
            try
            {
                await Task.Delay(ItemsProviderDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return new([], 0);
            }
        }

        if (cancellationToken.IsCancellationRequested) return new([], 0);

        _lastFetchTime = Environment.TickCount64;

        return await ItemsProvider(new BitBasicListItemsProviderRequest<TItem>(startIndex, count, cancellationToken));
    }

    private async ValueTask<ItemsProviderResult<TItem>> ProvideVirtualizedItems(ItemsProviderRequest request)
    {
        if (ItemsProvider is null) return new([], 0);

        var result = await FetchItems(request.StartIndex, request.Count, request.CancellationToken, true);

        if (request.CancellationToken.IsCancellationRequested) return new([], 0);

        return new ItemsProviderResult<TItem>(result.Items ?? [], result.TotalItemCount);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_globalCts is not null)
        {
            _globalCts.Cancel();
            _globalCts.Dispose();
            _globalCts = null;
        }

        if (_autoLoadRegistered)
        {
            _autoLoadRegistered = false;

            try
            {
                await _js.BitObserversUnregisterIntersection(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        _dotnetObj?.Dispose();
        _dotnetObj = null;

        await base.DisposeAsync(disposing);
    }
}
