using Bit.BlazorUI.Demo.Client.Core.Components;

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

    // example 4 - editing
    private readonly List<Product> editProducts = SampleData.Generate(25);
    private int nextId;
    private string editStatus = "";

    // example 5 - grouping
    private readonly List<Product> groupProducts = SampleData.Generate(80);

    // example 6 - templates
    private readonly List<Product> templateProducts = SampleData.Generate(30);

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
    private readonly List<Product> infiniteAll = SampleData.Generate(2_000);
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

    // example 16 - cell events
    private readonly List<Product> cellEventsProducts = SampleData.Generate(40);
    private string cellEventStatus = "Click, double-click or right-click any cell.";

    // example 17 - cell navigation
    private readonly List<Product> cellNavProducts = SampleData.Generate(40);

    // example 18 - variable row height
    private readonly List<Product> variableHeightProducts = SampleData.Generate(40);

    // example 19 - empty state
    private readonly List<Product> emptyData = SampleData.Generate(25);
    private readonly List<Product> emptyNone = new();
    private bool emptyHasData;
    private List<Product> EmptyCurrent => emptyHasData ? emptyData : emptyNone;

    // example 20 - theming
    private readonly List<Product> themeProducts = SampleData.Generate(60);
    private string theme = "";
    private bool rtl;
    private bool bordered = true;
    private bool striped = true;


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
        if (!editProducts.Contains(p)) editProducts.Insert(0, p);
        editStatus = $"Saved {p.Name} (#{p.Id}).";
    }

    private void OnDelete(Product p)
    {
        editProducts.Remove(p);
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
        await Task.Delay(250);

        IEnumerable<Product> query = serverAll;

        foreach (var f in request.Filters)
        {
            var term = f.Value?.ToString() ?? "";
            query = f.ColumnId switch
            {
                nameof(Product.Name) => query.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)),
                nameof(Product.Category) => query.Where(p => p.Category.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)),
                nameof(Product.Supplier) => query.Where(p => p.Supplier.Contains(term, StringComparison.OrdinalIgnoreCase)),
                nameof(Product.Price) => query.Where(p => p.Price.ToString().Contains(term)),
                nameof(Product.Stock) => query.Where(p => p.Stock.ToString().Contains(term)),
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
        var total = filtered.Count;
        var items = filtered.Skip(request.Skip).Take(request.Take ?? total).ToList();

        serverLastRequest = $"Last request → skip {request.Skip}, take {request.Take}, sorts: {request.Sorts.Count}, filters: {request.Filters.Count}, total: {total}";
        serverLoading = false;
        return new BitDataGridReadResult<Product>(items, total);
    }


    // ---- infinite scrolling ----
    private async Task<BitDataGridReadResult<Product>> LoadMore(BitDataGridReadRequest request)
    {
        await Task.Delay(350);

        IEnumerable<Product> query = infiniteAll;

        var sort = request.Sorts.FirstOrDefault();
        if (sort is not null)
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
            query = sort.Direction == BitDataGridSortDirection.Descending
                ? query.OrderByDescending(key)
                : query.OrderBy(key);
        }

        var batch = query.Skip(request.Skip).Take(request.Take ?? 40).ToList();

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
        reorderLog = $"{e.DraggedItem.Name} moved from #{e.FromIndex + 1} to #{e.ToIndex + 1}";
    }


    // ---- cell events ----
    private void OnCellClick(BitDataGridCellEventArgs<Product> e)
        => cellEventStatus = $"Clicked {e.ColumnTitle} = \"{e.Value}\" on {e.Item.Name}";

    private void OnCellDoubleClick(BitDataGridCellEventArgs<Product> e)
        => cellEventStatus = $"Double-clicked {e.ColumnTitle} on {e.Item.Name}";

    private void OnCellContextMenu(BitDataGridCellEventArgs<Product> e)
        => cellEventStatus = $"Right-clicked {e.ColumnTitle} on {e.Item.Name} at ({e.Mouse.ClientX}, {e.Mouse.ClientY})";


    // ---- variable row height ----
    private float RowHeight(Product p) => p.Price > 500 ? 64f : 36f;
}
