﻿using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Bit.BlazorUI.Demo.Shared.Dtos.DataGridDemo;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public partial class BitDataGridDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "Items",
            Type = "IQueryable<TGridItem>?",
            DefaultValue = "null",
            Description = @"A queryable source of data for the grid.
                            This could be in-memory data converted to queryable using the
                            System.Linq.Queryable.AsQueryable(System.Collections.IEnumerable) extension method,
                            or an EntityFramework DataSet or an IQueryable derived from it.
                            You should supply either Items or ItemsProvider, but not both.",
         },
         new()
         {
            Name = "ItemsProvider",
            Type = "BitDataGridItemsProvider<TGridItem>?",
            DefaultValue = "null",
            Description = @"A callback that supplies data for the rid.
                            You should supply either Items or ItemsProvider, but not both.",
         },
         new()
         {
            Name = "Class",
            Type = "string?",
            DefaultValue = "null",
            Description = "An optional CSS class name. If given, this will be included in the class attribute of the rendered table.",
         },
         new()
         {
            Name = "Theme",
            Type = "string?",
            DefaultValue = "default",
            Description = @"A theme name, with default value ""default"". This affects which styling rules match the table.",
         },
         new()
         {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Defines the child components of this instance. For example, you may define columns by adding components derived from the BitDataGridColumnBase<TGridItem>",
         },
         new()
         {
            Name = "Virtualize",
            Type = "bool",
            DefaultValue = "false",
            Description = @"If true, the grid will be rendered with virtualization. This is normally used in conjunction with
                            scrolling and causes the grid to fetch and render only the data around the current scroll viewport.
                            This can greatly improve the performance when scrolling through large data sets.",
         },
         new()
         {
            Name = "ItemSize",
            Type = "float",
            DefaultValue = "50",
            Description = @"This is applicable only when using Virtualize. It defines an expected height in pixels for
                            each row, allowing the virtualization mechanism to fetch the correct number of items to match the display
                            size and to ensure accurate scrolling.",
         },
         new()
         {
            Name = "ResizableColumns",
            Type = "bool",
            DefaultValue = "false",
            Description = @"If true, renders draggable handles around the column headers, allowing the user to resize the columns
                            manually. Size changes are not persisted.",
        },
         new()
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
         new()
         {
             Name = "Pagination",
             Type = "BitDataGridPaginationState?",
             DefaultValue = "null",
             Description = @"Optionally links this BitDataGrid<TGridItem> instance with a BitDataGridPaginationState model,
                             causing the grid to fetch and render only the current page of data.
                             This is normally used in conjunction with a Paginator component or some other UI logic
                             that displays and updates the supplied BitDataGridPaginationState instance.",
         }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
         {
            Id = "BitDataGridColumnBase",
            Title = "BitDataGridColumnBase",
            Parameters=
            [
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Title text for the column. This is rendered automatically if HeaderTemplate is not used.",
                },
                new()
                {
                    Name = "Class",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "An optional CSS class name. If specified, this is included in the class attribute of table header and body cells for this column.",
                },
                new()
                {
                    Name = "Align",
                    Type = "BitDataGridAlign?",
                    DefaultValue = "null",
                    Description = "If specified, controls the justification of table header and body cells for this column.",
                },
                new()
                {
                    Name = "HeaderTemplate",
                    Type = "RenderFragment<BitDataGridColumnBase<TGridItem>>?",
                    DefaultValue = "null",
                    Description = @"An optional template for this column's header cell. If not specified, the default header template
                                    includes the Title along with any applicable sort indicators and options buttons.",
                },
                new()
                {
                    Name = "ColumnOptions",
                    Type = "RenderFragment<BitDataGridColumnBase<TGridItem>>?",
                    DefaultValue = "null",
                    Description = @"If specified, indicates that this column has this associated options UI. A button to display this
                                    UI will be included in the header cell by default.
                                    If HeaderTemplate is used, it is left up to that template to render any relevant
                                    ""show options"" UI and invoke the grid's BitDataGrid<TGridItem>.ShowColumnOptions(BitDataGridColumnBase<TGridItem>)).",
                },
                new()
                {
                    Name = "Sortable",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = @"Indicates whether the data should be sortable by this column.
                                    The default value may vary according to the column type (for example, a BitDataGridTemplateColumn<TGridItem>
                                    is sortable by default if any BitDataGridTemplateColumn<TGridItem>.SortBy parameter is specified).",
                },
                new()
                {
                    Name = "IsDefaultSort",
                    Type = "BitDataGridSortDirection?",
                    DefaultValue = "null",
                    Description = "If specified and not null, indicates that this column represents the initial sort order for the grid. The supplied value controls the default sort direction.",
                },
                new()
                {
                    Name = "PlaceholderTemplate",
                    Type = "RenderFragment<PlaceholderContext>?",
                    DefaultValue = "null",
                    Description = "If specified, virtualized grids will use this template to render cells whose data has not yet been loaded.",
                }
            ],
            Description = @"BitDataGrid has two built-in column types, BitDataGridPropertyColumn and BitDataGridTemplateColumn. You can also create your own column types by subclassing ColumnBase
                            The BitDataGridColumnBase type, which all column must derive from, offers some common parameters",

        },
        new()
        {
            Id="BitDataGridPropertyColumn",
            Title = "BitDataGridPropertyColumn",
            Parameters=
            [
                new()
                {
                    Name = "Property",
                    Type = "Expression<Func<TGridItem, TProp>>",
                    Description = "Defines the value to be displayed in this column's cells.",
                },
                new()
                {
                    Name = "Format",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Optionally specifies a format string for the value. Using this requires the TProp type to implement IFormattable.",
                },
            ],
            Description = "It is for displaying a single value specified by the parameter Property. This column infers sorting rules automatically, and uses the property's name as its title if not otherwise set.",
        },
        new()
        {
            Id = "BitDataGridTemplateColumn",
            Title = "BitDataGridTemplateColumn",
            Parameters =
            [
                 new()
                 {
                    Name = "ChildContent",
                    Type = "RenderFragment<TGridItem>",
                    Description = @"Specifies the content to be rendered for each row in the table.",
                 },
                 new()
                 {
                    Name = "SortBy",
                    Type = "BitDataGridSort<TGridItem>?",
                    DefaultValue = "null",
                    Description = "Optionally specifies sorting rules for this column.",
                 },
            ],
            Description = @"It uses arbitrary Razor fragments to supply contents for its cells.
                            It can't infer the column's title or sort order automatically. 
                            also it's possible to add arbitrary Blazor components to your table cells.
                            Remember that rendering many components, or many event handlers, can impact the performance of your grid. One way to mitigate this issue is by paginating or virtualizing your grid",

        },

    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
             Id = "BitDataGridAlign",
             Name = "BitDataGridAlign",
             Description = "Describes alignment for a BitDataGrid<TGridItem> column.",
             Items =
             [
                 new()
                 {
                      Name = "Left",
                      Value = "0",
                      Description = "Justifies the content against the start of the container."
                 },
                 new()
                 {
                      Name = "Center",
                      Value = "1",
                      Description = "Justifies the content at the center of the container."
                 },
                 new()
                 {
                      Name = "Right",
                      Value = "2",
                      Description = "Justifies the content at the end of the container."
                 },

             ]
        },
    ];



    private static readonly CountryModel[] _countries =
    [
        new CountryModel { Code = "AR", Name = "Argentina", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 2 } },
        new CountryModel { Code = "AM", Name = "Armenia", Medals = new MedalsModel { Gold = 0, Silver = 2, Bronze = 2 } },
        new CountryModel { Code = "AU", Name = "Australia", Medals = new MedalsModel { Gold = 17, Silver = 7, Bronze = 22 } },
        new CountryModel { Code = "AT", Name = "Austria", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 5 } },
        new CountryModel { Code = "AZ", Name = "Azerbaijan", Medals = new MedalsModel { Gold = 0, Silver = 3, Bronze = 4 } },
        new CountryModel { Code = "BS", Name = "Bahamas", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 0 } },
        new CountryModel { Code = "BH", Name = "Bahrain", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
        new CountryModel { Code = "BY", Name = "Belarus", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 3 } },
        new CountryModel { Code = "BE", Name = "Belgium", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 3 } },
        new CountryModel { Code = "BM", Name = "Bermuda", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
        new CountryModel { Code = "BW", Name = "Botswana", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "BR", Name = "Brazil", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 8 } },
        new CountryModel { Code = "BF", Name = "Burkina Faso", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "CA", Name = "Canada", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 11 } },
        new CountryModel { Code = "TW", Name = "Chinese Taipei", Medals = new MedalsModel { Gold = 2, Silver = 4, Bronze = 6 } },
        new CountryModel { Code = "CO", Name = "Colombia", Medals = new MedalsModel { Gold = 0, Silver = 4, Bronze = 1 } },
        new CountryModel { Code = "CI", Name = "Côte d'Ivoire", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "HR", Name = "Croatia", Medals = new MedalsModel { Gold = 3, Silver = 3, Bronze = 2 } },
        new CountryModel { Code = "CU", Name = "Cuba", Medals = new MedalsModel { Gold = 7, Silver = 3, Bronze = 5 } },
        new CountryModel { Code = "CZ", Name = "Czech Republic", Medals = new MedalsModel { Gold = 4, Silver = 4, Bronze = 3 } },
        new CountryModel { Code = "DK", Name = "Denmark", Medals = new MedalsModel { Gold = 3, Silver = 4, Bronze = 4 } },
        new CountryModel { Code = "DO", Name = "Dominican Republic", Medals = new MedalsModel { Gold = 0, Silver = 3, Bronze = 2 } },
        new CountryModel { Code = "EC", Name = "Ecuador", Medals = new MedalsModel { Gold = 2, Silver = 1, Bronze = 0 } },
        new CountryModel { Code = "EE", Name = "Estonia", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "ET", Name = "Ethiopia", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 2 } },
        new CountryModel { Code = "FJ", Name = "Fiji", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "FI", Name = "Finland", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 2 } },
        new CountryModel { Code = "FR", Name = "France", Medals = new MedalsModel { Gold = 10, Silver = 12, Bronze = 11 } },
        new CountryModel { Code = "GE", Name = "Georgia", Medals = new MedalsModel { Gold = 2, Silver = 5, Bronze = 1 } },
        new CountryModel { Code = "DE", Name = "Germany", Medals = new MedalsModel { Gold = 10, Silver = 11, Bronze = 16 } },
        new CountryModel { Code = "GH", Name = "Ghana", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "GB", Name = "Great Britain", Medals = new MedalsModel { Gold = 22, Silver = 21, Bronze = 22 } },
        new CountryModel { Code = "GR", Name = "Greece", Medals = new MedalsModel { Gold = 2, Silver = 1, Bronze = 1 } },
        new CountryModel { Code = "GD", Name = "Grenada", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "HK", Name = "Hong Kong, China", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 3 } },
        new CountryModel { Code = "HU", Name = "Hungary", Medals = new MedalsModel { Gold = 6, Silver = 7, Bronze = 7 } },
        new CountryModel { Code = "ID", Name = "Indonesia", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 3 } },
        new CountryModel { Code = "IE", Name = "Ireland", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 2 } },
        new CountryModel { Code = "IR", Name = "Iran", Medals = new MedalsModel { Gold = 3, Silver = 2, Bronze = 2 } },
        new CountryModel { Code = "IL", Name = "Israel", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 2 } },
        new CountryModel { Code = "IT", Name = "Italy", Medals = new MedalsModel { Gold = 10, Silver = 10, Bronze = 20 } },
        new CountryModel { Code = "JM", Name = "Jamaica", Medals = new MedalsModel { Gold = 4, Silver = 1, Bronze = 4 } },
        new CountryModel { Code = "JO", Name = "Jordan", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 1 } },
        new CountryModel { Code = "KZ", Name = "Kazakhstan", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 8 } },
        new CountryModel { Code = "KE", Name = "Kenya", Medals = new MedalsModel { Gold = 4, Silver = 4, Bronze = 2 } },
        new CountryModel { Code = "XK", Name = "Kosovo", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 0 } },
        new CountryModel { Code = "KW", Name = "Kuwait", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "LV", Name = "Latvia", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "LT", Name = "Lithuania", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
        new CountryModel { Code = "MY", Name = "Malaysia", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 1 } },
        new CountryModel { Code = "MX", Name = "Mexico", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 4 } },
        new CountryModel { Code = "MA", Name = "Morocco", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
        new CountryModel { Code = "NA", Name = "Namibia", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
        new CountryModel { Code = "NL", Name = "Netherlands", Medals = new MedalsModel { Gold = 10, Silver = 12, Bronze = 14 } },
        new CountryModel { Code = "NZ", Name = "New Zealand", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 7 } },
        new CountryModel { Code = "MK", Name = "North Macedonia", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
        new CountryModel { Code = "NO", Name = "Norway", Medals = new MedalsModel { Gold = 4, Silver = 2, Bronze = 2 } },
        new CountryModel { Code = "PH", Name = "Philippines", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 1 } },
        new CountryModel { Code = "PL", Name = "Poland", Medals = new MedalsModel { Gold = 4, Silver = 5, Bronze = 5 } },
        new CountryModel { Code = "PT", Name = "Portugal", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 2 } },
        new CountryModel { Code = "PR", Name = "Puerto Rico", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
        new CountryModel { Code = "QA", Name = "Qatar", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "KR", Name = "Republic of Korea", Medals = new MedalsModel { Gold = 6, Silver = 4, Bronze = 10 } },
        new CountryModel { Code = "MD", Name = "Republic of Moldova", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "RO", Name = "Romania", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 0 } },
        new CountryModel { Code = "SM", Name = "San Marino", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 2 } },
        new CountryModel { Code = "SA", Name = "Saudi Arabia", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
        new CountryModel { Code = "RS", Name = "Serbia", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 5 } },
        new CountryModel { Code = "SK", Name = "Slovakia", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 1 } },
        new CountryModel { Code = "SI", Name = "Slovenia", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 1 } },
        new CountryModel { Code = "ZA", Name = "South Africa", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 0 } },
        new CountryModel { Code = "ES", Name = "Spain", Medals = new MedalsModel { Gold = 3, Silver = 8, Bronze = 6 } },
        new CountryModel { Code = "SE", Name = "Sweden", Medals = new MedalsModel { Gold = 3, Silver = 6, Bronze = 0 } },
        new CountryModel { Code = "CH", Name = "Switzerland", Medals = new MedalsModel { Gold = 3, Silver = 4, Bronze = 6 } },
        new CountryModel { Code = "SY", Name = "Syrian Arab Republic", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "TH", Name = "Thailand", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
        new CountryModel { Code = "TR", Name = "Turkey", Medals = new MedalsModel { Gold = 2, Silver = 2, Bronze = 9 } },
        new CountryModel { Code = "TM", Name = "Turkmenistan", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
        new CountryModel { Code = "UA", Name = "Ukraine", Medals = new MedalsModel { Gold = 1, Silver = 6, Bronze = 12 } },
        new CountryModel { Code = "US", Name = "United States of America", Medals = new MedalsModel { Gold = 39, Silver = 41, Bronze = 33 } },
        new CountryModel { Code = "UZ", Name = "Uzbekistan", Medals = new MedalsModel { Gold = 3, Silver = 0, Bronze = 2 } },
        new CountryModel { Code = "VE", Name = "Venezuela", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 0 } },
    ];

    [AutoInject] LazyAssemblyLoader lazyAssemblyLoader = default!;

    private bool isLoadingAssemblies = true;

    protected override async Task OnInitAsync()
    {
        allCountries = _countries.AsQueryable();

        foodRecallProvider = async req =>
        {
            try
            {
                var query = new Dictionary<string, object?>
                    {
                    { "search",$"recalling_firm:\"{_virtualSampleNameFilter}\"" },
                    { "skip", req.StartIndex },
                    { "limit", req.Count }
                    };

                var sort = req.GetSortByProperties().SingleOrDefault();

                if (sort != default)
                {
                    var sortByColumnName = sort.PropertyName switch
                    {
                        nameof(FoodRecall.ReportDate) => "report_date",
                        _ => throw new InvalidOperationException()
                    };

                    query.Add("sort", $"{sortByColumnName}:{(sort.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}");
                }

                var url = NavManager.GetUriWithQueryParameters("https://api.fda.gov/food/enforcement.json", query);

                var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.FoodRecallQueryResult, req.CancellationToken);

                return BitDataGridItemsProviderResult.From(data!.Results!, data!.Meta!.Results!.Total);
            }
            catch
            {
                return BitDataGridItemsProviderResult.From<FoodRecall>([], 0);
            }
        };

        productsItemsProvider = async req =>
        {
            try
            {
                // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

                var query = new Dictionary<string, object?>()
                {
                    { "$top", req.Count ?? 50 },
                    { "$skip", req.StartIndex }
                };

                if (string.IsNullOrEmpty(_odataSampleNameFilter) is false)
                {
                    query.Add("$filter", $"contains(Name,'{_odataSampleNameFilter}')");
                }

                if (req.GetSortByProperties().Any())
                {
                    query.Add("$orderby", string.Join(", ", req.GetSortByProperties().Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}")));
                }

                var url = NavManager.GetUriWithQueryParameters("Products/GetProducts", query);

                var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

                return BitDataGridItemsProviderResult.From(data!.Items!, data!.TotalCount);
            }
            catch
            {
                return BitDataGridItemsProviderResult.From<ProductDto>(new List<ProductDto> { }, 0);
            }
        };

        try
        {
            if (OperatingSystem.IsBrowser())
            {
                await lazyAssemblyLoader.LoadAssembliesAsync(["Newtonsoft.Json.wasm", "System.Private.Xml.wasm", "System.Data.Common.wasm"]);
            }
        }
        finally
        {
            isLoadingAssemblies = false;
        }

        await base.OnInitAsync();
    }



    IQueryable<CountryModel> allCountries = default!;
    BitDataGrid<FoodRecall> dataGrid = default!;
    BitDataGrid<ProductDto> productsDataGrid = default!;
    BitDataGridItemsProvider<FoodRecall> foodRecallProvider = default!;
    BitDataGridItemsProvider<ProductDto> productsItemsProvider = default!;
    BitDataGridPaginationState pagination1 = new() { ItemsPerPage = 7 };
    BitDataGridPaginationState pagination2 = new() { ItemsPerPage = 7 };

    IQueryable<CountryModel>? FilteredItems1 =>
        allCountries?.Where(x => x.Name.Contains(typicalSampleNameFilter1 ?? string.Empty, StringComparison.CurrentCultureIgnoreCase));
    IQueryable<CountryModel>? FilteredItems2 =>
        allCountries?.Where(x => x.Name.Contains(typicalSampleNameFilter2 ?? string.Empty, StringComparison.CurrentCultureIgnoreCase));

    string typicalSampleNameFilter1 = string.Empty;
    string typicalSampleNameFilter2 = string.Empty;

    string _virtualSampleNameFilter = string.Empty;
    string VirtualSampleNameFilter
    {
        get => _virtualSampleNameFilter;
        set
        {
            _virtualSampleNameFilter = value;
            _ = dataGrid.RefreshDataAsync();
        }
    }

    string _odataSampleNameFilter = string.Empty;

    string ODataSampleNameFilter
    {
        get => _odataSampleNameFilter;
        set
        {
            _odataSampleNameFilter = value;
            _ = productsDataGrid.RefreshDataAsync();
        }
    }



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
    <BitDataGridPaginator Value=""@pagination2"" />
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
}

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
";
}
