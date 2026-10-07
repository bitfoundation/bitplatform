namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Modal;

public partial class BitModalDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            DefaultValue = "false",
            Description = "Positions the Modal absolute instead of fixed, so it covers the element it is declared in (which needs position: relative) rather than the screen.",
        },
        new()
        {
            Name = "AriaModal",
            Type = "bool",
            DefaultValue = "true",
            Description = "Announces the Modal as modal to assistive technologies. A Modal that is not modal leaves the page behind it reachable with the keyboard (no focus trap, no scroll lock).",
        },
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Takes the overflow off the scroller (ScrollerElement, ScrollerSelector, the BitAppShell's, or the page) while the Modal is open, instead of the default scroll lock.",
        },
        new()
        {
            Name = "Blocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents a click on the overlay from dismissing the Modal. Escape still dismisses it unless NoDismissOnEscape is set too.",
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the body section, an alias of ChildContent that takes precedence over it.",
        },
        new()
        {
            Name = "CanClose",
            Type = "Func<Task<bool>>?",
            DefaultValue = "null",
            Description = "Asked before the user dismisses the Modal (close button, overlay, Escape); answering false keeps it open. Not asked when the app closes it (Close, IsOpen).",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Modal, it can be any custom tag or text.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitModalClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitModal component.",
            LinkType = LinkType.Link,
            Href = "#modal-class-styles",
        },
        new()
        {
            Name = "CloseButtonTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title and aria-label of the close button, for accessibility and localization. Defaults to \"Close\".",
        },
        new()
        {
            Name = "CloseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the close button from an external icon library. Takes precedence over CloseIconName.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "CloseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the close button icon from the built-in Fluent UI icons. Defaults to Cancel.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "DefaultIsOpen",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The initial open state when IsOpen is not set (uncontrolled mode).",
        },
        new()
        {
            Name = "DragElementSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the drag handle of a Draggable Modal. The whole content by default.",
        },
        new()
        {
            Name = "Draggable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the user drag the Modal around.",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template of the footer section. Takes precedence over FooterText.",
        },
        new()
        {
            Name = "FooterText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the footer section.",
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Modal as tall as the area it covers.",
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Modal as wide and as tall as the area it covers (FullWidth + FullHeight).",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Modal as wide as the area it covers.",
        },
        new()
        {
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template of the header section. Takes precedence over HeaderText. It does not name the dialog by itself: point TitleAriaId at the title inside it.",
        },
        new()
        {
            Name = "HeaderText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the header section, announced as a level-2 heading. Names the dialog unless TitleAriaId or AriaLabel is set.",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS height of the Modal (any CSS length). Wins over FullHeight; capped by MaxHeight or the screen.",
        },
        new()
        {
            Name = "IsAlert",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Renders the Modal as an alertdialog instead of a dialog. When not set, a Blocking Modal that is not Modeless is an alertdialog.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Modal is displayed.",
        },
        new()
        {
            Name = "KeepMounted",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the Modal when closed instead of removing it, so its content keeps its state. Nothing renders before the first open; a closed kept Modal is inert.",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS height the Modal does not grow past (any CSS length); it scrolls inside itself beyond it. The screen height when not set.",
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS width the Modal does not grow past (any CSS length). The screen width when not set.",
        },
        new()
        {
            Name = "ModeFull",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gives the overlay an opaque background that dims the page behind the Modal.",
        },
        new()
        {
            Name = "Modeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the page usable: no overlay, no focus trap, no scroll lock, and not announced as modal. Blocking is ignored.",
        },
        new()
        {
            Name = "NoAutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the focus where it was when the Modal opens. By default it moves to the element marked data-autofocus (or autofocus), the first focusable element, or the content itself.",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the accent border along the top edge of the Modal.",
        },
        new()
        {
            Name = "NoDismissOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the Escape key from dismissing the Modal.",
        },
        new()
        {
            Name = "NoFocusTrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets Tab move the focus out of the Modal while it is open.",
        },
        new()
        {
            Name = "NoRestoreFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the focus where it ends up when the Modal closes instead of returning it to the element that opened it.",
        },
        new()
        {
            Name = "NoScrollLock",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the page scrolling while the Modal is open. By default it is held still without a layout shift, and the holds of several open Modals are counted.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Invoked whenever the Modal closes, whether the user dismissed it or the app closed it. Not invoked for a dismissal CanClose turns down.",
        },
        new()
        {
            Name = "OnEscapeKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Invoked for every Escape pressed inside the Modal, including the ones it refuses to be dismissed by.",
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "Invoked once the Modal has opened, rendered and placed the focus.",
        },
        new()
        {
            Name = "OnOverlayClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Invoked for every click on the overlay, including the ones a Blocking Modal refuses to be dismissed by.",
        },
        new()
        {
            Name = "Position",
            Type = "BitPosition?",
            DefaultValue = "null",
            Description = "Where the Modal sits in the area it covers. The center when not set.",
            LinkType = LinkType.Link,
            Href = "#position-enum",
        },
        new()
        {
            Name = "ScrollerElement",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "The scroller the Modal holds while it is open. Takes precedence over ScrollerSelector and the BitAppShell's scroller.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the scroller the Modal holds while it is open, for layouts that scroll a region of their own. The BitAppShell's scroller, or the page, when not set.",
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows a close button in the header that dismisses the Modal.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitModalClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitModal component.",
            LinkType = LinkType.Link,
            Href = "#modal-class-styles",
        },
        new()
        {
            Name = "SubtitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element that describes the Modal (aria-describedby).",
        },
        new()
        {
            Name = "TitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element that names the Modal (aria-labelledby). Wins over the header.",
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS width of the Modal (any CSS length). Wins over FullWidth; capped by MaxWidth or the screen.",
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Open",
            Type = "Task",
            Description = "Opens the Modal.",
        },
        new()
        {
            Name = "Close",
            Type = "Task",
            Description = "Closes the Modal. CanClose is not asked.",
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Toggles the Modal between its open and closed states.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "modal-class-styles",
            Title = "BitModalClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitModal."
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overlay of the BitModal."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the content of the BitModal."
               },
               new()
               {
                   Name = "HeaderContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header container of the BitModal."
               },
               new()
               {
                   Name = "Header",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header of the BitModal."
               },
               new()
               {
                   Name = "CloseButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the close button of the BitModal."
               },
               new()
               {
                   Name = "CloseIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the close icon of the BitModal."
               },
               new()
               {
                   Name = "Body",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the body of the BitModal."
               },
               new()
               {
                   Name = "Footer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the footer of the BitModal."
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
        DemoSharedEnums.BitPosition(description: "Where the Modal sits inside the area it covers. Start and End follow the text direction; Left and Right stay on their side.")
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Modal-z-index",
            DefaultValue = "--bit-zin-modal",
            Description = "Stacking order of a Modal fixed to the screen, to keep it above or below the app's own layers.",
        },
        new()
        {
            Name = "--bit-Modal-offset",
            DefaultValue = "0px",
            Description = "Room kept between the Modal and the edges of the area it covers, including in FullWidth / FullHeight.",
        },
        new()
        {
            Name = "--bit-Modal-max-width",
            DefaultValue = "100%",
            Description = "Widest the Modal grows; never wider than the area less the offset. The MaxWidth parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Modal-max-height",
            DefaultValue = "100%",
            Description = "Tallest the Modal grows before it scrolls; never taller than the area less the offset. The MaxHeight parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Modal-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Fill of the surface, including the sticky header and footer.",
        },
        new()
        {
            Name = "--bit-Modal-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the surface, which the close button follows.",
        },
        new()
        {
            Name = "--bit-Modal-radius",
            DefaultValue = "--bit-shp-radius-dialog",
            Description = "Corner radius of the surface.",
        },
        new()
        {
            Name = "--bit-Modal-shadow",
            DefaultValue = "--bit-shd-dialog",
            Description = "Elevation of the surface.",
        },
        new()
        {
            Name = "--bit-Modal-border-color",
            DefaultValue = "--bit-clr-pri",
            Description = "Color of the accent along the top edge (removed by NoBorder).",
        },
        new()
        {
            Name = "--bit-Modal-border-width",
            DefaultValue = "4px",
            Description = "Thickness of the accent along the top edge.",
        },
        new()
        {
            Name = "--bit-Modal-overlay-background",
            DefaultValue = "--bit-clr-bg-overlay",
            Description = "Fill of the overlay in ModeFull.",
        },
        new()
        {
            Name = "--bit-Modal-overlay-backdrop-filter",
            DefaultValue = "none",
            Description = "Filter applied to the page behind the overlay, e.g. blur(4px).",
        },
        new()
        {
            Name = "--bit-Modal-padding",
            DefaultValue = "--bit-spa-dialog",
            Description = "Inner padding of the header, body and footer of the chrome.",
        },
        new()
        {
            Name = "--bit-Modal-header-font-size",
            DefaultValue = "--bit-tpg-fs-xl",
            Description = "Text size of the header.",
        },
        new()
        {
            Name = "--bit-Modal-header-font-weight",
            DefaultValue = "--bit-tpg-fw-semibold",
            Description = "Weight of the header.",
        },
    ];



    private bool isOpenBasic;
    private bool isOpenNoBorder;

    private bool isOpenCustomContent;
    private bool isOpenHeaderText;
    private bool isOpenHeaderTemplate;
    private bool isOpenFooter;

    private bool isOpenBlocking;
    private bool isOpenNoEscape;
    private bool isOpenCanClose;
    private string? editorName;
    private void OpenCanClose()
    {
        editorName = null;
        isOpenCanClose = true;
    }
    private Task<bool> CanCloseEditor() => Task.FromResult(string.IsNullOrEmpty(editorName));
    private void DiscardEditor()
    {
        editorName = null;
        isOpenCanClose = false;
    }

    private bool isOpenFocus;
    private bool isOpenNoFocus;
    private bool isOpenAutoFocus;
    private readonly Dictionary<string, object> autoFocusAttributes = new() { { "data-autofocus", true } };

    private bool isOpenScrollLock;
    private bool isOpenNoScrollLock;
    private bool isOpenAutoToggleScroll;

    private bool isOpenMaxWidth;
    private bool isOpenMaxHeight;
    private bool isOpenFixedSize;
    private bool isOpenFullWidth;
    private bool isOpenFullHeight;
    private bool isOpenFullSize;

    private bool isOpenModeFull;
    private bool isOpenModeless;

    private bool isOpenPosition;
    private BitPosition position = BitPosition.Center;
    private void OpenModalInPosition(BitPosition positionValue)
    {
        position = positionValue;
        isOpenPosition = true;
    }
    private bool isOpenAbsolute;
    private bool isOpenAbsoluteScroller;

    private bool isOpenDraggable;
    private bool isOpenDragHandle;

    private bool isOpenAutoNamed;
    private bool isOpenLabelled;
    private bool isOpenAlert;

    private bool isOpenKeptMounted;
    private bool isOpenNotKeptMounted;

    private bool isEventsOpen;
    private bool isOpened;
    private int openedVersion;
    private bool isDismissed;
    private bool isOverlayClicked;
    private bool isEscapePressed;
    private async Task HandleOnOpen()
    {
        // Each open starts a new countdown: a reopen within the 3 seconds must not be cleared by the reset of the previous one.
        var version = ++openedVersion;
        isOpened = true;
        await Task.Delay(3000);
        if (version != openedVersion) return;
        isOpened = false;
        StateHasChanged();
    }
    private async Task HandleOnDismiss()
    {
        isDismissed = true;
        await Task.Delay(3000);
        isDismissed = false;
    }
    private async Task HandleOnOverlayClick()
    {
        isOverlayClicked = true;
        await Task.Delay(2000);
        isOverlayClicked = false;
    }
    private async Task HandleOnEscapeKeyDown()
    {
        isEscapePressed = true;
        await Task.Delay(2000);
        isEscapePressed = false;
    }

    private BitModal refModal = default!;

    private bool isOpenOuter;
    private bool isOpenInner;
    private void HandleNestedDelete()
    {
        isOpenInner = false;
        isOpenOuter = false;
    }

    private bool isOpenCascaded;
    private bool isOpenCascadedOwn;
    private bool isOpenUncascaded;
    private readonly BitModalParams[] modalParams =
    [
        new()
        {
            ModeFull = true,
            ShowCloseButton = true,
            MaxWidth = "26rem",
            Position = BitPosition.TopCenter,
        }
    ];

    private bool isOpenExternalIcon;

    private bool isOpenStyle;
    private bool isOpenClass;
    private bool isOpenStyles;
    private bool isOpenClasses;
    private bool isOpenCssVars;

    private bool isOpenRtl;
}
