using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// BitVirtualize is a high-performance virtualization (windowing) component that renders only the items
/// currently visible in its scroll viewport (plus a configurable overscan buffer). It supports fixed and
/// dynamically measured item sizes, vertical and horizontal orientation, in-memory or lazy-loaded data,
/// placeholders, sticky group headers, keyboard navigation, and a bottom-anchored (chat) mode with scroll
/// anchoring that prevents content from jumping as dynamic items get measured.
/// </summary>
public partial class BitVirtualize<TItem> : BitComponentBase
{
    // Browsers cap the maximum pixel size of a single element (~33.5M px in Chrome, ~17.8M in Firefox).
    // When the virtual extent exceeds this, the spacer gets scaled down and the rendered block is placed
    // at a scaled offset so the whole range stays reachable.
    private const double MaxCssSize = 10_000_000d;
    private const int ProviderCacheCap = 600;
    private const int SizeCacheCap = 20_000;

    private int _loadedStart;
    private int _itemCount;
    private bool _initialized;
    private bool _loading;
    private bool _refreshPending;

    private IList<TItem>? _itemList;            // materialized view of Items
    private ICollection<TItem>? _lastItems;     // the Items reference of the last recompute
    private int _lastItemsCount = -1;           // the Items count of the last recompute (detects same-instance mutations)
    private IReadOnlyList<TItem>? _loadedItems; // current provider window
    private Dictionary<int, TItem>? _providerCache; // previously loaded provider items, keyed by absolute index

    private BitVirtualizePrefixSumTree? _tree;  // dynamic mode only
    private Dictionary<object, double>? _sizeByKey; // dynamic + ItemKey: measured sizes keyed by item identity
    private double _scale = 1d;                 // spacer compression ratio (1 unless the extent exceeds MaxCssSize)
    private double _scrollOffset;               // virtual scroll offset (item-coordinate space)
    private double _viewportSize;               // real viewport size (px)
    private double _renderStartOffset;          // virtual offset of the first rendered item (block base)
    private int _visibleStart;
    private int _visibleEnd;   // exclusive
    private int _renderStart;
    private int _renderEnd;    // exclusive

    private CancellationTokenSource? _loadCts;

    // Infinite-scroll edge tracking.
    private int _lastEndReachedCount = -1;
    private bool _wasAtStart;
    private bool _wasAtEnd;

    // Sticky (grouped) header tracking.
    private List<int>? _stickyIndices;   // sorted indices flagged by IsStickyItem
    private int _stickyActiveIndex = -1;
    private double _stickyNextOffset = -1;

    // Reversed / chat (bottom-anchored) tracking.
    private bool _pendingScrollToEnd;
    private double _preserveEndDistance = -1;
    private bool _initialScrollDone;
    private bool _stickToEnd;

    // Keyboard navigation.
    private int _activeIndex = -1;
    private int _pendingFocusIndex = -1;

    // Dynamic ScrollToIndex precision: re-align once the target region is measured.
    private int _pendingScrollIndex = -1;
    private BitVirtualizeScrollAlignment _pendingScrollAlignment;

    private DotNetObjectReference<BitVirtualize<TItem>>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The custom template to render each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

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
    /// A function that returns a stable identity key for an item. When provided, rendered rows are keyed by
    /// identity (instead of by index) so per-item DOM/component state survives insertions, removals and
    /// reordering, and dynamic measurements follow their item across those mutations.
    /// </summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

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
    /// The custom template to render before the component performs its first load.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// The callback to be called when the last item comes within ReachedThreshold items of the visible window,
    /// useful for appending more data in infinite scrolling scenarios. Fires once per item-count value.
    /// </summary>
    [Parameter] public EventCallback OnEndReached { get; set; }

    /// <summary>
    /// The callback to be called when the first item comes within ReachedThreshold items of the visible window,
    /// useful for prepending older data (for example, loading chat history when scrolling up).
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
    /// The custom template to render the pinned sticky item. Falls back to the item template when not provided.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? StickyTemplate { get; set; }




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
            var atEnd = Reversed && (_stickToEnd || IsNearEnd());
            var prevTotal = GetTotalSize();

            _itemList = Items as IList<TItem> ?? [.. Items];
            _lastItemsCount = _itemList.Count;
            SetItemCount(_itemList.Count);
            ComputeStickyIndices();
            RecomputeRange();

            if (Reversed && GetTotalSize() > prevTotal)
            {
                if (atEnd)
                {
                    // User was at the bottom: keep the newest items in view.
                    _pendingScrollToEnd = true;
                }
                else
                {
                    // Content grew (likely prepended history): keep the viewport anchored
                    // to the same distance from the end so it does not jump.
                    _preserveEndDistance = prevTotal - (_scrollOffset + ViewportVirtual);
                }
            }

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
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="alignment">Where the item should be positioned within the viewport.</param>
    /// <param name="smooth">Whether to animate the scroll.</param>
    public Task ScrollToIndexAsync(int index, BitVirtualizeScrollAlignment alignment = BitVirtualizeScrollAlignment.Start, bool smooth = false)
        => ScrollToIndexCoreAsync(index, alignment, smooth, markPending: Dynamic);

    private async Task ScrollToIndexCoreAsync(int index, BitVirtualizeScrollAlignment alignment, bool smooth, bool markPending)
    {
        if (_initialized is false || _itemCount == 0) return;

        index = Math.Clamp(index, 0, _itemCount - 1);
        var offset = GetItemOffset(index);
        var size = GetItemSize(index);
        var target = alignment switch
        {
            BitVirtualizeScrollAlignment.Start => offset,
            BitVirtualizeScrollAlignment.Center => offset - (ViewportVirtual - size) / 2d,
            BitVirtualizeScrollAlignment.End => offset - (ViewportVirtual - size),
            _ => ResolveAutoAlignment(offset, size)
        };

        // In dynamic mode the offset is derived from estimates for unmeasured items, so remember the
        // request and re-align once the target region reports its real sizes (see _ItemsMeasured).
        if (markPending)
        {
            _pendingScrollIndex = index;
            _pendingScrollAlignment = alignment;
        }

        await ScrollToOffsetAsync(target, smooth);
    }

    /// <summary>
    /// Scrolls to an absolute pixel offset along the scroll axis.
    /// </summary>
    public async Task ScrollToOffsetAsync(double offset, bool smooth = false)
    {
        if (_initialized is false) return;

        var maxVirtual = Math.Max(0, GetTotalSize() - ViewportVirtual);
        var virtualTarget = Math.Clamp(offset, 0, maxVirtual);

        var real = RealFromVirtual(virtualTarget);
        var maxReal = Math.Max(0, RealFromVirtual(GetTotalSize()) - _viewportSize);
        real = Math.Clamp(real, 0, maxReal);

        await _js.BitVirtualizeScrollToOffset(UniqueId, real, smooth);
    }

    /// <summary>
    /// Scrolls to the start (top/left) of the list.
    /// </summary>
    public Task ScrollToStartAsync(bool smooth = false) => ScrollToOffsetAsync(0, smooth);

    /// <summary>
    /// Scrolls to the end (bottom/right) of the list. Useful for chat and log views.
    /// </summary>
    public Task ScrollToEndAsync(bool smooth = false) => ScrollToOffsetAsync(GetTotalSize(), smooth);



    [JSInvokable("Scroll")]
    public async Task _Scroll(double scrollOffset, double viewportSize)
    {
        if (IsDisposed) return;

        _scrollOffset = VirtualFromReal(scrollOffset);
        _viewportSize = viewportSize;

        if (Reversed)
        {
            _stickToEnd = IsNearEnd();
        }

        int prevRenderStart = _renderStart, prevRenderEnd = _renderEnd;
        RecomputeRange();

        if (_renderStart != prevRenderStart || _renderEnd != prevRenderEnd)
        {
            if (ItemsProvider is not null)
            {
                await LoadProviderWindowAsync(forceCount: false);
            }

            StateHasChanged();
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
            if (idx < 0 || idx >= _itemCount) continue;

            if (_tree.SetSize(idx, sizes[i]) != 0d)
            {
                changed = true;
            }

            CacheMeasuredSize(idx, sizes[i]);
        }

        if (changed is false) return;

        // Scroll anchoring: keep the first visible item visually stable when the
        // cumulative size of the items above it changes.
        var newAnchorOffset = _tree.PrefixSum(anchor);
        var diff = newAnchorOffset - oldAnchorOffset;
        if (Math.Abs(diff) > 0.01 && _initialized)
        {
            _scrollOffset += diff;
            await _js.BitVirtualizeAdjustScroll(UniqueId, RealFromVirtual(diff));
        }

        RecomputeRange();
        StateHasChanged();

        // Re-align a pending ScrollToIndex now that the target region has real measurements.
        if (_pendingScrollIndex >= 0 && _initialized)
        {
            var target = _pendingScrollIndex;
            var alignment = _pendingScrollAlignment;
            _pendingScrollIndex = -1;
            await ScrollToIndexCoreAsync(target, alignment, smooth: false, markPending: false);
        }

        // In reversed (chat) mode, keep the newest items pinned as measurements settle.
        if (Reversed && _stickToEnd && _initialized)
        {
            await ScrollToEndAsync();
        }
    }

    [JSInvokable("KeyNavigate")]
    public async Task _KeyNavigate(string key)
    {
        if (IsDisposed || _initialized is false || _itemCount == 0) return;

        var current = _activeIndex >= 0 ? _activeIndex : _visibleStart;
        var page = Math.Max(1, _visibleEnd - _visibleStart);
        var target = key switch
        {
            "ArrowDown" or "ArrowRight" => current + 1,
            "ArrowUp" or "ArrowLeft" => current - 1,
            "PageDown" => current + page,
            "PageUp" => current - page,
            "Home" => 0,
            "End" => _itemCount - 1,
            _ => current
        };

        target = Math.Clamp(target, 0, _itemCount - 1);
        if (target == _activeIndex && _renderStart <= target && target < _renderEnd) return;

        _activeIndex = target;
        _pendingFocusIndex = target;

        // Bring the target into view (and into the rendered window) before focusing it.
        var offset = GetItemOffset(target);
        var size = GetItemSize(target);
        _scrollOffset = Math.Clamp(ResolveAutoAlignment(offset, size), 0, Math.Max(0, GetTotalSize() - ViewportVirtual));
        RecomputeRange();

        if (ItemsProvider is not null)
        {
            await LoadProviderWindowAsync(forceCount: false);
        }

        StateHasChanged();
        await ScrollToOffsetAsync(_scrollOffset);
    }



    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Items is not null && ItemsProvider is not null)
        {
            throw new InvalidOperationException($"BitVirtualize requires either {nameof(Items)} or {nameof(ItemsProvider)}, but not both.");
        }

        // Recompute when the Items reference changes, or when the same instance's count changed
        // (a common in-place add/remove); deeper in-place mutations are picked up via RefreshDataAsync.
        if (Items is not null && (ReferenceEquals(Items, _lastItems) is false || Items.Count != _lastItemsCount))
        {
            _lastItems = Items;
            _itemList = Items as IList<TItem> ?? [.. Items];
            _lastItemsCount = _itemList.Count;
            SetItemCount(_itemList.Count);
            _loadedItems = null;
            ComputeStickyIndices();
            RecomputeRange();
        }
        // Provider count is discovered on the first load.
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                _dotnetObj = DotNetObjectReference.Create(this);
                var threshold = Dynamic ? 0d : ItemSize;
                var metrics = await _js.BitVirtualizeSetup(UniqueId, RootElement, Horizontal, threshold, _dotnetObj);

                // metrics is null when the js runtime is not available (e.g. prerendering).
                if (metrics is not null)
                {
                    _viewportSize = metrics.ViewportSize;
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
            else
            {
                if (Dynamic && _initialized && _itemCount > 0)
                {
                    // Reconcile ResizeObserver subscriptions for newly rendered/removed items
                    // and refresh the sticky header push-out transform.
                    await _js.BitVirtualizeSyncMeasurements(UniqueId);
                }
                else if (_stickyIndices is not null && _initialized && _itemCount > 0)
                {
                    await _js.BitVirtualizeUpdateSticky(UniqueId);
                }

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
        ClassBuilder.Register(() => Horizontal ? "bit-vir-hor" : string.Empty);
    }



    private double ViewportVirtual => _scale > 0 ? _viewportSize / _scale : _viewportSize;

    private double RealFromVirtual(double value) => value * _scale;

    private double VirtualFromReal(double value) => _scale > 0 ? value / _scale : value;

    private void UpdateScale()
    {
        var total = GetTotalSize();
        _scale = total > MaxCssSize ? MaxCssSize / total : 1d;
    }

    private async Task TryApplyInitialScrollAsync()
    {
        if (_initialScrollDone || _initialized is false || _itemCount == 0) return;

        _initialScrollDone = true;

        if (Reversed)
        {
            _stickToEnd = true;
            await ScrollToEndAsync();
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
            await ScrollToEndAsync();
        }
        else if (_preserveEndDistance >= 0)
        {
            // Restore the distance from the end after a prepend so the viewport stays put.
            var target = Math.Max(0, GetTotalSize() - ViewportVirtual - _preserveEndDistance);
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

    private void ComputeStickyIndices()
    {
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
                _tree = new BitVirtualizePrefixSumTree(count, EstimatedItemSize);
            }
            else
            {
                // Keep the already measured sizes of the surviving indices so a count change
                // (e.g. infinite-scroll append) does not throw away the measurements.
                _tree.Resize(count, EstimatedItemSize);
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
        try
        {
            var result = await ItemsProvider(new BitVirtualizeItemsProviderRequest(start, count, token));

            if (token.IsCancellationRequested) return;

            if (result.TotalItemCount != _itemCount)
            {
                SetItemCount(result.TotalItemCount);
            }

            _loadedItems = result.Items;
            _loadedStart = start;
            _loading = false;

            CacheProviderWindow(start, result.Items);
            ComputeStickyIndices();
            RecomputeRange();
            StateHasChanged();
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer request; ignore.
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
        double size = Dynamic ? EstimatedItemSize : ItemSize;
        var viewport = _viewportSize > 0 ? _viewportSize : 600;
        return (int)Math.Ceiling(viewport / Math.Max(1, size)) + (OverscanCount * 2) + 1;
    }

    private bool TryGetItem(int index, out TItem item)
    {
        if (_itemList is not null)
        {
            item = _itemList[index];
            return true;
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
        if (ItemKey is not null && hasItem)
        {
            return ItemKey(item) ?? index;
        }
        return index;
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
            var count = Math.Min(_itemList.Count, _tree.Count);
            for (var i = 0; i < count; i++)
            {
                var key = ItemKey(_itemList[i]);
                if (key is not null && _sizeByKey.TryGetValue(key, out var size))
                {
                    _tree.SetSize(i, size);
                }
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

    private double GetItemOffset(int index) => Dynamic ? _tree!.PrefixSum(index) : index * (double)ItemSize;

    private double GetItemSize(int index) => Dynamic ? _tree!.GetSize(index) : ItemSize;

    private double GetTotalSize() => Dynamic ? _tree!.Total : _itemCount * (double)ItemSize;

    private int FindIndexAtOffset(double offset)
    {
        if (_itemCount == 0) return 0;

        if (Dynamic) return _tree!.FindIndex(offset);

        var index = (int)Math.Floor(offset / Math.Max(1, ItemSize));
        return Math.Clamp(index, 0, _itemCount - 1);
    }

    private void RecomputeRange()
    {
        if (_itemCount == 0 || _viewportSize <= 0)
        {
            _scale = 1d;
            int prevStart = _visibleStart, prevEnd = _visibleEnd;
            _visibleStart = _renderStart = 0;
            _visibleEnd = _renderEnd = Math.Min(_itemCount, _initialized ? 0 : EstimateInitialCount());
            _renderStartOffset = 0;
            _stickyActiveIndex = -1;
            if (prevStart != _visibleStart || prevEnd != _visibleEnd)
            {
                NotifyRangeChanged();
            }
            return;
        }

        UpdateScale();

        var viewport = ViewportVirtual;
        var maxOffset = Math.Max(0, GetTotalSize() - viewport);
        var offset = Math.Clamp(_scrollOffset, 0, maxOffset);

        var start = FindIndexAtOffset(offset);
        var end = FindIndexAtOffset(offset + viewport) + 1;
        end = Math.Min(end, _itemCount);

        var newVisibleStart = start;
        var newVisibleEnd = end;
        var newRenderStart = Math.Max(0, start - OverscanCount);
        var newRenderEnd = Math.Min(_itemCount, end + OverscanCount);

        var rangeChanged = newVisibleStart != _visibleStart || newVisibleEnd != _visibleEnd;

        _visibleStart = newVisibleStart;
        _visibleEnd = newVisibleEnd;
        _renderStart = newRenderStart;
        _renderEnd = newRenderEnd;
        _renderStartOffset = GetItemOffset(_renderStart);

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

        _stickyNextOffset = next >= 0 ? RealFromVirtual(GetItemOffset(next)) : -1;
    }

    private bool IsNearEnd() => GetTotalSize() - (_scrollOffset + ViewportVirtual) <= 4d;

    private void CheckEdgesReached()
    {
        if (_itemCount == 0) return;

        var atEnd = _visibleEnd >= _itemCount - ReachedThreshold;
        if (OnEndReached.HasDelegate && atEnd && (_wasAtEnd is false || _lastEndReachedCount != _itemCount))
        {
            _lastEndReachedCount = _itemCount;
            _ = ObserveCallbackAsync(OnEndReached.InvokeAsync());
        }
        _wasAtEnd = atEnd;

        var atStart = _visibleStart <= ReachedThreshold;
        if (OnStartReached.HasDelegate && atStart && _wasAtStart is false && _initialScrollDone)
        {
            _ = ObserveCallbackAsync(OnStartReached.InvokeAsync());
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

        if (offset + size > _scrollOffset + ViewportVirtual)
        {
            return offset - (ViewportVirtual - size); // below the viewport -> align to end
        }

        return _scrollOffset; // already visible -> no change
    }

    private string GetSpacerStyle()
    {
        var total = FormatCssValue(RealFromVirtual(GetTotalSize()));
        return Horizontal ? $"width:{total}px" : $"height:{total}px";
    }

    // The rendered items live inside a block translated to the (scaled) offset of the first rendered item.
    // This keeps the whole extent reachable even past the browser's max-element-size limit, while the items
    // themselves are positioned relative to the block in exact (unscaled) pixels.
    private string GetBlockStyle()
    {
        var offset = FormatCssValue(RealFromVirtual(_renderStartOffset));
        return Horizontal ? $"transform:translateX({offset}px)" : $"transform:translateY({offset}px)";
    }

    private string GetItemStyle(int index)
    {
        var offset = FormatCssValue(GetItemOffset(index) - _renderStartOffset);
        // In fixed mode pin the size so each item exactly fills its slot.
        var size = Dynamic ? string.Empty : Horizontal ? $"width:{FormatCssValue(ItemSize)}px" : $"height:{FormatCssValue(ItemSize)}px";
        return Horizontal ? $"transform:translateX({offset}px);{size}" : $"transform:translateY({offset}px);{size}";
    }

    // The pinned header is positioned by CSS position:sticky (so it never lags behind the scroll);
    // this style only pins its size in fixed mode. The push-out transform is applied in the browser.
    private string GetStickyStyle() =>
        Dynamic ? string.Empty : Horizontal ? $"width:{FormatCssValue(ItemSize)}px" : $"height:{FormatCssValue(ItemSize)}px";

    private string GetRootTabIndex() => _activeIndex >= 0 ? "-1" : (TabIndex?.ToString() ?? "0");

    private static string FormatCssValue(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

        _dotnetObj?.Dispose();

        try
        {
            await _js.BitVirtualizeDispose(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
