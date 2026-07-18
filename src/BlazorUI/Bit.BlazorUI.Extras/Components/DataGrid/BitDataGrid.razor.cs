using System.Globalization;
using System.Text.Json;
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
[CascadingTypeParameter(nameof(TItem))]
public partial class BitDataGrid<TItem> : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // ---------------------------------------------------------------- Data
    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Server-side data callback. When set, the grid delegates sort/filter/page to the caller.</summary>
    [Parameter] public Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TItem>>>? OnRead { get; set; }

    /// <summary>
    /// Infinite-scrolling data callback. When set, the grid loads rows in batches and appends the
    /// next batch automatically as the user scrolls to the end of the viewport - with no paging UI
    /// and no knowledge of the total row count. Each call receives a <see cref="BitDataGridReadRequest"/>
    /// whose <c>Skip</c> is the number of rows already loaded and whose <c>Take</c> is
    /// <see cref="LoadMoreBatchSize"/>. The grid stops requesting more once a batch returns fewer rows
    /// than requested (signalling the end of the data). Exports are the one exception to the batch
    /// shape: they issue a request with <c>Skip = 0</c> and <c>Take = null</c> ("all rows") so the
    /// exported file covers the full matching set, not just the batches already scrolled into view.
    /// Mirrors react-data-grid's Infinite Scrolling.
    /// Requires a fixed <see cref="Height"/>. The returned <c>TotalCount</c> is ignored in this mode.
    /// </summary>
    [Parameter] public Func<BitDataGridReadRequest, Task<BitDataGridReadResult<TItem>>>? OnLoadMore { get; set; }

    /// <summary>Number of rows fetched per batch in infinite-scrolling mode. Default: 50.</summary>
    [Parameter] public int LoadMoreBatchSize { get; set; } = 50;

    /// <summary>Column definitions and other declarative children.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Alias of <see cref="ChildContent"/>, letting column definitions read declaratively as
    /// <c>&lt;Columns&gt;...&lt;/Columns&gt;</c>. Both fragments are rendered when both are set.</summary>
    [Parameter] public RenderFragment? Columns { get; set; }

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

    /// <summary>When tree mode is active, controls whether nodes start expanded. Default: collapsed.
    /// Ignored in lazy mode (<see cref="ChildrenProvider"/>), where children aren't known up front.</summary>
    [Parameter] public bool TreeInitiallyExpanded { get; set; }

    /// <summary>
    /// Async children provider for a <b>lazily-loaded</b> tree grid: children are fetched (e.g. from a
    /// backend) the first time a node is expanded and cached for later toggles. Pair it with
    /// <see cref="HasChildrenSelector"/> so unloaded nodes know whether to render an expand toggle.
    /// Mutually exclusive with <see cref="ChildrenSelector"/>.
    /// </summary>
    [Parameter] public Func<TItem, Task<IEnumerable<TItem>?>>? ChildrenProvider { get; set; }

    /// <summary>
    /// Tells whether a node can have children before they are loaded, so the expand toggle renders on
    /// unloaded lazy nodes. Only used with <see cref="ChildrenProvider"/>; when omitted, a node shows
    /// a toggle only after its children have been loaded (i.e. never for the initial render), so
    /// providing it is strongly recommended.
    /// </summary>
    [Parameter] public Func<TItem, bool>? HasChildrenSelector { get; set; }

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

    /// <summary>
    /// Shows a compact operator dropdown next to each text/number/date filter editor so users can pick
    /// the comparison (contains/starts with/=/≠/&gt;/≥/&lt;/≤ …) instead of the fixed default
    /// (Contains for text, Equals otherwise). Overridable per column.
    /// </summary>
    [Parameter] public bool FilterOperators { get; set; }
    [Parameter] public bool Resizable { get; set; }
    [Parameter] public bool Reorderable { get; set; }
    [Parameter] public bool Groupable { get; set; }
    [Parameter] public bool ShowToolbar { get; set; }
    [Parameter] public bool ShowColumnChooser { get; set; }
    [Parameter] public bool ShowCsvExport { get; set; }

    /// <summary>Renders an Excel (.xlsx) export button in the toolbar. The export is generated
    /// in-process with no external dependency; in server mode it covers all matching rows.</summary>
    [Parameter] public bool ShowExcelExport { get; set; }

    /// <summary>When true, Excel exports (the toolbar button as well as
    /// <see cref="ToExcelAsync"/>/<see cref="ExportExcelAsync"/>) also carry the grid's current
    /// visual theme: the rendered header/row colors, striped alternating rows, border color and
    /// bold/italic fonts are sampled from the live DOM (so the active theme - including dark mode -
    /// is what lands in the workbook) and baked into the file's style sheet. Falls back to the
    /// plain bold-header styling when JS is unavailable (prerendering, disconnected circuit).</summary>
    [Parameter] public bool ExcelExportStyled { get; set; }

    /// <summary>
    /// Enables keyboard cell navigation. Cells become focusable via a roving tabindex and
    /// respond to arrow keys, <kbd>Home</kbd>/<kbd>End</kbd>, <kbd>PageUp</kbd>/<kbd>PageDown</kbd>
    /// (and <kbd>Ctrl</kbd> variants). <kbd>Enter</kbd>/<kbd>F2</kbd> begins editing an editable
    /// cell and <kbd>Esc</kbd> cancels. Mirrors react-data-grid's Cell Navigation. No JavaScript
    /// is used - focus is driven by Blazor's built-in <c>FocusAsync</c>.
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
    /// <summary>
    /// Renders only the rows in (and near) the viewport. Requires a fixed <see cref="Height"/> and a
    /// uniform <see cref="RowHeight"/>. In client mode the current view is virtualized in memory; in
    /// server mode (<see cref="OnRead"/>) with paging disabled, row windows are fetched on demand
    /// through <see cref="OnRead"/> as the user scrolls, so arbitrarily large remote datasets can be
    /// browsed without a pager.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }
    [Parameter] public float RowHeight { get; set; } = 36f;

    /// <summary>
    /// Renders only the columns in (and near) the horizontal viewport, replacing scrolled-out runs
    /// with spacer cells - for grids with very many columns. Column x-positions are computed from
    /// pixel widths, so give columns explicit px <c>Width</c>s (non-px widths fall back to their
    /// resize default). Not applied while column header groups or per-row <c>ColSpan</c>s are used
    /// (both can span across the skipped runs); keyboard cell navigation can only reach rendered
    /// columns while this is active.
    /// </summary>
    [Parameter] public bool VirtualizeColumns { get; set; }

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

    // ---------------------------------------------------------- Localization
    /// <summary>All user-visible strings rendered by the grid. Assign a customized
    /// <see cref="BitDataGridStrings"/> to localize the UI; defaults to English.</summary>
    [Parameter] public BitDataGridStrings Strings { get; set; } = new();

    // ---------------------------------------------------------------- State
    private readonly List<BitDataGridColumn<TItem>> _columns = new();
    private readonly Dictionary<string, BitDataGridColumn<TItem>> _columnsById = new();
    private readonly List<BitDataGridSortDescriptor> _sorts = new();
    private readonly List<BitDataGridFilterDescriptor> _filters = new();
    private readonly List<BitDataGridGroupDescriptor> _groups = new();
    // Per-column operator chosen in the filter-operator dropdown, and the raw editor text it applies
    // to (kept so an operator change can re-apply the current filter text under the new operator).
    private readonly Dictionary<string, BitDataGridFilterOperator> _filterOps = new();
    private readonly Dictionary<string, string?> _filterRaw = new();
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
    // lazy tree mode (ChildrenProvider): children fetched on first expand, cached by node key
    private readonly Dictionary<object, IReadOnlyList<TItem>> _loadedChildren = new();
    private readonly HashSet<object> _loadingNodes = new();

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
    // The page size the user picked in the pager. Kept separate from the PageSize parameter (which a
    // component must never write to: the parent's next render would silently revert it). A subsequent
    // PageSize parameter change from the parent supersedes the user's choice.
    private int? _pageSizeOverride;
    private bool _showColumnChooserPanel;
    // Stable per-instance id tying the column-chooser toggle button (aria-controls/aria-expanded) to
    // the chooser panel it shows, so assistive tech can announce whether the chooser is open.
    private readonly string _columnChooserPanelId = $"bit-dtg-cc-{Guid.NewGuid():n}";

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
    // Built-in editors write into this buffer instead of the live object, so the row's data is only
    // mutated when the edit is committed - Cancel simply discards the buffer. (Custom EditTemplates
    // bind straight to the item and are covered by the snapshot/restore path instead.)
    private Dictionary<string, object?>? _editBuffer;
    // Per-column validation errors for the row being edited (conversion failures and Validate results).
    // While non-empty, committing is blocked and the messages render under their editors.
    private Dictionary<string, string>? _editErrors;

    // resizing
    private BitDataGridColumn<TItem>? _resizingColumn;
    private double _resizeStartX;
    private double _resizeStartWidth;

    // reordering
    private BitDataGridColumn<TItem>? _dragColumn;
    private TItem? _dragRow;
    // touch/pen reorder (pointer events; mouse keeps native HTML5 DnD)
    private ElementReference _rootRef;
    // Shared .NET reference for every JS feature that calls back into this grid instance
    // (pointer reorder, horizontal scroll); disposed once in DisposeAsync.
    private DotNetObjectReference<BitDataGrid<TItem>>? _gridSelfRef;
    private IJSObjectReference? _pointerReorderHandle;
    private bool _pointerReorderAttached;

    // select-all indeterminate state ("some but not all selected") is a DOM property, not an HTML
    // attribute, so Blazor markup can't express it; it is synced via a small JS call after render.
    private ElementReference _selectAllCheckbox;
    private bool? _lastIndeterminate;

    // Maps row key -> absolute 0-based position in the full data view, rebuilt whenever the view
    // changes. Backs aria-rowindex, which must reflect a row's position in the whole dataset when
    // paging/virtualization means the DOM holds only a subset of the rows.
    private Dictionary<object, int>? _rowIndexByKey;

    // Text pushed to the visually-hidden aria-live region so screen readers hear sort/filter/page
    // changes, which are otherwise silent view mutations.
    private string? _srAnnouncement;

    // infinite scrolling
    private readonly List<TItem> _infiniteItems = new();
    private bool _infiniteHasMore = true;
    private bool _infiniteLoading;
    private ElementReference _infiniteViewport;
    private DotNetObjectReference<BitDataGrid<TItem>>? _infiniteSelfRef;
    private IJSObjectReference? _infiniteHandle;
    private bool _infiniteObserverAttached;

    // server-side virtualization (Virtualize + OnRead without paging)
    private Virtualize<TItem>? _serverVirtualize;
    private bool _serverVirtualizeEmpty;

    // True while the footer shows aggregates provided by the OnRead result (computed server-side over
    // the whole dataset); local per-page recomputation must not overwrite them.
    private bool _serverAggregates;

    // ------------------------------------------------- Column virtualization
    private const double ColumnOverscanPx = 200;
    // Hysteresis for the JS scroll reporter; kept below the overscan so a re-render always lands
    // before the user can scroll past the pre-rendered columns.
    private const double HScrollReportThresholdPx = 100;
    private IReadOnlyList<BitDataGridColumnSlot<TItem>>? _columnSlotsCache;
    private double _hScrollLeft;
    private double _hViewportWidth;
    private IJSObjectReference? _hScrollHandle;
    private bool _hScrollAttached;

    /// <summary>
    /// Column virtualization only engages once the viewport has been measured, and never together
    /// with column header groups or per-row column spans (both can span across a skipped run).
    /// Until then the slots are simply all visible columns, i.e. plain unvirtualized rendering.
    /// </summary>
    internal bool ColumnVirtualizationActive
        => VirtualizeColumns
        && _hViewportWidth > 0
        && !HasColumnGroups
        && !_columns.Any(c => c.ColSpan is not null);

    /// <summary>The horizontal render slots every row-like element iterates: all visible columns, or -
    /// while column virtualization is active - the on-screen window with spacers for skipped runs.</summary>
    internal IReadOnlyList<BitDataGridColumnSlot<TItem>> ColumnSlots => _columnSlotsCache ??= BuildColumnSlots();

    private IReadOnlyList<BitDataGridColumnSlot<TItem>> BuildColumnSlots()
    {
        var cols = VisibleColumns;
        var slots = new List<BitDataGridColumnSlot<TItem>>(cols.Count);

        if (!ColumnVirtualizationActive)
        {
            for (int i = 0; i < cols.Count; i++) slots.Add(new(cols[i], i, 0));
            return slots;
        }

        var windowStart = _hScrollLeft - ColumnOverscanPx;
        var windowEnd = _hScrollLeft + _hViewportWidth + ColumnOverscanPx;

        // Column x-positions start after the special (reorder/detail/select) columns; contiguous
        // scrolled-out runs collapse into a single spacer so total row width (and the scrollbar)
        // stays exactly the same.
        var x = SpecialStickyWidth;
        double pendingSpacer = 0;
        for (int i = 0; i < cols.Count; i++)
        {
            var column = cols[i];
            var width = ColumnPixelWidth(column);
            var rendered = IsSticky(column) || (x + width >= windowStart && x <= windowEnd);
            if (rendered)
            {
                if (pendingSpacer > 0)
                {
                    slots.Add(new(null, -1, pendingSpacer));
                    pendingSpacer = 0;
                }
                slots.Add(new(column, i, 0));
            }
            else
            {
                pendingSpacer += width;
            }
            x += width;
        }
        if (pendingSpacer > 0) slots.Add(new(null, -1, pendingSpacer));
        return slots;
    }

    /// <summary>Invoked from JavaScript when the viewport scrolls horizontally (or resizes), so the
    /// rendered column window can follow. RTL browsers report negative offsets - normalized here.</summary>
    [JSInvokable]
    public Task OnHorizontalScrollAsync(double scrollLeft, double clientWidth)
    {
        _hScrollLeft = Math.Abs(scrollLeft);
        _hViewportWidth = clientWidth;
        if (VirtualizeColumns)
        {
            _columnSlotsCache = null;
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    // Cancels superseded in-flight OnRead/OnLoadMore requests.
    private CancellationTokenSource? _loadCts;
    // Monotonic load version; bumped on every (re)load so a superseded response can detect it is stale.
    private int _loadVersion;

    internal IReadOnlyList<BitDataGridColumn<TItem>> AllColumns => _columns;
    // Cached: VisibleColumns is read many times per render (per row, per cell, in layout helpers), so
    // rebuilding the filtered list on every access would allocate O(rows × cells) lists per render.
    // Every mutation of _columns or a column's visibility must call InvalidateVisibleColumns().
    internal IReadOnlyList<BitDataGridColumn<TItem>> VisibleColumns => _visibleColumnsCache ??= _columns.Where(c => c.Visible).ToList();
    private List<BitDataGridColumn<TItem>>? _visibleColumnsCache;
    private void InvalidateVisibleColumns()
    {
        _visibleColumnsCache = null;
        _columnSlotsCache = null;
    }
    internal IReadOnlyList<BitDataGridSortDescriptor> Sorts => _sorts;
    internal bool IsServerMode => OnRead is not null;
    internal bool IsInfiniteMode => OnLoadMore is not null;
    /// <summary>Items is an <see cref="IQueryable{T}"/>: filters/sorts/paging are translated into
    /// expression trees and executed by the provider (e.g. EF Core → SQL) instead of in memory.</summary>
    internal bool IsQueryableMode => Items is IQueryable<TItem> && !IsTreeMode && !IsServerMode && !IsInfiniteMode;
    internal bool IsTreeMode => ChildrenSelector is not null || ChildrenProvider is not null;
    internal bool IsEditing(TItem item) => _editItem is not null && KeyEquals(_editItem, item);
    internal bool IsRowSelected(TItem item) => _selected.Contains(item);
    /// <summary>Total number of rows in the current (filtered) view; the provider-reported total in
    /// server and queryable modes.</summary>
    public int TotalCount => IsServerMode ? _totalCount : IsQueryableMode ? _queryableTotal : _view.Count;
    private int _queryableTotal;
    // Paging is suppressed while grouping is active: the grouped view renders every row, so a pager
    // would misrepresent the data and leave page math out of sync with what is displayed. Treat paging
    // as off in that case so the pager UI, TotalPages and GoToPageAsync all agree with the rendered rows.
    // Tree mode also flattens every visible node without paging (ProcessTreeData ignores paging), so the
    // pager is suppressed there too to stay consistent with the rendered rows. Infinite-scrolling mode
    // streams batches with no paging UI and no known total, so paging is suppressed there as well.
    internal bool PagingActive => Pageable && _groups.Count == 0 && !IsTreeMode && !IsInfiniteMode;
    /// <summary>Total number of pages while paging is active; 1 otherwise.</summary>
    public int TotalPages => (!PagingActive || _effectivePageSize <= 0) ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)_effectivePageSize));

    /// <summary>The 1-based current page.</summary>
    public int CurrentPage => _currentPage;
    internal IReadOnlyList<BitDataGridAggregateResult> FooterAggregates => _footerAggregates;
    internal TItem? PendingNewItem => _pendingNew;

    // ------------------------------------------------- Column registration
    internal bool AddColumn(BitDataGridColumn<TItem> column)
    {
        if (_columns.Contains(column)) return true;

        // Reject a second column registering under an id that is already taken. Overwriting the
        // registry entry while both columns remain in _columns would desync the two collections, so
        // sort/filter/group/footer lookups (which resolve a column by id) could resolve to the wrong
        // instance. Skip the duplicate instead of silently shadowing the existing column.
        if (_columnsById.ContainsKey(column.Id)) return false;

        _columns.Add(column);
        _columnsById[column.Id] = column;
        InvalidateVisibleColumns();

        // A column registering itself must not trigger a fresh data fetch in server/infinite modes -
        // doing so once per column re-queries the backend (or resets the infinite list) repeatedly.
        // Instead recompute footer/aggregate values from the rows already loaded and just re-render so
        // late-registered footer columns still get their values. In client mode RefreshAsync only
        // reprocesses the in-memory view (and recomputes aggregates), so it is cheap and used as-is.
        if (IsServerMode || IsInfiniteMode)
        {
            // Server-provided aggregates cover the whole dataset; don't overwrite them with a
            // page-local recomputation just because a column registered.
            if (!_serverAggregates)
            {
                _footerAggregates = BitDataGridDataProcessor.Aggregate(_pageItems, _columns);
            }
            InvokeAsync(StateHasChanged);
        }
        else
        {
            InvokeAsync(RefreshAsync);
        }

        return true;
    }

    /// <summary>
    /// Recomputes the grid's view and aggregates after a registered column's semantic parameters
    /// (Field/Aggregate/Format/AggregateFormat) change without its <see cref="BitDataGridColumn{TItem}.Id"/>
    /// changing. Mirrors <see cref="AddColumn"/>'s mode-aware refresh so the active view never goes stale.
    /// </summary>
    internal void NotifyColumnChanged()
    {
        if (IsServerMode || IsInfiniteMode)
        {
            if (!_serverAggregates)
            {
                _footerAggregates = BitDataGridDataProcessor.Aggregate(_pageItems, _columns);
            }
            InvokeAsync(StateHasChanged);
        }
        else
        {
            InvokeAsync(RefreshAsync);
        }
    }

    internal void RemoveColumn(BitDataGridColumn<TItem> column)
    {
        // Remove by the key the column is actually registered under: a column whose ColumnId/Field
        // changed after registration is re-keyed via UpdateColumnRegistration, but guard against any
        // stale key by also matching on the column instance.
        var key = column.Id;
        if (!(_columnsById.TryGetValue(key, out var byId) && ReferenceEquals(byId, column)))
        {
            var match = _columnsById.FirstOrDefault(kvp => ReferenceEquals(kvp.Value, column));
            if (match.Key is not null) key = match.Key;
        }

        if (_columns.Remove(column))
        {
            _columnsById.Remove(key);
            InvalidateVisibleColumns();
            // Drop any sort/filter/group descriptors that referenced the removed column so later
            // refreshes and remote reads no longer carry descriptors for a column that is gone.
            var removedDescriptors = _sorts.RemoveAll(s => s.ColumnId == key)
                + _filters.RemoveAll(f => f.ColumnId == key)
                + _groups.RemoveAll(g => g.ColumnId == key);

            // Rebuild the view/aggregates the same mode-aware way AddColumn does so dropping a column
            // (and any of its sort/filter/group descriptors) immediately updates the rendered rows
            // instead of leaving a stale _view/_pageItems. In server/infinite modes a removed
            // descriptor changes the active query (a filter/sort no longer applies), so re-load the
            // remote data to reflect it; when no descriptor was dropped only the aggregates and render
            // need refreshing, matching AddColumn's no-requery behavior during column teardown.
            if (IsServerMode || IsInfiniteMode)
            {
                if (removedDescriptors > 0)
                {
                    InvokeAsync(RefreshAsync);
                }
                else
                {
                    if (!_serverAggregates)
                    {
                        _footerAggregates = BitDataGridDataProcessor.Aggregate(_pageItems, _columns);
                    }
                    InvokeAsync(StateHasChanged);
                }
            }
            else
            {
                InvokeAsync(RefreshAsync);
            }
        }
    }

    /// <summary>
    /// Re-keys a column in the registry after its <see cref="BitDataGridColumn{TItem}.Id"/> changes
    /// (its <c>ColumnId</c>/<c>Field</c> parameters were mutated after the initial registration).
    /// Without this the registry keeps the stale key and sort/filter/group lookups - which resolve
    /// columns by id - would no longer find the column. Active descriptors are migrated to the new id.
    /// </summary>
    internal void UpdateColumnRegistration(BitDataGridColumn<TItem> column, string oldId)
    {
        if (oldId == column.Id) return;

        // Only re-key a column that is actually registered. AddColumn skips a duplicate-id registration
        // without adding the column to _columns; if such a skipped column later changes its id, re-keying
        // here would insert an unregistered column into _columnsById and desync it from _columns.
        if (!_columns.Contains(column)) return;

        // Reject the rename if the new id already belongs to a different live column. Overwriting the
        // registry entry would shadow that column and desync _columnsById from _columns. Unlike AddColumn
        // (where a duplicate column is simply never registered), the column here is already registered and
        // its Id has already changed, so silently returning would leave it partially updated (registered
        // under its old key while reporting the new id). Surface the collision as an error instead.
        if (_columnsById.TryGetValue(column.Id, out var clash) && !ReferenceEquals(clash, column))
            throw new InvalidOperationException(
                $"Cannot change a {nameof(BitDataGridColumn<TItem>)}'s id to '{column.Id}' because another " +
                $"column is already registered under that id. Column ids (ColumnId/Field) must be unique.");

        if (_columnsById.TryGetValue(oldId, out var existing) && ReferenceEquals(existing, column))
            _columnsById.Remove(oldId);
        _columnsById[column.Id] = column;

        // Descriptors are immutable on ColumnId, so rebuild the affected entries with the new id to
        // preserve the column's active sort/filter/group state across the rename.
        for (int i = 0; i < _sorts.Count; i++)
        {
            if (_sorts[i].ColumnId == oldId)
                _sorts[i] = new BitDataGridSortDescriptor { ColumnId = column.Id, Direction = _sorts[i].Direction, Priority = _sorts[i].Priority };
        }
        for (int i = 0; i < _filters.Count; i++)
        {
            if (_filters[i].ColumnId == oldId)
                _filters[i] = new BitDataGridFilterDescriptor { ColumnId = column.Id, Operator = _filters[i].Operator, Value = _filters[i].Value };
        }
        for (int i = 0; i < _groups.Count; i++)
        {
            if (_groups[i].ColumnId == oldId)
                _groups[i] = new BitDataGridGroupDescriptor { ColumnId = column.Id, Direction = _groups[i].Direction };
        }

        InvokeAsync(RefreshAsync);
    }

    // ------------------------------------------------------- Lifecycle
    protected override async Task OnParametersSetAsync()
    {
        // The razor markup dereferences Strings unconditionally; tolerate an explicit null.
        Strings ??= new BitDataGridStrings();

        // Server mode (OnRead) and infinite-scrolling mode (OnLoadMore) drive paging, total count and
        // ARIA state in mutually exclusive ways. Allowing both would let RefreshAsync behave like
        // infinite loading while TotalCount/TotalPages still report server paging, so reject the
        // ambiguous configuration up-front and force callers to pick a single data mode.
        if (OnRead is not null && OnLoadMore is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(BitDataGrid<TItem>)} cannot use both {nameof(OnRead)} (server mode) and " +
                $"{nameof(OnLoadMore)} (infinite-scrolling mode) at the same time. Provide only one data callback.");
        }

        // Tree mode flattens the bound Items via ProcessTreeData and never calls the remote data
        // callbacks, so combining it with OnRead/OnLoadMore would let RefreshAsync bypass tree
        // processing and present a flat remote list under a tree UI. Reject the ambiguous config.
        if ((ChildrenSelector is not null || ChildrenProvider is not null) && (OnRead is not null || OnLoadMore is not null))
        {
            throw new InvalidOperationException(
                $"{nameof(BitDataGrid<TItem>)} cannot combine tree mode ({nameof(ChildrenSelector)}/{nameof(ChildrenProvider)}) with " +
                $"{nameof(OnRead)} (server mode) or {nameof(OnLoadMore)} (infinite-scrolling mode). " +
                $"Tree data must be provided through {nameof(Items)}.");
        }

        // Two children sources would be ambiguous: the synchronous selector and the async lazy
        // provider resolve children in incompatible ways, so exactly one may drive tree mode.
        if (ChildrenSelector is not null && ChildrenProvider is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(BitDataGrid<TItem>)} cannot use both {nameof(ChildrenSelector)} and " +
                $"{nameof(ChildrenProvider)} at the same time. Provide only one children source.");
        }

        // A PageSize parameter change from the parent supersedes any page size the user picked in the pager.
        if (PageSize != _lastPageSize) _pageSizeOverride = null;
        _effectivePageSize = Pageable ? Math.Max(1, _pageSizeOverride ?? PageSize) : int.MaxValue;

        // Reset the current selection whenever the selection mode changes
        // (e.g. switching between Single and Multiple), since the previous
        // selection no longer makes sense under the new semantics.
        if (_lastSelectionMode is not null && _lastSelectionMode != SelectionMode)
        {
            // A parent may change SelectionMode and provide SelectedItems in the same render cycle.
            // When selection is controlled, apply the (mode-normalized) incoming selection directly
            // without first emitting a transient cleared selection back to the parent.
            if (SelectedItems is not null)
            {
                ApplyControlledSelection();
            }
            else if (_selected.Count > 0)
            {
                // Uncontrolled: the previous selection no longer fits the new mode, so clear and notify.
                _selected.Clear();
                await NotifySelectionAsync();
            }
        }
        else if (SelectedItems is not null)
        {
            ApplyControlledSelection();
        }
        _lastSelectionMode = SelectionMode;

        // Only (re)load data when an external input that affects it actually changes.
        // Refreshing on every parameter set would cause an infinite loop in server mode:
        // OnRead -> caller StateHasChanged -> parent re-render -> OnParametersSetAsync -> OnRead...
        // In client mode there is no such loop, and the parent may mutate the same Items instance
        // in place (so the reference is unchanged); force a refresh there so the view never goes stale.
        // A queryable source is treated like server mode: every refresh re-executes the query against
        // the provider (a database round trip), so only a genuinely changed input may trigger one.
        var inputsChanged = !ReferenceEquals(Items, _lastItems)
            || PageSize != _lastPageSize
            || (!IsServerMode && !IsInfiniteMode && Items is not IQueryable<TItem>);
        if (!_dataInitialized || inputsChanged)
        {
            // A genuinely new Items reference means a new tree hierarchy: clear the tree bootstrap
            // state so ProcessTreeData re-applies TreeInitiallyExpanded to the new roots instead of
            // carrying over the previous source's expand/collapse state.
            if (IsTreeMode && !ReferenceEquals(Items, _lastItems))
            {
                _treeInitialized = false;
                _expandedTree.Clear();
                // A new hierarchy invalidates lazily-fetched children of the previous one.
                _loadedChildren.Clear();
                _loadingNodes.Clear();
            }

            // A replaced client-side data source drops rows for good; prune per-row state keyed on the
            // old rows (selection, expanded details) so removed items neither linger in SelectedItems
            // nor stay strongly referenced (preventing GC) through these sets.
            if (_dataInitialized && !ReferenceEquals(Items, _lastItems) && !IsServerMode && !IsInfiniteMode && !IsTreeMode)
            {
                await PruneStaleRowStateAsync();
            }

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

        if (UseServerVirtualization)
        {
            // Virtualize owns data fetching in this mode: ask it to discard its cached windows and
            // re-query through the ItemsProvider with the current sorts/filters. Before the first
            // render the reference is still null - the initial provider call covers that case.
            if (_serverVirtualize is not null)
            {
                await _serverVirtualize.RefreshDataAsync();
            }
            StateHasChanged();
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

    // ---------------------------------------------- Server-side virtualization
    /// <summary>
    /// Active when <see cref="Virtualize"/> is combined with server mode and paging is off: row windows
    /// are fetched on demand through <see cref="OnRead"/> as the user scrolls, so the whole remote
    /// dataset is browsable without a pager and without loading it all.
    /// </summary>
    internal bool UseServerVirtualization => Virtualize && IsServerMode && !Pageable;

    private async ValueTask<ItemsProviderResult<TItem>> ProvideVirtualizedRowsAsync(ItemsProviderRequest request)
    {
        var read = new BitDataGridReadRequest
        {
            Skip = request.StartIndex,
            Take = request.Count,
            Sorts = _sorts.Where(s => s.Direction != BitDataGridSortDirection.None).OrderBy(s => s.Priority).ToList(),
            Filters = _filters.ToList(),
            Groups = _groups.ToList(),
            CancellationToken = request.CancellationToken
        };

        BitDataGridReadResult<TItem> result;
        try
        {
            result = await OnRead!(read);
        }
        catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
        {
            // Superseded by a newer scroll window; Virtualize discards this result, so the content is
            // irrelevant - just keep the last known total.
            return new ItemsProviderResult<TItem>(Array.Empty<TItem>(), _totalCount);
        }

        // An OnRead that never observes the token can still complete normally after cancellation.
        // Mirroring LoadServerDataAsync's stale-response guard, bail out before committing anything so
        // this superseded window can't overwrite state the newer request owns.
        if (request.CancellationToken.IsCancellationRequested)
            return new ItemsProviderResult<TItem>(Array.Empty<TItem>(), _totalCount);

        _totalCount = result.TotalCount;

        // The loaded window doubles as the grid's "current rows" so select-all, keyboard navigation
        // and page-scoped CSV export keep operating on what is actually rendered.
        _view = result.Items;
        _pageItems = result.Items;

        // Prefer aggregates the data source computed over the whole filtered dataset (mirroring
        // LoadServerDataAsync); the local fallback covers only the loaded window.
        var previousAggregates = _footerAggregates;
        _serverAggregates = result.Aggregates is not null;
        _footerAggregates = result.Aggregates?.ToList() ?? BitDataGridDataProcessor.Aggregate(result.Items, _columns);

        RebuildRowIndexMap(result.Items, request.StartIndex);
        ReconcileEditState();

        // The empty message and the footer render outside the Virtualize component (which only
        // re-renders its own rows), so surface total-count transitions into/out of empty and changed
        // footer aggregates with an explicit re-render.
        var empty = result.TotalCount == 0;
        var footerChanged = ShowFooter && !FooterAggregatesEqual(previousAggregates, _footerAggregates);
        if (empty != _serverVirtualizeEmpty || footerChanged)
        {
            _serverVirtualizeEmpty = empty;
            _ = InvokeAsync(StateHasChanged);
        }

        return new ItemsProviderResult<TItem>(result.Items, result.TotalCount);
    }

    private static bool FooterAggregatesEqual(IReadOnlyList<BitDataGridAggregateResult> a, IReadOnlyList<BitDataGridAggregateResult> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].ColumnId != b[i].ColumnId || a[i].FormattedValue != b[i].FormattedValue) return false;
        }
        return true;
    }

    private void ProcessClientData()
    {
        var source = Items ?? Enumerable.Empty<TItem>();

        if (IsTreeMode)
        {
            ProcessTreeData(source);
            return;
        }

        if (source is IQueryable<TItem> queryable)
        {
            ProcessQueryableData(queryable);
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

        RebuildRowIndexMap(_pageItems, _groups.Count == 0 && Pageable ? (_currentPage - 1) * _effectivePageSize : 0);
        ReconcileEditState();
    }

    /// <summary>
    /// Executes the view against an <see cref="IQueryable{T}"/> source: filters and sorts are composed
    /// as expression trees and the page window is fetched with Skip/Take, so a remote provider
    /// (e.g. EF Core) runs them at the source and only the visible rows are materialized.
    /// Grouping and row reordering are unavailable in this mode (they need the full set in memory).
    /// </summary>
    private void ProcessQueryableData(IQueryable<TItem> source)
    {
        var query = BitDataGridQueryableProcessor.Apply(source, _filters, _sorts, _columnsById);

        _queryableTotal = query.Count();
        ClampPage();

        var items = Pageable
            ? query.Skip((_currentPage - 1) * _effectivePageSize).Take(_effectivePageSize).ToList()
            : query.ToList();
        _view = items;
        _pageItems = items;
        _viewGroups = null;

        // Footer aggregates need the whole filtered set; enumerate it only when a column actually
        // requests an aggregate, since that is a second full query against the provider.
        _footerAggregates = _columns.Any(c => c.Aggregate != BitDataGridAggregateType.None || c.AggregateBy is not null)
            ? BitDataGridDataProcessor.Aggregate(
                BitDataGridQueryableProcessor.ApplyFilters(source, _filters, _columnsById).ToList(), _columns)
            : new List<BitDataGridAggregateResult>();

        RebuildRowIndexMap(_pageItems, Pageable ? (_currentPage - 1) * _effectivePageSize : 0);
        ReconcileEditState();
    }

    /// <summary>
    /// Flattens the hierarchical source into the list of currently-visible rows, honouring
    /// per-sibling sorting and expand/collapse state. Paging and grouping do not apply in tree mode.
    /// </summary>
    private void ProcessTreeData(IEnumerable<TItem> roots)
    {
        // Lazy mode can't expand up-front: children aren't known until fetched on first expand.
        if (!_treeInitialized && TreeInitiallyExpanded && ChildrenSelector is not null)
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
        RebuildRowIndexMap(flat, 0);

        void Walk(IEnumerable<TItem> siblings, int level)
        {
            var sorted = _sorts.Count > 0
                ? BitDataGridDataProcessor.Sort(siblings.ToList(), _sorts, _columnsById)
                : siblings.ToList();
            foreach (var item in sorted)
            {
                var children = ResolveTreeChildren(item);
                // In lazy mode an unloaded node's expandability comes from HasChildrenSelector (the
                // children themselves don't exist locally yet). Once the children are loaded (or a
                // selector provides them) the real list wins, so a node whose fetch returned no
                // children drops its expand toggle even when the selector claimed it had some.
                var hasChildren = ChildrenProvider is not null && children is null
                    ? HasChildrenSelector?.Invoke(item) ?? false
                    : children is { Count: > 0 };
                _treeMeta[GetKey(item)] = (level, hasChildren);
                flat.Add(item);
                if (children is { Count: > 0 } && IsTreeExpanded(item))
                    Walk(children, level + 1);
            }
        }
    }

    /// <summary>
    /// The node's currently-known children: resolved through <see cref="ChildrenSelector"/> in
    /// synchronous mode, or from the lazy-load cache (null while not yet fetched) in provider mode.
    /// Materialized once because a selector may return a lazy or single-pass sequence.
    /// </summary>
    private IReadOnlyList<TItem>? ResolveTreeChildren(TItem item)
    {
        if (ChildrenSelector is not null)
            return ChildrenSelector(item) is { } c ? c as IReadOnlyList<TItem> ?? c.ToList() : null;

        return _loadedChildren.TryGetValue(GetKey(item), out var loaded) ? loaded : null;
    }

    private void ExpandTreeRecursive(IEnumerable<TItem> siblings)
    {
        foreach (var item in siblings)
        {
            // Snapshot the children once to avoid re-enumerating a lazy/single-pass sequence.
            var children = ChildrenSelector!(item) is { } c ? c as IReadOnlyList<TItem> ?? c.ToList() : null;
            if (children is not null && children.Count > 0)
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

    /// <summary>True while a lazy node's children are being fetched (renders a loading toggle).</summary>
    internal bool IsTreeNodeLoading(TItem item) => _loadingNodes.Contains(GetKey(item));

    internal async Task ToggleTreeNodeAsync(TItem item)
    {
        var key = GetKey(item);
        if (_expandedTree.Add(key))
        {
            // Expanding a lazy node for the first time: fetch and cache its children. A re-entrant
            // toggle while the fetch is in flight is ignored so the provider isn't called twice.
            if (ChildrenProvider is not null && !_loadedChildren.ContainsKey(key))
            {
                if (!_loadingNodes.Add(key))
                {
                    _expandedTree.Remove(key);
                    return;
                }
                StateHasChanged();
                try
                {
                    var children = await ChildrenProvider(item);
                    _loadedChildren[key] = children as IReadOnlyList<TItem> ?? children?.ToList() ?? (IReadOnlyList<TItem>)Array.Empty<TItem>();
                }
                catch
                {
                    // A failed fetch collapses the node again so a later expand retries, instead of
                    // caching an empty result that would permanently swallow the branch.
                    _expandedTree.Remove(key);
                    throw;
                }
                finally
                {
                    _loadingNodes.Remove(key);
                }
            }
        }
        else
        {
            _expandedTree.Remove(key);
        }
        await RefreshAsync();
    }

    /// <summary>Expands every node in the tree. No-op outside tree mode. In lazy mode
    /// (<see cref="ChildrenProvider"/>) only already-loaded branches expand - recursively fetching the
    /// whole tree could be unbounded.</summary>
    public async Task ExpandAllAsync()
    {
        if (!IsTreeMode) return;
        _expandedTree.Clear();
        if (ChildrenProvider is not null)
        {
            foreach (var key in _loadedChildren.Keys) _expandedTree.Add(key);
        }
        else
        {
            ExpandTreeRecursive(Items ?? Enumerable.Empty<TItem>());
        }
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

        // Recompute footer aggregates against the now-empty list so ShowFooter doesn't keep displaying
        // totals from the pre-reset data while the first batch is still loading. LoadNextBatchAsync will
        // recompute them again once rows arrive.
        _footerAggregates = BitDataGridDataProcessor.Aggregate(_infiniteItems, _columns);

        // Bump the load version up-front so any batch still in flight from before this reset is
        // recognised as stale (by the version check in LoadNextBatchAsync) and won't append rows to
        // the freshly cleared list while we await scrollToTop below.
        _loadVersion++;

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
    /// rapid scroll events coalesce into a single in-flight request. Returns <c>true</c> only when a
    /// batch was actually appended (so callers can decide whether a viewport re-check is warranted);
    /// a no-op call (load already in flight, no more data, or a superseded/cancelled request) returns
    /// <c>false</c>.
    /// </summary>
    private async Task<bool> LoadNextBatchAsync()
    {
        if (OnLoadMore is null || _infiniteLoading || !_infiniteHasMore) return false;

        _infiniteLoading = true;
        StateHasChanged();

        var batch = Math.Max(1, LoadMoreBatchSize);
        var read = new BitDataGridReadRequest
        {
            Skip = _infiniteItems.Count,
            Take = batch,
            Sorts = _sorts.Where(s => s.Direction != BitDataGridSortDirection.None).OrderBy(s => s.Priority).ToList(),
            Filters = _filters.ToList(),
            Groups = _groups.ToList(),
            CancellationToken = ResetLoadCancellation()
        };
        var version = _loadVersion;
        var appended = false;

        try
        {
            var result = await OnLoadMore(read);
            // A newer request superseded this one (e.g. sort/filter changed mid-flight); drop the stale response.
            if (version != _loadVersion) return false;

            var loaded = result.Items;
            _infiniteItems.AddRange(loaded);
            if (loaded.Count < batch) _infiniteHasMore = false;

            _view = _infiniteItems;
            _pageItems = _infiniteItems;
            _footerAggregates = BitDataGridDataProcessor.Aggregate(_infiniteItems, _columns);
            RebuildRowIndexMap(_infiniteItems, 0);
            ReconcileEditState();
            appended = true;
        }
        catch (OperationCanceledException) when (read.CancellationToken.IsCancellationRequested)
        {
            // The in-flight batch was superseded by a newer load (sort/filter change or reset) whose
            // cancellation token fired. Cancellation from our own token is expected here, so drop this
            // batch and let the newer load own the loading state. Any other cancellation (e.g. a
            // provider-side timeout) is a real error and propagates.
            return false;
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

        // Only after a batch was genuinely appended do we ask JS whether the viewport still isn't
        // filled (e.g. a short first batch) and therefore needs another load. Gating the re-check on a
        // real append is essential: re-checking after a no-op load would spin a tight JS<->.NET loop -
        // while the initial batch is still in flight the viewport is empty (so it always looks "near the
        // end"), every check() would re-enter here, hit the _infiniteLoading guard, return immediately
        // and re-check again, starving the in-flight batch's continuation and freezing the UI thread.
        if (appended && _infiniteHasMore && _infiniteHandle is not null)
        {
            try { await _infiniteHandle.InvokeVoidAsync("check"); }
            catch (JSException) { }
            catch (JSDisconnectedException) { }
        }

        return appended;
    }

    /// <summary>Invoked from JavaScript when the viewport is scrolled near its end.</summary>
    [JSInvokable]
    public async Task<bool> OnInfiniteScrollNearEndAsync()
    {
        if (!IsInfiniteMode) return false;
        // Returns whether the JS watcher should re-check the viewport: only when this load actually
        // appended a batch and more data may remain. At end-of-data (or a no-op load) this returns
        // false so the watcher stops re-invoking, instead of spinning a tight JS<->.NET loop.
        var appended = await LoadNextBatchAsync();
        return appended && _infiniteHasMore;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (HasSelectColumn && ShowHeader)
        {
            // Sync the select-all checkbox's indeterminate DOM property. Only invoke when the value
            // actually changed (or on first render) to avoid a JS round trip on every render.
            var indeterminate = PageSelectionState.Some;
            if (firstRender || indeterminate != _lastIndeterminate)
            {
                _lastIndeterminate = indeterminate;
                try { await JS.InvokeVoidAsync("BitBlazorUI.DataGrid.setIndeterminate", _selectAllCheckbox, indeterminate); }
                catch (JSException) { }
                catch (JSDisconnectedException) { }
            }
        }
        else
        {
            // The checkbox element is gone (selection mode changed / header hidden); forget the last
            // synced value so a re-created checkbox gets re-synced instead of matching a stale cache.
            _lastIndeterminate = null;
        }

        // Touch/pen reorder needs a JS pointer-event bridge (native HTML5 DnD is mouse-only). Attached
        // once; the .NET drop handlers re-validate the feature gates, so a later parameter change that
        // disables reordering simply makes the callbacks no-ops.
        if ((RowReorderable || Reorderable) && !_pointerReorderAttached)
        {
            _gridSelfRef ??= DotNetObjectReference.Create(this);
            try
            {
                _pointerReorderHandle = await JS.InvokeAsync<IJSObjectReference>(
                    "BitBlazorUI.DataGrid.initPointerReorder", _rootRef, _gridSelfRef);
                // Only marked attached on success, so a failed init (transient JS error) is retried on
                // a later render instead of permanently disabling touch reorder.
                _pointerReorderAttached = true;
            }
            catch (JSException) { }
            catch (JSDisconnectedException) { }
        }

        // Column virtualization needs the viewport's horizontal scroll offset and width, which Blazor
        // event args don't expose; a JS observer reports them (rAF-throttled, with hysteresis).
        if (VirtualizeColumns && !_hScrollAttached)
        {
            _gridSelfRef ??= DotNetObjectReference.Create(this);
            try
            {
                _hScrollHandle = await JS.InvokeAsync<IJSObjectReference>(
                    "BitBlazorUI.DataGrid.initHorizontalScroll", _infiniteViewport, _gridSelfRef, HScrollReportThresholdPx);
                // Only marked attached on success so a failed init retries on a later render.
                _hScrollAttached = true;
            }
            catch (JSException) { }
            catch (JSDisconnectedException) { }
        }

        if (IsInfiniteMode && !_infiniteObserverAttached)
        {
            _infiniteObserverAttached = true;
            _infiniteSelfRef ??= DotNetObjectReference.Create(this);
            _infiniteHandle = await JS.InvokeAsync<IJSObjectReference>(
                "BitBlazorUI.DataGrid.initInfiniteScroll", _infiniteViewport, _infiniteSelfRef, 200);
        }
        else if (!IsInfiniteMode && _infiniteObserverAttached)
        {
            // Infinite mode was turned off (OnLoadMore became null) after the observer was attached.
            // Tear the JS observer down so it can't keep firing OnInfiniteScrollNearEndAsync against a
            // grid that no longer streams batches, and reset the state so a later re-enable re-attaches.
            _infiniteObserverAttached = false;
            var handle = _infiniteHandle;
            _infiniteHandle = null;
            if (handle is not null)
            {
                try
                {
                    await handle.InvokeVoidAsync("dispose");
                    await handle.DisposeAsync();
                }
                catch (JSDisconnectedException) { }
                catch (JSException) { }
            }
            // Drop the callback reference too; a later re-enable recreates it via the ??= above.
            _infiniteSelfRef?.Dispose();
            _infiniteSelfRef = null;
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
        try
        {
            if (_pointerReorderHandle is not null)
            {
                await _pointerReorderHandle.InvokeVoidAsync("dispose");
                await _pointerReorderHandle.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        try
        {
            if (_hScrollHandle is not null)
            {
                await _hScrollHandle.InvokeVoidAsync("dispose");
                await _hScrollHandle.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        _gridSelfRef?.Dispose();
        // Only signal cancellation here; deterministic disposal of _loadCts belongs to the request
        // lifecycle (ResetLoadCancellation). Disposing it during teardown could surface an
        // ObjectDisposedException for an OnRead/OnLoadMore call still holding the token.
        _loadCts?.Cancel();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Cancels any in-flight data request and returns a fresh token for the next one,
    /// so superseded requests can stop early.
    /// </summary>
    private CancellationToken ResetLoadCancellation()
    {
        // Cancel but do NOT dispose the previous source: an OnRead/OnLoadMore call may still be holding
        // its token and disposing it now would surface an ObjectDisposedException (e.g. on token
        // registration) instead of the expected OperationCanceledException. The orphaned source has no
        // timer or registrations of its own, so it is cheap to let the GC reclaim it once the in-flight
        // operation observes cancellation and completes.
        _loadCts?.Cancel();
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
            Groups = _groups.ToList(),
            CancellationToken = ResetLoadCancellation()
        };
        // Capture this request's version right after ResetLoadCancellation; bail out below if a newer
        // request has since superseded it so a stale response can't overwrite fresher state.
        var version = _loadVersion;
        BitDataGridReadResult<TItem> result;
        try
        {
            result = await OnRead!(request);
        }
        catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
        {
            // Superseded by a newer request whose cancellation token fired; cancellation from our own
            // token is expected, so keep the existing state and let the newer load complete. Any other
            // cancellation (e.g. a provider-side timeout) is a real error and propagates.
            return;
        }
        if (version != _loadVersion) return;

        // Enforce the OnRead (server-mode) contract here, where the data mode is unambiguous, rather
        // than in the shared BitDataGridReadResult constructor: a single page can never legitimately
        // hold more rows than the reported grand total, and accepting that would feed paging math an
        // inconsistent _pageItems/_totalCount pair. The constructor stays lenient so the infinite-
        // scrolling (OnLoadMore) path can keep returning batches with an unknown (0) total.
        if (result.Items.Count > result.TotalCount)
            throw new InvalidOperationException(
                $"{nameof(OnRead)} returned a page with more items ({result.Items.Count}) than the reported " +
                $"{nameof(BitDataGridReadResult<TItem>.TotalCount)} ({result.TotalCount}).");

        _pageItems = result.Items;
        _view = result.Items;
        _totalCount = result.TotalCount;
        // Prefer aggregates the data source computed over the whole filtered dataset; falling back to a
        // local computation covers only the current page (a per-page number shown as if it were a total).
        _serverAggregates = result.Aggregates is not null;
        _footerAggregates = result.Aggregates?.ToList() ?? BitDataGridDataProcessor.Aggregate(_pageItems, _columns);
        _viewGroups = null;
        RebuildRowIndexMap(_pageItems, Pageable ? (_currentPage - 1) * _effectivePageSize : 0);

        // If the server reported fewer rows than the requested page range implies, the page we just
        // fetched is out of range and produced an empty/short slice. Clamp to the last valid page and,
        // when that actually moved us, re-fetch so the UI shows the clamped page's data instead of the
        // stale out-of-range result. The clamped page is valid for the new TotalCount, so this recurses
        // at most once.
        var pageBeforeClamp = _currentPage;
        ClampPage();
        if (Pageable && _currentPage != pageBeforeClamp)
        {
            await LoadServerDataAsync();
        }

        ReconcileEditState();
    }

    private void ClampPage()
    {
        var pages = TotalPages;
        if (_currentPage > pages) _currentPage = pages;
        if (_currentPage < 1) _currentPage = 1;
    }

    // ------------------------------------------------------------- Sorting
    internal bool ColumnSortable(BitDataGridColumn<TItem> column)
        // A custom SortBy key makes a template-only column sortable; otherwise a bound field is required.
        // In server mode a SortBy delegate cannot be forwarded through the read request, and in
        // queryable mode a delegate has no expression tree to translate, so only field-backed columns
        // are sortable there.
        => (column.HasField || (column.SortBy is not null && !IsServerMode && !IsInfiniteMode && !IsQueryableMode))
        && (column.Sortable ?? Sortable);

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

        Announce(GetSort(column) switch
        {
            null => string.Format(Strings.AnnouncementSortCleared, column.DisplayTitle),
            { Direction: BitDataGridSortDirection.Descending } => string.Format(Strings.AnnouncementSortedDescending, column.DisplayTitle),
            _ => string.Format(Strings.AnnouncementSortedAscending, column.DisplayTitle),
        });

        await RefreshAsync();
    }

    private void Reprioritize()
    {
        for (int i = 0; i < _sorts.Count; i++) _sorts[i].Priority = i + 1;
    }

    // ----------------------------------------------------------- Filtering
    internal bool ColumnFilterable(BitDataGridColumn<TItem> column)
        // Filtering is not applied in tree mode: ProcessTreeData flattens the hierarchy using only the
        // sibling sort and never runs the filter pipeline, so a filter input there would appear active
        // without affecting the rendered rows. Disable it until tree mode carries filtering.
        => column.HasField && !IsTreeMode && (column.Filterable ?? Filterable);

    internal BitDataGridFilterDescriptor? GetFilter(BitDataGridColumn<TItem> column)
        => _filters.FirstOrDefault(f => f.ColumnId == column.Id);

    /// <summary>Whether the column renders a filter-operator dropdown next to its filter editor.</summary>
    internal bool ColumnFilterOperators(BitDataGridColumn<TItem> column)
        => ColumnFilterable(column) && (column.FilterOperators ?? FilterOperators);

    /// <summary>The operator the column's filter editor applies: the user's dropdown choice when one
    /// was made, otherwise the type default (Contains for text, Equals for everything else).</summary>
    internal BitDataGridFilterOperator EffectiveFilterOperator(BitDataGridColumn<TItem> column)
        => _filterOps.TryGetValue(column.Id, out var op) ? op
            : column.EffectiveDataType == BitDataGridColumnDataType.Text
                ? BitDataGridFilterOperator.Contains
                : BitDataGridFilterOperator.Equals;

    internal async Task SetFilterAsync(BitDataGridColumn<TItem> column, BitDataGridFilterOperator op, object? value)
    {
        // Always replace every descriptor the column holds, not just the first match: a date Equals
        // filter is stored as a paired half-open range (two descriptors), so updating one in place
        // (e.g. when the user switches the filter operator) would leave the partner descriptor
        // dangling and silently AND it into the new filter.
        _filters.RemoveAll(f => f.ColumnId == column.Id);

        // Treat a null, empty or whitespace-only value as "no filter" so a box cleared to spaces clears
        // the filter rather than being stored as a criterion. This matches the data processor, which
        // also ignores whitespace-only filter values, keeping remote and client modes consistent.
        // Unspecified is the omitted/invalid operator (see the enum), so it never produces a
        // descriptor either - the filter list and OnRead payload only ever carry real operators.
        var isEmpty = value is null || (value is string s && string.IsNullOrWhiteSpace(s));
        var active = op is not BitDataGridFilterOperator.Unspecified
            && (!isEmpty || op is BitDataGridFilterOperator.IsEmpty or BitDataGridFilterOperator.IsNotEmpty);
        if (active)
        {
            _filters.Add(new BitDataGridFilterDescriptor { ColumnId = column.Id, Operator = op, Value = value });
            // Keep the filter editor's UI state aligned with the descriptor so a programmatic
            // ApplyFilterAsync shows up in the header (operator dropdown + raw text) like a user edit.
            _filterOps[column.Id] = op;
            _filterRaw[column.Id] = FormatFilterRaw(value);
        }
        else
        {
            _filterRaw.Remove(column.Id);
        }
        Announce(string.Format(active ? Strings.AnnouncementFiltered : Strings.AnnouncementFilterCleared, column.DisplayTitle));
        _currentPage = 1;
        await RefreshAsync();
    }

    // Applies a half-open [start, endExclusive) range for a date/time column as two standard comparison
    // descriptors (>= start AND < endExclusive), which the data processor and OnRead consumers AND together.
    // This keeps day-level date filtering boundary-safe for server-side consumers that compare against the
    // raw values, instead of a single midnight Equals descriptor an exact match would never satisfy. A null
    // start clears the column's filter.
    internal async Task SetDateRangeFilterAsync(BitDataGridColumn<TItem> column, object? start, object? endExclusive)
    {
        _filters.RemoveAll(f => f.ColumnId == column.Id);
        if (start is not null && endExclusive is not null)
        {
            _filters.Add(new BitDataGridFilterDescriptor { ColumnId = column.Id, Operator = BitDataGridFilterOperator.GreaterThanOrEqual, Value = start });
            _filters.Add(new BitDataGridFilterDescriptor { ColumnId = column.Id, Operator = BitDataGridFilterOperator.LessThan, Value = endExclusive });
        }
        _currentPage = 1;
        await RefreshAsync();
    }

    public async Task ClearFiltersAsync()
    {
        _filters.Clear();
        _filterRaw.Clear();
        await RefreshAsync();
    }

    // The raw filter-editor text equivalent of a descriptor value, used to backfill _filterRaw when a
    // filter is applied programmatically or restored from a snapshot. Invariant formatting matches how
    // the typed editors parse their input back (see SetTypedFilterAsync).
    private static string? FormatFilterRaw(object? value)
        => value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value?.ToString();

    // Values in a state snapshot that was round-tripped through System.Text.Json deserialize as
    // JsonElement, which never equals the CLR values the filter pipeline compares against (so such
    // filters would fail closed). Unwrap the element to its primitive and coerce it to the column's
    // member type when one is known.
    private static object? NormalizeFilterValue(object? value, BitDataGridColumn<TItem> column)
    {
        if (value is not JsonElement json) return value;

        object? raw = json.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => json.TryGetInt64(out var l) ? l : json.TryGetDecimal(out var m) ? m : json.GetDouble(),
            JsonValueKind.String => json.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => json.ToString(),
        };

        return raw is not null && column.Accessor is { } accessor && accessor.TryConvertValue(raw, out var converted)
            ? converted
            : raw;
    }

    // ----------------------------------------------------------- Grouping
    // Grouping is a client-side operation: it reshapes the locally-held _view into _viewGroups.
    // Server mode (OnRead) and infinite-scrolling mode (OnLoadMore) only forward sorts and filters
    // to the data callback and render the returned rows flat, so a group would appear active without
    // affecting the list. Tree mode likewise flattens the hierarchy without grouping, and queryable
    // mode holds only the current page in memory. Grouping is rejected in those flows - both in the
    // UI (ColumnGroupable) and on the programmatic/restore paths (GroupByAsync, ApplyStateAsync).
    private bool GroupingAllowed => !IsServerMode && !IsInfiniteMode && !IsTreeMode && !IsQueryableMode;

    internal bool ColumnGroupable(BitDataGridColumn<TItem> column)
        => column.HasField && GroupingAllowed && (column.Groupable ?? Groupable);

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

    /// <summary>
    /// Single-pass, allocation-free page selection summary: <c>All</c> drives the select-all checkbox's
    /// checked state, <c>Some</c> its indeterminate state (some but not all selectable rows selected).
    /// </summary>
    internal (bool All, bool Some) PageSelectionState
    {
        get
        {
            int selectable = 0, selected = 0;
            foreach (var item in _pageItems)
            {
                if (!CanSelectRow(item)) continue;
                selectable++;
                if (_selected.Contains(item)) selected++;
            }
            return (selectable > 0 && selected == selectable, selected > 0 && selected < selectable);
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

    /// <summary>
    /// Applies the parent-controlled <see cref="SelectedItems"/> into the internal selection set,
    /// normalized to the current <see cref="SelectionMode"/> (None selects nothing, Single keeps at
    /// most one item). Does not notify the parent, since the selection is incoming rather than changed here.
    /// </summary>
    /// <summary>
    /// Removes selection and expanded-detail entries whose rows are no longer present in the (replaced)
    /// client-side data source. A null source counts as empty (all row state is dropped); otherwise only
    /// materialized sources (<see cref="ICollection{T}"/>) are pruned so a lazy
    /// <see cref="IEnumerable{T}"/> isn't enumerated an extra time. Controlled selection is skipped
    /// (<see cref="SelectedItems"/> is authoritative there and is re-applied on every parameter set).
    /// </summary>
    private async Task PruneStaleRowStateAsync()
    {
        // A cleared source (Items set to null) drops every row, so all keyed row state goes with it;
        // returning early here would leave removed rows selected/expanded (and strongly referenced).
        if (Items is null)
        {
            _expandedDetails.Clear();
            if (SelectedItems is null && _selectedSet is { Count: > 0 })
            {
                _selectedSet.Clear();
                await NotifySelectionAsync();
            }
            return;
        }

        if (Items is not ICollection<TItem> source) return;

        var keys = new HashSet<object>();
        foreach (var item in source) keys.Add(GetKey(item));

        _expandedDetails.RemoveWhere(k => !keys.Contains(k));

        if (SelectedItems is null && _selectedSet is { Count: > 0 })
        {
            var removed = _selectedSet.RemoveWhere(i => !keys.Contains(GetKey(i)));
            if (removed > 0) await NotifySelectionAsync();
        }
    }

    private void ApplyControlledSelection()
    {
        _selected.Clear();
        if (SelectedItems is null || SelectionMode == BitDataGridSelectionMode.None) return;
        foreach (var i in SelectedItems)
        {
            _selected.Add(i);
            if (SelectionMode == BitDataGridSelectionMode.Single) break;
        }
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
        // Value-type rows are excluded from inline editing: TItem is held (and passed to the property
        // setter) by value, so edits would mutate a throwaway copy and never persist back to the bound
        // data. Disallowing the editor here avoids silently discarding the user's input.
        => !typeof(TItem).IsValueType
        && column.HasField && column.Accessor?.CanWrite == true && (column.Editable ?? Editable);

    internal void BeginEdit(TItem item)
    {
        _editItem = item;
        _isNewItem = false;
        _editBuffer = null;
        _editErrors = null;
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
        _editBuffer = null;
        _editErrors = null;
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

        // Saving is blocked while any editor holds an invalid value; the errors are already rendered
        // under their editors, so just stay in edit mode.
        if (HasEditErrors)
        {
            StateHasChanged();
            return;
        }

        var item = _editItem;

        // Apply the buffered edits to the live object only now, at commit. Until this point the
        // built-in editors never touched the row, so a Cancel (or an abandoned edit) leaves it intact.
        if (_editBuffer is not null)
        {
            foreach (var (colId, value) in _editBuffer)
                if (_columnsById.TryGetValue(colId, out var col))
                    col.Accessor?.SetValue(item, value);
        }

        ClearEditState();
        if (OnRowSave.HasDelegate) await OnRowSave.InvokeAsync(item);
        await RefreshAsync();
    }

    internal async Task CancelEditAsync()
    {
        if (_editItem is null) return;
        var item = _editItem;
        // Built-in editors only wrote to the (now discarded) buffer, so the snapshot restore below is
        // solely for custom EditTemplates, which bind straight to the item.
        if (!_isNewItem && _editSnapshot is not null)
        {
            foreach (var (colId, value) in _editSnapshot)
                if (_columnsById.TryGetValue(colId, out var col))
                    col.Accessor?.SetValue(item, value);
        }
        ClearEditState();
        if (OnRowCancel.HasDelegate) await OnRowCancel.InvokeAsync(item);
        StateHasChanged();
    }

    private void ClearEditState()
    {
        _editItem = default;
        _pendingNew = default;
        _editSnapshot = null;
        _isNewItem = false;
        _editBuffer = null;
        _editErrors = null;
    }

    internal async Task DeleteRowAsync(TItem item)
    {
        if (OnRowDelete.HasDelegate)
        {
            await OnRowDelete.InvokeAsync(item);
            // Deleting leaves no visible trace at the trigger point (button or Delete key), so tell
            // screen-reader users it happened; only announce when a handler actually processed it.
            Announce(Strings.AnnouncementRowDeleted);
        }
        _selected.Remove(item);
        await RefreshAsync();
    }

    internal void SetEditValue(BitDataGridColumn<TItem> column, object? value)
    {
        if (_editItem is null || column.Accessor is null) return;

        _editBuffer ??= new Dictionary<string, object?>();
        _editErrors ??= new Dictionary<string, string>();

        // Convert first (same coercion a direct write would apply), but into the buffer, not the row.
        if (!column.Accessor.TryConvertValue(value, out var converted))
        {
            // Keep the raw input so the editor doesn't wipe what the user typed, and surface the
            // conversion failure - previously unparseable input was silently discarded.
            _editBuffer[column.Id] = value;
            _editErrors[column.Id] = string.Format(Strings.InvalidValueError, column.DisplayTitle);
            StateHasChanged();
            return;
        }

        var error = column.Validate?.Invoke(_editItem, converted);
        if (error is not null) _editErrors[column.Id] = error;
        else _editErrors.Remove(column.Id);

        _editBuffer[column.Id] = converted;
        StateHasChanged();
    }

    /// <summary>The value an editor should display: the buffered pending edit when one exists,
    /// otherwise the row's current value.</summary>
    internal object? GetEditValue(BitDataGridColumn<TItem> column, TItem item)
        => _editBuffer is not null && _editBuffer.TryGetValue(column.Id, out var pending)
            ? pending
            : column.GetValue(item);

    /// <summary>The active validation error for a column of the row being edited, or null.</summary>
    internal string? GetEditError(BitDataGridColumn<TItem> column)
        => _editErrors is not null && _editErrors.TryGetValue(column.Id, out var error) ? error : null;

    /// <summary>True while any editor of the row being edited holds an invalid value.</summary>
    internal bool HasEditErrors => _editErrors is { Count: > 0 };

    /// <summary>
    /// Drops a dangling inline edit whose row is no longer in the rendered view (it was filtered,
    /// sorted or paged away, or the data source was replaced mid-edit). The snapshot is restored first
    /// so the abandoned row doesn't keep half-committed values, mirroring an explicit Cancel.
    /// A pending new row is exempt: it renders above the view and is never part of _pageItems.
    /// </summary>
    private void ReconcileEditState()
    {
        if (_editItem is null || _isNewItem) return;

        foreach (var item in _pageItems)
        {
            if (KeyEquals(item, _editItem)) return;
        }

        var item2 = _editItem;
        if (_editSnapshot is not null)
        {
            foreach (var (colId, value) in _editSnapshot)
                if (_columnsById.TryGetValue(colId, out var col))
                    col.Accessor?.SetValue(item2, value);
        }
        _editItem = default;
        _editSnapshot = null;
        _isNewItem = false;
        _editBuffer = null;
        _editErrors = null;
    }

    // ---------------------------------------------------------- Resizing
    internal async Task StartResizeAsync(BitDataGridColumn<TItem> column, double clientX)
    {
        _resizingColumn = column;
        _resizeStartX = clientX;
        // Prefer the header cell's real rendered width: a %/fr-sized column has no px width .NET could
        // parse, and even a px-declared one may be constrained by min/max at render time. Falls back to
        // parsing the declared width when JS is unavailable (prerendering, disconnected circuit).
        _resizeStartWidth = column.ResizedWidth ?? await MeasureColumnWidthAsync(column);
        StateHasChanged();
    }

    private async Task<double> MeasureColumnWidthAsync(BitDataGridColumn<TItem> column)
    {
        try
        {
            var width = await JS.InvokeAsync<double>("BitBlazorUI.DataGrid.getWidth", column.HeaderCellRef);
            if (width > 0) return width;
        }
        catch (JSException) { }
        catch (JSDisconnectedException) { }
        return ParseInitialWidth(column);
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
        if (!ColumnReorderable(_dragColumn) || !ColumnReorderable(target)) { _dragColumn = null; return; }
        var from = _columns.IndexOf(_dragColumn);
        var to = _columns.IndexOf(target);
        if (from < 0 || to < 0) { _dragColumn = null; return; }
        _columns.RemoveAt(from);
        _columns.Insert(to, _dragColumn);
        _dragColumn = null;
        InvalidateVisibleColumns();
        StateHasChanged();
    }

    internal bool ColumnResizable(BitDataGridColumn<TItem> column) => column.Resizable ?? Resizable;
    internal bool ColumnReorderable(BitDataGridColumn<TItem> column) => column.Reorderable ?? Reorderable;

    // ----------------------------------------------------- Row reordering
    // Row reordering moves items by index within the bound source list (see DropRowAsync). That is only
    // coherent when the rendered order maps 1:1 to that source, so it is disabled whenever the view is
    // transformed (sorting, filtering, grouping, tree mode) or driven remotely (server/infinite), where
    // _view no longer matches the underlying Items list. Centralizing the gate here keeps the drag handle,
    // drag start, keyboard move and drop all consistent.
    internal bool RowReorderEnabled => RowReorderable
        && _sorts.Count == 0
        && _filters.Count == 0
        && _groups.Count == 0
        && !IsTreeMode
        && !IsServerMode
        && !IsInfiniteMode
        && !IsQueryableMode;

    internal void StartRowDrag(TItem row)
    {
        if (!RowReorderEnabled) return;
        _dragRow = row;
    }

    /// <summary>Invoked from JavaScript when a touch/pen drag drops a row onto another row. Indices
    /// are the rows' data-ri attributes (absolute dataset positions).</summary>
    [JSInvokable]
    public async Task OnPointerRowDropAsync(int fromIndex, int toIndex)
    {
        if (!RowReorderEnabled) return;
        if (!TryGetRowAtDataIndex(fromIndex, out var from) || !TryGetRowAtDataIndex(toIndex, out var to)) return;
        _dragRow = from;
        await DropRowAsync(to);
    }

    /// <summary>Invoked from JavaScript when a touch/pen drag drops a header cell onto another,
    /// identified by their data-col attributes (column ids).</summary>
    [JSInvokable]
    public async Task OnPointerColumnDropAsync(string fromId, string toId)
    {
        if (!_columnsById.TryGetValue(fromId, out var from) || !_columnsById.TryGetValue(toId, out var to)) return;
        StartColumnDrag(from);
        DropColumn(to);
        await Task.CompletedTask;
    }

    /// <summary>Resolves a rendered row from its absolute dataset index (the row's data-ri value).</summary>
    private bool TryGetRowAtDataIndex(int index, out TItem item)
    {
        var offset = PagingActive ? (_currentPage - 1) * _effectivePageSize : 0;
        var i = index - offset;
        if (i >= 0 && i < _pageItems.Count)
        {
            item = _pageItems[i];
            return true;
        }
        item = default!;
        return false;
    }

    /// <summary>
    /// Moves a row one position toward the start (<paramref name="delta"/> = -1) or end (+1) of the
    /// current view, reusing the same reorder pipeline as drag-and-drop. Backs the keyboard-accessible
    /// reorder handle so row reordering is not pointer-only.
    /// </summary>
    internal async Task MoveRowAsync(TItem row, int delta)
    {
        if (!RowReorderEnabled) return;

        // Confine neighbor selection to the current page slice so keyboard reordering never jumps across
        // pages. With no sort/filter/group active (RowReorderEnabled requires that), _pageItems is either
        // the visible page (when paging) or the full view, so it is always the correct lookup set.
        var view = _pageItems;
        int from = -1;
        for (int i = 0; i < view.Count; i++)
        {
            if (KeyEquals(view[i], row)) { from = i; break; }
        }
        if (from < 0) return;

        var to = from + delta;
        if (to < 0 || to >= view.Count) return;

        _dragRow = row;
        await DropRowAsync(view[to]);
    }

    internal async Task DropRowAsync(TItem target)
    {
        if (!RowReorderEnabled) { _dragRow = default; return; }
        if (_dragRow is null || KeyEquals(_dragRow, target)) { _dragRow = default; return; }

        var dragged = _dragRow;
        _dragRow = default;

        // Determine indices within the bound source.
        if (Items is IList<TItem> list)
        {
            var from = IndexOfByKey(list, dragged);
            var to = IndexOfByKey(list, target);
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
                FromIndex = null,
                ToIndex = null
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

    // Memoizes "is the focused row still in the current view?" for CellTabIndex. That check is an
    // O(rows) scan, and CellTabIndex runs for every rendered cell, so recomputing it per cell would be
    // O(rows² × cols) per render. The cache is keyed on the view instance and a focus version that is
    // bumped whenever _focusedRow changes, so any focus move or view swap recomputes exactly once.
    private int _focusVersion;
    private (IReadOnlyList<TItem>? Rows, int Version, bool Visible) _focusedRowVisibleCache;

    private bool FocusedRowVisible(IReadOnlyList<TItem> rows)
    {
        if (_focusedRow is null) return false;
        if (!ReferenceEquals(_focusedRowVisibleCache.Rows, rows) || _focusedRowVisibleCache.Version != _focusVersion)
        {
            var visible = false;
            for (int i = 0; i < rows.Count; i++)
            {
                if (KeyEquals(rows[i], _focusedRow)) { visible = true; break; }
            }
            _focusedRowVisibleCache = (rows, _focusVersion, visible);
        }
        return _focusedRowVisibleCache.Visible;
    }

    /// <summary>Roving tabindex: only one cell is in the tab order at a time.</summary>
    internal int CellTabIndex(TItem item, int colIndex)
    {
        var rows = NavigableRows;

        // If a focused row is set and still present in the current view, keep its cell tabbable.
        if (_focusedRow is not null && FocusedRowVisible(rows))
            return IsCellFocused(item, colIndex) ? 0 : -1;

        // Otherwise the focused row was paged/filtered/sorted away: fall back to the first cell.
        return rows.Count > 0 && KeyEquals(rows[0], item) && colIndex == 0 ? 0 : -1;
    }

    /// <summary>Records the focused cell when the user clicks/tabs into it (no re-focus needed).</summary>
    internal void SetFocusedCell(TItem item, int colIndex)
    {
        if (IsCellFocused(item, colIndex)) return;
        _focusedRow = item;
        _focusedCol = colIndex;
        _focusVersion++;
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
            case "Delete":
                // Keyboard parity with the command column's Delete button, gated the same way
                // (Editable). An editing row's cells never route here (see BitDataGridCell), so
                // Delete inside an editor keeps deleting text instead of the row.
                if (!Editable) { handled = false; break; }
                await DeleteRowAsync(item);
                // The focused row is gone; land on the row that took its place (or the new last
                // row) so the roving tab stop stays inside the grid.
                rows = NavigableRows;
                if (rows.Count == 0) { _focusedRow = default; return; }
                row = Math.Min(rowIdx, rows.Count - 1);
                break;
            default: handled = false; break;
        }
        if (!handled) return;

        row = Math.Clamp(row, 0, rows.Count - 1);
        col = Math.Clamp(col, 0, colCount - 1);
        // The target row may span columns; snap focus to the actually-rendered cell so it always
        // lands on a real BitDataGridCell with tabindex=0 instead of a spanned-away column index.
        col = SnapToRenderedColumn(rows[row], col, colDir);
        // While column virtualization is active only the ColumnSlots window exists in the DOM, so the
        // target must additionally snap to a rendered slot or focus would land on a spacer-collapsed
        // column with no cell (and no tabindex) to receive it.
        col = SnapToVirtualizedColumn(col, colDir);
        _focusedRow = rows[row];
        _focusedCol = col;
        _focusVersion++;
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

    /// <summary>
    /// Maps a desired visible-column index to a column actually present in <see cref="ColumnSlots"/>
    /// while column virtualization is active (a scrolled-out column has no rendered cell to focus).
    /// Advances past the skipped run in the travel direction; with no direction (vertical moves,
    /// Home) the nearest rendered column at or after the target wins. Spans never coexist with
    /// column virtualization, so this composes safely with <see cref="SnapToRenderedColumn"/>.
    /// </summary>
    private int SnapToVirtualizedColumn(int target, int dir)
    {
        if (!ColumnVirtualizationActive) return target;

        int before = -1, after = -1;
        foreach (var slot in ColumnSlots)
        {
            if (slot.Column is null) continue;
            if (slot.ColIndex == target) return target;
            if (slot.ColIndex < target) { if (slot.ColIndex > before) before = slot.ColIndex; }
            else if (after < 0 || slot.ColIndex < after) after = slot.ColIndex;
        }

        var preferred = dir < 0 ? before : after;
        var fallback = dir < 0 ? after : before;
        return preferred >= 0 ? preferred : fallback >= 0 ? fallback : target;
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
    /// <summary>Navigates to the given 1-based page (clamped to the valid range).</summary>
    public async Task GoToPageAsync(int page)
    {
        _currentPage = Math.Clamp(page, 1, TotalPages);
        Announce(string.Format(Strings.AnnouncementPage, _currentPage, TotalPages));
        await RefreshAsync();
    }

    /// <summary>Changes the page size and resets to the first page. Overrides the <see cref="PageSize"/>
    /// parameter until the parent changes that parameter again.</summary>
    public async Task SetPageSizeAsync(int size)
    {
        _pageSizeOverride = Math.Max(1, size);
        _effectivePageSize = Pageable ? _pageSizeOverride.Value : int.MaxValue;
        _currentPage = 1;
        await RefreshAsync();
    }

    // ------------------------------------------------------ Public state API
    // These return snapshot lists (not the live backing collections) so a caller holding the result
    // can't observe or induce mid-lifecycle mutations of the grid's internal descriptor state.

    /// <summary>The active sort descriptors, in priority order (a snapshot).</summary>
    public IReadOnlyList<BitDataGridSortDescriptor> ActiveSorts => _sorts.ToList();

    /// <summary>The active filter descriptors (a snapshot).</summary>
    public IReadOnlyList<BitDataGridFilterDescriptor> ActiveFilters => _filters.ToList();

    /// <summary>The active group descriptors, in nesting order (a snapshot).</summary>
    public IReadOnlyList<BitDataGridGroupDescriptor> ActiveGroups => _groups.ToList();

    /// <summary>
    /// Programmatically sorts by the given column. <paramref name="direction"/> of
    /// <see cref="BitDataGridSortDirection.None"/> removes the column's sort. When
    /// <paramref name="additive"/> is false (default) all other sorts are cleared first.
    /// </summary>
    public async Task SortByAsync(string columnId, BitDataGridSortDirection direction, bool additive = false)
    {
        if (!_columnsById.ContainsKey(columnId)) return;
        if (!additive) _sorts.Clear();
        else _sorts.RemoveAll(s => s.ColumnId == columnId);

        if (direction != BitDataGridSortDirection.None)
            _sorts.Add(new BitDataGridSortDescriptor { ColumnId = columnId, Direction = direction, Priority = _sorts.Count + 1 });

        Reprioritize();
        await RefreshAsync();
    }

    /// <summary>Removes all active sorts.</summary>
    public async Task ClearSortsAsync()
    {
        if (_sorts.Count == 0) return;
        _sorts.Clear();
        await RefreshAsync();
    }

    /// <summary>Programmatically applies a filter to the given column, replacing any existing filter on it.</summary>
    public Task ApplyFilterAsync(string columnId, BitDataGridFilterOperator op, object? value)
        => _columnsById.TryGetValue(columnId, out var column)
            ? SetFilterAsync(column, op, value)
            : Task.CompletedTask;

    /// <summary>Removes the filter(s) applied to the given column.</summary>
    public async Task ClearFilterAsync(string columnId)
    {
        _filterRaw.Remove(columnId);
        if (_filters.RemoveAll(f => f.ColumnId == columnId) == 0) return;
        _currentPage = 1;
        await RefreshAsync();
    }

    /// <summary>Adds the given column as the next (nested) grouping level. No-op when already grouped
    /// or when the grid's data mode doesn't support grouping (server/infinite/tree/queryable).</summary>
    public async Task GroupByAsync(string columnId)
    {
        if (!GroupingAllowed) return;
        if (!_columnsById.ContainsKey(columnId)) return;
        if (_groups.Any(g => g.ColumnId == columnId)) return;
        _groups.Add(new BitDataGridGroupDescriptor { ColumnId = columnId });
        await RefreshAsync();
    }

    /// <summary>Removes the given column's grouping level.</summary>
    public async Task UngroupAsync(string columnId)
    {
        if (_groups.RemoveAll(g => g.ColumnId == columnId) == 0) return;
        await RefreshAsync();
    }

    /// <summary>
    /// Captures the grid's user-adjustable state (page, page size, sorts, filters, groups and column
    /// layout) as a serializable snapshot. Restore it later with <see cref="ApplyStateAsync"/>.
    /// </summary>
    public BitDataGridState GetState()
    {
        var state = new BitDataGridState
        {
            CurrentPage = _currentPage,
            PageSize = _pageSizeOverride,
            Sorts = _sorts.Select(s => new BitDataGridSortDescriptor { ColumnId = s.ColumnId, Direction = s.Direction, Priority = s.Priority }).ToList(),
            Filters = _filters.Select(f => new BitDataGridFilterDescriptor { ColumnId = f.ColumnId, Operator = f.Operator, Value = f.Value }).ToList(),
            Groups = _groups.Select(g => new BitDataGridGroupDescriptor { ColumnId = g.ColumnId, Direction = g.Direction }).ToList(),
        };
        for (int i = 0; i < _columns.Count; i++)
        {
            var c = _columns[i];
            state.Columns.Add(new BitDataGridColumnState { ColumnId = c.Id, Visible = c.Visible, Width = c.ResizedWidth, Order = i });
        }
        return state;
    }

    /// <summary>
    /// Restores a state snapshot captured by <see cref="GetState"/>. Descriptors and column entries
    /// referencing ids that no longer exist are ignored; columns missing from the snapshot keep their
    /// relative order after the restored ones.
    /// </summary>
    public async Task ApplyStateAsync(BitDataGridState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Columns.Count > 0)
        {
            foreach (var cs in state.Columns)
            {
                if (string.IsNullOrEmpty(cs.ColumnId) || !_columnsById.TryGetValue(cs.ColumnId, out var col)) continue;
                col.Visible = cs.Visible;
                col.ResizedWidth = cs.Width;
            }

            var order = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var cs in state.Columns.OrderBy(c => c.Order))
            {
                if (!string.IsNullOrEmpty(cs.ColumnId)) order.TryAdd(cs.ColumnId, order.Count);
            }
            // Stable sort: restored columns take their snapshot order, unknown ones keep their
            // current relative order at the end.
            var reordered = _columns.OrderBy(c => order.TryGetValue(c.Id, out var i) ? i : int.MaxValue).ToList();
            _columns.Clear();
            _columns.AddRange(reordered);
            InvalidateVisibleColumns();
        }

        _sorts.Clear();
        foreach (var s in state.Sorts.OrderBy(s => s.Priority))
        {
            if (s.ColumnId is not null && _columnsById.ContainsKey(s.ColumnId) && s.Direction != BitDataGridSortDirection.None)
                _sorts.Add(new BitDataGridSortDescriptor { ColumnId = s.ColumnId, Direction = s.Direction, Priority = _sorts.Count + 1 });
        }

        _filters.Clear();
        _filterOps.Clear();
        _filterRaw.Clear();
        foreach (var f in state.Filters)
        {
            if (f.ColumnId is null || !_columnsById.TryGetValue(f.ColumnId, out var filterColumn)) continue;
            // Unspecified is the omitted/invalid operator; such a descriptor carries no criteria, so
            // don't restore it (matching SetFilterAsync, which never creates one).
            if (f.Operator == BitDataGridFilterOperator.Unspecified) continue;
            // A snapshot round-tripped through JSON carries JsonElement values that never equal the
            // CLR values the filter pipeline compares against; re-coerce them to the column's type.
            var value = NormalizeFilterValue(f.Value, filterColumn);
            _filters.Add(new BitDataGridFilterDescriptor { ColumnId = f.ColumnId, Operator = f.Operator, Value = value });
        }
        // Re-sync the filter editors' UI state (operator dropdown + raw text) with the restored
        // descriptors so they show what is actually applied. A column holding several descriptors is
        // a date-Equals half-open range pair, whose dropdown default (Equals) is already correct, so
        // only single-descriptor columns adopt the restored operator.
        foreach (var byColumn in _filters.GroupBy(f => f.ColumnId))
        {
            var first = byColumn.First();
            if (byColumn.Count() == 1) _filterOps[first.ColumnId] = first.Operator;
            _filterRaw[first.ColumnId] = FormatFilterRaw(first.Value);
        }

        _groups.Clear();
        if (GroupingAllowed)
        {
            // Skipped entirely in modes that don't support grouping, so a snapshot captured in a
            // groupable configuration can't reintroduce groups after e.g. OnRead was wired up.
            foreach (var g in state.Groups)
            {
                if (g.ColumnId is not null && _columnsById.ContainsKey(g.ColumnId))
                    _groups.Add(new BitDataGridGroupDescriptor { ColumnId = g.ColumnId, Direction = g.Direction });
            }
        }

        _pageSizeOverride = state.PageSize is { } ps ? Math.Max(1, ps) : null;
        _effectivePageSize = Pageable ? Math.Max(1, _pageSizeOverride ?? PageSize) : int.MaxValue;
        _currentPage = Math.Max(1, state.CurrentPage);

        // RefreshAsync clamps the page against the restored view's TotalPages.
        await RefreshAsync();
    }

    // ------------------------------------------------------- Column chooser
    internal void ToggleColumnChooser() { _showColumnChooserPanel = !_showColumnChooserPanel; StateHasChanged(); }
    internal void SetColumnVisibilityAsync(BitDataGridColumn<TItem> column, bool visible)
    {
        // Column visibility is a layout-only change, so just re-render. Calling RefreshAsync here would
        // needlessly re-run OnRead/ResetInfiniteAsync (requerying or clearing loaded data) for what is
        // purely a column-chooser toggle.
        column.Visible = visible;
        InvalidateVisibleColumns();
        StateHasChanged();
    }

    /// <summary>
    /// Called by a column when its <c>Visible</c> parameter changes from the parent after registration,
    /// so the cached visible-column list doesn't go stale.
    /// </summary>
    internal void NotifyColumnVisibilityChanged()
    {
        InvalidateVisibleColumns();
        InvokeAsync(StateHasChanged);
    }

    // ----------------------------------------------------------- Identity
    private object GetKey(TItem item) => KeyField?.Invoke(item) ?? item!;
    private bool KeyEquals(TItem a, TItem b)
        => KeyField is not null ? Equals(KeyField(a), KeyField(b)) : EqualityComparer<TItem>.Default.Equals(a, b);

    /// <summary>Finds the index of <paramref name="item"/> in <paramref name="list"/> using the grid's
    /// key-based identity (<see cref="KeyEquals"/>) so re-materialized rows still resolve when KeyField is set.</summary>
    private int IndexOfByKey(IList<TItem> list, TItem item)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (KeyEquals(list[i], item)) return i;
        }
        return -1;
    }

    // ------------------------------------------------------------- ARIA
    /// <summary>Rebuilds the key → absolute-row-position map backing <see cref="AriaRowIndex"/>.
    /// <paramref name="offset"/> is the 0-based dataset position of the first row in <paramref name="rows"/>
    /// (the page/window start in paged, server and virtualized modes).</summary>
    private void RebuildRowIndexMap(IReadOnlyList<TItem> rows, int offset)
    {
        var map = new Dictionary<object, int>(rows.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            map.TryAdd(GetKey(rows[i]), offset + i);
        }
        _rowIndexByKey = map;
    }

    /// <summary>Whether the filter editor row renders below the header row.</summary>
    internal bool HasFilterRow => !IsTreeMode && (Filterable || VisibleColumns.Any(ColumnFilterable));

    /// <summary>The number of rows the header rowgroup renders (group-header, header and filter rows),
    /// so aria-rowindex forms one consistent sequence across header and data rows.</summary>
    internal int HeaderRowCount => ShowHeader ? (HasColumnGroups ? 1 : 0) + 1 + (HasFilterRow ? 1 : 0) : 0;

    /// <summary>The 1-based aria-rowindex for a data row (accounting for all header rows), or null when
    /// unknown - a null renders no attribute.</summary>
    internal int? AriaRowIndex(TItem item)
    {
        if (_rowIndexByKey is null || !_rowIndexByKey.TryGetValue(GetKey(item), out var index)) return null;
        return index + 1 + HeaderRowCount;
    }

    /// <summary>The row's absolute 0-based dataset position (its data-ri attribute), or null when
    /// unknown. Used by the pointer-based (touch) reorder to identify rows across JS interop.</summary>
    internal int? RowDataIndex(TItem item)
        => _rowIndexByKey is not null && _rowIndexByKey.TryGetValue(GetKey(item), out var index) ? index : null;

    /// <summary>The 1-based aria-colindex of a data column's cells (after any special leading columns).</summary>
    internal int AriaColIndex(int colIndex) => colIndex + 1 + SpecialColumnCount;

    internal int SpecialColumnCount => (HasReorderColumn ? 1 : 0) + (HasDetailColumn ? 1 : 0) + (HasSelectColumn ? 1 : 0);

    /// <summary>The 1-based aria-colindex of the trailing command (Actions) column.</summary>
    internal int AriaCommandColIndex => TotalColumnSpan;

    private void Announce(string message)
    {
        // Assigning the same string twice would not re-trigger some screen readers; the view refresh
        // that accompanies every announcement re-renders the live region content anyway.
        _srAnnouncement = message;
    }

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
    /// <summary>
    /// Generates the current (filtered/sorted) data as CSV and triggers a client-side download.
    /// In server mode this fetches <b>all</b> matching rows through <see cref="OnRead"/> (not just the
    /// current page). Invoked on demand from the toolbar button so the CSV is built only when the user
    /// asks for it, rather than being regenerated into a DOM attribute on every render.
    /// </summary>
    public async Task ExportCsvAsync()
    {
        var csv = await ToCsvAsync();
        try
        {
            await JS.InvokeVoidAsync("BitBlazorUI.DataGrid.download", "export.csv", csv, "text/csv;charset=utf-8");
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }

    /// <summary>
    /// Builds a CSV string of the full (filtered/sorted) dataset in every data mode. Server and
    /// infinite-scrolling modes issue an <see cref="OnRead"/>/<see cref="OnLoadMore"/> request with
    /// no paging (<c>Take = null</c>, meaning "all rows") and the active sorts/filters, and tree
    /// mode includes collapsed branches, so the export contains every matching row rather than the
    /// currently rendered ones.
    /// </summary>
    public async Task<string> ToCsvAsync() => BuildCsv(await GetExportRowsAsync());

    /// <summary>Generates the full (filtered/sorted) dataset as an Excel workbook (.xlsx). Like
    /// <see cref="ToCsvAsync"/>, this covers all matching rows in every data mode - server and
    /// infinite-scrolling modes fetch them through <see cref="OnRead"/>/<see cref="OnLoadMore"/>.
    /// The workbook mirrors the grid's layout: a bold frozen header row, the leading
    /// <see cref="BitDataGridColumn{TItem}.Frozen"/> columns as a freeze pane, ColSpan cells as
    /// merged regions, and the grid's column widths. With <see cref="ExcelExportStyled"/> it also
    /// carries the grid's rendered theme (colors, striping, borders, fonts).</summary>
    public async Task<byte[]> ToExcelAsync()
    {
        var rows = await GetExportRowsAsync();
        var cols = VisibleColumns.Where(c => c.HasField).ToList();

        // Excel freeze panes can only pin a leading run of columns, so count consecutive Frozen
        // exported columns from the start; a Frozen column further in (or FrozenEnd) has no
        // workbook equivalent and exports unpinned.
        var frozen = 0;
        while (frozen < cols.Count && cols[frozen].Frozen) frozen++;

        BitDataGridExcelStyle? style = null;
        if (ExcelExportStyled)
        {
            try
            {
                style = await JS.InvokeAsync<BitDataGridExcelStyle?>("BitBlazorUI.DataGrid.getExportStyles", _rootRef);
                // Vertical borders come from the grid's own Bordered mode rather than a sampled edge:
                // cell borders are only rendered when Bordered is set, and the sampled row-separator
                // color already covers the shared border color.
                if (style is not null) style.VerticalBorders = Bordered;
            }
            catch (JSException) { }
            catch (JSDisconnectedException) { }
            catch (InvalidOperationException) { } // prerendering: JS interop not available yet
        }

        var widths = cols.Select(ColumnPixelWidth).ToList();
        return BitDataGridExcelWriter.Write(rows, cols, frozen, widths, style);
    }

    /// <summary>Generates the current dataset as an .xlsx workbook and triggers a client-side download.</summary>
    public async Task ExportExcelAsync()
    {
        var bytes = await ToExcelAsync();
        try
        {
            await JS.InvokeVoidAsync("BitBlazorUI.DataGrid.downloadBase64", "export.xlsx",
                Convert.ToBase64String(bytes),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }

    /// <summary>The rows an export should cover: everything matching the active sorts/filters,
    /// never just what happens to be rendered. Server and infinite-scrolling modes fetch the full
    /// matching set through their provider (<see cref="OnRead"/>/<see cref="OnLoadMore"/> with
    /// <c>Take = null</c>), queryable mode re-runs the translated pipeline without the page window,
    /// and tree mode walks the whole tree including collapsed branches (lazy trees are limited to
    /// the children already loaded - fetching the rest could be unbounded).</summary>
    private async Task<IReadOnlyList<TItem>> GetExportRowsAsync()
    {
        if (IsServerMode)
        {
            var result = await OnRead!(BuildExportReadRequest());
            return result.Items;
        }

        if (IsInfiniteMode)
        {
            var result = await OnLoadMore!(BuildExportReadRequest());
            return result.Items;
        }

        return GetSyncExportRows();
    }

    /// <summary>The export rows resolvable without a provider round-trip. Tree mode flattens the
    /// entire tree in display order (per-sibling sorting applied) regardless of expand/collapse
    /// state - the rendered view only contains expanded rows. Queryable mode re-runs the translated
    /// filter/sort pipeline without the page window so the provider (e.g. a database) streams the
    /// full matching set. Server/infinite modes fall back to the rows already loaded - only their
    /// async providers can supply more (see <see cref="GetExportRowsAsync"/>).</summary>
    private IReadOnlyList<TItem> GetSyncExportRows()
    {
        if (IsServerMode || IsInfiniteMode) return _pageItems;

        if (IsTreeMode)
        {
            var all = new List<TItem>();
            WalkAll(Items ?? Enumerable.Empty<TItem>());
            return all;

            void WalkAll(IEnumerable<TItem> siblings)
            {
                IEnumerable<TItem> sorted = _sorts.Count > 0
                    ? BitDataGridDataProcessor.Sort(siblings.ToList(), _sorts, _columnsById)
                    : siblings;
                foreach (var item in sorted)
                {
                    all.Add(item);
                    if (ResolveTreeChildren(item) is { Count: > 0 } children) WalkAll(children);
                }
            }
        }

        if (IsQueryableMode && Items is IQueryable<TItem> queryable)
            return BitDataGridQueryableProcessor.Apply(queryable, _filters, _sorts, _columnsById).ToList();

        return _view;
    }

    /// <summary>The provider request an export issues: no page window (<c>Take = null</c> means
    /// "all rows") with the active sorts/filters, so the provider streams every matching row.
    /// Deliberately not using ResetLoadCancellation: an export must not cancel (or be treated as
    /// superseding) the grid's own in-flight data load.</summary>
    private BitDataGridReadRequest BuildExportReadRequest() => new()
    {
        Skip = 0,
        Take = null,
        Sorts = _sorts.Where(s => s.Direction != BitDataGridSortDirection.None).OrderBy(s => s.Priority).ToList(),
        Filters = _filters.ToList(),
        Groups = _groups.ToList(),
        CancellationToken = CancellationToken.None
    };

    /// <summary>Builds a CSV string of the full (filtered/sorted) dataset without going async:
    /// tree mode includes collapsed branches and queryable mode covers all pages. Server and
    /// infinite-scrolling modes are the exception - fetching beyond the loaded rows requires their
    /// async provider, so they cover only the current page/loaded batches; use
    /// <see cref="ToCsvAsync"/> there for all rows.</summary>
    public string ToCsv() => BuildCsv(GetSyncExportRows());

    private string BuildCsv(IReadOnlyList<TItem> rows)
    {
        var cols = VisibleColumns.Where(c => c.HasField).ToList();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(string.Join(",", cols.Select(c => Escape(c.DisplayTitle))));
        foreach (var item in rows)
            sb.AppendLine(string.Join(",", cols.Select(c => Escape(c.GetFormattedValue(item)))));
        return sb.ToString();

        static string Escape(string v)
        {
            // Neutralise CSV formula injection: spreadsheet apps may execute a cell whose text begins
            // with =, +, - or @ as a formula. Leading whitespace can be used to bypass a naive first-char
            // check (the app trims it before evaluating), so test the trimmed value but keep the original
            // (whitespace included) when prefixing with a single quote to force it to be read as text.
            // Plain numbers are exempt: "-5" or "+3.2" is data, not a formula, and prefixing it would
            // corrupt every negative value in numeric columns (spreadsheets parse it as a number anyway).
            var trimmed = v.TrimStart(' ', '\t', '\n', '\r');
            if (trimmed.Length > 0 && (trimmed[0] is '=' or '+' or '-' or '@')
                && !double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                && !double.TryParse(trimmed, NumberStyles.Any, CultureInfo.CurrentCulture, out _))
            {
                v = "'" + v;
            }

            return v.Contains(',') || v.Contains('"') || v.Contains('\n') || v.Contains('\r')
                ? "\"" + v.Replace("\"", "\"\"") + "\""
                : v;
        }
    }

    // ----------------------------------------------------- Layout helpers
    internal bool HasSelectColumn => SelectionMode == BitDataGridSelectionMode.Multiple;
    internal bool HasDetailColumn => DetailTemplate is not null;
    internal bool HasCommandColumn => Editable;
    internal bool HasReorderColumn => RowReorderEnabled;

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

    /// <summary>
    /// Resolves the height (in px) for a given row, honouring <see cref="RowHeightSelector"/>.
    /// While <see cref="Virtualize"/> is enabled the selector is ignored and the uniform
    /// <see cref="RowHeight"/> is always returned, because virtualization requires a constant row height.
    /// </summary>
    internal float ResolveRowHeight(TItem item) => Virtualize ? RowHeight : (RowHeightSelector?.Invoke(item) ?? RowHeight);

    /// <summary>Builds the CSS grid template-columns value for the whole row layout.</summary>
    private string BuildGridTemplate()
    {
        var parts = new List<string>();
        if (HasReorderColumn) parts.Add($"{ReorderColWidth.ToString(CultureInfo.InvariantCulture)}px");
        if (HasDetailColumn) parts.Add($"{DetailColWidth.ToString(CultureInfo.InvariantCulture)}px");
        if (HasSelectColumn) parts.Add($"{SelectColWidth.ToString(CultureInfo.InvariantCulture)}px");

        var virtualized = ColumnVirtualizationActive;
        foreach (var slot in ColumnSlots)
        {
            if (slot.Column is null)
            {
                parts.Add($"{slot.SpacerWidth.ToString(CultureInfo.InvariantCulture)}px");
            }
            else if (virtualized)
            {
                // While virtualizing, the layout must use the same px widths the slot math used,
                // otherwise the spacers and the real columns would drift apart.
                parts.Add($"{ColumnPixelWidth(slot.Column).ToString(CultureInfo.InvariantCulture)}px");
            }
            else
            {
                parts.Add(ColumnWidthToken(slot.Column));
            }
        }

        if (HasCommandColumn) parts.Add("minmax(150px, max-content)");
        return string.Join(" ", parts);
    }

    private int TotalColumnSpan =>
        VisibleColumns.Count + (HasReorderColumn ? 1 : 0) + (HasDetailColumn ? 1 : 0) + (HasSelectColumn ? 1 : 0) + (HasCommandColumn ? 1 : 0);

    private string HeaderCellClass(BitDataGridColumn<TItem> column)
    {
        var c = "bit-dtg-hcell " + AlignClass(column.Align);
        if (IsSticky(column)) c += " bit-dtg-sticky";
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

    /// <summary>Sticky end-edge offset (in px) for an end-frozen column: the total width of the
    /// end-frozen columns that render after it (they stack from the end edge inward).</summary>
    internal double FrozenEndOffset(BitDataGridColumn<TItem> column)
    {
        double offset = 0;
        var cols = VisibleColumns;
        for (int i = cols.Count - 1; i >= 0; i--)
        {
            if (cols[i] == column) break;
            if (cols[i].FrozenEnd && !cols[i].Frozen) offset += ColumnPixelWidth(cols[i]);
        }
        return offset;
    }

    internal string FrozenStyle(BitDataGridColumn<TItem> column)
    {
        if (column.Frozen)
        {
            var edge = Direction == BitDir.Rtl ? "right" : "left";
            return $"{edge}:{FrozenOffset(column).ToString(CultureInfo.InvariantCulture)}px;";
        }
        if (column.FrozenEnd)
        {
            var edge = Direction == BitDir.Rtl ? "left" : "right";
            return $"{edge}:{FrozenEndOffset(column).ToString(CultureInfo.InvariantCulture)}px;";
        }
        return string.Empty;
    }

    /// <summary>True when the column is pinned to either edge (renders with the sticky class).</summary>
    internal static bool IsSticky(BitDataGridColumn<TItem> column) => column.Frozen || column.FrozenEnd;

    private string AggregateLabel(BitDataGridAggregateResult agg) => agg.Type switch
    {
        BitDataGridAggregateType.Sum => string.Format(Strings.AggregateSumFormat, agg.FormattedValue),
        BitDataGridAggregateType.Average => string.Format(Strings.AggregateAverageFormat, agg.FormattedValue),
        BitDataGridAggregateType.Count => string.Format(Strings.AggregateCountFormat, agg.FormattedValue),
        BitDataGridAggregateType.Min => string.Format(Strings.AggregateMinFormat, agg.FormattedValue),
        BitDataGridAggregateType.Max => string.Format(Strings.AggregateMaxFormat, agg.FormattedValue),
        _ => agg.FormattedValue
    };
}

