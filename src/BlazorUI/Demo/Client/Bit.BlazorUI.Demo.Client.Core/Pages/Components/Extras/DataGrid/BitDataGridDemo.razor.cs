using Bit.BlazorUI.Demo.Client.Core.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public partial class BitDataGridDemo : AppComponentBase
{
    // example 1 - basic & sorting
    private readonly List<Product> basicProducts = SampleData.Generate(50);

    // example 2 - filtering & paging
    private readonly List<Product> filterProducts = SampleData.Generate(200);

    // example 3 - selection
    private readonly List<Product> selectionProducts = SampleData.Generate(60);
    private BitDataGridSelectionMode selectionMode = BitDataGridSelectionMode.Multiple;
    private IReadOnlyList<Product> selectedProducts = new List<Product>();

    // Switching to Single must drop any extra selections so the bound state (and the "N selected"
    // label) matches Single semantics; the grid normalizes its internal set but does not push the
    // trimmed selection back to this controlled binding.
    private void SelectSingleMode()
    {
        selectionMode = BitDataGridSelectionMode.Single;
        if (selectedProducts.Count > 1)
        {
            selectedProducts = selectedProducts.Take(1).ToList();
        }
    }

    // example 4 - editing
    private readonly List<Product> editProducts = SampleData.Generate(25);
    private int nextId;
    private string editStatus = "";

    // example 5 - grouping
    private readonly List<Product> groupProducts = SampleData.Generate(80);
    private BitDataGrid<Product>? groupGrid;

    private object? DistinctSuppliers(IReadOnlyList<Product> rows)
        => $"{rows.Select(p => p.Supplier).Distinct().Count()} distinct";

    private async Task ExpandAllGroups() { if (groupGrid is not null) await groupGrid.ExpandAllGroupsAsync(); }
    private async Task CollapseAllGroups() { if (groupGrid is not null) await groupGrid.CollapseAllGroupsAsync(); }

    // example 6 - templates
    private readonly List<Product> templateProducts = SampleData.Generate(30);

    // Orders in-stock rows ahead of out-of-stock ones, then by quantity - an ordering the raw numeric
    // value cannot express on its own.
    private readonly IComparer<object?> stockComparer = new StockComparer();

    private sealed class StockComparer : IComparer<object?>
    {
        public int Compare(object? x, object? y)
        {
            var a = x as int? ?? 0;
            var b = y as int? ?? 0;
            var inStock = (b == 0).CompareTo(a == 0);
            return inStock != 0 ? inStock : a.CompareTo(b);
        }
    }

    // example 7 - columns resize/reorder/freeze
    private readonly List<Product> columnsProducts = SampleData.Generate(40);

    // example 8 - column groups
    private readonly List<Product> columnGroupsProducts = SampleData.Generate(40);

    // example 9 - column spanning
    private readonly List<Product> spanningProducts = SampleData.Generate(40);

    // example 10 - virtualization
    private List<Product> virtualProducts = SampleData.Generate(10_000);

    // example 11 - server-side
    private readonly List<Product> serverAll = SampleData.Generate(523);
    private bool serverLoading;
    private string serverLastRequest = "";

    // example 12 - infinite scrolling
    // Use a count that is not a multiple of the 40-row batch size so the final batch is short,
    // letting the grid detect the end without an extra empty fetch.
    private readonly List<Product> infiniteAll = SampleData.Generate(2_017);
    private string infiniteLog = "Scroll down to load more…";
    private int infiniteRequests;

    // example 13 - tree view
    private readonly List<FileNode> fileRoots = FileSystemData.Build();
    private BitDataGrid<FileNode>? treeGrid;

    // example 14 - master detail
    private readonly List<SupplierModel> suppliers = BuildSuppliers();

    // example 15 - row reordering
    private readonly List<Product> reorderProducts = SampleData.Generate(12);
    private string? reorderLog;

    // example 16 - cell events & context menu
    private readonly List<Product> cellEventsProducts = SampleData.Generate(40);
    private string cellEventStatus = "Click, double-click or right-click any cell.";
    private BitDataGrid<Product>? cellEventsGrid;
    private BitDataGridCellEventArgs<Product>? cellMenuArgs;
    private int cellMenuX;
    private int cellMenuY;
    private ElementReference cellMenuFirstItem;
    private bool cellMenuFocusPending;

    // example 17 - cell navigation
    private readonly List<Product> cellNavProducts = SampleData.Generate(40);

    // example 18 - variable row height
    private readonly List<Product> variableHeightProducts = SampleData.Generate(40);

    // example 19 - empty state
    private readonly List<Product> emptyData = SampleData.Generate(25);
    private readonly List<Product> emptyNone = new();
    private bool emptyHasData;
    private bool emptyLoading;
    private List<Product> EmptyCurrent => emptyHasData ? emptyData : emptyNone;

    private async Task SimulateLoading()
    {
        emptyLoading = true;
        StateHasChanged();
        await Task.Delay(1500);
        emptyLoading = false;
        emptyHasData = true;
    }

    // example 20 - borders, striping & row numbers
    private readonly List<Product> borderStripeProducts = SampleData.Generate(60);
    private bool bordered = true;
    private bool striped = true;
    private bool rowNumbers = true;

    // example 39 - RTL
    private readonly List<Product> rtlProducts = SampleData.GeneratePersian(60);

    // example 21 - filter operators
    private readonly List<Product> operatorProducts = SampleData.Generate(150);

    // example 22 - edit validation
    private readonly List<Product> validationProducts = SampleData.Generate(30);

    private string? ValidateName(Product product, object? value)
        => string.IsNullOrWhiteSpace(value as string) ? "Name is required." : null;

    private string? ValidatePrice(Product product, object? value)
        => value is decimal price && price < 0 ? "Price cannot be negative." : null;

    private string? ValidateStock(Product product, object? value)
        => value is int stock && stock < 0 ? "Stock cannot be negative." : null;

    // example 23 - state persistence
    private readonly List<Product> stateProducts = SampleData.Generate(120);
    private BitDataGrid<Product>? stateGrid;
    private BitDataGridState? savedGridState;
    private string gridStateStatus = "Adjust the grid, then save its state.";

    private void SaveGridState()
    {
        savedGridState = stateGrid?.GetState();
        gridStateStatus = savedGridState is null
            ? "Nothing to save yet."
            : $"Saved: page {savedGridState.CurrentPage}, {savedGridState.Sorts.Count} sort(s), {savedGridState.Filters.Count} filter(s).";
    }

    private async Task RestoreGridState()
    {
        if (stateGrid is null || savedGridState is null) return;
        await stateGrid.ApplyStateAsync(savedGridState);
        gridStateStatus = "State restored.";
    }

    // example 24 - server-side virtualization (+ server aggregates)
    private readonly List<Product> serverVirtualAll = SampleData.Generate(100_000);

    private async Task<BitDataGridReadResult<Product>> LoadVirtualServerData(BitDataGridReadRequest request)
    {
        // Simulate backend latency. Superseded scroll windows are cancelled by the grid, and the
        // OperationCanceledException must propagate so the grid discards the stale read - returning
        // an empty result instead would be rendered as real data and blank the viewport.
        await Task.Delay(150, request.CancellationToken);

        IEnumerable<Product> query = serverVirtualAll;

        foreach (var f in request.Filters)
        {
            query = f.ColumnId switch
            {
                nameof(Product.Name) => query.Where(p => MatchText(p.Name, f)),
                nameof(Product.Supplier) => query.Where(p => MatchText(p.Supplier, f)),
                nameof(Product.Category) => query.Where(p => MatchComparable(p.Category, f)),
                nameof(Product.Price) => query.Where(p => MatchComparable(p.Price, f)),
                nameof(Product.Stock) => query.Where(p => MatchComparable(p.Stock, f)),
                nameof(Product.Rating) => query.Where(p => MatchComparable(p.Rating, f)),
                _ => query
            };
        }

        IOrderedEnumerable<Product>? ordered = null;
        foreach (var sort in request.Sorts)
        {
            Func<Product, object> key = sort.ColumnId switch
            {
                nameof(Product.Name) => p => p.Name,
                nameof(Product.Category) => p => p.Category,
                nameof(Product.Supplier) => p => p.Supplier,
                nameof(Product.Price) => p => p.Price,
                nameof(Product.Stock) => p => p.Stock,
                nameof(Product.Rating) => p => p.Rating,
                _ => p => p.Id
            };
            ordered = ordered is null
                ? (sort.Direction == BitDataGridSortDirection.Descending ? query.OrderByDescending(key) : query.OrderBy(key))
                : (sort.Direction == BitDataGridSortDirection.Descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key));
        }
        if (ordered is not null) query = ordered;

        var filtered = query.ToList();
        var items = filtered.Skip(request.Skip).Take(request.Take ?? filtered.Count).ToList();

        // Aggregates computed over the WHOLE filtered dataset (not just the returned window), so the
        // footer shows a real grand total instead of a per-window number.
        var priceSum = filtered.Sum(p => p.Price);
        var aggregates = new List<BitDataGridAggregateResult>
        {
            new()
            {
                ColumnId = nameof(Product.Price),
                Type = BitDataGridAggregateType.Sum,
                Value = priceSum,
                FormattedValue = priceSum.ToString("C0")
            }
        };

        return new BitDataGridReadResult<Product>(items, filtered.Count) { Aggregates = aggregates };
    }

    // example 26 - IQueryable data source (an EF Core DbSet would bind the same way)
    private readonly IQueryable<Product> queryableProducts = SampleData.Generate(400).AsQueryable();

    // example 27 - Excel export
    private readonly List<Product> excelProducts = SampleData.Generate(120);

    // example 28 - export with complex layouts (master-detail + frozen + ColSpan)
    private readonly List<Product> complexExportProducts = SampleData.Generate(30);

    // example 29 - lazy tree loading
    private readonly List<FileNode> lazyTreeRoots = BuildLazyRoots();
    private int nextLazyNodeId = 1000;

    private static List<FileNode> BuildLazyRoots() =>
    [
        new() { Id = 1, Name = "src", Kind = "Folder", Modified = new DateTime(2025, 1, 10) },
        new() { Id = 2, Name = "docs", Kind = "Folder", Modified = new DateTime(2025, 2, 5) },
        new() { Id = 3, Name = "assets", Kind = "Folder", Modified = new DateTime(2025, 3, 20) },
        new() { Id = 4, Name = "LICENSE", Kind = "File", Size = 1_070, Modified = new DateTime(2025, 1, 2) },
    ];

    private static bool IsFolder(FileNode node) => node.Kind == "Folder";

    private async Task<IEnumerable<FileNode>?> LoadChildrenAsync(FileNode parent)
    {
        // Simulates a backend call; results are cached by the grid, so each folder loads once.
        await Task.Delay(600);
        return
        [
            new FileNode { Id = ++nextLazyNodeId, Name = $"{parent.Name}-sub", Kind = "Folder", Modified = parent.Modified.AddDays(1) },
            new FileNode { Id = ++nextLazyNodeId, Name = $"{parent.Name}-notes.md", Kind = "File", Size = 2_300 + parent.Id * 17, Modified = parent.Modified.AddDays(2) },
            new FileNode { Id = ++nextLazyNodeId, Name = $"{parent.Name}-data.json", Kind = "File", Size = 5_100 + parent.Id * 31, Modified = parent.Modified.AddDays(3) },
        ];
    }

    // example 30 - touch drag & drop
    private readonly List<Product> touchReorderProducts = SampleData.Generate(12);

    // example 31 - column virtualization
    private readonly List<Product> wideProducts = SampleData.Generate(3_000);

    // example 32 - programmatic control
    private readonly List<Product> apiProducts = SampleData.Generate(200);
    private BitDataGrid<Product>? apiGrid;

    private async Task ApiSortByPrice() { if (apiGrid is not null) await apiGrid.SortByAsync(nameof(Product.Price), BitDataGridSortDirection.Descending); }
    private async Task ApiClearSorts() { if (apiGrid is not null) await apiGrid.ClearSortsAsync(); }
    private async Task ApiFilterExpensive() { if (apiGrid is not null) await apiGrid.ApplyFilterAsync(nameof(Product.Price), BitDataGridFilterOperator.GreaterThan, 500m); }
    private async Task ApiClearFilters() { if (apiGrid is not null) await apiGrid.ClearFiltersAsync(); }
    private async Task ApiGroupByCategory() { if (apiGrid is not null) await apiGrid.GroupByAsync(nameof(Product.Category)); }
    private async Task ApiUngroup() { if (apiGrid is not null) await apiGrid.UngroupAsync(nameof(Product.Category)); }
    private async Task ApiGoToPage3() { if (apiGrid is not null) await apiGrid.GoToPageAsync(3); }
    private async Task ApiPageSize50() { if (apiGrid is not null) await apiGrid.SetPageSizeAsync(50); }
    private async Task ApiRefresh() { if (apiGrid is not null) await apiGrid.RefreshAsync(); }

    private string apiLog = "The grid reports every view change here.";

    private void OnApiSortChange(IReadOnlyList<BitDataGridSortDescriptor> sorts)
        => apiLog = sorts.Count == 0 ? "Sorting cleared." : $"Sorting by {string.Join(", ", sorts.Select(s => $"{s.ColumnId} {s.Direction}"))}.";

    private void OnApiFilterChange(IReadOnlyList<BitDataGridFilterDescriptor> filters)
        => apiLog = filters.Count == 0 ? "Filters cleared." : $"{filters.Count} filter(s) active.";

    private void OnApiGroupChange(IReadOnlyList<BitDataGridGroupDescriptor> groups)
        => apiLog = groups.Count == 0 ? "Grouping cleared." : $"Grouped by {string.Join(" › ", groups.Select(g => g.ColumnId))}.";

    private void OnApiPageChange(int page) => apiLog = $"Moved to page {page}.";

    // example 33 - detail rows from the row & from code
    private readonly List<Product> detailProducts = SampleData.Generate(20);
    private BitDataGrid<Product>? detailGrid;
    private string detailStatus = "Click any row to reveal its details.";

    private void OnDetailToggled(BitDataGridDetailEventArgs<Product> args)
        => detailStatus = $"{args.Item.Name} (#{args.Item.Id}) {(args.Expanded ? "expanded" : "collapsed")}.";

    private async Task ExpandAllDetails() { if (detailGrid is not null) await detailGrid.ExpandAllDetailsAsync(); }
    private async Task CollapseAllDetails() { if (detailGrid is not null) await detailGrid.CollapseAllDetailsAsync(); }
    private async Task ToggleFirstDetail() { if (detailGrid is not null) await detailGrid.ToggleDetailAsync(detailProducts[0]); }

    // example 34 - quick search
    private readonly List<Product> searchProducts = SampleData.Generate(200);
    private string? searchTerm;

    // example 35 - conditional row styling & cell tooltips
    private readonly List<Product> styledRowProducts = SampleData.Generate(60);

    private static string? RowClassFor(Product p) => p.Stock == 0 ? "row-out-of-stock" : null;

    private static string? RowStyleFor(Product p) => p.Price > 800 ? "font-weight:600;" : null;

    // example 36 - clipboard & range selection
    private readonly List<Product> clipboardProducts = SampleData.Generate(40);
    private BitDataGrid<Product>? clipboardGrid;
    private IReadOnlyList<Product> clipboardSelection = [];
    private string clipboardStatus = "Select rows, then copy them.";

    private async Task SelectAllRows()
    {
        if (clipboardGrid is null) return;
        await clipboardGrid.SelectAllAsync();
        clipboardStatus = $"{clipboardSelection.Count} rows selected.";
    }

    private async Task ClearRowSelection()
    {
        if (clipboardGrid is null) return;
        await clipboardGrid.ClearSelectionAsync();
        clipboardStatus = "Selection cleared.";
    }

    private async Task CopySelection()
    {
        if (clipboardGrid is null) return;
        var copied = await clipboardGrid.CopyToClipboardAsync();
        clipboardStatus = copied > 0
            ? $"{copied} rows copied to the clipboard."
            : "Nothing was copied (the clipboard may be unavailable here).";
    }

    private async Task ExportSelection()
    {
        if (clipboardGrid is null) return;
        await clipboardGrid.ExportExcelAsync(selectedOnly: true);
        clipboardStatus = $"{clipboardSelection.Count} rows exported to selection.xlsx.";
    }

    // example 37 - text wrapping
    private readonly List<Product> wrapProducts = SampleData.Generate(40);
    private bool wrapCellText = true;

    // A long, unbroken description per row, so the wrapping (and the growing row) is actually visible.
    private static string DescriptionOf(Product p)
        => $"{p.Name} is a {p.Category.ToString().ToLowerInvariant()} product supplied by {p.Supplier}, " +
           $"released on {p.ReleaseDate:d} and currently rated {p.Rating:0.0} out of 5 by our customers.";

    // example 38 - row double-click & programmatic editing
    private readonly List<Product> rowEventProducts = SampleData.Generate(30);
    private BitDataGrid<Product>? rowEventGrid;
    private string rowEventStatus = "Double-click a row to edit it.";

    private void EditOnDoubleClick(Product product)
    {
        if (rowEventGrid is null) return;
        // BeginEdit is public API, so a double-click (or any chrome of your own) can open the editors
        // the command column's Edit button would have opened.
        rowEventGrid.BeginEdit(product);
        rowEventStatus = $"Editing {product.Name}. Press Enter to save or Esc to cancel.";
    }

    private void OnRowEventSaved(Product product) => rowEventStatus = $"Saved {product.Name}.";

    private void OnRowEventCancelled(Product product) => rowEventStatus = $"Cancelled editing {product.Name}.";

    // example 25 - localization
    private readonly List<Product> localizedProducts = SampleData.GeneratePersian(60);

    private readonly BitDataGridStrings persianStrings = new()
    {
        EmptyText = "رکوردی برای نمایش وجود ندارد.",
        LoadingText = "در حال بارگذاری…",
        FilterPlaceholder = "فیلتر…",
        FilterAllText = "همه",
        PagerRangeFormat = "{0}–{1} از {2}",
        PagerPageFormat = "صفحهٔ {0} از {1}",
        PerPageFormat = "{0} در صفحه",
        RowsPerPageLabel = "تعداد ردیف در صفحه",
        FirstPageLabel = "صفحهٔ اول",
        PreviousPageLabel = "صفحهٔ قبل",
        NextPageLabel = "صفحهٔ بعد",
        LastPageLabel = "صفحهٔ آخر",
        ClearFiltersText = "حذف فیلترها",
    };

    private static string CategoryFa(Category category) => category switch
    {
        Category.Electronics => "الکترونیک",
        Category.Books => "کتاب",
        Category.Clothing => "پوشاک",
        Category.Home => "خانه",
        Category.Toys => "اسباب‌بازی",
        Category.Sports => "ورزش",
        Category.Grocery => "خواربار",
        _ => category.ToString()
    };


    protected override Task OnInitAsync()
    {
        nextId = editProducts.Max(p => p.Id) + 1;
        return base.OnInitAsync();
    }


    // ---- editing handlers ----
    private Product CreateProduct() => new()
    {
        Id = nextId++,
        Name = "New product",
        Category = Category.Electronics,
        Price = 0,
        Stock = 0,
        Rating = 3,
        ReleaseDate = DateTime.Today
    };

    private void OnCreate(Product p) => editStatus = $"Adding new product #{p.Id}…";

    private void OnSave(Product p)
    {
        // Match by identifier rather than reference so a distinct instance representing the same product
        // replaces the existing row instead of being inserted as a duplicate.
        var index = editProducts.FindIndex(x => x.Id == p.Id);
        if (index >= 0) editProducts[index] = p;
        else editProducts.Insert(0, p);
        editStatus = $"Saved {p.Name} (#{p.Id}).";
    }

    private void OnDelete(Product p)
    {
        // Locate by identifier so deletion still targets the right row when a different instance is passed.
        var index = editProducts.FindIndex(x => x.Id == p.Id);
        if (index >= 0) editProducts.RemoveAt(index);
        editStatus = $"Deleted #{p.Id}.";
    }


    // ---- column spanning helpers ----
    private int? NameSpan(Product p) => p.Discontinued ? 2 : null;
    private int? PriceSpan(Product p) => p.Price > 800 ? 2 : null;


    // ---- server-side ----
    private async Task<BitDataGridReadResult<Product>> LoadServerData(BitDataGridReadRequest request)
    {
        serverLoading = true;
        await InvokeAsync(StateHasChanged);

        int total = 0;
        try
        {
            await Task.Delay(250, request.CancellationToken);

            IEnumerable<Product> query = serverAll;

            foreach (var f in request.Filters)
            {
                query = f.ColumnId switch
                {
                    // Text columns use the string operators emitted by the grid's text filter editor.
                    nameof(Product.Name) => query.Where(p => MatchText(p.Name, f)),
                    nameof(Product.Supplier) => query.Where(p => MatchText(p.Supplier, f)),
                    // Non-text columns receive a typed value (enum/decimal/int) with a comparison
                    // operator, so compare against the typed value instead of a substring of ToString().
                    nameof(Product.Category) => query.Where(p => MatchComparable(p.Category, f)),
                    nameof(Product.Price) => query.Where(p => MatchComparable(p.Price, f)),
                    nameof(Product.Stock) => query.Where(p => MatchComparable(p.Stock, f)),
                    _ => query
                };
            }

            IOrderedEnumerable<Product>? ordered = null;
            foreach (var sort in request.Sorts)
            {
                Func<Product, object> key = sort.ColumnId switch
                {
                    nameof(Product.Name) => p => p.Name,
                    nameof(Product.Category) => p => p.Category,
                    nameof(Product.Supplier) => p => p.Supplier,
                    nameof(Product.Price) => p => p.Price,
                    nameof(Product.Stock) => p => p.Stock,
                    _ => p => p.Id
                };

                if (ordered is null)
                {
                    ordered = sort.Direction == BitDataGridSortDirection.Descending
                        ? query.OrderByDescending(key)
                        : query.OrderBy(key);
                }
                else
                {
                    ordered = sort.Direction == BitDataGridSortDirection.Descending
                        ? ordered.ThenByDescending(key)
                        : ordered.ThenBy(key);
                }
            }
            if (ordered is not null) query = ordered;

            var filtered = query.ToList();
            total = filtered.Count;
            var items = filtered.Skip(request.Skip).Take(request.Take ?? total).ToList();

            // A superseded request can finish filtering/sorting/paging after a newer one started; bail
            // out before returning so the grid never receives stale rows for a cancelled load.
            request.CancellationToken.ThrowIfCancellationRequested();

            return new BitDataGridReadResult<Product>(items, total);
        }
        finally
        {
            // A superseded request observes a cancelled token; skip writing UI state so a stale load
            // can't overwrite the fresher request's status. The newer load owns serverLoading.
            if (!request.CancellationToken.IsCancellationRequested)
            {
                serverLastRequest = $"Last request → skip {request.Skip}, take {request.Take}, sorts: {request.Sorts.Count}, filters: {request.Filters.Count}, total: {total}";
                serverLoading = false;
                // Ensure the parent re-renders after the load completes, since this runs as a callback.
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    // Applies a text-column filter the way the grid's text editor emits it: a string value combined with
    // one of the string/empty operators. Anything else is treated as "no criteria" so the row matches.
    private static bool MatchText(string value, BitDataGridFilterDescriptor f)
    {
        if (f.Operator is BitDataGridFilterOperator.IsEmpty) return string.IsNullOrEmpty(value);
        if (f.Operator is BitDataGridFilterOperator.IsNotEmpty) return !string.IsNullOrEmpty(value);

        var term = f.Value?.ToString();
        if (string.IsNullOrWhiteSpace(term)) return true;

        return f.Operator switch
        {
            BitDataGridFilterOperator.Contains => value.Contains(term, StringComparison.OrdinalIgnoreCase),
            BitDataGridFilterOperator.DoesNotContain => !value.Contains(term, StringComparison.OrdinalIgnoreCase),
            BitDataGridFilterOperator.StartsWith => value.StartsWith(term, StringComparison.OrdinalIgnoreCase),
            BitDataGridFilterOperator.EndsWith => value.EndsWith(term, StringComparison.OrdinalIgnoreCase),
            BitDataGridFilterOperator.Equals => string.Equals(value, term, StringComparison.OrdinalIgnoreCase),
            BitDataGridFilterOperator.NotEquals => !string.Equals(value, term, StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    // Applies a non-text-column filter against the typed value the grid emits (enum/decimal/int) so the
    // requested equality/range operator is honored instead of a substring match on ToString().
    private static bool MatchComparable<T>(T value, BitDataGridFilterDescriptor f) where T : IComparable
    {
        if (f.Operator is BitDataGridFilterOperator.IsEmpty) return value is null;
        if (f.Operator is BitDataGridFilterOperator.IsNotEmpty) return value is not null;
        if (f.Value is null) return true;

        // The grid hands back a value already of the column's type; guard against an unexpected type.
        if (f.Value is not T typed)
            return true;

        var cmp = value.CompareTo(typed);
        return f.Operator switch
        {
            BitDataGridFilterOperator.Equals => cmp == 0,
            BitDataGridFilterOperator.NotEquals => cmp != 0,
            BitDataGridFilterOperator.GreaterThan => cmp > 0,
            BitDataGridFilterOperator.GreaterThanOrEqual => cmp >= 0,
            BitDataGridFilterOperator.LessThan => cmp < 0,
            BitDataGridFilterOperator.LessThanOrEqual => cmp <= 0,
            _ => true
        };
    }


    // ---- infinite scrolling ----
    private async Task<BitDataGridReadResult<Product>> LoadMore(BitDataGridReadRequest request)
    {
        await Task.Delay(350, request.CancellationToken);

        IEnumerable<Product> query = infiniteAll;

        IOrderedEnumerable<Product>? ordered = null;
        foreach (var sort in request.Sorts)
        {
            Func<Product, object> key = sort.ColumnId switch
            {
                nameof(Product.Name) => p => p.Name,
                nameof(Product.Category) => p => p.Category,
                nameof(Product.Supplier) => p => p.Supplier,
                nameof(Product.Price) => p => p.Price,
                nameof(Product.Stock) => p => p.Stock,
                nameof(Product.Rating) => p => p.Rating,
                _ => p => p.Id
            };

            if (ordered is null)
            {
                ordered = sort.Direction == BitDataGridSortDirection.Descending
                    ? query.OrderByDescending(key)
                    : query.OrderBy(key);
            }
            else
            {
                ordered = sort.Direction == BitDataGridSortDirection.Descending
                    ? ordered.ThenByDescending(key)
                    : ordered.ThenBy(key);
            }
        }
        if (ordered is not null) query = ordered;

        // Take is the batch size while scrolling; null means "all rows" (issued by CSV/Excel exports).
        var batch = query.Skip(request.Skip).Take(request.Take ?? infiniteAll.Count).ToList();

        // Drop a superseded batch before mutating shared demo state so stale rows aren't logged.
        request.CancellationToken.ThrowIfCancellationRequested();

        infiniteRequests++;
        var end = request.Skip + batch.Count;
        infiniteLog = batch.Count == 0
            ? $"Batch #{infiniteRequests} → no additional rows loaded"
            : $"Batch #{infiniteRequests} → loaded rows {request.Skip + 1}–{end} ({batch.Count} rows)";
        await InvokeAsync(StateHasChanged);

        return new BitDataGridReadResult<Product>(batch, 0);
    }


    // ---- tree view ----
    private async Task ExpandAll() { if (treeGrid is not null) await treeGrid.ExpandAllAsync(); }
    private async Task CollapseAll() { if (treeGrid is not null) await treeGrid.CollapseAllAsync(); }


    // ---- master detail ----
    private static List<SupplierModel> BuildSuppliers() =>
        SampleData.Generate(240)
            .GroupBy(p => p.Supplier)
            .Select(g => new SupplierModel
            {
                Name = g.Key,
                Products = g.OrderBy(p => p.Name).ToList()
            })
            .OrderBy(s => s.Name)
            .ToList();


    // ---- row reordering ----
    private void OnReorder(BitDataGridRowReorderEventArgs<Product> e)
    {
        // FromIndex/ToIndex are null when the bound Items isn't an indexable IList<T>; fall back to "?"
        // so the log stays readable instead of rendering an empty position.
        var from = e.FromIndex is int fi ? (fi + 1).ToString() : "?";
        var to = e.ToIndex is int ti ? (ti + 1).ToString() : "?";
        reorderLog = $"{e.DraggedItem.Name} moved from #{from} to #{to}";
    }


    // ---- cell events ----
    private void OnCellClick(BitDataGridCellEventArgs<Product> e)
        => cellEventStatus = $"Clicked {e.ColumnTitle} = \"{e.Value}\" on {e.Item.Name}";

    private void OnCellDoubleClick(BitDataGridCellEventArgs<Product> e)
        => cellEventStatus = $"Double-clicked {e.ColumnTitle} on {e.Item.Name}";

    private void OnCellContextMenu(BitDataGridCellEventArgs<Product> e)
    {
        cellMenuArgs = e;
        // ClientX/Y are viewport coordinates, matching the menu's position:fixed placement.
        cellMenuX = (int)e.Mouse.ClientX;
        cellMenuY = (int)e.Mouse.ClientY;
        cellEventStatus = $"Right-clicked {e.ColumnTitle} on {e.Item.Name}";
        // Move focus into the menu once it renders so keyboard users can operate/dismiss it.
        cellMenuFocusPending = true;
    }

    private void CloseCellMenu() => cellMenuArgs = null;

    private void OnCellMenuKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape") CloseCellMenu();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (cellMenuFocusPending && cellMenuArgs is not null)
        {
            cellMenuFocusPending = false;
            await cellMenuFirstItem.FocusAsync();
        }
    }

    private async Task CopyCellValue()
    {
        if (cellMenuArgs is null) return;
        await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", cellMenuArgs.Value?.ToString() ?? "");
        cellEventStatus = $"Copied \"{cellMenuArgs.Value}\" to the clipboard.";
        cellMenuArgs = null;
    }

    private async Task DeleteCellMenuRow()
    {
        if (cellMenuArgs is null) return;
        cellEventsProducts.Remove(cellMenuArgs.Item);
        cellEventStatus = $"Deleted {cellMenuArgs.Item.Name}.";
        cellMenuArgs = null;
        // The grid caches its processed view; mutating the bound list in place requires an explicit refresh.
        if (cellEventsGrid is not null) await cellEventsGrid.RefreshAsync();
    }


    // ---- variable row height ----
    private float RowHeight(Product p) => p.Price > 500 ? 64f : 36f;
}
