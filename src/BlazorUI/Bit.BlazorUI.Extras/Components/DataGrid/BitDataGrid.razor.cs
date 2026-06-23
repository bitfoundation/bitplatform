using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

/// <summary>
/// A feature-rich, generic data grid for Blazor: sorting, filtering, paging,
/// virtualization, selection, inline editing, column resize/reorder, frozen
/// columns, grouping, aggregates and theming.
/// </summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public partial class BitDataGrid<TItem> : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // ---------------------------------------------------------------- Data
    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Server-side data callback. When set, the grid delegates sort/filter/page to the caller.</summary>
    [Parameter] public Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TItem>>>? OnRead { get; set; }

    /// <summary>
    /// Infinite-scrolling data callback. When set, the grid loads rows in batches and appends the
    /// next batch automatically as the user scrolls to the end of the viewport — with no paging UI
    /// and no knowledge of the total row count. Each call receives a <see cref="BitDataGridReadRequest"/>
    /// whose <c>Skip</c> is the number of rows already loaded and whose <c>Take</c> is
    /// <see cref="LoadMoreBatchSize"/>. The grid stops requesting more once a batch returns fewer rows
    /// than requested (signalling the end of the data). Mirrors react-data-grid's Infinite Scrolling.
    /// Requires a fixed <see cref="Height"/>. The returned <c>TotalCount</c> is ignored in this mode.
    /// </summary>
    [Parameter] public Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TItem>>>? OnLoadMore { get; set; }

    /// <summary>Number of rows fetched per batch in infinite-scrolling mode. Default: 50.</summary>
    [Parameter] public int LoadMoreBatchSize { get; set; } = 50;

    /// <summary>Column definitions and other declarative children.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public bool Loading { get; set; }

    /// <summary>Optional key selector used for selection/edit identity. Defaults to reference equality.</summary>
    [Parameter] public Func<TItem, object>? KeyField { get; set; }

    /// <summary>
    /// Optional child selector that turns the grid into a hierarchical <b>tree grid</b>.
    /// Return the direct children of an item, or <c>null</c>/empty for a leaf. When set,
    /// the bound <see cref="Items"/> are treated as the root nodes and rows render with
    /// expand/collapse toggles and indentation. Mirrors react-data-grid's Tree View.
    /// </summary>
    [Parameter] public Func<TItem, IEnumerable<TItem>?>? ChildrenSelector { get; set; }

    /// <summary>When tree mode is active, controls whether nodes start expanded. Default: collapsed.</summary>
    [Parameter] public bool TreeInitiallyExpanded { get; set; }

    // ------------------------------------------------------------ Appearance
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    /// <summary>Height of the scroll viewport, e.g. "480px". Required for virtualization.</summary>
    [Parameter] public string? Height { get; set; }
    [Parameter] public bool Striped { get; set; } = true;
    [Parameter] public bool Hoverable { get; set; } = true;
    [Parameter] public bool Bordered { get; set; } = true;
    [Parameter] public bool ShowHeader { get; set; } = true;
    [Parameter] public bool ShowFooter { get; set; }
    [Parameter] public BitDir Direction { get; set; } = BitDir.Ltr;

    // -------------------------------------------------------- Feature toggles
    [Parameter] public bool Sortable { get; set; } = true;
    [Parameter] public bool MultiSort { get; set; } = true;
    [Parameter] public bool Filterable { get; set; }
    [Parameter] public bool Resizable { get; set; }
    [Parameter] public bool Reorderable { get; set; }
    [Parameter] public bool Groupable { get; set; }
    [Parameter] public bool ShowToolbar { get; set; }
    [Parameter] public bool ShowColumnChooser { get; set; }
    [Parameter] public bool ShowCsvExport { get; set; }

    /// <summary>
    /// Enables keyboard cell navigation. Cells become focusable via a roving tabindex and
    /// respond to arrow keys, <kbd>Home</kbd>/<kbd>End</kbd>, <kbd>PageUp</kbd>/<kbd>PageDown</kbd>
    /// (and <kbd>Ctrl</kbd> variants). <kbd>Enter</kbd>/<kbd>F2</kbd> begins editing an editable
    /// cell and <kbd>Esc</kbd> cancels. Mirrors react-data-grid's Cell Navigation. No JavaScript
    /// is used — focus is driven by Blazor's built-in <c>FocusAsync</c>.
    /// </summary>
    [Parameter] public bool CellNavigation { get; set; }

    /// <summary>
    /// Enables drag-and-drop row reordering using native HTML drag-and-drop (no JS interop).
    /// Provide <see cref="OnRowReorder"/> to persist the new order.
    /// </summary>
    [Parameter] public bool RowReorderable { get; set; }

    /// <summary>
    /// Raised when a row is dropped onto another row during reordering. The grid reorders the
    /// bound <see cref="Items"/> list in place when it is a mutable <see cref="IList{T}"/>;
    /// use this callback to persist or override the change.
    /// </summary>
    [Parameter] public EventCallback<BitDataGridRowReorderEventArgs<TItem>> OnRowReorder { get; set; }

    // ------------------------------------------------------------- Selection
    [Parameter] public BitDataGridSelectionMode SelectionMode { get; set; } = BitDataGridSelectionMode.None;
    [Parameter] public IReadOnlyList<TItem>? SelectedItems { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<TItem>> SelectedItemsChanged { get; set; }
    [Parameter] public EventCallback<TItem> OnRowClick { get; set; }

    /// <summary>Raised when a data cell is clicked.</summary>
    [Parameter] public EventCallback<BitDataGridCellEventArgs<TItem>> OnCellClick { get; set; }

    /// <summary>Raised when a data cell is double-clicked.</summary>
    [Parameter] public EventCallback<BitDataGridCellEventArgs<TItem>> OnCellDoubleClick { get; set; }

    /// <summary>Raised when a data cell is right-clicked. Useful for custom context menus.</summary>
    [Parameter] public EventCallback<BitDataGridCellEventArgs<TItem>> OnCellContextMenu { get; set; }

    /// <summary>
    /// Optional predicate that returns <c>true</c> when a given row may not be selected.
    /// Mirrors react-data-grid's <c>isRowSelectionDisabled</c>; such rows are skipped by
    /// select-all and render a disabled checkbox.
    /// </summary>
    [Parameter] public Func<TItem, bool>? IsRowSelectionDisabled { get; set; }

    // --------------------------------------------------------------- Paging
    [Parameter] public bool Pageable { get; set; }
    [Parameter] public int PageSize { get; set; } = 20;
    [Parameter] public int[] PageSizeOptions { get; set; } = { 10, 20, 50, 100 };
    [Parameter] public BitDataGridPagerPosition PagerPosition { get; set; } = BitDataGridPagerPosition.Bottom;

    // --------------------------------------------------------- Virtualization
    [Parameter] public bool Virtualize { get; set; }
    [Parameter] public float RowHeight { get; set; } = 36f;

    /// <summary>
    /// Optional per-row height selector (in pixels). Mirrors react-data-grid's functional
    /// <c>rowHeight</c>. Ignored while <see cref="Virtualize"/> is enabled, which requires a
    /// uniform <see cref="RowHeight"/>.
    /// </summary>
    [Parameter] public Func<TItem, float>? RowHeightSelector { get; set; }

    // -------------------------------------------------------------- Editing
    [Parameter] public bool Editable { get; set; }
    [Parameter] public Func<TItem>? NewItemFactory { get; set; }
    [Parameter] public EventCallback<TItem> OnRowSave { get; set; }
    [Parameter] public EventCallback<TItem> OnRowCancel { get; set; }
    [Parameter] public EventCallback<TItem> OnRowDelete { get; set; }
    [Parameter] public EventCallback<TItem> OnRowCreate { get; set; }

    // ------------------------------------------------------------ Templates
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }
    [Parameter] public RenderFragment? ToolbarTemplate { get; set; }
    [Parameter] public RenderFragment<TItem>? DetailTemplate { get; set; }

    // ---------------------------------------------------------------- State
    private readonly List<BitDataGridColumn<TItem>> _columns = new();
    private readonly Dictionary<string, BitDataGridColumn<TItem>> _columnsById = new();
    private readonly List<BitDataGridSortDescriptor> _sorts = new();
    private readonly List<BitDataGridFilterDescriptor> _filters = new();
    private readonly List<BitDataGridGroupDescriptor> _groups = new();
    // Tracks the selected rows by their key (via GetKey) rather than by object reference, so a
    // selection survives data refreshes that produce new TItem instances with the same key.
    private HashSet<TItem>? _selectedSet;
    private HashSet<TItem> _selected => _selectedSet ??= new HashSet<TItem>(new KeySelectionComparer(GetKey));
    private readonly HashSet<object> _expandedDetails = new();
    private readonly HashSet<object> _collapsedGroups = new();

    // tree mode
    private readonly HashSet<object> _expandedTree = new();
    private readonly Dictionary<object, (int Level, bool HasChildren)> _treeMeta = new();
    private List<TItem>? _treeRows;
    private bool _treeInitialized;

    // cell navigation
    private TItem? _focusedRow;
    private int _focusedCol;
    private bool _focusPending;

    private IReadOnlyList<TItem> _view = Array.Empty<TItem>();      // filtered + sorted (full)
    private IReadOnlyList<TItem> _pageItems = Array.Empty<TItem>(); // current page slice
    private List<BitDataGridGroup<TItem>>? _viewGroups;
    private List<BitDataGridAggregateResult> _footerAggregates = new();
    private int _totalCount;

    private int _currentPage = 1;
    private int _effectivePageSize;
    private bool _showColumnChooserPanel;

    // Tracks external data inputs so we only (re)load when they actually change,
    // rather than on every parent re-render (which would loop in server mode).
    private bool _dataInitialized;
    private IEnumerable<TItem>? _lastItems;
    private int _lastPageSize;
    private BitDataGridSelectionMode? _lastSelectionMode;

    // editing
    private TItem? _editItem;
    private TItem? _pendingNew;
    private bool _isNewItem;
    private Dictionary<string, object?>? _editSnapshot;

    // resizing
    private BitDataGridColumn<TItem>? _resizingColumn;
    private double _resizeStartX;
    private double _resizeStartWidth;

    // reordering
    private BitDataGridColumn<TItem>? _dragColumn;
    private TItem? _dragRow;

    // infinite scrolling
    private readonly List<TItem> _infiniteItems = new();
    private bool _infiniteHasMore = true;
    private bool _infiniteLoading;
    private ElementReference _infiniteViewport;
    private DotNetObjectReference<BitDataGrid<TItem>>? _infiniteSelfRef;
    private IJSObjectReference? _infiniteHandle;
    private bool _infiniteObserverAttached;

    // Cancels superseded in-flight OnRead/OnLoadMore requests.
    private CancellationTokenSource? _loadCts;
    // Monotonic load version; bumped on every (re)load so a superseded response can detect it is stale.
    private int _loadVersion;

    internal IReadOnlyList<BitDataGridColumn<TItem>> AllColumns => _columns;
    internal IReadOnlyList<BitDataGridColumn<TItem>> VisibleColumns => _columns.Where(c => c.Visible).ToList();
    internal IReadOnlyList<BitDataGridSortDescriptor> Sorts => _sorts;
    internal bool IsServerMode => OnRead is not null;
    internal bool IsInfiniteMode => OnLoadMore is not null;
    internal bool IsTreeMode => ChildrenSelector is not null;
    internal bool IsEditing(TItem item) => _editItem is not null && KeyEquals(_editItem, item);
    internal bool IsRowSelected(TItem item) => _selected.Contains(item);
    internal int TotalCount => IsServerMode ? _totalCount : _view.Count;
    internal int TotalPages => _effectivePageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)_effectivePageSize));
    internal int CurrentPage => _currentPage;
    internal IReadOnlyList<BitDataGridAggregateResult> FooterAggregates => _footerAggregates;
    internal TItem? PendingNewItem => _pendingNew;

    // ------------------------------------------------- Column registration
    internal void AddColumn(BitDataGridColumn<TItem> column)
    {
        if (_columns.Contains(column)) return;
        _columns.Add(column);
        _columnsById[column.Id] = column;
        InvokeAsync(StateHasChanged);
    }

    internal void RemoveColumn(BitDataGridColumn<TItem> column)
    {
        if (_columns.Remove(column))
        {
            _columnsById.Remove(column.Id);
            InvokeAsync(StateHasChanged);
        }
    }

    // ------------------------------------------------------- Lifecycle
    protected override async Task OnParametersSetAsync()
    {
        _effectivePageSize = Pageable ? Math.Max(1, PageSize) : int.MaxValue;

        // Reset the current selection whenever the selection mode changes
        // (e.g. switching between Single and Multiple), since the previous
        // selection no longer makes sense under the new semantics.
        if (_lastSelectionMode is not null && _lastSelectionMode != SelectionMode)
        {
            if (_selected.Count > 0)
            {
                _selected.Clear();
                await NotifySelectionAsync();
            }
        }
        else if (SelectedItems is not null)
        {
            _selected.Clear();
            foreach (var i in SelectedItems) _selected.Add(i);
        }
        _lastSelectionMode = SelectionMode;

        // Only (re)load data when an external input that affects it actually changes.
        // Refreshing on every parameter set would cause an infinite loop in server mode:
        // OnRead -> caller StateHasChanged -> parent re-render -> OnParametersSetAsync -> OnRead...
        var inputsChanged = !ReferenceEquals(Items, _lastItems) || PageSize != _lastPageSize;
        if (!_dataInitialized || inputsChanged)
        {
            _lastItems = Items;
            _lastPageSize = PageSize;
            _dataInitialized = true;
            await RefreshAsync();
        }
    }

    /// <summary>Recomputes the data view (filter → sort → group → page).</summary>
    public async Task RefreshAsync()
    {
        if (IsInfiniteMode)
        {
            // True infinite scroll: reset the accumulated rows and load the first batch.
            // Further batches are appended as the user scrolls toward the end.
            await ResetInfiniteAsync();
            return;
        }

        if (IsServerMode)
        {
            await LoadServerDataAsync();
        }
        else
        {
            ProcessClientData();
        }
        StateHasChanged();
    }

    private void ProcessClientData()
    {
        var source = Items ?? Enumerable.Empty<TItem>();

        if (IsTreeMode)
        {
            ProcessTreeData(source);
            return;
        }

        var filtered = BitDataGridDataProcessor.Filter(source, _filters, _columnsById);
        _view = BitDataGridDataProcessor.Sort(filtered, _sorts, _columnsById);
        _footerAggregates = BitDataGridDataProcessor.Aggregate(_view, _columns);

        ClampPage();

        if (_groups.Count > 0)
        {
            _viewGroups = BitDataGridDataProcessor.Group(_view, _groups, _columnsById);
            _pageItems = _view; // grouping ignores paging in this implementation
        }
        else
        {
            _viewGroups = null;
            _pageItems = Pageable
                ? _view.Skip((_currentPage - 1) * _effectivePageSize).Take(_effectivePageSize).ToList()
                : _view;
        }
    }

    /// <summary>
    /// Flattens the hierarchical source into the list of currently-visible rows, honouring
    /// per-sibling sorting and expand/collapse state. Paging and grouping do not apply in tree mode.
    /// </summary>
    private void ProcessTreeData(IEnumerable<TItem> roots)
    {
        if (!_treeInitialized && TreeInitiallyExpanded)
        {
            ExpandTreeRecursive(roots);
            _treeInitialized = true;
        }

        var flat = new List<TItem>();
        _treeMeta.Clear();
        Walk(roots, 0);

        _treeRows = flat;
        _view = flat;
        _pageItems = flat;
        _viewGroups = null;
        _footerAggregates = BitDataGridDataProcessor.Aggregate(flat, _columns);

        void Walk(IEnumerable<TItem> siblings, int level)
        {
            var sorted = _sorts.Count > 0
                ? BitDataGridDataProcessor.Sort(siblings.ToList(), _sorts, _columnsById)
                : siblings.ToList();
            foreach (var item in sorted)
            {
                var children = ChildrenSelector!(item);
                var hasChildren = children is not null && children.Any();
                _treeMeta[GetKey(item)] = (level, hasChildren);
                flat.Add(item);
                if (hasChildren && IsTreeExpanded(item))
                    Walk(children!, level + 1);
            }
        }
    }

    private void ExpandTreeRecursive(IEnumerable<TItem> siblings)
    {
        foreach (var item in siblings)
        {
            var children = ChildrenSelector!(item);
            if (children is not null && children.Any())
            {
                _expandedTree.Add(GetKey(item));
                ExpandTreeRecursive(children);
            }
        }
    }

    // ------------------------------------------------------------- Tree view
    internal int TreeLevel(TItem item) => _treeMeta.TryGetValue(GetKey(item), out var m) ? m.Level : 0;
    internal bool TreeHasChildren(TItem item) => _treeMeta.TryGetValue(GetKey(item), out var m) && m.HasChildren;
    internal bool IsTreeExpanded(TItem item) => _expandedTree.Contains(GetKey(item));

    internal async Task ToggleTreeNodeAsync(TItem item)
    {
        var key = GetKey(item);
        if (!_expandedTree.Add(key)) _expandedTree.Remove(key);
        await RefreshAsync();
    }

    /// <summary>Expands every node in the tree. No-op outside tree mode.</summary>
    public async Task ExpandAllAsync()
    {
        if (!IsTreeMode) return;
        _expandedTree.Clear();
        ExpandTreeRecursive(Items ?? Enumerable.Empty<TItem>());
        await RefreshAsync();
    }

    /// <summary>Collapses every node in the tree. No-op outside tree mode.</summary>
    public async Task CollapseAllAsync()
    {
        if (!IsTreeMode) return;
        _expandedTree.Clear();
        await RefreshAsync();
    }

    // --------------------------------------------------- Infinite scrolling
    internal IReadOnlyList<TItem> InfiniteItems => _infiniteItems;
    internal bool InfiniteLoading => _infiniteLoading;
    internal bool InfiniteHasMore => _infiniteHasMore;

    /// <summary>Clears the accumulated rows and (re)loads the first batch. Used on init and whenever
    /// sorting/filtering changes in infinite-scrolling mode.</summary>
    private async Task ResetInfiniteAsync()
    {
        _infiniteItems.Clear();
        _infiniteHasMore = true;
        _infiniteLoading = false;
        _view = _infiniteItems;
        _pageItems = _infiniteItems;

        if (_infiniteHandle is not null)
        {
            try { await _infiniteHandle.InvokeVoidAsync("scrollToTop"); }
            catch (JSException) { }
            catch (JSDisconnectedException) { }
        }

        await LoadNextBatchAsync();
    }

    /// <summary>
    /// Appends the next batch of rows. No total count is assumed: the end of the data is detected
    /// when a batch returns fewer rows than requested (or none at all). Re-entrancy is guarded so
    /// rapid scroll events coalesce into a single in-flight request.
    /// </summary>
    private async Task LoadNextBatchAsync()
    {
        if (OnLoadMore is null || _infiniteLoading || !_infiniteHasMore) return;

        _infiniteLoading = true;
        StateHasChanged();

        var batch = Math.Max(1, LoadMoreBatchSize);
        var read = new BitDataGridReadRequest
        {
            Skip = _infiniteItems.Count,
            Take = batch,
            Sorts = _sorts.Where(s => s.Direction != BitDataGridSortDirection.None).OrderBy(s => s.Priority).ToList(),
            Filters = _filters.ToList(),
            CancellationToken = ResetLoadCancellation()
        };
        var version = _loadVersion;

        try
        {
            var result = await OnLoadMore(read);
            // A newer request superseded this one (e.g. sort/filter changed mid-flight); drop the stale response.
            if (version != _loadVersion) return;

            var loaded = result.Items;
            _infiniteItems.AddRange(loaded);
            if (loaded.Count < batch) _infiniteHasMore = false;

            _view = _infiniteItems;
            _pageItems = _infiniteItems;
            _footerAggregates = BitDataGridDataProcessor.Aggregate(_infiniteItems, _columns);
        }
        finally
        {
            // Only clear the loading flag if we are still the current request; a superseding request
            // owns the loading state otherwise.
            if (version == _loadVersion)
            {
                _infiniteLoading = false;
                StateHasChanged();
            }
        }
    }

    /// <summary>Invoked from JavaScript when the viewport is scrolled near its end.</summary>
    [JSInvokable]
    public async Task OnInfiniteScrollNearEndAsync()
    {
        if (!IsInfiniteMode) return;
        await LoadNextBatchAsync();
        // If the freshly loaded rows still don't fill the viewport, keep loading until they do
        // (or the data runs out). The JS re-check fires this method again only while near the end.
        if (_infiniteHasMore && _infiniteHandle is not null)
        {
            try { await _infiniteHandle.InvokeVoidAsync("check"); }
            catch (JSException) { }
            catch (JSDisconnectedException) { }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsInfiniteMode && !_infiniteObserverAttached)
        {
            _infiniteObserverAttached = true;
            _infiniteSelfRef ??= DotNetObjectReference.Create(this);
            _infiniteHandle = await JS.InvokeAsync<IJSObjectReference>(
                "BitBlazorUI.DataGrid.initInfiniteScroll", _infiniteViewport, _infiniteSelfRef, 200);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_infiniteHandle is not null)
            {
                await _infiniteHandle.InvokeVoidAsync("dispose");
                await _infiniteHandle.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        _infiniteSelfRef?.Dispose();
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Cancels any in-flight data request and returns a fresh token for the next one,
    /// so superseded requests can stop early.
    /// </summary>
    private CancellationToken ResetLoadCancellation()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        _loadVersion++;
        return _loadCts.Token;
    }

    private async Task LoadServerDataAsync()
    {
        var request = new BitDataGridReadRequest
        {
            Skip = Pageable ? (_currentPage - 1) * _effectivePageSize : 0,
            Take = Pageable ? _effectivePageSize : null,
            Sorts = _sorts.Where(s => s.Direction != BitDataGridSortDirection.None).OrderBy(s => s.Priority).ToList(),
            Filters = _filters.ToList(),
            CancellationToken = ResetLoadCancellation()
        };
        // Capture this request's version right after ResetLoadCancellation; bail out below if a newer
        // request has since superseded it so a stale response can't overwrite fresher state.
        var version = _loadVersion;
        var result = await OnRead!(request);
        if (version != _loadVersion) return;
        _pageItems = result.Items;
        _view = result.Items;
        _totalCount = result.TotalCount;
        _footerAggregates = BitDataGridDataProcessor.Aggregate(_pageItems, _columns);
        _viewGroups = null;
        ClampPage();
    }

    private void ClampPage()
    {
        var pages = TotalPages;
        if (_currentPage > pages) _currentPage = pages;
        if (_currentPage < 1) _currentPage = 1;
    }

    // ------------------------------------------------------------- Sorting
    internal bool ColumnSortable(BitDataGridColumn<TItem> column)
        => column.HasField && (column.Sortable ?? Sortable);

    internal BitDataGridSortDescriptor? GetSort(BitDataGridColumn<TItem> column)
        => _sorts.FirstOrDefault(s => s.ColumnId == column.Id);

    internal async Task ToggleSortAsync(BitDataGridColumn<TItem> column, bool additive)
    {
        if (!ColumnSortable(column)) return;
        var existing = GetSort(column);

        if (!additive)
        {
            // Non-additive action (plain click): clear all prior sorts regardless of MultiSort,
            // keeping only this column. Additive (Ctrl/⌘+click) preserves existing sorts.
            var keep = existing;
            _sorts.Clear();
            if (keep is not null) _sorts.Add(keep);
        }

        if (existing is null)
        {
            var initial = column.SortDescendingFirst
                ? BitDataGridSortDirection.Descending
                : BitDataGridSortDirection.Ascending;
            _sorts.Add(new BitDataGridSortDescriptor { ColumnId = column.Id, Direction = initial, Priority = _sorts.Count + 1 });
        }
        else if (existing.Direction == (column.SortDescendingFirst ? BitDataGridSortDirection.Descending : BitDataGridSortDirection.Ascending))
        {
            existing.Direction = column.SortDescendingFirst
                ? BitDataGridSortDirection.Ascending
                : BitDataGridSortDirection.Descending;
        }
        else
        {
            _sorts.Remove(existing);
        }
        Reprioritize();
        await RefreshAsync();
    }

    private void Reprioritize()
    {
        for (int i = 0; i < _sorts.Count; i++) _sorts[i].Priority = i + 1;
    }

    // ----------------------------------------------------------- Filtering
    internal bool ColumnFilterable(BitDataGridColumn<TItem> column)
        => column.HasField && (column.Filterable ?? Filterable);

    internal BitDataGridFilterDescriptor? GetFilter(BitDataGridColumn<TItem> column)
        => _filters.FirstOrDefault(f => f.ColumnId == column.Id);

    internal async Task SetFilterAsync(BitDataGridColumn<TItem> column, BitDataGridFilterOperator op, object? value)
    {
        var existing = GetFilter(column);
        var isEmpty = value is null || (value is string s && s.Length == 0);
        if (isEmpty && op is not (BitDataGridFilterOperator.IsEmpty or BitDataGridFilterOperator.IsNotEmpty))
        {
            if (existing is not null) _filters.Remove(existing);
        }
        else if (existing is null)
        {
            _filters.Add(new BitDataGridFilterDescriptor { ColumnId = column.Id, Operator = op, Value = value });
        }
        else
        {
            existing.Operator = op;
            existing.Value = value;
        }
        _currentPage = 1;
        await RefreshAsync();
    }

    public async Task ClearFiltersAsync()
    {
        _filters.Clear();
        await RefreshAsync();
    }

    // ----------------------------------------------------------- Grouping
    internal bool ColumnGroupable(BitDataGridColumn<TItem> column)
        => column.HasField && (column.Groupable ?? Groupable);

    internal bool IsGrouped(BitDataGridColumn<TItem> column) => _groups.Any(g => g.ColumnId == column.Id);

    internal async Task ToggleGroupAsync(BitDataGridColumn<TItem> column)
    {
        var existing = _groups.FirstOrDefault(g => g.ColumnId == column.Id);
        if (existing is null)
        {
            // Append as the next (nested) grouping level.
            _groups.Add(new BitDataGridGroupDescriptor { ColumnId = column.Id });
        }
        else
        {
            _groups.Remove(existing);
        }
        await RefreshAsync();
    }

    /// <summary>Removes all active groupings.</summary>
    public async Task ClearGroupsAsync()
    {
        if (_groups.Count == 0) return;
        _groups.Clear();
        await RefreshAsync();
    }

    internal int GroupLevel(BitDataGridColumn<TItem> column)
    {
        var idx = _groups.FindIndex(g => g.ColumnId == column.Id);
        return idx < 0 ? -1 : idx + 1;
    }

    internal bool IsGroupCollapsed(BitDataGridGroup<TItem> group) => _collapsedGroups.Contains(group.Path);
    internal void ToggleGroup(BitDataGridGroup<TItem> group)
    {
        if (!_collapsedGroups.Add(group.Path)) _collapsedGroups.Remove(group.Path);
        StateHasChanged();
    }

    // ---------------------------------------------------------- Selection
    internal bool SelectionEnabled => SelectionMode != BitDataGridSelectionMode.None;

    /// <summary>True when the given row is allowed to be selected.</summary>
    internal bool CanSelectRow(TItem item) => IsRowSelectionDisabled is null || !IsRowSelectionDisabled(item);

    internal async Task ToggleRowSelectionAsync(TItem item, bool? value = null)
    {
        if (SelectionMode == BitDataGridSelectionMode.None) return;
        if (!CanSelectRow(item)) return;
        var selected = value ?? !_selected.Contains(item);
        if (SelectionMode == BitDataGridSelectionMode.Single)
        {
            _selected.Clear();
            if (selected) _selected.Add(item);
        }
        else
        {
            if (selected) _selected.Add(item); else _selected.Remove(item);
        }
        await NotifySelectionAsync();
    }

    internal bool AllPageSelected
    {
        get
        {
            var selectable = _pageItems.Where(CanSelectRow).ToList();
            return selectable.Count > 0 && selectable.All(_selected.Contains);
        }
    }
    internal bool SomePageSelected
    {
        get
        {
            var selectable = _pageItems.Where(CanSelectRow).ToList();
            return selectable.Any(_selected.Contains) && !(selectable.Count > 0 && selectable.All(_selected.Contains));
        }
    }

    internal async Task ToggleSelectAllAsync(bool value)
    {
        foreach (var item in _pageItems)
        {
            if (!CanSelectRow(item)) continue;
            if (value) _selected.Add(item); else _selected.Remove(item);
        }
        await NotifySelectionAsync();
    }

    private async Task NotifySelectionAsync()
    {
        if (SelectedItemsChanged.HasDelegate)
            await SelectedItemsChanged.InvokeAsync(_selected.ToList());
        StateHasChanged();
    }

    internal async Task HandleRowClickAsync(TItem item)
    {
        if (OnRowClick.HasDelegate) await OnRowClick.InvokeAsync(item);
        if (SelectionMode == BitDataGridSelectionMode.Single && _editItem is null)
            await ToggleRowSelectionAsync(item, true);
    }

    // ------------------------------------------------------ Detail rows
    internal bool IsDetailExpanded(TItem item) => _expandedDetails.Contains(GetKey(item));
    internal void ToggleDetail(TItem item)
    {
        var key = GetKey(item);
        if (!_expandedDetails.Add(key)) _expandedDetails.Remove(key);
        StateHasChanged();
    }

    // ---------------------------------------------------------- Editing
    internal bool ColumnEditable(BitDataGridColumn<TItem> column)
        => column.HasField && column.Accessor?.CanWrite == true && (column.Editable ?? Editable);

    internal void BeginEdit(TItem item)
    {
        _editItem = item;
        _isNewItem = false;
        SnapshotEdit(item);
        StateHasChanged();
    }

    internal async Task AddNewRowAsync()
    {
        if (NewItemFactory is null) return;
        var item = NewItemFactory();
        _pendingNew = item;
        _editItem = item;
        _isNewItem = true;
        _editSnapshot = null;
        if (OnRowCreate.HasDelegate) await OnRowCreate.InvokeAsync(item);
        StateHasChanged();
    }

    private void SnapshotEdit(TItem item)
    {
        _editSnapshot = new Dictionary<string, object?>();
        foreach (var col in _columns.Where(ColumnEditable))
            _editSnapshot[col.Id] = col.GetValue(item);
    }

    internal async Task CommitEditAsync()
    {
        if (_editItem is null) return;
        var item = _editItem;
        _editItem = default;
        _pendingNew = default;
        _editSnapshot = null;
        _isNewItem = false;
        if (OnRowSave.HasDelegate) await OnRowSave.InvokeAsync(item);
        await RefreshAsync();
    }

    internal async Task CancelEditAsync()
    {
        if (_editItem is null) return;
        var item = _editItem;
        if (!_isNewItem && _editSnapshot is not null)
        {
            foreach (var (colId, value) in _editSnapshot)
                if (_columnsById.TryGetValue(colId, out var col))
                    col.Accessor?.SetValue(item, value);
        }
        _editItem = default;
        _pendingNew = default;
        _editSnapshot = null;
        _isNewItem = false;
        if (OnRowCancel.HasDelegate) await OnRowCancel.InvokeAsync(item);
        StateHasChanged();
    }

    internal async Task DeleteRowAsync(TItem item)
    {
        if (OnRowDelete.HasDelegate) await OnRowDelete.InvokeAsync(item);
        _selected.Remove(item);
        await RefreshAsync();
    }

    internal void SetEditValue(BitDataGridColumn<TItem> column, object? value)
    {
        if (_editItem is null) return;
        column.Accessor?.SetValue(_editItem, value);
    }

    // ---------------------------------------------------------- Resizing
    internal void StartResize(BitDataGridColumn<TItem> column, double clientX)
    {
        _resizingColumn = column;
        _resizeStartX = clientX;
        _resizeStartWidth = column.ResizedWidth ?? ParseInitialWidth(column);
        StateHasChanged();
    }

    internal void OnResizeMove(double clientX)
    {
        if (_resizingColumn is null) return;
        var delta = clientX - _resizeStartX;
        if (Direction == BitDir.Rtl) delta = -delta;
        var newWidth = Math.Max(_resizingColumn.MinWidth, _resizeStartWidth + delta);
        if (_resizingColumn.MaxWidth is { } max) newWidth = Math.Min(max, newWidth);
        _resizingColumn.ResizedWidth = newWidth;
        StateHasChanged();
    }

    internal void EndResize()
    {
        _resizingColumn = null;
        StateHasChanged();
    }

    internal bool IsResizing => _resizingColumn is not null;

    private static double ParseInitialWidth(BitDataGridColumn<TItem> column)
    {
        if (!string.IsNullOrEmpty(column.Width) && column.Width.EndsWith("px")
            && double.TryParse(column.Width[..^2], NumberStyles.Any, CultureInfo.InvariantCulture, out var px))
            return px;
        return 150;
    }

    // -------------------------------------------------------- Reordering
    internal void StartColumnDrag(BitDataGridColumn<TItem> column) => _dragColumn = column;

    internal void DropColumn(BitDataGridColumn<TItem> target)
    {
        if (_dragColumn is null || _dragColumn == target) { _dragColumn = null; return; }
        var from = _columns.IndexOf(_dragColumn);
        var to = _columns.IndexOf(target);
        if (from < 0 || to < 0) { _dragColumn = null; return; }
        _columns.RemoveAt(from);
        _columns.Insert(to, _dragColumn);
        _dragColumn = null;
        StateHasChanged();
    }

    internal bool ColumnResizable(BitDataGridColumn<TItem> column) => column.Resizable ?? Resizable;
    internal bool ColumnReorderable(BitDataGridColumn<TItem> column) => column.Reorderable ?? Reorderable;

    // ----------------------------------------------------- Row reordering
    internal void StartRowDrag(TItem row) => _dragRow = row;

    internal async Task DropRowAsync(TItem target)
    {
        if (_dragRow is null || EqualityComparer<TItem>.Default.Equals(_dragRow, target)) { _dragRow = default; return; }

        var dragged = _dragRow;
        _dragRow = default;

        // Determine indices within the bound source.
        if (Items is IList<TItem> list)
        {
            var from = list.IndexOf(dragged);
            var to = list.IndexOf(target);
            if (from < 0 || to < 0) return;

            if (!list.IsReadOnly)
            {
                list.RemoveAt(from);
                list.Insert(to, dragged);
            }

            if (OnRowReorder.HasDelegate)
                await OnRowReorder.InvokeAsync(new BitDataGridRowReorderEventArgs<TItem>
                {
                    DraggedItem = dragged,
                    TargetItem = target,
                    FromIndex = from,
                    ToIndex = to
                });

            await RefreshAsync();
        }
        else if (OnRowReorder.HasDelegate)
        {
            await OnRowReorder.InvokeAsync(new BitDataGridRowReorderEventArgs<TItem>
            {
                DraggedItem = dragged,
                TargetItem = target,
                FromIndex = -1,
                ToIndex = -1
            });
        }
    }

    // -------------------------------------------------------- Cell events
    internal async Task HandleCellClickAsync(BitDataGridColumn<TItem> column, TItem item, MouseEventArgs e)
    {
        if (OnCellClick.HasDelegate)
            await OnCellClick.InvokeAsync(MakeCellArgs(column, item, e));
    }

    internal async Task HandleCellDoubleClickAsync(BitDataGridColumn<TItem> column, TItem item, MouseEventArgs e)
    {
        if (OnCellDoubleClick.HasDelegate)
            await OnCellDoubleClick.InvokeAsync(MakeCellArgs(column, item, e));
    }

    internal async Task HandleCellContextMenuAsync(BitDataGridColumn<TItem> column, TItem item, MouseEventArgs e)
    {
        if (OnCellContextMenu.HasDelegate)
            await OnCellContextMenu.InvokeAsync(MakeCellArgs(column, item, e));
    }

    internal bool HasCellEvents => OnCellClick.HasDelegate || OnCellDoubleClick.HasDelegate || OnCellContextMenu.HasDelegate;

    private BitDataGridCellEventArgs<TItem> MakeCellArgs(BitDataGridColumn<TItem> column, TItem item, MouseEventArgs e)
        => new() { Item = item, Column = column, Value = column.GetValue(item), Mouse = e };

    // ------------------------------------------------- Keyboard cell navigation
    /// <summary>The flat, ordered list of rows the keyboard navigation moves across.</summary>
    internal IReadOnlyList<TItem> NavigableRows => _pageItems;

    internal bool IsCellFocused(TItem item, int colIndex)
        => _focusedRow is not null && KeyEquals(_focusedRow, item) && _focusedCol == colIndex;

    /// <summary>Roving tabindex: only one cell is in the tab order at a time.</summary>
    internal int CellTabIndex(TItem item, int colIndex)
    {
        if (_focusedRow is not null)
            return IsCellFocused(item, colIndex) ? 0 : -1;
        var rows = NavigableRows;
        return rows.Count > 0 && KeyEquals(rows[0], item) && colIndex == 0 ? 0 : -1;
    }

    /// <summary>Records the focused cell when the user clicks/tabs into it (no re-focus needed).</summary>
    internal void SetFocusedCell(TItem item, int colIndex)
    {
        if (IsCellFocused(item, colIndex)) return;
        _focusedRow = item;
        _focusedCol = colIndex;
        StateHasChanged();
    }

    internal bool ShouldFocusCell(TItem item, int colIndex) => _focusPending && IsCellFocused(item, colIndex);
    internal void ClearFocusPending() => _focusPending = false;

    /// <summary>Requests that the currently focused cell regain DOM focus on the next render
    /// (e.g. after leaving inline edit mode via the keyboard).</summary>
    internal void RefocusFocusedCell()
    {
        if (_focusedRow is null) return;
        _focusPending = true;
        StateHasChanged();
    }

    internal async Task HandleCellKeyDownAsync(TItem item, int colIndex, KeyboardEventArgs e)
    {
        var rows = NavigableRows;
        if (rows.Count == 0) return;
        var colCount = VisibleColumns.Count;
        if (colCount == 0) return;

        var rowIdx = IndexOfRow(rows, item);
        if (rowIdx < 0) rowIdx = 0;

        int row = rowIdx, col = colIndex;
        var rtl = Direction == BitDir.Rtl;
        var handled = true;
        // Horizontal travel direction in column-index space (used to skip over spanned-away columns).
        int colDir = 0;

        switch (e.Key)
        {
            case "ArrowRight": col += rtl ? -1 : 1; colDir = rtl ? -1 : 1; break;
            case "ArrowLeft": col += rtl ? 1 : -1; colDir = rtl ? 1 : -1; break;
            case "ArrowDown": row += 1; break;
            case "ArrowUp": row -= 1; break;
            case "Home": if (e.CtrlKey) { row = 0; col = 0; } else col = 0; break;
            case "End": if (e.CtrlKey) { row = rows.Count - 1; col = colCount - 1; } else col = colCount - 1; colDir = -1; break;
            case "PageDown": row += 10; break;
            case "PageUp": row -= 10; break;
            case "Enter":
            case "F2":
                var ec = VisibleColumns[Math.Clamp(col, 0, colCount - 1)];
                if (ColumnEditable(ec)) BeginEdit(item);
                return;
            case "Escape":
                if (_editItem is not null) await CancelEditAsync();
                return;
            default: handled = false; break;
        }
        if (!handled) return;

        row = Math.Clamp(row, 0, rows.Count - 1);
        col = Math.Clamp(col, 0, colCount - 1);
        // The target row may span columns; snap focus to the actually-rendered cell so it always
        // lands on a real BitDataGridCell with tabindex=0 instead of a spanned-away column index.
        col = SnapToRenderedColumn(rows[row], col, colDir);
        _focusedRow = rows[row];
        _focusedCol = col;
        _focusPending = true;
        StateHasChanged();
    }

    private int IndexOfRow(IReadOnlyList<TItem> rows, TItem item)
    {
        for (int i = 0; i < rows.Count; i++)
            if (KeyEquals(rows[i], item)) return i;
        return -1;
    }

    // ----------------------------------------------------- Column spanning
    /// <summary>Resolves the effective column span for a data cell (clamped to remaining columns).</summary>
    internal int ResolveColSpan(BitDataGridColumn<TItem> column, TItem item)
    {
        if (column.ColSpan is null) return 1;
        var span = column.ColSpan(item) ?? 1;
        if (span < 1) span = 1;
        var cols = VisibleColumns;
        var idx = -1;
        for (int i = 0; i < cols.Count; i++)
        {
            if (cols[i] == column) { idx = i; break; }
        }
        if (idx < 0) return 1;
        return Math.Min(span, cols.Count - idx);
    }

    /// <summary>
    /// Maps a desired visible-column index to the index of the cell actually rendered in the given
    /// row, accounting for column spanning (columns covered by a preceding span are not rendered).
    /// When travelling horizontally (<paramref name="dir"/> ≠ 0) the focus advances past the span in
    /// the travel direction so keyboard navigation never stalls inside a spanned-over column.
    /// </summary>
    private int SnapToRenderedColumn(TItem item, int target, int dir)
    {
        var cols = VisibleColumns;
        var count = cols.Count;
        if (count == 0) return 0;
        target = Math.Clamp(target, 0, count - 1);

        // For every column position compute the index of the rendered (span-start) cell that covers it
        // and the last column the span covers.
        var starts = new int[count];
        var ends = new int[count];
        int i = 0;
        while (i < count)
        {
            var span = Math.Max(1, ResolveColSpan(cols[i], item));
            var end = Math.Min(count - 1, i + span - 1);
            for (int j = i; j <= end; j++) { starts[j] = i; ends[j] = end; }
            i = end + 1;
        }

        var start = starts[target];
        // Moving right but the target fell inside a span that starts earlier: jump to the next span start.
        if (dir > 0 && start < target)
        {
            var next = ends[target] + 1;
            return next <= count - 1 ? starts[next] : start;
        }
        // Moving left (or stationary): the span start is the rendered cell to focus.
        return start;
    }

    // ------------------------------------------------- Column header groups
    internal bool HasColumnGroups => VisibleColumns.Any(c => !string.IsNullOrEmpty(c.Group));

    /// <summary>Builds the contiguous spans of the grouped header row (group name + column count).</summary>
    internal IReadOnlyList<(string? Name, int Span)> ColumnGroupSpans()
    {
        var spans = new List<(string?, int)>();
        string? current = null;
        int count = 0;
        bool started = false;
        foreach (var col in VisibleColumns)
        {
            var name = string.IsNullOrEmpty(col.Group) ? null : col.Group;
            if (started && name == current)
            {
                count++;
            }
            else
            {
                if (started) spans.Add((current, count));
                current = name;
                count = 1;
                started = true;
            }
        }
        if (started) spans.Add((current, count));
        return spans;
    }

    // ------------------------------------------------------------- Paging
    internal async Task GoToPageAsync(int page)
    {
        _currentPage = Math.Clamp(page, 1, TotalPages);
        await RefreshAsync();
    }

    internal async Task SetPageSizeAsync(int size)
    {
        PageSize = size;
        _effectivePageSize = Math.Max(1, size);
        _currentPage = 1;
        await RefreshAsync();
    }

    // ------------------------------------------------------- Column chooser
    internal void ToggleColumnChooser() { _showColumnChooserPanel = !_showColumnChooserPanel; StateHasChanged(); }
    internal async Task SetColumnVisibilityAsync(BitDataGridColumn<TItem> column, bool visible)
    {
        column.Visible = visible;
        await RefreshAsync();
    }

    // ----------------------------------------------------------- Identity
    private object GetKey(TItem item) => KeyField?.Invoke(item) ?? item!;
    private bool KeyEquals(TItem a, TItem b)
        => KeyField is not null ? Equals(KeyField(a), KeyField(b)) : EqualityComparer<TItem>.Default.Equals(a, b);

    /// <summary>Compares rows by their key (via <see cref="GetKey"/>) so selection tracks key identity
    /// rather than object reference, surviving refreshes that yield new instances with the same key.</summary>
    private sealed class KeySelectionComparer : IEqualityComparer<TItem>
    {
        private readonly Func<TItem, object> _keyOf;
        public KeySelectionComparer(Func<TItem, object> keyOf) => _keyOf = keyOf;
        public bool Equals(TItem? x, TItem? y)
            => (x is null || y is null) ? ReferenceEquals(x, y) : object.Equals(_keyOf(x), _keyOf(y));
        public int GetHashCode(TItem obj) => _keyOf(obj)?.GetHashCode() ?? 0;
    }

    // ----------------------------------------------------------- CSV export
    /// <summary>Builds a CSV string of the current (filtered/sorted) data.</summary>
    public string ToCsv()
    {
        var cols = VisibleColumns.Where(c => c.HasField).ToList();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(string.Join(",", cols.Select(c => Escape(c.DisplayTitle))));
        var rows = IsServerMode ? _pageItems : _view;
        foreach (var item in rows)
            sb.AppendLine(string.Join(",", cols.Select(c => Escape(c.GetFormattedValue(item)))));
        return sb.ToString();

        static string Escape(string v)
            => v.Contains(',') || v.Contains('"') || v.Contains('\n')
                ? "\"" + v.Replace("\"", "\"\"") + "\""
                : v;
    }

    // ----------------------------------------------------- Layout helpers
    internal bool HasSelectColumn => SelectionMode == BitDataGridSelectionMode.Multiple;
    internal bool HasDetailColumn => DetailTemplate is not null;
    internal bool HasCommandColumn => Editable;
    internal bool HasReorderColumn => RowReorderable;

    private const double ReorderColWidth = 36;
    private const double DetailColWidth = 44;
    private const double SelectColWidth = 44;

    private double DetailOffset => HasReorderColumn ? ReorderColWidth : 0;
    private double SelectOffset => DetailOffset + (HasDetailColumn ? DetailColWidth : 0);

    /// <summary>The inline-start CSS edge for sticky special columns, flipped to "right" in RTL.</summary>
    private string StickyEdge => Direction == BitDir.Rtl ? "right" : "left";

    internal string ReorderStickyStyle => $"{StickyEdge}:0;";
    internal string DetailStickyStyle => $"{StickyEdge}:{DetailOffset.ToString(CultureInfo.InvariantCulture)}px;";
    internal string SelectStickyStyle => $"{StickyEdge}:{SelectOffset.ToString(CultureInfo.InvariantCulture)}px;";

    private string ColumnWidthToken(BitDataGridColumn<TItem> column)
    {
        if (column.ResizedWidth is { } w) return $"{w.ToString(CultureInfo.InvariantCulture)}px";
        if (!string.IsNullOrEmpty(column.Width))
            return column.MaxWidth is { } mx
                ? $"minmax({column.MinWidth}px, min({column.Width}, {mx}px))"
                : column.Width!;
        return column.MaxWidth is { } max
            ? $"minmax({column.MinWidth}px, min(1fr, {max}px))"
            : $"minmax({Math.Max(120, column.MinWidth)}px, 1fr)";
    }

    /// <summary>Resolves the height (in px) for a given row, honouring <see cref="RowHeightSelector"/>.</summary>
    internal float ResolveRowHeight(TItem item) => RowHeightSelector?.Invoke(item) ?? RowHeight;

    /// <summary>Builds the CSS grid template-columns value for the whole row layout.</summary>
    private string BuildGridTemplate()
    {
        var parts = new List<string>();
        if (HasReorderColumn) parts.Add($"{ReorderColWidth.ToString(CultureInfo.InvariantCulture)}px");
        if (HasDetailColumn) parts.Add($"{DetailColWidth.ToString(CultureInfo.InvariantCulture)}px");
        if (HasSelectColumn) parts.Add($"{SelectColWidth.ToString(CultureInfo.InvariantCulture)}px");
        foreach (var c in VisibleColumns) parts.Add(ColumnWidthToken(c));
        if (HasCommandColumn) parts.Add("minmax(150px, max-content)");
        return string.Join(" ", parts);
    }

    private int TotalColumnSpan =>
        VisibleColumns.Count + (HasReorderColumn ? 1 : 0) + (HasDetailColumn ? 1 : 0) + (HasSelectColumn ? 1 : 0) + (HasCommandColumn ? 1 : 0);

    private string HeaderCellClass(BitDataGridColumn<TItem> column)
    {
        var c = "bit-dtg-hcell " + AlignClass(column.Align);
        if (column.Frozen) c += " bit-dtg-sticky";
        if (ColumnSortable(column)) c += " bit-dtg-sortable";
        if (!string.IsNullOrEmpty(column.HeaderClass)) c += " " + column.HeaderClass;
        return c;
    }

    private string RootClasses()
    {
        var c = "bit-dtg";
        if (Bordered) c += " bit-dtg-bordered";
        if (Striped) c += " bit-dtg-striped";
        if (Hoverable) c += " bit-dtg-hoverable";
        if (Direction == BitDir.Rtl) c += " bit-dtg-rtl";
        if (!string.IsNullOrEmpty(Class)) c += " " + Class;
        return c;
    }

    internal static string AlignClass(BitDataGridColumnAlign a) => a switch
    {
        BitDataGridColumnAlign.Center => "bit-dtg-center",
        BitDataGridColumnAlign.Right => "bit-dtg-right",
        _ => ""
    };

    private double SpecialStickyWidth => (HasReorderColumn ? ReorderColWidth : 0) + (HasDetailColumn ? DetailColWidth : 0) + (HasSelectColumn ? SelectColWidth : 0);

    private double ColumnPixelWidth(BitDataGridColumn<TItem> column)
    {
        if (column.ResizedWidth is { } w) return w;
        return ParseInitialWidth(column);
    }

    /// <summary>Sticky left offset (in px) for a frozen data column.</summary>
    internal double FrozenOffset(BitDataGridColumn<TItem> column)
    {
        double offset = SpecialStickyWidth;
        foreach (var c in VisibleColumns)
        {
            if (c == column) break;
            if (c.Frozen) offset += ColumnPixelWidth(c);
        }
        return offset;
    }

    internal string FrozenStyle(BitDataGridColumn<TItem> column)
    {
        if (!column.Frozen) return string.Empty;
        var edge = Direction == BitDir.Rtl ? "right" : "left";
        return $"{edge}:{FrozenOffset(column).ToString(CultureInfo.InvariantCulture)}px;";
    }

    private string AggregateLabel(BitDataGridAggregateResult agg) => agg.Type switch
    {
        BitDataGridAggregateType.Sum => $"Σ {agg.FormattedValue}",
        BitDataGridAggregateType.Average => $"avg {agg.FormattedValue}",
        BitDataGridAggregateType.Count => $"count {agg.FormattedValue}",
        BitDataGridAggregateType.Min => $"min {agg.FormattedValue}",
        BitDataGridAggregateType.Max => $"max {agg.FormattedValue}",
        _ => agg.FormattedValue
    };
}

