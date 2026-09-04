namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Pivot;

public partial class BitPivotDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Addable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a button at the end of the pivot items that reports a request for a new tab through the OnAdd callback. The pivot does not add an item itself, since the items belong to the markup that declares them, so the handler is what puts the new one into the list.",
        },
        new()
        {
            Name = "AddAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the add button of the pivot (default: Add).",
        },
        new()
        {
            Name = "AddIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon of the add button of the pivot using custom CSS classes for external icon libraries. Takes precedence over AddIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "AddIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon of the add button of the pivot from the built-in Fluent UI icons (default: Add).",
        },
        new()
        {
            Name = "AddTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title (tooltip) of the add button of the pivot (default: Add).",
        },
        new()
        {
            Name = "Alignment",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Determines the alignment of the header section of the pivot.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "AriaLabelledBy",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element that labels the header of the pivot (rendered into the aria-labelledby of the tablist), which is what names a pivot sitting under a heading of its own. It wins over the AriaLabel.",
        },
        new()
        {
            Name = "AutoHideSlideButtons",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the next and previous buttons of the Slide overflow behavior only while there actually is something to slide to, instead of leaving them in place in their disabled state.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of pivot.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitPivotClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the pivot.",
            LinkType = LinkType.Link,
            Href = "#pivot-class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the pivot.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DefaultSelectedKey",
            Type = "string?",
            DefaultValue = "null",
            Description = "Default selected key for the pivot.",
        },
        new()
        {
            Name = "DismissAriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the aria-label of the dismiss button of the pivot items (default: \"Remove {0}\"), where the placeholder is filled with the header text of the item.",
        },
        new()
        {
            Name = "DismissIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon of the dismiss button of the pivot items using custom CSS classes for external icon libraries. Takes precedence over DismissIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "DismissIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon of the dismiss button of the pivot items from the built-in Fluent UI icons (default: Cancel).",
        },
        new()
        {
            Name = "Dismissible",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a dismiss button on every pivot item, which reports the item to dismiss through the OnItemDismiss callback. A single item opts in or out of it on its own using the Dismissible parameter of the BitPivotItem.",
        },
        new()
        {
            Name = "DismissTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title (tooltip) of the dismiss button of the pivot items (default: Remove).",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stretches the pivot items to share the whole width (or the whole height in a vertical pivot) of the header.",
        },
        new()
        {
            Name = "Gap",
            Type = "string?",
            DefaultValue = "null",
            Description = "The gap between the pivot items of the header.",
        },
        new()
        {
            Name = "HeaderEnd",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered at the end of the header, after the pivot items and after the overflow or slide affordances, which is where the actions belonging to the whole pivot usually go.",
        },
        new()
        {
            Name = "HeaderOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to skip rendering the tabpanel with the content of the selected tab.",
        },
        new()
        {
            Name = "HeaderStart",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered at the start of the header, before the pivot items and before the overflow or slide affordances.",
        },
        new()
        {
            Name = "HeaderType",
            Type = "BitPivotHeaderType",
            DefaultValue = "BitPivotHeaderType.Link",
            Description = "The type of the pivot header items.",
            LinkType = LinkType.Link,
            Href = "#header-type-enum",
        },
        new()
        {
            Name = "KeepMounted",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the content of every tab that has been shown at least once mounted, so that leaving a tab and coming back to it finds the content in the state it was left in. Unlike MountAll, a tab that has never been selected is not rendered at all.",
        },
        new()
        {
            Name = "Loop",
            Type = "bool",
            DefaultValue = "true",
            Description = "Wraps the keyboard navigation of the header around at both of its ends, so that the next key on the last item lands on the first one.",
        },
        new()
        {
            Name = "MountAll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Mounts all tabs at render time and hide non-selected tabs with CSS styles instead of not-rendering them (useful for processing/extracting data).",
        },
        new()
        {
            Name = "Navigable",
            Type = "bool",
            DefaultValue = "true",
            Description = "Enables the roving tabindex behavior, which turns the whole header into a single tab stop that is navigable using the arrow, Home, and End keys.",
        },
        new()
        {
            Name = "NextAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the next button in the Slide overflow behavior (default: Next).",
        },
        new()
        {
            Name = "NextIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon of the next button in the Slide overflow behavior using custom CSS classes for external icon libraries. Takes precedence over NextIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "NextIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon of the next button in the Slide overflow behavior from the built-in Fluent UI icons (default: ChevronRight for horizontal pivots and ChevronDown for vertical pivots).",
        },
        new()
        {
            Name = "OnAdd",
            Type = "EventCallback",
            Description = "Callback for when the add button of an Addable pivot is clicked.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitPivotItem>",
            Description = "Callback for when the selected pivot item changes.",
            LinkType = LinkType.Link,
            Href = "#pivot-item",
        },
        new()
        {
            Name = "OnChanging",
            Type = "EventCallback<BitPivotChangeArgs>",
            Description = "Callback for just before the selected pivot item changes, which can call the change off by setting the Cancel of its arguments, so that a tab holding unsaved work can refuse to be left.",
            LinkType = LinkType.Link,
            Href = "#pivot-change-args",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitPivotItem>",
            Description = "Callback for when a pivot header item is clicked.",
            LinkType = LinkType.Link,
            Href = "#pivot-item",
        },
        new()
        {
            Name = "OnItemDismiss",
            Type = "EventCallback<BitPivotItem>",
            Description = "Callback for when the dismiss button of a pivot item is clicked, or the Delete key is pressed while it holds the focus. The pivot does not remove the item itself, since the items belong to the markup that declares them, so the handler is what takes the item out of the list.",
            LinkType = LinkType.Link,
            Href = "#pivot-item",
        },
        new()
        {
            Name = "OnItemReorder",
            Type = "EventCallback<BitPivotReorderEventArgs>",
            Description = "Callback for when a pivot item is dragged onto another one, or moved with the Ctrl+Arrow keys, in a Reorderable pivot. The pivot does not move the item itself, since the items belong to the markup that declares them, so the handler is what reorders the list.",
            LinkType = LinkType.Link,
            Href = "#pivot-reorder-event-args",
        },
        new()
        {
            Name = "OverflowAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the overflow menu button in the Menu overflow behavior.",
        },
        new()
        {
            Name = "OverflowBehavior",
            Type = "BitPivotOverflowBehavior?",
            DefaultValue = "null",
            Description = "Overflow behavior when there is not enough room to display all of the links/tabs.",
            LinkType = LinkType.Link,
            Href = "#overflowBehavior-enum",
        },
        new()
        {
            Name = "OverflowIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon of the overflow menu button in the Menu overflow behavior using custom CSS classes for external icon libraries. Takes precedence over OverflowIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "OverflowIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon of the overflow menu button in the Menu overflow behavior from the built-in Fluent UI icons (default: More).",
        },
        new()
        {
            Name = "Position",
            Type = "BitSide",
            DefaultValue = "BitSide.Top",
            Description = "Position of the pivot header.",
            LinkType = LinkType.Link,
            Href = "#pivotPosition-enum",
        },
        new()
        {
            Name = "PreviousAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the previous button in the Slide overflow behavior (default: Previous).",
        },
        new()
        {
            Name = "PreviousIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon of the previous button in the Slide overflow behavior using custom CSS classes for external icon libraries. Takes precedence over PreviousIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PreviousIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon of the previous button in the Slide overflow behavior from the built-in Fluent UI icons (default: ChevronLeft for horizontal pivots and ChevronUp for vertical pivots).",
        },
        new()
        {
            Name = "Reorderable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the pivot items be dragged onto one another, and the focused one be moved with the Ctrl+Arrow keys, to ask for a new order through the OnItemReorder callback. A single item opts in or out of it on its own using the Reorderable parameter of the BitPivotItem.",
        },
        new()
        {
            Name = "SelectedKey",
            Type = "string?",
            DefaultValue = "null",
            Description = "Key of the selected pivot item.",
        },
        new()
        {
            Name = "SelectOnFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Selects the focused pivot item while the header is navigated with the keyboard, so that the selection follows the focus (the automatic activation of the WAI-ARIA tabs pattern).",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the pivot header items.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Stacked",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stacks the icon of the pivot items on top of their text instead of putting the two side by side.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPivotClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the pivot.",
            Href = "#pivot-class-styles",
            LinkType = LinkType.Link
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "SelectedItem",
            Type = "BitPivotItem?",
            DefaultValue = "null",
            Description = "The pivot item that is currently selected.",
            LinkType = LinkType.Link,
            Href = "#pivot-item",
        },
        new()
        {
            Name = "Items",
            Type = "IReadOnlyList<BitPivotItem>",
            DefaultValue = "",
            Description = "The pivot items in the order they are declared in.",
            LinkType = LinkType.Link,
            Href = "#pivot-item",
        },
        new()
        {
            Name = "SelectItemByKey",
            Type = "Task SelectItemByKey(string? key)",
            DefaultValue = "",
            Description = "Selects the pivot item carrying the given key, if such an item exists and is enabled.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "pivot-item",
            Title = "BitPivotItem",
            Parameters =
            [
                new()
                {
                    Name = "Body",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The content of the pivot item, It can be Any custom tag or a text (alias of ChildContent).",
                },
                new()
                {
                    Name = "BodyClass",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom css class of the content of the pivot item.",
                },
                new()
                {
                    Name = "BodyStyle",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom css style of the content of the pivot item.",
                },
                new()
                {
                    Name = "ChildContent",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The content of the pivot item, It can be Any custom tag or a text.",
                },
                new()
                {
                    Name = "Dismissible",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Overrides the Dismissible of the parent pivot for this item alone, so a single tab can gain or lose its dismiss button independently of the rest.",
                },
                new()
                {
                    Name = "Header",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The content of the pivot item header, It can be Any custom tag or a text.",
                },
                new()
                {
                    Name = "HeaderText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The text of the pivot item header, The text displayed of each pivot link.",
                },
                new()
                {
                    Name = "Icon",
                    Type = "BitIconInfo?",
                    DefaultValue = "null",
                    Description = "Gets or sets the icon to display next to the pivot link using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
                    LinkType = LinkType.Link,
                    Href = "#bit-icon-info",
                },
                new()
                {
                    Name = "IconName",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the name of the icon to display next to the pivot link from the built-in Fluent UI icons.",
                    LinkType = LinkType.Link,
                    Href = "https://blazorui.bitplatform.dev/iconography",
                },
                new()
                {
                    Name = "ItemCount",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "Defines an optional item count displayed in parentheses just after the link text.",
                },
                new()
                {
                    Name = "IsSelected",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether or not the item is selected. An item that declares itself selected takes the selection of the pivot when it is registered.",
                },
                new()
                {
                    Name = "Key",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A required key to uniquely identify a pivot item.",
                },
                new()
                {
                    Name = "OnClick",
                    Type = "EventCallback",
                    Description = "Callback for when this pivot item header is clicked or activated from the keyboard.",
                },
                new()
                {
                    Name = "OnDismiss",
                    Type = "EventCallback",
                    Description = "Callback for when the dismiss button of this pivot item is clicked, or the Delete key is pressed while it holds the focus.",
                },
                new()
                {
                    Name = "Reorderable",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Overrides the Reorderable of the parent pivot for this item alone, so a single tab can be pinned in place while the rest of the header is reordered, or the other way around.",
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The title (tooltip) of the pivot item header, which is the usual place for the full text of a header that is too long to be shown in one.",
                }
            ]
        },
        new()
        {
            Id = "pivot-change-args",
            Title = "BitPivotChangeArgs",
            Parameters =
            [
                new()
                {
                    Name = "Item",
                    Type = "BitPivotItem",
                    DefaultValue = "",
                    Description = "The pivot item the selection is about to move to.",
                    LinkType = LinkType.Link,
                    Href = "#pivot-item",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the change and keep the item that is currently selected.",
                }
            ]
        },
        new()
        {
            Id = "pivot-reorder-event-args",
            Title = "BitPivotReorderEventArgs",
            Parameters =
            [
                new()
                {
                    Name = "Item",
                    Type = "BitPivotItem",
                    DefaultValue = "",
                    Description = "The pivot item that is being moved.",
                    LinkType = LinkType.Link,
                    Href = "#pivot-item",
                },
                new()
                {
                    Name = "OldIndex",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The index the item is moving from.",
                },
                new()
                {
                    Name = "NewIndex",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The index the item is moving to.",
                }
            ]
        },
        new()
        {
            Id = "pivot-class-styles",
            Title = "BitPivotClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitPivot."
               },
               new()
               {
                   Name = "Header",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header of the BitPivot."
               },
               new()
               {
                   Name = "HeaderContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header container (wrapper) of the BitPivot."
               },
               new()
               {
                   Name = "SlideButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the slide (next/previous) buttons of the BitPivot in the Slide overflow behavior."
               },
               new()
               {
                   Name = "OverflowButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overflow menu button of the BitPivot in the Menu overflow behavior."
               },
               new()
               {
                   Name = "OverflowCallout",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overflow menu (callout) of the BitPivot in the Menu overflow behavior."
               },
               new()
               {
                   Name = "OverflowItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overflow menu item of the BitPivot in the Menu overflow behavior."
               },
               new()
               {
                   Name = "Body",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the items body of the BitPivot."
               },
               new()
               {
                   Name = "HeaderItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header item of the BitPivot."
               },
               new()
               {
                   Name = "SelectedItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item of the BitPivot."
               },
               new()
               {
                   Name = "HeaderItemContent",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header item content of the BitPivot."
               },
               new()
               {
                   Name = "HeaderIconContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header icon container of the BitPivot."
               },
               new()
               {
                   Name = "HeaderIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header icon of the BitPivot."
               },
               new()
               {
                   Name = "HeaderText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header text of the BitPivot."
               },
               new()
               {
                   Name = "HeaderItemCount",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header item count of the BitPivot."
               },
               new()
               {
                   Name = "HeaderStart",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the start content of the header of the BitPivot."
               },
               new()
               {
                   Name = "HeaderEnd",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the end content of the header of the BitPivot."
               },
               new()
               {
                   Name = "DismissButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dismiss button of the header items of the BitPivot."
               },
               new()
               {
                   Name = "DismissIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dismiss icon of the header items of the BitPivot."
               },
               new()
               {
                   Name = "AddButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the add button of the BitPivot."
               },
               new()
               {
                   Name = "AddIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the add icon of the BitPivot."
               }
            ]
        },
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
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "alignment-enum",
            Name = "BitAlignment",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Value = "0",
                },
                new()
                {
                    Name = "End",
                    Value = "1",
                },
                new()
                {
                    Name = "Center",
                    Value = "2",
                },
                new()
                {
                    Name = "SpaceBetween",
                    Value = "3",
                },
                new()
                {
                    Name = "SpaceAround",
                    Value = "4",
                },
                new()
                {
                    Name = "SpaceEvenly",
                    Value = "5",
                },
                new()
                {
                    Name = "Baseline",
                    Value = "6",
                },
                new()
                {
                    Name = "Stretch",
                    Value = "7",
                }
            ]
        },
        new()
        {
            Id = "header-type-enum",
            Name = "BitPivotHeaderType",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Tab",
                    Description="Renders pivot header items as Tab.",
                    Value="0",
                },
                new()
                {
                    Name= "Link",
                    Description="Renders pivot header items as link.",
                    Value="1",
                },
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
                },
            ]
        },
        new()
        {
            Id = "overflowBehavior-enum",
            Name = "BitPivotOverflowBehavior",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "None",
                    Description="Pivot links will overflow the container and may not be visible.",
                    Value="0",
                },
                new()
                {
                    Name= "Menu",
                    Description="Display an overflow menu that contains the tabs that don't fit.",
                    Value="1",
                },
                new()
                {
                    Name= "Scroll",
                    Description="Display a scroll bar below of the tabs for moving between them.",
                    Value="2",
                },
                new()
                {
                    Name= "Slide",
                    Description="Display next and previous buttons to slide through the tabs that don't fit.",
                    Value="3",
                },
                new()
                {
                    Name= "Wrap",
                    Description="Wrap the tabs that don't fit onto as many extra lines (or columns) as they need.",
                    Value="4",
                },
            ]
        },
        new()
        {
            Id = "pivotPosition-enum",
            Name = "BitSide",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Top",
                    Value = "0",
                    Description = "The top edge."
                },
                new()
                {
                    Name = "Bottom",
                    Value = "1",
                    Description = "The bottom edge."
                },
                new()
                {
                    Name = "Start",
                    Value = "2",
                    Description = "The edge the reading direction starts from - the left in LTR, the right in RTL."
                },
                new()
                {
                    Name = "End",
                    Value = "3",
                    Description = "The edge the reading direction ends at - the right in LTR, the left in RTL."
                },
                new()
                {
                    Name = "Left",
                    Value = "4",
                    Description = "The left edge, in both reading directions."
                },
                new()
                {
                    Name = "Right",
                    Value = "5",
                    Description = "The right edge, in both reading directions."
                },
                new()
                {
                    Name = "TopAndBottom",
                    Value = "6",
                    Description = "Both edges of the block axis at once."
                },
                new()
                {
                    Name = "StartAndEnd",
                    Value = "7",
                    Description = "Both edges of the inline axis at once, following the reading direction the way Start and End do."
                }
            ]
        },
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Primary",
                    Description = "Primary general color.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "Secondary general color.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "Tertiary general color.",
                    Value = "2",
                },
                new()
                {
                    Name = "Info",
                    Description = "Info general color.",
                    Value = "3",
                },
                new()
                {
                    Name = "Success",
                    Description = "Success general color.",
                    Value = "4",
                },
                new()
                {
                    Name = "Warning",
                    Description = "Warning general color.",
                    Value = "5",
                },
                new()
                {
                    Name = "SevereWarning",
                    Description = "SevereWarning general color.",
                    Value = "6",
                },
                new()
                {
                    Name = "Error",
                    Description = "Error general color.",
                    Value = "7",
                },
                new()
                {
                    Name = "PrimaryBackground",
                    Description = "Primary background color.",
                    Value = "8",
                },
                new()
                {
                    Name = "SecondaryBackground",
                    Description = "Secondary background color.",
                    Value = "9",
                },
                new()
                {
                    Name = "TertiaryBackground",
                    Description = "Tertiary background color.",
                    Value = "10",
                },
                new()
                {
                    Name = "PrimaryForeground",
                    Description = "Primary foreground color.",
                    Value = "11",
                },
                new()
                {
                    Name = "SecondaryForeground",
                    Description = "Secondary foreground color.",
                    Value = "12",
                },
                new()
                {
                    Name = "TertiaryForeground",
                    Description = "Tertiary foreground color.",
                    Value = "13",
                },
                new()
                {
                    Name = "PrimaryBorder",
                    Description = "Primary border color.",
                    Value = "14",
                },
                new()
                {
                    Name = "SecondaryBorder",
                    Description = "Secondary border color.",
                    Value = "15",
                },
                new()
                {
                    Name = "TertiaryBorder",
                    Description = "Tertiary border color.",
                    Value = "16",
                },
            ]
        },
    ];




    private string selectedKey = "1";
    private int itemClickCount;
    private int addableTabCount = 3;
    private bool lockHistoryTab = true;
    private string? detachedSelectedKey = "Foo";
    private string? addableSelectedKey = "Tab 1";
    private BitPivotItem? changedPivotItem;
    private BitPivotItem? clickedPivotItem;
    private BitPivotItem? refusedPivotItem;
    private List<string> addableTabs = ["Tab 1", "Tab 2", "Tab 3"];
    private List<string> dismissibleTabs = ["Home", "Documents", "Pictures", "Settings"];
    private List<string> reorderableTabs = ["File", "Shared", "Recent", "Favorites"];

    private void ResetDismissibleTabs() => dismissibleTabs = ["Home", "Documents", "Pictures", "Settings"];

    private void HandleChanging(BitPivotChangeArgs args)
    {
        refusedPivotItem = null;

        if (lockHistoryTab is false || args.Item.HeaderText != "History") return;

        args.Cancel = true;
        refusedPivotItem = args.Item;
    }

    private void AddPivotTab()
    {
        var key = $"Tab {++addableTabCount}";

        addableTabs.Add(key);
        addableSelectedKey = key;
    }

    private void HandleReorder(BitPivotReorderEventArgs args)
    {
        var oldIndex = reorderableTabs.IndexOf(args.Item.Key!);
        var newIndex = args.NewIndex;

        if (oldIndex < 0 || newIndex < 0 || newIndex >= reorderableTabs.Count) return;

        reorderableTabs.RemoveAt(oldIndex);
        reorderableTabs.Insert(newIndex, args.Item.Key!);
    }



    private readonly string example1RazorCode = @"
<BitPivot>
    <BitPivotItem HeaderText=""File"">
        <h3>Pivot #1: File</h3>
        <div>Everything that has been saved to this workspace, newest first.</div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">
        <h3>Pivot #2: Shared with me</h3>
        <div>The files other people have given you access to, grouped by who shared them.</div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h3>Pivot #3: Recent</h3>
        <div>The documents you have opened over the last few days.</div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example2RazorCode = @"
<BitPivot>
    <BitPivotItem HeaderText=""Files"" IconName=""@BitIconName.FabricFolder"">
        <h3>Pivot #1: Files</h3>
        <div>Everything that has been saved to this workspace, newest first.</div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"" ItemCount=""32"">
        <h3>Pivot #2: Shared with me</h3>
        <div>The files other people have given you access to, grouped by who shared them.</div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"" IconName=""@BitIconName.Recent"" ItemCount=""12"">
        <h3>Pivot #3: Recent</h3>
        <div>The documents you have opened over the last few days.</div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example3RazorCode = @"
<BitPivot HeaderType=""@BitPivotHeaderType.Link"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot HeaderType=""@BitPivotHeaderType.Tab"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example4RazorCode = @"
<BitPivot Stacked>
    <BitPivotItem HeaderText=""Home"" IconName=""@BitIconName.Home""><div>Pivot #1: Home</div></BitPivotItem>
    <BitPivotItem HeaderText=""Files"" IconName=""@BitIconName.FabricFolder"" ItemCount=""8""><div>Pivot #2: Files</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent"" IconName=""@BitIconName.Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    <BitPivotItem HeaderText=""Settings"" IconName=""@BitIconName.Settings""><div>Pivot #4: Settings</div></BitPivotItem>
</BitPivot>";

    private readonly string example5RazorCode = @"
<BitPivot FullWidth>
    <BitPivotItem HeaderText=""Overview""><div>Pivot #1: Overview</div></BitPivotItem>
    <BitPivotItem HeaderText=""Activity""><div>Pivot #2: Activity</div></BitPivotItem>
    <BitPivotItem HeaderText=""Settings""><div>Pivot #3: Settings</div></BitPivotItem>
</BitPivot>

<BitPivot FullWidth HeaderType=""@BitPivotHeaderType.Tab"">
    <BitPivotItem HeaderText=""Overview""><div>Pivot #1: Overview</div></BitPivotItem>
    <BitPivotItem HeaderText=""Activity""><div>Pivot #2: Activity</div></BitPivotItem>
    <BitPivotItem HeaderText=""Settings""><div>Pivot #3: Settings</div></BitPivotItem>
</BitPivot>";

    private readonly string example6RazorCode = @"
<BitPivot Alignment=""BitAlignment.Start"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Alignment=""BitAlignment.Center"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Alignment=""BitAlignment.End"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Alignment=""BitAlignment.SpaceBetween"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example7RazorCode = @"
<BitPivot Position=""BitSide.Top"">
    <BitPivotItem HeaderText=""File"">
        <h3>Pivot #1: File</h3>
        <div>Everything that has been saved to this workspace, newest first.</div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared"">
        <h3>Pivot #2: Shared</h3>
        <div>The files other people have given you access to.</div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h3>Pivot #3: Recent</h3>
        <div>The documents you have opened over the last few days.</div>
    </BitPivotItem>
</BitPivot>

<BitPivot Position=""BitSide.Bottom"">
    <BitPivotItem HeaderText=""File"">...</BitPivotItem>
    <BitPivotItem HeaderText=""Shared"">...</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">...</BitPivotItem>
</BitPivot>

<BitPivot Position=""BitSide.Start"">
    <BitPivotItem HeaderText=""File"">...</BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">...</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">...</BitPivotItem>
</BitPivot>

<BitPivot Position=""BitSide.End"">
    <BitPivotItem HeaderText=""File"">...</BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">...</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">...</BitPivotItem>
</BitPivot>";

    private readonly string example8RazorCode = @"
<BitPivot OverflowBehavior=""@BitPivotOverflowBehavior.Menu"">
    <BitPivotItem HeaderText=""File"">Content of the File tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">Content of the Shared with me tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">Content of the Recent tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Favorites"">Content of the Favorites tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Documents"">Content of the Documents tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Pictures"">Content of the Pictures tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Downloads"">Content of the Downloads tab.</BitPivotItem>
</BitPivot>

<BitPivot OverflowBehavior=""@BitPivotOverflowBehavior.Slide"">
    <BitPivotItem HeaderText=""File"">Content of the File tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">Content of the Shared with me tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">Content of the Recent tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Favorites"">Content of the Favorites tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Documents"">Content of the Documents tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Pictures"">Content of the Pictures tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Downloads"">Content of the Downloads tab.</BitPivotItem>
</BitPivot>

<BitPivot OverflowBehavior=""@BitPivotOverflowBehavior.Scroll"">
    <BitPivotItem HeaderText=""File"">Content of the File tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">Content of the Shared with me tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">Content of the Recent tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Favorites"">Content of the Favorites tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Documents"">Content of the Documents tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Pictures"">Content of the Pictures tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Downloads"">Content of the Downloads tab.</BitPivotItem>
</BitPivot>

<BitPivot OverflowBehavior=""@BitPivotOverflowBehavior.Wrap"">
    <BitPivotItem HeaderText=""File"">Content of the File tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">Content of the Shared with me tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">Content of the Recent tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Favorites"">Content of the Favorites tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Documents"">Content of the Documents tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Pictures"">Content of the Pictures tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Downloads"">Content of the Downloads tab.</BitPivotItem>
</BitPivot>

<BitPivot AutoHideSlideButtons OverflowBehavior=""@BitPivotOverflowBehavior.Slide"">
    <BitPivotItem HeaderText=""File"">Content of the File tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Shared"">Content of the Shared tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">Content of the Recent tab.</BitPivotItem>
</BitPivot>

<BitPivot Position=""@BitSide.Start"" OverflowBehavior=""@BitPivotOverflowBehavior.Menu"" Style=""height:200px"">
    <BitPivotItem HeaderText=""File"">Content of the File tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">Content of the Shared with me tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">Content of the Recent tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Favorites"">Content of the Favorites tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Documents"">Content of the Documents tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Pictures"">Content of the Pictures tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Downloads"">Content of the Downloads tab.</BitPivotItem>
</BitPivot>

<BitPivot Position=""@BitSide.Start"" OverflowBehavior=""@BitPivotOverflowBehavior.Slide"" Style=""height:200px"">
    <BitPivotItem HeaderText=""File"">Content of the File tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">Content of the Shared with me tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">Content of the Recent tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Favorites"">Content of the Favorites tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Documents"">Content of the Documents tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Pictures"">Content of the Pictures tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Downloads"">Content of the Downloads tab.</BitPivotItem>
</BitPivot>

<BitPivot Position=""@BitSide.Start"" OverflowBehavior=""@BitPivotOverflowBehavior.Scroll"" Style=""height:200px"">
    <BitPivotItem HeaderText=""File"">Content of the File tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">Content of the Shared with me tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">Content of the Recent tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Favorites"">Content of the Favorites tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Documents"">Content of the Documents tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Pictures"">Content of the Pictures tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Downloads"">Content of the Downloads tab.</BitPivotItem>
</BitPivot>";

    private readonly string example9RazorCode = @"
<BitPivot Alignment=""BitAlignment.Center"">
    <HeaderStart>
        <BitIcon IconName=""@BitIconName.FabricFolder"" />
    </HeaderStart>
    <HeaderEnd>
        <BitButton Variant=""BitVariant.Text"" IconOnly IconName=""@BitIconName.Add"" Title=""New tab"" />
        <BitButton Variant=""BitVariant.Text"" IconOnly IconName=""@BitIconName.Refresh"" Title=""Refresh"" />
    </HeaderEnd>
    <ChildContent>
        <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
        <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
        <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    </ChildContent>
</BitPivot>";

    private readonly string example10RazorCode = @"
<BitPivot Dismissible OnItemDismiss=""@(item => dismissibleTabs.Remove(item.Key!))"">
    @foreach (var tab in dismissibleTabs)
    {
        <BitPivotItem @key=""tab"" Key=""@tab"" HeaderText=""@tab"">
            <div>Content of the @tab tab.</div>
        </BitPivotItem>
    }
</BitPivot>

<BitButton Variant=""BitVariant.Outline"" IsEnabled=""@(dismissibleTabs.Count != 4)"" OnClick=""ResetDismissibleTabs"">
    Reset
</BitButton>

<BitPivot Dismissible>
    <BitPivotItem HeaderText=""Home"" Dismissible=""false""><div>Pivot #1: Home</div></BitPivotItem>
    <BitPivotItem HeaderText=""Documents""><div>Pivot #2: Documents</div></BitPivotItem>
    <BitPivotItem HeaderText=""Pictures""><div>Pivot #3: Pictures</div></BitPivotItem>
</BitPivot>";

    private readonly string example10CsharpCode = @"
private List<string> dismissibleTabs = [""Home"", ""Documents"", ""Pictures"", ""Settings""];

private void ResetDismissibleTabs() => dismissibleTabs = [""Home"", ""Documents"", ""Pictures"", ""Settings""];";

    private readonly string example11RazorCode = @"
<BitPivot SelectOnFocus>
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Navigable=""false"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Loop=""false"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example12RazorCode = @"
<BitPivot @bind-SelectedKey=""selectedKey"">
    <BitPivotItem Key=""1"" HeaderText=""Samples""><div>Pivot #1: Samples</div></BitPivotItem>
    <BitPivotItem Key=""2"" HeaderText=""Files""><div>Pivot #2: Files</div></BitPivotItem>
    <BitPivotItem Key=""3"" HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    <BitPivotItem Key=""4"" HeaderText=""Last""><div>Pivot #4: Last</div></BitPivotItem>
</BitPivot>

<BitButton Variant=""BitVariant.Outline"" IconName=""@BitIconName.CaretSolidLeft"" IsEnabled=""@(selectedKey != ""1"")""
           OnClick=""(() => selectedKey = (int.Parse(selectedKey) - 1).ToString())"">
    Prev
</BitButton>
<BitButton Variant=""BitVariant.Outline"" IconName=""@BitIconName.CaretSolidRight""
           IsEnabled=""@(selectedKey != ""4"")"" OnClick=""(() => selectedKey = (int.Parse(selectedKey) + 1).ToString())"">
    Next
</BitButton>

<div>Selected key: <b>@selectedKey</b></div>";

    private readonly string example12CsharpCode = @"
private string selectedKey = ""1"";";

    private readonly string example13RazorCode = @"
<div style=""border:1px solid gray;padding:10px;"">
    @if (detachedSelectedKey == ""Foo"")
    {
        <div>Hello I am Fooooooooooooo</div>
    }
    else if (detachedSelectedKey == ""Bar"")
    {
        <div>Hello I am Barrrrrrrrrrrr</div>
    }
    else if (detachedSelectedKey == ""Bas"")
    {
        <div>Hello I am Bassssssssssss</div>
    }
    else if (detachedSelectedKey == ""Biz"")
    {
        <div>Hello I am Bizzzzzzzzzzzz</div>
    }
</div>

<BitPivot HeaderOnly=""true"" DefaultSelectedKey=""Foo"" OnItemClick=""@(item => detachedSelectedKey = item?.Key)"">
    <BitPivotItem HeaderText=""Foo"" Key=""Foo""></BitPivotItem>
    <BitPivotItem HeaderText=""Bar"" Key=""Bar""></BitPivotItem>
    <BitPivotItem HeaderText=""Bas"" Key=""Bas""></BitPivotItem>
    <BitPivotItem HeaderText=""Biz"" Key=""Biz""></BitPivotItem>
</BitPivot>";

    private readonly string example13CsharpCode = @"
private string? detachedSelectedKey = ""Foo"";";

    private readonly string example14RazorCode = @"
<BitPivot OnChange=""@(item => changedPivotItem = item)"" OnItemClick=""@(item => clickedPivotItem = item)"">
    <BitPivotItem HeaderText=""Foo"" Title=""The first tab""><div>Pivot #1: Foo</div></BitPivotItem>
    <BitPivotItem HeaderText=""Bar"" Title=""The second tab""><div>Pivot #2: Bar</div></BitPivotItem>
    <BitPivotItem HeaderText=""Bas"" Title=""The third tab""><div>Pivot #3: Bas</div></BitPivotItem>
    <BitPivotItem HeaderText=""Biz"" OnClick=""@(() => itemClickCount++)""><div>Pivot #4: Biz (counts its own clicks)</div></BitPivotItem>
</BitPivot>

<div>Last changed to: <b>@changedPivotItem?.HeaderText</b></div>
<div>Last header clicked: <b>@clickedPivotItem?.HeaderText</b></div>
<div>Clicks on the Biz header: <b>@itemClickCount</b></div>

<BitToggle @bind-Value=""lockHistoryTab"" Label=""Keep the History tab from being selected"" />

<BitPivot OnChanging=""@HandleChanging"">
    <BitPivotItem HeaderText=""Draft""><div>Pivot #1: Draft</div></BitPivotItem>
    <BitPivotItem HeaderText=""Preview""><div>Pivot #2: Preview</div></BitPivotItem>
    <BitPivotItem HeaderText=""History""><div>Pivot #3: History</div></BitPivotItem>
</BitPivot>

<div>Last refused: <b>@refusedPivotItem?.HeaderText</b></div>";

    private readonly string example14CsharpCode = @"
private int itemClickCount;
private bool lockHistoryTab = true;
private BitPivotItem? changedPivotItem;
private BitPivotItem? clickedPivotItem;
private BitPivotItem? refusedPivotItem;

private void HandleChanging(BitPivotChangeArgs args)
{
    refusedPivotItem = null;

    if (lockHistoryTab is false || args.Item.HeaderText != ""History"") return;

    args.Cancel = true;
    refusedPivotItem = args.Item;
}";

    private readonly string example15RazorCode = @"
<BitPivot>
    <BitPivotItem>
        <Header>
            <span style=""color:red"">Header #1</span>
        </Header>
        <Body>
            <h3>Pivot #1</h3>
            <div>The content of the first item, given through the Body template.</div>
        </Body>
    </BitPivotItem>
    <BitPivotItem ItemCount=""99"">
        <Header>
            <i style=""color:green"" class=""bit-icon bit-icon--HeartFill""></i>
            <span style=""color:blue"">Header #2</span>
            <i style=""color:green"" class=""bit-icon bit-icon--HeartFill""></i>
        </Header>
        <Body>
            <h3>Pivot #2</h3>
            <div>A custom header keeps the item count that follows it.</div>
        </Body>
    </BitPivotItem>
    <BitPivotItem IconName=""@BitIconName.Inbox"">
        <Header>
            <span style=""color:rebeccapurple"">
                Header
                <i style=""color:purple"" class=""bit-icon bit-icon--HeartFill""></i> #3
            </span>
        </Header>
        <Body>
            <h3>Pivot #3</h3>
            <div>An icon still comes before a custom header.</div>
        </Body>
    </BitPivotItem>
</BitPivot>";

    private readonly string example16RazorCode = @"
<BitPivot IsEnabled=""false"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot>
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared"" IsEnabled=""false""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot HeaderType=""BitPivotHeaderType.Tab"" IsEnabled=""false"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot HeaderType=""BitPivotHeaderType.Tab"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared"" IsEnabled=""false""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example17RazorCode = @"
<BitPivot Reorderable OnItemReorder=""@HandleReorder"">
    @foreach (var tab in reorderableTabs)
    {
        <BitPivotItem @key=""tab"" Key=""@tab"" HeaderText=""@tab"">
            <div>Content of the @tab tab.</div>
        </BitPivotItem>
    }
    <BitPivotItem Key=""Pinned"" HeaderText=""Pinned"" Reorderable=""false"" IconName=""@BitIconName.Pinned"">
        <div>This tab stays where it is.</div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example17CsharpCode = @"
private List<string> reorderableTabs = [""File"", ""Shared"", ""Recent"", ""Favorites""];

private void HandleReorder(BitPivotReorderEventArgs args)
{
    var oldIndex = reorderableTabs.IndexOf(args.Item.Key!);
    var newIndex = args.NewIndex;

    if (oldIndex < 0 || newIndex < 0 || newIndex >= reorderableTabs.Count) return;

    reorderableTabs.RemoveAt(oldIndex);
    reorderableTabs.Insert(newIndex, args.Item.Key!);
}";

    private readonly string example18RazorCode = @"
<BitPivot>
    <BitPivotItem HeaderText=""First""><input placeholder=""Type here..."" /></BitPivotItem>
    <BitPivotItem HeaderText=""Second""><input placeholder=""Type here..."" /></BitPivotItem>
</BitPivot>

<BitPivot KeepMounted>
    <BitPivotItem HeaderText=""First""><input placeholder=""Type here..."" /></BitPivotItem>
    <BitPivotItem HeaderText=""Second""><input placeholder=""Type here..."" /></BitPivotItem>
</BitPivot>

<BitPivot MountAll>
    <BitPivotItem HeaderText=""First""><input placeholder=""Type here..."" /></BitPivotItem>
    <BitPivotItem HeaderText=""Second""><input placeholder=""Type here..."" /></BitPivotItem>
</BitPivot>";

    private readonly string example19RazorCode = @"
<BitPivot Addable Dismissible
          @bind-SelectedKey=""addableSelectedKey""
          OnAdd=""AddPivotTab""
          OnItemDismiss=""@(item => addableTabs.Remove(item.Key!))"">
    @foreach (var tab in addableTabs)
    {
        <BitPivotItem @key=""tab"" Key=""@tab"" HeaderText=""@tab"">
            <div>Content of the @tab tab.</div>
        </BitPivotItem>
    }
</BitPivot>

<div>Selected key: <b>@addableSelectedKey</b></div>";

    private readonly string example19CsharpCode = @"
private int addableTabCount = 3;
private string? addableSelectedKey = ""Tab 1"";
private List<string> addableTabs = [""Tab 1"", ""Tab 2"", ""Tab 3""];

private void AddPivotTab()
{
    var key = $""Tab {++addableTabCount}"";

    addableTabs.Add(key);
    addableSelectedKey = key;
}";

    private readonly string example20RazorCode = @"
<BitPivot>
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Gap=""0"" HeaderType=""BitPivotHeaderType.Tab"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Gap=""2rem"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example21RazorCode = @"
<BitPivot Color=""BitColor.Primary"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.Secondary"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.Tertiary"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.Info"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.Success"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.Warning"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.SevereWarning"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.Error"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>


<BitPivot Color=""BitColor.PrimaryBackground"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.SecondaryBackground"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.TertiaryBackground"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.PrimaryForeground"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.SecondaryForeground"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.TertiaryForeground"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.PrimaryBorder"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.SecondaryBorder"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.TertiaryBorder"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>


<BitPivot HeaderType=""BitPivotHeaderType.Tab"" Color=""BitColor.Primary"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>


<BitPivot HeaderType=""BitPivotHeaderType.Tab"" Color=""BitColor.Success"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot HeaderType=""BitPivotHeaderType.Tab"" Color=""BitColor.Error"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";


    private readonly string example22RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitPivot>
    <BitPivotItem HeaderText=""Home"" Icon=""@(""fa-solid fa-house"")"">
        <h1>Pivot #1: Home</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Heart"" Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"">
        <h1>Pivot #2: Heart</h1>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Rocket"" Icon=""@BitIconInfo.Fa(""solid rocket"")"">
        <h1>Pivot #3: Rocket</h1>
        <div>
            In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead.
        </div>
    </BitPivotItem>
</BitPivot>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitPivot>
    <BitPivotItem HeaderText=""Home"" Icon=""@(""bi bi-house-fill"")"">
        <h1>Pivot #1: Home</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Heart"" Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"">
        <h1>Pivot #2: Heart</h1>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Gear"" Icon=""@BitIconInfo.Bi(""gear-fill"")"">
        <h1>Pivot #3: Gear</h1>
        <div>
            In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead.
        </div>
    </BitPivotItem>
</BitPivot>";


    private readonly string example23RazorCode = @"
<BitPivot Size=""@BitSize.Small"">
    <BitPivotItem HeaderText=""File"" IconName=""@BitIconName.FabricFolder""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared"" ItemCount=""32""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Size=""@BitSize.Medium"">
    <BitPivotItem HeaderText=""File"" IconName=""@BitIconName.FabricFolder""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared"" ItemCount=""32""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Size=""@BitSize.Large"">
    <BitPivotItem HeaderText=""File"" IconName=""@BitIconName.FabricFolder""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared"" ItemCount=""32""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example24RazorCode = @"
<style>
    .custom-class {
        margin: 1rem;
        padding-left: 0.25rem;
        box-shadow: 0 0 1rem lightskyblue;
    }

    .custom-selected-item {
        background-color: goldenrod;
    }

    .custom-header {
        overflow: hidden;
        border-radius: 1rem;
        border: 1px solid gray;
    }

    .custom-body {
        padding: 0.5rem;
        background-color: deepskyblue;
    }
</style>

<BitPivot Style=""border: 1px solid tomato;"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Class=""custom-class"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Styles=""@(new() { HeaderIcon = ""color: tomato;"", HeaderText = ""color: purple;"", HeaderItemCount = ""color: gray;"" })"">
    <BitPivotItem HeaderText=""File"" IconName=""@BitIconName.FabricFolder""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"" ItemCount=""32""><div>Pivot #2: Shared with me</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Classes=""@(new() { Body = ""custom-body"", SelectedItem = ""custom-selected-item"", Header = ""custom-header"" })"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example25RazorCode = @"
<BitPivot Dir=""BitDir.Rtl"" OverflowBehavior=""@BitPivotOverflowBehavior.Scroll"">
    <BitPivotItem HeaderText=""اسناد"" IconName=""@BitIconName.FabricFolder"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    </BitPivotItem>
    <BitPivotItem HeaderText=""آخرین ها"" ItemCount=""8"">
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد.
    </BitPivotItem>
    <BitPivotItem HeaderText=""شخصی"" IconName=""@BitIconName.Info"" ItemCount=""6"">
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها به پایان رسد.
    </BitPivotItem>
</BitPivot>

<BitPivot Dir=""BitDir.Rtl"" Position=""BitSide.Start"">
    <BitPivotItem HeaderText=""اسناد"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    </BitPivotItem>
    <BitPivotItem HeaderText=""آخرین ها"">
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد.
    </BitPivotItem>
    <BitPivotItem HeaderText=""شخصی"">
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها به پایان رسد.
    </BitPivotItem>
</BitPivot>

<BitPivot Dir=""BitDir.Rtl"" Position=""BitSide.End"">
    <BitPivotItem HeaderText=""اسناد"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    </BitPivotItem>
    <BitPivotItem HeaderText=""آخرین ها"">
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد.
    </BitPivotItem>
    <BitPivotItem HeaderText=""شخصی"">
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها به پایان رسد.
    </BitPivotItem>
</BitPivot>";
}
