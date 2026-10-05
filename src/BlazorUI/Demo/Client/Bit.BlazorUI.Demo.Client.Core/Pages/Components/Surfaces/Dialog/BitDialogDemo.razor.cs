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
            Description = "Enables the auto scrollbar toggle behavior of the Dialog, which stops the scroller from scrolling for as long as the Dialog is open. The scroller is the one ScrollerElement or ScrollerSelector names, the one a surrounding BitAppShell cascades when neither does, and the page when there is no shell either."
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
            Description = "Dismisses the Dialog when the Escape key is pressed while the focus is inside it. A blocking Dialog ignores the Escape key whatever this is set to, and an Escape a field inside answers first (closing its own open list, or an IME composition) is left to it."
        },
        new()
        {
            Name = "CloseOnOverlayClick",
            Type = "bool",
            DefaultValue = "true",
            Description = "Dismisses the Dialog when its overlay is clicked. Turn it off to refuse a stray click outside while the Escape key still closes the Dialog; a blocking Dialog refuses the click whatever this is set to."
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
            Name = "DefaultIsOpen",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The initial opening state of the Dialog in the uncontrolled mode, which is when the IsOpen parameter is not set. It is read once, at initialization, so closing such a Dialog is not undone by the next render."
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
            Description = "Determines the ARIA role of the Dialog (alertdialog/dialog). If this is set, it will override the ARIA role determined by Blocking and Modeless."
        },
        new()
        {
            Name = "Blocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the Dialog from being dismissed by a click on the overlay or by the Escape key, leaving its buttons as the only way out."
        },
        new()
        {
            Name = "IsCancelButtonEnabled",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the Cancel button of the Dialog can be pressed. Unlike Disabled, which turns the whole Dialog off, this leaves every other way out of the Dialog working."
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
            Name = "Modeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the Dialog). If true, Blocking is ignored, there will be no overlay, and the focus is not trapped - though the Dialog still takes it when it opens unless AutoFocus is turned off."
        },
        new()
        {
            Name = "IsOkButtonEnabled",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the Ok button of the Dialog can be pressed. This is what holds the answer shut until the content of the Dialog provides it - a consent to tick, a name to type - without turning the rest of the Dialog off the way Disabled would."
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
            Description = "Keeps the Dialog in the DOM while it is closed, hidden, instead of removing it - so its content, and whatever state it holds, survives until the next showing. Nothing is rendered until the first time it opens, so a Dialog that is never opened still costs nothing."
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
            Description = "The CSS maximum width of the Dialog surface. Defaults to the narrower of 100% of the area the Dialog is positioned in and --bit-Dialog-max-width (the --bit-siz-dialog-max-width theme token unless set), and setting it replaces that default rather than adding to it - min(100%, 32rem) is the whole of a responsive Dialog."
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
            Description = "A callback function for when a dismissal was refused: the Escape key or a click on the overlay the Dialog does not take (CloseOnEscape, CloseOnOverlayClick, Blocking), or a closing OnDismissing turned down. The Dialog shakes on its own; this is for saying why.",
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
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the element whose scrolling the Dialog holds while it is open, for the layouts whose scroller is not the page itself. A Dialog inside a BitAppShell holds the shell's scroller without being told to; the page (body) is what is held when there is no shell and this is not set."
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

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Dialog-z-index",
            DefaultValue = "--bit-zin-modal",
            Description = "Stacking order of the full-screen Dialog. An AbsolutePosition Dialog stacks inside its own area at auto and does not read it; give one a z-index through its own Class or Style.",
        },
        new()
        {
            Name = "--bit-Dialog-margin",
            DefaultValue = "0",
            Description = "Space kept between the surface and the edges of its area, for every Position and every size - a gutter on phones, where the surface otherwise reaches the edges.",
        },
        new()
        {
            Name = "--bit-Dialog-overlay-background",
            DefaultValue = "--bit-clr-bg-overlay",
            Description = "Color of the overlay behind the surface.",
        },
        new()
        {
            Name = "--bit-Dialog-overlay-backdrop-filter",
            DefaultValue = "none",
            Description = "Filter applied to the page behind the overlay, e.g. blur(4px).",
        },
        new()
        {
            Name = "--bit-Dialog-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of the surface, its header, its buttons band and its footer.",
        },
        new()
        {
            Name = "--bit-Dialog-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the surface, which the title, the close button and custom content inherit.",
        },
        new()
        {
            Name = "--bit-Dialog-border-width",
            DefaultValue = "0",
            Description = "Thickness of the border around the surface.",
        },
        new()
        {
            Name = "--bit-Dialog-border-color",
            DefaultValue = "transparent",
            Description = "Color of the border around the surface.",
        },
        new()
        {
            Name = "--bit-Dialog-radius",
            DefaultValue = "--bit-shp-radius-dialog",
            Description = "Corner radius of the surface and of the bands inside it.",
        },
        new()
        {
            Name = "--bit-Dialog-shadow",
            DefaultValue = "--bit-shd-dialog",
            Description = "Elevation of the surface.",
        },
        new()
        {
            Name = "--bit-Dialog-padding",
            DefaultValue = "--bit-spa-dialog",
            Description = "Inset of the header, the message and the buttons.",
        },
        new()
        {
            Name = "--bit-Dialog-max-width",
            DefaultValue = "--bit-siz-dialog-max-width",
            Description = "Width the surface stops growing at on its own. Ignored by a Dialog given a Width, MaxWidth, FullWidth or FullSize.",
        },
        new()
        {
            Name = "--bit-Dialog-text-align",
            DefaultValue = "--bit-layout-dialog-text-align",
            Description = "Alignment of the title, the subtitle and the message (start, or center under Cupertino).",
        },
        new()
        {
            Name = "--bit-Dialog-title-color",
            DefaultValue = "inherit",
            Description = "Color of the title.",
        },
        new()
        {
            Name = "--bit-Dialog-title-font-size",
            DefaultValue = "--bit-tpg-dialog-title-font-size",
            Description = "Font size of the title.",
        },
        new()
        {
            Name = "--bit-Dialog-title-font-weight",
            DefaultValue = "--bit-tpg-dialog-title-font-weight",
            Description = "Font weight of the title.",
        },
        new()
        {
            Name = "--bit-Dialog-subtitle-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the subtitle.",
        },
        new()
        {
            Name = "--bit-Dialog-message-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the message.",
        },
    ];



    private bool isOpenBasic;

    private bool isOpenLabels;
    private bool isOpenAcknowledge;
    private bool isOpenGated;
    private bool agreed;

    private bool isOpenSubtitle;
    private bool isOpenHeaderTemplate;
    private bool isOpenFooterTemplate;

    private bool isOpenCustom;
    private string? optionValue;

    private bool isOpenResult;
    private BitDialog resultDialogRef = default!;
    private BitDialog awaitDialogRef = default!;
    private string awaitedResult = "-";

    private async Task ShowAndAwait()
    {
        var result = await awaitDialogRef.Show();

        awaitedResult = result?.ToString() ?? "(dismissed)";
    }

    private bool isOpenEvents;
    private string lastEvent = "-";

    private async Task HandleSlowOk()
    {
        lastEvent = "OnOk (working...)";

        await Task.Delay(1000);

        lastEvent = "OnOk";
    }

    private bool isOpenBlocking;
    private bool isOpenNoOverlayClick;
    private bool isOpenNoEscape;
    private bool isOpenModeless;
    private string? preventedHint;

    private bool hasUnsavedChanges = true;
    private bool isOpenGuarded;
    private string? guardedHint;
    private string refusedGesture = "-";

    private void HandleDismissing(BitDialogDismissArgs args)
    {
        // Save is the way out that is always let through, so the Dialog is never a trap.
        args.Cancel = hasUnsavedChanges && args.Reason is not BitDialogDismissReason.OkButton;
    }

    private bool isOpenFocus;
    private bool isOpenNoFocus;
    private bool isOpenFocusCancel;
    private bool isOpenFocusSelector;

    private bool isOpenPosition;
    private BitDialogPosition position;
    private readonly BitDialogPosition[] dialogPositions =
    [
        BitDialogPosition.TopLeft, BitDialogPosition.TopCenter, BitDialogPosition.TopRight,
        BitDialogPosition.CenterLeft, BitDialogPosition.Center, BitDialogPosition.CenterRight,
        BitDialogPosition.BottomLeft, BitDialogPosition.BottomCenter, BitDialogPosition.BottomRight,
    ];

    private void OpenDialogInPosition(BitDialogPosition value)
    {
        position = value;
        isOpenPosition = true;
    }

    private bool isOpenScrollLock;
    private bool isOpenAbsolute;

    private bool isDraggable = true;
    private bool isOpenDraggable;
    private bool isOpenDragHandle;

    private bool isOpenOuter;
    private bool isOpenInner;

    private readonly List<BitDropdownItem<string>> audienceItems =
    [
        new() { Text = "Everyone", Value = "all" },
        new() { Text = "Editors", Value = "editors" },
        new() { Text = "Reviewers", Value = "reviewers" },
        new() { Text = "Nobody", Value = "none" }
    ];

    private bool isOpenKeptMounted;
    private bool isOpenUnmounted;

    private BitDialog programmaticDialogRef = default!;

    private bool isOpenCascadedFile;
    private bool isOpenCascadedFolder;

    private readonly BitDialogParams[] dialogParams =
    [
        new()
        {
            OkText = "Delete",
            CancelText = "Keep",
            ShowCloseButton = false,
            CloseOnOverlayClick = false,
            AutoFocusButton = BitDialogButton.Cancel,
            Position = BitDialogPosition.TopCenter,
        }
    ];

    private bool isOpenColor;
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
    }

    private bool isOpenIconName;
    private bool isOpenIconFa;
    private bool isOpenIconBi;
    private bool isOpenIconCss;

    private bool isOpenWidth;
    private bool isOpenResponsive;
    private bool isOpenHeight;
    private bool isOpenFullWidth;
    private bool isOpenFullSize;

    private bool isOpenStyles;
    private bool isOpenClasses;
    private bool isOpenCssVariables;

    private bool isOpenRtl;
    private bool isOpenRtlStart;
    private bool isOpenRtlLeft;
}
