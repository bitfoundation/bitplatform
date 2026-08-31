using Microsoft.AspNetCore.Components.Web;

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
            Description = "Lays the panel out against the nearest positioned ancestor instead of against the screen, so that the panel - and the overlay that comes with it - stay inside a container of the page rather than covering all of it.",
        },
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Takes the overflow off the scroller itself while the panel is open and hands it back once it closes, instead of taking the counted hold the panel otherwise takes on the page - the two would else both be holding the same page. The room the scrollbar gave back is what an AbsolutePosition panel is pushed down by.",
        },
        new()
        {
            Name = "Blocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps a click on the overlay from dismissing the panel, for the panels whose content has to be completed or cancelled through the panel itself. It says nothing about the Escape key or the swipe gesture.",
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias for ChildContent, named for the body it becomes on a panel that was given a header or a footer to lay out around it.",
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
            Description = "The title and accessible name of the close button, which is what a screen reader reads out for it and what the pointer shows as its tooltip. It defaults to \"Close\".",
        },
        new()
        {
            Name = "CloseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the close button, given as the CSS classes of an external icon library. It takes precedence over CloseIconName.",
        },
        new()
        {
            Name = "CloseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in Fluent UI icon of the close button. It defaults to Cancel.",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The footer of the panel, which stays put at the far edge of it while the content between it and the header scrolls.",
        },
        new()
        {
            Name = "FooterText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the footer of the panel, for the footer that is nothing but a line of text. Footer takes precedence over it.",
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stretches the panel over the whole of the screen, which takes over from Size and from the cap that otherwise leaves a strip of the page showing beside it.",
        },
        new()
        {
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The header of the panel, which stays put at the edge the panel slid in from while the content below it scrolls. It is also what names the panel to a screen reader, unless TitleAriaId or AriaLabel names it instead.",
        },
        new()
        {
            Name = "HeaderText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the header of the panel, for the header that is nothing but a title. Header takes precedence over it.",
        },
        new()
        {
            Name = "IsAlert",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reports the panel to assistive technologies as an alert dialog rather than a plain one, for the panels that carry an urgent message the user is expected to deal with before carrying on.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the openness of the panel.",
        },
        new()
        {
            Name = "KeepMounted",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the content of the panel in the page once it has been opened, instead of taking it back out every time the panel closes. Nothing of it is rendered until the first opening either way.",
        },
        new()
        {
            Name = "ModeFull",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the overlay in full mode that gives it an opaque background. The overlay catches the clicks meant for the page behind it either way; this is what makes it dim that page as well.",
        },
        new()
        {
            Name = "Modeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the page its own clicks while the panel is open, by not rendering the overlay that otherwise covers it. A modeless panel does not report itself as a modal dialog and does not keep the keyboard inside itself.",
        },
        new()
        {
            Name = "NoAutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the focus where it is when the panel opens, instead of moving it into the panel. An element in the content marked with a data-autofocus attribute takes the focus instead of the first focusable one. The Escape key reaches the panel from wherever the keyboard is inside it, so a panel that never took the keyboard over is also one Escape does not reach until the user has tabbed or clicked into it.",
        },
        new()
        {
            Name = "NoDismissOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the Escape key from dismissing the panel, for the panels that are only meant to be closed through their own content.",
        },
        new()
        {
            Name = "NoFocusTrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the keyboard leave the panel while it is open, instead of cycling Tab and Shift+Tab inside it. A Modeless panel never traps the focus.",
        },
        new()
        {
            Name = "NoRestoreFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the focus wherever the panel left it when it closes, instead of handing it back to the element that had it before the panel opened. Nothing is recorded for a panel that hands nothing back.",
        },
        new()
        {
            Name = "NoScrollLock",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the page scrolling behind the open panel, instead of holding it still. A Modeless panel never holds the page anyway, and one doing its own scroll handling through AutoToggleScroll holds its scroller itself. The gestures that land on a panel holding nothing are handed on to the scroller it names.",
        },
        new()
        {
            Name = "NoSwipe",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns off the swipe gesture that otherwise dismisses the panel when it is dragged towards the edge it slid in from.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when the panel is dismissed. It is called for every closing of the panel: the close button, the overlay, the Escape key, a swipe, the Close and Toggle methods, and the IsOpen parameter being set to false from the outside.",
        },
        new()
        {
            Name = "OnDismissing",
            Type = "EventCallback<BitPanelDismissArgs>",
            Description = "A callback function invoked before the panel closes, which lets the closing be refused by setting Cancel on the arguments it is given, and tells the closings apart through their Reason. The IsOpen parameter being set to false from the outside never passes through it.",
            Href = "#dismiss-args",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "OnEscapeKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "A callback function for when the Escape key is pressed inside the panel. It is called for every Escape, including the ones a panel with NoDismissOnEscape refuses to be dismissed by, which makes it the counterpart of OnOverlayClick for the keyboard.",
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "A callback function for when the panel is opened.",
        },
        new()
        {
            Name = "OnOverlayClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when a click lands on the overlay of the panel. It is called before the panel is dismissed, and it is called for a Blocking panel too.",
        },
        new()
        {
            Name = "OnSwipeStart",
            Type = "EventCallback<decimal>",
            Description = "The event callback for when the swipe action starts on the container of the panel.",
        },
        new()
        {
            Name = "OnSwipeMove",
            Type = "EventCallback<decimal>",
            Description = "The event callback for when the swipe action moves on the container of the panel.",
        },
        new()
        {
            Name = "OnSwipeEnd",
            Type = "EventCallback<decimal>",
            Description = "The event callback for when the swipe action ends on the container of the panel.",
        },
        new()
        {
            Name = "OnToggle",
            Type = "EventCallback<bool>",
            Description = "A callback function for when the panel opens or closes, called with the new open state.",
        },
        new()
        {
            Name = "OnTransitionEnd",
            Type = "EventCallback<bool>",
            Description = "A callback function for when the panel has finished sliding in or out, called with the state it settled in. OnOpen, OnDismiss and OnToggle are called on the frame the panel changed state on, which is the start of the movement rather than the end of it.",
        },
        new()
        {
            Name = "Position",
            Type = "BitPanelPosition?",
            DefaultValue = "null",
            Description = "The edge of the screen the panel slides in from. Start and End are the logical edges, so they follow the direction of the panel. It defaults to End.",
            Href = "#position-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ARIA role the panel reports itself under, which takes over from the dialog it is announced as by default. It is for the panel that is not a dialog at all: a Modeless panel left beside the page is better announced as a complementary or a region.",
        },
        new()
        {
            Name = "Size",
            Type = "double?",
            DefaultValue = "null",
            Description = "The size of the panel in pixels along the axis it slides on: the width of a panel at the start or the end of the screen, and the height of one at the top or the bottom. A size that is not a pixel value is given through the Container member of Styles.",
        },
        new()
        {
            Name = "ScrollerElement",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "The element reference of the scroller whose scrolling is taken away while the panel is open, for the layouts whose scroller cannot be named by a selector. It takes precedence over ScrollerSelector, and over the scroller a BitAppShell cascades.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the element whose scrolling is held while the panel is open, for the layouts whose scroller is not the page itself. A panel inside a BitAppShell holds the shell's scroller without being told to; the body of the document is what is held when there is no shell and this is not set.",
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the close button of the panel, at the end of the header row. It is what a Blocking or a Modeless panel needs to be closable with the pointer at all.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the panel component.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "SubtitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ARIA id of the element that describes the panel, which is what a screen reader reads out after the name of the panel when it opens.",
        },
        new()
        {
            Name = "SwipeTrigger",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "How far the panel has to be dragged towards the edge it slid in from before it is dismissed, as a fraction of its own size (default is 0.25). Values outside of the range greater than zero and no more than one fall back to the default.",
        },
        new()
        {
            Name = "TitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ARIA id of the element that names the panel, which is what a screen reader reads out when the panel opens. It defaults to the Header of the panel, and AriaLabel takes precedence over both.",
        },
        new()
        {
            Name = "ZIndex",
            Type = "int?",
            DefaultValue = "null",
            Description = "The layer the panel and its overlay are stacked at, which takes over from the one the whole library shares. The overlay takes this value and the panel itself sits one above it, which is what a panel opened from inside another one needs.",
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
            Description = "Closes the panel. A panel that is already closed is left alone, and one whose OnDismissing refuses the closing stays open.",
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Opens the panel when it is closed, and closes it when it is open.",
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
                   Description = "Custom CSS classes/styles for the header container of the BitPanel, which holds the header beside the close button."
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
                   Description = "Custom CSS classes/styles for the body of the BitPanel, which is the part that scrolls between the header and the footer."
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
                   Description = "What is closing the panel: the close button, a click on the overlay, the Escape key, a swipe, or the code that opened it.",
                   Href = "#dismiss-reason-enum",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "Mouse",
                   Type = "MouseEventArgs?",
                   DefaultValue = "null",
                   Description = "The click that is closing the panel, which is only there for a dismissal that came from a pointer."
               },
               new()
               {
                   Name = "Cancel",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Set to true to refuse the dismissal and leave the panel open."
               }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "position-enum",
            Name = "BitPanelPosition",
            Description = "The edge of the screen the panel slides in from.",
            Items =
            [
                new() { Name = "Start", Description = "The logical start edge of the screen: the left in left-to-right, the right in right-to-left.", Value = "0" },
                new() { Name = "End", Description = "The logical end edge of the screen: the right in left-to-right, the left in right-to-left.", Value = "1" },
                new() { Name = "Top", Description = "The top edge of the screen.", Value = "2" },
                new() { Name = "Bottom", Description = "The bottom edge of the screen.", Value = "3" }
            ]
        },
        new()
        {
            Id = "dismiss-reason-enum",
            Name = "BitPanelDismissReason",
            Description = "What closed the panel, reported to OnDismissing.",
            Items =
            [
                new() { Name = "Programmatic", Description = "The code that opened the panel closed it, through the Close or Toggle method.", Value = "0" },
                new() { Name = "Overlay", Description = "The user clicked the overlay that covers the page behind the panel.", Value = "1" },
                new() { Name = "Escape", Description = "The user pressed the Escape key while the keyboard was inside the panel.", Value = "2" },
                new() { Name = "Swipe", Description = "The user swiped the panel towards the edge it slid in from.", Value = "3" },
                new() { Name = "CloseButton", Description = "The user clicked the close button the panel renders in its own header.", Value = "4" }
            ]
        }
    ];



    private bool isBasicPanelOpen;
    private BitPanel basicPanelRef = default!;
    private bool isCloseButtonPanelOpen;

    private bool isHeaderPanelOpen;
    private bool isHeaderTextPanelOpen;
    private bool isFooterTextPanelOpen;

    private double customPanelSize = 300;
    private bool isOpenInPositionStart;
    private bool isOpenPositionEnd;
    private bool isOpenInPositionTop;
    private bool isOpenInPositionBottom;
    private bool isFullSizePanelOpen;

    private int dismissCount;
    private int overlayClickCount;
    private int escapeKeyCount;
    private string lastDismissReason = "-";
    private bool isBlockingPanelOpen;
    private bool isModeFullPanelOpen;
    private bool isModelessPanelOpen;
    private bool isNoEscapePanelOpen;
    private BitPanel modelessPanelRef = default!;

    private bool guardPanel = true;
    private bool guardedRefused;
    private bool isGuardedPanelOpen;
    private BitPanelDismissReason? guardedReason;
    private BitPanel guardedPanelRef = default!;

    private bool isFocusPanelOpen;
    private bool isNoFocusPanelOpen;
    private bool isNoRestoreFocusPanelOpen;

    private bool isScrollLockPanelOpen;
    private bool isNoScrollLockPanelOpen;
    private bool isAutoToggleScrollPanelOpen;

    private double swipeTrigger = 0.25;
    private decimal swipeStart;
    private decimal swipeDiff;
    private bool isSwipePanelOpen;
    private bool isNoSwipePanelOpen;

    private bool isOuterPanelOpen;
    private bool isInnerPanelOpen;

    private bool isAbsolutePanelOpen;

    private int openCount;
    private bool lastToggleState;
    private bool lastSettledState;
    private bool isKeptPanelOpen;
    private bool isUnrenderPanelOpen;

    private bool isStyledPanelOpen;
    private bool isClassedPanelOpen;
    private bool isPanelStylesOpen;
    private bool isPanelClassesOpen;

    private bool isExternalIconPanelOpen;

    private bool isRtlPanelOpenStart;
    private bool isRtlPanelOpenEnd;

    private void HandleOnDismiss(MouseEventArgs e)
    {
        dismissCount++;
    }

    // OnDismissing is what carries the reason, so the panel never has to be asked what closed it.
    private void HandleOnDismissing(BitPanelDismissArgs args)
    {
        lastDismissReason = args.Reason.ToString();
    }

    private void HandleOnGuardedDismissing(BitPanelDismissArgs args)
    {
        guardedReason = args.Reason;
        // The Close the panel's own button asked for is let through; the gestures that could be a slip are not.
        args.Cancel = guardPanel && args.Reason is not BitPanelDismissReason.Programmatic;
        guardedRefused = args.Cancel;
    }



    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => isBasicPanelOpen = true"">Open panel</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => basicPanelRef.Toggle()"">Toggle panel</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isCloseButtonPanelOpen = true"">ShowCloseButton</BitButton>

<BitPanel @bind-IsOpen=""isBasicPanelOpen"" @ref=""basicPanelRef"" AriaLabel=""A basic panel"">
    <div class=""panel-body"">
        <h3>Basic</h3>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting
            shared dreams. Each word carried meaning, each pause brought understanding. Placeholder
            text reminds us of that moment when possibilities are limitless, waiting for content to
            emerge.
        </div>
        <BitButton OnClick=""() => isBasicPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isCloseButtonPanelOpen"" ShowCloseButton AriaLabel=""A panel with a close button"">
    <div class=""panel-body"">
        <h3>ShowCloseButton</h3>
        <div>
            The close button in the corner dismisses this panel the way a click on the page behind
            it, the Escape key or a swipe towards the edge would.
        </div>
    </div>
</BitPanel>";
    private readonly string example1CsharpCode = @"
private bool isBasicPanelOpen;
private BitPanel basicPanelRef = default!;
private bool isCloseButtonPanelOpen;";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => isHeaderTextPanelOpen = true"">HeaderText</BitButton>
<BitButton OnClick=""() => isHeaderPanelOpen = true"">Header & Footer</BitButton>
<BitButton OnClick=""() => isFooterTextPanelOpen = true"">FooterText</BitButton>

<BitPanel @bind-IsOpen=""isHeaderTextPanelOpen"" Size=""320"" HeaderText=""A panel with a HeaderText"" ShowCloseButton>
    <div>
        Once upon a time, stories wove connections between people, a symphony of voices crafting
        shared dreams. Each word carried meaning, each pause brought understanding. Placeholder
        text reminds us of that moment when possibilities are limitless, waiting for content to
        emerge.
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isHeaderPanelOpen"" Size=""320"" ShowCloseButton>
    <Header>
        <BitStack Gap=""0.5rem"" FillContent>
            <div>A panel with a Header</div>
            <BitSearchBox Placeholder=""Search here..."" />
        </BitStack>
    </Header>
    <Body>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas,
            emotions, and dreams. These placeholder words symbolize the beginning - a moment of
            possibility where creativity has yet to take shape.
        </div>
    </Body>
    <Footer>
        <BitStack Horizontal Gap=""0.5rem"">
            <BitButton OnClick=""() => isHeaderPanelOpen = false"">Save</BitButton>
            <BitButton Variant=""BitVariant.Outline"" OnClick=""() => isHeaderPanelOpen = false"">Cancel</BitButton>
        </BitStack>
    </Footer>
</BitPanel>

<BitPanel @bind-IsOpen=""isFooterTextPanelOpen""
          Size=""320""
          HeaderText=""A panel with a FooterText""
          FooterText=""This is a footer text!""
          ShowCloseButton>
    <div>
        In the beginning, there is silence - a blank canvas yearning to be filled, a quiet space
        where creativity waits to awaken. These words are temporary, standing in place of ideas
        yet to come, a glimpse into the infinite possibilities that lie ahead.
    </div>
</BitPanel>";
    private readonly string example2CsharpCode = @"
private bool isHeaderPanelOpen;
private bool isHeaderTextPanelOpen;
private bool isFooterTextPanelOpen;";

    private readonly string example3RazorCode = @"
<BitNumberField @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />

<BitButton OnClick=""() => isOpenInPositionStart = true"">Start</BitButton>
<BitButton OnClick=""() => isOpenPositionEnd = true"">End</BitButton>
<BitButton OnClick=""() => isOpenInPositionTop = true"">Top</BitButton>
<BitButton OnClick=""() => isOpenInPositionBottom = true"">Bottom</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isFullSizePanelOpen = true"">FullSize</BitButton>

<BitPanel Size=""customPanelSize""
          @bind-IsOpen=""isOpenInPositionStart""
          AriaLabel=""A panel at the start of the screen""
          Position=""BitPanelPosition.Start"">
    <div class=""panel-body"">
        BitPanel with Start position and custom Size.
        <BitNumberField @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    </div>
</BitPanel>

<BitPanel Size=""customPanelSize""
          @bind-IsOpen=""isOpenPositionEnd""
          AriaLabel=""A panel at the end of the screen""
          Position=""BitPanelPosition.End"">
    <div class=""panel-body"">
        BitPanel with End position and custom Size.
        <BitNumberField @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    </div>
</BitPanel>

<BitPanel Size=""customPanelSize""
          @bind-IsOpen=""isOpenInPositionTop""
          AriaLabel=""A panel at the top of the screen""
          Position=""BitPanelPosition.Top"">
    <div class=""panel-body"">
        BitPanel with Top position and custom Size.
        <BitNumberField @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    </div>
</BitPanel>

<BitPanel Size=""customPanelSize""
          @bind-IsOpen=""isOpenInPositionBottom""
          AriaLabel=""A panel at the bottom of the screen""
          Position=""BitPanelPosition.Bottom"">
    <div class=""panel-body"">
        BitPanel with Bottom position and custom Size.
        <BitNumberField @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isFullSizePanelOpen"" FullSize AriaLabel=""A full size panel"">
    <div class=""panel-body"">
        BitPanel with <b>FullSize</b>, which takes the whole of the screen.
        <BitButton OnClick=""() => isFullSizePanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example3CsharpCode = @"
private double customPanelSize = 300;
private bool isOpenInPositionStart;
private bool isOpenPositionEnd;
private bool isOpenInPositionTop;
private bool isOpenInPositionBottom;
private bool isFullSizePanelOpen;";

    private readonly string example4RazorCode = @"
<BitButton OnClick=""() => isBlockingPanelOpen = true"">Blocking</BitButton>
<BitButton OnClick=""() => isModeFullPanelOpen = true"">ModeFull</BitButton>
<BitButton OnClick=""() => isModelessPanelOpen = true"">Modeless</BitButton>
<BitButton OnClick=""() => isNoEscapePanelOpen = true"">NoDismissOnEscape</BitButton>

<BitPanel @bind-IsOpen=""isBlockingPanelOpen""
          Blocking
          AriaLabel=""A blocking panel""
          OnDismiss=""HandleOnDismiss""
          OnDismissing=""HandleOnDismissing""
          OnOverlayClick=""() => overlayClickCount++"">
    <div class=""panel-body"">
        <h3>Blocking</h3>
        <div>
            A click on the overlay does not dismiss this panel, but it is still reported:
            <b>@overlayClickCount</b> so far.
        </div>
        <BitButton OnClick=""() => isBlockingPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isModeFullPanelOpen""
          ModeFull
          AriaLabel=""A panel that dims the page""
          OnDismiss=""HandleOnDismiss""
          OnDismissing=""HandleOnDismissing"">
    <div class=""panel-body"">
        <h3>ModeFull</h3>
        <div>The page behind this panel is dimmed by the overlay instead of only being covered by it.</div>
        <BitButton OnClick=""() => isModeFullPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isModelessPanelOpen""
          @ref=""modelessPanelRef""
          Modeless
          AriaLabel=""A modeless panel""
          OnDismiss=""HandleOnDismiss""
          OnDismissing=""HandleOnDismissing"">
    <div class=""panel-body"">
        <h3>Modeless</h3>
        <div>
            There is no overlay over the page, so everything behind this panel stays usable - and
            the keyboard is free to leave the panel too.
        </div>
        <BitButton OnClick=""() => modelessPanelRef.Close()"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isNoEscapePanelOpen""
          NoDismissOnEscape
          AriaLabel=""A panel that ignores the Escape key""
          OnDismiss=""HandleOnDismiss""
          OnDismissing=""HandleOnDismissing""
          OnEscapeKeyDown=""() => escapeKeyCount++"">
    <div class=""panel-body"">
        <h3>NoDismissOnEscape</h3>
        <div>
            The Escape key does not dismiss this panel, but it is still reported:
            <b>@escapeKeyCount</b> so far. A click on the overlay still dismisses the panel.
        </div>
        <BitButton OnClick=""() => isNoEscapePanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example4CsharpCode = @"
private int dismissCount;
private int overlayClickCount;
private int escapeKeyCount;
private string lastDismissReason = ""-"";
private bool isBlockingPanelOpen;
private bool isModeFullPanelOpen;
private bool isModelessPanelOpen;
private bool isNoEscapePanelOpen;
private BitPanel modelessPanelRef = default!;

private void HandleOnDismiss(MouseEventArgs e)
{
    dismissCount++;
}

private void HandleOnDismissing(BitPanelDismissArgs args)
{
    lastDismissReason = args.Reason.ToString();
}";

    private readonly string example5RazorCode = @"
<BitToggle @bind-Value=""guardPanel"" Label=""Refuse the dismissals the user asks for"" />
<BitButton OnClick=""() => isGuardedPanelOpen = true"">Open guarded panel</BitButton>

<BitPanel @bind-IsOpen=""isGuardedPanelOpen""
          @ref=""guardedPanelRef""
          AriaLabel=""A panel that can refuse to close""
          OnDismissing=""HandleOnGuardedDismissing"">
    <div class=""panel-body"">
        <h3>OnDismissing</h3>
        <div>
            While the toggle is on, the overlay, the Escape key and a swipe are all turned down -
            only the button below gets this panel closed.
        </div>
        <BitButton OnClick=""() => guardedPanelRef.Close()"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example5CsharpCode = @"
private bool guardPanel = true;
private bool guardedRefused;
private bool isGuardedPanelOpen;
private BitPanelDismissReason? guardedReason;
private BitPanel guardedPanelRef = default!;

private void HandleOnGuardedDismissing(BitPanelDismissArgs args)
{
    guardedReason = args.Reason;
    // The Close the panel's own button asked for is let through; the gestures that could be a slip are not.
    args.Cancel = guardPanel && args.Reason is not BitPanelDismissReason.Programmatic;
    guardedRefused = args.Cancel;
}";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""() => isFocusPanelOpen = true"">Auto focus</BitButton>
<BitButton OnClick=""() => isNoFocusPanelOpen = true"">NoAutoFocus & NoFocusTrap</BitButton>
<BitButton OnClick=""() => isNoRestoreFocusPanelOpen = true"">NoRestoreFocus</BitButton>

<BitPanel @bind-IsOpen=""isFocusPanelOpen""
          TitleAriaId=""panel-focus-title""
          SubtitleAriaId=""panel-focus-subtitle"">
    <div class=""panel-body"">
        <h3 id=""panel-focus-title"">Focus</h3>
        <div id=""panel-focus-subtitle"">
            The keyboard came in with the panel and cannot leave it. Closing hands it back to the
            button that opened the panel.
        </div>
        <BitTextField Label=""First field"" />
        <BitTextField Label=""Second field"" InputHtmlAttributes=""@(new() { { ""data-autofocus"", """" } })"" />
        <BitButton OnClick=""() => isFocusPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isNoFocusPanelOpen"" NoAutoFocus NoFocusTrap AriaLabel=""A panel that leaves the focus alone"">
    <div class=""panel-body"">
        <h3>NoAutoFocus & NoFocusTrap</h3>
        <div>The focus stayed on the button that opened this panel, and Tab walks out of it.</div>
        <BitButton OnClick=""() => isNoFocusPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isNoRestoreFocusPanelOpen"" NoRestoreFocus AriaLabel=""A panel that does not hand the focus back"">
    <div class=""panel-body"">
        <h3>NoRestoreFocus</h3>
        <div>
            The keyboard came in with this panel as usual, but closing it leaves the focus where
            the panel left it rather than back on the button that opened it.
        </div>
        <BitButton OnClick=""() => isNoRestoreFocusPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example6CsharpCode = @"
private bool isFocusPanelOpen;
private bool isNoFocusPanelOpen;
private bool isNoRestoreFocusPanelOpen;";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""() => isScrollLockPanelOpen = true"">Held page</BitButton>
<BitButton OnClick=""() => isNoScrollLockPanelOpen = true"">NoScrollLock</BitButton>
<BitButton OnClick=""() => isAutoToggleScrollPanelOpen = true"">AutoToggleScroll</BitButton>

<BitPanel @bind-IsOpen=""isScrollLockPanelOpen"" AriaLabel=""A panel that holds the page still"">
    <div class=""panel-body"">
        <h3>Held page</h3>
        <div>
            The page behind this panel cannot be scrolled while it is open, and nothing on it
            shifted sideways when the scrollbar went away. Close the panel and it comes back.
        </div>
        <BitButton OnClick=""() => isScrollLockPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isNoScrollLockPanelOpen"" NoScrollLock AriaLabel=""A panel that leaves the page scrolling"">
    <div class=""panel-body"">
        <h3>NoScrollLock</h3>
        <div>The page behind this panel carries on scrolling while it is open.</div>
        <BitButton OnClick=""() => isNoScrollLockPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isAutoToggleScrollPanelOpen"" AutoToggleScroll AriaLabel=""A panel that toggles the overflow of the page"">
    <div class=""panel-body"">
        <h3>AutoToggleScroll</h3>
        <div>
            This panel took the overflow off the page itself rather than taking the counted hold.
        </div>
        <BitButton OnClick=""() => isAutoToggleScrollPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example7CsharpCode = @"
private bool isScrollLockPanelOpen;
private bool isNoScrollLockPanelOpen;
private bool isAutoToggleScrollPanelOpen;";

    private readonly string example8RazorCode = @"
<BitNumberField @bind-Value=""swipeTrigger"" Step=""0.05"" Min=""0.05"" Max=""1"" Mode=""BitSpinButtonMode.Inline"" Label=""SwipeTrigger"" />

<BitButton OnClick=""() => isSwipePanelOpen = true"">Swipe</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isNoSwipePanelOpen = true"">NoSwipe</BitButton>

<BitPanel @bind-IsOpen=""isSwipePanelOpen""
          Size=""300""
          AriaLabel=""A swipeable panel""
          SwipeTrigger=""@((decimal)swipeTrigger)""
          OnSwipeStart=""v => { swipeStart = v; swipeDiff = 0; }""
          OnSwipeMove=""v => swipeDiff = v""
          OnSwipeEnd=""v => swipeDiff = v"">
    <div class=""panel-body"">
        <h3>Swipe</h3>
        <div>Drag this panel towards the end of the screen and let go.</div>
        <div>Start: <b>@swipeStart</b></div>
        <div>Diff: <b>@swipeDiff</b></div>
        <BitButton OnClick=""() => isSwipePanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isNoSwipePanelOpen"" NoSwipe Size=""300"" AriaLabel=""A panel that cannot be swiped away"">
    <div class=""panel-body"">
        <h3>NoSwipe</h3>
        <div>Dragging this panel does nothing - the gesture is left to whatever is inside it.</div>
        <BitButton OnClick=""() => isNoSwipePanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example8CsharpCode = @"
private double swipeTrigger = 0.25;
private decimal swipeStart;
private decimal swipeDiff;
private bool isSwipePanelOpen;
private bool isNoSwipePanelOpen;";

    private readonly string example9RazorCode = @"
<BitButton OnClick=""() => isOuterPanelOpen = true"">Open outer panel</BitButton>

<BitPanel @bind-IsOpen=""isOuterPanelOpen"" Size=""320"" ModeFull AriaLabel=""The outer panel"">
    <div class=""panel-body"">
        <h3>Outer</h3>
        <div>This panel sits at the layer every panel shares.</div>
        <BitButton OnClick=""() => isInnerPanelOpen = true"">Open inner panel</BitButton>
        <BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOuterPanelOpen = false"">Close</BitButton>

        <BitPanel @bind-IsOpen=""isInnerPanelOpen""
                  Size=""240""
                  ModeFull
                  ZIndex=""1310""
                  AriaLabel=""The inner panel""
                  Position=""BitPanelPosition.Start"">
            <div class=""panel-body"">
                <h3>Inner</h3>
                <div>Lifted over the panel it was opened from, so its own overlay covers it.</div>
                <BitButton OnClick=""() => isInnerPanelOpen = false"">Close</BitButton>
            </div>
        </BitPanel>
    </div>
</BitPanel>";
    private readonly string example9CsharpCode = @"
private bool isOuterPanelOpen;
private bool isInnerPanelOpen;";

    private readonly string example10RazorCode = @"
<div class=""absolute-container"">
    <div>The panel below opens inside this box, not over the page.</div>
    <BitButton OnClick=""() => isAbsolutePanelOpen = true"">Open</BitButton>

    <BitPanel @bind-IsOpen=""isAbsolutePanelOpen""
              AbsolutePosition
              Size=""200""
              AriaLabel=""A panel inside a container""
              ModeFull>
        <div class=""panel-body"">
            <h3>AbsolutePosition</h3>
            <BitButton OnClick=""() => isAbsolutePanelOpen = false"">Close</BitButton>
        </div>
    </BitPanel>
</div>";
    private readonly string example10CsharpCode = @"
private bool isAbsolutePanelOpen;";

    private readonly string example11RazorCode = @"
<BitButton OnClick=""() => isUnrenderPanelOpen = true"">Starts over</BitButton>
<BitButton OnClick=""() => isKeptPanelOpen = true"">KeepMounted</BitButton>

<BitPanel @bind-IsOpen=""isUnrenderPanelOpen""
          AriaLabel=""A panel that starts over every time""
          OnOpen=""() => openCount++""
          OnToggle=""v => lastToggleState = v""
          OnTransitionEnd=""v => lastSettledState = v"">
    <div class=""panel-body"">
        <h3>Starts over</h3>
        <div>This content is built again from nothing every time the panel is opened.</div>
        <BitTextField Label=""Type something, then close and reopen"" />
        <BitButton OnClick=""() => isUnrenderPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isKeptPanelOpen""
          KeepMounted
          AriaLabel=""A panel that keeps its content""
          OnTransitionEnd=""v => lastSettledState = v"">
    <div class=""panel-body"">
        <h3>KeepMounted</h3>
        <div>This content stays in the page once the panel has been opened for the first time.</div>
        <BitTextField Label=""Type something, then close and reopen"" />
        <BitButton OnClick=""() => isKeptPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example11CsharpCode = @"
private int openCount;
private bool lastToggleState;
private bool lastSettledState;
private bool isKeptPanelOpen;
private bool isUnrenderPanelOpen;";

    private readonly string example12RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitButton OnClick=""() => isExternalIconPanelOpen = true"">External close icon</BitButton>

<BitPanel @bind-IsOpen=""isExternalIconPanelOpen""
          Size=""320""
          ShowCloseButton
          HeaderText=""External close icon""
          CloseIcon=""@BitIconInfo.Fa(""solid xmark"")"">
    <div>
        The close button of this panel is drawn by FontAwesome instead of by the built-in icon
        set.
    </div>
</BitPanel>";
    private readonly string example12CsharpCode = @"
private bool isExternalIconPanelOpen;";

    private readonly string example13RazorCode = @"
<BitButton OnClick=""() => isStyledPanelOpen = true"">Open Styled panel</BitButton>
<BitButton OnClick=""() => isClassedPanelOpen = true"">Open Classed panel</BitButton>
<BitButton OnClick=""() => isPanelStylesOpen = true"">Open panel Styles</BitButton>
<BitButton OnClick=""() => isPanelClassesOpen = true"">Open panel Classes</BitButton>

<BitPanel @bind-IsOpen=""isStyledPanelOpen"" Style=""font-size: 3rem;"" AriaLabel=""A styled panel"">
    <div class=""panel-body"">
        BitPanel with custom style.
        <BitButton OnClick=""() => isStyledPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isClassedPanelOpen"" Class=""custom-class"" AriaLabel=""A classed panel"">
    <div class=""panel-body"">
        BitPanel with custom class:
        <div class=""item"">Item 1</div>
        <div class=""item"">Item 2</div>
        <div class=""item"">Item 3</div>
        <BitButton OnClick=""() => isClassedPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isPanelStylesOpen""
          AriaLabel=""A panel with custom Styles""
          Styles=""@(new() { Overlay = ""background-color: #4776f433;"",
                            Container = ""padding: 1rem; box-shadow: 0 0 1rem tomato;"" })"">
    <div>
        BitPanel with <b>Styles</b> to customize its elements.
        <BitButton OnClick=""() => isPanelStylesOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isPanelClassesOpen""
          ShowCloseButton
          HeaderText=""Classes""
          FooterText=""This is a footer text!""
          AriaLabel=""A panel with custom Classes""
          Classes=""@(new() { Container = ""custom-container"",
                             Overlay = ""custom-overlay"",
                             HeaderContainer = ""custom-header-container"",
                             Header = ""custom-header"",
                             Body = ""custom-body"",
                             Footer = ""custom-footer"" })"">
    <div>
        BitPanel with <b>Classes</b> to customize its elements.
        <BitButton OnClick=""() => isPanelClassesOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example13CsharpCode = @"
private bool isStyledPanelOpen;
private bool isClassedPanelOpen;
private bool isPanelStylesOpen;
private bool isPanelClassesOpen;";

    private readonly string example14RazorCode = @"
<BitButton OnClick=""() => isRtlPanelOpenStart = true"">آغاز</BitButton>
<BitButton OnClick=""() => isRtlPanelOpenEnd = true"">پایان</BitButton>

<BitPanel @bind-IsOpen=""isRtlPanelOpenStart""
          Dir=""BitDir.Rtl""
          AriaLabel=""پنل آغاز""
          Position=""BitPanelPosition.Start"">
    <div class=""panel-body"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
        <BitButton OnClick=""() => isRtlPanelOpenStart = false"">بستن</BitButton>
    </div>
</BitPanel>

<BitPanel @bind-IsOpen=""isRtlPanelOpenEnd""
          Dir=""BitDir.Rtl""
          AriaLabel=""پنل پایان""
          Position=""BitPanelPosition.End"">
    <div class=""panel-body"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
        <BitButton OnClick=""() => isRtlPanelOpenEnd = false"">بستن</BitButton>
    </div>
</BitPanel>";
    private readonly string example14CsharpCode = @"
private bool isRtlPanelOpenStart;
private bool isRtlPanelOpenEnd;";
}
