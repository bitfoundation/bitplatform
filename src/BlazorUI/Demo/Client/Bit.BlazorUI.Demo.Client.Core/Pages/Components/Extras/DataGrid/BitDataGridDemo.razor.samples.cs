namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public partial class BitDataGridDemo
{
    // ------------------------------------------------------------------
    // Shared supporting types used by the C# snippets below. They are
    // appended to each example's CsharpCode so every snippet is complete
    // and can be copied & pasted into a project as-is.
    // ------------------------------------------------------------------

    private const string ProductModelCode = @"

public enum Category { Electronics, Books, Clothing, Home, Toys, Sports, Grocery }

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = """";
    public Category Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public double Rating { get; set; }
    public bool Discontinued { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Supplier { get; set; } = """";
}";

    private const string SampleDataCode = @"

// Deterministic generator so the demo data is reproducible.
public static class SampleData
{
    static readonly string[] Adjectives =
        { ""Ultra"", ""Premium"", ""Eco"", ""Smart"", ""Classic"", ""Pro"", ""Mini"", ""Mega"", ""Vintage"", ""Modern"", ""Deluxe"", ""Compact"" };
    static readonly string[] Nouns =
        { ""Widget"", ""Gadget"", ""Speaker"", ""Notebook"", ""Jacket"", ""Lamp"", ""Blender"", ""Drone"", ""Backpack"", ""Sneaker"", ""Camera"", ""Mug"" };
    static readonly string[] Suppliers =
        { ""Acme Corp"", ""Globex"", ""Initech"", ""Umbrella"", ""Soylent"", ""Stark Industries"", ""Wayne Enterprises"", ""Wonka Inc"" };

    public static List<Product> Generate(int count, int seed = 42)
    {
        var rng = new Random(seed);
        var categories = Enum.GetValues<Category>();
        var list = new List<Product>(count);
        var referenceDate = new DateTime(2024, 1, 1);
        for (int i = 1; i <= count; i++)
        {
            list.Add(new Product
            {
                Id = i,
                Name = $""{Adjectives[rng.Next(Adjectives.Length)]} {Nouns[rng.Next(Nouns.Length)]} {rng.Next(100, 999)}"",
                Category = categories[rng.Next(categories.Length)],
                Price = Math.Round((decimal)(rng.NextDouble() * 990 + 5), 2),
                Stock = rng.Next(0, 500),
                Rating = Math.Round(rng.NextDouble() * 4 + 1, 1),
                Discontinued = rng.Next(0, 5) == 0,
                ReleaseDate = referenceDate.AddDays(-rng.Next(0, 2000)),
                Supplier = Suppliers[rng.Next(Suppliers.Length)]
            });
        }
        return list;
    }
}";

    private const string PersianSampleDataCode = @"

// Deterministic generator that produces Persian sample data for the RTL demo.
public static class SampleData
{
    static readonly string[] Adjectives =
        { ""فوق‌العاده"", ""ممتاز"", ""اقتصادی"", ""هوشمند"", ""کلاسیک"", ""حرفه‌ای"", ""کوچک"", ""بزرگ"", ""قدیمی"", ""مدرن"", ""لوکس"", ""فشرده"" };
    static readonly string[] Nouns =
        { ""ویجت"", ""گجت"", ""بلندگو"", ""دفترچه"", ""ژاکت"", ""چراغ"", ""مخلوط‌کن"", ""پهپاد"", ""کوله‌پشتی"", ""کفش"", ""دوربین"", ""لیوان"" };
    static readonly string[] Suppliers =
        { ""شرکت آلفا"", ""گلوبکس"", ""اینیتک"", ""آمبرلا"", ""سویلنت"", ""صنایع استارک"", ""شرکت وین"", ""ونکا"" };

    public static List<Product> GeneratePersian(int count, int seed = 42)
    {
        var rng = new Random(seed);
        var categories = Enum.GetValues<Category>();
        var list = new List<Product>(count);
        var referenceDate = new DateTime(2024, 1, 1);
        for (int i = 1; i <= count; i++)
        {
            list.Add(new Product
            {
                Id = i,
                Name = $""{Adjectives[rng.Next(Adjectives.Length)]} {Nouns[rng.Next(Nouns.Length)]} {rng.Next(100, 999)}"",
                Category = categories[rng.Next(categories.Length)],
                Price = Math.Round((decimal)(rng.NextDouble() * 990 + 5), 2),
                Stock = rng.Next(0, 500),
                Rating = Math.Round(rng.NextDouble() * 4 + 1, 1),
                Discontinued = rng.Next(0, 5) == 0,
                ReleaseDate = referenceDate.AddDays(-rng.Next(0, 2000)),
                Supplier = Suppliers[rng.Next(Suppliers.Length)]
            });
        }
        return list;
    }
}";

    private const string FileSystemDataCode = @"

public class FileNode
{
    public int Id { get; set; }
    public string Name { get; set; } = """";
    public string Kind { get; set; } = ""Folder"";
    public long Size { get; set; }
    public DateTime Modified { get; set; }
    public List<FileNode> Children { get; set; } = new();
}

public static class FileSystemData
{
    public static List<FileNode> Build()
    {
        var id = 0;
        var baseDate = new DateTime(2025, 1, 1);

        FileNode Folder(string name, params FileNode[] children)
        {
            var node = new FileNode { Id = ++id, Name = name, Kind = ""Folder"", Modified = baseDate.AddDays(id), Children = children.ToList() };
            node.Size = node.Children.Sum(c => c.Size);
            return node;
        }

        FileNode File(string name, long size) => new() { Id = ++id, Name = name, Kind = ""File"", Size = size, Modified = baseDate.AddDays(id) };

        return new List<FileNode>
        {
            Folder(""src"",
                Folder(""BitDataGrid"",
                    File(""BitDataGrid.razor"", 24_500),
                    File(""BitDataGrid.razor.cs"", 41_200),
                    Folder(""Models"",
                        File(""BitDataGridColumnAlign.cs"", 320),
                        File(""BitDataGridSortDescriptor.cs"", 540),
                        File(""BitDataGridFilterOperator.cs"", 610)),
                    Folder(""Infrastructure"",
                        File(""BitDataGridDataProcessor.cs"", 8_900),
                        File(""BitDataGridPropertyAccessor.cs"", 3_400))),
                Folder(""BitDataGrid.Demo"",
                    File(""Program.cs"", 1_200),
                    Folder(""Components"",
                        File(""App.razor"", 760),
                        File(""Routes.razor"", 280)))),
            Folder(""docs"",
                File(""README.md"", 6_400),
                File(""CHANGELOG.md"", 2_100)),
            Folder(""assets"",
                File(""logo.svg"", 4_800),
                File(""styles.css"", 12_300),
                File(""favicon.ico"", 1_150)),
            File(""LICENSE"", 1_070),
            File("".gitignore"", 410)
        };
    }
}";

    private const string SupplierModelCode = @"

public sealed class SupplierModel
{
    public string Name { get; set; } = """";
    public List<Product> Products { get; set; } = new();
    public int ProductCount => Products.Count;
    public int TotalStock => Products.Sum(p => p.Stock);
    public decimal AveragePrice => Products.Count == 0 ? 0 : Math.Round(Products.Average(p => p.Price), 2);
}";


    private readonly string example1RazorCode = @"
@* TItem is inferred from the grid for all columns; Property is the strongly typed
   alternative to Field=""Name""; the Columns wrapper element is an optional alias
   of the grid's child content. *@
<BitDataGrid Items=""@products"" Height=""430px"" MultiSort=""true"">
    <Columns>
        <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitDataGridColumnAlign.Right"" />
        <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
        <BitDataGridColumn Property=""p => p.Category"" />
        <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitDataGridColumnAlign.Right"" />
        <BitDataGridColumn Property=""p => p.Stock"" Align=""BitDataGridColumnAlign.Right"" />
        <BitDataGridColumn Property=""p => p.Rating"" Format=""N1"" Align=""BitDataGridColumnAlign.Right"" />
    </Columns>
</BitDataGrid>";
    private readonly string example1CsharpCode = @"
private List<Product> products = SampleData.Generate(50);" + ProductModelCode + SampleDataCode;

    private readonly string example2RazorCode = @"
<BitDataGrid Items=""@products"" Height=""430px""
             Filterable=""true"" Pageable=""true"" PageSize=""10""
             PagerPosition=""BitDataGridPagerPosition.Bottom""
             ShowToolbar=""true"" ShowCsvExport=""true"">
    <BitDataGridColumn Field=""Id"" Title=""ID"" Filterable=""false"" />
    <BitDataGridColumn Field=""Name"" />
    <BitDataGridColumn Field=""Category"" />
    <BitDataGridColumn Field=""Supplier"" />
    <BitDataGridColumn Field=""Price"" Format=""C2"" Align=""BitDataGridColumnAlign.Right"" />
    <BitDataGridColumn Field=""Stock"" Align=""BitDataGridColumnAlign.Right"" />
</BitDataGrid>";
    private readonly string example2CsharpCode = @"
private List<Product> products = SampleData.Generate(200);" + ProductModelCode + SampleDataCode;

    private readonly string example3RazorCode = @"
<BitDataGrid Items=""@products"" Height=""420px""
             SelectionMode=""BitDataGridSelectionMode.Multiple"" @bind-SelectedItems=""selected""
             Pageable=""true"" PageSize=""10"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Align=""BitDataGridColumnAlign.Right"" />
    <BitDataGridColumn Property=""p => p.Name"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitDataGridColumnAlign.Right"" />
</BitDataGrid>";
    private readonly string example3CsharpCode = @"
private List<Product> products = SampleData.Generate(60);
private IReadOnlyList<Product> selected = new List<Product>();" + ProductModelCode + SampleDataCode;

    private readonly string example4RazorCode = @"
@* TItem stays explicit here: EventCallback parameters (OnRowSave/OnRowDelete/OnRowCreate)
   bound to method groups cannot participate in generic type inference. *@
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""460px""
             Editable=""true"" NewItemFactory=""CreateProduct""
             OnRowSave=""OnSave"" OnRowDelete=""OnDelete"" OnRowCreate=""OnCreate""
             ShowToolbar=""true"" Pageable=""true"" PageSize=""10"" KeyField=""p => p.Id"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Editable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
    <BitDataGridColumn Property=""p => p.Discontinued"" />
</BitDataGrid>";
    private readonly string example4CsharpCode = @"
private List<Product> products = SampleData.Generate(25);
private int nextId;

protected override void OnInitialized() => nextId = products.Max(p => p.Id) + 1;

private Product CreateProduct() => new()
{
    Id = nextId++,
    Name = ""New product"",
    Category = Category.Electronics,
    ReleaseDate = DateTime.Today
};
private void OnCreate(Product p) { /* called when a new row starts being added */ }
private void OnSave(Product p)
{
    // The grid is keyed by Id (KeyField=""p => p.Id"") and hands back an edited copy, so match on Id
    // rather than the object reference: update the existing row in place, or insert a brand-new one.
    var index = products.FindIndex(x => x.Id == p.Id);
    if (index >= 0) products[index] = p;
    else products.Insert(0, p);
}
private void OnDelete(Product p) => products.RemoveAll(x => x.Id == p.Id);" + ProductModelCode + SampleDataCode;

    private readonly string example5RazorCode = @"
<BitDataGrid Items=""@products"" Height=""500px""
             Groupable=""true"" ShowFooter=""true"" Sortable=""true"">
    <BitDataGridColumn Property=""p => p.Name"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Supplier"" AggregateBy=""rows => DistinctSuppliers(rows)"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitDataGridColumnAlign.Right""
                       Aggregate=""BitDataGridAggregateType.Sum"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitDataGridColumnAlign.Right""
                       Aggregate=""BitDataGridAggregateType.Average"" AggregateFormat=""N0"" Groupable=""false"" />
</BitDataGrid>";
    private readonly string example5CsharpCode = @"
private List<Product> products = SampleData.Generate(80);

// A custom aggregate: receives the rows of the footer's view (or of each group) and returns any value.
private object? DistinctSuppliers(IReadOnlyList<Product> rows)
    => $""{rows.Select(p => p.Supplier).Distinct().Count()} distinct"";" + ProductModelCode + SampleDataCode;

    private readonly string example6RazorCode = @"
<BitDataGrid Items=""@products"" Height=""470px"" Sortable=""true"" ShowFooter=""true"">
    <DetailTemplate Context=""p"">
        <div>Supplier: @p.Supplier</div>
    </DetailTemplate>
    <Columns>
        <BitDataGridColumn Property=""p => p.Name"">
            <HeaderTemplate>📦 Product</HeaderTemplate>
        </BitDataGridColumn>
        <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitDataGridColumnAlign.Right""
                           Aggregate=""BitDataGridAggregateType.Sum"">
            <FooterTemplate Context=""agg"">Total: @agg.FormattedValue</FooterTemplate>
        </BitDataGridColumn>
        <BitDataGridColumn Property=""p => p.Stock"" Align=""BitDataGridColumnAlign.Right"">
            <Template Context=""p"">@p.Stock in stock</Template>
        </BitDataGridColumn>
        @* A template-only column (no Field) becomes sortable through its SortBy key selector. *@
        <BitDataGridColumn ColumnId=""Value"" Title=""Value"" Align=""BitDataGridColumnAlign.Right""
                           SortBy=""@(p => p.Price * p.Stock)"">
            <Template Context=""p"">@((p.Price * p.Stock).ToString(""C0""))</Template>
        </BitDataGridColumn>
    </Columns>
</BitDataGrid>";
    private readonly string example6CsharpCode = @"
private List<Product> products = SampleData.Generate(30);" + ProductModelCode + SampleDataCode;

    private readonly string example7RazorCode = @"
<BitDataGrid Items=""@products"" Height=""430px"" Resizable=""true"" Reorderable=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""80px"" Frozen=""true"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" Frozen=""true"" />
    <BitDataGridColumn Property=""p => p.Category"" Width=""160px"" />
    <BitDataGridColumn Property=""p => p.Price"" Width=""160px"" Format=""C2"" Align=""BitDataGridColumnAlign.Right"" />
</BitDataGrid>";
    private readonly string example7CsharpCode = @"
private List<Product> products = SampleData.Generate(40);" + ProductModelCode + SampleDataCode;

    private readonly string example8RazorCode = @"
<BitDataGrid Items=""@products"" Height=""460px"" Sortable=""true"" Bordered=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" />
    <BitDataGridColumn Property=""p => p.Name"" Group=""Identity"" />
    <BitDataGridColumn Property=""p => p.Category"" Group=""Identity"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Group=""Commercials"" />
    <BitDataGridColumn Property=""p => p.Stock"" Group=""Commercials"" />
    <BitDataGridColumn Property=""p => p.Rating"" Format=""N1"" Group=""Quality"" />
    <BitDataGridColumn Property=""p => p.Supplier"" Group=""Quality"" />
</BitDataGrid>";
    private readonly string example8CsharpCode = @"
private List<Product> products = SampleData.Generate(40);" + ProductModelCode + SampleDataCode;

    private readonly string example9RazorCode = @"
<BitDataGrid Items=""@products"" Height=""460px"" Bordered=""true"">
    <BitDataGridColumn Property=""p => p.Name"" ColSpan=""p => NameSpan(p)"">
        <Template Context=""p"">@p.Name</Template>
    </BitDataGridColumn>
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" ColSpan=""p => PriceSpan(p)"" />
    <BitDataGridColumn Property=""p => p.Stock"" />
</BitDataGrid>";
    private readonly string example9CsharpCode = @"
private List<Product> products = SampleData.Generate(40);

private int? NameSpan(Product p) => p.Discontinued ? 2 : null;
private int? PriceSpan(Product p) => p.Price > 800 ? 2 : null;" + ProductModelCode + SampleDataCode;

    private readonly string example10RazorCode = @"
<BitDataGrid Items=""@products"" Height=""520px""
             Virtualize=""true"" RowHeight=""36"" Sortable=""true"" Filterable=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example10CsharpCode = @"
private List<Product> products = SampleData.Generate(10_000);" + ProductModelCode + SampleDataCode;

    private readonly string example11RazorCode = @"
<BitDataGrid OnRead=""LoadData"" Height=""430px""
             Pageable=""true"" PageSize=""10"" Sortable=""true"" Filterable=""true"" Loading=""loading"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example11CsharpCode = @"
private bool loading;
private readonly List<Product> all = SampleData.Generate(523);

private async Task<BitDataGridReadResult<Product>> LoadData(BitDataGridReadRequest request)
{
    loading = true;
    await InvokeAsync(StateHasChanged); // re-render so the loading indicator shows
    try
    {
        await Task.Delay(250, request.CancellationToken); // simulate a backend round-trip

        IEnumerable<Product> query = all;

        // filtering — honor the operator the grid emits, not just contains/equals
        foreach (var f in request.Filters)
        {
            query = f.ColumnId switch
            {
                // text column uses the string operators from the grid's text filter editor
                nameof(Product.Name) => query.Where(p => MatchText(p.Name, f)),
                // numeric columns receive a typed value with a comparison operator, so honor the
                // requested equality/range operator instead of a hard-coded equals
                nameof(Product.Price) => query.Where(p => MatchComparable(p.Price, f)),
                nameof(Product.Id) => query.Where(p => MatchComparable(p.Id, f)),
                _ => query
            };
        }

        // sorting (honor every active sort descriptor, not just the first)
        IOrderedEnumerable<Product>? ordered = null;
        foreach (var sort in request.Sorts)
        {
            Func<Product, object> key = sort.ColumnId switch
            {
                nameof(Product.Name) => p => p.Name,
                nameof(Product.Price) => p => p.Price,
                _ => p => p.Id
            };
            if (ordered is null)
                ordered = sort.Direction == BitDataGridSortDirection.Descending
                    ? query.OrderByDescending(key)
                    : query.OrderBy(key);
            else
                ordered = sort.Direction == BitDataGridSortDirection.Descending
                    ? ordered.ThenByDescending(key)
                    : ordered.ThenBy(key);
        }
        if (ordered is not null) query = ordered;

        // paging
        var filtered = query.ToList();
        var items = filtered.Skip(request.Skip).Take(request.Take ?? filtered.Count).ToList();

        // A superseded request can finish filtering/sorting/paging after a newer one started; bail
        // out before returning so the grid never receives stale rows for a cancelled load.
        request.CancellationToken.ThrowIfCancellationRequested();

        return new BitDataGridReadResult<Product>(items, filtered.Count);
    }
    finally
    {
        // Only the active request should clear the loading state; a superseded request observes a
        // cancelled token, so skip the reset and let the newer in-flight load own the indicator.
        if (!request.CancellationToken.IsCancellationRequested)
        {
            loading = false;
            await InvokeAsync(StateHasChanged); // re-render after the load completes (runs as a callback)
        }
    }
}

// Applies a text-column filter the way the grid's text editor emits it.
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

// Applies a non-text-column filter against the typed value the grid emits so the requested
// equality/range operator is honored instead of a substring match on ToString().
private static bool MatchComparable<T>(T value, BitDataGridFilterDescriptor f) where T : IComparable
{
    if (f.Operator is BitDataGridFilterOperator.IsEmpty) return value is null;
    if (f.Operator is BitDataGridFilterOperator.IsNotEmpty) return value is not null;
    if (f.Value is null) return true;
    if (f.Value is not T typed) return true;

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
}" + ProductModelCode + SampleDataCode;

    private readonly string example12RazorCode = @"
<BitDataGrid OnLoadMore=""LoadMore"" LoadMoreBatchSize=""40""
             Height=""520px"" RowHeight=""40"" Sortable=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" />
    <BitDataGridColumn Property=""p => p.Name"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example12CsharpCode = @"
private readonly List<Product> all = SampleData.Generate(2_017);

private async Task<BitDataGridReadResult<Product>> LoadMore(BitDataGridReadRequest request)
{
    await Task.Delay(350, request.CancellationToken); // simulate a backend round-trip

    IEnumerable<Product> query = all;

    // Apply every active sort descriptor before paging out the batch.
    IOrderedEnumerable<Product>? ordered = null;
    foreach (var sort in request.Sorts)
    {
        Func<Product, object> key = sort.ColumnId switch
        {
            nameof(Product.Name) => p => p.Name,
            nameof(Product.Price) => p => p.Price,
            _ => p => p.Id
        };
        if (ordered is null)
            ordered = sort.Direction == BitDataGridSortDirection.Descending
                ? query.OrderByDescending(key)
                : query.OrderBy(key);
        else
            ordered = sort.Direction == BitDataGridSortDirection.Descending
                ? ordered.ThenByDescending(key)
                : ordered.ThenBy(key);
    }
    if (ordered is not null) query = ordered;

    // Take is the batch size while scrolling; null means ""all rows"" (issued by CSV/Excel exports).
    var batch = query.Skip(request.Skip).Take(request.Take ?? all.Count).ToList();

    // Drop a superseded batch before returning so a cancelled load never yields stale rows.
    request.CancellationToken.ThrowIfCancellationRequested();

    // Pass 0 as the total count to signal there is no known total (infinite scrolling).
    return new BitDataGridReadResult<Product>(batch, 0);
}" + ProductModelCode + SampleDataCode;

    private readonly string example13RazorCode = @"
<BitDataGrid Items=""@roots"" Height=""460px"" Sortable=""true""
             ChildrenSelector=""n => n.Children"" TreeInitiallyExpanded=""true""
             KeyField=""n => n.Id"" @ref=""grid"">
    <BitDataGridColumn Property=""p => p.Name"" Width=""320px"" />
    <BitDataGridColumn Property=""p => p.Kind"" Title=""Type"" />
    <BitDataGridColumn Property=""p => p.Size"" Format=""N0"" Align=""BitDataGridColumnAlign.Right"" />
</BitDataGrid>";
    private readonly string example13CsharpCode = @"
private List<FileNode> roots = FileSystemData.Build();
private BitDataGrid<FileNode>? grid;

private async Task ExpandAll() { if (grid is not null) await grid.ExpandAllAsync(); }
private async Task CollapseAll() { if (grid is not null) await grid.CollapseAllAsync(); }" + FileSystemDataCode;

    private readonly string example14RazorCode = @"
<BitDataGrid Items=""@suppliers"" Height=""520px"" Sortable=""true"">
    <DetailTemplate Context=""supplier"">
        <BitDataGrid Items=""supplier.Products"" Sortable=""true"">
            <BitDataGridColumn Property=""p => p.Name"" />
            <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
        </BitDataGrid>
    </DetailTemplate>
    <Columns>
        <BitDataGridColumn Property=""p => p.Name"" Title=""Supplier"" />
        <BitDataGridColumn Property=""p => p.ProductCount"" Title=""Products"" />
    </Columns>
</BitDataGrid>";
    private readonly string example14CsharpCode = @"
private List<SupplierModel> suppliers = BuildSuppliers();

private static List<SupplierModel> BuildSuppliers() =>
    SampleData.Generate(240)
        .GroupBy(p => p.Supplier)
        .Select(g => new SupplierModel { Name = g.Key, Products = g.OrderBy(p => p.Name).ToList() })
        .OrderBy(s => s.Name)
        .ToList();" + SupplierModelCode + ProductModelCode + SampleDataCode;

    private readonly string example15RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""460px""
             RowReorderable=""true"" OnRowReorder=""OnReorder"" Sortable=""false"">
    <BitDataGridColumn Property=""p => p.Name"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example15CsharpCode = @"
private List<Product> products = SampleData.Generate(12);

private void OnReorder(BitDataGridRowReorderEventArgs<Product> e)
{
    // e.DraggedItem, e.TargetItem, e.FromIndex, e.ToIndex
    // FromIndex/ToIndex are int? and may be null when Items is not an indexable IList<T>.
}" + ProductModelCode + SampleDataCode;

    private readonly string example16RazorCode = @"
<style>
    /* The invisible overlay catches the click/right-click that dismisses the menu. */
    .ctx-menu-overlay {
        position: fixed;
        inset: 0;
        z-index: 999;
    }

    .ctx-menu {
        position: fixed;
        z-index: 1000;
        min-width: 180px;
        padding: 4px;
        display: flex;
        flex-direction: column;
        background: var(--bit-clr-bg-pri);
        border: 1px solid var(--bit-clr-brd-ter);
        border-radius: var(--bit-shp-brd-radius);
        box-shadow: 0 4px 14px rgba(0, 0, 0, 0.25);
    }
</style>

<BitDataGrid @ref=""grid"" TItem=""Product"" Items=""@products"" Height=""420px""
             OnCellClick=""OnCellClick""
             OnCellDoubleClick=""OnCellDoubleClick""
             OnCellContextMenu=""OnCellContextMenu"">
    <BitDataGridColumn Property=""p => p.Name"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
</BitDataGrid>

@if (cellMenuArgs is not null)
{
    <div class=""ctx-menu-overlay""
         @onclick=""CloseCellMenu""
         @oncontextmenu=""CloseCellMenu"" @oncontextmenu:preventDefault=""true""></div>
    <div class=""ctx-menu"" role=""menu"" style=""left:@(cellMenuX)px;top:@(cellMenuY)px""
         @onkeydown=""OnCellMenuKeyDown"">
        <div>@cellMenuArgs.Item.Name — @cellMenuArgs.ColumnTitle</div>
        <button type=""button"" role=""menuitem"" @ref=""cellMenuFirstItem"" @onclick=""CopyCellValue"">Copy value</button>
        <button type=""button"" role=""menuitem"" @onclick=""DeleteCellMenuRow"">Delete row</button>
    </div>
}";
    private readonly string example16CsharpCode = @"
private List<Product> products = SampleData.Generate(40);
private BitDataGrid<Product>? grid;
private BitDataGridCellEventArgs<Product>? cellMenuArgs;
private int cellMenuX;
private int cellMenuY;
private ElementReference cellMenuFirstItem;
private bool cellMenuFocusPending;

private void OnCellClick(BitDataGridCellEventArgs<Product> e) { /* e.Item, e.ColumnTitle, e.Value */ }
private void OnCellDoubleClick(BitDataGridCellEventArgs<Product> e) { /* e.Item, e.ColumnTitle, e.Value */ }

private void OnCellContextMenu(BitDataGridCellEventArgs<Product> e)
{
    cellMenuArgs = e;
    // ClientX/Y are viewport coordinates, matching the menu's position:fixed placement.
    cellMenuX = (int)e.Mouse.ClientX;
    cellMenuY = (int)e.Mouse.ClientY;
    // Move focus into the menu once it renders so keyboard users can operate/dismiss it.
    cellMenuFocusPending = true;
}

private void CloseCellMenu() => cellMenuArgs = null;

private void OnCellMenuKeyDown(KeyboardEventArgs e)
{
    if (e.Key == ""Escape"") CloseCellMenu();
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
    await JSRuntime.InvokeVoidAsync(""navigator.clipboard.writeText"", cellMenuArgs.Value?.ToString() ?? """");
    cellMenuArgs = null;
}

private async Task DeleteCellMenuRow()
{
    if (cellMenuArgs is null) return;
    products.Remove(cellMenuArgs.Item);
    cellMenuArgs = null;
    // The grid caches its processed view; mutating the bound list in place requires an explicit refresh.
    if (grid is not null) await grid.RefreshAsync();
}" + ProductModelCode + SampleDataCode;

    private readonly string example17RazorCode = @"
<BitDataGrid Items=""@products"" Height=""460px""
             CellNavigation=""true"" Sortable=""true"" Editable=""true"" OnRowSave=""(Product _) => {}""
             OnRowDelete=""(Product p) => products.Remove(p)"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Editable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example17CsharpCode = @"
private List<Product> products = SampleData.Generate(40);" + ProductModelCode + SampleDataCode;

    private readonly string example18RazorCode = @"
<BitDataGrid Items=""@products"" Height=""480px"" Sortable=""true""
             RowHeightSelector=""RowHeight"">
    <BitDataGridColumn Property=""p => p.Name"">
        <Template Context=""p""><strong>@p.Name</strong></Template>
    </BitDataGridColumn>
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example18CsharpCode = @"
private List<Product> products = SampleData.Generate(40);

private float RowHeight(Product p) => p.Price > 500 ? 64f : 36f;" + ProductModelCode + SampleDataCode;

    private readonly string example19RazorCode = @"
<BitDataGrid Items=""@items"" Height=""320px"" Sortable=""true"">
    <EmptyTemplate>
        <div>Nothing here yet. Try loading the sample data to populate the grid.</div>
    </EmptyTemplate>
    <Columns>
        <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" />
        <BitDataGridColumn Property=""p => p.Name"" />
    </Columns>
</BitDataGrid>";
    private readonly string example19CsharpCode = @"
private List<Product> items = new(); // empty" + ProductModelCode;

    private readonly string example20RazorCode = @"
<BitDataGrid Items=""@products"" Height=""420px""
             Bordered=""@bordered"" Striped=""@striped""
             Sortable=""true"" Pageable=""true"" PageSize=""8"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Frozen=""true"" />
    <BitDataGridColumn Property=""p => p.Name"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example20CsharpCode = @"
private List<Product> products = SampleData.Generate(60);
private bool bordered = true;
private bool striped = true;" + ProductModelCode + SampleDataCode;

    private readonly string example21RazorCode = @"
<BitDataGrid Items=""@products"" Height=""420px""
             Direction=""BitDir.Rtl""
             Sortable=""true"" Pageable=""true"" PageSize=""8"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""شناسه"" Frozen=""true"" />
    <BitDataGridColumn Property=""p => p.Name"" Title=""نام"" />
    <BitDataGridColumn Property=""p => p.Category"" Title=""دسته‌بندی"">
        <Template Context=""product"">@CategoryFa(product.Category)</Template>
    </BitDataGridColumn>
    <BitDataGridColumn Property=""p => p.Price"" Title=""قیمت"" Format=""C2"" />
    <BitDataGridColumn Property=""p => p.Stock"" Title=""موجودی"" />
</BitDataGrid>";
    private readonly string example21CsharpCode = @"
private List<Product> products = SampleData.GeneratePersian(60);

private static string CategoryFa(Category category) => category switch
{
    Category.Electronics => ""الکترونیک"",
    Category.Books => ""کتاب"",
    Category.Clothing => ""پوشاک"",
    Category.Home => ""خانه"",
    Category.Toys => ""اسباب‌بازی"",
    Category.Sports => ""ورزش"",
    Category.Grocery => ""خواربار"",
    _ => category.ToString()
};" + ProductModelCode + PersianSampleDataCode;

    private readonly string example22RazorCode = @"
<BitDataGrid Items=""@products"" Height=""430px""
             Filterable=""true"" FilterOperators=""true"" ShowToolbar=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    @* A column can override the grid-level setting and keep the fixed default filter. *@
    <BitDataGridColumn Property=""p => p.Supplier"" FilterOperators=""false"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
    <BitDataGridColumn Property=""p => p.Stock"" />
    <BitDataGridColumn Property=""p => p.ReleaseDate"" Title=""Released"" Format=""yyyy-MM-dd"" />
</BitDataGrid>";
    private readonly string example22CsharpCode = @"
private List<Product> products = SampleData.Generate(150);" + ProductModelCode + SampleDataCode;

    private readonly string example23RazorCode = @"
<BitDataGrid Items=""@products"" Height=""430px""
             Editable=""true"" KeyField=""p => p.Id"" Pageable=""true"" PageSize=""8"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Editable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" Validate=""(p, v) => ValidateName(p, v)"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Validate=""(p, v) => ValidatePrice(p, v)"" />
    <BitDataGridColumn Property=""p => p.Stock"" Validate=""(p, v) => ValidateStock(p, v)"" />
</BitDataGrid>";
    private readonly string example23CsharpCode = @"
private List<Product> products = SampleData.Generate(30);

// Validators receive the row and the proposed (type-converted) value.
// Return an error message to reject the edit (blocking Save), or null to accept.
private string? ValidateName(Product product, object? value)
    => string.IsNullOrWhiteSpace(value as string) ? ""Name is required."" : null;

private string? ValidatePrice(Product product, object? value)
    => value is decimal price && price < 0 ? ""Price cannot be negative."" : null;

private string? ValidateStock(Product product, object? value)
    => value is int stock && stock < 0 ? ""Stock cannot be negative."" : null;" + ProductModelCode + SampleDataCode;

    private readonly string example24RazorCode = @"
<BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
    <BitButton OnClick=""SaveGridState"">Save state</BitButton>
    <BitButton OnClick=""RestoreGridState"" IsEnabled=""savedState is not null"" Variant=""BitVariant.Outline"">Restore state</BitButton>
    <BitText>@stateStatus</BitText>
</BitStack>

<BitDataGrid @ref=""grid"" Items=""@products"" Height=""430px""
             Sortable=""true"" Filterable=""true"" Resizable=""true""
             Pageable=""true"" PageSize=""10"" ShowToolbar=""true"" ShowColumnChooser=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
    <BitDataGridColumn Property=""p => p.Stock"" />
</BitDataGrid>";
    private readonly string example24CsharpCode = @"
private List<Product> products = SampleData.Generate(120);
private BitDataGrid<Product>? grid;
private BitDataGridState? savedState;
private string stateStatus = ""Adjust the grid, then save its state."";

private void SaveGridState()
{
    // The snapshot is serializable — persist it to local storage or a user-preferences store.
    savedState = grid?.GetState();
    stateStatus = savedState is null
        ? ""Nothing to save yet.""
        : $""Saved: page {savedState.CurrentPage}, {savedState.Sorts.Count} sort(s), {savedState.Filters.Count} filter(s)."";
}

private async Task RestoreGridState()
{
    if (grid is null || savedState is null) return;
    await grid.ApplyStateAsync(savedState);
    stateStatus = ""State restored."";
}" + ProductModelCode + SampleDataCode;

    private readonly string example25RazorCode = @"
<BitDataGrid OnRead=""LoadVirtualServerData"" Virtualize=""true""
             Height=""480px"" RowHeight=""40"" Sortable=""true"" Filterable=""true"" ShowFooter=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""90px"" Frozen=""true"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""240px"" />
    <BitDataGridColumn Property=""p => p.Category"" Width=""140px"" />
    <BitDataGridColumn Property=""p => p.Supplier"" Width=""180px"" />
    <BitDataGridColumn Property=""p => p.Price"" Width=""140px"" Format=""C2""
                       Aggregate=""BitDataGridAggregateType.Sum"" AggregateFormat=""C0"" />
    <BitDataGridColumn Property=""p => p.Stock"" Width=""120px"" />
    <BitDataGridColumn Property=""p => p.Rating"" Width=""110px"" Format=""N1"" FrozenEnd=""true"" />
</BitDataGrid>";
    private readonly string example25CsharpCode = @"
private List<Product> all = SampleData.Generate(100_000);

private async Task<BitDataGridReadResult<Product>> LoadVirtualServerData(BitDataGridReadRequest request)
{
    // Simulate backend latency. Superseded scroll windows are cancelled by the grid; let the
    // OperationCanceledException propagate so the grid discards the stale read — returning an
    // empty result instead would be rendered as real data and blank the viewport.
    await Task.Delay(150, request.CancellationToken);

    IEnumerable<Product> query = all;
    // ...apply request.Filters and request.Sorts (see the Server-side data example)...

    var filtered = query.ToList();
    var items = filtered.Skip(request.Skip).Take(request.Take ?? filtered.Count).ToList();

    // Aggregates computed over the WHOLE filtered dataset (not just the returned window),
    // so the footer shows a real grand total instead of a per-window number.
    var priceSum = filtered.Sum(p => p.Price);
    var aggregates = new List<BitDataGridAggregateResult>
    {
        new()
        {
            ColumnId = nameof(Product.Price),
            Type = BitDataGridAggregateType.Sum,
            Value = priceSum,
            FormattedValue = priceSum.ToString(""C0"")
        }
    };

    return new BitDataGridReadResult<Product>(items, filtered.Count) { Aggregates = aggregates };
}" + ProductModelCode + SampleDataCode;

    private readonly string example26RazorCode = @"
<BitDataGrid Items=""@products"" Height=""420px""
             Direction=""BitDir.Rtl"" Strings=""@persianStrings""
             Filterable=""true"" Pageable=""true"" PageSize=""8"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""شناسه"" Width=""90px"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Title=""نام"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Price"" Title=""قیمت"" Format=""C2"" />
    <BitDataGridColumn Property=""p => p.Stock"" Title=""موجودی"" />
</BitDataGrid>";
    private readonly string example26CsharpCode = @"
private List<Product> products = SampleData.GeneratePersian(60);

// Every user-visible string has an English default; override what you need.
private readonly BitDataGridStrings persianStrings = new()
{
    EmptyText = ""رکوردی برای نمایش وجود ندارد."",
    LoadingText = ""در حال بارگذاری…"",
    FilterPlaceholder = ""فیلتر…"",
    FilterAllText = ""همه"",
    PagerRangeFormat = ""{0}–{1} از {2}"",
    PagerPageFormat = ""صفحهٔ {0} از {1}"",
    PerPageFormat = ""{0} در صفحه"",
    RowsPerPageLabel = ""تعداد ردیف در صفحه"",
    FirstPageLabel = ""صفحهٔ اول"",
    PreviousPageLabel = ""صفحهٔ قبل"",
    NextPageLabel = ""صفحهٔ بعد"",
    LastPageLabel = ""صفحهٔ آخر"",
    ClearFiltersText = ""حذف فیلترها"",
};" + ProductModelCode + PersianSampleDataCode;

    private readonly string example27RazorCode = @"
<BitDataGrid Items=""@products"" Height=""430px""
             Sortable=""true"" Filterable=""true"" Pageable=""true"" PageSize=""10"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Supplier"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
    <BitDataGridColumn Property=""p => p.Stock"" />
</BitDataGrid>";
    private readonly string example27CsharpCode = @"
// Any IQueryable works — filtering, sorting and paging are composed as expression trees the
// provider executes at the source, so with EF Core this becomes SQL WHERE/ORDER BY/OFFSET
// and only the current page is materialized:
//     private IQueryable<Product> products => dbContext.Products;
private IQueryable<Product> products = SampleData.Generate(400).AsQueryable();" + ProductModelCode + SampleDataCode;

    private readonly string example28RazorCode = @"
@* ExcelExportStyled samples the grid's rendered theme (colors, striping, borders, fonts)
   from the live DOM at export time and bakes it into the workbook's styles. *@
<BitDataGrid Items=""@products"" Height=""430px""
             Filterable=""true"" Pageable=""true"" PageSize=""10"" Striped=""true""
             ShowToolbar=""true"" ShowCsvExport=""true"" ShowExcelExport=""true"" ExcelExportStyled=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
    <BitDataGridColumn Property=""p => p.Stock"" />
    <BitDataGridColumn Property=""p => p.Discontinued"" />
</BitDataGrid>";
    private readonly string example28CsharpCode = @"
private List<Product> products = SampleData.Generate(120);

// Programmatic exports are also available:
//     string csv = await grid.ToCsvAsync();     // all matching rows in every data mode
//     byte[] xlsx = await grid.ToExcelAsync();  // real .xlsx workbook, no external library" + ProductModelCode + SampleDataCode;

    private readonly string example29RazorCode = @"
<BitDataGrid Items=""@roots"" Height=""430px""
             ChildrenProvider=""LoadChildrenAsync"" HasChildrenSelector=""IsFolder""
             KeyField=""n => n.Id"" Sortable=""false"">
    <BitDataGridColumn Property=""p => p.Name"" Width=""280px"" />
    <BitDataGridColumn Property=""p => p.Kind"" Width=""110px"" />
    <BitDataGridColumn Property=""p => p.Size"" Format=""N0"" />
    <BitDataGridColumn Property=""p => p.Modified"" Format=""yyyy-MM-dd"" />
</BitDataGrid>";
    private readonly string example29CsharpCode = @"
private List<FileNode> roots = new()
{
    new() { Id = 1, Name = ""src"", Kind = ""Folder"" },
    new() { Id = 2, Name = ""docs"", Kind = ""Folder"" },
    new() { Id = 4, Name = ""LICENSE"", Kind = ""File"", Size = 1_070 },
};

// Unloaded nodes can't know whether they have children — this predicate decides
// whether the expand toggle renders before the first fetch.
private static bool IsFolder(FileNode node) => node.Kind == ""Folder"";

// Called once per node, on its first expand; the grid caches the result.
private async Task<IEnumerable<FileNode>?> LoadChildrenAsync(FileNode parent)
{
    return await httpClient.GetFromJsonAsync<List<FileNode>>($""api/files/{parent.Id}/children"");
}

public class FileNode
{
    public int Id { get; set; }
    public string Name { get; set; } = """";
    public string Kind { get; set; } = ""Folder"";
    public long Size { get; set; }
    public DateTime Modified { get; set; }
}";

    private readonly string example30RazorCode = @"
@* Row and column reordering automatically work with touch/pen input as well:
   a pointer-event fallback drives the same reorder pipeline where native
   HTML5 drag-and-drop is mouse-only. No extra configuration is needed. *@
<BitDataGrid Items=""@products"" Height=""430px""
             RowReorderable=""true"" Reorderable=""true"" Sortable=""false"" KeyField=""p => p.Id"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example30CsharpCode = @"
private List<Product> products = SampleData.Generate(12);" + ProductModelCode + SampleDataCode;

    private readonly string example31RazorCode = @"
<BitDataGrid Items=""@products"" Height=""430px""
             VirtualizeColumns=""true"" Virtualize=""true"" RowHeight=""36"" Sortable=""false"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""80px"" Frozen=""true"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""200px"" />
    @for (var m = 1; m <= 40; m++)
    {
        var month = m;
        <BitDataGridColumn ColumnId=""@($""m{month}"")"" Title=""@($""M{month:00}"")"" Width=""110px"">
            <Template Context=""product"">@(((product.Id * 37 + month * 13) % 1000))</Template>
        </BitDataGridColumn>
    }
</BitDataGrid>";
    private readonly string example31CsharpCode = @"
// 3,000 rows x 42 columns — but only the visible window of rows AND columns exists in the DOM.
// Column virtualization needs explicit pixel widths so the column window can be computed.
private List<Product> products = SampleData.Generate(3_000);" + ProductModelCode + SampleDataCode;

    private readonly string example32RazorCode = @"
<BitButton OnClick=""SortByPrice"">Sort by price ↓</BitButton>
<BitButton OnClick=""FilterExpensive"">Filter price > 500</BitButton>
<BitButton OnClick=""GroupByCategory"">Group by category</BitButton>
<BitButton OnClick=""GoToPage3"">Go to page 3</BitButton>

<BitDataGrid @ref=""grid"" Items=""@products"" Height=""430px""
             Sortable=""true"" Filterable=""true"" Groupable=""true""
             Pageable=""true"" PageSize=""10"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Filterable=""false"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Stock"" Groupable=""false"" />
</BitDataGrid>";
    private readonly string example32CsharpCode = @"
private List<Product> products = SampleData.Generate(200);
private BitDataGrid<Product>? grid;

private async Task SortByPrice() => await grid!.SortByAsync(nameof(Product.Price), BitDataGridSortDirection.Descending);
private async Task FilterExpensive() => await grid!.ApplyFilterAsync(nameof(Product.Price), BitDataGridFilterOperator.GreaterThan, 500m);
private async Task GroupByCategory() => await grid!.GroupByAsync(nameof(Product.Category));
private async Task GoToPage3() => await grid!.GoToPageAsync(3);

// Also available: ClearSortsAsync, ClearFilterAsync/ClearFiltersAsync, UngroupAsync/ClearGroupsAsync,
// SetPageSizeAsync, RefreshAsync, GetState/ApplyStateAsync, ToCsvAsync/ToExcelAsync.
" + ProductModelCode + SampleDataCode;

    private readonly string example33RazorCode = @"
<BitDataGrid Items=""@products"" Height=""460px"" Bordered=""true""
             Sortable=""true"" Filterable=""true""
             ShowToolbar=""true"" ShowCsvExport=""true"" ShowExcelExport=""true"" ExcelExportStyled=""true"">
    <DetailTemplate Context=""p"">
        @* Detail content is presentation-only: exports cover the master rows. *@
        <div><strong>Supplier:</strong> @p.Supplier · <strong>Rating:</strong> @p.Rating.ToString(""N1"")</div>
    </DetailTemplate>
    <Columns>
        @* Leading frozen columns (with the header row) become an Excel freeze pane;
           the exported columns keep their declared order in both formats. *@
        <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""80px"" Frozen=""true"" Filterable=""false"" />

        @* A templated field column exports the raw field value, not the rendered markup.
           In Excel the ColSpan becomes a merged cell; in CSV it is flattened, so there the
           Category hidden under the span is exported too. *@
        <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" Frozen=""true"" ColSpan=""p => NameSpan(p)"">
            <Template Context=""p"">
                @if (p.Discontinued)
                {
                    <span>⚠ @p.Name — discontinued</span>
                }
                else
                {
                    @p.Name
                }
            </Template>
        </BitDataGridColumn>

        <BitDataGridColumn Property=""p => p.Category"" Width=""170px"" />
        <BitDataGridColumn Property=""p => p.Price"" Width=""150px"" Format=""C2"" />
        <BitDataGridColumn Property=""p => p.Stock"" Width=""130px"" />
        <BitDataGridColumn Property=""p => p.Supplier"" Width=""200px"" />

        @* A template-only column (no Field) has no exportable value and is skipped. *@
        <BitDataGridColumn ColumnId=""Value"" Title=""Value"" Width=""140px""
                           SortBy=""@(p => p.Price * p.Stock)"">
            <Template Context=""p"">@((p.Price * p.Stock).ToString(""C0""))</Template>
        </BitDataGridColumn>
    </Columns>
</BitDataGrid>";
    private readonly string example33CsharpCode = @"
private List<Product> products = SampleData.Generate(30);

// In the Excel export this span becomes a merged cell (the covered Category cell stays empty);
// the CSV export has no merge concept and writes every column's own value.
private int? NameSpan(Product p) => p.Discontinued ? 2 : null;" + ProductModelCode + SampleDataCode;
}
