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
<BitPagination Count=""11"" MiddleCount=""3"" BoundaryCount=""1"" DefaultSelectedPage=""6"" />";

    private readonly string example6RazorCode = @"
<BitPagination Count=""24"" ShowFirstButton ShowLastButton />";

    private readonly string example7RazorCode = @"
<BitPagination Count=""5"" NextIcon=""@BitIconName.Next"" PreviousIcon=""@BitIconName.Previous"" />";

    private readonly string example8RazorCode = @"
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
<BitPagination Count=""5"" Color=""BitColor.Error"" Variant=""BitVariant.Text"" />";

    private readonly string example9RazorCode = @"
<BitPagination Count=""5"" Size=""BitSize.Small"" Variant=""BitVariant.Fill"" />
<BitPagination Count=""5"" Size=""BitSize.Small"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Size=""BitSize.Small"" Variant=""BitVariant.Text"" />

<BitPagination Count=""5"" Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" />
<BitPagination Count=""5"" Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" />
<BitPagination Count=""5"" Size=""BitSize.Medium"" Variant=""BitVariant.Text"" />

<BitPagination Count=""5"" Size=""BitSize.Large"" Variant=""BitVariant.Fill"" />
<BitPagination Count=""5"" Size=""BitSize.Large"" Variant=""BitVariant.Text"" />
<BitPagination Count=""5"" Size=""BitSize.Large"" Variant=""BitVariant.Outline"" />";

    private readonly string example10RazorCode = @"
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
               NextIcon=""@BitIconName.ChevronDown""
               PreviousIcon=""@BitIconName.ChevronUp""
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

    private readonly string example11RazorCode = @"
<BitPagination Count=""5"" SelectedPage=""oneWaySelectedPage"" />
<BitNumberField @bind-Value=""oneWaySelectedPage"" Min=""1"" Max=""5"" />

<BitPagination Count=""5"" @bind-SelectedPage=""twoWaySelectedPage"" />
<BitNumberField @bind-Value=""twoWaySelectedPage"" Min=""1"" Max=""5"" />

<BitPagination Count=""5"" OnChange=""p => onChangeSelectedPage = p"" />
<div>Changed page: <b>@onChangeSelectedPage</b></div>";
    private readonly string example11CsharpCode = @"
private int oneWaySelectedPage = 1;
private int twoWaySelectedPage = 2;
private int onChangeSelectedPage = 3;";

    private readonly string example12RazorCode = @"
<BitPagination Dir=""BitDir.Rtl"" Count=""5"" Variant=""BitVariant.Fill"" />
<BitPagination Dir=""BitDir.Rtl"" Count=""5"" Variant=""BitVariant.Outline"" />
<BitPagination Dir=""BitDir.Rtl"" Count=""5"" Variant=""BitVariant.Text"" />";

}
