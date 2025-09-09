using Bit.BlazorUI.Demo.Client.Core.Components;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public partial class BitDataGridDemo : AppComponentBase
{
    private readonly string example1RazorCode = @"
<BitDataGrid Items=""@FilteredItems"" Pagination=""@pagination"" ResizableColumns>
    <BitDataGridPropertyColumn Property=""@(c => c.Name)"" Sortable=""true"" IsDefaultSort=""BitDataGridSortDirection.Ascending"">
        <ColumnOptions>
            <BitSearchBox @bind-Value=""typicalSampleNameFilter""
                          FixedIcon
                          Immediate DebounceTime=""300""
                          Placeholder=""Search...""
                          InputHtmlAttributes=""@(new Dictionary<string, object> {{""autofocus"", true}})"" />
        </ColumnOptions>
    </BitDataGridPropertyColumn>
    <BitDataGridPropertyColumn Property=""@(c => c.Medals.Gold)"" Sortable=""true"" />
    <BitDataGridPropertyColumn Property=""@(c => c.Medals.Silver)"" Sortable=""true"" />
    <BitDataGridPropertyColumn Property=""@(c => c.Medals.Bronze)"" Sortable=""true"" />
    <BitDataGridPropertyColumn Property=""@(c => c.Medals.Total)"" Sortable=""true"" />
</BitDataGrid>
<BitDataGridPaginator Value=""@pagination"" />";
    private readonly string example1CsharpCode = @"
private IQueryable<CountryModel> allCountries;
private string typicalSampleNameFilter = string.Empty;
private BitDataGridPaginationState pagination = new() { ItemsPerPage = 7 };
private IQueryable<CountryModel> FilteredItems => 
    allCountries?.Where(x => x.Name.Contains(typicalSampleNameFilter ?? string.Empty, StringComparison.CurrentCultureIgnoreCase));

protected override async Task OnInitializedAsync()
{
    allCountries = _countries.AsQueryable();
}

private static readonly CountryModel[] _countries =
[
    new CountryModel { Code = ""AR"", Name = ""Argentina"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""AM"", Name = ""Armenia"", Medals = new MedalsModel { Gold = 0, Silver = 2, Bronze = 2 } },
    new CountryModel { Code = ""AU"", Name = ""Australia"", Medals = new MedalsModel { Gold = 17, Silver = 7, Bronze = 22 } },
    new CountryModel { Code = ""AT"", Name = ""Austria"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 5 } },
    new CountryModel { Code = ""AZ"", Name = ""Azerbaijan"", Medals = new MedalsModel { Gold = 0, Silver = 3, Bronze = 4 } },
    new CountryModel { Code = ""BS"", Name = ""Bahamas"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""BH"", Name = ""Bahrain"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""BY"", Name = ""Belarus"", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 3 } },
    new CountryModel { Code = ""BE"", Name = ""Belgium"", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 3 } },
    new CountryModel { Code = ""BM"", Name = ""Bermuda"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""BW"", Name = ""Botswana"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""BR"", Name = ""Brazil"", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 8 } },
    new CountryModel { Code = ""BF"", Name = ""Burkina Faso"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""CA"", Name = ""Canada"", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 11 } },
    new CountryModel { Code = ""TW"", Name = ""Chinese Taipei"", Medals = new MedalsModel { Gold = 2, Silver = 4, Bronze = 6 } },
    new CountryModel { Code = ""CO"", Name = ""Colombia"", Medals = new MedalsModel { Gold = 0, Silver = 4, Bronze = 1 } },
    new CountryModel { Code = ""CI"", Name = ""Côte d'Ivoire"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""HR"", Name = ""Croatia"", Medals = new MedalsModel { Gold = 3, Silver = 3, Bronze = 2 } },
    new CountryModel { Code = ""CU"", Name = ""Cuba"", Medals = new MedalsModel { Gold = 7, Silver = 3, Bronze = 5 } },
    new CountryModel { Code = ""CZ"", Name = ""Czech Republic"", Medals = new MedalsModel { Gold = 4, Silver = 4, Bronze = 3 } },
    new CountryModel { Code = ""DK"", Name = ""Denmark"", Medals = new MedalsModel { Gold = 3, Silver = 4, Bronze = 4 } },
    new CountryModel { Code = ""DO"", Name = ""Dominican Republic"", Medals = new MedalsModel { Gold = 0, Silver = 3, Bronze = 2 } },
    new CountryModel { Code = ""EC"", Name = ""Ecuador"", Medals = new MedalsModel { Gold = 2, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""EE"", Name = ""Estonia"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""ET"", Name = ""Ethiopia"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""FJ"", Name = ""Fiji"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""FI"", Name = ""Finland"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""FR"", Name = ""France"", Medals = new MedalsModel { Gold = 10, Silver = 12, Bronze = 11 } },
    new CountryModel { Code = ""GE"", Name = ""Georgia"", Medals = new MedalsModel { Gold = 2, Silver = 5, Bronze = 1 } },
    new CountryModel { Code = ""DE"", Name = ""Germany"", Medals = new MedalsModel { Gold = 10, Silver = 11, Bronze = 16 } },
    new CountryModel { Code = ""GH"", Name = ""Ghana"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""GB"", Name = ""Great Britain"", Medals = new MedalsModel { Gold = 22, Silver = 21, Bronze = 22 } },
    new CountryModel { Code = ""GR"", Name = ""Greece"", Medals = new MedalsModel { Gold = 2, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""GD"", Name = ""Grenada"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""HK"", Name = ""Hong Kong, China"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 3 } },
    new CountryModel { Code = ""HU"", Name = ""Hungary"", Medals = new MedalsModel { Gold = 6, Silver = 7, Bronze = 7 } },
    new CountryModel { Code = ""ID"", Name = ""Indonesia"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 3 } },
    new CountryModel { Code = ""IE"", Name = ""Ireland"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""IR"", Name = ""Iran"", Medals = new MedalsModel { Gold = 3, Silver = 2, Bronze = 2 } },
    new CountryModel { Code = ""IL"", Name = ""Israel"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""IT"", Name = ""Italy"", Medals = new MedalsModel { Gold = 10, Silver = 10, Bronze = 20 } },
    new CountryModel { Code = ""JM"", Name = ""Jamaica"", Medals = new MedalsModel { Gold = 4, Silver = 1, Bronze = 4 } },
    new CountryModel { Code = ""JO"", Name = ""Jordan"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""KZ"", Name = ""Kazakhstan"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 8 } },
    new CountryModel { Code = ""KE"", Name = ""Kenya"", Medals = new MedalsModel { Gold = 4, Silver = 4, Bronze = 2 } },
    new CountryModel { Code = ""XK"", Name = ""Kosovo"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""KW"", Name = ""Kuwait"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""LV"", Name = ""Latvia"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""LT"", Name = ""Lithuania"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""MY"", Name = ""Malaysia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""MX"", Name = ""Mexico"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 4 } },
    new CountryModel { Code = ""MA"", Name = ""Morocco"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""NA"", Name = ""Namibia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""NL"", Name = ""Netherlands"", Medals = new MedalsModel { Gold = 10, Silver = 12, Bronze = 14 } },
    new CountryModel { Code = ""NZ"", Name = ""New Zealand"", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 7 } },
    new CountryModel { Code = ""MK"", Name = ""North Macedonia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""NO"", Name = ""Norway"", Medals = new MedalsModel { Gold = 4, Silver = 2, Bronze = 2 } },
    new CountryModel { Code = ""PH"", Name = ""Philippines"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 1 } },
    new CountryModel { Code = ""PL"", Name = ""Poland"", Medals = new MedalsModel { Gold = 4, Silver = 5, Bronze = 5 } },
    new CountryModel { Code = ""PT"", Name = ""Portugal"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""PR"", Name = ""Puerto Rico"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""QA"", Name = ""Qatar"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""KR"", Name = ""Republic of Korea"", Medals = new MedalsModel { Gold = 6, Silver = 4, Bronze = 10 } },
    new CountryModel { Code = ""MD"", Name = ""Republic of Moldova"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""RO"", Name = ""Romania"", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 0 } },
    new CountryModel { Code = ""SM"", Name = ""San Marino"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""SA"", Name = ""Saudi Arabia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""RS"", Name = ""Serbia"", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 5 } },
    new CountryModel { Code = ""SK"", Name = ""Slovakia"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 1 } },
    new CountryModel { Code = ""SI"", Name = ""Slovenia"", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""ZA"", Name = ""South Africa"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 0 } },
    new CountryModel { Code = ""ES"", Name = ""Spain"", Medals = new MedalsModel { Gold = 3, Silver = 8, Bronze = 6 } },
    new CountryModel { Code = ""SE"", Name = ""Sweden"", Medals = new MedalsModel { Gold = 3, Silver = 6, Bronze = 0 } },
    new CountryModel { Code = ""CH"", Name = ""Switzerland"", Medals = new MedalsModel { Gold = 3, Silver = 4, Bronze = 6 } },
    new CountryModel { Code = ""SY"", Name = ""Syrian Arab Republic"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""TH"", Name = ""Thailand"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""TR"", Name = ""Turkey"", Medals = new MedalsModel { Gold = 2, Silver = 2, Bronze = 9 } },
    new CountryModel { Code = ""TM"", Name = ""Turkmenistan"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""UA"", Name = ""Ukraine"", Medals = new MedalsModel { Gold = 1, Silver = 6, Bronze = 12 } },
    new CountryModel { Code = ""US"", Name = ""United States of America"", Medals = new MedalsModel { Gold = 39, Silver = 41, Bronze = 33 } },
    new CountryModel { Code = ""UZ"", Name = ""Uzbekistan"", Medals = new MedalsModel { Gold = 3, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""VE"", Name = ""Venezuela"", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 0 } },
];

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
    .custom-grid {
        border: 1px solid;
    }

    .custom-grid .container {
        overflow: auto;
    }

    .custom-grid .flag {
        vertical-align: middle;
    }

    .custom-grid table {
        border-spacing: 0;
    }

    .custom-grid tr {
        height: 42px;
    }

    .custom-grid tr:nth-child(even) {
        background: var(--bit-clr-bg-sec);
    }

    .custom-grid tr:nth-child(odd) {
        background: var(--bit-clr-bg-pri);
    }

    .custom-grid th {
        border-bottom: 1px solid;
        background-color: var(--bit-clr-bg-sec);
        border-bottom-color: var(--bit-clr-brd-sec);
    }

    
    .custom-grid th:not(:last-child) {
        border-right: 1px solid;
        border-right-color: var(--bit-clr-brd-sec);
    }

    .custom-grid td {
        overflow: hidden;
        white-space: nowrap;
        padding: 0.25rem 0.5rem;
        text-overflow: ellipsis;
        border-bottom: 1px solid var(--bit-clr-brd-sec);
    }

    .custom-grid .bit-dtg-drg::after {
        border-left: unset;
    }

    .custom-grid .wide {
        width: 220px;
    }
</style>

<div class=""custom-grid"">
    <div class=""container"">
        <BitDataGrid Items=""@FilteredItems"" Pagination=""@pagination"" ResizableColumns>
            <BitDataGridPropertyColumn Property=""@(c => c.Name)"" IsDefaultSort=""BitDataGridSortDirection.Ascending"" Sortable=""true"" Class=""wide"">
                <ColumnOptions>
                    <BitSearchBox @bind-Value=""typicalSampleNameFilter""
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
    <BitDataGridPaginator Value=""@pagination"" SummaryFormat=""@(v => $""Total: {v.TotalItemCount}"")"">
        <TextTemplate Context=""state"">@(state.CurrentPageIndex + 1) / @(state.LastPageIndex + 1)</TextTemplate>
    </BitDataGridPaginator>
</div>";
    private readonly string example2CsharpCode = @"
private IQueryable<CountryModel> allCountries;
private string typicalSampleNameFilter = string.Empty;
private BitDataGridPaginationState pagination = new() { ItemsPerPage = 7 };
private IQueryable<CountryModel> FilteredItems 
    => allCountries?.Where(x => x.Name.Contains(typicalSampleNameFilter ?? string.Empty, StringComparison.CurrentCultureIgnoreCase));

protected override async Task OnInitializedAsync()
{
    allCountries = _countries.AsQueryable();
}

private static readonly CountryModel[] _countries =
[
    new CountryModel { Code = ""AR"", Name = ""Argentina"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""AM"", Name = ""Armenia"", Medals = new MedalsModel { Gold = 0, Silver = 2, Bronze = 2 } },
    new CountryModel { Code = ""AU"", Name = ""Australia"", Medals = new MedalsModel { Gold = 17, Silver = 7, Bronze = 22 } },
    new CountryModel { Code = ""AT"", Name = ""Austria"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 5 } },
    new CountryModel { Code = ""AZ"", Name = ""Azerbaijan"", Medals = new MedalsModel { Gold = 0, Silver = 3, Bronze = 4 } },
    new CountryModel { Code = ""BS"", Name = ""Bahamas"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""BH"", Name = ""Bahrain"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""BY"", Name = ""Belarus"", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 3 } },
    new CountryModel { Code = ""BE"", Name = ""Belgium"", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 3 } },
    new CountryModel { Code = ""BM"", Name = ""Bermuda"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""BW"", Name = ""Botswana"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""BR"", Name = ""Brazil"", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 8 } },
    new CountryModel { Code = ""BF"", Name = ""Burkina Faso"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""CA"", Name = ""Canada"", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 11 } },
    new CountryModel { Code = ""TW"", Name = ""Chinese Taipei"", Medals = new MedalsModel { Gold = 2, Silver = 4, Bronze = 6 } },
    new CountryModel { Code = ""CO"", Name = ""Colombia"", Medals = new MedalsModel { Gold = 0, Silver = 4, Bronze = 1 } },
    new CountryModel { Code = ""CI"", Name = ""Côte d'Ivoire"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""HR"", Name = ""Croatia"", Medals = new MedalsModel { Gold = 3, Silver = 3, Bronze = 2 } },
    new CountryModel { Code = ""CU"", Name = ""Cuba"", Medals = new MedalsModel { Gold = 7, Silver = 3, Bronze = 5 } },
    new CountryModel { Code = ""CZ"", Name = ""Czech Republic"", Medals = new MedalsModel { Gold = 4, Silver = 4, Bronze = 3 } },
    new CountryModel { Code = ""DK"", Name = ""Denmark"", Medals = new MedalsModel { Gold = 3, Silver = 4, Bronze = 4 } },
    new CountryModel { Code = ""DO"", Name = ""Dominican Republic"", Medals = new MedalsModel { Gold = 0, Silver = 3, Bronze = 2 } },
    new CountryModel { Code = ""EC"", Name = ""Ecuador"", Medals = new MedalsModel { Gold = 2, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""EE"", Name = ""Estonia"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""ET"", Name = ""Ethiopia"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""FJ"", Name = ""Fiji"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""FI"", Name = ""Finland"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""FR"", Name = ""France"", Medals = new MedalsModel { Gold = 10, Silver = 12, Bronze = 11 } },
    new CountryModel { Code = ""GE"", Name = ""Georgia"", Medals = new MedalsModel { Gold = 2, Silver = 5, Bronze = 1 } },
    new CountryModel { Code = ""DE"", Name = ""Germany"", Medals = new MedalsModel { Gold = 10, Silver = 11, Bronze = 16 } },
    new CountryModel { Code = ""GH"", Name = ""Ghana"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""GB"", Name = ""Great Britain"", Medals = new MedalsModel { Gold = 22, Silver = 21, Bronze = 22 } },
    new CountryModel { Code = ""GR"", Name = ""Greece"", Medals = new MedalsModel { Gold = 2, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""GD"", Name = ""Grenada"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""HK"", Name = ""Hong Kong, China"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 3 } },
    new CountryModel { Code = ""HU"", Name = ""Hungary"", Medals = new MedalsModel { Gold = 6, Silver = 7, Bronze = 7 } },
    new CountryModel { Code = ""ID"", Name = ""Indonesia"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 3 } },
    new CountryModel { Code = ""IE"", Name = ""Ireland"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""IR"", Name = ""Iran"", Medals = new MedalsModel { Gold = 3, Silver = 2, Bronze = 2 } },
    new CountryModel { Code = ""IL"", Name = ""Israel"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""IT"", Name = ""Italy"", Medals = new MedalsModel { Gold = 10, Silver = 10, Bronze = 20 } },
    new CountryModel { Code = ""JM"", Name = ""Jamaica"", Medals = new MedalsModel { Gold = 4, Silver = 1, Bronze = 4 } },
    new CountryModel { Code = ""JO"", Name = ""Jordan"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""KZ"", Name = ""Kazakhstan"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 8 } },
    new CountryModel { Code = ""KE"", Name = ""Kenya"", Medals = new MedalsModel { Gold = 4, Silver = 4, Bronze = 2 } },
    new CountryModel { Code = ""XK"", Name = ""Kosovo"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""KW"", Name = ""Kuwait"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""LV"", Name = ""Latvia"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""LT"", Name = ""Lithuania"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""MY"", Name = ""Malaysia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""MX"", Name = ""Mexico"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 4 } },
    new CountryModel { Code = ""MA"", Name = ""Morocco"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""NA"", Name = ""Namibia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""NL"", Name = ""Netherlands"", Medals = new MedalsModel { Gold = 10, Silver = 12, Bronze = 14 } },
    new CountryModel { Code = ""NZ"", Name = ""New Zealand"", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 7 } },
    new CountryModel { Code = ""MK"", Name = ""North Macedonia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""NO"", Name = ""Norway"", Medals = new MedalsModel { Gold = 4, Silver = 2, Bronze = 2 } },
    new CountryModel { Code = ""PH"", Name = ""Philippines"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 1 } },
    new CountryModel { Code = ""PL"", Name = ""Poland"", Medals = new MedalsModel { Gold = 4, Silver = 5, Bronze = 5 } },
    new CountryModel { Code = ""PT"", Name = ""Portugal"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""PR"", Name = ""Puerto Rico"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""QA"", Name = ""Qatar"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""KR"", Name = ""Republic of Korea"", Medals = new MedalsModel { Gold = 6, Silver = 4, Bronze = 10 } },
    new CountryModel { Code = ""MD"", Name = ""Republic of Moldova"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""RO"", Name = ""Romania"", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 0 } },
    new CountryModel { Code = ""SM"", Name = ""San Marino"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""SA"", Name = ""Saudi Arabia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""RS"", Name = ""Serbia"", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 5 } },
    new CountryModel { Code = ""SK"", Name = ""Slovakia"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 1 } },
    new CountryModel { Code = ""SI"", Name = ""Slovenia"", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""ZA"", Name = ""South Africa"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 0 } },
    new CountryModel { Code = ""ES"", Name = ""Spain"", Medals = new MedalsModel { Gold = 3, Silver = 8, Bronze = 6 } },
    new CountryModel { Code = ""SE"", Name = ""Sweden"", Medals = new MedalsModel { Gold = 3, Silver = 6, Bronze = 0 } },
    new CountryModel { Code = ""CH"", Name = ""Switzerland"", Medals = new MedalsModel { Gold = 3, Silver = 4, Bronze = 6 } },
    new CountryModel { Code = ""SY"", Name = ""Syrian Arab Republic"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""TH"", Name = ""Thailand"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""TR"", Name = ""Turkey"", Medals = new MedalsModel { Gold = 2, Silver = 2, Bronze = 9 } },
    new CountryModel { Code = ""TM"", Name = ""Turkmenistan"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""UA"", Name = ""Ukraine"", Medals = new MedalsModel { Gold = 1, Silver = 6, Bronze = 12 } },
    new CountryModel { Code = ""US"", Name = ""United States of America"", Medals = new MedalsModel { Gold = 39, Silver = 41, Bronze = 33 } },
    new CountryModel { Code = ""UZ"", Name = ""Uzbekistan"", Medals = new MedalsModel { Gold = 3, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""VE"", Name = ""Venezuela"", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 0 } },
];

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

    .grid thead {
        top: 0;
        z-index: 1;
        position: sticky;
        background-color: var(--bit-clr-bg-sec);
    }

    .grid tr {
        height: 2rem;
    }

    .grid td {
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
    <BitDataGrid @ref=""dataGrid"" ItemsProvider=""@foodRecallProvider"" TGridItem=""FoodRecall"" Virtualize>
        <BitDataGridPropertyColumn Property=""@(c=>c.EventId)"" />
        <BitDataGridPropertyColumn Property=""@(c => c.State)"" />
        <BitDataGridPropertyColumn Property=""@(c => c.City)"" />
        <BitDataGridPropertyColumn Property=""@(c => c.RecallingFirm)"" Title=""Company"" />
        <BitDataGridPropertyColumn Property=""@(c => c.Status)"" />
        <BitDataGridPropertyColumn Property=""@(c => c.ReportDate)"" Title=""Report Date"" Sortable=""true"" />
    </BitDataGrid>
</div>
<div class=""search-panel"">
    <BitSearchBox @bind-Value=""virtualSampleNameFilter"" 
                  Immediate DebounceTime=""300""
                  Placeholder=""Search...""/>
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

    .grid thead {
        top: 0;
        z-index: 1;
        position: sticky;
        background-color: var(--bit-clr-bg-sec);
    }

    .grid tr {
        height: 2rem;
    }

    .grid td {
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
    <BitDataGrid @ref=""productsDataGrid"" ItemsProvider=""@productsItemsProvider"" ItemKey=""(p => p.Id)"" TGridItem=""ProductDto"" Virtualize>
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

            var url = NavManager.GetUriWithQueryParameters(""api/Products/GetProducts"", query);

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

    .grid thead {
        top: 0;
        z-index: 1;
        position: sticky;
        background-color: var(--bit-clr-bg-sec);
    }

    .grid tr {
        height: 2rem;
    }

    .grid td {
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
    <BitDataGrid @ref=""productsDataGrid"" ItemsProvider=""@productsItemsProvider"" ItemKey=""(p => p.Id)"" TGridItem=""ProductDto"" Pagination=""pagination"">
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

            var url = NavManager.GetUriWithQueryParameters(""api/Products/GetProducts"", query);

            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

            return BitDataGridItemsProviderResult.From(data!.Items, (int)data!.TotalCount);
        }
        catch
        {
            return BitDataGridItemsProviderResult.From<ProductDto>(new List<ProductDto> { }, 0);
        }
    };
}";

    private readonly string example6RazorCode = @"
<style>
    .responsive-grid table {
        border-collapse: collapse;
    }

    .responsive-grid tr {
        padding: 1rem;
        margin-bottom: 1rem;
        border: 1px solid var(--bit-clr-brd-sec);
    }

    @media (max-width: 600px) {
        .responsive-grid table {
            width: 100%;
        }

        .responsive-grid table, 
        .responsive-grid thead, 
        .responsive-grid tbody, 
        .responsive-grid th, 
        .responsive-grid td, 
        .responsive-grid tr, 
        .responsive-grid td {
            display: block;
        }

        .responsive-grid thead tr {
            display: none;
        }

        .responsive-grid td::before {
            font-weight: bold;
            content: attr(data-title) "" : "";
        }
    }
</style>
<div class=""responsive-grid"">
    <BitDataGrid Items=""@allCountries"" Pagination=""@pagination6"">
        <BitDataGridPropertyColumn Property=""@(c => c.Name)"" />
        <BitDataGridPropertyColumn Property=""@(c => c.Medals.Gold)"" />
        <BitDataGridPropertyColumn Property=""@(c => c.Medals.Silver)"" />
        <BitDataGridPropertyColumn Property=""@(c => c.Medals.Bronze)"" />
        <BitDataGridPropertyColumn Property=""@(c => c.Medals.Total)"" />
    </BitDataGrid>
    <BitDataGridPaginator Value=""@pagination6"" />
</div>";
    private readonly string example6CsharpCode = @"
private IQueryable<CountryModel> allCountries;
private string typicalSampleNameFilter = string.Empty;
private BitDataGridPaginationState pagination = new() { ItemsPerPage = 7 };
private IQueryable<CountryModel> FilteredItems => 
    allCountries?.Where(x => x.Name.Contains(typicalSampleNameFilter ?? string.Empty, StringComparison.CurrentCultureIgnoreCase));

protected override async Task OnInitializedAsync()
{
    allCountries = _countries.AsQueryable();
}

private static readonly CountryModel[] _countries =
[
    new CountryModel { Code = ""AR"", Name = ""Argentina"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""AM"", Name = ""Armenia"", Medals = new MedalsModel { Gold = 0, Silver = 2, Bronze = 2 } },
    new CountryModel { Code = ""AU"", Name = ""Australia"", Medals = new MedalsModel { Gold = 17, Silver = 7, Bronze = 22 } },
    new CountryModel { Code = ""AT"", Name = ""Austria"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 5 } },
    new CountryModel { Code = ""AZ"", Name = ""Azerbaijan"", Medals = new MedalsModel { Gold = 0, Silver = 3, Bronze = 4 } },
    new CountryModel { Code = ""BS"", Name = ""Bahamas"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""BH"", Name = ""Bahrain"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""BY"", Name = ""Belarus"", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 3 } },
    new CountryModel { Code = ""BE"", Name = ""Belgium"", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 3 } },
    new CountryModel { Code = ""BM"", Name = ""Bermuda"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""BW"", Name = ""Botswana"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""BR"", Name = ""Brazil"", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 8 } },
    new CountryModel { Code = ""BF"", Name = ""Burkina Faso"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""CA"", Name = ""Canada"", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 11 } },
    new CountryModel { Code = ""TW"", Name = ""Chinese Taipei"", Medals = new MedalsModel { Gold = 2, Silver = 4, Bronze = 6 } },
    new CountryModel { Code = ""CO"", Name = ""Colombia"", Medals = new MedalsModel { Gold = 0, Silver = 4, Bronze = 1 } },
    new CountryModel { Code = ""CI"", Name = ""Côte d'Ivoire"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""HR"", Name = ""Croatia"", Medals = new MedalsModel { Gold = 3, Silver = 3, Bronze = 2 } },
    new CountryModel { Code = ""CU"", Name = ""Cuba"", Medals = new MedalsModel { Gold = 7, Silver = 3, Bronze = 5 } },
    new CountryModel { Code = ""CZ"", Name = ""Czech Republic"", Medals = new MedalsModel { Gold = 4, Silver = 4, Bronze = 3 } },
    new CountryModel { Code = ""DK"", Name = ""Denmark"", Medals = new MedalsModel { Gold = 3, Silver = 4, Bronze = 4 } },
    new CountryModel { Code = ""DO"", Name = ""Dominican Republic"", Medals = new MedalsModel { Gold = 0, Silver = 3, Bronze = 2 } },
    new CountryModel { Code = ""EC"", Name = ""Ecuador"", Medals = new MedalsModel { Gold = 2, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""EE"", Name = ""Estonia"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""ET"", Name = ""Ethiopia"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""FJ"", Name = ""Fiji"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""FI"", Name = ""Finland"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""FR"", Name = ""France"", Medals = new MedalsModel { Gold = 10, Silver = 12, Bronze = 11 } },
    new CountryModel { Code = ""GE"", Name = ""Georgia"", Medals = new MedalsModel { Gold = 2, Silver = 5, Bronze = 1 } },
    new CountryModel { Code = ""DE"", Name = ""Germany"", Medals = new MedalsModel { Gold = 10, Silver = 11, Bronze = 16 } },
    new CountryModel { Code = ""GH"", Name = ""Ghana"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""GB"", Name = ""Great Britain"", Medals = new MedalsModel { Gold = 22, Silver = 21, Bronze = 22 } },
    new CountryModel { Code = ""GR"", Name = ""Greece"", Medals = new MedalsModel { Gold = 2, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""GD"", Name = ""Grenada"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""HK"", Name = ""Hong Kong, China"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 3 } },
    new CountryModel { Code = ""HU"", Name = ""Hungary"", Medals = new MedalsModel { Gold = 6, Silver = 7, Bronze = 7 } },
    new CountryModel { Code = ""ID"", Name = ""Indonesia"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 3 } },
    new CountryModel { Code = ""IE"", Name = ""Ireland"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""IR"", Name = ""Iran"", Medals = new MedalsModel { Gold = 3, Silver = 2, Bronze = 2 } },
    new CountryModel { Code = ""IL"", Name = ""Israel"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""IT"", Name = ""Italy"", Medals = new MedalsModel { Gold = 10, Silver = 10, Bronze = 20 } },
    new CountryModel { Code = ""JM"", Name = ""Jamaica"", Medals = new MedalsModel { Gold = 4, Silver = 1, Bronze = 4 } },
    new CountryModel { Code = ""JO"", Name = ""Jordan"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""KZ"", Name = ""Kazakhstan"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 8 } },
    new CountryModel { Code = ""KE"", Name = ""Kenya"", Medals = new MedalsModel { Gold = 4, Silver = 4, Bronze = 2 } },
    new CountryModel { Code = ""XK"", Name = ""Kosovo"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""KW"", Name = ""Kuwait"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""LV"", Name = ""Latvia"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""LT"", Name = ""Lithuania"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""MY"", Name = ""Malaysia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""MX"", Name = ""Mexico"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 4 } },
    new CountryModel { Code = ""MA"", Name = ""Morocco"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""NA"", Name = ""Namibia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""NL"", Name = ""Netherlands"", Medals = new MedalsModel { Gold = 10, Silver = 12, Bronze = 14 } },
    new CountryModel { Code = ""NZ"", Name = ""New Zealand"", Medals = new MedalsModel { Gold = 7, Silver = 6, Bronze = 7 } },
    new CountryModel { Code = ""MK"", Name = ""North Macedonia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""NO"", Name = ""Norway"", Medals = new MedalsModel { Gold = 4, Silver = 2, Bronze = 2 } },
    new CountryModel { Code = ""PH"", Name = ""Philippines"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 1 } },
    new CountryModel { Code = ""PL"", Name = ""Poland"", Medals = new MedalsModel { Gold = 4, Silver = 5, Bronze = 5 } },
    new CountryModel { Code = ""PT"", Name = ""Portugal"", Medals = new MedalsModel { Gold = 1, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""PR"", Name = ""Puerto Rico"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 0 } },
    new CountryModel { Code = ""QA"", Name = ""Qatar"", Medals = new MedalsModel { Gold = 2, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""KR"", Name = ""Republic of Korea"", Medals = new MedalsModel { Gold = 6, Silver = 4, Bronze = 10 } },
    new CountryModel { Code = ""MD"", Name = ""Republic of Moldova"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""RO"", Name = ""Romania"", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 0 } },
    new CountryModel { Code = ""SM"", Name = ""San Marino"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 2 } },
    new CountryModel { Code = ""SA"", Name = ""Saudi Arabia"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""RS"", Name = ""Serbia"", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 5 } },
    new CountryModel { Code = ""SK"", Name = ""Slovakia"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 1 } },
    new CountryModel { Code = ""SI"", Name = ""Slovenia"", Medals = new MedalsModel { Gold = 3, Silver = 1, Bronze = 1 } },
    new CountryModel { Code = ""ZA"", Name = ""South Africa"", Medals = new MedalsModel { Gold = 1, Silver = 2, Bronze = 0 } },
    new CountryModel { Code = ""ES"", Name = ""Spain"", Medals = new MedalsModel { Gold = 3, Silver = 8, Bronze = 6 } },
    new CountryModel { Code = ""SE"", Name = ""Sweden"", Medals = new MedalsModel { Gold = 3, Silver = 6, Bronze = 0 } },
    new CountryModel { Code = ""CH"", Name = ""Switzerland"", Medals = new MedalsModel { Gold = 3, Silver = 4, Bronze = 6 } },
    new CountryModel { Code = ""SY"", Name = ""Syrian Arab Republic"", Medals = new MedalsModel { Gold = 0, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""TH"", Name = ""Thailand"", Medals = new MedalsModel { Gold = 1, Silver = 0, Bronze = 1 } },
    new CountryModel { Code = ""TR"", Name = ""Turkey"", Medals = new MedalsModel { Gold = 2, Silver = 2, Bronze = 9 } },
    new CountryModel { Code = ""TM"", Name = ""Turkmenistan"", Medals = new MedalsModel { Gold = 0, Silver = 1, Bronze = 0 } },
    new CountryModel { Code = ""UA"", Name = ""Ukraine"", Medals = new MedalsModel { Gold = 1, Silver = 6, Bronze = 12 } },
    new CountryModel { Code = ""US"", Name = ""United States of America"", Medals = new MedalsModel { Gold = 39, Silver = 41, Bronze = 33 } },
    new CountryModel { Code = ""UZ"", Name = ""Uzbekistan"", Medals = new MedalsModel { Gold = 3, Silver = 0, Bronze = 2 } },
    new CountryModel { Code = ""VE"", Name = ""Venezuela"", Medals = new MedalsModel { Gold = 1, Silver = 3, Bronze = 0 } },
];

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
}
