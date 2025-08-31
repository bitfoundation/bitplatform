using Bit.BlazorUI.Demo.Client.Core.Components;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public partial class BitDataGridDemo : AppComponentBase
{
    private readonly string example1RazorCode = @"
<style>
    .grid .grid-container {
        overflow: auto;
    }

    .grid table {
        width: 100%;
        border-spacing: 0;
    }

    .grid tr {
        height: 30px;
    }
</style>

<div class=""grid"">
    <div class=""grid-container"">
        <BitDataGrid Items=""@FilteredItems1"" ResizableColumns=""true"" Pagination=""@pagination1"">
            <BitDataGridPropertyColumn Property=""@(c => c.Name)"" Sortable=""true"" IsDefaultSort=""BitDataGridSortDirection.Ascending"">
                <ColumnOptions>
                    <BitSearchBox @bind-Value=""typicalSampleNameFilter1""
                                  FixedIcon
                                  Immediate DebounceTime=""300""
                                  Placeholder=""Search on Name""
                                  InputHtmlAttributes=""@(new Dictionary<string, object> {{""autofocus"", true}})"" />
                </ColumnOptions>
            </BitDataGridPropertyColumn>
            <BitDataGridPropertyColumn Property=""@(c => c.Medals.Gold)"" Sortable=""true"" />
            <BitDataGridPropertyColumn Property=""@(c => c.Medals.Silver)"" Sortable=""true"" />
            <BitDataGridPropertyColumn Property=""@(c => c.Medals.Bronze)"" Sortable=""true"" />
            <BitDataGridPropertyColumn Property=""@(c => c.Medals.Total)"" Sortable=""true"" />
        </BitDataGrid>
    </div>
    <BitDataGridPaginator Value=""@pagination1"" />
</div>";
    private readonly string example1CsharpCode = @"
private IQueryable<CountryModel> allCountries;
private string typicalSampleNameFilter1 = string.Empty;
private BitDataGridPaginationState pagination1 = new() { ItemsPerPage = 7 };
private IQueryable<CountryModel> FilteredItems1 => 
    allCountries?.Where(x => x.Name.Contains(typicalSampleNameFilter1 ?? string.Empty, StringComparison.CurrentCultureIgnoreCase));

protected override async Task OnInitializedAsync()
{
    allCountries = _countries.AsQueryable();
}

private readonly static CountryModel[] _countries = new[]
{
    new CountryModel { Code = ""AR"", Name = ""Argentina"", MedalsModel = new MedalsModel { Gold = 0, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""AM"", Name = ""Armenia"", MedalsModel = new MedalsModel { Gold = 0, Silver = 2, Bronze = 2 } },
    new CountryModel { Code = ""AU"", Name = ""Australia"", MedalsModel = new MedalsModel { Gold = 17, Silver = 7, Bronze = 22 } },
    new CountryModel { Code = ""AT"", Name = ""Austria"", MedalsModel = new MedalsModel { Gold = 1, Silver = 1, Bronze = 5 } },
    new CountryModel { Code = ""AZ"", Name = ""Azerbaijan"", MedalsModel = new MedalsModel { Gold = 0, Silver = 3, Bronze = 4 } },
    new CountryModel { Code = ""BS"", Name = ""Bahamas"", MedalsModel = new MedalsModel { Gold = 2, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""BH"", Name = ""Bahrain"", MedalsModel = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    // ...
};

public class CountryModel
{
    public string Code { get; set; }
    public string Name { get; set; }
    public MedalsModel MedalsModel { get; set; }
}

public class MedalsModel
{
    public int Gold { get; set; }
    public int Silver { get; set; }
    public int Bronze { get; set; }
    public int Total => Gold + Silver + Bronze;
}";

    private readonly string example2RazorCode = @"
<style>
    .grid {
        border: 1px solid;
        border-color: var(--bit-clr-brd-pri);
    }

    .grid .grid-container {
        overflow: auto;
    }

    .grid .flag {
        vertical-align: middle;
    }

    .grid table {
        width: 100%;
        border-spacing: 0;
    }

    .grid tr {
        height: 42px;
    }

    .grid th {
        padding: .5rem;
        font-weight: bold;
        display: table-cell;
        vertical-align: inherit;
        border-bottom: 1px solid;
        background-color: var(--bit-clr-bg-sec);
        border-bottom-color: var(--bit-clr-brd-sec);
    }

    .grid .col-options-button {
        cursor: pointer;
        background-image: none;
    }

    .grid .col-options-button:before {
        top: 2px;
        color: #7A7A7C;
        content: ""\E721"";
        position: relative;
        font-style: normal;
        font-weight: normal;
        display: inline-block;
        font-family: 'Fabric MDL2';
    }

    .grid .col-width-draghandle {
        width: 3px;
        cursor: col-resize;
    }

    .grid .col-width-draghandle:active {
        background: unset;
    }

    .grid .col-width-draghandle:after {
        left: 3px;
        border-left: unset;
    }

    .grid .col-width-draghandle:hover {
        background: unset;
    }

    .grid .column--large {
        width: 220px;
    }

    .grid th:not(:last-child) {
        border-right: 1px solid;
        border-right-color: var(--bit-clr-brd-sec);
    }

    .grid th:not(.col-sort-asc):not(.col-sort-desc) .sort-indicator:before {
        top: -2px;
        color: #7A7A7C;
        content: ""\21C5"";
        position: relative;
        font-style: normal;
        font-weight: normal;
        display: inline-block;
        font-family: 'Fabric MDL2';
    }

    .grid tr:nth-child(even) {
        background: var(--bit-clr-bg-sec);
    }

    .grid tr:nth-child(odd) {
        background: var(--bit-clr-bg-pri);
    }

    .grid tr:last-child > td {
        border-bottom: none;
    }

    .grid td {
        overflow: hidden;
        white-space: nowrap;
        padding: 0.25rem 0.5rem;
        text-overflow: ellipsis;
        border-bottom: 1px solid var(--bit-clr-brd-sec);
    }

    .grid td:not(:last-child) {
        border-right: 1px solid var(--bit-clr-brd-sec);
    }

    .grid .sort-indicator {
        margin-left: auto;
    }

    .grid .col-width-draghandle:after {
        border-left: unset;
    }

    .grid .col-header-content {
        padding-right: 0px;
    }

    .grid .bitdatagrid-paginator {
        margin-top: 0;
        padding: 0.5rem;
        border-top: 1px solid;
    }

    .grid .bitdatagrid-paginator .pagination-text {
        padding-top: 3px;
    }

    .grid .bitdatagrid-paginator nav button {
        border-radius: 0.25rem;
    }

    .grid .bitdatagrid-paginator nav button:before {
        vertical-align: middle;
    }
</style>

<div class=""grid"">
    <div class=""grid-container"">
        <BitDataGrid Items=""@FilteredItems2"" ResizableColumns=""true"" Pagination=""@pagination2"">
            <BitDataGridPropertyColumn Class=""column--large"" Property=""@(c => c.Name)"" Sortable=""true"" IsDefaultSort=""BitDataGridSortDirection.Ascending"">
                <ColumnOptions>
                    <BitSearchBox @bind-Value=""typicalSampleNameFilter2""
                                  FixedIcon
                                  Immediate DebounceTime=""300""
                                  Placeholder=""Search on Name""
                                  InputHtmlAttributes=""@(new Dictionary<string, object> {{""autofocus"", true}})"" />
                </ColumnOptions>
            </BitDataGridPropertyColumn>
            <BitDataGridTemplateColumn Title=""Flag"" Align=""BitDataGridAlign.Center"">
                <img class=""flag"" src=""https://flagsapi.com/@(context.Code)/shiny/32.png"" loading=""lazy"" alt=""@(context.Code)"" />
            </BitDataGridTemplateColumn>
            <BitDataGridPropertyColumn Property=""@(c => c.Medals.Gold)"" Sortable=""true"" />
            <BitDataGridPropertyColumn Property=""@(c => c.Medals.Silver)"" Sortable=""true"" />
            <BitDataGridPropertyColumn Property=""@(c => c.Medals.Bronze)"" Sortable=""true"" />
            <BitDataGridTemplateColumn Title=""Action"" Align=""BitDataGridAlign.Center"">
                <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"" Title=""Edit"" />
                <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Delete"" Title=""Delete"" />
            </BitDataGridTemplateColumn>
        </BitDataGrid>
    </div>
    <BitDataGridPaginator Value=""@pagination2"" SummaryFormat=""@(v => $""Total: {v.TotalItemCount}"")"">
        <TextTemplate Context=""state"">@(state.CurrentPageIndex + 1) / @(state.LastPageIndex + 1)</TextTemplate>
    </BitDataGridPaginator>
</div>";
    private readonly string example2CsharpCode = @"
private IQueryable<CountryModel> allCountries;
private string typicalSampleNameFilter2 = string.Empty;
private BitDataGridPaginationState pagination2 = new() { ItemsPerPage = 7 };
private IQueryable<CountryModel> FilteredItems2 
    => allCountries?.Where(x => x.Name.Contains(typicalSampleNameFilter2 ?? string.Empty, StringComparison.CurrentCultureIgnoreCase));

protected override async Task OnInitializedAsync()
{
    allCountries = _countries.AsQueryable();
}

private readonly static CountryModel[] _countries = new[]
{
    new CountryModel { Code = ""AR"", Name = ""Argentina"", MedalsModel = new MedalsModel { Gold = 0, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""AM"", Name = ""Armenia"", MedalsModel = new MedalsModel { Gold = 0, Silver = 2, Bronze = 2 } },
    new CountryModel { Code = ""AU"", Name = ""Australia"", MedalsModel = new MedalsModel { Gold = 17, Silver = 7, Bronze = 22 } },
    new CountryModel { Code = ""AT"", Name = ""Austria"", MedalsModel = new MedalsModel { Gold = 1, Silver = 1, Bronze = 5 } },
    new CountryModel { Code = ""AZ"", Name = ""Azerbaijan"", MedalsModel = new MedalsModel { Gold = 0, Silver = 3, Bronze = 4 } },
    new CountryModel { Code = ""BS"", Name = ""Bahamas"", MedalsModel = new MedalsModel { Gold = 2, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""BH"", Name = ""Bahrain"", MedalsModel = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    // ...
};

public class CountryModel
{
    public string Code { get; set; }
    public string Name { get; set; }
    public MedalsModel MedalsModel { get; set; }
}

public class MedalsModel
{
    public int Gold { get; set; }
    public int Silver { get; set; }
    public int Bronze { get; set; }
    public int Total => Gold + Silver + Bronze;
}
";

    private readonly string example3RazorCode = @"
@using System.Text.Json;
@inject HttpClient HttpClient
@inject NavigationManager NavManager

<style>
    .grid {
        height: 15rem;
        overflow-y: auto;
    }

    .grid table {
        width: 100%;
    }

    .grid tr {
        height: 35px;
    }

    .grid thead {
        top: 0;
        z-index: 1;
        position: sticky;
        background-color: var(--bit-clr-bg-sec);
    }

    .grid tbody td {
        max-width: 0;
        overflow: hidden;
        white-space: nowrap;
        text-overflow: ellipsis;
    }

    .search-panel {
        max-width: 15rem;
        margin-top: 2rem;
    }
</style>

<div class=""grid"">
    <BitDataGrid ItemsProvider=""@foodRecallProvider"" TGridItem=""FoodRecall"" Virtualize=""true"" @ref=""dataGrid"">
        <BitDataGridPropertyColumn Property=""@(c=>c.EventId)"" />
        <BitDataGridPropertyColumn Property=""@(c => c.State)"" />
        <BitDataGridPropertyColumn Property=""@(c => c.City)"" />
        <BitDataGridPropertyColumn Property=""@(c => c.RecallingFirm)"" Title=""Company"" />
        <BitDataGridPropertyColumn Property=""@(c => c.Status)"" />
        <BitDataGridPropertyColumn Sortable=""true"" Property=""@(c => c.ReportDate)"" Title=""Report Date"" />
    </BitDataGrid>
</div>
<div class=""search-panel"">
    <BitSearchBox @bind-Value=""virtualSampleNameFilter"" 
                  Immediate DebounceTime=""300""
                  Placeholder=""Search on Company""/>
</div>";
    private readonly string example3CsharpCode = @"
BitDataGrid<FoodRecall>? dataGrid;
string _virtualSampleNameFilter = string.Empty;
BitDataGridItemsProvider<FoodRecall> foodRecallProvider;

string VirtualSampleNameFilter
{
    get => _virtualSampleNameFilter;
    set
    {
        _virtualSampleNameFilter = value;
        _ = dataGrid.RefreshDataAsync();
    }
}

protected override async Task OnInitializedAsync()
{
    foodRecallProvider = async req =>
    {
        try
        {
            var query = new Dictionary<string, object?>
            {
                { ""search"",$""recalling_firm:\""{_virtualSampleNameFilter}\"" },
                { ""skip"", req.StartIndex },
                { ""limit"", req.Count }
            };

            var sort = req.GetSortByProperties().SingleOrDefault();

            if (sort != default)
            {
                var sortByColumnName = sort.PropertyName switch
                {
                    nameof(FoodRecall.ReportDate) => ""report_date"",
                    _ => throw new InvalidOperationException()
                };

                query.Add(""sort"", $""{sortByColumnName}:{(sort.Direction == BitDataGridSortDirection.Ascending ? ""asc"" : ""desc"")}"");
            }

            var url = NavManager.GetUriWithQueryParameters(""https://api.fda.gov/food/enforcement.json"", query);

            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.FoodRecallQueryResult, req.CancellationToken);

            return BitDataGridItemsProviderResult.From(
                                            items: data!.Results,
                                            totalItemCount: data!.Meta.Results.Total);
        }
        catch
        {
            return BitDataGridItemsProviderResult.From<FoodRecall>(new List<FoodRecall> { }, 0);
        }
    };
}

//https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
[JsonSerializable(typeof(FoodRecallQueryResult))]
[JsonSerializable(typeof(Meta))]
[JsonSerializable(typeof(FoodRecall))]
[JsonSerializable(typeof(Results))]
[JsonSerializable(typeof(Openfda))]
public partial class AppJsonContext : JsonSerializerContext
{
  
}

public class FoodRecallQueryResult
{
    [JsonPropertyName(""meta"")]
    public Meta? Meta { get; set; }

    [JsonPropertyName(""results"")]
    public List<FoodRecall>? Results { get; set; }
}

public class Meta
{
    [JsonPropertyName(""disclaimer"")]
    public string? Disclaimer { get; set; }

    [JsonPropertyName(""terms"")]
    public string? Terms { get; set; }

    [JsonPropertyName(""license"")]
    public string? License { get; set; }

    [JsonPropertyName(""last_updated"")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName(""results"")]
    public Results? Results { get; set; }
}

public class FoodRecall
{
    [JsonPropertyName(""country"")]
    public string? CountryModel { get; set; }

    [JsonPropertyName(""city"")]
    public string? City { get; set; }

    [JsonPropertyName(""address_1"")]
    public string? Address1 { get; set; }

    [JsonPropertyName(""reason_for_recall"")]
    public string? ReasonForRecall { get; set; }

    [JsonPropertyName(""address_2"")]
    public string? Address2 { get; set; }

    [JsonPropertyName(""product_quantity"")]
    public string? ProductQuantity { get; set; }

    [JsonPropertyName(""code_info"")]
    public string? CodeInfo { get; set; }

    [JsonPropertyName(""center_classification_date"")]
    public string? CenterClassificationDate { get; set; }

    [JsonPropertyName(""distribution_pattern"")]
    public string? DistributionPattern { get; set; }

    [JsonPropertyName(""state"")]
    public string? State { get; set; }

    [JsonPropertyName(""product_description"")]
    public string? ProductDescription { get; set; }

    [JsonPropertyName(""report_date"")]
    public string? ReportDate { get; set; }

    [JsonPropertyName(""classification"")]
    public string? Classification { get; set; }

    [JsonPropertyName(""openfda"")]
    public Openfda? Openfda { get; set; }

    [JsonPropertyName(""recalling_firm"")]
    public string? RecallingFirm { get; set; }

    [JsonPropertyName(""recall_number"")]
    public string? RecallNumber { get; set; }

    [JsonPropertyName(""initial_firm_notification"")]
    public string? InitialFirmNotification { get; set; }

    [JsonPropertyName(""product_type"")]
    public string? ProductType { get; set; }

    [JsonPropertyName(""event_id"")]
    public string? EventId { get; set; }

    [JsonPropertyName(""more_code_info"")]
    public string? MoreCodeInfo { get; set; }

    [JsonPropertyName(""recall_initiation_date"")]
    public string? RecallInitiationDate { get; set; }

    [JsonPropertyName(""postal_code"")]
    public string? PostalCode { get; set; }

    [JsonPropertyName(""voluntary_mandated"")]
    public string? VoluntaryMandated { get; set; }

    [JsonPropertyName(""status"")]
    public string? Status { get; set; }
}

public class Results
{
    [JsonPropertyName(""skip"")]
    public int Skip { get; set; }

    [JsonPropertyName(""limit"")]
    public int Limit { get; set; }

    [JsonPropertyName(""total"")]
    public int Total { get; set; }
}

public class Openfda
{
}
";

    private readonly string example4RazorCode = @"
@using System.Text.Json;
@inject HttpClient HttpClient
@inject NavigationManager NavManager

<style>
    .grid {
        height: 15rem;
        overflow-y: auto;
    }

    .grid table {
        width: 100%;
    }

    .grid tr {
        height: 35px;
    }

    .grid thead {
        top: 0;
        z-index: 1;
        position: sticky;
        background-color: var(--bit-clr-bg-sec);
    }

    .grid tbody td {
        max-width: 0;
        overflow: hidden;
        white-space: nowrap;
        text-overflow: ellipsis;
    }

    .search-panel {
        max-width: 15rem;
        margin-top: 2rem;
    }
</style>

<div class=""grid"">
    <BitDataGrid ItemKey=""(p => p.Id)"" ItemsProvider=""@productsItemsProvider"" TGridItem=""ProductDto"" Virtualize=""true"" @ref=""productsDataGrid"">
        <BitDataGridPropertyColumn Property=""@(p => p.Id)"" Sortable=""true"" IsDefaultSort=""BitDataGridSortDirection.Ascending"" />
        <BitDataGridPropertyColumn Property=""@(p => p.Name)"" Sortable=""true"" />
        <BitDataGridPropertyColumn Property=""@(p => p.Price)"" Sortable=""true"" />
    </BitDataGrid>
</div>
<div class=""search-panel"">
    <BitSearchBox @bind-Value=""ODataSampleNameFilter"" 
                  Immediate DebounceTime=""300""
                  Placeholder=""Search on Name"" />
</div>";
    private readonly string example4CsharpCode = @"

// ========== Server code ==========

// To make following aspnetcore controller work, simply change services.AddControllers(); to services.AddControllers().AddOData(options => options.EnableQueryFeatures())
// Note that this need Microsoft.AspNetCore.OData nuget package to be installed

[ApiController]
[Route(""[controller]/[action]"")]
public class ProductsController : ControllerBase
{
    private static readonly Random _random = new Random();

    private static readonly ProductDto[] _products = Enumerable.Range(1, 500_000)
        .Select(i => new ProductDto { Id = i, Name = Guid.NewGuid().ToString(""N""), Price = _random.Next(1, 100) })
        .ToArray();

    [HttpGet]
    public async Task<PagedResult<ProductDto>> GetProducts(ODataQueryOptions<ProductDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = _products.AsQueryable();

        query = (IQueryable<ProductDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = query.Count();

        if (odataQuery.Skip is not null)
            query = query.Skip(odataQuery.Skip.Value);

        query = query.Take(odataQuery.Top?.Value ?? 50);

        return new PagedResult<ProductDto>(query.ToArray(), totalCount);
    }
}


// ========== Shared code ==========


//https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
[JsonSerializable(typeof(PagedResult<ProductDto>))]
public partial class AppJsonContext : JsonSerializerContext
{
  
}

public class ProductDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public decimal Price { get; set; }
}

public class PagedResult<T>
{
    public IList<T>? Items { get; set; }

    public int TotalCount { get; set; }

    public PagedResult(IList<T> items, int totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }

    public PagedResult()
    {

    }
}


// ========== Client code ==========


BitDataGrid<ProductDto>? productsDataGrid;
string _odataSampleNameFilter = string.Empty;
BitDataGridItemsProvider<ProductDto> productsItemsProvider;

string ODataSampleNameFilter
{
    get => _odataSampleNameFilter;
    set
    {
        _odataSampleNameFilter = value;
        _ = productsDataGrid.RefreshDataAsync();
    }
}

protected override async Task OnInitializedAsync()
{
    productsItemsProvider = async req =>
    {
        try
        {
            // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

            var query = new Dictionary<string, object>()
            {
                { ""$top"", req.Count ?? 50 },
                { ""$skip"", req.StartIndex }
            };

            if (string.IsNullOrEmpty(_odataSampleNameFilter) is false)
            {
                query.Add(""$filter"", $""contains(Name,'{_odataSampleNameFilter}')"");
            }

            if (req.GetSortByProperties().Any())
            {
                query.Add(""$orderby"", string.Join("", "", req.GetSortByProperties().Select(p => $""{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? ""asc"" : ""desc"")}"")));
            }

            var url = NavManager.GetUriWithQueryParameters(""Products/GetProducts"", query);

            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

            return BitDataGridItemsProviderResult.From(data!.Items, (int)data!.TotalCount);
        }
        catch
        {
            return BitDataGridItemsProviderResult.From<ProductDto>(new List<ProductDto> { }, 0);
        }
    };
}";

    private readonly string example5RazorCode = @"
@using System.Text.Json;
@inject HttpClient HttpClient
@inject NavigationManager NavManager

<style>
    .grid {
        height: 15rem;
        overflow-y: auto;
    }

    .grid table {
        width: 100%;
    }

    .grid tr {
        height: 35px;
    }

    .grid thead {
        top: 0;
        z-index: 1;
        position: sticky;
        background-color: var(--bit-clr-bg-sec);
    }

    .grid tbody td {
        max-width: 0;
        overflow: hidden;
        white-space: nowrap;
        text-overflow: ellipsis;
    }

    .search-panel {
        max-width: 15rem;
        margin-top: 2rem;
    }
</style>

<div class=""grid"">
    <BitDataGrid ItemKey=""(p => p.Id)"" ItemsProvider=""@productsItemsProvider"" TGridItem=""ProductDto"" Pagination=""pagination"" @ref=""productsDataGrid"">
        <Columns>
            <BitDataGridPropertyColumn Property=""@(p => p.Id)"" Sortable=""true"" IsDefaultSort=""BitDataGridSortDirection.Ascending"" />
            <BitDataGridPropertyColumn Property=""@(p => p.Name)"" Sortable=""true"" />
            <BitDataGridPropertyColumn Property=""@(p => p.Price)"" Sortable=""true"" />
        </Columns>
        <LoadingTemplate>
            <BitStack Alignment=""BitAlignment.Center"" Style=""height:185px"">
                <b>Loading items...</b>
                <BitOrbitingDotsLoading Size=""BitSize.Large"" />
            </BitStack>
        </LoadingTemplate>
    </BitDataGrid>
</div>
<BitDataGridPaginator Value=""@pagination"" SummaryFormat=""@(v => $""Total: {v.TotalItemCount?.ToString(""N0"")}"")"">
    <TextTemplate Context=""state"">@(state.CurrentPageIndex + 1) / @(state.LastPageIndex + 1)</TextTemplate>
</BitDataGridPaginator>";
    private readonly string example5CsharpCode = @"

// ========== Server code ==========

// To make following aspnetcore controller work, simply change services.AddControllers(); to services.AddControllers().AddOData(options => options.EnableQueryFeatures())
// Note that this need Microsoft.AspNetCore.OData nuget package to be installed

[ApiController]
[Route(""[controller]/[action]"")]
public class ProductsController : ControllerBase
{
    private static readonly Random _random = new Random();

    private static readonly ProductDto[] _products = Enumerable.Range(1, 500_000)
        .Select(i => new ProductDto { Id = i, Name = Guid.NewGuid().ToString(""N""), Price = _random.Next(1, 100) })
        .ToArray();

    [HttpGet]
    public async Task<PagedResult<ProductDto>> GetProducts(ODataQueryOptions<ProductDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = _products.AsQueryable();

        query = (IQueryable<ProductDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = query.Count();

        if (odataQuery.Skip is not null)
        {
            query = query.Skip(odataQuery.Skip.Value);
        }

        query = query.Take(odataQuery.Top?.Value ?? 50);

        return new PagedResult<ProductDto>(query.ToArray(), totalCount);
    }
}


// ========== Shared code ==========


//https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
[JsonSerializable(typeof(PagedResult<ProductDto>))]
public partial class AppJsonContext : JsonSerializerContext
{

}

public class ProductDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public decimal Price { get; set; }
}

public class PagedResult<T>
{
    public IList<T>? Items { get; set; }

    public int TotalCount { get; set; }

    public PagedResult(IList<T> items, int totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }

    public PagedResult()
    {

    }
}


// ========== Client code ==========


BitDataGrid<ProductDto>? productsDataGrid;
BitDataGridItemsProvider<ProductDto> productsItemsProvider;
BitDataGridPaginationState pagination = new() { ItemsPerPage = 7 };

protected override async Task OnInitializedAsync()
{
    productsItemsProvider = async req =>
    {
        try
        {
            // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

            var query = new Dictionary<string, object>()
            {
                { ""$top"", req.Count ?? 50 },
                { ""$skip"", req.StartIndex }
            };

            if (req.GetSortByProperties().Any())
            {
                query.Add(""$orderby"", string.Join("", "", req.GetSortByProperties().Select(p => $""{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? ""asc"" : ""desc"")}"")));
            }

            var url = NavManager.GetUriWithQueryParameters(""Products/GetProducts"", query);

            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

            return BitDataGridItemsProviderResult.From(data!.Items, (int)data!.TotalCount);
        }
        catch
        {
            return BitDataGridItemsProviderResult.From<ProductDto>(new List<ProductDto> { }, 0);
        }
    };
}";
}
