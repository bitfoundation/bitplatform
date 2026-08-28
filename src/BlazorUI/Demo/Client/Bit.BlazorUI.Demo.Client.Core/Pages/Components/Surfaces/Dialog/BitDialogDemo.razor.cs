namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Dialog;

public partial class BitDialogDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Dialog will be positioned absolute instead of fixed, so it covers its nearest positioned ancestor instead of the screen."
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Moves the focus into the Dialog when it opens, onto the first focusable element it holds, falling back to the Dialog itself when it holds none."
        },
        new()
        {
            Name = "AutoFocusButton",
            Type = "BitDialogButton?",
            DefaultValue = "null",
            Description = "Which of the Dialog's own buttons AutoFocus lands on, instead of the first focusable element the Dialog holds. Takes precedence over AutoFocusSelector.",
            LinkType = LinkType.Link,
            Href = "#component-button-enum",
        },
        new()
        {
            Name = "AutoFocusSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the element inside the Dialog that AutoFocus lands on, instead of the first focusable element it holds. A selector that matches nothing visible falls back to that first element."
        },
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the auto scrollbar toggle behavior of the Dialog, which stops the scroller from scrolling for as long as the Dialog is open."
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias for child content."
        },
        new()
        {
            Name = "CancelText",
            Type = "string?",
            DefaultValue = "Cancel",
            Description = "The text of the cancel button."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Dialog, it can be any custom tag or text."
        },
        new()
        {
            Name = "Classes",
            Type = "BitDialogClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitDialog component.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "CloseButtonTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title (and aria-label) of the close button, for accessibility and localization. Defaults to \"Close\" when not set."
        },
        new()
        {
            Name = "CloseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display for the close button using custom CSS classes for external icon libraries. Takes precedence over CloseIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "CloseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display for the close button from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "CloseOnEscape",
            Type = "bool",
            DefaultValue = "true",
            Description = "Dismisses the Dialog when the Escape key is pressed while the focus is inside it. A blocking Dialog ignores the Escape key whatever this is set to."
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the Dialog, which its Ok and Cancel buttons, the Ok spinner and the focus ring of both are painted in. Defaults to Primary.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DragElementSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the element the Dialog is dragged by. By default it is the header when the Dialog has one, and the whole container when it has none."
        },
        new()
        {
            Name = "FooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize how the footer inside the Dialog is rendered."
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Dialog height 100% of the area it is positioned in."
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Dialog width and height 100% of the area it is positioned in."
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Dialog width 100% of the area it is positioned in."
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the header of the Dialog, replacing the Title and Subtitle while keeping the close button beside it."
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS height of the Dialog surface. A Dialog is as tall as its content by default, and FullHeight and FullSize take precedence over this."
        },
        new()
        {
            Name = "IsAlert",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Determines the ARIA role of the Dialog (alertdialog/dialog). If this is set, it will override the ARIA role determined by IsBlocking and IsModeless."
        },
        new()
        {
            Name = "IsBlocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the Dialog from being dismissed by a click on the overlay or by the Escape key, leaving its buttons as the only way out."
        },
        new()
        {
            Name = "IsCancelButtonEnabled",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the Cancel button of the Dialog can be pressed. Unlike IsEnabled, which turns the whole Dialog off, this leaves every other way out of the Dialog working."
        },
        new()
        {
            Name = "IsDraggable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Dialog can be dragged around."
        },
        new()
        {
            Name = "IsModeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the Dialog). If true, IsBlocking is ignored, there will be no overlay, and the focus is not trapped - though the Dialog still takes it when it opens unless AutoFocus is turned off."
        },
        new()
        {
            Name = "IsOkButtonEnabled",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the Ok button of the Dialog can be pressed. This is what holds the answer shut until the content of the Dialog provides it - a consent to tick, a name to type - without turning the rest of the Dialog off the way IsEnabled would."
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Dialog is displayed."
        },
        new()
        {
            Name = "IsOpenChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "null",
            Description = "A callback function for when the Dialog is opened or closed."
        },
        new()
        {
            Name = "KeepMounted",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the Dialog in the DOM while it is closed, hidden, instead of removing it - so its content, and whatever state it holds, survives until the next showing."
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS maximum height of the Dialog surface. Defaults to 100% of the area the Dialog is positioned in, and setting it replaces that default rather than adding to it."
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS maximum width of the Dialog surface. Defaults to 100% of the area the Dialog is positioned in, and setting it replaces that default rather than adding to it - min(100%, 32rem) is the whole of a responsive Dialog."
        },
        new()
        {
            Name = "Message",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message to display in the dialog. It also describes the Dialog to a screen reader unless a Subtitle or a SubtitleAriaId takes that job instead."
        },
        new()
        {
            Name = "MinHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS minimum height of the Dialog surface."
        },
        new()
        {
            Name = "MinWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS minimum width of the Dialog surface, the floor under a Dialog whose message is a handful of words."
        },
        new()
        {
            Name = "NoDismissPreventedAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns off the shake the Dialog plays when a dismissal is refused. OnDismissPrevented is raised either way."
        },
        new()
        {
            Name = "OkText",
            Type = "string?",
            DefaultValue = "Ok",
            Description = "The text of the ok button."
        },
        new()
        {
            Name = "OnCancel",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "A callback function for when the Cancel button is clicked."
        },
        new()
        {
            Name = "OnClose",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "A callback function for when the Close button is clicked."
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "A callback function for when the the dialog is dismissed (closed). It is invoked for every closing the Dialog carries out itself, including a Close or Toggle call, and DismissReason names the gesture that ended the showing by the time it runs."
        },
        new()
        {
            Name = "OnDismissing",
            Type = "EventCallback<BitDialogDismissArgs>",
            DefaultValue = "null",
            Description = "A callback function invoked before the Dialog closes, letting the closing be refused. Set Cancel on the arguments to leave the Dialog where it is, and read Reason to tell the gestures apart. It is awaited, so it can run asynchronous work of its own.",
            LinkType = LinkType.Link,
            Href = "#dismiss-args",
        },
        new()
        {
            Name = "OnDismissPrevented",
            Type = "EventCallback<BitDialogDismissReason>",
            DefaultValue = "null",
            Description = "A callback function for when a dismissal was refused: the Escape key on a Dialog that does not take it, or a click on the overlay of a blocking one. The Dialog shakes on its own; this is for saying why.",
            LinkType = LinkType.Link,
            Href = "#component-dismiss-reason-enum",
        },
        new()
        {
            Name = "OnOverlayClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "A callback function for when the overlay of the Dialog is clicked, whether or not the click goes on to dismiss the Dialog."
        },
        new()
        {
            Name = "OnOk",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "A callback function for when the Ok button is clicked. The Dialog waits for it before closing and shows a spinner in place of the Ok text while it waits."
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            DefaultValue = "null",
            Description = "A callback function for when the Dialog is opened."
        },
        new()
        {
            Name = "Position",
            Type = "BitDialogPosition",
            DefaultValue = "BitDialogPosition.Center",
            Description = "Position of the Dialog on the screen.",
            LinkType = LinkType.Link,
            Href = "#component-position-enum",
        },
        new()
        {
            Name = "RestoreFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Hands the focus back to whatever held it when the Dialog opened, once the Dialog closes."
        },
        new()
        {
            Name = "ScrollerElement",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "Set the element reference for which the Dialog disables its scroll if applicable. Takes precedence over ScrollerSelector when both are set."
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string",
            DefaultValue = "body",
            Description = "Set the element selector for which the Dialog disables its scroll if applicable."
        },
        new()
        {
            Name = "ShowCancelButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the cancel button of the Dialog."
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the close button of the Dialog."
        },
        new()
        {
            Name = "ShowOkButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the ok button of the Dialog."
        },
        new()
        {
            Name = "Styles",
            Type = "BitDialogClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitDialog component.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Subtitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The secondary line of the header, under the title."
        },
        new()
        {
            Name = "SubtitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the subtitle of the Dialog, if any. When it is not set, the Dialog describes itself with its own Subtitle, or with its Message when there is no subtitle."
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title text to display at the top of the dialog."
        },
        new()
        {
            Name = "TitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the title of the Dialog, if any. When it is not set, the Dialog names itself with its own Title, and falls back to AriaLabel when there is none."
        },
        new()
        {
            Name = "TrapFocus",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Keeps Tab and Shift+Tab cycling inside the Dialog while it is open. Defaults to true for a normal Dialog and false for a modeless one."
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS width of the Dialog surface. A Dialog is as wide as its content by default, and FullWidth and FullSize take precedence over this."
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Result",
            Type = "BitDialogResult?",
            DefaultValue = "null",
            Description = "The result of the last showing of the Dialog: Ok or Cancel when one of those buttons ended it, and null when it was dismissed without an answer or has not been shown yet.",
            LinkType = LinkType.Link,
            Href = "#component-result-enum",
        },
        new()
        {
            Name = "DismissReason",
            Type = "BitDialogDismissReason?",
            DefaultValue = "null",
            Description = "What ended the last showing of the Dialog - the gesture that closed it - and null while it is open or before it has been shown at all. It is set before OnDismiss and IsOpenChanged run.",
            LinkType = LinkType.Link,
            Href = "#component-dismiss-reason-enum",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitDialogResult?>",
            Description = "Opens the Dialog and waits for it to close, reporting how it closed."
        },
        new()
        {
            Name = "Open",
            Type = "Task",
            Description = "Opens the Dialog."
        },
        new()
        {
            Name = "Close",
            Type = "Task",
            Description = "Closes the Dialog the same way its own gestures do: OnDismissing gets its say and can refuse it, DismissReason is named Programmatic, and OnDismiss is invoked once it is done."
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Opens the Dialog when it is closed and closes it when it is open."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitDialogClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitDialog."
                },
                new()
                {
                    Name = "Document",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the document element of the BitDialog, the layer that holds the overlay and the container and decides where on the screen the Dialog sits."
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitDialog."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the BitDialog."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitDialog."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of the BitDialog."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitDialog."
                },
                new()
                {
                    Name = "Subtitle",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the subtitle of the BitDialog."
                },
                new()
                {
                    Name = "CloseButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the close button of the BitDialog."
                },
                new()
                {
                    Name = "CloseIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the close button of the BitDialog."
                },
                new()
                {
                    Name = "Message",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the message of the BitDialog."
                },
                new()
                {
                    Name = "ButtonsContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the buttons container of the BitDialog."
                },
                new()
                {
                    Name = "Spinner",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spinner of the ok button of the BitDialog."
                },
                new()
                {
                    Name = "OkButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the ok button of the BitDialog."
                },
                new()
                {
                    Name = "CancelButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the cancel button of the BitDialog."
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer of the BitDialog, the element that wraps the FooterTemplate."
                }
            ]
        },
        new()
        {
            Id = "dismiss-args",
            Title = "BitDialogDismissArgs",
            Parameters =
            [
                new()
                {
                    Name = "Reason",
                    Type = "BitDialogDismissReason",
                    DefaultValue = "",
                    Description = "What is about to close the Dialog: one of its three buttons, a click on the overlay, the Escape key, or a call to one of its Close and Toggle methods.",
                    LinkType = LinkType.Link,
                    Href = "#component-dismiss-reason-enum",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to refuse the closing and leave the Dialog where it is. A refused closing shakes the surface and raises OnDismissPrevented with the same reason."
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
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
            ]
        },
        new()
        {
            Id = "component-position-enum",
            Name = "BitDialogPosition",
            Description = "The Left and Right values are physical and stay on the same side of the screen in both reading directions. The Start and End values are logical: Start is the left in an LTR Dialog and the right in an RTL one.",
            Items =
            [
                new() { Name = "Center", Value = "0", Description = "Centered both ways." },
                new() { Name = "TopLeft", Value = "1", Description = "The top left corner, in both reading directions." },
                new() { Name = "TopCenter", Value = "2", Description = "The top edge, centered horizontally." },
                new() { Name = "TopRight", Value = "3", Description = "The top right corner, in both reading directions." },
                new() { Name = "CenterLeft", Value = "4", Description = "The left edge, centered vertically." },
                new() { Name = "CenterRight", Value = "5", Description = "The right edge, centered vertically." },
                new() { Name = "BottomLeft", Value = "6", Description = "The bottom left corner, in both reading directions." },
                new() { Name = "BottomCenter", Value = "7", Description = "The bottom edge, centered horizontally." },
                new() { Name = "BottomRight", Value = "8", Description = "The bottom right corner, in both reading directions." },
                new() { Name = "TopStart", Value = "9", Description = "The top edge, on the side the reading direction starts from." },
                new() { Name = "TopEnd", Value = "10", Description = "The top edge, on the side the reading direction ends at." },
                new() { Name = "CenterStart", Value = "11", Description = "Centered vertically, on the side the reading direction starts from." },
                new() { Name = "CenterEnd", Value = "12", Description = "Centered vertically, on the side the reading direction ends at." },
                new() { Name = "BottomStart", Value = "13", Description = "The bottom edge, on the side the reading direction starts from." },
                new() { Name = "BottomEnd", Value = "14", Description = "The bottom edge, on the side the reading direction ends at." }
            ]
        },
        new()
        {
            Id = "component-result-enum",
            Name = "BitDialogResult",
            Description = "How a showing of the Dialog ended. A dismissal that answered neither way reports null rather than one of these.",
            Items =
            [
                new() { Name = "Ok", Value = "0", Description = "The Ok button ended the showing." },
                new() { Name = "Cancel", Value = "1", Description = "The Cancel button ended the showing." }
            ]
        },
        new()
        {
            Id = "component-dismiss-reason-enum",
            Name = "BitDialogDismissReason",
            Description = "What closed the last showing of the Dialog. BitDialogResult reports the answer a showing was given; this reports the gesture that ended it, which is what tells an Escape apart from a click on the overlay when neither leaves an answer.",
            Items =
            [
                new() { Name = "OkButton", Value = "0", Description = "The Ok button ended the showing." },
                new() { Name = "CancelButton", Value = "1", Description = "The Cancel button ended the showing." },
                new() { Name = "CloseButton", Value = "2", Description = "The close button in the header ended the showing." },
                new() { Name = "OverlayClick", Value = "3", Description = "A click on the overlay ended the showing." },
                new() { Name = "Escape", Value = "4", Description = "The Escape key ended the showing." },
                new() { Name = "Programmatic", Value = "5", Description = "The page closed the Dialog itself, by setting IsOpen or by calling Close or Toggle." }
            ]
        },
        new()
        {
            Id = "component-button-enum",
            Name = "BitDialogButton",
            Description = "One of the three buttons a BitDialog renders of its own.",
            Items =
            [
                new() { Name = "Ok", Value = "0", Description = "The Ok button, which answers the Dialog with BitDialogResult.Ok." },
                new() { Name = "Cancel", Value = "1", Description = "The Cancel button, which answers the Dialog with BitDialogResult.Cancel." },
                new() { Name = "Close", Value = "2", Description = "The close button in the header, which dismisses the Dialog without an answer." }
            ]
        }
    ];



    private bool IsOpen = false;

    private bool isOpenLabels = false;
    private bool isOpenAcknowledge = false;
    private bool isOpenNoClose = false;
    private bool isOpenGated = false;
    private bool agreed = false;

    private bool isOpenSubtitle = false;
    private bool isOpenHeaderTemplate = false;
    private bool isOpenFooterTemplate = false;

    private bool IsOpen2 = false;
    private string? optionValue;
    private BitDialog customDialogRef = default!;

    private bool IsOpen1 = false;
    private BitDialog dialogRef = default!;
    private BitDialog awaitDialogRef = default!;
    private string awaitedResultText = "(not shown yet)";

    private bool IsOpenEvent = false;
    private string lastEvent = "-";

    private bool IsOpen4 = false;
    private bool isOpenNoEscape = false;
    private bool isOpenModeless = false;
    private bool isOpenPrevented = false;
    private string? preventedHint;

    private bool hasUnsavedChanges = true;
    private bool isOpenGuarded = false;
    private string? guardedHint;
    private string refusedGesture = "-";
    private BitDialog guardedDialogRef = default!;

    private bool isOpenFocus = false;
    private bool isOpenNoFocus = false;
    private bool isOpenFocusCancel = false;
    private bool isOpenFocusSelector = false;

    private bool IsOpen5 = false;
    private bool IsOpen7 = false;

    private bool IsOpenInPosition = false;
    private BitDialogPosition position;
    private bool isOpenPhysical = false;
    private bool isOpenLogical = false;

    private bool IsOpen6 = false;

    private bool IsDraggable = false;
    private bool IsOpen8 = false;
    private bool IsOpen9 = false;

    private bool isOpenOuter = false;
    private bool isOpenInner = false;

    private bool isOpenKeptMounted = false;
    private bool isOpenUnmounted = false;

    private bool isOpenColor = false;
    private BitColor dialogColor = BitColor.Primary;
    private readonly BitColor[] dialogColors = Enum.GetValues<BitColor>();

    private bool IsOpenExtIcon1 = false;
    private bool IsOpenExtIcon2 = false;
    private bool IsOpenExtIcon3 = false;
    private bool IsOpenExtIcon4 = false;

    private bool isOpenSized = false;
    private bool isOpenResponsive = false;
    private bool isOpenTall = false;
    private bool isOpenFullWidth = false;
    private bool isOpenFullSize = false;

    private bool isOpenStyles = false;
    private bool isOpenClasses = false;

    private bool IsOpen10 = false;

    private async Task ShowAndAwait()
    {
        var result = await awaitDialogRef.Show();

        awaitedResultText = result?.ToString() ?? "(dismissed)";
    }

    private async Task HandleSlowOk()
    {
        lastEvent = "OnOk (working...)";

        await Task.Delay(1000);

        lastEvent = "OnOk";
    }

    private async Task HandleColorOk()
    {
        await Task.Delay(1000);
    }

    private void HandleDismissing(BitDialogDismissArgs args)
    {
        // Save is the way out that is always let through, so the Dialog is never a trap.
        args.Cancel = hasUnsavedChanges && args.Reason is not BitDialogDismissReason.OkButton;
    }

    private void OpenDialogInColor(BitColor color)
    {
        dialogColor = color;
        isOpenColor = true;
    }

    private void OpenDialogInPosition(BitDialogPosition positionValue)
    {
        IsOpenInPosition = true;
        position = positionValue;
    }



    private readonly string example1RazorCode = @"
<BitButton OnClick=""@(() => IsOpen = true)"">Open Dialog</BitButton>
<BitDialog @bind-IsOpen=""IsOpen"" Title=""Missing Subject"" Message=""Do you want to send this message without a subject?"" />";
    private readonly string example1CsharpCode = @"
private bool IsOpen = false;";

    private readonly string example2RazorCode = @"
<style>
    .dialog-body {
        max-width: 40rem;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>

<BitButton OnClick=""@(() => isOpenLabels = true)"">Custom labels</BitButton>
<BitButton OnClick=""@(() => isOpenAcknowledge = true)"">Single action</BitButton>
<BitButton OnClick=""@(() => isOpenNoClose = true)"">No close button</BitButton>

<BitDialog @bind-IsOpen=""isOpenLabels""
           Title=""Delete this file?""
           Message=""This file will be moved to the trash. You can restore it for 30 days.""
           OkText=""Move to trash""
           CancelText=""Keep it"" />

<BitDialog @bind-IsOpen=""isOpenAcknowledge""
           ShowCancelButton=""false""
           Title=""Your session expired""
           Message=""Sign in again to pick up where you left off.""
           OkText=""Got it"" />

<BitDialog @bind-IsOpen=""isOpenNoClose""
           ShowCloseButton=""false""
           Title=""Missing Subject""
           Message=""Do you want to send this message without a subject?"" />


<BitButton OnClick=""@(() => { agreed = false; isOpenGated = true; })"">Open Dialog</BitButton>
<BitDialog @bind-IsOpen=""isOpenGated""
           IsOkButtonEnabled=""agreed""
           Title=""Before you continue""
           ShowCloseButton=""false""
           OkText=""Accept"">
    <div class=""dialog-body"">
        <BitCheckbox @bind-Value=""agreed"" Label=""I have read and agree to the terms"" />
    </div>
</BitDialog>";
    private readonly string example2CsharpCode = @"
private bool isOpenLabels = false;
private bool isOpenAcknowledge = false;
private bool isOpenNoClose = false;
private bool isOpenGated = false;
private bool agreed = false;";

    private readonly string example3RazorCode = @"
<style>
    .dialog-header {
        gap: 0.5rem;
        display: flex;
        font-size: 20px;
        font-weight: 600;
        align-items: center;
    }

    .dialog-footer {
        display: flex;
        align-items: center;
        padding: 0 14px 14px;
        justify-content: flex-end;
    }
</style>

<BitButton OnClick=""@(() => isOpenSubtitle = true)"">Title & subtitle</BitButton>
<BitButton OnClick=""@(() => isOpenHeaderTemplate = true)"">Header template</BitButton>
<BitButton OnClick=""@(() => isOpenFooterTemplate = true)"">Footer template</BitButton>

<BitDialog @bind-IsOpen=""isOpenSubtitle""
           Title=""Publish this version?""
           Subtitle=""Version 4.2.0 · 18 changed files""
           Message=""Everyone in the workspace will see this version as soon as it goes out."" />

<BitDialog @bind-IsOpen=""isOpenHeaderTemplate""
           AriaLabel=""Storage almost full""
           Message=""Delete something, or move up to the next plan to keep syncing."">
    <HeaderTemplate>
        <div class=""dialog-header"">
            <BitIcon IconName=""@BitIconName.Warning"" Color=""BitColor.Warning"" />
            <span>Storage almost full</span>
        </div>
    </HeaderTemplate>
</BitDialog>

<BitDialog @bind-IsOpen=""isOpenFooterTemplate""
           ShowOkButton=""false""
           ShowCancelButton=""false""
           Title=""Delete all""
           Message=""+99 emails will be deleted."">
    <FooterTemplate>
        <div class=""dialog-footer"">
            Are you sure?! there's no going back.
        </div>
    </FooterTemplate>
</BitDialog>";
    private readonly string example3CsharpCode = @"
private bool isOpenSubtitle = false;
private bool isOpenHeaderTemplate = false;
private bool isOpenFooterTemplate = false;";

    private readonly string example4RazorCode = @"
<style>
    .dialog-title {
        display: flex;
        font-size: 24px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        border-top: 4px solid #0054C6;
        justify-content: space-between;
    }

    .dialog-body {
        max-width: 40rem;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>

<BitButton OnClick=""@(() => IsOpen2 = true)"">Open Dialog</BitButton>
<div>Result is: @customDialogRef?.Result</div>
@if (customDialogRef?.Result == BitDialogResult.Ok)
{
    <div>Value is: @optionValue</div>
}

<BitDialog @ref=""customDialogRef""
           @bind-IsOpen=""@IsOpen2""
           TitleAriaId=""dialog-custom-title""
           ShowCloseButton=""false"">
    <div class=""dialog-title"" id=""dialog-custom-title"">
        <span>All emails together</span>
    </div>
    <div class=""dialog-body"">
        <p>
            Your Inbox has changed. No longer does it include favorites, it is a singular destination for your emails.
        </p>
        <br />
        <BitChoiceGroup @bind-Value=""optionValue"" Label=""Basic Options"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
            <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
            <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
            <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        </BitChoiceGroup>
    </div>
</BitDialog>";
    private readonly string example4CsharpCode = @"
private bool IsOpen2 = false;
private string? optionValue;
private BitDialog customDialogRef = default!;";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""@(() => IsOpen1 = true)"">Open Dialog</BitButton>
<div>Result is: @(dialogRef?.Result?.ToString() ?? ""(dismissed)"")</div>
<div>Dismiss reason is: @(dialogRef?.DismissReason?.ToString() ?? ""-"")</div>

<BitDialog @ref=""@dialogRef""
           @bind-IsOpen=""@IsOpen1""
           Title=""Missing Subject""
           Message=""Do you want to send this message without a subject?"" />


<BitButton OnClick=""ShowAndAwait"">Show and await</BitButton>
<span>Awaited result is: @awaitedResultText</span>

<BitDialog @ref=""awaitDialogRef""
           Title=""Discard draft?""
           OkText=""Discard""
           CancelText=""Keep editing""
           Message=""Your changes since the last save will be lost."" />";
    private readonly string example5CsharpCode = @"
private bool IsOpen1 = false;
private BitDialog dialogRef = default!;
private BitDialog awaitDialogRef = default!;
private string awaitedResultText = ""(not shown yet)"";

private async Task ShowAndAwait()
{
    var result = await awaitDialogRef.Show();

    awaitedResultText = result?.ToString() ?? ""(dismissed)"";
}";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""@(() => IsOpenEvent = true)"">Open Dialog</BitButton>
<div>Last event: @lastEvent</div>

<BitDialog @bind-IsOpen=""IsOpenEvent""
           Title=""Missing Subject""
           Message=""Do you want to send this message without a subject?""
           OnOpen=""@(() => lastEvent = ""OnOpen"")""
           OnOk=""HandleSlowOk""
           OnCancel=""@(() => lastEvent = ""OnCancel"")""
           OnClose=""@(() => lastEvent = ""OnClose"")""
           OnOverlayClick=""@(() => lastEvent = ""OnOverlayClick"")""
           OnDismiss=""@(() => lastEvent += "" → OnDismiss"")"" />";
    private readonly string example6CsharpCode = @"
private bool IsOpenEvent = false;
private string lastEvent = ""-"";

private async Task HandleSlowOk()
{
    lastEvent = ""OnOk (working...)"";

    await Task.Delay(1000);

    lastEvent = ""OnOk"";
}";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""@(() => IsOpen4 = true)"">IsBlocking</BitButton>
<BitButton OnClick=""@(() => isOpenNoEscape = true)"">CloseOnEscape = false</BitButton>
<BitButton OnClick=""@(() => isOpenModeless = true)"">IsModeless</BitButton>

<BitDialog IsBlocking
           @bind-IsOpen=""IsOpen4""
           Title=""Missing Subject""
           Message=""Neither the Escape key nor a click outside will close this one."" />

<BitDialog CloseOnEscape=""false""
           @bind-IsOpen=""isOpenNoEscape""
           Title=""Missing Subject""
           Message=""Escape does nothing here, but a click on the overlay still closes it."" />

<BitDialog IsModeless
           @bind-IsOpen=""isOpenModeless""
           Position=""BitDialogPosition.TopEnd""
           Title=""Modeless""
           Message=""There is no overlay, so the page behind this one is still usable."" />


<BitButton OnClick=""@(() => isOpenPrevented = true)"">Blocking with a hint</BitButton>

<BitDialog IsBlocking
           @bind-IsOpen=""isOpenPrevented""
           Title=""Two-factor code""
           Subtitle=""@preventedHint""
           Message=""Enter the six-digit code from your authenticator app to finish signing in.""
           OkText=""Verify""
           CancelText=""Use another method""
           OnOpen=""@(() => preventedHint = null)""
           OnDismissPrevented=""@(r => preventedHint = $""{r} will not close this one - answer it with one of the buttons."")"" />";
    private readonly string example7CsharpCode = @"
private bool IsOpen4 = false;
private bool isOpenNoEscape = false;
private bool isOpenModeless = false;
private bool isOpenPrevented = false;
private string? preventedHint;";

    private readonly string example8RazorCode = @"
<BitToggle Label=""The note has unsaved changes"" @bind-Value=""hasUnsavedChanges"" />

<BitButton OnClick=""@(() => isOpenGuarded = true)"">Open Dialog</BitButton>

<div>Last refused gesture: @refusedGesture</div>
<div>Result is: @(guardedDialogRef?.Result?.ToString() ?? ""(none yet)"")</div>

<BitDialog @ref=""guardedDialogRef""
           @bind-IsOpen=""isOpenGuarded""
           Title=""Edit the note""
           Subtitle=""@guardedHint""
           Message=""While the toggle above is on, everything but Save is refused - try Escape, the overlay, the close button and Cancel.""
           OkText=""Save""
           CancelText=""Discard""
           OnDismissing=""HandleDismissing""
           OnOpen=""@(() => { guardedHint = null; refusedGesture = ""-""; })""
           OnDismissPrevented=""@(r => { refusedGesture = r.ToString(); guardedHint = ""There are unsaved changes - save them first.""; })"" />";
    private readonly string example8CsharpCode = @"
private bool hasUnsavedChanges = true;
private bool isOpenGuarded = false;
private string? guardedHint;
private string refusedGesture = ""-"";
private BitDialog guardedDialogRef = default!;

private void HandleDismissing(BitDialogDismissArgs args)
{
    // Save is the way out that is always let through, so the Dialog is never a trap.
    args.Cancel = hasUnsavedChanges && args.Reason is not BitDialogDismissReason.OkButton;
}";

    private readonly string example9RazorCode = @"
<style>
    .dialog-body {
        max-width: 40rem;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>

<BitButton OnClick=""@(() => isOpenFocus = true)"">Default focus behavior</BitButton>
<BitButton OnClick=""@(() => isOpenNoFocus = true)"">AutoFocus & TrapFocus off</BitButton>

<BitDialog @bind-IsOpen=""isOpenFocus""
           Title=""Rename the project""
           ShowCloseButton=""false"">
    <div class=""dialog-body"">
        <BitTextField Label=""Name"" DefaultValue=""Untitled project"" />
    </div>
</BitDialog>

<BitDialog AutoFocus=""false""
           TrapFocus=""false""
           @bind-IsOpen=""isOpenNoFocus""
           Title=""Rename the project""
           ShowCloseButton=""false"">
    <div class=""dialog-body"">
        <BitTextField Label=""Name"" DefaultValue=""Untitled project"" />
    </div>
</BitDialog>


<BitButton OnClick=""@(() => isOpenFocusCancel = true)"">Focus the safe answer</BitButton>
<BitDialog @bind-IsOpen=""isOpenFocusCancel""
           IsAlert
           ShowCloseButton=""false""
           AutoFocusButton=""BitDialogButton.Cancel""
           Title=""Delete this workspace?""
           Message=""Every project, file and comment in it goes with it. This cannot be undone.""
           OkText=""Delete workspace""
           CancelText=""Cancel"" />


<BitButton OnClick=""@(() => isOpenFocusSelector = true)"">Focus a field of your own</BitButton>

<BitDialog @bind-IsOpen=""isOpenFocusSelector""
           Title=""Invite a teammate""
           ShowCloseButton=""false""
           AutoFocusSelector="".invite-email input""
           OkText=""Send invite"">
    <div class=""dialog-body"">
        <BitLink Href=""/components/dialog"">What can a guest see?</BitLink>
        <BitTextField Class=""invite-email"" Label=""Email"" Placeholder=""name@example.com"" />
    </div>
</BitDialog>";
    private readonly string example9CsharpCode = @"
private bool isOpenFocus = false;
private bool isOpenNoFocus = false;
private bool isOpenFocusCancel = false;
private bool isOpenFocusSelector = false;";

    private readonly string example10RazorCode = @"
<style>
    .relative-container {
        width: 100%;
        height: 20rem;
        overflow: auto;
        padding: 0.5rem;
        margin-top: 1rem;
        position: relative;
        border: 2px lightgreen solid;
    }
</style>

<BitButton OnClick=""@(() => IsOpen5 = true)"">AutoToggleScroll</BitButton>
<BitButton OnClick=""@(() => IsOpen7 = true)"">ScrollerSelector</BitButton>

<BitDialog AutoToggleScroll
           @bind-IsOpen=""IsOpen5""
           Title=""Missing Subject""
           Message=""The page behind this one cannot be scrolled while it is open."" />

<div class=""relative-container"">
    <BitDialog AbsolutePosition AutoToggleScroll
               @bind-IsOpen=""IsOpen7""
               ScrollerSelector="".relative-container""
               Title=""Missing Subject""
               Message=""This one locks the box it sits in, not the page."" />

    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
    when possibilities are limitless, waiting for content to emerge.
</div>";
    private readonly string example10CsharpCode = @"
private bool IsOpen5 = false;
private bool IsOpen7 = false;";

    private readonly string example11RazorCode = @"
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.TopLeft)"">Top Left</BitButton>
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.TopCenter)"">Top Center</BitButton>
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.TopRight)"">Top Right</BitButton>
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.CenterLeft)"">Center Left</BitButton>
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.Center)"">Center</BitButton>
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.CenterRight)"">Center Right</BitButton>
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.BottomLeft)"">Bottom Left</BitButton>
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.BottomCenter)"">Bottom Center</BitButton>
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.BottomRight)"">Bottom Right</BitButton>

<BitDialog @bind-IsOpen=""IsOpenInPosition""
           Position=""position""
           Title=""Missing Subject""
           Message=""Do you want to send this message without a subject?"" />


<BitButton Dir=""BitDir.Rtl"" OnClick=""@(() => isOpenPhysical = true)"">TopLeft</BitButton>
<BitButton Dir=""BitDir.Rtl"" OnClick=""@(() => isOpenLogical = true)"">TopStart</BitButton>

<BitDialog @bind-IsOpen=""isOpenPhysical""
           Dir=""BitDir.Rtl""
           Position=""BitDialogPosition.TopLeft""
           Title=""TopLeft""
           OkText=""تایید""
           CancelText=""انصراف""
           Message=""موقعیت فیزیکی: همیشه سمت چپ"" />

<BitDialog @bind-IsOpen=""isOpenLogical""
           Dir=""BitDir.Rtl""
           Position=""BitDialogPosition.TopStart""
           Title=""TopStart""
           OkText=""تایید""
           CancelText=""انصراف""
           Message=""موقعیت منطقی: ابتدای جهت خواندن"" />";
    private readonly string example11CsharpCode = @"
private bool IsOpenInPosition = false;
private BitDialogPosition position;
private bool isOpenPhysical = false;
private bool isOpenLogical = false;

private void OpenDialogInPosition(BitDialogPosition positionValue)
{
    IsOpenInPosition = true;
    position = positionValue;
}";

    private readonly string example12RazorCode = @"
<style>
    .relative-container {
        width: 100%;
        height: 20rem;
        overflow: auto;
        padding: 0.5rem;
        margin-top: 1rem;
        position: relative;
        border: 2px lightgreen solid;
    }
</style>

<BitButton OnClick=""@(() => IsOpen6 = true)"">Open Dialog</BitButton>

<div class=""relative-container"">
    <BitDialog AbsolutePosition
               @bind-IsOpen=""IsOpen6""
               Title=""Missing Subject""
               Message=""This Dialog covers the bordered box, not the page."" />

    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
</div>";
    private readonly string example12CsharpCode = @"
private bool IsOpen6 = false;";

    private readonly string example13RazorCode = @"
<style>
    .dialog-title {
        display: flex;
        font-size: 24px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        border-top: 4px solid #0054C6;
        justify-content: space-between;
    }

    .dialog-body {
        max-width: 40rem;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>

<BitToggle Label=""Is Draggable"" @bind-Value=""IsDraggable"" />

<BitButton OnClick=""@(() => IsOpen8 = true)"">Open Dialog</BitButton>
<BitDialog @bind-IsOpen=""IsOpen8""
           IsDraggable=""IsDraggable""
           Title=""Draggable dialog""
           Message=""Do you want to send this message without a subject?"" />

<BitButton OnClick=""@(() => IsOpen9 = true)"">Open Dialog</BitButton>
<BitDialog IsDraggable
           @bind-IsOpen=""IsOpen9""
           ShowCloseButton=""false""
           AriaLabel=""Draggable Dialog with custom drag element""
           DragElementSelector="".dialog-title-drag"">
    <div class=""dialog-title dialog-title-drag"">
        <span>Draggable Dialog with custom drag element</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => IsOpen9 = false)"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""dialog-body"">
        <p>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        </p>
    </div>
</BitDialog>";
    private readonly string example13CsharpCode = @"
private bool IsDraggable = false;
private bool IsOpen8 = false;
private bool IsOpen9 = false;";

    private readonly string example14RazorCode = @"
<style>
    .dialog-body {
        max-width: 40rem;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>

<BitButton OnClick=""@(() => isOpenOuter = true)"">Open Dialog</BitButton>

<BitDialog @bind-IsOpen=""isOpenOuter""
           Title=""Publish this version?""
           Message=""Everyone in the workspace will see this version as soon as it goes out.""
           OkText=""Publish"">
    <div class=""dialog-body"">
        <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => isOpenInner = true)"">What changed?</BitButton>

        <BitDialog @bind-IsOpen=""isOpenInner""
                   Title=""Changes in 4.2.0""
                   Subtitle=""18 changed files""
                   ShowOkButton=""false""
                   CancelText=""Back""
                   Message=""Press Escape here and only this Dialog closes."" />
    </div>
</BitDialog>";
    private readonly string example14CsharpCode = @"
private bool isOpenOuter = false;
private bool isOpenInner = false;";

    private readonly string example15RazorCode = @"
<style>
    .dialog-body {
        max-width: 40rem;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>

<BitButton OnClick=""@(() => isOpenKeptMounted = true)"">KeepMounted</BitButton>
<BitButton OnClick=""@(() => isOpenUnmounted = true)"">Default</BitButton>

<BitDialog KeepMounted
           @bind-IsOpen=""isOpenKeptMounted""
           Title=""Report an issue""
           ShowCloseButton=""false""
           OkText=""Send"">
    <div class=""dialog-body"">
        <BitTextField Label=""What happened?"" Multiline Rows=""4"" />
    </div>
</BitDialog>

<BitDialog @bind-IsOpen=""isOpenUnmounted""
           Title=""Report an issue""
           ShowCloseButton=""false""
           OkText=""Send"">
    <div class=""dialog-body"">
        <BitTextField Label=""What happened?"" Multiline Rows=""4"" />
    </div>
</BitDialog>";
    private readonly string example15CsharpCode = @"
private bool isOpenKeptMounted = false;
private bool isOpenUnmounted = false;";

    private readonly string example16RazorCode = @"
@foreach (var color in dialogColors)
{
    <BitButton Color=""color"" OnClick=""() => OpenDialogInColor(color)"">@color</BitButton>
}

<BitDialog @bind-IsOpen=""isOpenColor""
           Color=""dialogColor""
           AutoFocusButton=""BitDialogButton.Cancel""
           Title=""@($""{dialogColor} dialog"")""
           Message=""The two buttons, the ring around the focused one and the spinner the Ok button shows all follow the color.""
           OkText=""Confirm""
           OnOk=""HandleColorOk"" />";
    private readonly string example16CsharpCode = @"
private bool isOpenColor = false;
private BitColor dialogColor = BitColor.Primary;
private readonly BitColor[] dialogColors = Enum.GetValues<BitColor>();

private void OpenDialogInColor(BitColor color)
{
    dialogColor = color;
    isOpenColor = true;
}

private async Task HandleColorOk()
{
    await Task.Delay(1000);
}";

    private readonly string example17RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitButton OnClick=""@(() => IsOpenExtIcon1 = true)"">Open Dialog (CloseIcon = fa)</BitButton>
<BitDialog @bind-IsOpen=""IsOpenExtIcon1""
           Title=""FontAwesome Close Icon""
           Message=""This dialog uses a FontAwesome icon for the close button.""
           CloseIcon=""@BitIconInfo.Fa(""solid xmark"")"" />

<BitButton OnClick=""@(() => IsOpenExtIcon2 = true)"">Open Dialog (CloseIcon = Css)</BitButton>
<BitDialog @bind-IsOpen=""IsOpenExtIcon2""
           Title=""Custom CSS Close Icon""
           Message=""This dialog uses custom CSS classes for the close button icon.""
           CloseIcon=""@BitIconInfo.Css(""fa-solid fa-circle-xmark"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitButton OnClick=""@(() => IsOpenExtIcon3 = true)"">Open Dialog (CloseIcon = Bi)</BitButton>
<BitDialog @bind-IsOpen=""IsOpenExtIcon3""
           Title=""Bootstrap Close Icon""
           Message=""This dialog uses a Bootstrap icon for the close button.""
           CloseIcon=""@BitIconInfo.Bi(""x-lg"")"" />

<BitButton OnClick=""@(() => IsOpenExtIcon4 = true)"">Open Dialog (CloseIconName)</BitButton>
<BitDialog @bind-IsOpen=""IsOpenExtIcon4""
           Title=""CloseIconName""
           Message=""This dialog uses CloseIconName to set a built-in Fluent UI icon for the close button.""
           CloseButtonTitle=""Dismiss""
           CloseIconName=""@BitIconName.ChromeClose"" />";
    private readonly string example17CsharpCode = @"
private bool IsOpenExtIcon1 = false;
private bool IsOpenExtIcon2 = false;
private bool IsOpenExtIcon3 = false;
private bool IsOpenExtIcon4 = false;";

    private readonly string example18RazorCode = @"
<style>
    .dialog-body {
        max-width: 40rem;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>

<BitButton OnClick=""@(() => isOpenSized = true)"">Width</BitButton>
<BitButton OnClick=""@(() => isOpenResponsive = true)"">MaxWidth & MinWidth</BitButton>
<BitButton OnClick=""@(() => isOpenTall = true)"">Height & MaxHeight</BitButton>
<BitButton OnClick=""@(() => isOpenFullWidth = true)"">FullWidth</BitButton>
<BitButton OnClick=""@(() => isOpenFullSize = true)"">FullSize</BitButton>

<BitDialog Width=""32rem""
           @bind-IsOpen=""isOpenSized""
           Title=""Fixed width""
           Message=""This Dialog is 32rem wide however little it has to say."" />

<BitDialog MinWidth=""20rem""
           MaxWidth=""min(100%, 28rem)""
           @bind-IsOpen=""isOpenResponsive""
           Title=""Responsive width""
           Message=""No narrower than 20rem, no wider than 28rem, and never wider than the screen."" />

<BitDialog Height=""24rem""
           MaxHeight=""min(100%, 24rem)""
           @bind-IsOpen=""isOpenTall""
           Title=""Fixed height""
           OkText=""Agree"">
    <div class=""dialog-body"">
        <p>
            The surface keeps its height whatever it holds, and the body scrolls inside it while the header
            above and the buttons below stay where they are.
        </p>
    </div>
</BitDialog>

<BitDialog FullWidth
           @bind-IsOpen=""isOpenFullWidth""
           Position=""BitDialogPosition.BottomCenter""
           Title=""Missing Subject""
           Message=""Do you want to send this message without a subject?"" />

<BitDialog FullSize
           @bind-IsOpen=""isOpenFullSize""
           Title=""Missing Subject""
           Message=""Do you want to send this message without a subject?"" />";
    private readonly string example18CsharpCode = @"
private bool isOpenSized = false;
private bool isOpenResponsive = false;
private bool isOpenTall = false;
private bool isOpenFullWidth = false;
private bool isOpenFullSize = false;";

    private readonly string example19RazorCode = @"
<style>
    .custom-container {
        border: 2px solid tomato;
    }

    .custom-header {
        background-color: #fff3f0;
    }

    .custom-ok {
        border-color: tomato;
        background-color: tomato;
    }
</style>

<BitButton OnClick=""@(() => isOpenStyles = true)"">Styles</BitButton>
<BitButton OnClick=""@(() => isOpenClasses = true)"">Classes</BitButton>

<BitDialog @bind-IsOpen=""isOpenStyles""
           Title=""Styled Dialog""
           Subtitle=""Every part reachable on its own""
           Message=""The overlay, the container, the title and the two buttons are all restyled here.""
           Styles=""@(new()
           {
               Overlay = ""backdrop-filter: blur(2px);"",
               Container = ""width: 24rem; border: 2px solid blueviolet;"",
               Title = ""color: blueviolet;"",
               Message = ""font-style: italic;"",
               OkButton = ""background-color: blueviolet; border-color: blueviolet;"",
               CancelButton = ""color: blueviolet; border-color: blueviolet;""
           })"" />

<BitDialog @bind-IsOpen=""isOpenClasses""
           Title=""Classed Dialog""
           Message=""The same parts, reached with CSS classes of your own.""
           Classes=""@(new()
           {
               Container = ""custom-container"",
               Header = ""custom-header"",
               OkButton = ""custom-ok""
           })"" />";
    private readonly string example19CsharpCode = @"
private bool isOpenStyles = false;
private bool isOpenClasses = false;";

    private readonly string example20RazorCode = @"
<BitButton Dir=""BitDir.Rtl"" OnClick=""@(() => IsOpen10 = true)"">باز کردن پنجره پیام</BitButton>
<BitDialog @bind-IsOpen=""IsOpen10""
           Dir=""BitDir.Rtl""
           Title=""بدون موضوع""
           OkText=""تایید""
           CancelText=""انصراف""
           CloseButtonTitle=""بستن""
           Message=""آیا می خواهید این پیام را بدون موضوع ارسال کنید؟"" />";
    private readonly string example20CsharpCode = @"
private bool IsOpen10 = false;";
}
