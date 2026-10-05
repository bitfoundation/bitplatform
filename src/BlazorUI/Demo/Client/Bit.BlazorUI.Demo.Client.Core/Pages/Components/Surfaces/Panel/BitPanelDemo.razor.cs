namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Panel;

public partial class BitPanelDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the panel and its overlay out against the nearest positioned ancestor instead of the screen. The page scroll is then left alone; a named scroller is still locked.",
        },
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Toggles the overflow of the scroller while the panel is open, instead of taking the scroll lock. An AbsolutePosition panel is pushed down by the room the scrollbar gave back.",
        },
        new()
        {
            Name = "Blocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps a click on the overlay from dismissing the panel. Escape and the swipe are controlled by NoDismissOnEscape and NoSwipe.",
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent, named for the scrolling body between the header and the footer.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the panel.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the panel.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "CloseButtonTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name and tooltip of the close button. Defaults to \"Close\".",
        },
        new()
        {
            Name = "CloseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the close button from an external icon library, given as its CSS classes. Takes precedence over CloseIconName.",
        },
        new()
        {
            Name = "CloseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in Fluent UI icon of the close button. Defaults to Cancel.",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The footer of the panel, fixed at its far edge while the body scrolls.",
        },
        new()
        {
            Name = "FooterText",
            Type = "string?",
            DefaultValue = "null",
            Description = "A plain-text footer. Footer takes precedence over it.",
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stretches the panel over the whole screen, overriding Size and the size cap.",
        },
        new()
        {
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The header of the panel, fixed at the edge it slides in from while the body scrolls. It names the panel for screen readers unless TitleAriaId or AriaLabel is set.",
        },
        new()
        {
            Name = "HeaderText",
            Type = "string?",
            DefaultValue = "null",
            Description = "A plain-text header, rendered as a level-2 heading. Header takes precedence over it.",
        },
        new()
        {
            Name = "IsAlert",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reports the panel as an alertdialog instead of a dialog, for urgent content the user has to deal with.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the panel is open. Two-way bindable: the panel writes it back when it dismisses itself.",
        },
        new()
        {
            Name = "KeepMounted",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the content in the page after the first opening, hidden while closed, so its state survives a close. Nothing is rendered before the first opening either way.",
        },
        new()
        {
            Name = "ModeFull",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gives the overlay a background that dims the page.",
        },
        new()
        {
            Name = "Modeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders no overlay, so the page stays usable. A modeless panel is not reported as modal, does not trap the focus and does not lock the scroll.",
        },
        new()
        {
            Name = "NoAutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the focus where it is when the panel opens. Otherwise the element marked data-autofocus, or the first focusable one, takes it.",
        },
        new()
        {
            Name = "NoDismissOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the Escape key from dismissing the panel. OnEscapeKeyDown still fires. An Escape that closes a popup inside the panel (an open dropdown list) never reaches the panel either way.",
        },
        new()
        {
            Name = "NoFocusTrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets Tab leave the open panel. A Modeless panel never traps the focus.",
        },
        new()
        {
            Name = "NoRestoreFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the focus where it is when the panel closes, instead of returning it to the element that had it before the panel opened.",
        },
        new()
        {
            Name = "NoScrollLock",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the page scrolling while the panel is open. Wheel and touch gestures on the overlay are forwarded to the named or app-shell scroller.",
        },
        new()
        {
            Name = "NoSwipe",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns off the swipe gesture that dismisses the panel. To keep it but exempt one region (a canvas, a sideways-scrolling table), mark that region data-no-swipe instead; fields and mouse text selection are always exempt.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Fires whenever the panel closes: close button, overlay, Escape, swipe, Close/Toggle, or IsOpen set to false from outside. Carries the click where there was one.",
        },
        new()
        {
            Name = "OnDismissing",
            Type = "EventCallback<BitPanelDismissArgs>",
            Description = "Fires before the panel closes itself; set Cancel to keep it open, and read Reason to tell the closings apart. Not raised when IsOpen is set to false from outside.",
            Href = "#dismiss-args",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "OnEscapeKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Fires for every Escape pressed inside the open panel, including the ones NoDismissOnEscape refuses - but not for one that closes a popup inside it.",
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "Fires when the panel opens.",
        },
        new()
        {
            Name = "OnOverlayClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Fires for a click on the overlay, before the panel is dismissed - and for a Blocking panel too.",
        },
        new()
        {
            Name = "OnSwipeStart",
            Type = "EventCallback<decimal>",
            Description = "Fires when a swipe starts on the panel, with the start coordinate along its axis.",
        },
        new()
        {
            Name = "OnSwipeMove",
            Type = "EventCallback<decimal>",
            Description = "Fires while a swipe moves, with the distance along the panel's axis.",
        },
        new()
        {
            Name = "OnSwipeEnd",
            Type = "EventCallback<decimal>",
            Description = "Fires when a swipe ends, with the distance along the panel's axis.",
        },
        new()
        {
            Name = "OnToggle",
            Type = "EventCallback<bool>",
            Description = "Fires when the panel opens or closes, with the new state.",
        },
        new()
        {
            Name = "OnTransitionEnd",
            Type = "EventCallback<bool>",
            Description = "Fires once the panel has finished sliding in or out, with the state it settled in. The other callbacks fire at the start of the movement.",
        },
        new()
        {
            Name = "Placement",
            Type = "BitPlacement?",
            DefaultValue = "null",
            Description = "The edge the panel slides in from; Start and End follow the text direction, Left and Right stay where they are named in both. Center and the two combined values fall back to End. Defaults to End.",
            Href = "#placement-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "Replaces the dialog (or alertdialog) role, e.g. complementary or region for a Modeless panel beside the page.",
        },
        new()
        {
            Name = "ScrollerElement",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "The scroller to lock while the panel is open, when no selector can reach it. Takes precedence over ScrollerSelector and the BitAppShell scroller.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the scroller to lock while the panel is open. Defaults to the BitAppShell scroller, or the page.",
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows a close button at the end of the header row.",
        },
        new()
        {
            Name = "Size",
            Type = "double?",
            DefaultValue = "null",
            Description = "The size in pixels along the axis the panel slides on (the width at Start/End/Left/Right, the height at Top/Bottom). Unset, the panel fits its content; other units go through --bit-Panel-size or Styles.Container.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the panel.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "SubtitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element that describes the panel (aria-describedby).",
        },
        new()
        {
            Name = "SwipeTrigger",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "How far the panel has to be dragged to be dismissed, as a fraction of its size (0 to 1, default 0.25).",
        },
        new()
        {
            Name = "TitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element that names the panel (aria-labelledby). Defaults to the header; AriaLabel takes precedence.",
        },
        new()
        {
            Name = "ZIndex",
            Type = "int?",
            DefaultValue = "null",
            Description = "The layer of the overlay; the panel sits one above it. A panel declared inside another needs none; this lifts one over a sibling panel or page chrome.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Open",
            Type = "Task",
            Description = "Opens the panel, unless it is disabled.",
        },
        new()
        {
            Name = "Close",
            Type = "Task",
            Description = "Closes the panel, unless OnDismissing refuses it.",
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Opens the panel when it is closed, and closes it when it is open.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Panel-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Fill of the panel.",
        },
        new()
        {
            Name = "--bit-Panel-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the panel and its close button.",
        },
        new()
        {
            Name = "--bit-Panel-shadow",
            DefaultValue = "--bit-shd-sheet",
            Description = "Elevation of the panel.",
        },
        new()
        {
            Name = "--bit-Panel-radius",
            DefaultValue = "--bit-shp-radius-sheet",
            Description = "Radius of the two corners the panel turns towards the page. FullSize panels stay square.",
        },
        new()
        {
            Name = "--bit-Panel-border-width",
            DefaultValue = "0",
            Description = "Width of the rule along the edge the panel turns towards the page.",
        },
        new()
        {
            Name = "--bit-Panel-border-color",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "Color of that rule.",
        },
        new()
        {
            Name = "--bit-Panel-size",
            DefaultValue = "fit-content",
            Description = "Size along the axis the panel slides on, in any CSS unit. The Size parameter wins.",
        },
        new()
        {
            Name = "--bit-Panel-max-size",
            DefaultValue = "85%",
            Description = "Cap of that size, which keeps a strip of the page visible. FullSize lifts it.",
        },
        new()
        {
            Name = "--bit-Panel-z-index",
            DefaultValue = "--bit-zin-overlay",
            Description = "Layer of the overlay; the panel sits one above it. The ZIndex parameter wins.",
        },
        new()
        {
            Name = "--bit-Panel-overlay-background",
            DefaultValue = "--bit-clr-bg-overlay",
            Description = "Fill of the overlay of a ModeFull panel.",
        },
        new()
        {
            Name = "--bit-Panel-overlay-backdrop-filter",
            DefaultValue = "none",
            Description = "Filter applied to the page behind the overlay, e.g. blur(4px).",
        },
        new()
        {
            Name = "--bit-Panel-padding",
            DefaultValue = "--bit-spa-dialog",
            Description = "Inset of the header, the body and the footer.",
        },
        new()
        {
            Name = "--bit-Panel-header-font-size",
            DefaultValue = "--bit-tpg-fs-xl",
            Description = "Text size of the header.",
        },
        new()
        {
            Name = "--bit-Panel-header-font-weight",
            DefaultValue = "--bit-tpg-fw-semibold",
            Description = "Weight of the header.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitPanelClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitPanel."
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overlay of the BitPanel."
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the BitPanel, which is the panel surface itself."
               },
               new()
               {
                   Name = "HeaderContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header row of the BitPanel, which holds the header beside the close button."
               },
               new()
               {
                   Name = "Header",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header of the BitPanel."
               },
               new()
               {
                   Name = "CloseButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the close button of the BitPanel."
               },
               new()
               {
                   Name = "CloseIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the close button of the BitPanel."
               },
               new()
               {
                   Name = "Body",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the body of the BitPanel, which scrolls between the header and the footer."
               },
               new()
               {
                   Name = "Footer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the footer of the BitPanel."
               }
            ]
        },
        new()
        {
            Id = "dismiss-args",
            Title = "BitPanelDismissArgs",
            Parameters =
            [
               new()
               {
                   Name = "Reason",
                   Type = "BitPanelDismissReason",
                   DefaultValue = "",
                   Description = "What is closing the panel.",
                   Href = "#dismiss-reason-enum",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "Mouse",
                   Type = "MouseEventArgs?",
                   DefaultValue = "null",
                   Description = "The click that is closing the panel, for a dismissal that came from a pointer."
               },
               new()
               {
                   Name = "Cancel",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Set to true to refuse the dismissal and keep the panel open."
               }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        SharedSubEnums.BitPlacement,
        new()
        {
            Id = "dismiss-reason-enum",
            Name = "BitPanelDismissReason",
            Description = "What is closing the panel, reported to OnDismissing.",
            Items =
            [
                new() { Name = "Programmatic", Description = "The Close or Toggle method.", Value = "0" },
                new() { Name = "Overlay", Description = "A click on the overlay.", Value = "1" },
                new() { Name = "Escape", Description = "The Escape key.", Value = "2" },
                new() { Name = "Swipe", Description = "A swipe towards the edge the panel slid in from.", Value = "3" },
                new() { Name = "CloseButton", Description = "The close button in the header.", Value = "4" }
            ]
        }
    ];



    private bool isBasicPanelOpen;
    private BitPanel basicPanelRef = default!;

    private bool isHeaderTextPanelOpen;
    private bool isTemplatePanelOpen;

    private BitPlacement panelPosition = BitPlacement.End;
    private double panelSize = 300;
    private bool panelFullSize;
    private bool isPositionPanelOpen;

    private bool overlayModeFull;
    private bool overlayBlocking;
    private bool overlayNoEscape;
    private bool overlayModeless;
    private int overlayClickCount;
    private int escapeKeyCount;
    private int dismissCount;
    private bool isOverlayPanelOpen;
    private readonly List<BitDropdownItem<string>> dismissalItems =
    [
        new() { Text = "A", Value = "A" },
        new() { Text = "B", Value = "B" },
        new() { Text = "C", Value = "C" },
    ];

    private bool guardPanel = true;
    private bool guardedRefused;
    private BitPanelDismissReason? guardedReason;
    private bool isGuardedPanelOpen;
    private BitPanel guardedPanelRef = default!;

    private bool a11yNoAutoFocus;
    private bool a11yNoFocusTrap;
    private bool a11yNoRestoreFocus;
    private bool a11yIsAlert;
    private bool isA11yPanelOpen;

    private string scrollMode = "Lock";
    private bool isScrollPanelOpen;

    private double swipeTrigger = 0.25;
    private bool noSwipe;
    private decimal swipeStart;
    private decimal swipeDiff;
    private bool isSwipePanelOpen;

    private bool isOuterPanelOpen;
    private bool isInnerPanelOpen;
    private bool isSiblingPanelOpen;

    private bool isAbsolutePanelOpen;

    private bool keepMounted;
    private int openCount;
    private bool lastToggleState;
    private bool lastSettledState;
    private bool isRenderPanelOpen;

    private readonly BitPanelParams[] panelParams =
    [
        new()
        {
            Placement = BitPlacement.Start,
            Size = 320,
            ModeFull = true,
            ShowCloseButton = true,
        }
    ];
    private bool isCascadedPanelOpen;
    private bool isOverridingPanelOpen;

    private bool isExternalIconPanelOpen;
    private bool isIconNamePanelOpen;

    private bool isStylesPanelOpen;
    private bool isClassesPanelOpen;
    private bool isCssVarsPanelOpen;

    private bool isRtlPanelOpenStart;
    private bool isRtlPanelOpenEnd;

    // The gestures that could be a slip are refused; the close button and the panel's own Save go through.
    private void HandleOnDismissing(BitPanelDismissArgs args)
    {
        guardedReason = args.Reason;
        args.Cancel = guardPanel && args.Reason is BitPanelDismissReason.Overlay
                                                or BitPanelDismissReason.Escape
                                                or BitPanelDismissReason.Swipe;
        guardedRefused = args.Cancel;
    }
}
