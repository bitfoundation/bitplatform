using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// BitVirtualize is a high-performance virtualization (windowing) component that renders only the items
/// currently visible in its scroll viewport (plus a configurable overscan buffer). It supports fixed and
/// dynamically measured item sizes, vertical and horizontal orientation, in-memory or lazy-loaded data,
/// placeholders, header and footer content, sticky group headers, keyboard navigation, and a bottom-anchored
/// (chat) mode with scroll anchoring that prevents content from jumping as dynamic items get measured.
/// </summary>
public partial class BitVirtualize<TItem> : BitComponentBase
{
    // Browsers cap the maximum pixel size of a single element (~33.5M px in Chrome, ~17.8M in older Firefox).
    // When the virtual extent exceeds this, the spacer gets capped and the scroll position is mapped
    // proportionally between the real (capped) and the virtual (full) extent so the whole range stays reachable.
    private const double MaxCssSize = 10_000_000d;
    private const int ProviderCacheCap = 600;
    private const int SizeCacheCap = 20_000;
    // How many measurement batches a dynamic ScrollToIndex keeps re-aligning its target for.
    private const int MaxScrollRealignBatches = 10;

    private int _loadedStart;
    private int _itemCount;
    private bool _initialized;
    private bool _loading;
    private bool _fetching;
    private bool _refreshPending;
    private bool _usingProvider;
    private bool _lastHorizontal;
    private System.Reflection.MethodInfo? _lastStickyMethod;

    private IList<TItem>? _itemList;            // materialized view of Items
    private ICollection<TItem>? _lastItems;     // the Items reference of the last recompute
    private int _lastItemsCount = -1;           // the Items count of the last recompute (detects same-instance mutations)
    private IReadOnlyList<TItem>? _loadedItems; // current provider window
    private Dictionary<int, TItem>? _providerCache; // previously loaded provider items, keyed by absolute index

    private BitVirtualizePrefixSumTree? _tree;  // dynamic mode only
    private Dictionary<object, double>? _sizeByKey; // dynamic + ItemKey: measured sizes keyed by item identity
    private double _realTotal;                  // the rendered (capped) size of the spacer (px)
    private double _ratio = 1d;                 // virtual px per real px of scrolling (1 unless the extent exceeds MaxCssSize)
    private double _scrollOffset;               // virtual scroll offset (item-coordinate space)
    private double _realScrollOffset;           // real scroll offset of the viewport, relative to the start of the spacer (px)
    private double _viewportSize;               // viewport size (px)
    private double _renderStartOffset;          // virtual offset of the first rendered item
    private double _blockOffset;                // real offset of the rendered block within the spacer (px)
    private int _visibleStart;
    private int _visibleEnd;   // exclusive
    private int _renderStart;
    private int _renderEnd;    // exclusive

    private CancellationTokenSource? _loadCts;

    // Infinite-scroll edge tracking.
    private int _lastEndReachedCount = -1;
    private int _lastStartReachedCount = -1;
    private bool _wasAtStart;
    private bool _wasAtEnd;

    // Sticky (grouped) header tracking.
    private List<int>? _stickyIndices;   // sorted indices flagged by IsStickyItem
    private int _stickyActiveIndex = -1;
    private double _stickyNextOffset = -1;

    // Scroll position preservation across data changes.
    private object? _anchorKey;          // the ItemKey of the first visible item (in-memory Items only)
    private int _anchorIndex;
    private bool _pendingScrollToEnd;
    private double _pendingScrollOffset = -1;
    private double _preserveEndDistance = -1;
    private bool _initialScrollDone;
    private bool _stickToEnd;
    private Func<Task>? _queuedScroll;   // a scroll requested before the component was ready to perform it

    // Keyboard navigation.
    private int _activeIndex = -1;
    private int _pendingFocusIndex = -1;

    // Dynamic ScrollToIndex precision: re-align once the target region is measured.
    private int _pendingScrollIndex = -1;
    private int _pendingScrollBatches;
    private BitVirtualizeScrollAlignment _pendingScrollAlignment;

    // The options last sent to the browser, to only send them again when they change.
    private bool _sentHorizontal;
    private bool _sentDynamic;
    private BitDir? _sentDir;
    private double _sentThreshold;
    private bool _syncedSpacer;

    private DotNetObjectReference<BitVirtualize<TItem>>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Pushes the items to the end (bottom, or the right in horizontal mode) of the viewport while they are too
    /// few to fill it, the way a chat conversation starts at the bottom. Pairs naturally with Reversed.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool AlignToEnd { get; set; }

    /// <summary>
    /// The custom template to render each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitVirtualize.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVirtualizeClassStyles? Classes { get; set; }

    /// <summary>
    /// Enables dynamic item sizing in which each rendered item gets measured in the browser and its real
    /// size gets cached, using the EstimatedItemSize for the items that have not been measured yet.
    /// </summary>
    [Parameter] public bool Dynamic { get; set; }

    /// <summary>
    /// The custom template to render when there is no item available.
    /// </summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// The assumed size in pixels of the items that have not been measured yet in dynamic mode.
    /// </summary>
    [Parameter] public float EstimatedItemSize { get; set; } = 50f;

    /// <summary>
    /// The custom template to render after the last item, inside the scroll container
    /// (for example, a loading indicator at the end of an infinite list).
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// The custom template to render before the first item, inside the scroll container.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Renders the items horizontally so the viewport scrolls along the x-axis.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Horizontal { get; set; }

    /// <summary>
    /// The index of the item to scroll to on the first render. Ignored when Reversed is set.
    /// </summary>
    [Parameter] public int? InitialIndex { get; set; }

    /// <summary>
    /// A predicate that marks certain items (for example, group headers) as sticky. The active sticky item
    /// gets pinned to the leading edge of the viewport while its group scrolls. Fully supported with in-memory
    /// Items; in provider mode it is applied on a best-effort basis to the currently loaded window.
    /// </summary>
    [Parameter] public Func<TItem, bool>? IsStickyItem { get; set; }

    /// <summary>
    /// The in-memory collection of items to virtualize. Mutually exclusive with ItemsProvider.
    /// </summary>
    [Parameter] public ICollection<TItem>? Items { get; set; }

    /// <summary>
    /// A function that returns a stable and unique identity key for an item. When provided, rendered rows are keyed by
    /// identity (instead of by index) so per-item DOM/component state survives insertions, removals and
    /// reordering, dynamic measurements follow their item across those mutations, and the item in view stays
    /// in place when items get inserted or removed before it.
    /// </summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>
    /// The ARIA role of each item element.
    /// </summary>
    [Parameter] public string? ItemRole { get; set; } = "listitem";

    /// <summary>
    /// The size in pixels of each item along the scroll axis when the Dynamic mode is off.
    /// </summary>
    [Parameter] public float ItemSize { get; set; } = 50f;

    /// <summary>
    /// The item provider function that lazily supplies windows of items on demand. Mutually exclusive with Items.
    /// </summary>
    [Parameter] public BitVirtualizeItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>
    /// Alias for ChildContent.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The custom template to render until the component has performed its first load.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// The callback to be called when the last item comes within ReachedThreshold items of the visible window,
    /// useful for appending more data in infinite scrolling scenarios. Fires once per item-count value.
    /// </summary>
    [Parameter] public EventCallback OnEndReached { get; set; }

    /// <summary>
    /// The callback to be called when the first item comes within ReachedThreshold items of the visible window,
    /// useful for prepending older data (for example, loading chat history when scrolling up). Fires once per
    /// item-count value.
    /// </summary>
    [Parameter] public EventCallback OnStartReached { get; set; }

    /// <summary>
    /// The callback to be called whenever the visible index range changes.
    /// </summary>
    [Parameter] public EventCallback<(int Start, int End)> OnVisibleRangeChanged { get; set; }

    /// <summary>
    /// The number of extra items to render on each side of the visible window for smoother scrolling.
    /// </summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>
    /// The custom template to render an item whose data has not been loaded yet in provider mode.
    /// </summary>
    [Parameter] public RenderFragment<BitVirtualizePlaceholderContext>? PlaceholderTemplate { get; set; }

    /// <summary>
    /// The number of items away from an edge the visible window must be before OnEndReached/OnStartReached fire.
    /// </summary>
    [Parameter] public int ReachedThreshold { get; set; }

    /// <summary>
    /// Enables the bottom-anchored mode in which the list starts scrolled to the end and automatically keeps
    /// the newest items in view when data gets appended while the user is at the bottom. Ideal for chat and log views.
    /// </summary>
    [Parameter] public bool Reversed { get; set; }

    /// <summary>
    /// The ARIA role of the root element.
    /// </summary>
    [Parameter] public string? Role { get; set; } = "list";

    /// <summary>
    /// The custom template to render the pinned sticky item. Falls back to the item template when not provided.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? StickyTemplate { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitVirtualize.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitVirtualizeClassStyles? Styles { get; set; }




    /// <summary>
    /// Re-requests the data from the ItemsProvider (or re-reads the Items) and refreshes the view.
    /// </summary>
    public async Task RefreshDataAsync()
    {
        if (_initialized is false)
        {
            _refreshPending = true;
            return;
        }

        if (Items is not null)
        {
            ApplyItems();
            StateHasChanged();
            await TryApplyInitialScrollAsync();
        }
        else if (ItemsProvider is not null)
        {
            _providerCache?.Clear();
            await LoadProviderWindowAsync(forceCount: true);
            await TryApplyInitialScrollAsync();
        }
    }

    /// <summary>
    /// Scrolls the viewport so that the item at the provided index becomes visible.
    /// A call made before the component is ready (for example, before its data arrives) gets applied once it is.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="alignment">Where the item should be positioned within the viewport.</param>
    /// <param name="smooth">Whether to animate the scroll.</param>
    public Task ScrollToIndexAsync(int index, BitVirtualizeScrollAlignment alignment = BitVirtualizeScrollAlignment.Start, bool smooth = false)
        => ScrollToIndexCoreAsync(index, alignment, smooth, markPending: Dynamic);

    /// <summary>
    /// Scrolls to an absolute pixel offset along the scroll axis, measured from the start of the first item.
    /// </summary>
    public async Task ScrollToOffsetAsync(double offset, bool smooth = false)
    {
        if (_initialized is false)
        {
            _queuedScroll = () => ScrollToOffsetAsync(offset, smooth);
            return;
        }

        var virtualTarget = Math.Clamp(offset, 0, MaxScrollOffset);
        var realTarget = Math.Clamp(RealFromVirtual(virtualTarget), 0, Math.Max(0, _realTotal - _viewportSize));

        var rangeChanged = smooth is false && MoveTo(virtualTarget, realTarget);
        if (rangeChanged)
        {
            StateHasChanged();
        }

        await _js.BitVirtualizeScrollToOffset(UniqueId, realTarget, smooth);

        if (rangeChanged && ItemsProvider is not null)
        {
            await LoadProviderWindowAsync(forceCount: false);
        }
    }

    /// <summary>
    /// Scrolls the viewport by the provided number of pixels along the scroll axis (negative values scroll back).
    /// </summary>
    public Task ScrollByAsync(double delta, bool smooth = false) => ScrollToOffsetAsync(_scrollOffset + delta, smooth);

    /// <summary>
    /// Scrolls to the very start (top/left) of the list, including the HeaderTemplate.
    /// </summary>
    public Task ScrollToStartAsync(bool smooth = false) => ScrollToEdgeAsync(end: false, smooth);

    /// <summary>
    /// Scrolls to the very end (bottom/right) of the list, including the FooterTemplate. Useful for chat and log views.
    /// </summary>
    public Task ScrollToEndAsync(bool smooth = false) => ScrollToEdgeAsync(end: true, smooth);



    [JSInvokable("Scroll")]
    public async Task _Scroll(double scrollOffset, double viewportSize)
    {
        if (IsDisposed) return;

        var viewportChanged = Math.Abs(viewportSize - _viewportSize) > 0.5;

        _viewportSize = viewportSize;
        _realScrollOffset = scrollOffset;
        UpdateScale();
        _scrollOffset = VirtualFromReal(scrollOffset);

        var restick = false;
        if (Reversed)
        {
            // A resize (e.g. a soft keyboard opening) must not unpin a list that was pinned to the end.
            var wasStuck = _stickToEnd;
            _stickToEnd = IsNearEnd() || (viewportChanged && wasStuck && _initialScrollDone);
            restick = _stickToEnd && IsNearEnd() is false;
        }

        int prevRenderStart = _renderStart, prevRenderEnd = _renderEnd;
        RecomputeRange();

        if (_renderStart != prevRenderStart || _renderEnd != prevRenderEnd)
        {
            // Render right away (placeholders included) rather than after the provider responds.
            StateHasChanged();

            if (ItemsProvider is not null)
            {
                await LoadProviderWindowAsync(forceCount: false);
            }
        }

        if (restick)
        {
            await ScrollToEdgeAsync(end: true, smooth: false);
        }
    }

    [JSInvokable("ItemsMeasured")]
    public async Task _ItemsMeasured(int[] indices, double[] sizes)
    {
        if (IsDisposed || Dynamic is false || _tree is null || indices.Length == 0 || indices.Length != sizes.Length) return;

        var anchor = _visibleStart;
        var oldAnchorOffset = _tree.PrefixSum(anchor);

        var changed = false;
        for (var i = 0; i < indices.Length; i++)
        {
            var idx = indices[i];
            var size = sizes[i];
            if (idx < 0 || idx >= _itemCount || size < 0 || double.IsFinite(size) is false) continue;

            if (_tree.SetSize(idx, size) != 0d)
            {
                changed = true;
            }

            CacheMeasuredSize(idx, size);
        }

        var realign = _pendingScrollIndex >= 0 && _initialized;
        if (changed is false && realign is false) return;

        if (changed)
        {
            // Scroll anchoring: keep the first visible item visually stable when the
            // cumulative size of the items above it changes.
            var newAnchorOffset = _tree.PrefixSum(anchor);
            var diff = newAnchorOffset - oldAnchorOffset;
            if (Math.Abs(diff) > 0.01 && _initialized && _scrollOffset > 0)
            {
                var realDiff = diff / _ratio;
                _scrollOffset += diff;
                _realScrollOffset += realDiff;
                await _js.BitVirtualizeAdjustScroll(UniqueId, realDiff);
            }

            RecomputeRange();
            StateHasChanged();
        }

        // Re-align a pending ScrollToIndex once its target is rendered and measured, since its offset
        // was first derived from the estimates of the items before it.
        if (realign)
        {
            var target = _pendingScrollIndex;
            var alignment = _pendingScrollAlignment;
            var targetMeasured = Array.IndexOf(indices, target) >= 0;

            if (targetMeasured || ++_pendingScrollBatches >= MaxScrollRealignBatches)
            {
                _pendingScrollIndex = -1;
            }

            if (changed && target >= _renderStart && target < _renderEnd)
            {
                await ScrollToIndexCoreAsync(target, alignment, smooth: false, markPending: false);
            }
        }

        // In reversed (chat) mode, keep the newest items pinned as measurements settle.
        if (changed && Reversed && _stickToEnd && _initialized)
        {
            await ScrollToEdgeAsync(end: true, smooth: false);
        }
    }

    [JSInvokable("KeyNavigate")]
    public async Task _KeyNavigate(string key, int focusedIndex = -1)
    {
        if (IsDisposed || _initialized is false || _itemCount == 0) return;

        // The item the user focused (e.g. by clicking it) is where navigation continues from.
        if (focusedIndex >= 0 && focusedIndex < _itemCount)
        {
            _activeIndex = focusedIndex;
        }

        var hasActive = _activeIndex >= 0 && _activeIndex < _itemCount;
        var current = hasActive ? _activeIndex : _visibleStart;
        var page = Math.Max(1, _visibleEnd - _visibleStart - 1);
        var target = key switch
        {
            // With nothing active yet, the first arrow press activates the first visible item.
            "ArrowDown" or "ArrowRight" => hasActive ? current + 1 : current,
            "ArrowUp" or "ArrowLeft" => hasActive ? current - 1 : current,
            "PageDown" => current + page,
            "PageUp" => current - page,
            "Home" => 0,
            "End" => _itemCount - 1,
            _ => current
        };

        target = Math.Clamp(target, 0, _itemCount - 1);
        if (target == _activeIndex && _renderStart <= target && target < _renderEnd && focusedIndex == target) return;

        _activeIndex = target;
        _pendingFocusIndex = target;

        // Dynamic offsets are estimates until measured: keep the target in view once its real size is known.
        _pendingScrollIndex = Dynamic ? target : -1;
        _pendingScrollAlignment = BitVirtualizeScrollAlignment.Auto;
        _pendingScrollBatches = 0;

        // Bring the target into view (and into the rendered window) before focusing it.
        var offset = GetItemOffset(target);
        var size = GetItemSize(target);
        var virtualTarget = Math.Clamp(ResolveAutoAlignment(offset, size), 0, MaxScrollOffset);
        var realTarget = Math.Clamp(RealFromVirtual(virtualTarget), 0, Math.Max(0, _realTotal - _viewportSize));
        var rangeChanged = MoveTo(virtualTarget, realTarget);

        StateHasChanged();
        await _js.BitVirtualizeScrollToOffset(UniqueId, realTarget, false);

        if (rangeChanged && ItemsProvider is not null)
        {
            await LoadProviderWindowAsync(forceCount: false);
        }
    }



    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Items is not null && ItemsProvider is not null)
        {
            throw new InvalidOperationException($"BitVirtualize requires either {nameof(Items)} or {nameof(ItemsProvider)}, but not both.");
        }

        // A change of the orientation invalidates the sizes measured along the old axis.
        if (Horizontal != _lastHorizontal)
        {
            _lastHorizontal = Horizontal;
            if (_tree is not null)
            {
                _tree = null;
                _sizeByKey = null;
                SetItemCount(_itemCount);
            }
        }

        // Switching Dynamic on/off creates or drops the size tree.
        if (Dynamic != (_tree is not null))
        {
            SetItemCount(_itemCount);
        }

        if (Items is not null)
        {
            // Recompute when the Items reference changes, or when the same instance's count changed
            // (a common in-place add/remove); deeper in-place mutations are picked up via RefreshDataAsync.
            if (ReferenceEquals(Items, _lastItems) is false || Items.Count != _lastItemsCount)
            {
                ApplyItems();
            }
            else
            {
                // Sizing parameters (ItemSize, OverscanCount, ...) may have changed.
                RecomputeRange();
            }
        }
        else if (_itemList is not null)
        {
            // The Items got removed: drop everything derived from them.
            _itemList = null;
            _lastItems = null;
            _lastItemsCount = -1;
            _anchorKey = null;
            SetItemCount(0);
            ComputeStickyIndices();
            RecomputeRange();
        }
        else
        {
            RecomputeRange();
        }

        // A different sticky predicate (compared by method, so a lambda re-created on every render of the
        // parent does not rescan the items each time).
        if (IsStickyItem?.Method != _lastStickyMethod)
        {
            ComputeStickyIndices();
            UpdateSticky();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ItemsProvider is not null && _usingProvider is false)
        {
            // Switched to (or started with) a provider; the very first load happens after the first render.
            _usingProvider = true;
            if (_initialized)
            {
                await LoadProviderWindowAsync(forceCount: true);
                await TryApplyInitialScrollAsync();
            }
        }
        else if (ItemsProvider is null && _usingProvider)
        {
            _usingProvider = false;
            _loadCts?.Cancel();
            _fetching = false;
            _loadedItems = null;
            _providerCache = null;
            if (Items is null)
            {
                SetItemCount(0);
                ComputeStickyIndices();
                RecomputeRange();
            }
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                _dotnetObj = DotNetObjectReference.Create(this);
                _sentHorizontal = Horizontal;
                _sentDynamic = Dynamic;
                _sentDir = Dir;
                _sentThreshold = ScrollThreshold;
                var metrics = await _js.BitVirtualizeSetup(UniqueId, RootElement, Horizontal, Dynamic, _sentThreshold, _dotnetObj);

                // metrics is null when the js runtime is not available (e.g. prerendering).
                if (metrics is not null)
                {
                    _syncedSpacer = ShowsSpacer();
                    _viewportSize = metrics.ViewportSize;
                    _realScrollOffset = metrics.ScrollOffset;
                    UpdateScale();
                    _scrollOffset = VirtualFromReal(metrics.ScrollOffset);
                    _initialized = true;
                }

                await InitialLoadAsync();

                if (_initialized && _refreshPending)
                {
                    _refreshPending = false;
                    await RefreshDataAsync();
                }

                await TryApplyInitialScrollAsync();
            }
            else if (_initialized)
            {
                var threshold = ScrollThreshold;
                // A changed direction is picked up by the update too: it re-reads the computed direction.
                if (Horizontal != _sentHorizontal || Dynamic != _sentDynamic || Dir != _sentDir || Math.Abs(threshold - _sentThreshold) > 0.01)
                {
                    _sentHorizontal = Horizontal;
                    _sentDynamic = Dynamic;
                    _sentDir = Dir;
                    _sentThreshold = threshold;
                    await _js.BitVirtualizeUpdate(UniqueId, Horizontal, Dynamic, threshold);
                }

                // Reconciles the ResizeObserver subscriptions of the rendered items (dynamic mode), the size of
                // the content before the items (header / AlignToEnd) and the sticky header's push-out transform.
                // Also once whenever the spacer appears or goes (e.g. after the first load of a provider).
                var showsSpacer = ShowsSpacer();
                if (Dynamic || _stickyIndices is not null || HeaderTemplate is not null || AlignToEnd || showsSpacer != _syncedSpacer)
                {
                    _syncedSpacer = showsSpacer;
                    await _js.BitVirtualizeSync(UniqueId);
                }

                await TryApplyInitialScrollAsync();
                await ApplyPendingScrollAsync();
                await ApplyPendingFocusAsync();
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.OnAfterRenderAsync(firstRender);
    }



    protected override string RootElementClass => "bit-vir";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Horizontal ? "bit-vir-hor" : string.Empty);
        ClassBuilder.Register(() => AlignToEnd ? "bit-vir-ate" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }



    // Whether the items spacer (rather than the loading or the empty content) is rendered.
    private bool ShowsSpacer() => ShowsLoading() is false && ShowsEmpty() is false;

    private bool ShowsLoading() => _itemCount == 0 && LoadingTemplate is not null && (_initialized is false || _loading);

    private bool ShowsEmpty() => _itemCount == 0 && _loading is false && (Items is not null || _initialized);

    private float FixedSize => ItemSize >= 1 ? ItemSize : 1f;

    private float EstimatedSize => EstimatedItemSize >= 1 ? EstimatedItemSize : 1f;

    private int Overscan => Math.Max(0, OverscanCount);

    private int Threshold => Math.Max(0, ReachedThreshold);

    private double MaxScrollOffset => Math.Max(0, GetTotalSize() - _viewportSize);

    // The scroll movement (real px) the browser may coalesce before reporting it: one fixed item, since the overscan
    // covers it. Dynamic mode and a zero overscan need every movement.
    private double ScrollThreshold => Dynamic || Overscan == 0 ? 0d : FixedSize / _ratio;

    private double RealFromVirtual(double value) => value / _ratio;

    private double VirtualFromReal(double value) => value * _ratio;

    private void UpdateScale()
    {
        var total = GetTotalSize();
        if (total <= MaxCssSize)
        {
            _realTotal = total;
            _ratio = 1d;
            return;
        }

        // Map the scrollable ranges onto each other, so the real end of the capped spacer is the virtual end.
        _realTotal = MaxCssSize;
        var realRange = MaxCssSize - _viewportSize;
        _ratio = realRange > 0 ? Math.Max(1d, (total - _viewportSize) / realRange) : 1d;
    }

    // Moves the virtual scroll position without waiting for the browser to report it; returns whether the
    // rendered range changed.
    private bool MoveTo(double virtualOffset, double realOffset)
    {
        int prevRenderStart = _renderStart, prevRenderEnd = _renderEnd;

        _scrollOffset = virtualOffset;
        _realScrollOffset = realOffset;
        RecomputeRange();

        return _renderStart != prevRenderStart || _renderEnd != prevRenderEnd;
    }

    private async Task ScrollToIndexCoreAsync(int index, BitVirtualizeScrollAlignment alignment, bool smooth, bool markPending)
    {
        if (_initialized is false || _itemCount == 0)
        {
            _queuedScroll = () => ScrollToIndexCoreAsync(index, alignment, smooth, markPending);
            return;
        }

        index = Math.Clamp(index, 0, _itemCount - 1);
        var offset = GetItemOffset(index);
        var size = GetItemSize(index);
        var target = alignment switch
        {
            BitVirtualizeScrollAlignment.Start => offset,
            BitVirtualizeScrollAlignment.Center => offset - (_viewportSize - size) / 2d,
            BitVirtualizeScrollAlignment.End => offset - (_viewportSize - size),
            _ => ResolveAutoAlignment(offset, size)
        };

        // In dynamic mode the offset is derived from estimates for unmeasured items, so remember the
        // request and re-align once the target region reports its real sizes (see _ItemsMeasured).
        if (markPending)
        {
            _pendingScrollIndex = index;
            _pendingScrollAlignment = alignment;
            _pendingScrollBatches = 0;
        }

        await ScrollToOffsetAsync(target, smooth);
    }

    private async Task ScrollToEdgeAsync(bool end, bool smooth)
    {
        if (_initialized is false)
        {
            _queuedScroll = () => ScrollToEdgeAsync(end, smooth);
            return;
        }

        if (smooth is false && MoveTo(end ? MaxScrollOffset : 0, end ? Math.Max(0, _realTotal - _viewportSize) : 0))
        {
            StateHasChanged();
        }

        // The browser scrolls to its real edge, so the header / footer come into view as well.
        await _js.BitVirtualizeScrollToEdge(UniqueId, end, smooth);

        if (ItemsProvider is not null)
        {
            await LoadProviderWindowAsync(forceCount: false);
        }
    }

    private async Task TryApplyInitialScrollAsync()
    {
        if (_initialized is false || _itemCount == 0) return;

        if (_queuedScroll is { } queued)
        {
            // An explicit scroll request takes precedence over the initial position.
            _queuedScroll = null;
            _initialScrollDone = true;
            await queued();
            return;
        }

        if (_initialScrollDone) return;

        _initialScrollDone = true;

        if (Reversed)
        {
            _stickToEnd = true;
            await ScrollToEdgeAsync(end: true, smooth: false);
        }
        else if (InitialIndex is { } idx)
        {
            await ScrollToIndexAsync(idx);
        }
    }

    private async Task ApplyPendingScrollAsync()
    {
        if (_initialized is false) return;

        if (_pendingScrollToEnd)
        {
            _pendingScrollToEnd = false;
            _pendingScrollOffset = -1;
            _preserveEndDistance = -1;
            await ScrollToEdgeAsync(end: true, smooth: false);
        }
        else if (_pendingScrollOffset >= 0)
        {
            // Keep the anchored item where it was after data got inserted or removed before it.
            var target = _pendingScrollOffset;
            _pendingScrollOffset = -1;
            await ScrollToOffsetAsync(target, false);
        }
        else if (_preserveEndDistance >= 0)
        {
            // Restore the distance from the end after a prepend so the viewport stays put.
            var target = Math.Max(0, GetTotalSize() - _viewportSize - _preserveEndDistance);
            _preserveEndDistance = -1;
            await ScrollToOffsetAsync(target, false);
        }
    }

    private async Task ApplyPendingFocusAsync()
    {
        if (_initialized is false || _pendingFocusIndex < 0) return;

        var index = _pendingFocusIndex;
        _pendingFocusIndex = -1;

        if (index >= _renderStart && index < _renderEnd)
        {
            await _js.BitVirtualizeFocusIndex(UniqueId, index);
        }
    }

    // Re-reads Items and everything derived from them, keeping the viewport where the user expects it: pinned
    // to the end in a Reversed list that was at the bottom; otherwise on the item that was first in view
    // (tracked by ItemKey), or, without an ItemKey, at the same distance from the end in a Reversed list.
    private void ApplyItems()
    {
        var hadItems = _initialized && _initialScrollDone && _itemCount > 0 && _viewportSize > 0;
        var atEnd = Reversed && (_stickToEnd || IsNearEnd());
        var prevTotal = GetTotalSize();
        var prevCount = _itemCount;
        var anchorKey = _anchorKey;
        var anchorIndex = _anchorIndex;
        var anchorDelta = anchorKey is not null && anchorIndex < _itemCount ? _scrollOffset - GetItemOffset(anchorIndex) : 0;

        _lastItems = Items;
        _itemList = Items as IList<TItem> ?? [.. Items!];
        _lastItemsCount = _itemList.Count;
        _loadedItems = null;
        SetItemCount(_itemList.Count);
        ComputeStickyIndices();

        if (hadItems && _itemCount > 0)
        {
            if (atEnd)
            {
                // The user was at the bottom: keep the newest items in view.
                if (GetTotalSize() > prevTotal)
                {
                    _pendingScrollToEnd = true;
                }
            }
            else if (anchorKey is not null && (Reversed || _scrollOffset > 0))
            {
                var index = FindIndexByKey(anchorKey, anchorIndex, _itemCount - prevCount);
                if (index >= 0)
                {
                    var target = Math.Max(0, GetItemOffset(index) + anchorDelta);
                    if (Math.Abs(target - _scrollOffset) > 0.5)
                    {
                        _scrollOffset = target;
                        _pendingScrollOffset = target;
                    }
                }
            }
            else if (Reversed && GetTotalSize() > prevTotal)
            {
                // Content grew (likely prepended history): keep the viewport anchored
                // to the same distance from the end so it does not jump.
                _preserveEndDistance = prevTotal - (_scrollOffset + _viewportSize);
            }
        }

        RecomputeRange();
    }

    private int FindIndexByKey(object key, int oldIndex, int countDelta)
    {
        if (_itemList is null || ItemKey is null) return -1;

        // The likely spots first: shifted by a prepend, or unmoved by an append.
        if (IsKeyAt(oldIndex + countDelta)) return oldIndex + countDelta;
        if (IsKeyAt(oldIndex)) return oldIndex;

        for (var i = 0; i < _itemList.Count; i++)
        {
            if (IsKeyAt(i)) return i;
        }

        return -1;

        bool IsKeyAt(int index) => (uint)index < (uint)_itemList.Count && Equals(ItemKey(_itemList[index]), key);
    }

    private void ComputeStickyIndices()
    {
        _lastStickyMethod = IsStickyItem?.Method;

        if (IsStickyItem is null)
        {
            _stickyIndices = null;
            return;
        }

        var list = new List<int>();

        if (_itemList is not null)
        {
            for (var i = 0; i < _itemList.Count; i++)
            {
                if (IsStickyItem(_itemList[i]))
                {
                    list.Add(i);
                }
            }
        }
        else if (_loadedItems is not null)
        {
            // Provider mode: best-effort over the currently loaded window.
            for (var local = 0; local < _loadedItems.Count; local++)
            {
                if (IsStickyItem(_loadedItems[local]))
                {
                    list.Add(_loadedStart + local);
                }
            }
        }

        _stickyIndices = list;
    }

    private void SetItemCount(int count)
    {
        if (_activeIndex >= count)
        {
            _activeIndex = count - 1;
        }

        if (count == _itemCount && (_tree is not null) == Dynamic)
        {
            ReseedTreeFromKeys();
            return;
        }

        _itemCount = count;

        if (Dynamic)
        {
            if (_tree is null)
            {
                _tree = new BitVirtualizePrefixSumTree(count, EstimatedSize);
            }
            else
            {
                // Keep the already measured sizes of the surviving indices so a count change
                // (e.g. infinite-scroll append) does not throw away the measurements.
                _tree.Resize(count, EstimatedSize);
            }

            // Re-apply identity-keyed measurements so sizes follow their items across
            // insertions/removals/reordering (index-based Resize alone assumes append-at-end).
            ReseedTreeFromKeys();
        }
        else
        {
            _tree = null;
        }
    }

    private async Task InitialLoadAsync()
    {
        RecomputeRange();

        if (ItemsProvider is not null)
        {
            await LoadProviderWindowAsync(forceCount: true);
        }

        StateHasChanged();
    }

    private async Task LoadProviderWindowAsync(bool forceCount)
    {
        if (ItemsProvider is null) return;

        var start = Math.Max(0, _renderStart);
        // When the count is still unknown, fetch a screen-sized window from the top.
        var count = _initialized && _itemCount > 0
            ? Math.Max(1, _renderEnd - start)
            : Math.Max(1, EstimateInitialCount());

        // Skip the load if the requested window is already fully available (current window or cache).
        if (forceCount is false && IsRangeAvailable(_renderStart, _renderEnd))
        {
            return;
        }

        if (_loadCts is not null)
        {
            _loadCts.Cancel();
            _loadCts.Dispose();
        }
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        _loading = _loadedItems is null;
        if (_fetching is false)
        {
            // Announce the load (aria-busy) while it is in flight.
            _fetching = true;
            StateHasChanged();
        }

        try
        {
            var result = await ItemsProvider(new BitVirtualizeItemsProviderRequest(start, count, token));

            if (token.IsCancellationRequested || IsDisposed) return;

            var totalCount = Math.Max(0, result.TotalItemCount);
            if (totalCount != _itemCount)
            {
                SetItemCount(totalCount);
            }

            var items = result.Items ?? [];
            _loadedItems = items;
            _loadedStart = start;
            _loading = false;
            _fetching = false;

            CacheProviderWindow(start, items);
            ComputeStickyIndices();
            RecomputeRange();
            StateHasChanged();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Superseded by a newer request; ignore.
        }
        catch (Exception ex) when (ex is not JSDisconnectedException)
        {
            // Surface provider failures through the renderer (e.g. to an ErrorBoundary) instead of
            // leaving them to die as an unobserved rejection of a browser-initiated call.
            _loading = false;
            _fetching = false;
            await DispatchExceptionAsync(ex);
        }
    }

    private void CacheProviderWindow(int start, IReadOnlyList<TItem> items)
    {
        _providerCache ??= [];
        for (var i = 0; i < items.Count; i++)
        {
            _providerCache[start + i] = items[i];
        }

        // Evict the entries farthest from the current window when the cache grows too large.
        if (_providerCache.Count > ProviderCacheCap)
        {
            var center = (_renderStart + _renderEnd) / 2;
            var farthest = _providerCache.Keys
                .OrderByDescending(k => Math.Abs(k - center))
                .Take(_providerCache.Count - ProviderCacheCap)
                .ToArray();
            foreach (var key in farthest)
            {
                _providerCache.Remove(key);
            }
        }
    }

    private bool IsRangeAvailable(int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            if (TryGetItem(i, out _) is false) return false;
        }
        return true;
    }

    private int EstimateInitialCount()
    {
        double size = Dynamic ? EstimatedSize : FixedSize;
        var viewport = _viewportSize > 0 ? _viewportSize : 600;
        return (int)Math.Ceiling(viewport / size) + (Overscan * 2) + 1;
    }

    private bool TryGetItem(int index, out TItem item)
    {
        if (_itemList is not null)
        {
            // Guarded: the same Items instance may have shrunk in place since the last recompute.
            if ((uint)index < (uint)_itemList.Count)
            {
                item = _itemList[index];
                return true;
            }

            item = default!;
            return false;
        }

        if (_loadedItems is not null)
        {
            var local = index - _loadedStart;
            if (local >= 0 && local < _loadedItems.Count)
            {
                item = _loadedItems[local];
                return true;
            }
        }

        if (_providerCache is not null && _providerCache.TryGetValue(index, out var cached))
        {
            item = cached;
            return true;
        }

        item = default!;
        return false;
    }

    private object GetItemKeyAt(int index, bool hasItem, TItem item)
    {
        if (ItemKey is null) return index;

        // Rows without an identity get a key of their own type so they can never collide with an item's key.
        return (hasItem ? ItemKey(item) : null) ?? new IndexKey(index);
    }

    private void CacheMeasuredSize(int index, double size)
    {
        if (ItemKey is null) return;
        if (TryGetItem(index, out var item) is false) return;

        var key = ItemKey(item);
        if (key is null) return;

        _sizeByKey ??= [];
        if (_sizeByKey.ContainsKey(key) is false && _sizeByKey.Count >= SizeCacheCap) return;
        _sizeByKey[key] = size;
    }

    private void ReseedTreeFromKeys()
    {
        if (Dynamic is false || _tree is null || ItemKey is null || _sizeByKey is null || _sizeByKey.Count == 0) return;

        if (_itemList is not null)
        {
            // Every item's size follows its key; an item with no known size (e.g. a newly inserted one) gets the
            // estimate rather than the stale size of whichever item held its index before.
            var estimate = EstimatedSize;
            var count = Math.Min(_itemList.Count, _tree.Count);
            for (var i = 0; i < count; i++)
            {
                var key = ItemKey(_itemList[i]);
                _tree.SetSize(i, key is not null && _sizeByKey.TryGetValue(key, out var size) ? size : estimate);
            }
        }
        else if (_loadedItems is not null)
        {
            for (var local = 0; local < _loadedItems.Count; local++)
            {
                var idx = _loadedStart + local;
                if (idx >= _tree.Count) break;

                var key = ItemKey(_loadedItems[local]);
                if (key is not null && _sizeByKey.TryGetValue(key, out var size))
                {
                    _tree.SetSize(idx, size);
                }
            }
        }
    }

    private double GetItemOffset(int index) => _tree is not null ? _tree.PrefixSum(index) : index * (double)FixedSize;

    private double GetItemSize(int index) => _tree is not null ? _tree.GetSize(index) : FixedSize;

    private double GetTotalSize() => _tree is not null ? _tree.Total : _itemCount * (double)FixedSize;

    private int FindIndexAtOffset(double offset)
    {
        if (_itemCount == 0) return 0;

        if (_tree is not null) return _tree.FindIndex(offset);

        var index = (int)Math.Floor(offset / FixedSize);
        return Math.Clamp(index, 0, _itemCount - 1);
    }

    private void RecomputeRange()
    {
        if (_itemCount == 0 || _viewportSize <= 0)
        {
            _ratio = 1d;
            _realTotal = Math.Min(GetTotalSize(), MaxCssSize);
            int prevStart = _visibleStart, prevEnd = _visibleEnd;
            _visibleStart = _renderStart = 0;
            _visibleEnd = _renderEnd = Math.Min(_itemCount, _initialized ? 0 : EstimateInitialCount());
            _renderStartOffset = 0;
            _blockOffset = 0;
            _stickyActiveIndex = -1;
            if (prevStart != _visibleStart || prevEnd != _visibleEnd)
            {
                NotifyRangeChanged();
            }
            return;
        }

        UpdateScale();

        var viewport = _viewportSize;
        var offset = Math.Clamp(_scrollOffset, 0, MaxScrollOffset);

        var start = FindIndexAtOffset(offset);
        // An item starting exactly at the far edge of the viewport is not in view.
        var end = FindIndexAtOffset(Math.Max(offset, offset + viewport - 0.01)) + 1;
        end = Math.Min(end, _itemCount);

        var newVisibleStart = start;
        var newVisibleEnd = end;
        var newRenderStart = Math.Max(0, start - Overscan);
        var newRenderEnd = Math.Min(_itemCount, end + Overscan);

        var rangeChanged = newVisibleStart != _visibleStart || newVisibleEnd != _visibleEnd;

        _visibleStart = newVisibleStart;
        _visibleEnd = newVisibleEnd;
        _renderStart = newRenderStart;
        _renderEnd = newRenderEnd;
        _renderStartOffset = GetItemOffset(_renderStart);

        // Unscaled, the block sits at the exact offset of its first item. Scaled, the items keep their real
        // sizes, so the block is placed where the item at the virtual scroll offset meets the real one.
        _blockOffset = _ratio <= 1d
            ? _renderStartOffset
            : Math.Clamp(_realScrollOffset, 0, Math.Max(0, _realTotal - viewport)) + (_renderStartOffset - offset);

        if (ItemKey is not null && _itemList is not null && (uint)_visibleStart < (uint)_itemList.Count)
        {
            _anchorIndex = _visibleStart;
            _anchorKey = ItemKey(_itemList[_visibleStart]);
        }

        UpdateSticky();
        CheckEdgesReached();

        if (rangeChanged)
        {
            NotifyRangeChanged();
        }
    }

    private void UpdateSticky()
    {
        if (_stickyIndices is null || _stickyIndices.Count == 0)
        {
            _stickyActiveIndex = -1;
            return;
        }

        // Greatest sticky index that starts at or before the first visible item.
        var active = -1;
        int lo = 0, hi = _stickyIndices.Count - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (_stickyIndices[mid] <= _visibleStart)
            {
                active = _stickyIndices[mid];
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        _stickyActiveIndex = active;
        if (active < 0) return;

        // The smallest sticky index after the active one; the browser uses its offset
        // to push the pinned header out as the next group header approaches.
        int l = 0, h = _stickyIndices.Count - 1, next = -1;
        while (l <= h)
        {
            var mid = (l + h) >> 1;
            if (_stickyIndices[mid] > active)
            {
                next = _stickyIndices[mid];
                h = mid - 1;
            }
            else
            {
                l = mid + 1;
            }
        }

        // The real position of the next header within the spacer, the way the rendered block places it.
        _stickyNextOffset = next >= 0 ? _blockOffset + (GetItemOffset(next) - _renderStartOffset) : -1;
    }

    private bool IsNearEnd() => GetTotalSize() - (_scrollOffset + _viewportSize) <= 4d;

    private void CheckEdgesReached()
    {
        if (_itemCount == 0) return;

        var threshold = Threshold;

        var atEnd = _visibleEnd >= _itemCount - threshold;
        if (OnEndReached.HasDelegate && atEnd && (_wasAtEnd is false || _lastEndReachedCount != _itemCount))
        {
            _lastEndReachedCount = _itemCount;
            _ = ObserveCallbackAsync(OnEndReached.InvokeAsync());
        }
        _wasAtEnd = atEnd;

        // Not fired for the initial position; fired again when the count changes while still at the start
        // (so a prepended batch too small to leave the threshold does not stall the history loading).
        var atStart = _visibleStart <= threshold;
        if (OnStartReached.HasDelegate && atStart && _initialScrollDone && (_wasAtStart is false || _lastStartReachedCount != _itemCount))
        {
            _lastStartReachedCount = _itemCount;
            _ = ObserveCallbackAsync(OnStartReached.InvokeAsync());
        }
        else if (atStart && _initialScrollDone is false)
        {
            _lastStartReachedCount = _itemCount;
        }
        _wasAtStart = atStart;
    }

    private void NotifyRangeChanged()
    {
        if (OnVisibleRangeChanged.HasDelegate is false) return;

        _ = ObserveCallbackAsync(OnVisibleRangeChanged.InvokeAsync((_visibleStart, _visibleEnd)));
    }

    // These callbacks fire from synchronous recompute paths, so their tasks cannot be awaited
    // in place; observing them surfaces consumer handler failures through the renderer instead
    // of silently swallowing them.
    private async Task ObserveCallbackAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private double ResolveAutoAlignment(double offset, double size)
    {
        if (offset < _scrollOffset)
        {
            return offset; // above the viewport -> align to start
        }

        if (offset + size > _scrollOffset + _viewportSize)
        {
            return offset - (_viewportSize - size); // below the viewport -> align to end
        }

        return _scrollOffset; // already visible -> no change
    }

    private string GetSpacerStyle()
    {
        var total = FormatCssValue(_realTotal);
        return Horizontal ? $"width:{total}px" : $"height:{total}px";
    }

    // The rendered items live inside a block placed at the (real) offset of the first rendered item; the items
    // themselves are positioned relative to the block in exact (unscaled) pixels. Horizontal lists use the
    // logical inline-start inset so they lay out from the right in RTL.
    private string GetBlockStyle()
    {
        var offset = FormatCssValue(_blockOffset);
        return Horizontal ? $"inset-inline-start:{offset}px" : $"transform:translateY({offset}px)";
    }

    private string GetItemStyle(int index)
    {
        var offset = FormatCssValue(GetItemOffset(index) - _renderStartOffset);
        // In fixed mode pin the size so each item exactly fills its slot.
        if (Horizontal)
        {
            return Dynamic ? $"inset-inline-start:{offset}px" : $"inset-inline-start:{offset}px;width:{FormatCssValue(FixedSize)}px";
        }

        return Dynamic ? $"transform:translateY({offset}px)" : $"transform:translateY({offset}px);height:{FormatCssValue(FixedSize)}px";
    }

    // The pinned header is positioned by CSS position:sticky (so it never lags behind the scroll);
    // this style only pins its size in fixed mode. The push-out transform is applied in the browser.
    private string GetStickyStyle() =>
        Dynamic ? string.Empty : Horizontal ? $"width:{FormatCssValue(FixedSize)}px" : $"height:{FormatCssValue(FixedSize)}px";

    // The root leaves the tab order only while the roving (tabindex=0) item is actually rendered,
    // so the list always stays reachable with the Tab key.
    private string GetRootTabIndex() =>
        _activeIndex >= _renderStart && _activeIndex < _renderEnd && _activeIndex < _itemCount ? "-1" : (TabIndex ?? "0");

    private static string FormatCssValue(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

        // Tear the browser side down first, so it cannot call into an already released reference.
        try
        {
            await _js.BitVirtualizeDispose(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _dotnetObj?.Dispose();

        await base.DisposeAsync(disposing);
    }



    // The @key of a row that has no identity of its own (a placeholder, or an item whose ItemKey is null).
    private readonly record struct IndexKey(int Index);
}
