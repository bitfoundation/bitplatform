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
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""430px"" MultiSort=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Width=""70px"" Align=""BitDataGridColumnAlign.Right"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" Width=""220px"" />
    <BitDataGridColumn TItem=""Product"" Field=""Category"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" Align=""BitDataGridColumnAlign.Right"" />
    <BitDataGridColumn TItem=""Product"" Field=""Stock"" Align=""BitDataGridColumnAlign.Right"" />
    <BitDataGridColumn TItem=""Product"" Field=""Rating"" Format=""N1"" Align=""BitDataGridColumnAlign.Right"" />
</BitDataGrid>";
    private readonly string example1CsharpCode = @"
private List<Product> products = SampleData.Generate(50);" + ProductModelCode + SampleDataCode;

    private readonly string example2RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""430px""
             Filterable=""true"" Pageable=""true"" PageSize=""10""
             PagerPosition=""BitDataGridPagerPosition.Bottom""
             ShowToolbar=""true"" ShowCsvExport=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Filterable=""false"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Category"" />
    <BitDataGridColumn TItem=""Product"" Field=""Supplier"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" Align=""BitDataGridColumnAlign.Right"" />
    <BitDataGridColumn TItem=""Product"" Field=""Stock"" Align=""BitDataGridColumnAlign.Right"" />
</BitDataGrid>";
    private readonly string example2CsharpCode = @"
private List<Product> products = SampleData.Generate(200);" + ProductModelCode + SampleDataCode;

    private readonly string example3RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""420px""
             SelectionMode=""BitDataGridSelectionMode.Multiple"" @bind-SelectedItems=""selected""
             Pageable=""true"" PageSize=""10"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Align=""BitDataGridColumnAlign.Right"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Category"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" Align=""BitDataGridColumnAlign.Right"" />
</BitDataGrid>";
    private readonly string example3CsharpCode = @"
private List<Product> products = SampleData.Generate(60);
private IReadOnlyList<Product> selected = new List<Product>();" + ProductModelCode + SampleDataCode;

    private readonly string example4RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""460px""
             Editable=""true"" NewItemFactory=""CreateProduct""
             OnRowSave=""OnSave"" OnRowDelete=""OnDelete"" OnRowCreate=""OnCreate""
             ShowToolbar=""true"" Pageable=""true"" PageSize=""10"" KeyField=""p => p.Id"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Editable=""false"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Category"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
    <BitDataGridColumn TItem=""Product"" Field=""Discontinued"" />
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
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""500px""
             Groupable=""true"" ShowFooter=""true"" Sortable=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Name"" Groupable=""false"" />
    <BitDataGridColumn TItem=""Product"" Field=""Category"" />
    <BitDataGridColumn TItem=""Product"" Field=""Supplier"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" Align=""BitDataGridColumnAlign.Right""
                       Aggregate=""BitDataGridAggregateType.Sum"" Groupable=""false"" />
    <BitDataGridColumn TItem=""Product"" Field=""Stock"" Align=""BitDataGridColumnAlign.Right""
                       Aggregate=""BitDataGridAggregateType.Average"" AggregateFormat=""N0"" Groupable=""false"" />
</BitDataGrid>";
    private readonly string example5CsharpCode = @"
private List<Product> products = SampleData.Generate(80);" + ProductModelCode + SampleDataCode;

    private readonly string example6RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""470px"" Sortable=""true"" ShowFooter=""true"">
    <DetailTemplate Context=""p"">
        <div>Supplier: @p.Supplier</div>
    </DetailTemplate>
    <ChildContent>
        <BitDataGridColumn TItem=""Product"" Field=""Name"">
            <HeaderTemplate>📦 Product</HeaderTemplate>
        </BitDataGridColumn>
        <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" Align=""BitDataGridColumnAlign.Right""
                           Aggregate=""BitDataGridAggregateType.Sum"">
            <FooterTemplate Context=""agg"">Total: @agg.FormattedValue</FooterTemplate>
        </BitDataGridColumn>
        <BitDataGridColumn TItem=""Product"" Field=""Stock"" Align=""BitDataGridColumnAlign.Right"">
            <Template Context=""p"">@p.Stock in stock</Template>
        </BitDataGridColumn>
    </ChildContent>
</BitDataGrid>";
    private readonly string example6CsharpCode = @"
private List<Product> products = SampleData.Generate(30);" + ProductModelCode + SampleDataCode;

    private readonly string example7RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""430px"" Resizable=""true"" Reorderable=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Width=""80px"" Frozen=""true"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" Width=""220px"" Frozen=""true"" />
    <BitDataGridColumn TItem=""Product"" Field=""Category"" Width=""160px"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Width=""160px"" Format=""C2"" Align=""BitDataGridColumnAlign.Right"" />
</BitDataGrid>";
    private readonly string example7CsharpCode = @"
private List<Product> products = SampleData.Generate(40);" + ProductModelCode + SampleDataCode;

    private readonly string example8RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""460px"" Sortable=""true"" Bordered=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" Group=""Identity"" />
    <BitDataGridColumn TItem=""Product"" Field=""Category"" Group=""Identity"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" Group=""Commercials"" />
    <BitDataGridColumn TItem=""Product"" Field=""Stock"" Group=""Commercials"" />
    <BitDataGridColumn TItem=""Product"" Field=""Rating"" Format=""N1"" Group=""Quality"" />
    <BitDataGridColumn TItem=""Product"" Field=""Supplier"" Group=""Quality"" />
</BitDataGrid>";
    private readonly string example8CsharpCode = @"
private List<Product> products = SampleData.Generate(40);" + ProductModelCode + SampleDataCode;

    private readonly string example9RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""460px"" Bordered=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Name"" ColSpan=""NameSpan"">
        <Template Context=""p"">@p.Name</Template>
    </BitDataGridColumn>
    <BitDataGridColumn TItem=""Product"" Field=""Category"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" ColSpan=""PriceSpan"" />
    <BitDataGridColumn TItem=""Product"" Field=""Stock"" />
</BitDataGrid>";
    private readonly string example9CsharpCode = @"
private List<Product> products = SampleData.Generate(40);

private int? NameSpan(Product p) => p.Discontinued ? 2 : null;
private int? PriceSpan(Product p) => p.Price > 800 ? 2 : null;" + ProductModelCode + SampleDataCode;

    private readonly string example10RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""520px""
             Virtualize=""true"" RowHeight=""36"" Sortable=""true"" Filterable=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Filterable=""false"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example10CsharpCode = @"
private List<Product> products = SampleData.Generate(10_000);" + ProductModelCode + SampleDataCode;

    private readonly string example11RazorCode = @"
<BitDataGrid TItem=""Product"" OnRead=""LoadData"" Height=""430px""
             Pageable=""true"" PageSize=""10"" Sortable=""true"" Filterable=""true"" Loading=""loading"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Filterable=""false"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
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
<BitDataGrid TItem=""Product"" OnLoadMore=""LoadMore"" LoadMoreBatchSize=""40""
             Height=""520px"" RowHeight=""40"" Sortable=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
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

    var batch = query.Skip(request.Skip).Take(request.Take ?? 40).ToList();

    // Drop a superseded batch before returning so a cancelled load never yields stale rows.
    request.CancellationToken.ThrowIfCancellationRequested();

    // Pass 0 as the total count to signal there is no known total (infinite scrolling).
    return new BitDataGridReadResult<Product>(batch, 0);
}" + ProductModelCode + SampleDataCode;

    private readonly string example13RazorCode = @"
<BitDataGrid TItem=""FileNode"" Items=""@roots"" Height=""460px"" Sortable=""true""
             ChildrenSelector=""n => n.Children"" TreeInitiallyExpanded=""true""
             KeyField=""n => n.Id"" @ref=""grid"">
    <BitDataGridColumn TItem=""FileNode"" Field=""Name"" Width=""320px"" />
    <BitDataGridColumn TItem=""FileNode"" Field=""Kind"" Title=""Type"" />
    <BitDataGridColumn TItem=""FileNode"" Field=""Size"" Format=""N0"" Align=""BitDataGridColumnAlign.Right"" />
</BitDataGrid>";
    private readonly string example13CsharpCode = @"
private List<FileNode> roots = FileSystemData.Build();
private BitDataGrid<FileNode>? grid;

private async Task ExpandAll() { if (grid is not null) await grid.ExpandAllAsync(); }
private async Task CollapseAll() { if (grid is not null) await grid.CollapseAllAsync(); }" + FileSystemDataCode;

    private readonly string example14RazorCode = @"
<BitDataGrid TItem=""SupplierModel"" Items=""@suppliers"" Height=""520px"" Sortable=""true"">
    <DetailTemplate Context=""supplier"">
        <BitDataGrid TItem=""Product"" Items=""supplier.Products"" Sortable=""true"">
            <BitDataGridColumn TItem=""Product"" Field=""Name"" />
            <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
        </BitDataGrid>
    </DetailTemplate>
    <ChildContent>
        <BitDataGridColumn TItem=""SupplierModel"" Field=""Name"" Title=""Supplier"" />
        <BitDataGridColumn TItem=""SupplierModel"" Field=""ProductCount"" Title=""Products"" />
    </ChildContent>
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
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example15CsharpCode = @"
private List<Product> products = SampleData.Generate(12);

private void OnReorder(BitDataGridRowReorderEventArgs<Product> e)
{
    // e.DraggedItem, e.TargetItem, e.FromIndex, e.ToIndex
    // FromIndex/ToIndex are int? and may be null when Items is not an indexable IList<T>.
}" + ProductModelCode + SampleDataCode;

    private readonly string example16RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""420px""
             OnCellClick=""OnCellClick""
             OnCellDoubleClick=""OnCellDoubleClick""
             OnCellContextMenu=""OnCellContextMenu"">
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example16CsharpCode = @"
private List<Product> products = SampleData.Generate(40);

private void OnCellClick(BitDataGridCellEventArgs<Product> e) { /* e.Item, e.ColumnTitle, e.Value */ }
private void OnCellDoubleClick(BitDataGridCellEventArgs<Product> e) { /* e.Item, e.ColumnTitle, e.Value */ }
private void OnCellContextMenu(BitDataGridCellEventArgs<Product> e) { /* e.Mouse.ClientX / e.Mouse.ClientY */ }" + ProductModelCode + SampleDataCode;

    private readonly string example17RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""460px""
             CellNavigation=""true"" Sortable=""true"" Editable=""true"" OnRowSave=""_ => {}"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Editable=""false"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example17CsharpCode = @"
private List<Product> products = SampleData.Generate(40);" + ProductModelCode + SampleDataCode;

    private readonly string example18RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""480px"" Sortable=""true""
             RowHeightSelector=""RowHeight"">
    <BitDataGridColumn TItem=""Product"" Field=""Name"">
        <Template Context=""p""><strong>@p.Name</strong></Template>
    </BitDataGridColumn>
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example18CsharpCode = @"
private List<Product> products = SampleData.Generate(40);

private float RowHeight(Product p) => p.Price > 500 ? 64f : 36f;" + ProductModelCode + SampleDataCode;

    private readonly string example19RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@items"" Height=""320px"" Sortable=""true"">
    <EmptyTemplate>
        <div>Nothing here yet. Try loading the sample data to populate the grid.</div>
    </EmptyTemplate>
    <ChildContent>
        <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" />
        <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    </ChildContent>
</BitDataGrid>";
    private readonly string example19CsharpCode = @"
private List<Product> items = new(); // empty" + ProductModelCode;

    private readonly string example20RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""420px""
             Bordered=""@bordered"" Striped=""@striped""
             Sortable=""true"" Pageable=""true"" PageSize=""8"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Frozen=""true"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example20CsharpCode = @"
private List<Product> products = SampleData.Generate(60);
private bool bordered = true;
private bool striped = true;" + ProductModelCode + SampleDataCode;

    private readonly string example21RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""420px""
             Direction=""BitDir.Rtl""
             Sortable=""true"" Pageable=""true"" PageSize=""8"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""شناسه"" Frozen=""true"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" Title=""نام"" />
    <BitDataGridColumn TItem=""Product"" Field=""Category"" Title=""دسته‌بندی"">
        <Template Context=""product"">@CategoryFa(product.Category)</Template>
    </BitDataGridColumn>
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Title=""قیمت"" Format=""C2"" />
    <BitDataGridColumn TItem=""Product"" Field=""Stock"" Title=""موجودی"" />
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
}
