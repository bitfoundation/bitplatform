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
            Description = "Determines the alignment of the header section of the pivot (default: Start).",
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
            Description = "The general color of the pivot (default: Primary).",
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
            Description = "The gap between the pivot items of the header (any CSS length), overriding the --bit-Pivot-gap CSS variable.",
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
            Type = "BitPivotHeaderType?",
            DefaultValue = "null",
            Description = "The type of the pivot header items (default: Link).",
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
            Description = "Mounts all tabs at render time and hides the non-selected ones instead of not rendering them (useful for processing or extracting their content).",
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
            Description = "The aria-label of the overflow menu button in the Menu overflow behavior (default: More).",
        },
        new()
        {
            Name = "OverflowBehavior",
            Type = "BitPivotOverflowBehavior?",
            DefaultValue = "null",
            Description = "Overflow behavior when there is not enough room to display all of the links/tabs (default: None).",
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
            Type = "BitPivotPosition?",
            DefaultValue = "null",
            Description = "Position of the pivot header (default: Top).",
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
            Description = "The size of the pivot header items (default: Medium).",
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

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Pivot-color",
            DefaultValue = "the Color's main color",
            Description = "Accent: the Link indicator, the fill of the selected Tab, the drop marker and the More button that holds the selection.",
        },
        new()
        {
            Name = "--bit-Pivot-focus-color",
            DefaultValue = "the Color's focus color",
            Description = "Focus ring of the items, the panel and the header buttons.",
        },
        new()
        {
            Name = "--bit-Pivot-disabled-color",
            DefaultValue = "the Color's disabled color",
            Description = "Indicator and Tab fill of a disabled selection.",
        },
        new()
        {
            Name = "--bit-Pivot-disabled-text-color",
            DefaultValue = "the Color's disabled text color",
            Description = "Text of a disabled item or pivot.",
        },
        new()
        {
            Name = "--bit-Pivot-indicator-color",
            DefaultValue = "--bit-Pivot-color",
            Description = "Selection indicator of a Link pivot (transparent hides it).",
        },
        new()
        {
            Name = "--bit-Pivot-indicator-thickness",
            DefaultValue = "--bit-siz-tab-indicator",
            Description = "Stroke of the selection indicator.",
        },
        new()
        {
            Name = "--bit-Pivot-indicator-inset",
            DefaultValue = "spacing(1)",
            Description = "Inset of the indicator from the item's edges (0 spans the whole item).",
        },
        new()
        {
            Name = "--bit-Pivot-indicator-radius",
            DefaultValue = "--bit-shp-radius-tab-indicator",
            Description = "Corner radius of the indicator.",
        },
        new()
        {
            Name = "--bit-Pivot-item-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text of an item at rest.",
        },
        new()
        {
            Name = "--bit-Pivot-item-hover-color",
            DefaultValue = "--bit-clr-fg-pri-hover (Tab: the Color's on-color)",
            Description = "Text of a hovered item.",
        },
        new()
        {
            Name = "--bit-Pivot-item-hover-background",
            DefaultValue = "--bit-clr-bg-pri-hover (Tab: the Color's hover color)",
            Description = "Background of a hovered item.",
        },
        new()
        {
            Name = "--bit-Pivot-item-selected-color",
            DefaultValue = "--bit-clr-fg-pri (Tab: the Color's on-color)",
            Description = "Text of the selected item.",
        },
        new()
        {
            Name = "--bit-Pivot-item-selected-background",
            DefaultValue = "transparent (Tab: --bit-Pivot-color)",
            Description = "Background of the selected item.",
        },
        new()
        {
            Name = "--bit-Pivot-item-selected-font-weight",
            DefaultValue = "--bit-tpg-fw-semibold",
            Description = "Weight of the selected item's text; its room is reserved, so the tabs never shift.",
        },
        new()
        {
            Name = "--bit-Pivot-item-height",
            DefaultValue = "--bit-siz-tab",
            Description = "Height of an item and of the slide and add buttons.",
        },
        new()
        {
            Name = "--bit-Pivot-item-padding-inline",
            DefaultValue = "spacing(1)",
            Description = "Inline padding of an item.",
        },
        new()
        {
            Name = "--bit-Pivot-item-radius",
            DefaultValue = "0 (Tab: --bit-shp-radius-control)",
            Description = "Corner radius of an item and its focus ring.",
        },
        new()
        {
            Name = "--bit-Pivot-font-size",
            DefaultValue = "per Size, from the type ramp",
            Description = "Text size of the items and the overflow menu.",
        },
        new()
        {
            Name = "--bit-Pivot-gap",
            DefaultValue = "spacing(1); spacing(0.5) vertical or wrapped; 0 FullWidth",
            Description = "Room between the items. The Gap parameter overrides it.",
        },
        new()
        {
            Name = "--bit-Pivot-icon-size",
            DefaultValue = "inherited",
            Description = "Size of an item's icon.",
        },
        new()
        {
            Name = "--bit-Pivot-dismiss-size",
            DefaultValue = "spacing(3)",
            Description = "Box of the dismiss button: its pointer target, 24px by default for WCAG 2.2 SC 2.5.8.",
        },
        new()
        {
            Name = "--bit-Pivot-body-padding",
            DefaultValue = "0",
            Description = "Padding of the tab panel.",
        },
        new()
        {
            Name = "--bit-Pivot-callout-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of the overflow menu.",
        },
        new()
        {
            Name = "--bit-Pivot-callout-shadow",
            DefaultValue = "the popup elevation token",
            Description = "Elevation of the overflow menu.",
        },
        new()
        {
            Name = "--bit-Pivot-callout-radius",
            DefaultValue = "--bit-shp-radius-popup",
            Description = "Corner radius of the overflow menu.",
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
            Name = "BitPivotPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Top",
                    Description="Display header at the top.",
                    Value="0",
                },
                new()
                {
                    Name= "Bottom",
                    Description="Display header at the Bottom.",
                    Value="1",
                },
                new()
                {
                    Name= "Start",
                    Description="Display header at the start (Left for LTR and Right for RTL).",
                    Value="2",
                },
                new()
                {
                    Name= "End",
                    Description="Display header at the end (Right for LTR and Left for RTL).",
                    Value="3",
                },
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



    private readonly List<string> overflowTabs = ["File", "Shared with me", "Recent", "Favorites", "Documents", "Pictures", "Downloads"];

    private string selectedKey = "1";

    private BitPivotItem? changedPivotItem;
    private BitPivotItem? clickedPivotItem;
    private int itemClickCount;
    private bool lockHistoryTab = true;
    private BitPivotItem? refusedPivotItem;

    private string? detachedSelectedKey = "Foo";

    private int editableTabCount = 2;
    private string? editableSelectedKey = "Home";
    private readonly List<string> editableTabs = ["Tab 1", "Tab 2"];

    private readonly List<string> reorderableTabs = ["File", "Shared", "Recent", "Favorites"];

    private readonly BitPivotParams[] pivotParams =
    [
        new()
        {
            HeaderType = BitPivotHeaderType.Tab,
            Color = BitColor.Success,
            Size = BitSize.Small,
        }
    ];

    private void HandleChanging(BitPivotChangeArgs args)
    {
        refusedPivotItem = null;

        if (lockHistoryTab is false || args.Item.HeaderText != "History") return;

        args.Cancel = true;
        refusedPivotItem = args.Item;
    }

    private void AddTab()
    {
        var key = $"Tab {++editableTabCount}";

        editableTabs.Add(key);
        editableSelectedKey = key;
    }

    private void HandleReorder(BitPivotReorderEventArgs args)
    {
        var oldIndex = reorderableTabs.IndexOf(args.Item.Key!);
        var newIndex = args.NewIndex;

        if (oldIndex < 0 || newIndex < 0 || newIndex >= reorderableTabs.Count) return;

        reorderableTabs.RemoveAt(oldIndex);
        reorderableTabs.Insert(newIndex, args.Item.Key!);
    }
}
