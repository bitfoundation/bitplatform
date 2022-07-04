using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.DataGrid;

public partial class BitDataGridDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {

         new ComponentParameter()
        {
            Name = "Items",
            Type = "IQueryable<TGridItem>",
            DefaultValue = "",
            Description = @"A queryable source of data for the grid.
                            This could be in-memory data converted to queryable using the
                            System.Linq.Queryable.AsQueryable(System.Collections.IEnumerable) extension method,
                            or an EntityFramework DataSet or an IQueryable derived from it.
                            You should supply either Items or ItemsProvider, but not both.
                            ",
        },
         new ComponentParameter()
        {
            Name = "ItemsProvider",
            Type = "BitDataGridItemsProvider<TGridItem>",
            DefaultValue = "",
            Description = @"A callback that supplies data for the rid.
                            You should supply either Items or ItemsProvider, but not both.
                           ",
        },

          new ComponentParameter()
        {
            Name = "Class",
            Type = "string",
            DefaultValue = "",
            Description = "An optional CSS class name. If given, this will be included in the class attribute of the rendered table.",
        },

          new ComponentParameter()
        {
            Name = "Theme",
            Type = "IQueryable<TGridItem>",
            DefaultValue = "default",
            Description = @"A theme name, with default value ""default"". This affects which styling rules match the table.",
        },

        new ComponentParameter()
        {
            Name = "ChildContent",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Defines the child components of this instance. For example, you may define columns by adding components derived from the BitDataGridColumnBase<TGridItem>",
        },

         new ComponentParameter()
        {
            Name = "Virtualize",
            Type = "bool",
            DefaultValue = "false",
            Description = @"If true, the grid will be rendered with virtualization. This is normally used in conjunction with
                            scrolling and causes the grid to fetch and render only the data around the current scroll viewport.
                            This can greatly improve the performance when scrolling through large data sets.",
        },

         new ComponentParameter()
        {
            Name = "ItemSize",
            Type = "float",
            DefaultValue = "50",
            Description = @"This is applicable only when using Virtualize. It defines an expected height in pixels for
                            each row, allowing the virtualization mechanism to fetch the correct number of items to match the display
                            size and to ensure accurate scrolling.",
        },

         new ComponentParameter()
        {
            Name = "ResizableColumns",
            Type = "bool",
            DefaultValue = "",
            Description = @"If true, renders draggable handles around the column headers, allowing the user to resize the columns
                            manually. Size changes are not persisted.
                           ",
        },

        new ComponentParameter()
        {
            Name = "ItemKey",
            Type = "Func<TGridItem, object>",
            DefaultValue = "x => x!",
            Description = @"Optionally defines a value for @key on each rendered row. Typically this should be used to specify a
                            unique identifier, such as a primary key value, for each data item.
                            This allows the grid to preserve the association between row elements and data items based on their
                            unique identifiers, even when the TGridItem instances are replaced by new copies (for example, after a new query against the underlying data store).
                            If not set, the @key will be the TGridItem instance itself.",
        },

        new ComponentParameter()
        {
            Name = "Pagination",
            Type = "BitDataGridPaginationState",
            DefaultValue = "",
            Description = @"Optionally links this BitDataGrid<TGridItem> instance with a BitDataGridPaginationState model,
                            causing the grid to fetch and render only the current page of data.
                            This is normally used in conjunction with a Paginator component or some other UI logic
                            that displays and updates the supplied BitDataGridPaginationState instance.",
        },


    };
    private readonly List<ComponentSubParameter> componentSubParameter = new()
    {
         new ComponentSubParameter()
        {
            Id="BitDataGridColumnBase",
            Title = "BitDataGridColumnBase",
            Parameters=new List<ComponentParameter>()
            {
                 new ComponentParameter()
                    {
                        Name = "Title",
                        Type = "string",
                        DefaultValue = "",
                        Description = @"Title text for the column. This is rendered automatically if HeaderTemplate is not used.",
                    },
                 new ComponentParameter()
                    {
                        Name = "Class",
                        Type = "string",
                        DefaultValue = "",
                        Description = @"An optional CSS class name. If specified, this is included in the class attribute of table header and body cells
                                        for this column.
                                        ",
                    },
                 new ComponentParameter()
                    {
                        Name = "Align",
                        Type = "BitDataGridAlign",
                        DefaultValue = "",
                        Description = @"If specified, controls the justification of table header and body cells for this column.",
                    },
                  new ComponentParameter()
                    {
                        Name = "HeaderTemplate",
                        Type = "RenderFragment<BitDataGridColumnBase<TGridItem>>",
                        DefaultValue = "",
                        Description = @"An optional template for this column's header cell. If not specified, the default header template
                                        includes the Title along with any applicable sort indicators and options buttons.
                                        ",
                    },
                   new ComponentParameter()
                    {
                        Name = "ColumnOptions",
                        Type = "RenderFragment<BitDataGridColumnBase<TGridItem>>",
                        DefaultValue = "",
                        Description = @"If specified, indicates that this column has this associated options UI. A button to display this
                                        UI will be included in the header cell by default.
                                        If HeaderTemplate is used, it is left up to that template to render any relevant
                                        ""show options"" UI and invoke the grid's BitDataGrid<TGridItem>.ShowColumnOptions(BitDataGridColumnBase<TGridItem>)).
                                        ",
                    },
                   new ComponentParameter()
                    {
                        Name = "Sortable",
                        Type = "bool",
                        DefaultValue = "",
                        Description = @"Indicates whether the data should be sortable by this column.
                                        The default value may vary according to the column type (for example, a BitDataGridTemplateColumn<TGridItem>
                                        is sortable by default if any BitDataGridTemplateColumn<TGridItem>.SortBy parameter is specified).
                                        ",
                    },
                   new ComponentParameter()
                    {
                        Name = "IsDefaultSort",
                        Type = "BitDataGridSortDirection",
                        DefaultValue = "",
                        Description = @"If specified and not null, indicates that this column represents the initial sort order
                                        for the grid. The supplied value controls the default sort direction.
                                        ",
                    },
                   new ComponentParameter()
                    {
                        Name = "PlaceholderTemplate",
                        Type = "RenderFragment<PlaceholderContext>",
                        DefaultValue = "",
                        Description = @"If specified, virtualized grids will use this template to render cells whose data has not yet been loaded.",
                    },
            },
            Description=@"BitDataGrid has two built-in column types, BitDataGridPropertyColumn and BitDataGridTemplateColumn. You can also create your own column types by subclassing ColumnBase
                          The BitDataGridColumnBase type, which all column must derive from, offers some common parameters",

        },

         new ComponentSubParameter()
        {
            Id="BitDataGridPropertyColumn",
            Title = "BitDataGridPropertyColumn",
            Parameters=new List<ComponentParameter>()
            {
                 new ComponentParameter()
                    {
                        Name = "Property",
                        Type = "Expression<Func<TGridItem, TProp>>",
                        DefaultValue="",
                        Description = @"Defines the value to be displayed in this column's cells.",
                    },
                  new ComponentParameter()
                    {
                        Name = "Format",
                        Type = "string",
                        DefaultValue="",
                        Description = @"Optionally specifies a format string for the value.
                                        Using this requires the TProp type to implement IFormattable.
                                       ",
                    },
            },
            Description=@"It is for displaying a single value specified by the parameter Property. This column infers sorting rules automatically,
                          and uses the property's name as its title if not otherwise set.",

        },

         new ComponentSubParameter()
        {
            Id="BitDataGridTemplateColumn",
            Title = "BitDataGridTemplateColumn",
            Parameters=new List<ComponentParameter>()
            {
                 new ComponentParameter()
                    {
                        Name = "ChildContent",
                        Type = "RenderFragment<TGridItem>",
                        DefaultValue="",
                        Description = @"Specifies the content to be rendered for each row in the table.",
                    },
                  new ComponentParameter()
                    {
                        Name = "SortBy",
                        Type = "BitDataGridSort<TGridItem>",
                        DefaultValue="",
                        Description = @"Optionally specifies sorting rules for this column.",
                    },
            },
            Description=@"It uses arbitrary Razor fragments to supply contents for its cells.
                          It can't infer the column's title or sort order automatically. 
                          also it's possible to add arbitrary Blazor components to your table cells.
                          Remember that rendering many components, or many event handlers, can impact the performance of your grid. One way to mitigate this issue is by paginating or virtualizing your grid",

        },

    };
    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
             Id="BitDataGridAlign",
             Title="BitDataGridAlign Enum",
             Description="Describes alignment for a BitDataGrid<TGridItem> column.",
             EnumList=new List<EnumItem>()
             {
                 new EnumItem()
                 {
                      Name="Left",
                      Value="0",
                      Description="Justifies the content against the start of the container."
                 },
                 new EnumItem()
                 {
                      Name="Center",
                      Value="1",
                      Description="Justifies the content at the center of the container."
                 },
                 new EnumItem()
                 {
                      Name="Right",
                      Value="2",
                      Description="Justifies the content at the end of the container."
                 },

             }
        },
    };
    private readonly string example1HTMLCode = @"
<style scoped>
.grid {
  display: inline-flex;
  flex-direction: column;
  width: 100%;
}

.grid ::deep table {
  min-width: 100%;
}

.grid ::deep tr {
  height: 1.8rem;
}

.grid ::deep th:nth-child(1) {
  width: 15rem;
}

.grid ::deep th:nth-child(1) .col-options-button {
    background-image: none;
    cursor: pointer;

    &:before {
        display: inline-block;
        font-family: 'Fabric MDL2 Assets';
        font-style: normal;
        font-weight: normal;
        content: ""\E721"";
    }
}
</style>

<div class=""grid"">
    <BitDataGrid Items = ""@FilteredItems"" ResizableColumns=""true"" Pagination=""@pagination"">
        <BitDataGridPropertyColumn Property = ""@(c => c.Name)"" Sortable=""true"" Class=""column1"">
            <ColumnOptions>
                <BitSearchBox @bind-Value=""typicalSampleNameFilter""
                                          Placeholder=""Search on Name""
                                          InputHtmlAttributes=""@(new Dictionary<string, object> {{""autofocus"", true}})"" />
            </ColumnOptions>
        </BitDataGridPropertyColumn>
        <BitDataGridPropertyColumn Property=""@(c => c.Medals.Gold)"" Sortable=""true"" Align=""BitDataGridAlign.Right"" />
        <BitDataGridPropertyColumn Property=""@(c => c.Medals.Silver)"" Sortable=""true"" Align=""BitDataGridAlign.Right"" />
        <BitDataGridPropertyColumn Property=""@(c => c.Medals.Bronze)"" Sortable=""true"" Align=""BitDataGridAlign.Right"" />
        <BitDataGridPropertyColumn Property=""@(c => c.Medals.Total)"" Sortable=""true"" Align=""BitDataGridAlign.Right"" />
        </BitDataGrid>
        <BitDataGridPaginator Value=""@pagination"" />
</div>";
    private readonly string example1CSharpCode = @"
BitDataGridPaginationState pagination = new() { ItemsPerPage = 15 };
IQueryable<Country> items;
string typicalSampleNameFilter = string.Empty;
BitDataGridSort<Country> rankSort = BitDataGridSort<Country>.ByDescending(x => x.Medals.Gold).ThenDescending(x => x.Medals.Silver).ThenDescending(x => x.Medals.Bronze);

IQueryable<Country> FilteredItems => items?.Where(x => x.Name.Contains(typicalSampleNameFilter, StringComparison.CurrentCultureIgnoreCase));

protected override async Task OnInitializedAsync()
{
    items = (await GetCountriesAsync(0, null, null, true, CancellationToken.None)).Items.AsQueryable();
}
private async Task<BitDataGridItemsProviderResult<Country>> GetCountriesAsync(int startIndex, int? count, string sortBy, bool sortAscending, CancellationToken cancellationToken)
{
    var ordered = (sortBy, sortAscending) switch
    {
        (nameof(Country.Name), true) => _countries.OrderBy(c => c.Name),
        (nameof(Country.Name), false) => _countries.OrderByDescending(c => c.Name),
        (nameof(Country.Code), true) => _countries.OrderBy(c => c.Code),
        (nameof(Country.Code), false) => _countries.OrderByDescending(c => c.Code),
        (""Medals.Gold"", true) => _countries.OrderBy(c => c.Medals.Gold),
        (""Medals.Gold"", false) => _countries.OrderByDescending(c => c.Medals.Gold),
        _ => _countries.OrderByDescending(c => c.Medals.Gold),
    };

    var result = ordered.Skip(startIndex);

    if (count.HasValue)
    {
       result = result.Take(count.Value);
    }

    return BitDataGridItemsProviderResult.From(result.ToArray(), ordered.Count());
}
private readonly static Country[] _countries = new[]
{
    new Country { Code = ""AR"", Name=""Argentina"", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 2 } },
    new Country { Code = ""AM"", Name=""Armenia"", Medals = new Medals { Gold = 0, Silver = 2, Bronze = 2 } },
    new Country { Code = ""AU"", Name = ""Australia"", Medals = new Medals { Gold = 17, Silver = 7, Bronze = 22 } },
    new Country { Code = ""AT"", Name = ""Austria"", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 5 } },
    new Country { Code = ""AZ"", Name = ""Azerbaijan"", Medals = new Medals { Gold = 0, Silver = 3, Bronze = 4 } },
    new Country { Code = ""BS"", Name = ""Bahamas"", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 0 } },
    new Country { Code = ""BH"", Name = ""Bahrain"", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
    ...
};

public class Country
{
    public string Code { get; set; }
    public string Name { get; set; }
    public Medals Medals { get; set; }
}

public class Medals
{
    public int Gold { get; set; }
    public int Silver { get; set; }
    public int Bronze { get; set; }

    public int Total => Gold + Silver + Bronze;
}
";
    private readonly string example2HTMLCode = @"
<style scoped>
.grid {
  display: inline-flex;
  flex-direction: column;
  width: 100%;
}

.grid ::deep table {
  min-width: 100%;
}

.grid ::deep tr {
  height: 1.8rem;
}

.grid ::deep th:nth-child(1) {
  width: 15rem;
}

.grid ::deep th:nth-child(1) .col-options-button {
    background-image: none;
    cursor: pointer;

    &:before {
        display: inline-block;
        font-family: 'Fabric MDL2 Assets';
        font-style: normal;
        font-weight: normal;
        content: ""\E721"";
    }
}

.grid--fix-height {
  height: 15rem;
  overflow-y: auto;
}

.search-panel {
  margin-top: 0.5rem;
  margin-bottom: 0.5rem;
  border-top: 1px solid black;
  padding-top: 5px;
}

.inline-block {
  display: inline-block;
}
</style>
<div class=""grid grid--fix-height"">
    <BitDataGrid ItemsProvider=""@foodRecallProvider"" TGridItem=""FoodRecall"">
        <BitDataGridPropertyColumn Property = ""@(c=>c.EventId)"" />
        < BitDataGridPropertyColumn Property=""@(c => c.State)"" />
        <BitDataGridPropertyColumn Property = ""@(c => c.City)"" />
        < BitDataGridPropertyColumn Title=""Company"" Property=""@(c => c.RecallingFirm)"" />
        <BitDataGridPropertyColumn Property = ""@(c => c.Status)"" />
    </ BitDataGrid >
</div>
<div class=""search-panel"">
     <div class=""inline-block"">
        <BitSearchBox @bind-Value=""virtualSampleNameFilter"" Width=""250px""
                                          Placeholder=""Search on Name""
                                          InputHtmlAttributes=""@(new Dictionary<string, object> {{""autofocus"", true}})"" />
     </div>
     <div class=""inline-block"">
        Total: <strong>@FilteredItems?.Count()</strong>
    </div>
</div>
";
    private readonly string example2CSharpCode = @"
BitDataGridItemsProvider<FoodRecall> foodRecallProvider;
string virtualSampleNameFilter = string.Empty;

protected override async Task OnInitializedAsync()
{
     foodRecallProvider = async req =>
     {
        var url = NavManager.GetUriWithQueryParameters(""https://api.fda.gov/food/enforcement.json"", new Dictionary<string, object?>
            {
                { ""skip"",req.StartIndex},
                { ""limit"", req.Count },
                { ""search"", virtualSampleNameFilter },
            });

        var response = await Http.GetFromJsonAsync<FoodRecallQueryResult>(url, req.CancellationToken);

        return BitDataGridItemsProviderResult.From(items: response!.Results,totalItemCount: response!.Meta.Results.Total);
     };

    // Display the number of results just for information. This is completely separate from the grid.
    virtualNumResults = (await Http.GetFromJsonAsync<FoodRecallQueryResult>(""https://api.fda.gov/food/enforcement.json""))!.Meta.Results.Total;
}

public class FoodRecallQueryResult
{
    [JsonPropertyName(""meta"")]
    public Meta Meta { get; set; }

    [JsonPropertyName(""results"")]
    public List<FoodRecall> Results { get; set; }
}
public class Meta
{
    [JsonPropertyName(""disclaimer"")]
    public string Disclaimer { get; set; }

    [JsonPropertyName(""terms"")]
    public string Terms { get; set; }

    [JsonPropertyName(""license"")]
    public string License { get; set; }

    [JsonPropertyName(""last_updated"")]
    public string LastUpdated { get; set; }

    [JsonPropertyName(""results"")]
    public Results Results { get; set; }
}
public class FoodRecall
{
    [JsonPropertyName(""country"")]
    public string Country { get; set; }

    [JsonPropertyName(""city"")]
    public string City { get; set; }

    [JsonPropertyName(""address_1"")]
    public string Address1 { get; set; }

    [JsonPropertyName(""reason_for_recall"")]
    public string ReasonForRecall { get; set; }

    [JsonPropertyName(""address_2"")]
    public string Address2 { get; set; }

    [JsonPropertyName(""product_quantity"")]
    public string ProductQuantity { get; set; }

    [JsonPropertyName(""code_info"")]
    public string CodeInfo { get; set; }

    [JsonPropertyName(""center_classification_date"")]
    public string CenterClassificationDate { get; set; }

    [JsonPropertyName(""distribution_pattern"")]
    public string DistributionPattern { get; set; }

    [JsonPropertyName(""state"")]
    public string State { get; set; }

    [JsonPropertyName(""product_description"")]
    public string ProductDescription { get; set; }

    [JsonPropertyName(""report_date"")]
    public string ReportDate { get; set; }

    [JsonPropertyName(""classification"")]
    public string Classification { get; set; }

    [JsonPropertyName(""openfda"")]
    public Openfda Openfda { get; set; }

    [JsonPropertyName(""recalling_firm"")]
    public string RecallingFirm { get; set; }

    [JsonPropertyName(""recall_number"")]
    public string RecallNumber { get; set; }

    [JsonPropertyName(""initial_firm_notification"")]
    public string InitialFirmNotification { get; set; }

    [JsonPropertyName(""product_type"")]
    public string ProductType { get; set; }

    [JsonPropertyName(""event_id"")]
    public string EventId { get; set; }

    [JsonPropertyName(""more_code_info"")]
    public string MoreCodeInfo { get; set; }

    [JsonPropertyName(""recall_initiation_date"")]
    public string RecallInitiationDate { get; set; }

    [JsonPropertyName(""postal_code"")]
    public string PostalCode { get; set; }

    [JsonPropertyName(""voluntary_mandated"")]
    public string VoluntaryMandated { get; set; }

    [JsonPropertyName(""status"")]
    public string Status { get; set; }

    [JsonPropertyName(""skip"")]
    public int Skip { get; set; }

    [JsonPropertyName(""limit"")]
    public int Limit { get; set; }

    [JsonPropertyName(""total"")]
    public int Total { get; set; }
}
public class Results
{
    [JsonPropertyName(""skip"")]
    public int Skip { get; set; }

    [JsonPropertyName(""limit"")]
    public int Limit { get; set; }

    [JsonPropertyName(""Total"")]
    public int Total { get; set; }
}
public class Openfda
{
}

";
    private readonly string example3HTMLCode = @"
<style scoped>
::deep .bitdatagrid[theme=redskin] {
    font-style :italic;
    color: red;
}

::deep .bitdatagrid[theme=redskin] .col-title {
    gap: 0.4rem; /* Separate the sort indicator from title text */
    font-weight: bold;
    text-transform: uppercase;
    background-color:red;
    color:white;
}

::deep .bitdatagrid[theme=redskin] .sort-indicator {
    color: white;
}
</style>

<div class=""grid"">
     <BitDataGrid Items = ""@data""  Theme=""redskin"">
         <BitDataGridPropertyColumn Property = ""@(c => c.Name)"" Sortable=""true"" Class=""column1"">
         </BitDataGridPropertyColumn>
         <BitDataGridPropertyColumn Property = ""@(c => c.Medals.Gold)"" Sortable=""true"" Align=""BitDataGridAlign.Right"" />
         <BitDataGridPropertyColumn Property = ""@(c => c.Medals.Silver)"" Sortable=""true"" Align=""BitDataGridAlign.Right"" />
         <BitDataGridPropertyColumn Property = ""@(c => c.Medals.Bronze)"" Sortable=""true"" Align=""BitDataGridAlign.Right"" />
         <BitDataGridPropertyColumn Property = ""@(c => c.Medals.Total)"" Sortable=""true"" Align=""BitDataGridAlign.Right"" />
    </BitDataGrid>
</div>
";
    private readonly string example3CSharpCode = @"
IQueryable<Country> data;
protected override async Task OnInitializedAsync()
{
    data= (await Get7CountriesAsync()).Items.AsQueryable();
}
private async Task<BitDataGridItemsProviderResult<Country>> Get7CountriesAsync()
{
     var Countries = _countries.Take(7).AsQueryable();
     return BitDataGridItemsProviderResult.From(Countries.ToArray(), Countries.Count());
}
private readonly static Country[] _countries = new[]
{
    new Country { Code = ""AR"", Name=""Argentina"", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 2 } },
    new Country { Code = ""AM"", Name=""Armenia"", Medals = new Medals { Gold = 0, Silver = 2, Bronze = 2 } },
    new Country { Code = ""AU"", Name = ""Australia"", Medals = new Medals { Gold = 17, Silver = 7, Bronze = 22 } },
    new Country { Code = ""AT"", Name = ""Austria"", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 5 } },
    new Country { Code = ""AZ"", Name = ""Azerbaijan"", Medals = new Medals { Gold = 0, Silver = 3, Bronze = 4 } },
    new Country { Code = ""BS"", Name = ""Bahamas"", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 0 } },
    new Country { Code = ""BH"", Name = ""Bahrain"", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
    ...
};
";

    BitDataGridPaginationState pagination = new() { ItemsPerPage = 15 };
    IQueryable<Country> items;
    IQueryable<Country> data;
    BitDataGridItemsProvider<FoodRecall> foodRecallProvider;

    string typicalSampleNameFilter = string.Empty;
    string virtualSampleNameFilter = string.Empty;
    bool virtualSampleHaseError = false;
    int virtualNumResults;

    BitDataGridSort<Country> rankSort = BitDataGridSort<Country>.ByDescending(x => x.Medals.Gold).ThenDescending(x => x.Medals.Silver).ThenDescending(x => x.Medals.Bronze);

    IQueryable<Country> FilteredItems => items?.Where(x => x.Name.Contains(typicalSampleNameFilter, StringComparison.CurrentCultureIgnoreCase));

    List<FoodRecall> foods = new List<FoodRecall>();
    
    protected override async Task OnInitializedAsync()
    {
        items = (await GetCountriesAsync(0, null, null, true, CancellationToken.None)).Items.AsQueryable();
        data = (await Get7CountriesAsync()).Items.AsQueryable();

        try
        {
            foodRecallProvider = async req =>
            {
                var url = NavManager.GetUriWithQueryParameters("https://api.fda.gov/food/enforcement.json", new Dictionary<string, object?>
            {
                { "skip",req.StartIndex },
                { "limit", req.Count },
                { "search", virtualSampleNameFilter },
            });
                var response = await Http.GetFromJsonAsync<FoodRecallQueryResult>(url, req.CancellationToken);
                return BitDataGridItemsProviderResult.From(
                    items: response!.Results,
                    totalItemCount: response!.Meta.Results.Total);
            };
            // Display the number of results just for information. This is completely separate from the grid.
            virtualNumResults = (await Http.GetFromJsonAsync<FoodRecallQueryResult>("https://api.fda.gov/food/enforcement.json"))!.Meta.Results.Total;
        }
        catch 
        {
            //If the ItemsProvider parameter of the DataGrid is assigned a null value, it will cause Blazor to disconnect and cause the application to hang (this is the DataGrid bug).
            foodRecallProvider = async req => { await Task.Delay(1); virtualNumResults = 0; return BitDataGridItemsProviderResult.From(new FoodRecall[] { }, 0); };
            virtualSampleHaseError = true;
        }
    }

    private async Task<BitDataGridItemsProviderResult<Country>> GetCountriesAsync(int startIndex, int? count, string sortBy, bool sortAscending, CancellationToken cancellationToken)
    {
        await Task.Delay(1000);

        var ordered = (sortBy, sortAscending) switch
        {
            (nameof(Country.Name), true) => _countries.OrderBy(c => c.Name),
            (nameof(Country.Name), false) => _countries.OrderByDescending(c => c.Name),
            (nameof(Country.Code), true) => _countries.OrderBy(c => c.Code),
            (nameof(Country.Code), false) => _countries.OrderByDescending(c => c.Code),
            ("Medals.Gold", true) => _countries.OrderBy(c => c.Medals.Gold),
            ("Medals.Gold", false) => _countries.OrderByDescending(c => c.Medals.Gold),
            _ => _countries.OrderByDescending(c => c.Medals.Gold),
        };

        var result = ordered.Skip(startIndex);

        if (count.HasValue)
        {
            result = result.Take(count.Value);
        }

        return BitDataGridItemsProviderResult.From(result.ToArray(), ordered.Count());
    }
    private async Task<BitDataGridItemsProviderResult<Country>> Get7CountriesAsync()
    {
        await Task.Delay(1000);

        var Countries = _countries.Take(7).AsQueryable();
        return BitDataGridItemsProviderResult.From(Countries.ToArray(), Countries.Count());
    }

    private readonly static Country[] _countries = new[]
    {
        new Country { Code = "AR", Name="Argentina", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 2 } },
        new Country { Code = "AM", Name="Armenia", Medals = new Medals { Gold = 0, Silver = 2, Bronze = 2 } },
        new Country { Code = "AU", Name="Australia", Medals = new Medals { Gold = 17, Silver = 7, Bronze = 22 } },
        new Country { Code = "AT", Name="Austria", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 5 } },
        new Country { Code = "AZ", Name="Azerbaijan", Medals = new Medals { Gold = 0, Silver = 3, Bronze = 4 } },
        new Country { Code = "BS", Name="Bahamas", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 0 } },
        new Country { Code = "BH", Name="Bahrain", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "BY", Name="Belarus", Medals = new Medals { Gold = 1, Silver = 3, Bronze = 3 } },
        new Country { Code = "BE", Name="Belgium", Medals = new Medals { Gold = 3, Silver = 1, Bronze = 3 } },
        new Country { Code = "BM", Name="Bermuda", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 0 } },
        new Country { Code = "BW", Name="Botswana", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "BR", Name="Brazil", Medals = new Medals { Gold = 7, Silver = 6, Bronze = 8 } },
        new Country { Code = "BG", Name="Bulgaria", Medals = new Medals { Gold = 3, Silver = 1, Bronze = 2 } },
        new Country { Code = "BF", Name="Burkina Faso", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "CA", Name="Canada", Medals = new Medals { Gold = 7, Silver = 6, Bronze = 11 } },
        new Country { Code = "TW", Name="Chinese Taipei", Medals = new Medals { Gold = 2, Silver = 4, Bronze = 6 } },
        new Country { Code = "CO", Name="Colombia", Medals = new Medals { Gold = 0, Silver = 4, Bronze = 1 } },
        new Country { Code = "CI", Name="Côte d'Ivoire", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "HR", Name="Croatia", Medals = new Medals { Gold = 3, Silver = 3, Bronze = 2 } },
        new Country { Code = "CU", Name="Cuba", Medals = new Medals { Gold = 7, Silver = 3, Bronze = 5 } },
        new Country { Code = "CZ", Name="Czech Republic", Medals = new Medals { Gold = 4, Silver = 4, Bronze = 3 } },
        new Country { Code = "DK", Name="Denmark", Medals = new Medals { Gold = 3, Silver = 4, Bronze = 4 } },
        new Country { Code = "DO", Name="Dominican Republic", Medals = new Medals { Gold = 0, Silver = 3, Bronze = 2 } },
        new Country { Code = "EC", Name="Ecuador", Medals = new Medals { Gold = 2, Silver = 1, Bronze = 0 } },
        new Country { Code = "EG", Name="Egypt", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 4 } },
        new Country { Code = "EE", Name="Estonia", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 1 } },
        new Country { Code = "ET", Name="Ethiopia", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 2 } },
        new Country { Code = "FJ", Name="Fiji", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 1 } },
        new Country { Code = "FI", Name="Finland", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 2 } },
        new Country { Code = "FR", Name="France", Medals = new Medals { Gold = 10, Silver = 12, Bronze = 11 } },
        new Country { Code = "GE", Name="Georgia", Medals = new Medals { Gold = 2, Silver = 5, Bronze = 1 } },
        new Country { Code = "DE", Name="Germany", Medals = new Medals { Gold = 10, Silver = 11, Bronze = 16 } },
        new Country { Code = "GH", Name="Ghana", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "GB", Name="Great Britain", Medals = new Medals { Gold = 22, Silver = 21, Bronze = 22 } },
        new Country { Code = "GR", Name="Greece", Medals = new Medals { Gold = 2, Silver = 1, Bronze = 1 } },
        new Country { Code = "GD", Name="Grenada", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "HK", Name="Hong Kong, China", Medals = new Medals { Gold = 1, Silver = 2, Bronze = 3 } },
        new Country { Code = "HU", Name="Hungary", Medals = new Medals { Gold = 6, Silver = 7, Bronze = 7 } },
        new Country { Code = "IN", Name="India", Medals = new Medals { Gold = 1, Silver = 2, Bronze = 4 } },
        new Country { Code = "ID", Name="Indonesia", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 3 } },
        new Country { Code = "IE", Name="Ireland", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 2 } },
        new Country { Code = "IR", Name="Islamic Republic of Iran", Medals = new Medals { Gold = 3, Silver = 2, Bronze = 2 } },
        new Country { Code = "IL", Name="Israel", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 2 } },
        new Country { Code = "IT", Name="Italy", Medals = new Medals { Gold = 10, Silver = 10, Bronze = 20 } },
        new Country { Code = "JM", Name="Jamaica", Medals = new Medals { Gold = 4, Silver = 1, Bronze = 4 } },
        new Country { Code = "JP", Name="Japan", Medals = new Medals { Gold = 27, Silver = 14, Bronze = 17 } },
        new Country { Code = "JO", Name="Jordan", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 1 } },
        new Country { Code = "KZ", Name="Kazakhstan", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 8 } },
        new Country { Code = "KE", Name="Kenya", Medals = new Medals { Gold = 4, Silver = 4, Bronze = 2 } },
        new Country { Code = "XK", Name="Kosovo", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 0 } },
        new Country { Code = "KW", Name="Kuwait", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "KG", Name="Kyrgyzstan", Medals = new Medals { Gold = 0, Silver = 2, Bronze = 1 } },
        new Country { Code = "LV", Name="Latvia", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 1 } },
        new Country { Code = "LT", Name="Lithuania", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "MY", Name="Malaysia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 1 } },
        new Country { Code = "MX", Name="Mexico", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 4 } },
        new Country { Code = "MN", Name="Mongolia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 3 } },
        new Country { Code = "MA", Name="Morocco", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 0 } },
        new Country { Code = "NA", Name="Namibia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "NL", Name="Netherlands", Medals = new Medals { Gold = 10, Silver = 12, Bronze = 14 } },
        new Country { Code = "NZ", Name="New Zealand", Medals = new Medals { Gold = 7, Silver = 6, Bronze = 7 } },
        new Country { Code = "NG", Name="Nigeria", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 1 } },
        new Country { Code = "MK", Name="North Macedonia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "NO", Name="Norway", Medals = new Medals { Gold = 4, Silver = 2, Bronze = 2 } },
        new Country { Code = "CN", Name="People's Republic of China", Medals = new Medals { Gold = 38, Silver = 32, Bronze = 18 } },
        new Country { Code = "PH", Name="Philippines", Medals = new Medals { Gold = 1, Silver = 2, Bronze = 1 } },
        new Country { Code = "PL", Name="Poland", Medals = new Medals { Gold = 4, Silver = 5, Bronze = 5 } },
        new Country { Code = "PT", Name="Portugal", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 2 } },
        new Country { Code = "PR", Name="Puerto Rico", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 0 } },
        new Country { Code = "QA", Name="Qatar", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 1 } },
        new Country { Code = "KR", Name="Republic of Korea", Medals = new Medals { Gold = 6, Silver = 4, Bronze = 10 } },
        new Country { Code = "MD", Name="Republic of Moldova", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "ROC", Name="ROC", Medals = new Medals { Gold = 20, Silver = 28, Bronze = 23 } },
        new Country { Code = "RO", Name="Romania", Medals = new Medals { Gold = 1, Silver = 3, Bronze = 0 } },
        new Country { Code = "SM", Name="San Marino", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 2 } },
        new Country { Code = "SA", Name="Saudi Arabia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "RS", Name="Serbia", Medals = new Medals { Gold = 3, Silver = 1, Bronze = 5 } },
        new Country { Code = "SK", Name="Slovakia", Medals = new Medals { Gold = 1, Silver = 2, Bronze = 1 } },
        new Country { Code = "SI", Name="Slovenia", Medals = new Medals { Gold = 3, Silver = 1, Bronze = 1 } },
        new Country { Code = "ZA", Name="South Africa", Medals = new Medals { Gold = 1, Silver = 2, Bronze = 0 } },
        new Country { Code = "ES", Name="Spain", Medals = new Medals { Gold = 3, Silver = 8, Bronze = 6 } },
        new Country { Code = "SE", Name="Sweden", Medals = new Medals { Gold = 3, Silver = 6, Bronze = 0 } },
        new Country { Code = "CH", Name="Switzerland", Medals = new Medals { Gold = 3, Silver = 4, Bronze = 6 } },
        new Country { Code = "SY", Name="Syrian Arab Republic", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "TH", Name="Thailand", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 1 } },
        new Country { Code = "TN", Name="Tunisia", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 0 } },
        new Country { Code = "TR", Name="Turkey", Medals = new Medals { Gold = 2, Silver = 2, Bronze = 9 } },
        new Country { Code = "TM", Name="Turkmenistan", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "UG", Name="Uganda", Medals = new Medals { Gold = 2, Silver = 1, Bronze = 1 } },
        new Country { Code = "UA", Name="Ukraine", Medals = new Medals { Gold = 1, Silver = 6, Bronze = 12 } },
        new Country { Code = "US", Name="United States of America", Medals = new Medals { Gold = 39, Silver = 41, Bronze = 33 } },
        new Country { Code = "UZ", Name="Uzbekistan", Medals = new Medals { Gold = 3, Silver = 0, Bronze = 2 } },
        new Country { Code = "VE", Name="Venezuela", Medals = new Medals { Gold = 1, Silver = 3, Bronze = 0 } },
    };
}

public class Country
{
    public string Code { get; set; }
    public string Name { get; set; }
    public Medals Medals { get; set; }
}

public class Medals
{
    public int Gold { get; set; }
    public int Silver { get; set; }
    public int Bronze { get; set; }

    public int Total => Gold + Silver + Bronze;
}


#region OpenFda models
public class FoodRecallQueryResult
{
    [JsonPropertyName("meta")]
    public Meta Meta { get; set; }

    [JsonPropertyName("results")]
    public List<FoodRecall> Results { get; set; }
}
public class Meta
{
    [JsonPropertyName("disclaimer")]
    public string Disclaimer { get; set; }

    [JsonPropertyName("terms")]
    public string Terms { get; set; }

    [JsonPropertyName("license")]
    public string License { get; set; }

    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; set; }

    [JsonPropertyName("results")]
    public Results Results { get; set; }
}
public class FoodRecall
{
    [JsonPropertyName("country")]
    public string Country { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; }

    [JsonPropertyName("address_1")]
    public string Address1 { get; set; }

    [JsonPropertyName("reason_for_recall")]
    public string ReasonForRecall { get; set; }

    [JsonPropertyName("address_2")]
    public string Address2 { get; set; }

    [JsonPropertyName("product_quantity")]
    public string ProductQuantity { get; set; }

    [JsonPropertyName("code_info")]
    public string CodeInfo { get; set; }

    [JsonPropertyName("center_classification_date")]
    public string CenterClassificationDate { get; set; }

    [JsonPropertyName("distribution_pattern")]
    public string DistributionPattern { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("product_description")]
    public string ProductDescription { get; set; }

    [JsonPropertyName("report_date")]
    public string ReportDate { get; set; }

    [JsonPropertyName("classification")]
    public string Classification { get; set; }

    [JsonPropertyName("openfda")]
    public Openfda Openfda { get; set; }

    [JsonPropertyName("recalling_firm")]
    public string RecallingFirm { get; set; }

    [JsonPropertyName("recall_number")]
    public string RecallNumber { get; set; }

    [JsonPropertyName("initial_firm_notification")]
    public string InitialFirmNotification { get; set; }

    [JsonPropertyName("product_type")]
    public string ProductType { get; set; }

    [JsonPropertyName("event_id")]
    public string EventId { get; set; }

    [JsonPropertyName("more_code_info")]
    public string MoreCodeInfo { get; set; }

    [JsonPropertyName("recall_initiation_date")]
    public string RecallInitiationDate { get; set; }

    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; }

    [JsonPropertyName("voluntary_mandated")]
    public string VoluntaryMandated { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("skip")]
    public int Skip { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
public class Results
{
    [JsonPropertyName("skip")]
    public int Skip { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("Total")]
    public int Total { get; set; }
}
public class Openfda
{
}

#endregion

