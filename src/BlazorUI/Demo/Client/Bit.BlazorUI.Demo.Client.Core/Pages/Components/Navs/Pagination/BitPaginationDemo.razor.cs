namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Pagination;

public partial class BitPaginationDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "BoundaryCount",
            Type = "int",
            DefaultValue = "2",
            Description = "The number of items at the start and end of the pagination. A value that is not positive falls back to the default."
        },
        new()
        {
            Name = "Classes",
            Type = "BitPaginationClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the pagination.",
            LinkType = LinkType.Link,
            Href = "#pagination-class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the pagination.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "Count",
            Type = "int",
            DefaultValue = "1",
            Description = "The total number of pages."
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
            Name = "FirstButtonAriaLabel",
            Type = "string",
            DefaultValue = "\"First page\"",
            Description = "The accessible label of the first button, which is used as its tooltip as well."
        },
        new()
        {
            Name = "FirstButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon for the first button using custom CSS classes for external icon libraries. Takes precedence over FirstButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "FirstButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The built-in icon name for the first button.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "GetPageAriaLabel",
            Type = "Func<int, bool, string>?",
            DefaultValue = "null",
            Description = "Provides the accessible label of a page button, from its one-based number and whether it is the selected one, replacing the default \"Page {number}\" label."
        },
        new()
        {
            Name = "GetSummary",
            Type = "Func<int, int, string>?",
            DefaultValue = "null",
            Description = "Provides the text of the summary, from the selected page and the total number of pages, replacing the default \"Page {number} of {count}\" text."
        },
        new()
        {
            Name = "HideOnSinglePage",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders nothing at all while there is a single page to navigate."
        },
        new()
        {
            Name = "LastButtonAriaLabel",
            Type = "string",
            DefaultValue = "\"Last page\"",
            Description = "The accessible label of the last button, which is used as its tooltip as well."
        },
        new()
        {
            Name = "LastButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon for the last button using custom CSS classes for external icon libraries. Takes precedence over LastButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "LastButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The built-in icon name for the last button.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "Loop",
            Type = "bool",
            DefaultValue = "false",
            Description = "Wraps the next and previous buttons around the ends of the range, and keeps them enabled there."
        },
        new()
        {
            Name = "MiddleCount",
            Type = "int",
            DefaultValue = "3",
            Description = "The number of items to render in the middle of the pagination. A value that is not positive falls back to the default."
        },
        new()
        {
            Name = "NextButtonAriaLabel",
            Type = "string",
            DefaultValue = "\"Next page\"",
            Description = "The accessible label of the next button, which is used as its tooltip as well."
        },
        new()
        {
            Name = "NextButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon for the next button using custom CSS classes for external icon libraries. Takes precedence over NextButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "NextButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The built-in icon name for the next button.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<int>",
            DefaultValue = "null",
            Description = "The event callback for when selected page changes. It also runs when SelectedPage is bound one way."
        },
        new()
        {
            Name = "PreviousButtonAriaLabel",
            Type = "string",
            DefaultValue = "\"Previous page\"",
            Description = "The accessible label of the previous button, which is used as its tooltip as well."
        },
        new()
        {
            Name = "PreviousButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon for the previous button using custom CSS classes for external icon libraries. Takes precedence over PreviousButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PreviousButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The built-in icon name for the previous button.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "Rounded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the buttons of the pagination with fully rounded (circular) corners."
        },
        new()
        {
            Name = "SelectedPage",
            Type = "int",
            DefaultValue = "0",
            Description = "The selected page number. It is one-based and is clamped into the available range while rendering."
        },
        new()
        {
            Name = "ShowFirstButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether to show the first button."
        },
        new()
        {
            Name = "ShowLastButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether to show the last button."
        },
        new()
        {
            Name = "ShowNextButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Determines whether to show the next button."
        },
        new()
        {
            Name = "ShowPageButtons",
            Type = "bool",
            DefaultValue = "true",
            Description = "Determines whether to show the numeric page buttons. Turning them off leaves a compact pagination made of the navigation buttons only."
        },
        new()
        {
            Name = "ShowPreviousButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Determines whether to show the previous button."
        },
        new()
        {
            Name = "ShowSummary",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the position in the range, which reads \"Page {number} of {count}\" unless GetSummary replaces it, ahead of the buttons of the pagination. It is a status region, so a screen reader reports the new position as the page changes."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the buttons.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPaginationClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitPagination.",
            LinkType = LinkType.Link,
            Href = "#pagination-class-styles"
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the pagination.",
            LinkType = LinkType.Link,
            Href = "#variant-enum"
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Info Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name= "Fill",
                    Description="Fill styled variant.",
                    Value="0",
                },
                new()
                {
                    Name= "Standard",
                    Description="Outline styled variant.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="Text styled variant.",
                    Value="2",
                }
            ]
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        },
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
                    Name = "Summary",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the summary of the BitPagination."
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



    private int oneWaySelectedPage = 1;
    private int twoWaySelectedPage = 2;
    private int onChangeSelectedPage = 3;

    private string GetResultsRangeLabel(int page, bool isSelected)
    {
        return $"Results {(page - 1) * 10 + 1} to {page * 10}";
    }

    private string GetItemsRangeSummary(int page, int count)
    {
        return $"Showing {(page - 1) * 10 + 1} to {page * 10} of {count * 10} results";
    }
}
