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
            Description = "Holds the page still while the panel is open, by taking the scrollbar off the element named by ScrollerSelector - the body of the document by default - and giving it back when the panel closes.",
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
            Name = "Content",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias for ChildContent.",
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stretches the panel to the full size of the screen along the axis it is sized on, which takes over from Size and from the cap that otherwise leaves a strip of the page showing beside it.",
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
            Name = "LazyRender",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the content of the panel out of the page until the panel is opened for the first time. Once rendered it stays, so whatever state the content holds survives the panel closing.",
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
            Description = "Leaves the focus where it is when the panel opens, instead of moving it into the panel. An element in the content marked with a data-autofocus attribute takes the focus instead of the first focusable one.",
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
            Name = "NoSwipe",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns off the swipe gesture that otherwise dismisses the panel when it is dragged towards the edge it slid in from.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when the panel is dismissed. It is called for every closing of the panel: the overlay, the Escape key, a swipe, the Close and Toggle methods, and the IsOpen parameter being set to false from the outside.",
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
            Name = "Position",
            Type = "BitPanelPosition?",
            DefaultValue = "null",
            Description = "The edge of the screen the panel slides in from. Start and End are the logical edges, so they follow the direction of the panel. It defaults to End.",
            Href = "#position-enum",
            LinkType = LinkType.Link,
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
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the element whose scrolling is taken away while the panel is open, for AutoToggleScroll. It defaults to the body of the document.",
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
            Description = "The ARIA id of the element that names the panel, which is what a screen reader reads out when the panel opens. AriaLabel names the panel where there is no such element.",
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
            Description = "Closes the panel. A panel that is already closed is left alone.",
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
        }
    ];



    private bool isBasicPanelOpen;
    private BitPanel basicPanelRef = default!;

    private double customPanelSize = 300;
    private bool isOpenInPositionStart;
    private bool isOpenPositionEnd;
    private bool isOpenInPositionTop;
    private bool isOpenInPositionBottom;
    private bool isFullSizePanelOpen;
    private bool isCssSizePanelOpen;

    private int dismissCount;
    private int overlayClickCount;
    private bool lastDismissWasOverlayClick;
    private bool isBlockingPanelOpen;
    private bool isModelessPanelOpen;
    private bool isNoEscapePanelOpen;
    private BitPanel modelessPanelRef = default!;

    private bool isFocusPanelOpen;
    private bool isNoFocusPanelOpen;

    private bool isAutoToggleScrollPanelOpen;

    private double swipeTrigger = 0.25;
    private decimal swipeStart;
    private decimal swipeDiff;
    private bool isSwipePanelOpen;
    private bool isNoSwipePanelOpen;

    private bool isAbsolutePanelOpen;

    private int openCount;
    private bool lastToggleState;
    private bool isLazyPanelOpen;

    private bool isStyledPanelOpen;
    private bool isClassedPanelOpen;
    private bool isPanelStylesOpen;
    private bool isPanelClassesOpen;

    private bool isRtlPanelOpenStart;
    private bool isRtlPanelOpenEnd;

    private void HandleOnDismiss(MouseEventArgs e)
    {
        dismissCount++;
        // A dismissal that did not come from a pointer carries the empty arguments the panel makes for it.
        lastDismissWasOverlayClick = e.ClientX != 0 || e.ClientY != 0;
    }



    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => isBasicPanelOpen = true"">Open panel</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => basicPanelRef.Toggle()"">Toggle panel</BitButton>

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
</BitPanel>";
    private readonly string example1CsharpCode = @"
private bool isBasicPanelOpen;
private BitPanel basicPanelRef = default!;";

    private readonly string example2RazorCode = @"
<BitNumberField @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />

<BitButton OnClick=""() => isOpenInPositionStart = true"">Start</BitButton>
<BitButton OnClick=""() => isOpenPositionEnd = true"">End</BitButton>
<BitButton OnClick=""() => isOpenInPositionTop = true"">Top</BitButton>
<BitButton OnClick=""() => isOpenInPositionBottom = true"">Bottom</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isFullSizePanelOpen = true"">FullSize</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isCssSizePanelOpen = true"">Styles.Container</BitButton>

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
</BitPanel>

<BitPanel @bind-IsOpen=""isCssSizePanelOpen""
          AriaLabel=""A panel sized in percent""
          Styles=""@(new() { Container = ""width:50%"" })"">
    <div class=""panel-body"">
        BitPanel sized through <b>Styles.Container</b>, which takes any CSS value.
        <BitButton OnClick=""() => isCssSizePanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example2CsharpCode = @"
private double customPanelSize = 300;
private bool isOpenInPositionStart;
private bool isOpenPositionEnd;
private bool isOpenInPositionTop;
private bool isOpenInPositionBottom;
private bool isFullSizePanelOpen;
private bool isCssSizePanelOpen;";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""() => isBlockingPanelOpen = true"">Blocking</BitButton>
<BitButton OnClick=""() => isModelessPanelOpen = true"">Modeless</BitButton>
<BitButton OnClick=""() => isNoEscapePanelOpen = true"">NoDismissOnEscape</BitButton>

<BitPanel @bind-IsOpen=""isBlockingPanelOpen""
          Blocking
          AriaLabel=""A blocking panel""
          OnDismiss=""HandleOnDismiss""
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

<BitPanel @bind-IsOpen=""isModelessPanelOpen""
          @ref=""modelessPanelRef""
          Modeless
          AriaLabel=""A modeless panel""
          OnDismiss=""HandleOnDismiss"">
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
          OnDismiss=""HandleOnDismiss"">
    <div class=""panel-body"">
        <h3>NoDismissOnEscape</h3>
        <div>The Escape key does nothing here. A click on the overlay still dismisses the panel.</div>
        <BitButton OnClick=""() => isNoEscapePanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example3CsharpCode = @"
private int dismissCount;
private int overlayClickCount;
private bool lastDismissWasOverlayClick;
private bool isBlockingPanelOpen;
private bool isModelessPanelOpen;
private bool isNoEscapePanelOpen;
private BitPanel modelessPanelRef = default!;

private void HandleOnDismiss(MouseEventArgs e)
{
    dismissCount++;
    // A dismissal that did not come from a pointer carries the empty arguments the panel makes for it.
    lastDismissWasOverlayClick = e.ClientX != 0 || e.ClientY != 0;
}";

    private readonly string example4RazorCode = @"
<BitButton OnClick=""() => isFocusPanelOpen = true"">Auto focus</BitButton>
<BitButton OnClick=""() => isNoFocusPanelOpen = true"">NoAutoFocus & NoFocusTrap</BitButton>

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
</BitPanel>";
    private readonly string example4CsharpCode = @"
private bool isFocusPanelOpen;
private bool isNoFocusPanelOpen;";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""() => isAutoToggleScrollPanelOpen = true"">AutoToggleScroll</BitButton>

<BitPanel @bind-IsOpen=""isAutoToggleScrollPanelOpen"" AutoToggleScroll AriaLabel=""A panel that holds the page still"">
    <div class=""panel-body"">
        <h3>AutoToggleScroll</h3>
        <div>
            The page behind this panel cannot be scrolled while it is open. Close the panel and the
            scrollbar comes back where it was.
        </div>
        <BitButton OnClick=""() => isAutoToggleScrollPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example5CsharpCode = @"
private bool isAutoToggleScrollPanelOpen;";

    private readonly string example6RazorCode = @"
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
    private readonly string example6CsharpCode = @"
private double swipeTrigger = 0.25;
private decimal swipeStart;
private decimal swipeDiff;
private bool isSwipePanelOpen;
private bool isNoSwipePanelOpen;";

    private readonly string example7RazorCode = @"
<div class=""absolute-container"">
    <div>The panel below opens inside this box, not over the page.</div>
    <BitButton OnClick=""() => isAbsolutePanelOpen = true"">Open</BitButton>

    <BitPanel @bind-IsOpen=""isAbsolutePanelOpen""
              AbsolutePosition
              Size=""200""
              AriaLabel=""A panel inside a container""
              Styles=""@(new() { Overlay = ""background-color:#00000033"" })"">
        <div class=""panel-body"">
            <h3>AbsolutePosition</h3>
            <BitButton OnClick=""() => isAbsolutePanelOpen = false"">Close</BitButton>
        </div>
    </BitPanel>
</div>";
    private readonly string example7CsharpCode = @"
private bool isAbsolutePanelOpen;";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""() => isLazyPanelOpen = true"">LazyRender</BitButton>

<BitPanel @bind-IsOpen=""isLazyPanelOpen""
          LazyRender
          AriaLabel=""A lazily rendered panel""
          OnOpen=""() => openCount++""
          OnToggle=""v => lastToggleState = v"">
    <div class=""panel-body"">
        <h3>LazyRender</h3>
        <div>This content was not in the page until the panel was first opened.</div>
        <BitButton OnClick=""() => isLazyPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example8CsharpCode = @"
private int openCount;
private bool lastToggleState;
private bool isLazyPanelOpen;";

    private readonly string example9RazorCode = @"
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
          AriaLabel=""A panel with custom Classes""
          Classes=""@(new() { Container = ""custom-container"",
                             Overlay = ""custom-overlay"" })"">
    <div>
        BitPanel with <b>Classes</b> to customize its elements.
        <BitButton OnClick=""() => isPanelClassesOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example9CsharpCode = @"
private bool isStyledPanelOpen;
private bool isClassedPanelOpen;
private bool isPanelStylesOpen;
private bool isPanelClassesOpen;";

    private readonly string example10RazorCode = @"
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
    private readonly string example10CsharpCode = @"
private bool isRtlPanelOpenStart;
private bool isRtlPanelOpenEnd;";
}
