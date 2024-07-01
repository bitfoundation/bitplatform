namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Pagination;

public partial class BitPaginationDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Appearance",
            Type = "BitAppearance",
            DefaultValue = "BitAppearance.Primary",
            Description = "The appearance of pagination, Possible values: Primary | Standard | Text",
            LinkType = LinkType.Link,
            Href = "#pagination-appearance-enum"
        },
        new()
        {
            Name = "BoundaryCount",
            Type = "int",
            DefaultValue = "2",
            Description = "The number of items at the start and end of the pagination."
        },
        new()
        {
            Name = "Classes",
            Type = "BitPaginationClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitPagination.",
            LinkType = LinkType.Link,
            Href = "#pagination-class-styles"
        },
        new()
        {
            Name = "Count",
            Type = "int",
            DefaultValue = "1",
            Description = "The number of pages."
        },
        new()
        {
            Name = "DefaultSelectedPage",
            Type = "int",
            DefaultValue = "0",
            Description = "The default selected page number."
        },
        new()
        {
            Name = "FirstIcon",
            Type = "string",
            DefaultValue = "ChevronLeftEnd6",
            Description = "Icon of the first button."
        },
        new()
        {
            Name = "LastIcon",
            Type = "string",
            DefaultValue = "ChevronRightEnd6",
            Description = "Icon of the last button."
        },
        new()
        {
            Name = "MiddleCount",
            Type = "int",
            DefaultValue = "3",
            Description = "The number of items in the middle of the pagination."
        },
        new()
        {
            Name = "NextIcon",
            Type = "string",
            DefaultValue = "ChevronRight",
            Description = "Icon of the next button."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<int>",
            DefaultValue = "null",
            Description = "Invokes the callback when the selected page changes."
        },
        new()
        {
            Name = "PreviousIcon",
            Type = "string",
            DefaultValue = "ChevronLeft",
            Description = "Icon of the previous button."
        },
        new()
        {
            Name = "SelectedPage",
            Type = "int",
            DefaultValue = "0",
            Description = "The selected page number."
        },
        new()
        {
            Name = "SelectedPageChanged",
            Type = "EventCallback<int>",
            DefaultValue = "null",
            Description = "Invokes the callback when the selected page changes."
        },
        new()
        {
            Name = "Severity",
            Type = "BitSeverity?",
            DefaultValue = "null",
            Description = "The severity of the pagination.",
            LinkType = LinkType.Link,
            Href = "#severity-enum"
        },
        new()
        {
            Name = "ShowFirstButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the navigate to the first page button is shown."
        },
        new()
        {
            Name = "ShowLastButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the navigate to the last page button is shown."
        },
        new()
        {
            Name = "ShowNextButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "If true, the navigate to the next page button is shown."
        },
        new()
        {
            Name = "ShowPreviousButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "If true, the navigate to the previous page button is shown."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of Pagination, Possible values: Small | Medium | Large",
            LinkType = LinkType.Link,
            Href = "#pagination-size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPaginationClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitPagination.",
            LinkType = LinkType.Link,
            Href = "#pagination-class-styles"
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "pagination-appearance-enum",
            Name = "BitAppearance",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="The appearance for primary actions that are high-emphasis.",
                    Value="0",
                },
                new()
                {
                    Name= "Standard",
                    Description="The appearance for important actions that are medium-emphasis.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="The appearance for less-pronounced actions.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "severity-enum",
            Name = "BitSeverity",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Info",
                    Description="Info styled Pagination.",
                    Value="0",
                },
                new()
                {
                    Name= "Success",
                    Description="Success styled Pagination.",
                    Value="1",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning styled Pagination.",
                    Value="2",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="Severe Warning styled Pagination.",
                    Value="3",
                },
                new()
                {
                    Name= "Error",
                    Description="Error styled Pagination.",
                    Value="4",
                }
            ]
        },
        new()
        {
            Id = "pagination-size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size Pagination.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size Pagination.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size Pagination.",
                    Value="2",
                }
            ]
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "pagination-class-styles",
            Title = "BitPaginationClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitPagination."
                },
                new()
                {
                    Name = "Button",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button of the BitPagination."
                },
                new()
                {
                    Name = "Ellipsis",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the ellipsis of the BitPagination."
                },
                new()
                {
                    Name = "SelectedButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the selected button of the BitPagination."
                },
                new()
                {
                    Name = "FirstButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the first button of the BitPagination."
                },
                new()
                {
                    Name = "FirstButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the first button of the BitPagination."
                },
                new()
                {
                    Name = "PreviousButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the previous button of the BitPagination."
                },
                new()
                {
                    Name = "PreviousButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the previous button of the BitPagination."
                },
                new()
                {
                    Name = "NextButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the next button of the BitPagination."
                },
                new()
                {
                    Name = "NextButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the next button of the BitPagination."
                },
                new()
                {
                    Name = "LastButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the last button of the BitPagination."
                },
                new()
                {
                    Name = "LastButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the last button of the BitPagination."
                }
            ]
        }
    ];



    private readonly string example1RazorCode = @"
<BitPagination Count=""4"" />

<BitPagination Count=""4"" Appearance=""BitAppearance.Standard"" />

<BitPagination Count=""4"" Appearance=""BitAppearance.Text"" />";

    private readonly string example2RazorCode = @"
<BitPagination Count=""4"" />

<BitPagination Count=""4"" IsEnabled=""false"" />";

    private readonly string example3RazorCode = @"
<BitPagination Count=""4"" Appearance=""BitAppearance.Standard"" />

<BitPagination Count=""4"" Appearance=""BitAppearance.Standard"" IsEnabled=""false"" />";

    private readonly string example4RazorCode = @"
<BitPagination Count=""4"" Appearance=""BitAppearance.Text"" />

<BitPagination Count=""4"" Appearance=""BitAppearance.Text"" IsEnabled=""false"" />";

    private readonly string example5RazorCode = @"
<BitPagination Count=""5"" DefaultSelectedPage=""3"" />";

    private readonly string example6RazorCode = @"
<BitPagination Count=""11"" DefaultSelectedPage=""6"" BoundaryCount=""1"" />";

    private readonly string example7RazorCode = @"
<BitPagination Count=""11"" MiddleCount=""3"" BoundaryCount=""1"" DefaultSelectedPage=""6"" />";

    private readonly string example8RazorCode = @"
<BitPagination Count=""4"" OnChange=""(int page) => paginationSelectedPage = page"" />
<div>Changed page: <b>@paginationSelectedPage</b></div>";
    private readonly string example8CsharpCode = @"
private int paginationSelectedPage = 1;";

    private readonly string example9RazorCode = @"
<BitPagination Count=""24"" ShowFirstButton ShowLastButton />";

    private readonly string example10RazorCode = @"
<BitPagination Count=""4"" NextIcon=""@BitIconName.Next"" PreviousIcon=""@BitIconName.Previous"" />";

    private readonly string example11RazorCode = @"
<BitPagination Count=""5"" Color=""BitSeverity.Info"" />
<BitPagination Count=""5"" Color=""BitSeverity.Info"" Appearance=""BitAppearance.Standard"" />
<BitPagination Count=""5"" Color=""BitSeverity.Info"" Appearance=""BitAppearance.Text"" />

<BitPagination Count=""5"" Color=""BitSeverity.Success"" />
<BitPagination Count=""5"" Color=""BitSeverity.Success"" Appearance=""BitAppearance.Standard"" />
<BitPagination Count=""5"" Color=""BitSeverity.Success"" Appearance=""BitAppearance.Text"" />

<BitPagination Count=""5"" Color=""BitSeverity.Warning"" />
<BitPagination Count=""5"" Color=""BitSeverity.Warning"" Appearance=""BitAppearance.Standard"" />
<BitPagination Count=""5"" Color=""BitSeverity.Warning"" Appearance=""BitAppearance.Text"" />

<BitPagination Count=""5"" Color=""BitSeverity.SevereWarning"" />
<BitPagination Count=""5"" Color=""BitSeverity.SevereWarning"" Appearance=""BitAppearance.Standard"" />
<BitPagination Count=""5"" Color=""BitSeverity.SevereWarning"" Appearance=""BitAppearance.Text"" />

<BitPagination Count=""5"" Color=""BitSeverity.Error"" />
<BitPagination Count=""5"" Color=""BitSeverity.Error"" Appearance=""BitAppearance.Standard"" />
<BitPagination Count=""5"" Color=""BitSeverity.Error"" Appearance=""BitAppearance.Text"" />";

    private readonly string example12RazorCode = @"
<BitPagination Count=""5"" Size=""BitSize.Small"" />
<BitPagination Count=""5"" Size=""BitSize.Medium"" />
<BitPagination Count=""5"" Size=""BitSize.Large"" />

<BitPagination Count=""5"" Size=""BitSize.Small"" Appearance=""BitAppearance.Standard"" />
<BitPagination Count=""5"" Size=""BitSize.Medium"" Appearance=""BitAppearance.Standard"" />
<BitPagination Count=""5"" Size=""BitSize.Large"" Appearance=""BitAppearance.Standard"" />

<BitPagination Count=""5"" Size=""BitSize.Small"" Appearance=""BitAppearance.Text"" />
<BitPagination Count=""5"" Size=""BitSize.Medium"" Appearance=""BitAppearance.Text"" />
<BitPagination Count=""5"" Size=""BitSize.Large"" Appearance=""BitAppearance.Text"" />";

    private readonly string example13RazorCode = @"
<style>
    .custom-class {
        margin-left: 1rem;
        border-radius: 0.125rem;
        box-shadow: aqua 0 0 0.5rem;
        background-color: #00ffff7d;
    }

    .custom-root {
        color: aqua;
        margin-left: 1rem;
    }

    .custom-button {
        border-radius: 50%;
    }
</style>


<BitPagination Count=""4""
               NextIcon=""@BitIconName.ChevronDown""
               PreviousIcon=""@BitIconName.ChevronUp""
               Style=""margin-left: 1rem; flex-flow: column;"" />

<BitPagination Count=""4"" Class=""custom-class"" />


<BitPagination Count=""4""
               Styles=""@(new() { Root = ""margin-left: 1rem; gap: 1rem;"",
                                 SelectedButton = ""background-color: tomato; color: #2e2e2e;"",
                                 Button = ""border-color: transparent; background-color: #2e2e2e; color: tomato;"" })"" />

<BitPagination Count=""4"" 
               Appearance=""BitAppearance.Standard""
               Classes=""@(new() { Root = ""custom-root"", Button = ""custom-button"" })"" />";

    private readonly string example14RazorCode = @"
<BitPagination Dir=""BitDir.Rtl"" Count=""4"" />

<BitPagination Dir=""BitDir.Rtl"" Count=""4"" Appearance=""BitAppearance.Standard"" />

<BitPagination Dir=""BitDir.Rtl"" Count=""4"" Appearance=""BitAppearance.Text"" />";

}
