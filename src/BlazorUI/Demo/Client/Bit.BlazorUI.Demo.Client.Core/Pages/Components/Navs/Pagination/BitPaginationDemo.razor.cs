namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Pagination;

public partial class BitPaginationDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alignment",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "The horizontal alignment of the pagination inside the room it is given, which stretches it across that room. The pagination is only as wide as its own controls without it.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum"
        },
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
            Name = "ClickableEllipsis",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns every ellipsis into a control that jumps into the middle of the pages it collapses. It follows the rest of the pagination as a button or a link, is named by EllipsisAriaLabel, and hands the focus over to the page the jump landed on."
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
            Description = "The total number of pages. It is ignored while TotalItems is set, since the number of pages then follows from the number of items and the page size."
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
            Name = "EllipsisAriaLabel",
            Type = "string",
            DefaultValue = "\"More pages\"",
            Description = "The accessible label of the item standing in for the pages an ellipsis collapses. The glyph itself is hidden from assistive technologies and this label is announced in its place."
        },
        new()
        {
            Name = "EllipsisText",
            Type = "string",
            DefaultValue = "\"•••\"",
            Description = "The text of the ellipsis standing in for the pages that are collapsed out of the range."
        },
        new()
        {
            Name = "FirstButtonAriaLabel",
            Type = "string",
            DefaultValue = "\"First page\"",
            Description = "The accessible label of the first button, which is used as its tooltip as well until FirstButtonText puts a visible text beside its icon."
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
            Name = "FirstButtonText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text rendered beside the icon of the first button, which widens to fit it. The accessible name still comes from FirstButtonAriaLabel."
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
            Name = "GetPageHref",
            Type = "Func<int, string?>?",
            DefaultValue = "null",
            Description = "Provides the address a page control points at, from its one-based number, which turns every control of the pagination into a link instead of a button. A control with no address to point at reports aria-disabled and stays out of the tab order."
        },
        new()
        {
            Name = "GetSummary",
            Type = "Func<int, int, string>?",
            DefaultValue = "null",
            Description = "Provides the text of the summary, from the selected page and the total number of pages, replacing the default \"Page {number} of {count}\" (or \"1 - 10 of 240\" while TotalItems is set) text."
        },
        new()
        {
            Name = "GoToPageAriaLabel",
            Type = "string",
            DefaultValue = "\"Go to page\"",
            Description = "The accessible label of the go to page input, which steps in once the visible GoToPageText beside it is dropped. While that text is there it is the one naming the input."
        },
        new()
        {
            Name = "GoToPageText",
            Type = "string?",
            DefaultValue = "\"Go to\"",
            Description = "The text rendered ahead of the go to page input, which names it. An empty text leaves the input on its own, named by GoToPageAriaLabel."
        },
        new()
        {
            Name = "HideOnSinglePage",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders nothing at all while there is a single page to navigate, the summary, the page size selector and the jump included."
        },
        new()
        {
            Name = "LastButtonAriaLabel",
            Type = "string",
            DefaultValue = "\"Last page\"",
            Description = "The accessible label of the last button, which is used as its tooltip as well until LastButtonText puts a visible text beside its icon."
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
            Name = "LastButtonText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text rendered beside the icon of the last button, which widens to fit it. The accessible name still comes from LastButtonAriaLabel."
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
            Description = "The accessible label of the next button, which is used as its tooltip as well until NextButtonText puts a visible text beside its icon."
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
            Name = "NextButtonText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text rendered beside the icon of the next button, which widens to fit it. The accessible name still comes from NextButtonAriaLabel."
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
            Name = "OnPageSizeChange",
            Type = "EventCallback<int>",
            DefaultValue = "null",
            Description = "The event callback for when the page size is picked out of the page size selector. It also runs when PageSize is bound one way, and it is where Count is recomputed from the new page size."
        },
        new()
        {
            Name = "PageSize",
            Type = "int",
            DefaultValue = "0",
            Description = "The number of items a page holds, which the page size selector picks. A value that is not positive falls back to the first of the PageSizeOptions and the fallback is written back while the selector is shown, and one that is positive but not among them is offered by the selector along with them."
        },
        new()
        {
            Name = "PageSizeAriaLabel",
            Type = "string",
            DefaultValue = "\"Items per page\"",
            Description = "The accessible label of the page size selector, which steps in once the visible PageSizeText beside it is dropped. While that text is there it is the one naming the selector."
        },
        new()
        {
            Name = "PageSizeOptions",
            Type = "IEnumerable<int>?",
            DefaultValue = "null",
            Description = "The page sizes the page size selector offers. Sizes that are not positive are dropped, and an empty list falls back to the default 10, 25, 50 and 100. A PageSize the list does not hold is offered along with the others."
        },
        new()
        {
            Name = "PageSizeText",
            Type = "string?",
            DefaultValue = "\"Items per page\"",
            Description = "The text rendered ahead of the page size selector, which names it. An empty text leaves the selector on its own, named by PageSizeAriaLabel."
        },
        new()
        {
            Name = "PreviousButtonAriaLabel",
            Type = "string",
            DefaultValue = "\"Previous page\"",
            Description = "The accessible label of the previous button, which is used as its tooltip as well until PreviousButtonText puts a visible text beside its icon."
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
            Name = "PreviousButtonText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text rendered beside the icon of the previous button, which widens to fit it. The accessible name still comes from PreviousButtonAriaLabel."
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
            Name = "ShowGoToPage",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows an input that jumps straight to the page number typed into it, at the end of the pagination. The jump runs when the input is committed and a number outside of the range lands on the nearest end of it."
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
            Name = "ShowPageSizeSelector",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows a selector that picks how many items a page holds, ahead of everything else in the pagination. Picking a size reports it through PageSize and OnPageSizeChange, and recomputes the range of pages while TotalItems is set."
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
            Description = "Shows the position in the range, which reads \"Page {number} of {count}\" (or \"1 - 10 of 240\" while TotalItems is set) unless GetSummary replaces it, ahead of the buttons of the pagination. It is a status region, so a screen reader reports the new position as the page changes."
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
            Name = "TotalItems",
            Type = "int",
            DefaultValue = "0",
            Description = "The total number of items the pagination pages through, which the number of pages is worked out from along with PageSize, replacing Count. Picking a new page size then keeps the first item of the current page in view, and the summary reports the items instead of the pages."
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

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "() => ValueTask",
            Description = "Gives the keyboard focus to the button of the selected page, falling back to the first navigation button that is rendered while the page buttons are turned off. This is what a consumer reloading the list behind the pagination calls to put the focus back on the navigation the reload was asked from."
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "alignment-enum",
            Name = "BitAlignment",
            Description = "Defines the horizontal alignment of the pagination inside the room it is given.",
            Items =
            [
                new()
                {
                    Name= "Start",
                    Description="Lines the controls up at the start of the room.",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="Lines the controls up at the end of the room.",
                    Value="1",
                },
                new()
                {
                    Name= "Center",
                    Description="Centers the controls inside the room.",
                    Value="2",
                },
                new()
                {
                    Name= "SpaceBetween",
                    Description="Shares the room out between the controls, leaving none of it at the two ends.",
                    Value="3",
                },
                new()
                {
                    Name= "SpaceAround",
                    Description="Shares the room out around the controls, leaving half of it at the two ends.",
                    Value="4",
                },
                new()
                {
                    Name= "SpaceEvenly",
                    Description="Shares the room out evenly between the controls and at the two ends.",
                    Value="5",
                },
                new()
                {
                    Name= "Baseline",
                    Description="Lines the controls up on their baseline.",
                    Value="6",
                },
                new()
                {
                    Name= "Stretch",
                    Description="Stretches the controls across the room.",
                    Value="7",
                }
            ]
        },
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
                    Name = "PageSizeSelector",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the page size selector container of the BitPagination."
                },
                new()
                {
                    Name = "PageSizeLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the page size label of the BitPagination."
                },
                new()
                {
                    Name = "PageSizeSelect",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the page size select of the BitPagination."
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
                    Name = "GoToPage",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the go to page container of the BitPagination."
                },
                new()
                {
                    Name = "GoToPageLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the go to page label of the BitPagination."
                },
                new()
                {
                    Name = "GoToPageInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the go to page input of the BitPagination."
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
                    Name = "ButtonText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the text rendered beside the icon of a navigation button of the BitPagination."
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



    private const int totalItems = 240;
    private int selectedPageSize = 10;
    private int pageSizeSelectedPage = 1;
    private int pageSizeCount => (int)Math.Ceiling(totalItems / (double)selectedPageSize);

    private string GetPageSizeSummary(int page, int count)
    {
        return $"Showing {(page - 1) * selectedPageSize + 1} to {Math.Min(page * selectedPageSize, totalItems)} of {totalItems}";
    }

    private int totalItemsPageSize = 10;
    private int totalItemsSelectedPage = 1;

    private int linkSelectedPage = 1;

    private string GetDemoPageHref(int page)
    {
        return $"#example20-page-{page}";
    }

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
