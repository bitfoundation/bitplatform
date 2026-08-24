namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.SnackBar;

public partial class BitSnackBarDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ActionsTemplate",
            Type = "RenderFragment<BitSnackBarItem>?",
            DefaultValue = "null",
            Description = "The content of the action area of every snack bar item, rendered under its body. An item that carries its own Actions renders that instead.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "AutoDismiss",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not automatically dismiss the snack bar. The countdown of an item is paused while the pointer or the keyboard focus is inside it, and a persistent item never takes part in it at all.",
        },
        new()
        {
            Name = "AutoDismissTime",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "How long does it take to automatically dismiss the snack bar (default is 3 seconds). A single item can ask for a lifetime of its own through BitSnackBarItem.AutoDismissTime, and a value of zero or less turns the countdown off.",
        },
        new()
        {
            Name = "BodyTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "Used to customize how the content inside the body is rendered.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitSnackBarClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the snack bar.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "ClearOnNavigation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Closes every snack bar item as soon as the app navigates somewhere else, so a notification about the page it was raised on does not outlive that page.",
        },
        new()
        {
            Name = "DismissAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible label of the dismiss button (default is \"Close\"). The button holds nothing but an icon, so without a label of its own a screen reader has nothing to announce it by.",
        },
        new()
        {
            Name = "DismissIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon of the dismiss button using custom CSS classes for external icon libraries. Takes precedence over DismissIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "DismissIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the dismiss button from the built-in Fluent UI icons. If unset, default will be the Fluent UI Cancel icon.",
        },
        new()
        {
            Name = "DismissOnClick",
            Type = "bool",
            DefaultValue = "false",
            Description = "Dismisses a snack bar item when anywhere inside it is clicked. Leave this off while the item holds interactive content the user is still filling in.",
        },
        new()
        {
            Name = "HideDismiss",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents rendering the dismiss button of every snack bar item. Unlike Persistent this only takes the button away: the items still count down, still answer the Escape key and still take part in DismissOnClick.",
        },
        new()
        {
            Name = "HideProgress",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents rendering the countdown progress bar of the auto-dismissing snack bars.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The leading icon of every snack bar item using custom CSS classes for external icon libraries. Only rendered while ShowIcon is enabled, and takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the leading icon of every snack bar item from the built-in Fluent UI icons. Only rendered while ShowIcon is enabled. If unset, the icon of each item is selected automatically based on its color.",
        },
        new()
        {
            Name = "MaxItems",
            Type = "int?",
            DefaultValue = "null",
            Description = "The maximum number of snack bar items to show at once, so a burst of notifications cannot grow into a wall that covers the page. What happens to the item that does not fit is up to OverflowBehavior. Unset (or zero and below) means no cap.",
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum width of the snack bar items. Any CSS length is accepted, and the stack never grows past the width of the screen whatever this says. Unset, an item is as wide as its longest line needs.",
        },
        new()
        {
            Name = "Multiline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the multiline mode of both title and body. A single-line title or body that does not fit is cut off with an ellipsis and keeps its full text in a tooltip.",
        },
        new()
        {
            Name = "NewestOnTop",
            Type = "bool",
            DefaultValue = "false",
            Description = "Puts the newest snack bar item at the top of the stack instead of the bottom.",
        },
        new()
        {
            Name = "Offset",
            Type = "string?",
            DefaultValue = "null",
            Description = "The distance of the stack from the edges of the screen (default is 8px). Any CSS length is accepted, which is how a snack bar is kept clear of the chrome the app already has at that edge.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<BitSnackBarItem>",
            Description = "Callback for when any snack bar is dismissed, reporting the item that was dismissed. Its DismissReason tells what took it away.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitSnackBarItem>",
            Description = "Callback for when any snack bar item is clicked.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "OnShow",
            Type = "EventCallback<BitSnackBarItem>",
            Description = "Callback for when a new snack bar item is shown. An item held back by the Queue overflow behavior reports this when it reaches the screen rather than when it was handed to Show.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "OverflowBehavior",
            Type = "BitSnackBarOverflowBehavior",
            DefaultValue = "BitSnackBarOverflowBehavior.DismissOldest",
            Description = "What happens to a new snack bar item that arrives while MaxItems is already reached: the oldest one is dismissed to make room for it, the new one is held back until a slot frees up, or the new one is dropped.",
            LinkType = LinkType.Link,
            Href = "#snackbar-overflow-behavior-enum",
        },
        new()
        {
            Name = "PauseOnHover",
            Type = "bool",
            DefaultValue = "true",
            Description = "Pauses the auto-dismiss countdown while the pointer or the keyboard focus is inside a snack bar item. This is how the countdown meets WCAG 2.2.1 (Timing Adjustable) without a longer timeout.",
        },
        new()
        {
            Name = "PauseOnPageHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "Pauses the auto-dismiss countdown of every snack bar item while the page is hidden, so a notification is not spent in a background tab. Needs the bit BlazorUI services to be registered (AddBitBlazorUIServices).",
        },
        new()
        {
            Name = "Persistent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the snack bar non-dismissible in UI and removes the dismiss button. A persistent snack bar also opts out of the auto-dismiss countdown and of the Escape key. To take the button away without also taking the countdown away, use HideDismiss instead.",
        },
        new()
        {
            Name = "Position",
            Type = "BitSnackBarPosition?",
            DefaultValue = "null",
            Description = "The position of the snack bars to show (default is bottom right).",
            LinkType = LinkType.Link,
            Href = "#snackbar-position-enum"
        },
        new()
        {
            Name = "PreventDuplicates",
            Type = "bool",
            DefaultValue = "false",
            Description = "Skips showing a new snack bar while an identical one (same title, body and color) is already on screen, returning the one that is already showing instead and starting its countdown over.",
        },
        new()
        {
            Name = "ReverseProgress",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the countdown progress bar depleting from full to empty instead of filling from empty to full, so it reads as the time the notification has left rather than as how far it has got through its lifetime.",
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "A custom ARIA role for every snack bar item, overriding the one its color implies. By default the colors that report a problem are announced as an alert and the rest as a status, and a role that is not a live one leaves the item unannounced.",
        },
        new()
        {
            Name = "ShowIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a leading icon in each snack bar item, chosen from its color unless one is provided.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the snack bar items.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitSnackBarClassStyles?",
            Description = "Custom CSS styles for different parts of the snack bar.",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Template",
            Type = "RenderFragment<BitSnackBarItem>?",
            DefaultValue = "null",
            Description = "Used to fully customize how a snack bar item is rendered, taking the place of its header, body and actions. The countdown progress bar is still rendered under the template.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "TitleTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the title is rendered.",
        },
        new()
        {
            Name = "TransitionDuration",
            Type = "int",
            DefaultValue = "200",
            Description = "The duration in milliseconds of the enter and exit animations of the snack bar items. A dismissed item is kept in the DOM for this long so its exit animation can play. Set it to zero to remove the item at once.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the snack bar items.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "snackbar-position-enum",
            Name = "BitSnackBarPosition",
            Description = "Determines the corner or edge of the screen the snack bars are stacked at. The start/end naming follows the text direction.",
            Items =
            [
                new()
                {
                    Name = "TopStart",
                    Description = "Top of the screen, at the inline start.",
                    Value = "0",
                },
                new()
                {
                    Name = "TopCenter",
                    Description = "Top of the screen, centered.",
                    Value = "1",
                },
                new()
                {
                    Name = "TopEnd",
                    Description = "Top of the screen, at the inline end.",
                    Value = "2",
                },
                new()
                {
                    Name = "BottomStart",
                    Description = "Bottom of the screen, at the inline start.",
                    Value = "3",
                },
                new()
                {
                    Name = "BottomCenter",
                    Description = "Bottom of the screen, centered.",
                    Value = "4",
                },
                new()
                {
                    Name = "BottomEnd",
                    Description = "Bottom of the screen, at the inline end (the default).",
                    Value = "5",
                },
            ]
        },
        new()
        {
            Id = "snackbar-overflow-behavior-enum",
            Name = "BitSnackBarOverflowBehavior",
            Description = "Determines what happens to a new snack bar item that arrives while MaxItems is already reached.",
            Items =
            [
                new()
                {
                    Name = "DismissOldest",
                    Description = "Dismisses the oldest item on screen to make room for the new one. This is the default.",
                    Value = "0",
                },
                new()
                {
                    Name = "Queue",
                    Description = "Holds the new item back until a slot frees up, then shows it in the order it arrived.",
                    Value = "1",
                },
                new()
                {
                    Name = "Skip",
                    Description = "Drops the new item, leaving what is already on screen untouched.",
                    Value = "2",
                },
            ]
        },
        new()
        {
            Id = "snackbar-dismiss-reason-enum",
            Name = "BitSnackBarDismissReason",
            Description = "Tells what took a snack bar item off the screen, reported through BitSnackBarItem.DismissReason.",
            Items =
            [
                new()
                {
                    Name = "Programmatic",
                    Description = "The code that opened the item closed it through Close.",
                    Value = "0",
                },
                new()
                {
                    Name = "DismissButton",
                    Description = "The user pressed the dismiss button of the item.",
                    Value = "1",
                },
                new()
                {
                    Name = "Escape",
                    Description = "The user pressed the Escape key while the focus was inside the item.",
                    Value = "2",
                },
                new()
                {
                    Name = "Click",
                    Description = "The user clicked the item while DismissOnClick was enabled.",
                    Value = "3",
                },
                new()
                {
                    Name = "Timeout",
                    Description = "The auto-dismiss countdown of the item ran out.",
                    Value = "4",
                },
                new()
                {
                    Name = "Overflow",
                    Description = "The item was taken away to make room for a newer one under MaxItems.",
                    Value = "5",
                },
                new()
                {
                    Name = "Clear",
                    Description = "The host was emptied through Clear.",
                    Value = "6",
                },
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
                    Name = "Fill",
                    Description = "Fill styled variant.",
                    Value = "0",
                },
                new()
                {
                    Name = "Outline",
                    Description = "Outline styled variant.",
                    Value = "1",
                },
                new()
                {
                    Name = "Text",
                    Description = "Text styled variant.",
                    Value = "2",
                }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Determines the size of the snack bar items.",
            Items =
            [
                new()
                {
                    Name = "Small",
                    Description = "The small size snack bar.",
                    Value = "0",
                },
                new()
                {
                    Name = "Medium",
                    Description = "The medium size snack bar (the default).",
                    Value = "1",
                },
                new()
                {
                    Name = "Large",
                    Description = "The large size snack bar.",
                    Value = "2",
                }
            ]
        }
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
            Id = "class-styles",
            Title = "BitSnackBarClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitSnackBar."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main container of the BitSnackBar."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitSnackBar."
                },
                new()
                {
                    Name = "IconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon container of the BitSnackBar."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the leading icon of the BitSnackBar."
                },
                new()
                {
                    Name = "DismissButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the dismiss button of the BitSnackBar."
                },
                new()
                {
                    Name = "DismissIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the dismiss icon of the BitSnackBar."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitSnackBar."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of the BitSnackBar."
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the action area of the BitSnackBar."
                },
                new()
                {
                    Name = "ProgressBar",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the progress bar of the BitSnackBar."
                }
            ]
        },
        new()
        {
            Id = "snackbar-item",
            Title = "BitSnackBarItem",
            Description = "A class to represent each snack bar item. It is handed to Show and stays the handle of the snack bar it opened, which Close, Update, Pause and Resume take and which OnDismiss reports back. Every member overrides the matching parameter of the host for this one item only.",
            Parameters =
            [
                new()
                {
                    Name = "Id",
                    Type = "Guid",
                    DefaultValue = "Guid.NewGuid()",
                    Description = "The unique identifier of the snack bar item."
                },
                new()
                {
                    Name = "Actions",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The content of the action area of this snack bar item, rendered under its body, taking the place of the host's ActionsTemplate."
                },
                new()
                {
                    Name = "AnnounceText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "What a screen reader is told when this item arrives, instead of its title and body. An empty string leaves the item unannounced.",
                },
                new()
                {
                    Name = "AutoDismissTime",
                    Type = "TimeSpan?",
                    DefaultValue = "null",
                    Description = "How long it takes to automatically dismiss this specific snack bar item, overriding the AutoDismissTime of the host."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The body text of the snack bar item."
                },
                new()
                {
                    Name = "Color",
                    Type = "BitColor?",
                    DefaultValue = "null",
                    Description = "The color theme of the snack bar item, which also decides its default icon and the politeness of its live region.",
                    LinkType = LinkType.Link,
                    Href = "#color-enum",
                },
                new()
                {
                    Name = "CssClass",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS class to apply to the snack bar item."
                },
                new()
                {
                    Name = "CssStyle",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS style to apply to the snack bar item."
                },
                new()
                {
                    Name = "Data",
                    Type = "object?",
                    DefaultValue = "null",
                    Description = "An arbitrary payload to carry along with the snack bar item. Nothing in the component reads it; it is a place to keep whatever the callbacks of the item need."
                },
                new()
                {
                    Name = "DuplicateCount",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "How many further times this notification was raised while it was already on screen, counted by PreventDuplicates. Zero for an item that was raised once, and reset each time the item is shown afresh.",
                },
                new()
                {
                    Name = "DismissReason",
                    Type = "BitSnackBarDismissReason?",
                    DefaultValue = "null",
                    Description = "What took this item off the screen, set by the host just before the dismiss callbacks run. Null until the item has been dismissed.",
                    LinkType = LinkType.Link,
                    Href = "#snackbar-dismiss-reason-enum",
                },
                new()
                {
                    Name = "HideDismiss",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Prevents rendering the dismiss button of this specific item, without taking its countdown, its answer to Escape or its part in DismissOnClick away.",
                },
                new()
                {
                    Name = "HideIcon",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Prevents rendering the leading icon of this specific snack bar item."
                },
                new()
                {
                    Name = "Icon",
                    Type = "BitIconInfo?",
                    DefaultValue = "null",
                    Description = "The leading icon of this snack bar item using custom CSS classes for external icon libraries.",
                    LinkType = LinkType.Link,
                    Href = "#bit-icon-info",
                },
                new()
                {
                    Name = "IconName",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The name of the leading icon of this snack bar item from the built-in Fluent UI icons."
                },
                new()
                {
                    Name = "OnClick",
                    Type = "Func<BitSnackBarItem, Task>?",
                    DefaultValue = "null",
                    Description = "A callback that is invoked when this snack bar item is clicked, before the host's OnItemClick."
                },
                new()
                {
                    Name = "OnDismiss",
                    Type = "Func<BitSnackBarItem, Task>?",
                    DefaultValue = "null",
                    Description = "A callback that is invoked when this snack bar item is dismissed, before the host's OnDismiss."
                },
                new()
                {
                    Name = "Persistent",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Makes this specific snack bar item non-dismissible and removes its dismiss button, which also opts it out of the auto-dismiss countdown."
                },
                new()
                {
                    Name = "Role",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A custom ARIA role for this snack bar item, overriding the one its color implies."
                },
                new()
                {
                    Name = "Title",
                    Type = "string",
                    DefaultValue = "null",
                    Description = "The title text of the snack bar item."
                },
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Items",
            Type = "IReadOnlyList<BitSnackBarItem>",
            DefaultValue = "[]",
            Description = "The snack bar items that are currently showing, oldest first (or newest first with NewestOnTop).",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "PendingItems",
            Type = "IReadOnlyList<BitSnackBarItem>",
            DefaultValue = "[]",
            Description = "The snack bar items that are waiting for a slot under the Queue overflow behavior, in the order they will be shown.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Info",
            Type = "Task<BitSnackBarItem> Info(string title, string? body = \"\", bool persistent = false, TimeSpan? autoDismissTime = null)",
            Description = "Shows a new snackbar with Info color.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Success",
            Type = "Task<BitSnackBarItem> Success(string title, string? body = \"\", bool persistent = false, TimeSpan? autoDismissTime = null)",
            Description = "Shows a new snackbar with Success color.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Warning",
            Type = "Task<BitSnackBarItem> Warning(string title, string? body = \"\", bool persistent = false, TimeSpan? autoDismissTime = null)",
            Description = "Shows a new snackbar with Warning color.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "SevereWarning",
            Type = "Task<BitSnackBarItem> SevereWarning(string title, string? body = \"\", bool persistent = false, TimeSpan? autoDismissTime = null)",
            Description = "Shows a new snackbar with SevereWarning color.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Error",
            Type = "Task<BitSnackBarItem> Error(string title, string? body = \"\", bool persistent = false, TimeSpan? autoDismissTime = null)",
            Description = "Shows a new snackbar with Error color.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitSnackBarItem> Show(string title, string? body = \"\", BitColor color = BitColor.Info, string? cssClass = null, string? cssStyle = null, bool persistent = false, TimeSpan? autoDismissTime = null)",
            Description = "Shows a new snackbar.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitSnackBarItem> Show(BitSnackBarItem item)",
            Description = "Shows a new snackbar. Showing an item that is already showing (or already waiting in the queue) is a no-op that returns it unchanged, and so is showing a duplicate of one while PreventDuplicates is enabled - in which case the item that is already showing comes back instead, with its countdown started over. An item that does not fit under MaxItems is dealt with by OverflowBehavior.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Close",
            Type = "Task Close(BitSnackBarItem item)",
            Description = "Closes a snackbar item. The returned task completes once the item has left the DOM, which is after its exit animation has played. An item that is still waiting in the queue is taken out of it instead.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Clear",
            Type = "Task Clear()",
            Description = "Closes every snackbar item that is currently showing, and drops everything that was waiting in the queue.",
        },
        new()
        {
            Name = "Update",
            Type = "Task Update(BitSnackBarItem item)",
            Description = "Re-renders a snackbar item after its properties were changed, restarts its auto-dismiss countdown and announces its new text again. This is how a notification is turned into the report of what it was waiting for.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Pause",
            Type = "Task Pause(BitSnackBarItem item)",
            Description = "Pauses the auto-dismiss countdown of a snackbar item. This is a hold of its own rather than the same one the pointer takes, so it is not let go again by the pointer happening to leave the item - only Resume releases it.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Resume",
            Type = "Task Resume(BitSnackBarItem item)",
            Description = "Resumes the auto-dismiss countdown of a snackbar item that was paused. A countdown is held back for as long as any one reason to hold it back stands, so this does nothing while the pointer or the keyboard focus is still inside the item or the page is still hidden.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
    ];



    private BitSnackBar basicRef = default!;
    private async Task OpenBasicSnackBar()
    {
        await basicRef.Info("This is title", "This is body");
    }


    private string offset = "8px";
    private BitSnackBar positionRef = default!;
    private BitSnackBarPosition position = BitSnackBarPosition.BottomEnd;
    private async Task OpenPositionSnackBar()
    {
        await positionRef.Info($"{position}", $"Pinned to the selected position, {offset} from the edges.");
    }


    private BitSnackBar autoDismissRef = default!;
    private BitSnackBar noProgressRef = default!;
    private BitSnackBar perItemTimeRef = default!;
    private BitSnackBar reverseProgressRef = default!;

    private async Task OpenAutoDismiss()
    {
        await autoDismissRef.Info("Dismissing in 5 seconds", "Hover over me and the countdown holds.");
    }

    private async Task OpenReverseProgress()
    {
        await reverseProgressRef.Info("Dismissing in 5 seconds", "The bar drains as the time runs out.");
    }

    private async Task OpenNoProgress()
    {
        await noProgressRef.Info("Dismissing in 5 seconds", "The countdown runs without a progress bar.");
    }

    private async Task OpenPerItemTime()
    {
        await perItemTimeRef.Show("Quick one", "This item lives for 2 seconds.", autoDismissTime: TimeSpan.FromSeconds(2));
        await perItemTimeRef.Show("Slow one", "This item takes the host's 10 seconds.", BitColor.Success);
    }


    private BitSnackBarItem? persistentItem;
    private BitSnackBar persistentRef = default!;
    private BitSnackBar hideDismissRef = default!;
    private BitSnackBar perItemPersistentRef = default!;

    private async Task OpenPersistentSnackBar()
    {
        await ClosePersistentSnackBar();

        persistentItem = await persistentRef.Info("This is persistent title", "This is persistent body");
    }

    private async Task ClosePersistentSnackBar()
    {
        if (persistentItem is not null)
        {
            await persistentRef.Close(persistentItem);
            persistentItem = null;
        }
    }

    private async Task OpenMixedPersistence()
    {
        await perItemPersistentRef.Info("Goes away", "This one is dismissed after 3 seconds.");
        await perItemPersistentRef.Show(new BitSnackBarItem
        {
            Title = "Stays put",
            Body = "This one is persistent, so it has no dismiss button and no countdown.",
            Color = BitColor.Warning,
            Persistent = true
        });
    }


    private async Task OpenHideDismiss()
    {
        await hideDismissRef.Info("No way out but the clock", "This item has no dismiss button, but it still counts down.");
    }


    private int stackingCounter;
    private bool newestOnTop;
    private bool preventDuplicates;
    private BitSnackBar stackingRef = default!;
    private BitSnackBarItem? duplicateItem;
    private BitSnackBarOverflowBehavior overflowBehavior;

    // The counts below are the host's own state, so this page only keeps up with them because the snack bar
    // reports what it did.
    private void HandleStackingChange(BitSnackBarItem item) => StateHasChanged();

    private async Task OpenStacking()
    {
        stackingCounter++;
        await stackingRef.Info($"Notification {stackingCounter}", "Only three of these fit at a time.");
    }

    private async Task OpenDuplicate()
    {
        duplicateItem = await stackingRef.Info("Duplicate", "Showing this twice only adds one while PreventDuplicates is on.");
    }


    private BitSnackBar iconRef = default!;
    private BitSnackBar customIconRef = default!;
    private BitSnackBar perItemIconRef = default!;

    private async Task OpenIconInfo() => await iconRef.Info("Info", "The icon follows the color of the item.");

    private async Task OpenIconSuccess() => await iconRef.Success("Success", "The icon follows the color of the item.");

    private async Task OpenIconError() => await iconRef.Error("Error", "The icon follows the color of the item.");

    private async Task OpenCustomIcon()
    {
        await customIconRef.Info("Reminder", "Every item of this host uses the Ringer icon.");
    }

    private async Task OpenPerItemIcon()
    {
        await perItemIconRef.Show(new BitSnackBarItem
        {
            Title = "Deployed",
            Body = "This one item asked for the Rocket icon.",
            Color = BitColor.Success,
            IconName = BitIconName.Rocket
        });
        await perItemIconRef.Show(new BitSnackBarItem
        {
            Title = "No icon",
            Body = "And this one dropped its icon.",
            Color = BitColor.Info,
            HideIcon = true
        });
    }


    private string actionResult = "-";
    private BitSnackBar actionsRef = default!;

    private async Task OpenActions()
    {
        actionResult = "-";
        await actionsRef.Warning("Item deleted", "The item was moved to the recycle bin.");
    }

    private async Task Undo(BitSnackBarItem item)
    {
        actionResult = $"Undone: {item.Title}";
        await actionsRef.Close(item);
    }


    private BitSnackBar singleLineRef = default!;
    private BitSnackBar multilineRef = default!;
    private BitSnackBar maxWidthRef = default!;

    private const string LongBody = "This body is long enough that it does not fit on a single line, so it is either cut off with an ellipsis or wrapped over as many lines as it needs.";

    private async Task OpenSingleLine() => await singleLineRef.Info("A title that is also too long to fit on one line", LongBody);

    private async Task OpenMultiline() => await multilineRef.Info("A title that is also too long to fit on one line", LongBody);

    private async Task OpenMaxWidth() => await maxWidthRef.Info("A title that is also too long to fit on one line", LongBody);


    private string? bodyTemplateAnswer;
    private BitSnackBar bodyTemplateRef = default!;
    private BitSnackBar titleTemplateRef = default!;
    private BitSnackBar fullTemplateRef = default!;

    private async Task OpenTitleTemplate()
    {
        await titleTemplateRef.Warning("This is title", "This is body");
    }

    private async Task OpenBodyTemplate()
    {
        bodyTemplateAnswer = null;
        await bodyTemplateRef.Error("This is title", "This is body");
    }

    private async Task OpenFullTemplate()
    {
        await fullTemplateRef.Show("Alice Johnson", "sent you a message", BitColor.Primary);
    }


    private BitSnackBar eventsRef = default!;
    private readonly List<string> eventLogs = [];

    private void Log(string message)
    {
        eventLogs.Insert(0, message);
        if (eventLogs.Count > 5) eventLogs.RemoveAt(eventLogs.Count - 1);
    }

    private void HandleOnShow(BitSnackBarItem item) => Log($"OnShow: {item.Title}");

    private void HandleOnDismiss(BitSnackBarItem item) => Log($"OnDismiss: {item.Title} ({item.DismissReason})");

    private void HandleOnItemClick(BitSnackBarItem item) => Log($"OnItemClick: {item.Title}");

    private async Task OpenEvents()
    {
        await eventsRef.Info($"Notification {eventLogs.Count + 1}", "Click me, close me or wait - the reason is reported.");
    }


    private BitSnackBarItem? uploadItem;
    private BitSnackBar controlRef = default!;

    // A countdown that dismisses an item re-renders the snack bar, not this page, so the count below only keeps up
    // with it because the snack bar reports what it did.
    private void HandleControlChange(BitSnackBarItem item) => StateHasChanged();

    private async Task StartUpload()
    {
        uploadItem = await controlRef.Show(new BitSnackBarItem
        {
            Title = "Uploading...",
            Body = "report.pdf",
            Color = BitColor.Info,
            Persistent = true
        });
    }

    private async Task CompleteUpload()
    {
        if (uploadItem is null) return;

        uploadItem.Title = "Upload complete";
        uploadItem.Color = BitColor.Success;
        uploadItem.Persistent = false;

        await controlRef.Update(uploadItem);

        uploadItem = null;
    }

    private async Task PauseAll()
    {
        foreach (var item in controlRef.Items.ToArray())
        {
            await controlRef.Pause(item);
        }
    }

    private async Task ResumeAll()
    {
        foreach (var item in controlRef.Items.ToArray())
        {
            await controlRef.Resume(item);
        }
    }

    private async Task ClearAll() => await controlRef.Clear();


    private BitSnackBar a11yRef = default!;

    private async Task OpenPoliteA11y()
    {
        await a11yRef.Success("Saved", "A screen reader hears this at the next pause in what it is saying.");
    }

    private async Task OpenAssertiveA11y()
    {
        await a11yRef.Error("Save failed", "A problem interrupts the screen reader instead of waiting.");
    }

    private async Task OpenAnnounceText()
    {
        await a11yRef.Show(new BitSnackBarItem
        {
            Title = "ETA 5m",
            Body = "Sync in progress.",
            Color = BitColor.Info,
            AnnounceText = "Estimated time of arrival: five minutes. Sync in progress."
        });
    }

    private async Task OpenSilentA11y()
    {
        await a11yRef.Show(new BitSnackBarItem
        {
            Title = "Seen but not heard",
            Body = "A role that is not a live one leaves the item unannounced.",
            Color = BitColor.Warning,
            Role = "presentation"
        });
    }


    private BitDir direction;
    private bool customShowIcon;
    private bool basicSnackBarMultiline;
    private bool basicSnackBarAutoDismiss;
    private int basicSnackBarDismissSeconds = 3;
    private int customTransitionDuration = 200;
    private BitSnackBar customizationRef = default!;
    private BitSize customSize = BitSize.Medium;
    private BitVariant customVariant = BitVariant.Fill;
    private string basicSnackBarBody = "This is body";
    private string basicSnackBarTitle = "This is title";
    private BitColor basicSnackBarColor = BitColor.Info;
    private BitSnackBarPosition basicSnackBarPosition = BitSnackBarPosition.BottomEnd;

    private async Task OpenCustomizationSnackBar()
    {
        await customizationRef.Show(basicSnackBarTitle, basicSnackBarBody, basicSnackBarColor);
    }


    private BitSnackBar colorRef = default!;
    private BitVariant colorVariant = BitVariant.Fill;


    private BitSnackBar dismissIconFaRef = default!;
    private BitSnackBar dismissIconCssRef = default!;
    private BitSnackBar leadingIconFaRef = default!;
    private BitSnackBar dismissIconBiRef = default!;
    private BitSnackBar dismissIconImplicitRef = default!;

    private async Task OpenDismissIconFa()
    {
        await dismissIconFaRef.Info("Notification", "Click the FontAwesome dismiss icon to close.");
    }

    private async Task OpenDismissIconCss()
    {
        await dismissIconCssRef.Info("Notification", "Click the CSS class dismiss icon to close.");
    }

    private async Task OpenLeadingIconFa()
    {
        await leadingIconFaRef.Info("Notification", "The leading icon comes from FontAwesome.");
    }

    private async Task OpenDismissIconBi()
    {
        await dismissIconBiRef.Info("Notification", "Click the Bootstrap dismiss icon to close.");
    }

    private async Task OpenDismissIconImplicit()
    {
        await dismissIconImplicitRef.Info("Notification", "Click the implicit CSS dismiss icon to close.");
    }


    private BitSnackBar sizeSmallRef = default!;
    private BitSnackBar sizeMediumRef = default!;
    private BitSnackBar sizeLargeRef = default!;

    private async Task OpenSizeSmall() => await sizeSmallRef.Info("Small", "The small size snack bar.");

    private async Task OpenSizeMedium() => await sizeMediumRef.Info("Medium", "The medium size snack bar.");

    private async Task OpenSizeLarge() => await sizeLargeRef.Info("Large", "The large size snack bar.");


    private BitSnackBar snackBarStyleRef = default!;
    private BitSnackBar snackBarClassRef = default!;
    private BitSnackBar snackBarStylesRef = default!;
    private BitSnackBar snackBarClassesRef = default!;

    private async Task OpenSnackBarStyle()
    {
        await snackBarStyleRef.Show("This is title", "This is body", cssStyle: "background-color: dodgerblue; border-radius: 0.5rem;");
    }

    private async Task OpenSnackBarClass()
    {
        await snackBarClassRef.Show("This is title", "This is body", cssClass: "custom-class");
    }

    private async Task OpenSnackBarStyles()
    {
        await snackBarStylesRef.Show("This is title", "This is body");
    }

    private async Task OpenSnackBarClasses()
    {
        await snackBarClassesRef.Show("This is title", "This is body");
    }


    private BitSnackBar rtlRef = default!;

    private async Task OpenRtl()
    {
        await rtlRef.Success("عنوان پیام", "این متن پیام است.");
    }
}
