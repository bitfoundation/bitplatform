using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Overlay;

public partial class BitOverlayDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            DefaultValue = "false",
            Description = "Covers the element the Overlay is declared in (which needs position: relative) instead of the screen, taking its rounded corners.",
        },
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the scroller behind the Overlay while it is open, without a layout shift, and hands it back once the last Overlay holding it closes. The scroller is ScrollerElement, then ScrollerSelector, then the scroller of the BitAppShell the Overlay is in, then the page (body).",
        },
        new()
        {
            Name = "Blocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the Overlay open on a click on the layer and on Escape. The click is still reported through OnClick.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Overlay. A click on it never closes the Overlay.",
        },
        new()
        {
            Name = "DefaultIsOpen",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The state an uncontrolled Overlay (IsOpen not set) starts in.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Overlay is shown; bindable, so a dismissal is reported back. A closed Overlay is inert, even while it fades out.",
        },
        new()
        {
            Name = "ModeFull",
            Type = "bool",
            DefaultValue = "false",
            Description = "Dims what the Overlay covers with the theme's overlay color (--bit-Overlay-background). The layer is transparent otherwise.",
        },
        new()
        {
            Name = "NoDismissOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the Overlay open on Escape, while a click on the layer still closes it.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Called for every click on an open Overlay - its content and the clicks a Blocking Overlay refuses included - before it closes.",
        },
        new()
        {
            Name = "OnClose",
            Type = "EventCallback",
            Description = "Called once the Overlay has closed, however it was closed, after the scroller it held has been handed back.",
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "Called once the Overlay has opened, however it was opened, after the scroller it holds has been taken.",
        },
        new()
        {
            Name = "Position",
            Type = "BitPosition?",
            DefaultValue = "null",
            Description = "Where the content is placed on the layer. The content stretches over the whole layer when it is not set.",
            LinkType = LinkType.Link,
            Href = "#position-enum",
        },
        new()
        {
            Name = "ScrollerElement",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "The scroller AutoToggleScroll stops, for one a selector cannot name. Wins over ScrollerSelector.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the scroller AutoToggleScroll stops. An Overlay that leaves it scrolling hands it the wheel and the touch drag it catches.",
        },
        new()
        {
            Name = "ZIndex",
            Type = "int?",
            DefaultValue = "null",
            Description = "The stacking order of the Overlay, over the shared overlay layer (--bit-Overlay-z-index).",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Open",
            Type = "Task",
            Description = "Opens the Overlay, unless it is disabled.",
        },
        new()
        {
            Name = "Close",
            Type = "Task",
            Description = "Closes the Overlay, even a disabled one.",
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Opens the Overlay when it is closed, and closes it when it is open.",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        SharedSubEnums.BitPosition,
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Overlay-z-index",
            DefaultValue = "--bit-zin-overlay",
            Description = "Stacking order of an Overlay fixed to the screen. The ZIndex parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Overlay-background",
            DefaultValue = "--bit-clr-bg-overlay",
            Description = "Fill of the layer in ModeFull.",
        },
        new()
        {
            Name = "--bit-Overlay-backdrop-filter",
            DefaultValue = "none",
            Description = "Filter over what the layer covers, e.g. blur(4px) for a frosted layer.",
        },
        new()
        {
            Name = "--bit-Overlay-padding",
            DefaultValue = "0px",
            Description = "Room kept between the content and the edges of the layer.",
        },
        new()
        {
            Name = "--bit-Overlay-transition-duration",
            DefaultValue = "--bit-mot-duration-short",
            Description = "How long the layer takes to fade in and out. The default collapses under reduced motion; a value set here does not.",
        },
    ];



    private bool basicIsOpen;
    private bool modeFullIsOpen;

    private static readonly BitPosition[] positions =
    [
        BitPosition.TopStart, BitPosition.TopCenter, BitPosition.TopEnd,
        BitPosition.CenterStart, BitPosition.Center, BitPosition.CenterEnd,
        BitPosition.BottomStart, BitPosition.BottomCenter, BitPosition.BottomEnd,
    ];
    private bool positionIsOpen;
    private BitPosition position = BitPosition.Center;
    private void OpenAt(BitPosition value)
    {
        position = value;
        positionIsOpen = true;
    }

    private bool dismissalIsOpen;
    private bool dismissalBlocking;
    private bool dismissalNoEscape;
    private int dismissalClicks;

    private bool absoluteIsOpen;
    private async Task LoadReport()
    {
        if (absoluteIsOpen) return;

        absoluteIsOpen = true;
        await Task.Delay(3000);
        absoluteIsOpen = false;
    }

    private bool pageLockIsOpen;
    private bool boxScrollIsOpen;
    private bool boxLockIsOpen;

    private bool eventsIsOpen;
    private int eventsClicks;
    private int eventsOpened;
    private int eventsClosed;
    private void HandleOverlayClick(MouseEventArgs e)
    {
        if (++eventsClicks >= 3)
        {
            eventsIsOpen = false;
        }
    }
    private void HandleOverlayOpen()
    {
        eventsClicks = 0;
        eventsOpened++;
    }
    private void HandleOverlayClose() => eventsClosed++;

    private BitOverlay overlayRef = default!;
    private int openCount;
    private async Task OpenAndCloseLater()
    {
        var current = ++openCount;
        await overlayRef.Open();
        await Task.Delay(3000);
        if (current == openCount)
        {
            await overlayRef.Close();
        }
    }

    private bool cascadedIsOpen;
    private bool cascadedOwnIsOpen;
    private readonly BitOverlayParams[] overlayParams =
    [
        new() { ModeFull = true, Position = BitPosition.Center }
    ];

    private bool styleIsOpen;
    private bool classIsOpen;
    private bool cssVarsIsOpen;

    private bool rtlIsOpen;
}
