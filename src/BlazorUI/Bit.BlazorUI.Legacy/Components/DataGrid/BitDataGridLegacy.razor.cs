// a fork from the Blazor QuickGrid at https://github.com/dotnet/aspnetcore/tree/main/src/Components/QuickGrid

namespace Bit.BlazorUI.Legacy;

/// <summary>
/// BitDataGridLegacy is a robust way to display an information-rich collection of items, and allow people to sort, and filter the content.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
public partial class BitDataGridLegacy<TGridItem> : IAsyncDisposable
{
    private bool _disposed;
    private int _ariaBodyRowCount;
    private ElementReference _tableReference;
    private Virtualize<(int, TGridItem)>? _virtualizeComponent;
    private ICollection<TGridItem> _currentNonVirtualizedViewItems = Array.Empty<TGridItem>();

    // IQueryable only exposes synchronous query APIs. IAsyncQueryExecutor is an adapter that lets us invoke any
    // async query APIs that might be available. We have built-in support for using EF Core's async query APIs.
    private IAsyncQueryExecutor? _asyncQueryExecutor;

    // We cascade the InternalGridContext to descendants, which in turn call it to add themselves to _columns
    // This happens on every render so that the column list can be updated dynamically
    private InternalGridContext<TGridItem> _internalGridContext;
    private List<BitDataGridLegacyColumnBase<TGridItem>> _columns;
    private bool _collectingColumns; // Columns might re-render themselves arbitrarily. We only want to capture them at a defined time.

    // Tracking state for options and sorting
    private BitDataGridLegacyColumnBase<TGridItem>? _displayOptionsForColumn;
    private BitDataGridLegacyColumnBase<TGridItem>? _sortByColumn;
    private bool _sortByAscending;
    private bool _checkColumnOptionsPosition;
    // Set when column recollection drops the active sort column; triggers a data refresh after render
    // so the grid query stays in sync with the (now changed) header sort state.
    private bool _queueSortReconciliationRefresh;
    // Tracks whether columns have been collected at least once, and the sort column captured at the
    // start of a collection pass, so a *new* default sort applied during a later recollection can also
    // queue a refresh (the very first collection already loads data via ColumnsFirstCollected).
    private bool _columnsCollectedOnce;
    private BitDataGridLegacyColumnBase<TGridItem>? _sortByColumnBeforeCollect;
    // Captures the first default-sort column (and its direction) discovered during the current
    // collection pass, so FinishCollectingColumns can adopt it when the previously active sort column
    // is no longer present after recollection instead of clearing sorting outright.
    private BitDataGridLegacyColumnBase<TGridItem>? _defaultSortColumnDuringCollect;
    private BitDataGridLegacySortDirection? _defaultSortDirectionDuringCollect;

    // The associated ES6 module, which uses document-level event listeners
    //private IJSObjectReference? _jsModule;
    private IJSObjectReference? _jsEventDisposable;

    // Caches of method->delegate conversions
    private readonly RenderFragment _renderColumnHeaders;
    private readonly RenderFragment _renderNonVirtualizedRows;

    // We try to minimize the number of times we query the items provider, since queries may be expensive
    // We only re-query when the developer calls RefreshDataAsync, or if we know something's changed, such
    // as sort order, the pagination state, or the data source itself. These fields help us detect when
    // things have changed, and to discard earlier load attempts that were superseded.
    private int? _lastRefreshedPaginationStateHash;
    private object? _lastAssignedItemsOrProvider;
    // Tracks the Virtualize value the data was last refreshed under, so a flip between virtualized and
    // non-virtualized rendering forces a re-query (otherwise the stale non-virtualized view would linger).
    private bool? _lastRefreshedVirtualize;
    private CancellationTokenSource? _pendingDataLoadCancellationTokenSource;
    // Hash of the collected column set the resize handles were last bound against, so we only rebind
    // when the columns actually change rather than on every render.
    private int? _lastInitColumnsHash;
    // Tracks the ResizableColumns value the resize handles were last bound against, so toggling the
    // feature on/off (without otherwise changing the columns) still rebinds the new/removed handles.
    private bool _lastResizableColumns;

    // If the PaginationState mutates, it raises this event. We use it to trigger a re-render.
    private readonly EventCallbackSubscriber<BitDataGridLegacyPaginationState> _currentPageItemsChanged;



    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private IServiceProvider _services { get; set; } = default!;



    /// <summary>
    /// Constructs an instance of <see cref="BitDataGridLegacy{TGridItem}"/>.
    /// </summary>
    public BitDataGridLegacy()
    {
        _columns = new();
        _internalGridContext = new(this);
        _currentPageItemsChanged = new(EventCallback.Factory.Create<BitDataGridLegacyPaginationState>(this, RefreshDataCoreAsync));
        _renderColumnHeaders = RenderColumnHeaders;
        _renderNonVirtualizedRows = RenderNonVirtualizedRows;

        // As a special case, we don't issue the first data load request until we've collected the initial set of columns
        // This is so we can apply default sort order (or any future per-column options) before loading data
        // We use EventCallbackSubscriber to safely hook this async operation into the synchronous rendering flow
        var columnsFirstCollectedSubscriber = new EventCallbackSubscriber<object?>(
            EventCallback.Factory.Create<object?>(this, RefreshDataCoreAsync));
        columnsFirstCollectedSubscriber.SubscribeOrMove(_internalGridContext.ColumnsFirstCollected);
    }

    private bool IsLoading => _pendingDataLoadCancellationTokenSource is not null;



    /// <summary>
    /// Defines the child components of this instance. For example, you may define columns by adding
    /// components derived from the <see cref="BitDataGridLegacyColumnBase{TGridItem}"/> base class.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// An optional CSS class name. If given, this will be included in the class attribute of the rendered table.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Alias of the ChildContent parameter.
    /// </summary>
    [Parameter] public RenderFragment? Columns { get; set; }

    /// <summary>
    /// Optionally defines a value for @key on each rendered row. Typically this should be used to specify a
    /// unique identifier, such as a primary key value, for each data item.
    ///
    /// This allows the grid to preserve the association between row elements and data items based on their
    /// unique identifiers, even when the TGridItem instances are replaced by new copies (for
    /// example, after a new query against the underlying data store).
    ///
    /// If not set, the @key will be the TGridItem instance itself.
    /// </summary>
    [Parameter] public Func<TGridItem, object> ItemKey { get; set; } = x => x!;

    /// <summary>
    /// A queryable source of data for the grid.
    ///
    /// This could be in-memory data converted to queryable using the
    /// <see cref="System.Linq.Queryable.AsQueryable(System.Collections.IEnumerable)"/> extension method,
    /// or an EntityFramework DataSet or an <see cref="IQueryable"/> derived from it.
    ///
    /// You should supply either <see cref="Items"/> or <see cref="ItemsProvider"/>, but not both.
    /// </summary>
    [Parameter] public IQueryable<TGridItem>? Items { get; set; }

    /// <summary>
    /// This is applicable only when using <see cref="Virtualize"/>. It defines an expected height in pixels for
    /// each row, allowing the virtualization mechanism to fetch the correct number of items to match the display
    /// size and to ensure accurate scrolling.
    /// </summary>
    [Parameter] public float ItemSize { get; set; } = 50;

    /// <summary>
    /// A callback that supplies data for the grid.
    ///
    /// You should supply either <see cref="Items"/> or <see cref="ItemsProvider"/>, but not both.
    /// </summary>
    [Parameter] public BitDataGridItemsProvider<TGridItem>? ItemsProvider { get; set; }

    /// <summary>
    /// The custom template to render while loading the new items.
    /// It is honored in both the non-virtualized and the virtualized (<see cref="Virtualize"/>) paths:
    /// while a data refresh is in flight the template is shown instead of the rows, and the
    /// <see cref="Virtualize{TItem}"/> placeholder flow still covers rows streamed in during scrolling.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Optionally links this <see cref="BitDataGridLegacy{TGridItem}"/> instance with a <see cref="BitDataGridLegacyPaginationState"/> model,
    /// causing the grid to fetch and render only the current page of data.
    ///
    /// This is normally used in conjunction with a <see cref="BitDataGridLegacyPaginator"/> component or some other UI logic
    /// that displays and updates the supplied <see cref="BitDataGridLegacyPaginationState"/> instance.
    /// </summary>
    [Parameter] public BitDataGridLegacyPaginationState? Pagination { get; set; }

    /// <summary>
    /// If true, renders draggable handles around the column headers, allowing the user to resize the columns
    /// manually. Size changes are not persisted.
    /// </summary>
    [Parameter] public bool ResizableColumns { get; set; }

    /// <summary>
    /// The CSS class of all rows of the data grid.
    /// </summary>
    [Parameter] public string? RowClass { get; set; }

    /// <summary>
    /// The function to generate the CSS class of each row of the data grid.
    /// </summary>
    [Parameter] public Func<TGridItem, string>? RowClassSelector { get; set; }

    /// <summary>
    /// The CSS style of all row of the data grid.
    /// </summary>
    [Parameter] public string? RowStyle { get; set; }

    /// <summary>
    /// The function to generate the CSS style of each row of the data grid.
    /// </summary>
    [Parameter] public Func<TGridItem, string>? RowStyleSelector { get; set; }

    /// <summary>
    /// Optional template to customize row rendering. Receives <see cref="BitDataGridLegacyRowTemplateArgs{TGridItem}"/> with
    /// <see cref="BitDataGridLegacyRowTemplateArgs{T}.OriginalRow"/> set to the default row content; call it to render the original cells or replace with custom content.
    /// </summary>
    [Parameter] public RenderFragment<BitDataGridLegacyRowTemplateArgs<TGridItem>>? RowTemplate { get; set; }

    /// <summary>
    /// A theme name, with default value "default". This affects which styling rules match the table.
    /// </summary>
    [Parameter] public string? Theme { get; set; } = "default";

    /// <summary>
    /// If true, the grid will be rendered with virtualization. This is normally used in conjunction with
    /// scrolling and causes the grid to fetch and render only the data around the current scroll viewport.
    /// This can greatly improve the performance when scrolling through large data sets.
    ///
    /// If you use <see cref="Virtualize"/>, you should supply a value for <see cref="ItemSize"/> and must
    /// ensure that every row renders with the same constant height.
    ///
    /// Generally it's preferable not to use <see cref="Virtualize"/> if the amount of data being rendered
    /// is small or if you are using pagination.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }



    /// <summary>
    /// Sets the grid's current sort column to the specified <paramref name="column"/>.
    /// </summary>
    /// <param name="column">The column that defines the new sort order.</param>
    /// <param name="direction">The direction of sorting. If the value is <see cref="BitDataGridLegacySortDirection.Auto"/>, then it will toggle the direction on each call.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    public Task SortByColumnAsync(BitDataGridLegacyColumnBase<TGridItem> column, BitDataGridLegacySortDirection direction = BitDataGridLegacySortDirection.Auto)
    {
        _sortByAscending = direction switch
        {
            BitDataGridLegacySortDirection.Ascending => true,
            BitDataGridLegacySortDirection.Descending => false,
            BitDataGridLegacySortDirection.Auto => _sortByColumn == column ? !_sortByAscending : true,
            _ => throw new NotSupportedException($"Unknown sort direction {direction}"),
        };

        _sortByColumn = column;

        StateHasChanged(); // We want to see the updated sort order in the header, even before the data query is completed
        return RefreshDataAsync();
    }

    /// <summary>
    /// Displays the <see cref="BitDataGridLegacyColumnBase{TGridItem}.ColumnOptions"/> UI for the specified column, closing any other column
    /// options UI that was previously displayed.
    /// </summary>
    /// <param name="column">The column whose options are to be displayed, if any are available.</param>
    public void ShowColumnOptions(BitDataGridLegacyColumnBase<TGridItem> column)
    {
        _displayOptionsForColumn = column;
        _checkColumnOptionsPosition = true; // Triggers a call to JS to position the options element, apply autofocus, and any other setup
        StateHasChanged();
    }

    /// <summary>
    /// Instructs the grid to re-fetch and render the current data from the supplied data source
    /// (either <see cref="Items"/> or <see cref="ItemsProvider"/>).
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the completion of the operation.</returns>
    public async Task RefreshDataAsync()
    {
        try
        {
            await RefreshDataCoreAsync();
        }
        finally
        {
            // Always rerender after the core refresh settles, even when it throws, so the grid
            // doesn't get stuck showing the loading state if the caller handles the exception.
            StateHasChanged();
        }
    }



    // Invoked by descendant columns at a special time during rendering
    internal void AddColumn(BitDataGridLegacyColumnBase<TGridItem> column, BitDataGridLegacySortDirection? isDefaultSortDirection)
    {
        if (_collectingColumns)
        {
            _columns.Add(column);

            if (_sortByColumn is null && isDefaultSortDirection.HasValue)
            {
                _sortByColumn = column;
                _sortByAscending = isDefaultSortDirection.Value != BitDataGridLegacySortDirection.Descending;
            }

            // Remember the first default-sort column collected in this pass even when a (possibly stale)
            // _sortByColumn is still set. If that prior column turns out to have been dropped, this lets
            // FinishCollectingColumns switch to the newly declared default instead of clearing sorting.
            if (isDefaultSortDirection.HasValue && _defaultSortColumnDuringCollect is null)
            {
                _defaultSortColumnDuringCollect = column;
                _defaultSortDirectionDuringCollect = isDefaultSortDirection.Value;
            }
        }
    }



    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        // The associated pagination state may have been added/removed/replaced
        _currentPageItemsChanged.SubscribeOrMove(Pagination?.CurrentPageItemsChanged);

        if (Items is not null && ItemsProvider is not null)
        {
            throw new InvalidOperationException($"BitDataGridLegacy requires one of {nameof(Items)} or {nameof(ItemsProvider)}, but both were specified.");
        }

        // Perform a re-query only if the data source or something else has changed
        var _newItemsOrItemsProvider = Items ?? (object?)ItemsProvider;
        var dataSourceHasChanged = _newItemsOrItemsProvider != _lastAssignedItemsOrProvider;
        if (dataSourceHasChanged)
        {
            _lastAssignedItemsOrProvider = _newItemsOrItemsProvider;
            _asyncQueryExecutor = AsyncQueryExecutorSupplier.GetAsyncQueryExecutor(_services, Items);
        }

        var mustRefreshData = dataSourceHasChanged
            || (_lastRefreshedVirtualize != Virtualize)
            || (ComputePaginationStateHash() != _lastRefreshedPaginationStateHash);

        // We don't want to trigger the first data load until we've collected the initial set of columns,
        // because they might perform some action like setting the default sort order, so it would be wasteful
        // to have to re-query immediately
        if (_columns.Count > 0 && mustRefreshData)
        {
            return RefreshDataCoreAsync();
        }

        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsEventDisposable = await _js.BitDataGridInit(_tableReference);
            _lastInitColumnsHash = ComputeColumnsHash();
            _lastResizableColumns = ResizableColumns;
        }
        else if (ResizableColumns)
        {
            // The resize handles (.bit-qkg-drg) are bound per-element by init. When the column set
            // changes, the header re-renders with fresh handles that have no listeners, so rebind them.
            // The handles also appear/disappear when ResizableColumns itself is toggled, so rebind on
            // that transition too. Re-running init re-adds the document-level listeners, so stop the
            // previous registration first to avoid leaking duplicate handlers. Unchanged renders are skipped.
            var hash = ComputeColumnsHash();
            if (hash != _lastInitColumnsHash || !_lastResizableColumns)
            {
                _lastInitColumnsHash = hash;
                _lastResizableColumns = true;
                await StopJsEventsAsync();
                _jsEventDisposable = await _js.BitDataGridInit(_tableReference);
            }
        }
        else if (_lastResizableColumns)
        {
            // ResizableColumns was just turned off; the drag handles are gone. Rebind so the
            // document-level listeners are refreshed and the stale handle registration is dropped.
            _lastResizableColumns = false;
            await StopJsEventsAsync();
            _jsEventDisposable = await _js.BitDataGridInit(_tableReference);
        }

        if (_checkColumnOptionsPosition && _displayOptionsForColumn is not null)
        {
            _checkColumnOptionsPosition = false;
            await _js.BitDataGridCheckColumnOptionsPosition(_tableReference);
        }

        if (_queueSortReconciliationRefresh)
        {
            // Column recollection dropped the active sort column; re-query so the grid data matches
            // the header state that no longer shows that sort.
            _queueSortReconciliationRefresh = false;
            await RefreshDataAsync();
        }
    }

    private int ComputeColumnsHash()
    {
        var hash = new HashCode();
        foreach (var col in _columns) hash.Add(col);
        return hash.ToHashCode();
    }

    // Only the requested slice inputs (page index + page size) should trigger a re-query. The pagination
    // state's own GetHashCode also folds in TotalItemCount, which the grid mutates after every successful
    // load, so using it here would make a completed fetch look like a fresh pagination change and kick off
    // an immediate redundant second query.
    private int? ComputePaginationStateHash()
        => Pagination is null ? null : HashCode.Combine(Pagination.CurrentPageIndex, Pagination.ItemsPerPage);

    private async Task StopJsEventsAsync()
    {
        try
        {
            if (_jsEventDisposable is not null)
            {
                await _jsEventDisposable.InvokeVoidAsync("stop");
                await _jsEventDisposable.DisposeAsync();
                _jsEventDisposable = null;
            }
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }



    private void StartCollectingColumns()
    {
        _sortByColumnBeforeCollect = _sortByColumn;
        _defaultSortColumnDuringCollect = null;
        _defaultSortDirectionDuringCollect = null;
        _columns.Clear();
        _collectingColumns = true;
    }

    private void FinishCollectingColumns()
    {
        _collectingColumns = false;

        // The column that drove the last data load may no longer be among the freshly collected
        // columns (it was removed or replaced). Leaving _sortByColumn pointing at a dropped column
        // desyncs the data query from the header, so reconcile it and queue a refresh so the grid
        // re-queries. The refresh is run from OnAfterRenderAsync because this runs mid-render.
        if (_sortByColumn is not null && _columns.Contains(_sortByColumn) is false)
        {
            if (_defaultSortColumnDuringCollect is not null)
            {
                // A newly collected column declares a default sort, so adopt it instead of clearing
                // sorting; otherwise a dynamic column swap would drop the intended default order.
                _sortByColumn = _defaultSortColumnDuringCollect;
                _sortByAscending = _defaultSortDirectionDuringCollect!.Value != BitDataGridLegacySortDirection.Descending;
            }
            else
            {
                _sortByColumn = null;
                _sortByAscending = false;
            }
            _queueSortReconciliationRefresh = true;
        }
        else if (_columnsCollectedOnce && _sortByColumnBeforeCollect is null && _sortByColumn is not null)
        {
            // A recollection assigned a brand-new default sort (none was active before). The initial
            // collection already loads data via ColumnsFirstCollected, but later recollections do not,
            // so queue a refresh to re-query in the newly defaulted sort order and keep header/data in sync.
            _queueSortReconciliationRefresh = true;
        }

        _columnsCollectedOnce = true;
    }

    // Same as RefreshDataAsync, except without forcing a re-render. We use this from OnParametersSetAsync
    // because in that case there's going to be a re-render anyway.
    private async Task RefreshDataCoreAsync()
    {
        // Record the Virtualize mode this refresh runs under so every refresh path keeps the marker
        // current: the initial column-driven load (via ColumnsFirstCollected), RefreshDataAsync, and the
        // parameter-change trigger in OnParametersSetAsync all funnel through here. Updating it only in
        // OnParametersSetAsync would leave it stale after those other paths, so a later parameter set
        // could wrongly (or never) detect a virtualized/non-virtualized flip.
        _lastRefreshedVirtualize = Virtualize;

        // Snapshot the requested pagination slice up front so both the virtualized and non-virtualized
        // paths record it. Doing this only in the non-virtualized branch (and relying on
        // ProvideVirtualizedItems otherwise) leaves the marker stale when Virtualize is on but its child
        // hasn't requested items yet (e.g. first render / right after toggling virtualization). The next
        // OnParametersSetAsync would then see ComputePaginationStateHash() != _lastRefreshedPaginationStateHash
        // and fire a duplicate initial query for the same slice.
        _lastRefreshedPaginationStateHash = ComputePaginationStateHash();

        // Move into a "loading" state, cancelling any earlier-but-still-pending load. Do NOT dispose
        // the previous source here: the load that owns it may still be in flight and holding its token
        // (e.g. registered on it), so disposing now could surface an ObjectDisposedException instead of
        // the expected OperationCanceledException. Each load disposes its own source in its finally block
        // once it has finished using it (whether or not it is still the current one), so a superseded
        // source is disposed by its owning load rather than leaked to the GC.
        _pendingDataLoadCancellationTokenSource?.Cancel();
        var thisLoadCts = _pendingDataLoadCancellationTokenSource = new CancellationTokenSource();

        // Render now so the loading state (IsLoading / LoadingTemplate) becomes visible as soon as the
        // refresh starts, instead of only after the async load below completes.
        StateHasChanged();

        if (Virtualize)
        {
            // If we're using Virtualize, we have to go through its RefreshDataAsync API otherwise:
            // (1) It won't know to update its own internal state if the provider output has changed
            // (2) We won't know what slice of data to query for
            // The reference can still be null before it's captured (first render) or right after toggling
            // virtualization on; in that case Virtualize will request its own items once it renders, so we
            // just reconcile the load-state here. The non-virtualized provider request must never run for a
            // virtualized grid.
            try
            {
                if (_virtualizeComponent is not null)
                {
                    await _virtualizeComponent.RefreshDataAsync();
                }
            }
            finally
            {
                // Always reconcile the load-state, even if RefreshDataAsync threw, so we don't leak the
                // CTS or leave _pendingDataLoadCancellationTokenSource pointing at a disposed instance.
                // This load is done with its own source, so dispose it unconditionally; only clear the
                // field when it still points at this source (a newer load may already own it).
                thisLoadCts.Dispose();
                if (ReferenceEquals(_pendingDataLoadCancellationTokenSource, thisLoadCts))
                {
                    _pendingDataLoadCancellationTokenSource = null;
                }
            }
        }
        else
        {
            // If we're not using Virtualize, we build and execute a request against the items provider directly
            var startIndex = Pagination is null ? 0 : (Pagination.CurrentPageIndex * Pagination.ItemsPerPage);
            var request = new BitDataGridLegacyItemsProviderRequest<TGridItem>(
                startIndex, Pagination?.ItemsPerPage, _sortByColumn, _sortByAscending, thisLoadCts.Token);
            try
            {
                var result = await ResolveItemsRequestAsync(request);
                if (!thisLoadCts.IsCancellationRequested)
                {
                    _currentNonVirtualizedViewItems = result.Items;
                    _ariaBodyRowCount = _currentNonVirtualizedViewItems.Count;
                    await (Pagination?.SetTotalItemCountAsync(result.TotalItemCount) ?? Task.CompletedTask);
                }
            }
            catch (OperationCanceledException) when (thisLoadCts.IsCancellationRequested)
            {
                // This load was superseded by a newer request (our own cancellation token fired); swallow
                // the cancellation and fall through to the cleanup below so the load-state remains
                // consistent. Cancellations from any other source (e.g. a provider-side timeout) propagate.
            }
            finally
            {
                // This load is done with its own source, so dispose it unconditionally to avoid leaking
                // a superseded source; only clear the field when it still points at this source.
                thisLoadCts.Dispose();
                if (ReferenceEquals(_pendingDataLoadCancellationTokenSource, thisLoadCts))
                {
                    _pendingDataLoadCancellationTokenSource = null;
                }
            }
        }
    }

    // Gets called both by RefreshDataCoreAsync and directly by the Virtualize child component during scrolling
    private async ValueTask<ItemsProviderResult<(int, TGridItem)>> ProvideVirtualizedItems(ItemsProviderRequest request)
    {
        _lastRefreshedPaginationStateHash = ComputePaginationStateHash();

        // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
        // TODO: Consider making this configurable, or smarter (e.g., doesn't delay on first call in a batch, then the amount
        // of delay increases if you rapidly issue repeated requests, such as when scrolling a long way)
        try
        {
            await Task.Delay(100, request.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The request was superseded/cancelled during the debounce window; abandon it early.
            return default;
        }
        if (request.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        // Combine the query parameters from Virtualize with the ones from PaginationState
        var startIndex = request.StartIndex;
        var count = request.Count;
        if (Pagination is not null)
        {
            startIndex += Pagination.CurrentPageIndex * Pagination.ItemsPerPage;
            count = Math.Max(0, Math.Min(request.Count, Pagination.ItemsPerPage - request.StartIndex));
        }

        var providerRequest = new BitDataGridLegacyItemsProviderRequest<TGridItem>(
            startIndex, count, _sortByColumn, _sortByAscending, request.CancellationToken);
        BitDataGridLegacyItemsProviderResult<TGridItem> providerResult;
        try
        {
            providerResult = await ResolveItemsRequestAsync(providerRequest);
        }
        catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
        {
            // The request was superseded by a newer one after the debounce window (our own cancellation
            // token fired); the items provider observed the cancellation token and bailed out. Return an
            // empty result the virtualization system can handle rather than letting the cancellation
            // propagate out of here. Cancellations from any other source propagate as real errors.
            return default;
        }

        if (!request.CancellationToken.IsCancellationRequested)
        {
            // ARIA's rowcount is part of the UI, so it should reflect what the human user regards as the number of rows in the table,
            // not the number of physical <tr> elements. For virtualization this means what's in the entire scrollable range, not just
            // the current viewport. In the case where you're also paginating then it means what's conceptually on the current page.
            // The last page can hold fewer than ItemsPerPage rows, so clamp the paginated count to the items remaining on the current
            // page; otherwise assistive tech would announce non-existent trailing rows on a short final page.
            _ariaBodyRowCount = Pagination is null
                ? providerResult.TotalItemCount
                : Math.Clamp(providerResult.TotalItemCount - Pagination.CurrentPageIndex * Pagination.ItemsPerPage, 0, Pagination.ItemsPerPage);

            await (Pagination?.SetTotalItemCountAsync(providerResult.TotalItemCount) ?? Task.CompletedTask);

            // We're supplying the row index along with each row's data because we need it for aria-rowindex, and we have to account for
            // the virtualized start index. It might be more performant just to have some _latestQueryRowStartIndex field, but we'd have
            // to make sure it doesn't get out of sync with the rows being rendered.
            return new ItemsProviderResult<(int, TGridItem)>(
                 items: providerResult.Items.Select((x, i) => ValueTuple.Create(i + request.StartIndex + 2, x)),
                 totalItemCount: _ariaBodyRowCount);
        }

        return default;
    }

    // Normalizes all the different ways of configuring a data source so they have common GridItemsProvider-shaped API
    private async ValueTask<BitDataGridLegacyItemsProviderResult<TGridItem>> ResolveItemsRequestAsync(BitDataGridLegacyItemsProviderRequest<TGridItem> request)
    {
        if (ItemsProvider is not null)
        {
            return await ItemsProvider(request);
        }
        else if (Items is not null)
        {
            var totalItemCount = _asyncQueryExecutor is null ? Items.Count() : await _asyncQueryExecutor.CountAsync(Items);
            var result = request.ApplySorting(Items).Skip(request.StartIndex);
            if (request.Count.HasValue)
            {
                result = result.Take(request.Count.Value);
            }
            var resultArray = _asyncQueryExecutor is null ? result.ToArray() : await _asyncQueryExecutor.ToArrayAsync(result);
            return BitDataGridLegacyItemsProviderResult.From(resultArray, totalItemCount);
        }
        else
        {
            return BitDataGridLegacyItemsProviderResult.From(Array.Empty<TGridItem>(), 0);
        }
    }

    private string AriaSortValue(BitDataGridLegacyColumnBase<TGridItem> column)
        => _sortByColumn == column
            ? (_sortByAscending ? "ascending" : "descending")
            : "none";

    private string? ColumnHeaderClass(BitDataGridLegacyColumnBase<TGridItem> column)
        => _sortByColumn == column
        ? $"{ColumnClass(column)} {(_sortByAscending ? "bit-qkg-csa" : "bit-qkg-csd")}"
        : ColumnClass(column);

    private string GridClass()
        => $"bit-qkg {Class} {((IsLoading && LoadingTemplate is null) ? "loading" : null)}".Trim();

    private void CloseColumnOptions()
    {
        _displayOptionsForColumn = null;
    }

    private string? GetRowClass(TGridItem item)
    {
        var selected = RowClassSelector?.Invoke(item);

        if (string.IsNullOrEmpty(RowClass)) return string.IsNullOrEmpty(selected) ? null : selected;
        if (string.IsNullOrEmpty(selected)) return RowClass;
        return $"{RowClass} {selected}";
    }

    private string? GetRowStyle(TGridItem item)
    {
        var selected = RowStyleSelector?.Invoke(item);

        if (string.IsNullOrEmpty(RowStyle)) return string.IsNullOrEmpty(selected) ? null : selected;
        if (string.IsNullOrEmpty(selected)) return RowStyle;
        return $"{RowStyle};{selected}";
    }



    private static string? ColumnClass(BitDataGridLegacyColumnBase<TGridItem> column) => column.Align switch
    {
        BitDataGridLegacyAlign.Center => $"bit-qkg-cjc {column.Class}",
        BitDataGridLegacyAlign.Right => $"bit-qkg-cje {column.Class}",
        _ => column.Class,
    };




    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        // Cancel (but don't dispose) any in-flight load: the load that owns this source may still be
        // holding its token, so disposing here could race into an ObjectDisposedException. The owning
        // load disposes it in its finally block (it's still the current source during disposal), so we
        // only signal cancellation here.
        _pendingDataLoadCancellationTokenSource?.Cancel();

        _currentPageItemsChanged.Dispose();

        try
        {
            if (_jsEventDisposable is not null)
            {
                await _jsEventDisposable.InvokeVoidAsync("stop");
                await _jsEventDisposable.DisposeAsync();
            }

            //if (_jsModule is not null)
            //{
            //    await _jsModule.DisposeAsync();
            //}
        }
        catch (JSDisconnectedException)
        {
            // The JS side may routinely be gone already if the reason we're disposing is that
            // the client disconnected. This is not an error.
        }
        catch (JSException ex)
        {
            // it seems it's safe to just ignore this exception here.
            // otherwise it will blow up the MAUI app in a page refresh for example.
            Console.WriteLine(ex.Message);
        }

        _disposed = true;
    }
}
