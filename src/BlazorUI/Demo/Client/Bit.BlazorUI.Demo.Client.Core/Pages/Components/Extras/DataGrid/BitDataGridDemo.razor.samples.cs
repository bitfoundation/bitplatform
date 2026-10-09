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
                        File(""BitTextAlign.cs"", 320),
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
<BitDataGrid Items=""@basicProducts"" Height=""430px"" AriaLabel=""Products"">
    <Columns>
        <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
        <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
        <BitDataGridColumn Property=""p => p.Category"" />
        <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
        <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" />
        <BitDataGridColumn Property=""p => p.Rating"" Format=""N1"" Align=""BitTextAlign.End"" AllowUnsorted=""false"" />
    </Columns>
</BitDataGrid>";
    private readonly string example1CsharpCode = @"
private readonly List<Product> basicProducts = SampleData.Generate(50);" + ProductModelCode + SampleDataCode;

    private readonly string example2RazorCode = @"
<BitDataGrid Items=""@filterProducts"" Height=""430px""
             Filterable=""true"" FilterOperators=""true""
             Pageable=""true"" PageSize=""10"" ShowToolbar=""true"">
    <BitDataGridColumn Field=""Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" Filterable=""false"" />
    <BitDataGridColumn Field=""Name"" Width=""220px"" />
    <BitDataGridColumn Field=""Category"">
        <FilterTemplate>
            <BitDropdown TItem=""BitDropdownItem<string>"" TValue=""string"" Items=""categoryFilterItems""
                         MultiSelect Size=""BitSize.Small"" Placeholder=""All""
                         AriaLabel=""@context.Label"" Disabled=""context.Disabled""
                         Values=""@(context.Value as IEnumerable<string?>)""
                         ValuesChanged=""values => context.ApplyAsync(BitDataGridFilterOperator.In, values?.ToList())"" />
        </FilterTemplate>
    </BitDataGridColumn>
    <BitDataGridColumn Field=""Supplier"" FilterOperators=""false"" />
    <BitDataGridColumn Field=""Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Field=""ReleaseDate"" Title=""Released"" Format=""yyyy-MM-dd"" />
</BitDataGrid>";
    private readonly string example2CsharpCode = @"
private readonly List<Product> filterProducts = SampleData.Generate(200);

private readonly List<BitDropdownItem<string>> categoryFilterItems =
    Enum.GetNames<Category>().Select(name => new BitDropdownItem<string> { Text = name, Value = name }).ToList();" + ProductModelCode + SampleDataCode;

    private readonly string example3RazorCode = @"
<BitDataGrid Items=""@searchProducts"" Height=""430px""
             ShowSearchBox=""true"" @bind-SearchText=""searchTerm""
             Pageable=""true"" PageSize=""10"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Supplier"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" Searchable=""false"" />
</BitDataGrid>

<BitText>@(string.IsNullOrEmpty(searchTerm) ? ""No search term."" : $""Searching for \""{searchTerm}\""."")</BitText>";
    private readonly string example3CsharpCode = @"
private readonly List<Product> searchProducts = SampleData.Generate(200);
private string? searchTerm;" + ProductModelCode + SampleDataCode;

    private readonly string example4RazorCode = @"
<BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
    <BitButton Variant=""@(selectionMode == BitSelectionMode.Single ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""SelectSingleMode"">Single</BitButton>
    <BitButton Variant=""@(selectionMode == BitSelectionMode.Multiple ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""() => selectionMode = BitSelectionMode.Multiple"">Multiple</BitButton>
    <BitText>@selectedProducts.Count selected</BitText>
</BitStack>

<BitDataGrid Items=""@selectionProducts"" Height=""420px"" KeyField=""p => p.Id""
             SelectionMode=""selectionMode"" @bind-SelectedItems=""selectedProducts""
             IsRowSelectionDisabled=""p => p.Discontinued""
             Pageable=""true"" PageSize=""10"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" RowHeader=""true"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Discontinued"" Align=""BitTextAlign.Center"" />
</BitDataGrid>";
    private readonly string example4CsharpCode = @"
private readonly List<Product> selectionProducts = SampleData.Generate(60);
private BitSelectionMode selectionMode = BitSelectionMode.Multiple;
private IReadOnlyList<Product> selectedProducts = [];

// Switching to Single drops the extra selections, so the bound list matches what the grid can hold.
private void SelectSingleMode()
{
    selectionMode = BitSelectionMode.Single;
    if (selectedProducts.Count > 1)
    {
        selectedProducts = selectedProducts.Take(1).ToList();
    }
}" + ProductModelCode + SampleDataCode;

    private readonly string example5RazorCode = @"
<BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
    <BitButton OnClick=""SelectAllRows"">Select all</BitButton>
    <BitButton OnClick=""ClearRowSelection"" Variant=""BitVariant.Outline"">Clear selection</BitButton>
    <BitButton OnClick=""CopySelection"" Disabled=""clipboardSelection.Count is 0"">Copy selection</BitButton>
    <BitText>@clipboardStatus</BitText>
</BitStack>

<BitDataGrid @ref=""clipboardGrid"" TItem=""Product"" Items=""@clipboardProducts"" Height=""430px"" KeyField=""p => p.Id""
             SelectionMode=""BitSelectionMode.Multiple"" @bind-SelectedItems=""clipboardSelection""
             CellNavigation=""true"" ClipboardCopy=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" />
</BitDataGrid>";
    private readonly string example5CsharpCode = @"
private readonly List<Product> clipboardProducts = SampleData.Generate(40);
private BitDataGrid<Product>? clipboardGrid;
private IReadOnlyList<Product> clipboardSelection = [];
private string clipboardStatus = ""Select rows, then copy them."";

private async Task SelectAllRows()
{
    if (clipboardGrid is null) return;
    await clipboardGrid.SelectAllAsync();
    clipboardStatus = $""{clipboardSelection.Count} rows selected."";
}

private async Task ClearRowSelection()
{
    if (clipboardGrid is null) return;
    await clipboardGrid.ClearSelectionAsync();
    clipboardStatus = ""Selection cleared."";
}

private async Task CopySelection()
{
    if (clipboardGrid is null) return;
    var copied = await clipboardGrid.CopyToClipboardAsync();
    clipboardStatus = copied > 0
        ? $""{copied} rows copied to the clipboard.""
        : ""Nothing was copied (the clipboard may be unavailable here)."";
}" + ProductModelCode + SampleDataCode;

    private readonly string example6RazorCode = @"
<BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
    <BitButton Variant=""@(editMode == BitDataGridEditMode.Row ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""() => editMode = BitDataGridEditMode.Row"">Row</BitButton>
    <BitButton Variant=""@(editMode == BitDataGridEditMode.Cell ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""() => editMode = BitDataGridEditMode.Cell"">Cell</BitButton>
</BitStack>

<BitDataGrid @ref=""editGrid"" TItem=""Product"" Items=""@editProducts"" Height=""460px"" KeyField=""p => p.Id""
             Editable=""true"" EditMode=""editMode"" NewItemFactory=""CreateProduct"" CellNavigation=""true""
             OnRowSave=""OnSave"" OnRowCancel=""OnCancel"" OnRowDelete=""OnDelete"" OnRowCreate=""OnCreate""
             OnRowDoubleClick=""EditOnDoubleClick""
             Pageable=""true"" PageSize=""10"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" Editable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""200px"" Validate=""(p, v) => ValidateName(p, v)"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" Validate=""(p, v) => ValidatePrice(p, v)"" />
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" Validate=""(p, v) => ValidateStock(p, v)"" />
    <BitDataGridColumn Property=""p => p.ReleaseDate"" Title=""Released"" Format=""yyyy-MM-dd"" />
    <BitDataGridColumn Property=""p => p.Discontinued"" Align=""BitTextAlign.Center"" />
</BitDataGrid>

<BitText>@editStatus</BitText>";
    private readonly string example6CsharpCode = @"
private readonly List<Product> editProducts = SampleData.Generate(25);
private BitDataGrid<Product>? editGrid;
private BitDataGridEditMode editMode;
private int nextId;
private string editStatus = ""Double-click a row, or use its Edit button."";

protected override void OnInitialized() => nextId = editProducts.Max(p => p.Id) + 1;

private Product CreateProduct() => new()
{
    Id = nextId++,
    Name = ""New product"",
    Category = Category.Electronics,
    Rating = 3,
    ReleaseDate = DateTime.Today
};

private void OnCreate(Product p) => editStatus = $""Adding new product #{p.Id}…"";

private void OnSave(Product p)
{
    // The grid hands back an edited copy, so match on the key rather than the reference.
    var index = editProducts.FindIndex(x => x.Id == p.Id);
    if (index >= 0) editProducts[index] = p;
    else editProducts.Insert(0, p);
    editStatus = $""Saved {p.Name} (#{p.Id})."";
}

private void OnCancel(Product p) => editStatus = $""Cancelled editing {p.Name}."";

private void OnDelete(Product p)
{
    editProducts.RemoveAll(x => x.Id == p.Id);
    editStatus = $""Deleted #{p.Id}."";
}

private void EditOnDoubleClick(Product product)
{
    editGrid?.BeginEdit(product);
    editStatus = $""Editing {product.Name}. Enter saves, Esc cancels."";
}

// A validator gets the row and the proposed, already converted value; a message rejects it and blocks Save.
private string? ValidateName(Product product, object? value)
    => string.IsNullOrWhiteSpace(value as string) ? ""Name is required."" : null;

private string? ValidatePrice(Product product, object? value)
    => value is decimal price && price < 0 ? ""Price cannot be negative."" : null;

private string? ValidateStock(Product product, object? value)
    => value is int stock && stock < 0 ? ""Stock cannot be negative."" : null;" + ProductModelCode + SampleDataCode;

    private readonly string example7RazorCode = @"
<BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
    <BitButton OnClick=""ExpandAllGroups"">Expand all groups</BitButton>
    <BitButton OnClick=""CollapseAllGroups"" Variant=""BitVariant.Outline"">Collapse all groups</BitButton>
</BitStack>

<BitDataGrid @ref=""groupGrid"" TItem=""Product"" Items=""@groupProducts"" Height=""500px""
             Groupable=""true"" ShowFooter=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""200px"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Supplier"" AggregateBy=""rows => DistinctSuppliers(rows)"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End""
                       Aggregate=""BitDataGridAggregateType.Sum"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End""
                       Aggregate=""BitDataGridAggregateType.Average"" AggregateFormat=""N0"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Rating"" Format=""N1"" Align=""BitTextAlign.End""
                       Aggregate=""BitDataGridAggregateType.Max"" Groupable=""false"" />
</BitDataGrid>";
    private readonly string example7CsharpCode = @"
private readonly List<Product> groupProducts = SampleData.Generate(80);
private BitDataGrid<Product>? groupGrid;

// A custom aggregate: gets the rows of a group (or of the whole view for the footer) and returns any value.
private object? DistinctSuppliers(IReadOnlyList<Product> rows)
    => $""{rows.Select(p => p.Supplier).Distinct().Count()} distinct"";

private async Task ExpandAllGroups() { if (groupGrid is not null) await groupGrid.ExpandAllGroupsAsync(); }
private async Task CollapseAllGroups() { if (groupGrid is not null) await groupGrid.CollapseAllGroupsAsync(); }" + ProductModelCode + SampleDataCode;

    private readonly string example8RazorCode = @"
<BitDataGrid Items=""@templateProducts"" Height=""470px"" ShowFooter=""true"">
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"">
        <HeaderTemplate>📦 Product</HeaderTemplate>
    </BitDataGridColumn>
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Rating"">
        <Template Context=""p"">
            <span role=""img"" aria-label=""@($""{p.Rating} out of 5"")"" title=""@p.Rating"">
                @for (var i = 0; i < 5; i++)
                {
                    <span style=""color:@(i < Math.Round(p.Rating, MidpointRounding.AwayFromZero) ? ""#f5a623"" : ""#ccc"")"">★</span>
                }
            </span>
        </Template>
    </BitDataGridColumn>
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End""
                       Aggregate=""BitDataGridAggregateType.Sum"">
        <FooterTemplate Context=""agg"">Total: @agg.FormattedValue</FooterTemplate>
    </BitDataGridColumn>
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" Comparer=""stockComparer"">
        <Template Context=""p"">
            <BitTag Text=""@(p.Stock == 0 ? ""Out of stock"" : $""{p.Stock} in stock"")""
                    Color=""@(p.Stock == 0 ? BitColor.Error : BitColor.Success)"" />
        </Template>
    </BitDataGridColumn>
    <BitDataGridColumn ColumnId=""Value"" Title=""Value"" Align=""BitTextAlign.End""
                       SortBy=""@(p => p.Price * p.Stock)"">
        <Template Context=""p"">@((p.Price * p.Stock).ToString(""C0""))</Template>
    </BitDataGridColumn>
</BitDataGrid>";
    private readonly string example8CsharpCode = @"
private readonly List<Product> templateProducts = SampleData.Generate(30);

// In-stock rows first, then by quantity - an order the raw number cannot express.
private readonly IComparer<object?> stockComparer = new StockComparer();

private sealed class StockComparer : IComparer<object?>
{
    public int Compare(object? x, object? y)
    {
        var a = x as int? ?? 0;
        var b = y as int? ?? 0;
        var inStock = (a == 0).CompareTo(b == 0);
        return inStock != 0 ? inStock : a.CompareTo(b);
    }
}" + ProductModelCode + SampleDataCode;

    private readonly string example9RazorCode = @"
<BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
    <BitButton OnClick=""ExpandAllDetails"">Expand all</BitButton>
    <BitButton OnClick=""CollapseAllDetails"" Variant=""BitVariant.Outline"">Collapse all</BitButton>
    <BitText>@detailStatus</BitText>
</BitStack>

<BitDataGrid @ref=""detailGrid"" TItem=""SupplierModel"" Items=""@suppliers"" Height=""520px""
             ExpandDetailOnRowClick=""true"" OnDetailToggle=""OnDetailToggled"">
    <DetailTemplate Context=""supplier"">
        <BitDataGrid Items=""supplier.Products"" AriaLabel=""@($""Products of {supplier.Name}"")"">
            <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
            <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
            <BitDataGridColumn Property=""p => p.Category"" />
            <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
        </BitDataGrid>
    </DetailTemplate>
    <Columns>
        <BitDataGridColumn Property=""p => p.Name"" Title=""Supplier"" Width=""260px"" />
        <BitDataGridColumn Property=""p => p.ProductCount"" Title=""Products"" Align=""BitTextAlign.End"" />
        <BitDataGridColumn Property=""p => p.TotalStock"" Title=""Total stock"" Format=""N0"" Align=""BitTextAlign.End"" />
        <BitDataGridColumn Property=""p => p.AveragePrice"" Title=""Avg price"" Format=""C2"" Align=""BitTextAlign.End"" />
    </Columns>
</BitDataGrid>";
    private readonly string example9CsharpCode = @"
private readonly List<SupplierModel> suppliers = BuildSuppliers();
private BitDataGrid<SupplierModel>? detailGrid;
private string detailStatus = ""Click a row, or its ▸ toggle, to open it."";

private static List<SupplierModel> BuildSuppliers() =>
    SampleData.Generate(240)
        .GroupBy(p => p.Supplier)
        .Select(g => new SupplierModel { Name = g.Key, Products = g.OrderBy(p => p.Name).ToList() })
        .OrderBy(s => s.Name)
        .ToList();

private void OnDetailToggled(BitDataGridDetailEventArgs<SupplierModel> args)
    => detailStatus = $""{args.Item.Name} {(args.Expanded ? ""expanded"" : ""collapsed"")}."";

private async Task ExpandAllDetails() { if (detailGrid is not null) await detailGrid.ExpandAllDetailsAsync(); }
private async Task CollapseAllDetails() { if (detailGrid is not null) await detailGrid.CollapseAllDetailsAsync(); }" + SupplierModelCode + ProductModelCode + SampleDataCode;

    private readonly string example10RazorCode = @"
<BitDataGrid Items=""@columnsProducts"" Height=""460px""
             Resizable=""true"" Reorderable=""true"" ShowColumnChooser=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""80px"" Align=""BitTextAlign.End"" Frozen=""true"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" MinWidth=""120"" MaxWidth=""400"" Frozen=""true"" Group=""Identity"" />
    <BitDataGridColumn Property=""p => p.Category"" Width=""160px"" Group=""Identity"" />
    <BitDataGridColumn Property=""p => p.Supplier"" Width=""200px"" Group=""Identity"" />
    <BitDataGridColumn Property=""p => p.Price"" Width=""160px"" Format=""C2"" Align=""BitTextAlign.End"" Group=""Commercials"" />
    <BitDataGridColumn Property=""p => p.Stock"" Width=""140px"" Align=""BitTextAlign.End"" Group=""Commercials"" />
    <BitDataGridColumn Property=""p => p.ReleaseDate"" Title=""Released"" Width=""160px"" Format=""yyyy-MM-dd"" />
    <BitDataGridColumn Property=""p => p.Rating"" Width=""110px"" Format=""N1"" Align=""BitTextAlign.End"" FrozenEnd=""true"" />
</BitDataGrid>";
    private readonly string example10CsharpCode = @"
private readonly List<Product> columnsProducts = SampleData.Generate(40);" + ProductModelCode + SampleDataCode;

    private readonly string example11RazorCode = @"
<BitDataGrid Items=""@spanningProducts"" Height=""460px"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" ColSpan=""p => NameSpan(p)"">
        <Template Context=""p"">
            @if (p.Discontinued)
            {
                <span class=""span-banner"">⚠ @p.Name - discontinued</span>
            }
            else
            {
                @p.Name
            }
        </Template>
    </BitDataGridColumn>
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" ColSpan=""p => PriceSpan(p)"">
        <Template Context=""p"">
            @if (p.Price > 800)
            {
                <span class=""span-banner"">★ Premium: @p.Price.ToString(""C2"") · @p.Stock in stock</span>
            }
            else
            {
                @p.Price.ToString(""C2"")
            }
        </Template>
    </BitDataGridColumn>
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" />
</BitDataGrid>";
    private readonly string example11CsharpCode = @"
private readonly List<Product> spanningProducts = SampleData.Generate(40);

private int? NameSpan(Product p) => p.Discontinued ? 2 : null;
private int? PriceSpan(Product p) => p.Price > 800 ? 2 : null;" + ProductModelCode + SampleDataCode;

    private readonly string example12RazorCode = @"
<BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
    <BitButton OnClick=""() => bordered = !bordered"">Borders: @(bordered ? ""on"" : ""off"")</BitButton>
    <BitButton OnClick=""() => striped = !striped"">Striping: @(striped ? ""on"" : ""off"")</BitButton>
    <BitButton OnClick=""() => hoverable = !hoverable"">Hover: @(hoverable ? ""on"" : ""off"")</BitButton>
    <BitButton OnClick=""() => rowNumbers = !rowNumbers"">Row numbers: @(rowNumbers ? ""on"" : ""off"")</BitButton>
</BitStack>

<BitDataGrid Items=""@borderStripeProducts"" Height=""420px""
             Bordered=""bordered"" Striped=""striped"" Hoverable=""hoverable"" ShowRowNumbers=""rowNumbers""
             Pageable=""true"" PageSize=""8"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" Frozen=""true"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" />
</BitDataGrid>";
    private readonly string example12CsharpCode = @"
private readonly List<Product> borderStripeProducts = SampleData.Generate(60);
private bool bordered = true;
private bool striped = true;
private bool hoverable = true;
private bool rowNumbers = true;" + ProductModelCode + SampleDataCode;

    private readonly string example13RazorCode = @"
@* The row class lands on an element the grid renders, so the page's scoped CSS needs a
   plain wrapper of its own to hang ::deep off (see the stylesheet tab). *@
<div class=""styled-rows"">
    <BitDataGrid Items=""@styledRowProducts"" Height=""430px"" Resizable=""true""
                 ShowCellTooltips=""true""
                 RowClass=""RowClassFor"" RowStyle=""RowStyleFor"">
        <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
        <BitDataGridColumn Property=""p => p.Name"" Width=""160px"" />
        <BitDataGridColumn Property=""p => p.Supplier"" Width=""140px"" />
        <BitDataGridColumn Property=""p => p.Category"" ShowTooltip=""false"" />
        <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
        <BitDataGridColumn TItem=""Product"" Property=""p => p.Stock"" Align=""BitTextAlign.End"" CellStyleSelector=""StockStyleFor"" />
    </BitDataGrid>
</div>";
    private readonly string example13CsharpCode = @"
private readonly List<Product> styledRowProducts = SampleData.Generate(60);

// Both run per row and come after the grid's own class and style, so they win whatever they repeat.
private static string? RowClassFor(Product p) => p.Stock == 0 ? ""row-out-of-stock"" : null;

private static string? RowStyleFor(Product p) => p.Price > 800 ? ""font-weight:600;"" : null;

// Per cell of one column: only the Stock cell of a low-stock row.
private static string? StockStyleFor(Product p) => p.Stock is > 0 and < 50 ? ""color:var(--bit-clr-wrn-fg);font-weight:600;"" : null;" + ProductModelCode + SampleDataCode;
    private readonly DemoCodeFile[] example13CodeFiles =
    [
        new("Page.razor.css", example13CssCode),
    ];
    private const string example13CssCode = @"
/* The class RowClass returns lands on a row the grid renders, not on markup this page wrote, so the
   page's scope attribute never reaches it. ::deep off the page's own wrapper crosses that boundary. */
.styled-rows ::deep .row-out-of-stock .bit-dtg-cell {
    color: var(--bit-clr-err-fg);
}";

    private readonly string example14RazorCode = @"
<BitButton OnClick=""() => wrapCellText = !wrapCellText"">Wrapping: @(wrapCellText ? ""on"" : ""off"")</BitButton>

<BitDataGrid Items=""@wrapProducts"" Height=""430px"" Resizable=""true""
             WrapCellText=""wrapCellText"" RowHeightSelector=""RowHeightOf"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" WrapText=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""160px"" />
    <BitDataGridColumn TItem=""Product"" Title=""Description"" Width=""280px"">
        <Template Context=""product"">@DescriptionOf(product)</Template>
    </BitDataGridColumn>
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Width=""100px"" Align=""BitTextAlign.End"" WrapText=""false"" />
    <BitDataGridColumn Property=""p => p.Stock"" Width=""90px"" Align=""BitTextAlign.End"" WrapText=""false"" />
</BitDataGrid>";
    private readonly string example14CsharpCode = @"
private readonly List<Product> wrapProducts = SampleData.Generate(40);
private bool wrapCellText = true;

private static string DescriptionOf(Product p)
    => $""{p.Name} is a {p.Category.ToString().ToLowerInvariant()} product supplied by {p.Supplier}, "" +
       $""released on {p.ReleaseDate:d} and currently rated {p.Rating:0.0} out of 5 by our customers."";

// The minimum height of a row; a wrapped row still grows past it.
private static float RowHeightOf(Product p) => p.Price > 800 ? 56f : 36f;" + ProductModelCode + SampleDataCode;

    private readonly string example15RazorCode = @"
<BitStack Horizontal Wrap>
    <BitButton OnClick=""() => emptyHasData = false"">Clear data</BitButton>
    <BitButton OnClick=""SimulateLoading"" Variant=""BitVariant.Outline"">Simulate loading</BitButton>
</BitStack>

<BitDataGrid Items=""@EmptyCurrent"" Height=""320px"" Loading=""emptyLoading"">
    <LoadingTemplate>
        <BitSpinnerLoading Label=""Fetching the latest products…"" />
    </LoadingTemplate>
    <EmptyTemplate>
        <BitStack HorizontalAlign=""BitAlignment.Center"">
            <BitText Typography=""BitTypography.H6"">Nothing here yet</BitText>
            <BitButton OnClick=""() => emptyHasData = true"">Load sample data</BitButton>
        </BitStack>
    </EmptyTemplate>
    <Columns>
        <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
        <BitDataGridColumn Property=""p => p.Name"" Width=""240px"" />
        <BitDataGridColumn Property=""p => p.Category"" />
        <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    </Columns>
</BitDataGrid>";
    private readonly string example15CsharpCode = @"
private readonly List<Product> emptyData = SampleData.Generate(25);
private readonly List<Product> emptyNone = [];
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
}" + ProductModelCode + SampleDataCode;

    private readonly string example16RazorCode = @"
<BitStack Horizontal Wrap>
    <BitButton OnClick=""ExpandAll"">Expand all</BitButton>
    <BitButton OnClick=""CollapseAll"" Variant=""BitVariant.Outline"">Collapse all</BitButton>
</BitStack>

<BitDataGrid @ref=""treeGrid"" Items=""@fileRoots"" Height=""460px"" KeyField=""n => n.Id""
             ChildrenSelector=""n => n.Children"" TreeInitiallyExpanded=""true""
             ShowSearchBox=""true"" Filterable=""true"" CellNavigation=""true"">
    <BitDataGridColumn Property=""p => p.Name"" Width=""320px"" />
    <BitDataGridColumn Property=""p => p.Kind"" Title=""Type"" Width=""120px"" />
    <BitDataGridColumn Property=""p => p.Size"" Title=""Size (bytes)"" Format=""N0"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Modified"" Format=""yyyy-MM-dd"" Align=""BitTextAlign.End"" />
</BitDataGrid>

<BitDataGrid Items=""@lazyTreeRoots"" Height=""360px"" KeyField=""n => n.Id""
             ChildrenProvider=""LoadChildrenAsync"" HasChildrenSelector=""IsFolder"">
    <BitDataGridColumn Property=""p => p.Name"" Width=""280px"" />
    <BitDataGridColumn Property=""p => p.Kind"" Title=""Type"" Width=""110px"" />
    <BitDataGridColumn Property=""p => p.Size"" Title=""Size (bytes)"" Format=""N0"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Modified"" Format=""yyyy-MM-dd"" />
</BitDataGrid>";
    private readonly string example16CsharpCode = @"
private readonly List<FileNode> fileRoots = FileSystemData.Build();
private BitDataGrid<FileNode>? treeGrid;

private async Task ExpandAll() { if (treeGrid is not null) await treeGrid.ExpandAllAsync(); }
private async Task CollapseAll() { if (treeGrid is not null) await treeGrid.CollapseAllAsync(); }

private readonly List<FileNode> lazyTreeRoots =
[
    new() { Id = 1, Name = ""src"", Kind = ""Folder"", Modified = new DateTime(2025, 1, 10) },
    new() { Id = 2, Name = ""docs"", Kind = ""Folder"", Modified = new DateTime(2025, 2, 5) },
    new() { Id = 3, Name = ""assets"", Kind = ""Folder"", Modified = new DateTime(2025, 3, 20) },
    new() { Id = 4, Name = ""LICENSE"", Kind = ""File"", Size = 1_070, Modified = new DateTime(2025, 1, 2) },
];
private int nextLazyNodeId = 1000;

// An unloaded node cannot know its children; this decides whether it offers a toggle.
private static bool IsFolder(FileNode node) => node.Kind == ""Folder"";

// Called on a node's first expand only; the grid caches the result.
private async Task<IEnumerable<FileNode>?> LoadChildrenAsync(FileNode parent)
{
    await Task.Delay(600); // e.g. await http.GetFromJsonAsync<List<FileNode>>($""api/files/{parent.Id}"")
    return
    [
        new FileNode { Id = ++nextLazyNodeId, Name = $""{parent.Name}-sub"", Kind = ""Folder"", Modified = parent.Modified.AddDays(1) },
        new FileNode { Id = ++nextLazyNodeId, Name = $""{parent.Name}-notes.md"", Kind = ""File"", Size = 2_300, Modified = parent.Modified.AddDays(2) },
        new FileNode { Id = ++nextLazyNodeId, Name = $""{parent.Name}-data.json"", Kind = ""File"", Size = 5_100, Modified = parent.Modified.AddDays(3) },
    ];
}" + FileSystemDataCode;

    private readonly string example17RazorCode = @"
<BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
    <BitButton OnClick=""() => virtualProducts = SampleData.Generate(1_000)"">1k rows</BitButton>
    <BitButton OnClick=""() => virtualProducts = SampleData.Generate(10_000)"">10k rows</BitButton>
    <BitButton OnClick=""() => virtualProducts = SampleData.Generate(100_000)"">100k rows</BitButton>
    <BitText>@virtualProducts.Count.ToString(""N0"") rows</BitText>
</BitStack>

<BitDataGrid Items=""@virtualProducts"" Height=""520px""
             Virtualize=""true"" VirtualizeColumns=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""90px"" Align=""BitTextAlign.End"" Frozen=""true"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""240px"" />
    @for (var m = 1; m <= 40; m++)
    {
        var month = m;
        <BitDataGridColumn ColumnId=""@($""m{month}"")"" Title=""@($""M{month:00}"")"" Width=""110px"" Align=""BitTextAlign.End"">
            <Template Context=""product"">@((product.Id * 37 + month * 13) % 1000)</Template>
        </BitDataGridColumn>
    }
</BitDataGrid>";
    private readonly string example17CsharpCode = @"
private List<Product> virtualProducts = SampleData.Generate(10_000);" + ProductModelCode + SampleDataCode;

    private readonly string example18RazorCode = @"
<BitDataGrid OnRead=""LoadServerData"" Height=""430px"" Loading=""serverLoading""
             Pageable=""true"" PageSize=""10"" Filterable=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Supplier"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" />
</BitDataGrid>

<BitText>@serverLastRequest</BitText>

<BitDataGrid OnRead=""LoadVirtualServerData"" Virtualize=""true"" Height=""480px"" RowHeight=""40""
             Filterable=""true"" ShowFooter=""true"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""90px"" Align=""BitTextAlign.End"" Frozen=""true"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""240px"" />
    <BitDataGridColumn Property=""p => p.Category"" Width=""140px"" />
    <BitDataGridColumn Property=""p => p.Supplier"" Width=""180px"" />
    <BitDataGridColumn Property=""p => p.Price"" Width=""140px"" Format=""C2"" Align=""BitTextAlign.End"" Aggregate=""BitDataGridAggregateType.Sum"" AggregateFormat=""C0"" />
    <BitDataGridColumn Property=""p => p.Stock"" Width=""120px"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Rating"" Width=""110px"" Format=""N1"" Align=""BitTextAlign.End"" FrozenEnd=""true"" />
</BitDataGrid>";
    private readonly string example18CsharpCode = @"
private readonly List<Product> serverAll = SampleData.Generate(523);
private bool serverLoading;
private string serverLastRequest = """";

private async Task<BitDataGridReadResult<Product>> LoadServerData(BitDataGridReadRequest request)
{
    serverLoading = true;
    await InvokeAsync(StateHasChanged);

    int total = 0;
    try
    {
        await Task.Delay(250, request.CancellationToken); // a backend round trip

        var filtered = Query(serverAll, request).ToList();
        total = filtered.Count;
        var items = filtered.Skip(request.Skip).Take(request.Take ?? total).ToList();

        // A superseded request must not hand back stale rows.
        request.CancellationToken.ThrowIfCancellationRequested();

        return new BitDataGridReadResult<Product>(items, total);
    }
    finally
    {
        // A superseded request leaves the loading state to the newer one.
        if (!request.CancellationToken.IsCancellationRequested)
        {
            serverLastRequest = $""Last request → skip {request.Skip}, take {request.Take}, sorts: {request.Sorts.Count}, filters: {request.Filters.Count}, total: {total}"";
            serverLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}

private readonly List<Product> serverVirtualAll = SampleData.Generate(100_000);

private async Task<BitDataGridReadResult<Product>> LoadVirtualServerData(BitDataGridReadRequest request)
{
    // A scroll window the grid no longer needs is cancelled; let the exception propagate.
    await Task.Delay(150, request.CancellationToken);

    var filtered = Query(serverVirtualAll, request).ToList();
    var items = filtered.Skip(request.Skip).Take(request.Take ?? filtered.Count).ToList();

    // Computed over the WHOLE filtered set, so the footer shows a real grand total.
    var priceSum = filtered.Sum(p => p.Price);
    var aggregates = new List<BitDataGridAggregateResult>
    {
        new() { ColumnId = nameof(Product.Price), Type = BitDataGridAggregateType.Sum, Value = priceSum, FormattedValue = priceSum.ToString(""C0"") }
    };

    return new BitDataGridReadResult<Product>(items, filtered.Count) { Aggregates = aggregates };
}

// Applies the request's filters, search and sorts the way a backend would.
private static IEnumerable<Product> Query(IEnumerable<Product> source, BitDataGridReadRequest request)
{
    var query = source;

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

    if (string.IsNullOrWhiteSpace(request.Search) is false)
    {
        query = query.Where(p => p.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)
                              || p.Supplier.Contains(request.Search, StringComparison.OrdinalIgnoreCase));
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
        var descending = sort.Direction == BitDataGridSortDirection.Descending;
        ordered = ordered is null
            ? (descending ? query.OrderByDescending(key) : query.OrderBy(key))
            : (descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key));
    }

    return ordered ?? query;
}

// A text filter as the grid's text editor emits it: a string value with one of the string/empty operators.
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

// Any other filter arrives as a value of the column's own type with a comparison operator.
private static bool MatchComparable<T>(T value, BitDataGridFilterDescriptor f) where T : IComparable
{
    if (f.Operator is BitDataGridFilterOperator.IsEmpty) return value is null;
    if (f.Operator is BitDataGridFilterOperator.IsNotEmpty) return value is not null;
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

    private readonly string example19RazorCode = @"
<BitDataGrid OnLoadMore=""LoadMore"" LoadMoreBatchSize=""40"" Height=""520px"" RowHeight=""40"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""90px"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""240px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Supplier"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" />
</BitDataGrid>

<BitText>@infiniteLog</BitText>";
    private readonly string example19CsharpCode = @"
private readonly List<Product> infiniteAll = SampleData.Generate(2_017);
private string infiniteLog = ""Scroll down to load more…"";
private int infiniteRequests;

private async Task<BitDataGridReadResult<Product>> LoadMore(BitDataGridReadRequest request)
{
    await Task.Delay(350, request.CancellationToken); // a backend round trip

    // Take is the batch size while scrolling; null means ""every row"" (an export).
    var batch = Query(infiniteAll, request).Skip(request.Skip).Take(request.Take ?? infiniteAll.Count).ToList();

    request.CancellationToken.ThrowIfCancellationRequested();

    infiniteRequests++;
    infiniteLog = batch.Count == 0
        ? $""Batch #{infiniteRequests} → no more rows""
        : $""Batch #{infiniteRequests} → rows {request.Skip + 1}–{request.Skip + batch.Count}"";
    await InvokeAsync(StateHasChanged);

    // There is no known total in this mode; a batch shorter than requested ends the list.
    return new BitDataGridReadResult<Product>(batch, 0);
}

// Query applies request.Sorts and request.Filters - see the Server-side data example." + ProductModelCode + SampleDataCode;

    private readonly string example20RazorCode = @"
<BitDataGrid Items=""@queryableProducts"" Height=""430px""
             Filterable=""true"" Pageable=""true"" PageSize=""10"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Supplier"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" />
</BitDataGrid>";
    private readonly string example20CsharpCode = @"
// Any IQueryable works; with EF Core this is simply: private IQueryable<Product> queryableProducts => db.Products;
private readonly IQueryable<Product> queryableProducts = SampleData.Generate(400).AsQueryable();" + ProductModelCode + SampleDataCode;

    private readonly string example21RazorCode = @"
<BitDataGrid Items=""@exportProducts"" Height=""460px"" Filterable=""true""
             ShowCsvExport=""true"" ShowExcelExport=""true"" ExcelExportStyled=""true"" ExportFileName=""products"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""80px"" Align=""BitTextAlign.End"" Frozen=""true"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" Frozen=""true"" ColSpan=""p => NameSpan(p)"">
        <Template Context=""p"">
            @if (p.Discontinued)
            {
                <span class=""span-banner"">⚠ @p.Name - discontinued</span>
            }
            else
            {
                @p.Name
            }
        </Template>
    </BitDataGridColumn>
    <BitDataGridColumn Property=""p => p.Category"" Width=""170px"" />
    <BitDataGridColumn Property=""p => p.Price"" Width=""150px"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.ReleaseDate"" Title=""Released"" Width=""130px"" Format=""d"" />
    <BitDataGridColumn Property=""p => p.Supplier"" Width=""200px"" Exportable=""false"" />
    <BitDataGridColumn ColumnId=""Value"" Title=""Value"" Width=""140px"" Align=""BitTextAlign.End""
                       SortBy=""@(p => p.Price * p.Stock)"" ExportValue=""@(p => p.Price * p.Stock)"" Format=""C0"">
        <Template Context=""p"">@((p.Price * p.Stock).ToString(""C0""))</Template>
    </BitDataGridColumn>
</BitDataGrid>";
    private readonly string example21CsharpCode = @"
private readonly List<Product> exportProducts = SampleData.Generate(30);

// A merged cell in Excel; CSV has no merges and writes every column's own value.
private int? NameSpan(Product p) => p.Discontinued ? 2 : null;

// The same exports from code (all return or download every matching row):
//     string csv = await grid.ToCsvAsync();
//     byte[] xlsx = await grid.ToExcelAsync(selectedOnly: true);" + ProductModelCode + SampleDataCode;

    private readonly string example22RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@reorderProducts"" Height=""460px"" KeyField=""p => p.Id""
             RowReorderable=""true"" OnRowReorder=""OnReorder"" Sortable=""false"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
</BitDataGrid>

<BitText>@reorderLog</BitText>";
    private readonly string example22CsharpCode = @"
private readonly List<Product> reorderProducts = SampleData.Generate(12);
private string? reorderLog;

private void OnReorder(BitDataGridRowReorderEventArgs<Product> e)
{
    // FromIndex/ToIndex are null when the bound Items isn't an indexable IList<T>.
    var from = e.FromIndex is int fi ? (fi + 1).ToString() : ""?"";
    var to = e.ToIndex is int ti ? (ti + 1).ToString() : ""?"";
    reorderLog = $""{e.DraggedItem.Name} moved from #{from} to #{to}"";
}" + ProductModelCode + SampleDataCode;

    private readonly string example23RazorCode = @"
<BitDataGrid @ref=""cellEventsGrid"" TItem=""Product"" Items=""@cellEventsProducts"" Height=""420px""
             OnCellClick=""OnCellClick""
             OnCellDoubleClick=""OnCellDoubleClick""
             OnCellContextMenu=""OnCellContextMenu"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
</BitDataGrid>

<BitText>@cellEventStatus</BitText>

@if (cellMenuArgs is not null)
{
    <div class=""ctx-menu-overlay""
         @onclick=""CloseCellMenu""
         @oncontextmenu=""CloseCellMenu"" @oncontextmenu:preventDefault=""true""></div>
    <div class=""ctx-menu"" role=""menu"" style=""left:@(cellMenuX)px;top:@(cellMenuY)px""
         @onkeydown=""OnCellMenuKeyDown"">
        <div class=""ctx-menu-header"">@cellMenuArgs.Item.Name - @cellMenuArgs.ColumnTitle</div>
        <button type=""button"" class=""ctx-menu-item"" role=""menuitem"" @ref=""cellMenuFirstItem"" @onclick=""CopyCellValue"">Copy value</button>
        <button type=""button"" class=""ctx-menu-item ctx-menu-danger"" role=""menuitem"" @onclick=""DeleteCellMenuRow"">Delete row</button>
    </div>
}";
    private readonly string example23CsharpCode = @"
private readonly List<Product> cellEventsProducts = SampleData.Generate(40);
private string cellEventStatus = ""Click, double-click or right-click any cell."";
private BitDataGrid<Product>? cellEventsGrid;
private BitDataGridCellEventArgs<Product>? cellMenuArgs;
private int cellMenuX;
private int cellMenuY;
private ElementReference cellMenuFirstItem;
private bool cellMenuFocusPending;

private void OnCellClick(BitDataGridCellEventArgs<Product> e)
    => cellEventStatus = $""Clicked {e.ColumnTitle} = \""{e.Value}\"" on {e.Item.Name}"";

private void OnCellDoubleClick(BitDataGridCellEventArgs<Product> e)
    => cellEventStatus = $""Double-clicked {e.ColumnTitle} on {e.Item.Name}"";

private void OnCellContextMenu(BitDataGridCellEventArgs<Product> e)
{
    cellMenuArgs = e;
    // ClientX/Y are viewport coordinates, matching the menu's position:fixed placement.
    cellMenuX = (int)e.Mouse.ClientX;
    cellMenuY = (int)e.Mouse.ClientY;
    cellEventStatus = $""Right-clicked {e.ColumnTitle} on {e.Item.Name}"";
    cellMenuFocusPending = true;
}

private void CloseCellMenu() => cellMenuArgs = null;

private void OnCellMenuKeyDown(KeyboardEventArgs e)
{
    if (e.Key == ""Escape"") CloseCellMenu();
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    // Move the focus into the menu once it renders, so the keyboard can operate and dismiss it.
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
    cellEventStatus = $""Copied \""{cellMenuArgs.Value}\"" to the clipboard."";
    cellMenuArgs = null;
}

private async Task DeleteCellMenuRow()
{
    if (cellMenuArgs is null) return;
    cellEventsProducts.Remove(cellMenuArgs.Item);
    cellEventStatus = $""Deleted {cellMenuArgs.Item.Name}."";
    cellMenuArgs = null;
    // The grid caches its processed view; a list changed in place needs an explicit refresh.
    if (cellEventsGrid is not null) await cellEventsGrid.RefreshAsync();
}" + ProductModelCode + SampleDataCode;
    private readonly DemoCodeFile[] example23CodeFiles =
    [
        new("Page.razor.css", example23CssCode),
    ];
    private const string example23CssCode = @"
/* The invisible overlay catches the click or right-click that dismisses the menu. */
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
    border-radius: var(--bit-shp-radius-popup);
    box-shadow: var(--bit-shd-popup);
}

.ctx-menu-header {
    padding: 6px 10px;
    margin-bottom: 4px;
    font-size: 0.75rem;
    color: var(--bit-clr-fg-sec);
    border-bottom: 1px solid var(--bit-clr-brd-ter);
    white-space: nowrap;
}

.ctx-menu-item {
    padding: 6px 10px;
    border: none;
    border-radius: 4px;
    background: none;
    color: var(--bit-clr-fg-pri);
    font: inherit;
    text-align: start;
    cursor: pointer;
}

.ctx-menu-item:hover,
.ctx-menu-item:focus-visible {
    background: var(--bit-clr-bg-pri-hover);
}

.ctx-menu-danger {
    color: var(--bit-clr-err-fg);
}";

    private readonly string example24RazorCode = @"
<BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
    <BitButton OnClick=""ApiSortByPrice"">Sort by price ↓</BitButton>
    <BitButton OnClick=""ApiClearSorts"" Variant=""BitVariant.Outline"">Clear sorts</BitButton>
    <BitButton OnClick=""ApiFilterExpensive"">Filter price &gt; 500</BitButton>
    <BitButton OnClick=""ApiClearFilters"" Variant=""BitVariant.Outline"">Clear filters</BitButton>
    <BitButton OnClick=""ApiGroupByCategory"">Group by category</BitButton>
    <BitButton OnClick=""ApiUngroup"" Variant=""BitVariant.Outline"">Ungroup</BitButton>
    <BitButton OnClick=""ApiGoToPage3"">Go to page 3</BitButton>
    <BitButton OnClick=""ApiPageSize50"">Page size 50</BitButton>
    <BitButton OnClick=""ApiPriceFirst"">Move Price first</BitButton>
    <BitButton OnClick=""ApiRefresh"" Variant=""BitVariant.Outline"">Refresh</BitButton>
</BitStack>

<BitDataGrid @ref=""apiGrid"" TItem=""Product"" Items=""@apiProducts"" Height=""430px""
             Filterable=""true"" Groupable=""true"" Pageable=""true"" PageSize=""10""
             OnSortChange=""OnApiSortChange"" OnFilterChange=""OnApiFilterChange""
             OnGroupChange=""OnApiGroupChange"" OnPageChange=""OnApiPageChange"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" Filterable=""false"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" Groupable=""false"" />
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" Groupable=""false"" />
</BitDataGrid>

<BitText>@apiLog</BitText>";
    private readonly string example24CsharpCode = @"
private readonly List<Product> apiProducts = SampleData.Generate(200);
private BitDataGrid<Product>? apiGrid;
private string apiLog = ""The grid reports every view change here."";

private async Task ApiSortByPrice() { if (apiGrid is not null) await apiGrid.SortByAsync(nameof(Product.Price), BitDataGridSortDirection.Descending); }
private async Task ApiClearSorts() { if (apiGrid is not null) await apiGrid.ClearSortsAsync(); }
private async Task ApiFilterExpensive() { if (apiGrid is not null) await apiGrid.ApplyFilterAsync(nameof(Product.Price), BitDataGridFilterOperator.GreaterThan, 500m); }
private async Task ApiClearFilters() { if (apiGrid is not null) await apiGrid.ClearFiltersAsync(); }
private async Task ApiGroupByCategory() { if (apiGrid is not null) await apiGrid.GroupByAsync(nameof(Product.Category)); }
private async Task ApiUngroup() { if (apiGrid is not null) await apiGrid.UngroupAsync(nameof(Product.Category)); }
private async Task ApiGoToPage3() { if (apiGrid is not null) await apiGrid.GoToPageAsync(3); }
private async Task ApiPageSize50() { if (apiGrid is not null) await apiGrid.SetPageSizeAsync(50); }
private async Task ApiPriceFirst() { if (apiGrid is not null) await apiGrid.MoveColumnAsync(nameof(Product.Price), 0); }
private async Task ApiRefresh() { if (apiGrid is not null) await apiGrid.RefreshAsync(); }

private void OnApiSortChange(IReadOnlyList<BitDataGridSortDescriptor> sorts)
    => apiLog = sorts.Count == 0 ? ""Sorting cleared."" : $""Sorting by {string.Join("", "", sorts.Select(s => $""{s.ColumnId} {s.Direction}""))}."";

private void OnApiFilterChange(IReadOnlyList<BitDataGridFilterDescriptor> filters)
    => apiLog = filters.Count == 0 ? ""Filters cleared."" : $""{filters.Count} filter(s) active."";

private void OnApiGroupChange(IReadOnlyList<BitDataGridGroupDescriptor> groups)
    => apiLog = groups.Count == 0 ? ""Grouping cleared."" : $""Grouped by {string.Join("" › "", groups.Select(g => g.ColumnId))}."";

private void OnApiPageChange(int page) => apiLog = $""Moved to page {page}."";" + ProductModelCode + SampleDataCode;

    private readonly string example25RazorCode = @"
<BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
    <BitButton OnClick=""() => RecreateGrid(restore: true)"" Disabled=""savedGridState is null"">Recreate &amp; restore</BitButton>
    <BitButton OnClick=""() => RecreateGrid(restore: false)"" Variant=""BitVariant.Outline"">Recreate fresh</BitButton>
    <BitText>@gridStateStatus</BitText>
</BitStack>

<BitDataGrid @key=""stateGridKey"" @ref=""stateGrid"" TItem=""Product"" Items=""@stateProducts"" Height=""430px""
             Filterable=""true"" Resizable=""true"" Reorderable=""true"" ShowColumnChooser=""true""
             Pageable=""true"" PageSize=""10"" OnStateChange=""SaveGridState"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Stock"" Align=""BitTextAlign.End"" />
</BitDataGrid>";
    private readonly string example25CsharpCode = @"
private readonly List<Product> stateProducts = SampleData.Generate(120);
private BitDataGrid<Product>? stateGrid;
private BitDataGridState? savedGridState;
private int stateGridKey;
private bool stateRestorePending;
private string gridStateStatus = ""Sort, filter, resize, move or hide columns - every change is saved."";

// A real app would serialize the snapshot to local storage or a user-preferences store here.
private void SaveGridState(BitDataGridState state)
{
    savedGridState = state;
    gridStateStatus = $""Saved: page {state.CurrentPage}, {state.Sorts.Count} sort(s), {state.Filters.Count} filter(s)."";
}

// A new key mounts a brand-new grid, the way a page reload would.
private void RecreateGrid(bool restore)
{
    stateGridKey++;
    stateRestorePending = restore && savedGridState is not null;
    gridStateStatus = stateRestorePending ? ""Recreated and restored."" : ""Recreated with its defaults."";
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    // The new grid has rendered and its columns have registered, so the snapshot can be applied.
    if (stateRestorePending && stateGrid is not null && savedGridState is not null)
    {
        stateRestorePending = false;
        await stateGrid.ApplyStateAsync(savedGridState);
    }
}" + ProductModelCode + SampleDataCode;

    private readonly string example26RazorCode = @"
<BitDataGrid Items=""@localizedProducts"" Height=""420px"" Strings=""@germanStrings""
             ShowSearchBox=""true"" Filterable=""true"" Pageable=""true"" PageSize=""8"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""Nr."" Width=""80px"" Align=""BitTextAlign.End"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Title=""Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Price"" Title=""Preis"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Stock"" Title=""Bestand"" Align=""BitTextAlign.End"" />
</BitDataGrid>";
    private readonly string example26CsharpCode = @"
private readonly List<Product> localizedProducts = SampleData.Generate(60);

// Every string has an English default; override what you need.
private readonly BitDataGridStrings germanStrings = new()
{
    GridLabel = ""Produkte"",
    EmptyText = ""Keine Einträge vorhanden."",
    LoadingText = ""Wird geladen…"",
    SearchPlaceholder = ""Suchen…"",
    SearchLabel = ""Alle Spalten durchsuchen"",
    ClearSearchLabel = ""Suche löschen"",
    FilterPlaceholder = ""Filtern…"",
    FilterByFormat = ""Nach {0} filtern"",
    FilterAllText = ""Alle"",
    ClearFiltersText = ""Filter löschen"",
    PagerRangeFormat = ""{0}–{1} von {2}"",
    PagerPageFormat = ""Seite {0} von {1}"",
    PerPageFormat = ""{0} pro Seite"",
    RowsPerPageLabel = ""Zeilen pro Seite"",
    FirstPageLabel = ""Erste Seite"",
    PreviousPageLabel = ""Vorherige Seite"",
    NextPageLabel = ""Nächste Seite"",
    LastPageLabel = ""Letzte Seite"",
    AnnouncementSortedAscending = ""Nach {0} aufsteigend sortiert"",
    AnnouncementSortedDescending = ""Nach {0} absteigend sortiert"",
    AnnouncementSortCleared = ""Sortierung nach {0} entfernt"",
    AnnouncementPage = ""Seite {0} von {1}"",
};" + ProductModelCode + SampleDataCode;

    private readonly string example27RazorCode = @"
<BitToggle @bind-Value=""gridDisabled"" Label=""Disabled"" Inline />

<BitDataGrid Items=""@disabledProducts"" Height=""380px"" Disabled=""gridDisabled""
             SelectionMode=""BitSelectionMode.Multiple"" CellNavigation=""true"" ClipboardCopy=""true""
             Filterable=""true"" Resizable=""true"" Pageable=""true"" PageSize=""8"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" Filterable=""false"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
</BitDataGrid>";
    private readonly string example27CsharpCode = @"
private readonly List<Product> disabledProducts = SampleData.Generate(40);
private bool gridDisabled = true;" + ProductModelCode + SampleDataCode;

    private readonly string example28RazorCode = @"
<BitParams Parameters=""@dataGridParams"">
    <BitDataGrid Items=""@cascadingProducts"" AriaLabel=""Cascaded defaults"">
        <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
        <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
        <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    </BitDataGrid>

    <BitDataGrid Items=""@cascadingProducts"" AriaLabel=""Its own Striped"" Striped=""true"">
        <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
        <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
        <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    </BitDataGrid>
</BitParams>";
    private readonly string example28CsharpCode = @"
private readonly List<Product> cascadingProducts = SampleData.Generate(30);

private readonly BitDataGridParams[] dataGridParams =
[
    new()
    {
        Striped = false,
        ShowRowNumbers = true,
        Pageable = true,
        PageSize = 5,
        PageSizeOptions = [5, 10],
        Filterable = true,
    }
];" + ProductModelCode + SampleDataCode;

    private readonly string example29RazorCode = @"
<div class=""styled-grid"">
    <BitDataGrid Items=""@styleProducts"" Height=""300px"" Style=""box-shadow: var(--bit-shd-sm);""
                 Classes=""@(new() { HeaderCell = ""custom-header-cell"", SelectedRow = ""custom-selected-row"" })""
                 Styles=""@(new() { Root = ""border-radius: 1rem;"", Pager = ""justify-content: center;"" })""
                 SelectionMode=""BitSelectionMode.Multiple"" @bind-SelectedItems=""styleSelection""
                 Pageable=""true"" PageSize=""5"">
        <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
        <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
        <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
    </BitDataGrid>
</div>

<BitDataGrid Items=""@styleProducts"" Height=""300px"" ShowRowNumbers=""true""
             SelectionMode=""BitSelectionMode.Multiple""
             Style=""--bit-DataGrid-cell-padding: 2px 8px;
                    --bit-DataGrid-font-size: var(--bit-tpg-fs-xs);
                    --bit-DataGrid-header-background: var(--bit-clr-pri);
                    --bit-DataGrid-header-color: var(--bit-clr-pri-text);
                    --bit-DataGrid-selected-background: var(--bit-clr-suc-tint);
                    --bit-DataGrid-accent-color: var(--bit-clr-suc);
                    --bit-DataGrid-border-radius: 0;"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""ID"" Width=""70px"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Name"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" />
    <BitDataGridColumn Property=""p => p.Price"" Format=""C2"" Align=""BitTextAlign.End"" />
</BitDataGrid>";
    private readonly string example29CsharpCode = @"
private readonly List<Product> styleProducts = SampleData.Generate(20);
private IReadOnlyList<Product> styleSelection = [];" + ProductModelCode + SampleDataCode;
    private readonly DemoCodeFile[] example29CodeFiles =
    [
        new("Page.razor.css", example29CssCode),
    ];
    private const string example29CssCode = @"
/* Classes puts these on parts the grid renders, so they are reached through ::deep off a wrapper the
   page owns. */
.styled-grid ::deep .custom-header-cell {
    color: var(--bit-clr-pri);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.styled-grid ::deep .custom-selected-row .bit-dtg-cell {
    font-style: italic;
}";

    private readonly string example30RazorCode = @"
<BitDataGrid Items=""@rtlProducts"" Height=""420px"" Dir=""BitDir.Rtl"" Strings=""@persianStrings""
             Resizable=""true"" Pageable=""true"" PageSize=""8"">
    <BitDataGridColumn Property=""p => p.Id"" Title=""شناسه"" Width=""90px"" Align=""BitTextAlign.End"" Frozen=""true"" />
    <BitDataGridColumn Property=""p => p.Name"" Title=""نام"" Width=""220px"" />
    <BitDataGridColumn Property=""p => p.Category"" Title=""دسته‌بندی"">
        <Template Context=""product"">@CategoryFa(product.Category)</Template>
    </BitDataGridColumn>
    <BitDataGridColumn Property=""p => p.Price"" Title=""قیمت"" Format=""C2"" Align=""BitTextAlign.End"" />
    <BitDataGridColumn Property=""p => p.Stock"" Title=""موجودی"" Align=""BitTextAlign.End"" />
</BitDataGrid>";
    private readonly string example30CsharpCode = @"
private readonly List<Product> rtlProducts = SampleData.GeneratePersian(60);

private readonly BitDataGridStrings persianStrings = new()
{
    GridLabel = ""محصولات"",
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
};

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
