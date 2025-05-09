﻿// a fork from the Blazor QuickGrid at https://github.com/dotnet/aspnetcore/tree/main/src/Components/QuickGrid

namespace Bit.BlazorUI;

/// <summary>
/// BitDataGrid is a robust way to display an information-rich collection of items, and allow people to sort, and filter the content.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
public partial class BitDataGrid<TGridItem> : IAsyncDisposable
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
    private List<BitDataGridColumnBase<TGridItem>> _columns;
    private bool _collectingColumns; // Columns might re-render themselves arbitrarily. We only want to capture them at a defined time.

    // Tracking state for options and sorting
    private BitDataGridColumnBase<TGridItem>? _displayOptionsForColumn;
    private BitDataGridColumnBase<TGridItem>? _sortByColumn;
    private bool _sortByAscending;
    private bool _checkColumnOptionsPosition;

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
    private CancellationTokenSource? _pendingDataLoadCancellationTokenSource;

    // If the PaginationState mutates, it raises this event. We use it to trigger a re-render.
    private readonly EventCallbackSubscriber<BitDataGridPaginationState> _currentPageItemsChanged;



    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private IServiceProvider _services { get; set; } = default!;



    /// <summary>
    /// Constructs an instance of <see cref="BitDataGrid{TGridItem}"/>.
    /// </summary>
    public BitDataGrid()
    {
        _columns = new();
        _internalGridContext = new(this);
        _currentPageItemsChanged = new(EventCallback.Factory.Create<BitDataGridPaginationState>(this, RefreshDataCoreAsync));
        _renderColumnHeaders = RenderColumnHeaders;
        _renderNonVirtualizedRows = RenderNonVirtualizedRows;

        // As a special case, we don't issue the first data load request until we've collected the initial set of columns
        // This is so we can apply default sort order (or any future per-column options) before loading data
        // We use EventCallbackSubscriber to safely hook this async operation into the synchronous rendering flow
        var columnsFirstCollectedSubscriber = new EventCallbackSubscriber<object?>(
            EventCallback.Factory.Create<object?>(this, RefreshDataCoreAsync));
        columnsFirstCollectedSubscriber.SubscribeOrMove(_internalGridContext.ColumnsFirstCollected);
    }



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
    /// A callback that supplies data for the rid.
    ///
    /// You should supply either <see cref="Items"/> or <see cref="ItemsProvider"/>, but not both.
    /// </summary>
    [Parameter] public BitDataGridItemsProvider<TGridItem>? ItemsProvider { get; set; }

    /// <summary>
    /// An optional CSS class name. If given, this will be included in the class attribute of the rendered table.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// A theme name, with default value "default". This affects which styling rules match the table.
    /// </summary>
    [Parameter] public string? Theme { get; set; } = "default";

    /// <summary>
    /// Defines the child components of this instance. For example, you may define columns by adding
    /// components derived from the <see cref="BitDataGridColumnBase{TGridItem}"/> base class.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

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
    /// This is applicable only when using <see cref="Virtualize"/>. It defines an expected height in pixels for
    /// each row, allowing the virtualization mechanism to fetch the correct number of items to match the display
    /// size and to ensure accurate scrolling.
    /// </summary>
    [Parameter] public float ItemSize { get; set; } = 50;

    /// <summary>
    /// If true, renders draggable handles around the column headers, allowing the user to resize the columns
    /// manually. Size changes are not persisted.
    /// </summary>
    [Parameter] public bool ResizableColumns { get; set; }

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
    /// Optionally links this <see cref="BitDataGrid{TGridItem}"/> instance with a <see cref="BitDataGridPaginationState"/> model,
    /// causing the grid to fetch and render only the current page of data.
    ///
    /// This is normally used in conjunction with a <see cref="BitDataGridPaginator"/> component or some other UI logic
    /// that displays and updates the supplied <see cref="BitDataGridPaginationState"/> instance.
    /// </summary>
    [Parameter] public BitDataGridPaginationState? Pagination { get; set; }



    /// <summary>
    /// Sets the grid's current sort column to the specified <paramref name="column"/>.
    /// </summary>
    /// <param name="column">The column that defines the new sort order.</param>
    /// <param name="direction">The direction of sorting. If the value is <see cref="BitDataGridSortDirection.Auto"/>, then it will toggle the direction on each call.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    public Task SortByColumnAsync(BitDataGridColumnBase<TGridItem> column, BitDataGridSortDirection direction = BitDataGridSortDirection.Auto)
    {
        _sortByAscending = direction switch
        {
            BitDataGridSortDirection.Ascending => true,
            BitDataGridSortDirection.Descending => false,
            BitDataGridSortDirection.Auto => _sortByColumn == column ? !_sortByAscending : true,
            _ => throw new NotSupportedException($"Unknown sort direction {direction}"),
        };

        _sortByColumn = column;

        StateHasChanged(); // We want to see the updated sort order in the header, even before the data query is completed
        return RefreshDataAsync();
    }

    /// <summary>
    /// Displays the <see cref="BitDataGridColumnBase{TGridItem}.ColumnOptions"/> UI for the specified column, closing any other column
    /// options UI that was previously displayed.
    /// </summary>
    /// <param name="column">The column whose options are to be displayed, if any are available.</param>
    public void ShowColumnOptions(BitDataGridColumnBase<TGridItem> column)
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
        await RefreshDataCoreAsync();
        StateHasChanged();
    }



    // Invoked by descendant columns at a special time during rendering
    internal void AddColumn(BitDataGridColumnBase<TGridItem> column, BitDataGridSortDirection? isDefaultSortDirection)
    {
        if (_collectingColumns)
        {
            _columns.Add(column);

            if (_sortByColumn is null && isDefaultSortDirection.HasValue)
            {
                _sortByColumn = column;
                _sortByAscending = isDefaultSortDirection.Value != BitDataGridSortDirection.Descending;
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
            throw new InvalidOperationException($"BitDataGrid requires one of {nameof(Items)} or {nameof(ItemsProvider)}, but both were specified.");
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
            || (Pagination?.GetHashCode() != _lastRefreshedPaginationStateHash);

        // We don't want to trigger the first data load until we've collected the initial set of columns,
        // because they might perform some action like setting the default sort order, so it would be wasteful
        // to have to re-query immediately
        return (_columns.Count > 0 && mustRefreshData) ? RefreshDataCoreAsync() : Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsEventDisposable = await _js.BitDataGridInit(_tableReference);
        }

        if (_checkColumnOptionsPosition && _displayOptionsForColumn is not null)
        {
            _checkColumnOptionsPosition = false;
            _ = _js.BitDataGridCheckColumnOptionsPosition(_tableReference);
        }
    }



    private void StartCollectingColumns()
    {
        _columns.Clear();
        _collectingColumns = true;
    }

    private void FinishCollectingColumns()
    {
        _collectingColumns = false;
    }

    // Same as RefreshDataAsync, except without forcing a re-render. We use this from OnParametersSetAsync
    // because in that case there's going to be a re-render anyway.
    private async Task RefreshDataCoreAsync()
    {
        // Move into a "loading" state, cancelling any earlier-but-still-pending load
        _pendingDataLoadCancellationTokenSource?.Cancel();
        var thisLoadCts = _pendingDataLoadCancellationTokenSource = new CancellationTokenSource();

        if (_virtualizeComponent is not null)
        {
            // If we're using Virtualize, we have to go through its RefreshDataAsync API otherwise:
            // (1) It won't know to update its own internal state if the provider output has changed
            // (2) We won't know what slice of data to query for
            await _virtualizeComponent.RefreshDataAsync();
            _pendingDataLoadCancellationTokenSource = null;
        }
        else
        {
            // If we're not using Virtualize, we build and execute a request against the items provider directly
            _lastRefreshedPaginationStateHash = Pagination?.GetHashCode();
            var startIndex = Pagination is null ? 0 : (Pagination.CurrentPageIndex * Pagination.ItemsPerPage);
            var request = new BitDataGridItemsProviderRequest<TGridItem>(
                startIndex, Pagination?.ItemsPerPage, _sortByColumn, _sortByAscending, thisLoadCts.Token);
            var result = await ResolveItemsRequestAsync(request);
            if (!thisLoadCts.IsCancellationRequested)
            {
                _currentNonVirtualizedViewItems = result.Items;
                _ariaBodyRowCount = _currentNonVirtualizedViewItems.Count;
                Pagination?.SetTotalItemCountAsync(result.TotalItemCount);
                _pendingDataLoadCancellationTokenSource = null;
            }
        }
    }

    // Gets called both by RefreshDataCoreAsync and directly by the Virtualize child component during scrolling
    private async ValueTask<ItemsProviderResult<(int, TGridItem)>> ProvideVirtualizedItems(ItemsProviderRequest request)
    {
        _lastRefreshedPaginationStateHash = Pagination?.GetHashCode();

        // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
        // TODO: Consider making this configurable, or smarter (e.g., doesn't delay on first call in a batch, then the amount
        // of delay increases if you rapidly issue repeated requests, such as when scrolling a long way)
        await Task.Delay(100);
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
            count = Math.Min(request.Count, Pagination.ItemsPerPage - request.StartIndex);
        }

        var providerRequest = new BitDataGridItemsProviderRequest<TGridItem>(
            startIndex, count, _sortByColumn, _sortByAscending, request.CancellationToken);
        var providerResult = await ResolveItemsRequestAsync(providerRequest);

        if (!request.CancellationToken.IsCancellationRequested)
        {
            // ARIA's rowcount is part of the UI, so it should reflect what the human user regards as the number of rows in the table,
            // not the number of physical <tr> elements. For virtualization this means what's in the entire scrollable range, not just
            // the current viewport. In the case where you're also paginating then it means what's conceptually on the current page.
            // TODO: This currently assumes we always want to expand the last page to have ItemsPerPage rows, but the experience might
            //       be better if we let the last page only be as big as its number of actual rows.
            _ariaBodyRowCount = Pagination is null ? providerResult.TotalItemCount : Pagination.ItemsPerPage;

            Pagination?.SetTotalItemCountAsync(providerResult.TotalItemCount);

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
    private async ValueTask<BitDataGridItemsProviderResult<TGridItem>> ResolveItemsRequestAsync(BitDataGridItemsProviderRequest<TGridItem> request)
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
            return BitDataGridItemsProviderResult.From(resultArray, totalItemCount);
        }
        else
        {
            return BitDataGridItemsProviderResult.From(Array.Empty<TGridItem>(), 0);
        }
    }

    private string AriaSortValue(BitDataGridColumnBase<TGridItem> column)
        => _sortByColumn == column
            ? (_sortByAscending ? "ascending" : "descending")
            : "none";

    private string? ColumnHeaderClass(BitDataGridColumnBase<TGridItem> column)
        => _sortByColumn == column
        ? $"{ColumnClass(column)} {(_sortByAscending ? "col-sort-asc" : "col-sort-desc")}"
        : ColumnClass(column);

    private string GridClass()
        => $"bitdatagrid {Class} {(_pendingDataLoadCancellationTokenSource is null ? null : "loading")}";

    private void CloseColumnOptions()
    {
        _displayOptionsForColumn = null;
    }

    private static string? ColumnClass(BitDataGridColumnBase<TGridItem> column) => column.Align switch
    {
        BitDataGridAlign.Center => $"col-justify-center {column.Class}",
        BitDataGridAlign.Right => $"col-justify-end {column.Class}",
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
        catch(JSException ex)
        {
            // it seems it's safe to just ignore this exception here.
            // otherwise it will blow up the MAUI app in a page refresh for example.
            Console.WriteLine(ex.Message);
        }

        _disposed = true;
    }
}
