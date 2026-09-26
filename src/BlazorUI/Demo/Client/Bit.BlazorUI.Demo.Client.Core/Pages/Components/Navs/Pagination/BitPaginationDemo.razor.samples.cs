namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Pagination;

public partial class BitPaginationDemo
{
    private readonly string example1RazorCode = @"
<BitPagination Count=""5"" />

<BitPagination Count=""5"" DefaultSelectedPage=""3"" />";

    private readonly string example2RazorCode = @"
<BitPagination Count=""5"" SelectedPage=""oneWaySelectedPage"" />
<BitNumberField @bind-Value=""oneWaySelectedPage"" Min=""1"" Max=""5"" />

<BitPagination Count=""5"" @bind-SelectedPage=""twoWaySelectedPage"" />
<BitNumberField @bind-Value=""twoWaySelectedPage"" Min=""1"" Max=""5"" />

<BitPagination Count=""5"" OnChange=""p => onChangeSelectedPage = p"" />
<div>Changed page: <b>@onChangeSelectedPage</b></div>";
    private readonly string example2CsharpCode = @"
private int oneWaySelectedPage = 1;
private int twoWaySelectedPage = 2;
private int onChangeSelectedPage = 3;";

    private readonly string example3RazorCode = @"
<BitPagination Count=""5"" Variant=""BitVariant.Fill"" />
<BitPagination Count=""5"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Variant=""BitVariant.Text"" />

<BitPagination Count=""9"" Rounded ShowFirstButton ShowLastButton DefaultSelectedPage=""5"" />
<BitPagination Count=""9"" Rounded Variant=""BitVariant.Outline"" DefaultSelectedPage=""5"" />";

    private readonly string example4RazorCode = @"
<BitPagination Count=""11"" BoundaryCount=""1"" DefaultSelectedPage=""6"" />

<BitPagination Count=""20"" BoundaryCount=""1"" MiddleCount=""5"" DefaultSelectedPage=""10"" />";

    private readonly string example5RazorCode = @"
<BitPagination Count=""24"" EllipsisText=""..."" EllipsisAriaLabel=""Hidden pages"" DefaultSelectedPage=""12"" />

<BitPagination Count=""500"" ClickableEllipsis DefaultSelectedPage=""250"" />";

    private readonly string example6RazorCode = @"
<BitPagination Count=""24"" ShowFirstButton ShowLastButton />

<BitPagination Count=""24"" ShowFirstButton ShowLastButton ShowNextButton=""false"" ShowPreviousButton=""false"" />

<BitPagination Count=""5"" Loop />";

    private readonly string example7RazorCode = @"
<BitPagination Count=""12"" PreviousButtonText=""Previous"" NextButtonText=""Next"" DefaultSelectedPage=""5"" />

<BitPagination Count=""5""
               ShowFirstButton
               ShowLastButton
               NextButtonIconName=""@BitIconName.Next""
               PreviousButtonIconName=""@BitIconName.Previous""
               FirstButtonIconName=""@BitIconName.DoubleChevronLeft""
               LastButtonIconName=""@BitIconName.DoubleChevronRight"" />";

    private readonly string example8RazorCode = @"
<BitPagination Count=""12"" ShowSummary DefaultSelectedPage=""4"" />

<BitPagination Count=""12"" ShowSummary ShowPageButtons=""false"" ShowFirstButton ShowLastButton DefaultSelectedPage=""4"" />

<BitPagination Count=""24"" ShowSummary ShowPageButtons=""false"" GetSummary=""@GetItemsRangeSummary"" DefaultSelectedPage=""3"" />";
    private readonly string example8CsharpCode = @"
private string GetItemsRangeSummary(int page, int count)
{
    return $""Showing {(page - 1) * 10 + 1} to {page * 10} of {count * 10} results"";
}";

    private readonly string example9RazorCode = @"
<BitPagination ShowSummary
               ShowPageSizeSelector
               TotalItems=""240""
               PageSizeOptions=""@([10, 25, 100])""
               @bind-PageSize=""totalItemsPageSize""
               @bind-SelectedPage=""totalItemsSelectedPage"" />

<div>Page <b>@totalItemsSelectedPage</b>, <b>@totalItemsPageSize</b> items a page</div>


<BitPagination ShowPageSizeSelector
               PageSizeText=""""
               Count=""@pageSizeCount""
               PageSizeOptions=""@([10, 20, 50])""
               @bind-PageSize=""selectedPageSize""
               @bind-SelectedPage=""pageSizeSelectedPage"" />

<div>Page <b>@pageSizeSelectedPage</b> of <b>@pageSizeCount</b>, <b>@selectedPageSize</b> items a page</div>";
    private readonly string example9CsharpCode = @"
private int totalItemsPageSize = 10;
private int totalItemsSelectedPage = 1;

private const int totalItems = 240;
private int selectedPageSize = 10;
private int pageSizeSelectedPage = 1;
private int pageSizeCount => (int)Math.Ceiling(totalItems / (double)selectedPageSize);";

    private readonly string example10RazorCode = @"
<BitPagination Count=""1250"" ShowGoToPage DefaultSelectedPage=""4"" />

<BitPagination Count=""24"" ShowGoToPage GoToPageText="""" ShowSummary ShowPageButtons=""false"" DefaultSelectedPage=""4"" />";

    private readonly string example11RazorCode = @"
<BitPagination Count=""1"" HideOnSinglePage />

<BitPagination Count=""3"" HideOnSinglePage />";

    private readonly string example12RazorCode = @"
<style>
    .alignment-box {
        padding: 0.5rem;
        border: 1px solid gray;
    }
</style>

<div class=""alignment-box"">
    <BitPagination Count=""5"" Alignment=""BitAlignment.Start"" />
</div>

<div class=""alignment-box"">
    <BitPagination Count=""5"" Alignment=""BitAlignment.Center"" />
</div>

<div class=""alignment-box"">
    <BitPagination Count=""5"" Alignment=""BitAlignment.End"" />
</div>

<div class=""alignment-box"">
    <BitPagination Count=""5"" Alignment=""BitAlignment.SpaceBetween"" />
</div>";

    private readonly string example13RazorCode = @"
<BitPagination Count=""linkPageCount""
               ShowFirstButton
               ShowLastButton
               GetPageHref=""@GetDemoPageHref""
               @bind-SelectedPage=""linkSelectedPage"" />

<div>Selected page: <b>@linkSelectedPage</b></div>";
    private readonly string example13CsharpCode = @"
[SupplyParameterFromQuery(Name = ""page"")] public string? LinkPage { get; set; }

private const int linkPageCount = 8;
private int linkSelectedPage = 1;

protected override void OnParametersSet()
{
    if (int.TryParse(LinkPage, out var page))
    {
        linkSelectedPage = Math.Clamp(page, 1, linkPageCount);
    }

    base.OnParametersSet();
}

private string GetDemoPageHref(int page)
{
    return $""?page={page}"";
}";

    private readonly string example14RazorCode = @"
<BitPagination @ref=""accessiblePagination""
               Count=""12""
               ShowFirstButton
               ShowLastButton
               DefaultSelectedPage=""4""
               AriaLabel=""Search results pages""
               FirstButtonAriaLabel=""Go to the first page""
               LastButtonAriaLabel=""Go to the last page""
               NextButtonAriaLabel=""Go to the next page""
               PreviousButtonAriaLabel=""Go to the previous page""
               GetPageAriaLabel=""@GetResultsRangeLabel"" />

<BitButton Variant=""BitVariant.Outline"" OnClick=""@(async () => await accessiblePagination.FocusAsync())"">Reload & focus the pagination</BitButton>";
    private readonly string example14CsharpCode = @"
private BitPagination accessiblePagination = default!;

private string GetResultsRangeLabel(int page, bool isSelected)
{
    return $""Results {(page - 1) * 10 + 1} to {page * 10}"";
}";

    private readonly string example15RazorCode = @"
<BitParams Parameters=""@paginationParams"">
    <BitPagination Count=""12"" DefaultSelectedPage=""4"" />
    <BitPagination Count=""12"" DefaultSelectedPage=""4"" Variant=""BitVariant.Fill"" ShowSummary=""false"" />
</BitParams>

<BitPagination Count=""12"" DefaultSelectedPage=""4"" />";
    private readonly string example15CsharpCode = @"
private readonly BitPaginationParams[] paginationParams =
[
    new()
    {
        Rounded = true,
        ShowSummary = true,
        ShowFirstButton = true,
        ShowLastButton = true,
        Variant = BitVariant.Outline,
        AriaLabel = ""Paginación"",
        FirstButtonAriaLabel = ""Primera página"",
        PreviousButtonAriaLabel = ""Página anterior"",
        NextButtonAriaLabel = ""Página siguiente"",
        LastButtonAriaLabel = ""Última página"",
        EllipsisAriaLabel = ""Más páginas"",
        GetPageAriaLabel = (page, _) => $""Página {page}"",
        GetSummary = (page, count) => $""Página {page} de {count}"",
    }
];";

    private readonly string example16RazorCode = @"
<BitPagination Count=""5"" Color=""BitColor.Primary"" />
<BitPagination Count=""5"" Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Color=""BitColor.Primary"" Variant=""BitVariant.Text"" />

<BitPagination Count=""5"" Color=""BitColor.Secondary"" />
<BitPagination Count=""5"" Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" />

<BitPagination Count=""5"" Color=""BitColor.Tertiary"" />
<BitPagination Count=""5"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" />

<BitPagination Count=""5"" Color=""BitColor.Info"" />
<BitPagination Count=""5"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Color=""BitColor.Info"" Variant=""BitVariant.Text"" />

<BitPagination Count=""5"" Color=""BitColor.Success"" />
<BitPagination Count=""5"" Color=""BitColor.Success"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Color=""BitColor.Success"" Variant=""BitVariant.Text"" />

<BitPagination Count=""5"" Color=""BitColor.Warning"" />
<BitPagination Count=""5"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Color=""BitColor.Warning"" Variant=""BitVariant.Text"" />

<BitPagination Count=""5"" Color=""BitColor.SevereWarning"" />
<BitPagination Count=""5"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" />

<BitPagination Count=""5"" Color=""BitColor.Error"" />
<BitPagination Count=""5"" Color=""BitColor.Error"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Color=""BitColor.Error"" Variant=""BitVariant.Text"" />

<BitPagination IsEnabled=""false"" Count=""5"" />
<BitPagination IsEnabled=""false"" Count=""5"" Variant=""BitVariant.Outline"" />
<BitPagination IsEnabled=""false"" Count=""5"" Variant=""BitVariant.Text"" />";

    private readonly string example17RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitPagination Count=""5""
               ShowFirstButton
               ShowLastButton
               NextButtonIcon=""@BitIconInfo.Fa(""solid angle-right"")""
               PreviousButtonIcon=""@BitIconInfo.Fa(""solid angle-left"")""
               FirstButtonIcon=""@BitIconInfo.Fa(""solid angles-left"")""
               LastButtonIcon=""@BitIconInfo.Fa(""solid angles-right"")"" />

<BitPagination Count=""5""
               ShowFirstButton
               ShowLastButton
               NextButtonIcon=""@BitIconInfo.Bi(""caret-right-fill"")""
               PreviousButtonIcon=""@BitIconInfo.Bi(""caret-left-fill"")""
               FirstButtonIcon=""@BitIconInfo.Css(""bi bi-skip-start-fill"")""
               LastButtonIcon=""@BitIconInfo.Css(""bi bi-skip-end-fill"")"" />";

    private readonly string example18RazorCode = @"
<BitPagination Count=""5"" Size=""BitSize.Small"" ShowSummary ShowGoToPage PreviousButtonText=""Prev"" NextButtonText=""Next"" />

<BitPagination Count=""5"" Size=""BitSize.Medium"" ShowSummary ShowGoToPage PreviousButtonText=""Prev"" NextButtonText=""Next"" />

<BitPagination Count=""5"" Size=""BitSize.Large"" ShowSummary ShowGoToPage PreviousButtonText=""Prev"" NextButtonText=""Next"" />";

    private readonly string example19RazorCode = @"
<BitPagination Count=""5""
               Variant=""BitVariant.Text""
               Style=""--bit-Pagination-selected-background: #0f766e; --bit-Pagination-button-hover-background: #ccfbf1; --bit-Pagination-button-color: #0f766e; --bit-Pagination-button-hover-color: #0f766e; --bit-Pagination-selected-font-weight: 700;"" />

<div class=""pill-pagination"">
    <BitPagination Count=""12"" DefaultSelectedPage=""6"" Variant=""BitVariant.Outline"" />
    <BitPagination Count=""5"" ShowPageSizeSelector PageSizeText="""" Variant=""BitVariant.Outline"" />
</div>


<div>
    <BitPagination Count=""5"" Style=""margin-inline: 1rem; flex-flow: column;""
                   NextButtonIconName=""@BitIconName.ChevronDown"" PreviousButtonIconName=""@BitIconName.ChevronUp"" />

    <BitPagination Count=""5"" Class=""custom-class"" />


    <BitPagination Count=""5""
                   Styles=""@(new() { Root = ""margin-inline: 1rem; gap: 1rem;"",
                                     SelectedButton = ""background-color: tomato; color: #2e2e2e;"",
                                     Button = ""border-color: transparent; background-color: #2e2e2e; color: tomato;"" })"" />

    <BitPagination Count=""5""
                   Variant=""BitVariant.Outline""
                   Classes=""@(new() { Root = ""custom-root"",
                                      Button = ""custom-button"",
                                      SelectedButton = ""custom-selected-button"" })"" />
</div>";
    private const string example19ScssCode = @"
.pill-pagination {
    // The variables inherit, so one block on an ancestor re-skins every pagination underneath it.
    --bit-Pagination-gap: 0.375rem;
    --bit-Pagination-button-radius: 999px;
    --bit-Pagination-button-size: 2.25rem;
    --bit-Pagination-button-color: #7c3aed;
    --bit-Pagination-button-border-color: #ddd6fe;
    --bit-Pagination-button-hover-color: #5b21b6;
    --bit-Pagination-button-hover-background: #ede9fe;
    --bit-Pagination-selected-color: #ffffff;
    --bit-Pagination-selected-background: #7c3aed;
    --bit-Pagination-focus-color: #7c3aed;
    --bit-Pagination-input-border-color: #c4b5fd;
}

::deep {
    .custom-class {
        margin-inline: 1rem;
        border-radius: 0.125rem;
        box-shadow: aqua 0 0 0.5rem;
        background-color: #00ffff7d;
    }

    .custom-root {
        margin-inline: 1rem;
    }

    .custom-button {
        color: seagreen;
        border-radius: 50%;
        border-color: seagreen;
    }

    .custom-button:hover {
        color: white;
        background-color: mediumseagreen;
    }

    .custom-selected-button {
        color: white;
        background-color: seagreen;
    }
}";
    private readonly DemoCodeFile[] example19CodeFiles =
    [
        new("BitPaginationDemo.razor.scss", example19ScssCode),
    ];

    private readonly string example20RazorCode = @"
<BitPagination Dir=""BitDir.Rtl"" Count=""5"" ShowFirstButton ShowLastButton />

<BitPagination Dir=""BitDir.Rtl"" Count=""5"" Variant=""BitVariant.Outline"" ShowSummary GetSummary=""@((p, c) => $""صفحه {p} از {c}"")"" />";
}
