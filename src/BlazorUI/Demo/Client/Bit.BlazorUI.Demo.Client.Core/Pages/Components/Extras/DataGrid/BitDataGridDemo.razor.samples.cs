namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public partial class BitDataGridDemo
{
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
private List<Product> products = SampleData.Generate(50);";

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
private List<Product> products = SampleData.Generate(200);";

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
private BitDataGridSelectionMode selectionMode = BitDataGridSelectionMode.Multiple;
private IReadOnlyList<Product> selected = new List<Product>();";

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

private Product CreateProduct() => new() { Id = NextId(), Name = ""New product"", Category = Category.Electronics };
private void OnCreate(Product p) { /* ... */ }
private void OnSave(Product p) { if (!products.Contains(p)) products.Insert(0, p); }
private void OnDelete(Product p) => products.Remove(p);";

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
private List<Product> products = SampleData.Generate(80);";

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
private List<Product> products = SampleData.Generate(30);";

    private readonly string example7RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""430px"" Resizable=""true"" Reorderable=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Width=""80px"" Frozen=""true"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" Width=""220px"" Frozen=""true"" />
    <BitDataGridColumn TItem=""Product"" Field=""Category"" Width=""160px"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Width=""160px"" Format=""C2"" Align=""BitDataGridColumnAlign.Right"" />
</BitDataGrid>";
    private readonly string example7CsharpCode = @"
private List<Product> products = SampleData.Generate(40);";

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
private List<Product> products = SampleData.Generate(40);";

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
private int? PriceSpan(Product p) => p.Price > 800 ? 2 : null;";

    private readonly string example10RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""520px""
             Virtualize=""true"" RowHeight=""36"" Sortable=""true"" Filterable=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Filterable=""false"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example10CsharpCode = @"
private List<Product> products = SampleData.Generate(10_000);";

    private readonly string example11RazorCode = @"
<BitDataGrid TItem=""Product"" OnRead=""LoadData"" Height=""430px""
             Pageable=""true"" PageSize=""10"" Sortable=""true"" Filterable=""true"" Loading=""loading"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Filterable=""false"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example11CsharpCode = @"
async Task<BitDataGridReadResult<Product>> LoadData(BitDataGridReadRequest request)
{
    // request.Sorts, request.Filters, request.Skip, request.Take
    var page = await Backend.QueryAsync(request);
    return new BitDataGridReadResult<Product>(page.Items, page.TotalCount);
}";

    private readonly string example12RazorCode = @"
<BitDataGrid TItem=""Product"" OnLoadMore=""LoadMore"" LoadMoreBatchSize=""40""
             Height=""520px"" RowHeight=""40"" Sortable=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example12CsharpCode = @"
async Task<BitDataGridReadResult<Product>> LoadMore(BitDataGridReadRequest request)
{
    var batch = Query.Skip(request.Skip).Take(request.Take ?? 40).ToList();
    // Return fewer rows than requested to signal the end of the data.
    return new BitDataGridReadResult<Product>(batch, 0);
}";

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
private async Task CollapseAll() { if (grid is not null) await grid.CollapseAllAsync(); }";

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
private List<SupplierModel> suppliers = BuildSuppliers();";

    private readonly string example15RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""460px""
             RowReorderable=""true"" OnRowReorder=""OnReorder"" Sortable=""false"">
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example15CsharpCode = @"
void OnReorder(BitDataGridRowReorderEventArgs<Product> e)
{
    // e.DraggedItem, e.FromIndex, e.ToIndex
}";

    private readonly string example16RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""420px""
             OnCellClick=""OnCellClick""
             OnCellDoubleClick=""OnCellDoubleClick""
             OnCellContextMenu=""OnCellContextMenu"">
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example16CsharpCode = @"
void OnCellClick(BitDataGridCellEventArgs<Product> e) { /* e.Item, e.ColumnTitle, e.Value */ }
void OnCellContextMenu(BitDataGridCellEventArgs<Product> e) { /* e.Mouse.ClientX / e.Mouse.ClientY */ }";

    private readonly string example17RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""460px""
             CellNavigation=""true"" Sortable=""true"" Editable=""true"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Editable=""false"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example17CsharpCode = @"
private List<Product> products = SampleData.Generate(40);";

    private readonly string example18RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""480px"" Sortable=""true""
             RowHeightSelector=""RowHeight"">
    <BitDataGridColumn TItem=""Product"" Field=""Name"">
        <Template Context=""p""><strong>@p.Name</strong></Template>
    </BitDataGridColumn>
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example18CsharpCode = @"
private float RowHeight(Product p) => p.Price > 500 ? 64f : 36f;";

    private readonly string example19RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@items"" Height=""320px"" Sortable=""true"">
    <EmptyTemplate>
        <div>Nothing here yet. Try loading the sample data or adjusting your filters.</div>
    </EmptyTemplate>
    <ChildContent>
        <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" />
        <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    </ChildContent>
</BitDataGrid>";
    private readonly string example19CsharpCode = @"
private List<Product> items = new(); // empty";

    private readonly string example20RazorCode = @"
<BitDataGrid TItem=""Product"" Items=""@products"" Height=""420px""
             Class=""@theme"" Bordered=""@bordered"" Striped=""@striped""
             Direction=""@(rtl ? BitDataGridDirection.Rtl : BitDataGridDirection.Ltr)""
             Sortable=""true"" Pageable=""true"" PageSize=""8"">
    <BitDataGridColumn TItem=""Product"" Field=""Id"" Title=""ID"" Frozen=""true"" />
    <BitDataGridColumn TItem=""Product"" Field=""Name"" />
    <BitDataGridColumn TItem=""Product"" Field=""Price"" Format=""C2"" />
</BitDataGrid>";
    private readonly string example20CsharpCode = @"
/* Override CSS tokens to create a theme: */
.theme-emerald {
    --bit-dtg-accent: #0f9d58;
    --bit-dtg-header-bg: light-dark(#e7f6ee, #10241a);
    --bit-dtg-row-selected: light-dark(#c9efda, #14402a);
}";
}
