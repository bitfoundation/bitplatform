namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Pagination;

public partial class BitPaginationDemo
{
    private readonly string example1RazorCode = @"
<BitPagination Count=""5"" />";

    private readonly string example2RazorCode = @"
<BitPagination Count=""5"" Variant=""BitVariant.Fill"" />
<BitPagination Count=""5"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Variant=""BitVariant.Text"" />";

    private readonly string example3RazorCode = @"
<BitPagination Count=""5"" DefaultSelectedPage=""3"" />";

    private readonly string example4RazorCode = @"
<BitPagination Count=""11"" DefaultSelectedPage=""6"" BoundaryCount=""1"" />";

    private readonly string example5RazorCode = @"
<BitPagination Count=""11"" MiddleCount=""3"" BoundaryCount=""1"" DefaultSelectedPage=""6"" />

<BitPagination Count=""20"" MiddleCount=""5"" BoundaryCount=""1"" DefaultSelectedPage=""10"" />";

    private readonly string example6RazorCode = @"
<BitPagination Count=""24"" EllipsisText=""..."" DefaultSelectedPage=""12"" />

<BitPagination Count=""24"" EllipsisText=""⋯"" EllipsisAriaLabel=""Hidden pages"" DefaultSelectedPage=""12"" />";

    private readonly string example7RazorCode = @"
<BitPagination Count=""24"" ShowFirstButton ShowLastButton />

<BitPagination Count=""24"" ShowFirstButton ShowLastButton ShowNextButton=""false"" ShowPreviousButton=""false"" />";

    private readonly string example8RazorCode = @"
<BitPagination Count=""12"" PreviousButtonText=""Previous"" NextButtonText=""Next"" DefaultSelectedPage=""5"" />

<BitPagination Count=""12""
               Rounded
               ShowFirstButton
               ShowLastButton
               FirstButtonText=""First""
               LastButtonText=""Last""
               ShowPageButtons=""false""
               PreviousButtonText=""Previous""
               NextButtonText=""Next""
               Variant=""BitVariant.Outline""
               DefaultSelectedPage=""5"" />";

    private readonly string example9RazorCode = @"
<BitPagination Count=""24"" ShowPageButtons=""false"" />

<BitPagination Count=""24"" ShowPageButtons=""false"" ShowFirstButton ShowLastButton DefaultSelectedPage=""12"" />";

    private readonly string example10RazorCode = @"
<BitPagination Count=""12"" ShowSummary DefaultSelectedPage=""4"" />

<BitPagination Count=""12"" ShowSummary ShowPageButtons=""false"" ShowFirstButton ShowLastButton DefaultSelectedPage=""4"" />

<BitPagination Count=""24"" ShowSummary ShowPageButtons=""false"" GetSummary=""@GetItemsRangeSummary"" DefaultSelectedPage=""3"" />";
    private readonly string example10CsharpCode = @"
private string GetItemsRangeSummary(int page, int count)
{
    return $""Showing {(page - 1) * 10 + 1} to {page * 10} of {count * 10} results"";
}";

    private readonly string example11RazorCode = @"
<BitPagination ShowSummary
               ShowPageSizeSelector
               Count=""@pageSizeCount""
               PageSizeOptions=""@([10, 20, 50])""
               @bind-PageSize=""selectedPageSize""
               @bind-SelectedPage=""pageSizeSelectedPage""
               GetSummary=""@GetPageSizeSummary"" />

<div>Page size: <b>@selectedPageSize</b>, page: <b>@pageSizeSelectedPage</b> of <b>@pageSizeCount</b></div>";
    private readonly string example11CsharpCode = @"
private const int totalItems = 240;
private int selectedPageSize = 10;
private int pageSizeSelectedPage = 1;
private int pageSizeCount => (int)Math.Ceiling(totalItems / (double)selectedPageSize);

private string GetPageSizeSummary(int page, int count)
{
    return $""Showing {(page - 1) * selectedPageSize + 1} to {Math.Min(page * selectedPageSize, totalItems)} of {totalItems}"";
}";

    private readonly string example12RazorCode = @"
<BitPagination ShowSummary
               ShowPageSizeSelector
               TotalItems=""243""
               PageSizeOptions=""@([10, 25, 50])""
               @bind-PageSize=""totalItemsPageSize""
               @bind-SelectedPage=""totalItemsSelectedPage"" />

<div>Page size: <b>@totalItemsPageSize</b>, page: <b>@totalItemsSelectedPage</b></div>";
    private readonly string example12CsharpCode = @"
private int totalItemsPageSize = 10;
private int totalItemsSelectedPage = 1;";

    private readonly string example13RazorCode = @"
<BitPagination Count=""1250"" ShowGoToPage DefaultSelectedPage=""4"" />

<BitPagination Count=""24"" ShowGoToPage GoToPageText="""" ShowSummary ShowPageButtons=""false"" ShowFirstButton ShowLastButton DefaultSelectedPage=""4"" />";

    private readonly string example14RazorCode = @"
<BitPagination Count=""5"" Loop />";

    private readonly string example15RazorCode = @"
<BitPagination Count=""1"" HideOnSinglePage />

<BitPagination Count=""3"" HideOnSinglePage />";

    private readonly string example16RazorCode = @"
<BitPagination Count=""5""
               ShowFirstButton
               ShowLastButton
               NextButtonIconName=""@BitIconName.Next""
               PreviousButtonIconName=""@BitIconName.Previous""
               FirstButtonIconName=""@BitIconName.DoubleChevronLeft""
               LastButtonIconName=""@BitIconName.DoubleChevronRight"" />";

    private readonly string example17RazorCode = @"
<BitPagination Count=""9"" Rounded ShowFirstButton ShowLastButton DefaultSelectedPage=""5"" />

<BitPagination Count=""9"" Rounded Variant=""BitVariant.Outline"" DefaultSelectedPage=""5"" />

<BitPagination Count=""9"" Rounded Variant=""BitVariant.Text"" DefaultSelectedPage=""5"" />";

    private readonly string example18RazorCode = @"
<BitPagination Count=""5"" SelectedPage=""oneWaySelectedPage"" />
<BitNumberField @bind-Value=""oneWaySelectedPage"" Min=""1"" Max=""5"" />

<BitPagination Count=""5"" @bind-SelectedPage=""twoWaySelectedPage"" />
<BitNumberField @bind-Value=""twoWaySelectedPage"" Min=""1"" Max=""5"" />

<BitPagination Count=""5"" OnChange=""p => onChangeSelectedPage = p"" />
<div>Changed page: <b>@onChangeSelectedPage</b></div>";
    private readonly string example18CsharpCode = @"
private int oneWaySelectedPage = 1;
private int twoWaySelectedPage = 2;
private int onChangeSelectedPage = 3;";

    private readonly string example19RazorCode = @"
<BitPagination Count=""8""
               ShowFirstButton
               ShowLastButton
               GetPageHref=""@GetDemoPageHref""
               @bind-SelectedPage=""linkSelectedPage"" />

<div>Selected page: <b>@linkSelectedPage</b></div>";
    private readonly string example19CsharpCode = @"
private int linkSelectedPage = 1;

private string GetDemoPageHref(int page)
{
    return $""#example19-page-{page}"";
}";

    private readonly string example20RazorCode = @"
<BitPagination Count=""12""
               ShowFirstButton
               ShowLastButton
               DefaultSelectedPage=""4""
               AriaLabel=""Search results pages""
               FirstButtonAriaLabel=""Go to the first page""
               LastButtonAriaLabel=""Go to the last page""
               NextButtonAriaLabel=""Go to the next page""
               PreviousButtonAriaLabel=""Go to the previous page""
               GetPageAriaLabel=""@GetResultsRangeLabel"" />";
    private readonly string example20CsharpCode = @"
private string GetResultsRangeLabel(int page, bool isSelected)
{
    return $""Results {(page - 1) * 10 + 1} to {page * 10}"";
}";

    private readonly string example21RazorCode = @"
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


<div><b>Disabled</b>:</div>

<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Primary"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Primary"" Variant=""BitVariant.Text"" />

<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Secondary"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" />

<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Tertiary"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" />

<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Info"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Info"" Variant=""BitVariant.Text"" />

<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Success"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Success"" Variant=""BitVariant.Outline"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Success"" Variant=""BitVariant.Text"" />

<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Warning"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Warning"" Variant=""BitVariant.Text"" />

<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.SevereWarning"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" />

<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Error"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Error"" Variant=""BitVariant.Outline"" />
<BitPagination IsEnabled=""false"" Count=""5"" Color=""BitColor.Error"" Variant=""BitVariant.Text"" />";

    private readonly string example22RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitPagination Count=""5""
               NextButtonIcon=""@BitIconInfo.Fa(""solid forward"")""
               PreviousButtonIcon=""@BitIconInfo.Fa(""solid backward"")"" />

<BitPagination Count=""5""
               ShowFirstButton
               ShowLastButton
               NextButtonIcon=""@BitIconInfo.Css(""fa-solid fa-angle-right"")""
               PreviousButtonIcon=""@BitIconInfo.Css(""fa-solid fa-angle-left"")""
               FirstButtonIcon=""@BitIconInfo.Css(""fa-solid fa-angles-left"")""
               LastButtonIcon=""@BitIconInfo.Css(""fa-solid fa-angles-right"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitPagination Count=""5""
               NextButtonIcon=""@BitIconInfo.Bi(""chevron-right"")""
               PreviousButtonIcon=""@BitIconInfo.Bi(""chevron-left"")"" />

<BitPagination Count=""5""
               ShowFirstButton
               ShowLastButton
               NextButtonIcon=""@BitIconInfo.Css(""bi bi-caret-right-fill"")""
               PreviousButtonIcon=""@BitIconInfo.Css(""bi bi-caret-left-fill"")""
               FirstButtonIcon=""@BitIconInfo.Css(""bi bi-skip-start-fill"")""
               LastButtonIcon=""@BitIconInfo.Css(""bi bi-skip-end-fill"")"" />";

    private readonly string example23RazorCode = @"
<BitPagination Count=""5"" Size=""BitSize.Small"" Variant=""BitVariant.Fill"" />
<BitPagination Count=""5"" Size=""BitSize.Small"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Size=""BitSize.Small"" Variant=""BitVariant.Text"" />

<BitPagination Count=""5"" Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" />
<BitPagination Count=""5"" Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Size=""BitSize.Medium"" Variant=""BitVariant.Text"" />

<BitPagination Count=""5"" Size=""BitSize.Large"" Variant=""BitVariant.Fill"" />
<BitPagination Count=""5"" Size=""BitSize.Large"" Variant=""BitVariant.Text"" />
<BitPagination Count=""5"" Size=""BitSize.Large"" Variant=""BitVariant.Outline"" />";

    private readonly string example24RazorCode = @"
<style>
    .custom-class {
        margin-left: 1rem;
        border-radius: 0.125rem;
        box-shadow: aqua 0 0 0.5rem;
        background-color: #00ffff7d;
    }


    .custom-root {
        margin-left: 1rem;
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
</style>

<BitPagination Count=""5""
               NextButtonIconName=""@BitIconName.ChevronDown""
               PreviousButtonIconName=""@BitIconName.ChevronUp""
               Style=""margin-left: 1rem; flex-flow: column;"" />

<BitPagination Count=""5"" Class=""custom-class"" />

<BitPagination Count=""5""
               Styles=""@(new() { Root = ""margin-left: 1rem; gap: 1rem;"",
                                 SelectedButton = ""background-color: tomato; color: #2e2e2e;"",
                                 Button = ""border-color: transparent; background-color: #2e2e2e; color: tomato;"" })"" />

<BitPagination Count=""5""
               Variant=""BitVariant.Outline""
               Classes=""@(new() { Root = ""custom-root"",
                                  Button = ""custom-button"",
                                  SelectedButton = ""custom-selected-button""})"" />";

    private readonly string example25RazorCode = @"
<BitPagination Dir=""BitDir.Rtl"" Count=""5"" Variant=""BitVariant.Fill"" ShowFirstButton ShowLastButton />
<BitPagination Dir=""BitDir.Rtl"" Count=""5"" Variant=""BitVariant.Outline"" />
<BitPagination Dir=""BitDir.Rtl"" Count=""5"" Variant=""BitVariant.Text"" />";

}
