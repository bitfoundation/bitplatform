namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Panel;

public partial class BitPanelDemo
{
    private readonly string example1RazorCode = @"
<style>
    .panel-body {
        gap: 1rem;
        width: 300px;
        display: flex;
        padding: 1rem;
        max-width: 100%;
        box-sizing: border-box;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitButton OnClick=""() => isBasicPanelOpen = true"">Open panel</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => basicPanelRef.Toggle()"">Toggle panel</BitButton>

<BitPanel @ref=""basicPanelRef"" @bind-IsOpen=""isBasicPanelOpen"" AriaLabel=""Basic panel"">
    <div class=""panel-body"">
        <h3>Basic</h3>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting
            shared dreams. Each word carried meaning, each pause brought understanding.
        </div>
        <BitButton OnClick=""() => basicPanelRef.Close()"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example1CsharpCode = @"
private bool isBasicPanelOpen;
private BitPanel basicPanelRef = default!;";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => isHeaderTextPanelOpen = true"">HeaderText &amp; FooterText</BitButton>
<BitButton OnClick=""() => isTemplatePanelOpen = true"">Header, Body &amp; Footer</BitButton>

<BitPanel @bind-IsOpen=""isHeaderTextPanelOpen""
          HeaderText=""Notifications""
          FooterText=""Updated a minute ago""
          ShowCloseButton
          CloseButtonTitle=""Close notifications"">
    You have no new notifications.
</BitPanel>

<BitPanel @bind-IsOpen=""isTemplatePanelOpen"" ShowCloseButton AriaLabel=""Filters"">
    <Header>
        <BitStack Gap=""0.5rem"" FillContent>
            <div>Filters</div>
            <BitSearchBox Placeholder=""Search filters"" />
        </BitStack>
    </Header>
    <Body>
        <BitStack Gap=""0.5rem"">
            @for (var i = 1; i <= 30; i++)
            {
                var index = i;
                <BitCheckbox Label=""@($""Filter {index}"")"" />
            }
        </BitStack>
    </Body>
    <Footer>
        <BitStack Horizontal Gap=""0.5rem"">
            <BitButton OnClick=""() => isTemplatePanelOpen = false"">Apply</BitButton>
            <BitButton Variant=""BitVariant.Outline"" OnClick=""() => isTemplatePanelOpen = false"">Cancel</BitButton>
        </BitStack>
    </Footer>
</BitPanel>";
    private readonly string example2CsharpCode = @"
private bool isHeaderTextPanelOpen;
private bool isTemplatePanelOpen;";

    private readonly string example3RazorCode = @"
<BitChoiceGroup @bind-Value=""panelPosition"" Horizontal Label=""Position""
                TItem=""BitChoiceGroupOption<BitPanelPosition>"" TValue=""BitPanelPosition"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitPanelPosition.Start"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitPanelPosition.End"" />
    <BitChoiceGroupOption Text=""Top"" Value=""BitPanelPosition.Top"" />
    <BitChoiceGroupOption Text=""Bottom"" Value=""BitPanelPosition.Bottom"" />
</BitChoiceGroup>
<BitNumberField @bind-Value=""panelSize"" Min=""100"" Step=""20"" Mode=""BitSpinButtonMode.Inline"" Label=""Size"" />
<BitToggle @bind-Value=""panelFullSize"" Label=""FullSize"" />

<BitButton OnClick=""() => isPositionPanelOpen = true"">Open panel</BitButton>

<BitPanel @bind-IsOpen=""isPositionPanelOpen""
          Position=""panelPosition""
          Size=""panelSize""
          FullSize=""panelFullSize""
          HeaderText=""@($""Position.{panelPosition}"")""
          ShowCloseButton>
    Size: @(panelFullSize ? ""FullSize"" : $""{panelSize}px"")
</BitPanel>";
    private readonly string example3CsharpCode = @"
private BitPanelPosition panelPosition = BitPanelPosition.End;
private double panelSize = 300;
private bool panelFullSize;
private bool isPositionPanelOpen;";

    private readonly string example4RazorCode = @"
<BitToggle @bind-Value=""overlayModeFull"" Label=""ModeFull"" />
<BitToggle @bind-Value=""overlayBlocking"" Label=""Blocking"" />
<BitToggle @bind-Value=""overlayNoEscape"" Label=""NoDismissOnEscape"" />
<BitToggle @bind-Value=""overlayModeless"" Label=""Modeless"" />

<BitButton OnClick=""() => isOverlayPanelOpen = true"">Open panel</BitButton>
<div>Overlay clicks: <b>@overlayClickCount</b>, Escape presses: <b>@escapeKeyCount</b>, dismissals: <b>@dismissCount</b></div>

<BitPanel @bind-IsOpen=""isOverlayPanelOpen""
          ModeFull=""overlayModeFull""
          Blocking=""overlayBlocking""
          NoDismissOnEscape=""overlayNoEscape""
          Modeless=""overlayModeless""
          HeaderText=""Overlay and dismissal""
          ShowCloseButton
          OnOverlayClick=""() => overlayClickCount++""
          OnEscapeKeyDown=""() => escapeKeyCount++""
          OnDismiss=""() => dismissCount++"">
    <BitStack Gap=""1rem"">
        <div>Click the page behind this panel or press Escape.</div>
        <BitDropdown Label=""Escape closes this list, not the panel""
                     Items=""dismissalItems""
                     Placeholder=""Select a letter""
                     TItem=""BitDropdownItem<string>"" TValue=""string"" />
    </BitStack>
</BitPanel>";
    private readonly string example4CsharpCode = @"
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
    new() { Text = ""A"", Value = ""A"" },
    new() { Text = ""B"", Value = ""B"" },
    new() { Text = ""C"", Value = ""C"" },
];";

    private readonly string example5RazorCode = @"
<BitToggle @bind-Value=""guardPanel"" Label=""Refuse the overlay, Escape and swipe"" />
<BitButton OnClick=""() => isGuardedPanelOpen = true"">Open panel</BitButton>
<div>Last attempt: <b>@(guardedReason?.ToString() ?? ""-"")</b>, refused: <b>@guardedRefused</b></div>

<BitPanel @ref=""guardedPanelRef""
          @bind-IsOpen=""isGuardedPanelOpen""
          HeaderText=""Unsaved changes""
          ShowCloseButton
          OnDismissing=""HandleOnDismissing"">
    <Body>
        <BitTextField Label=""Name"" />
    </Body>
    <Footer>
        <BitButton OnClick=""() => guardedPanelRef.Close()"">Save</BitButton>
    </Footer>
</BitPanel>";
    private readonly string example5CsharpCode = @"
private bool guardPanel = true;
private bool guardedRefused;
private BitPanelDismissReason? guardedReason;
private bool isGuardedPanelOpen;
private BitPanel guardedPanelRef = default!;

// The gestures that could be a slip are refused; the close button and the panel's own Save go through.
private void HandleOnDismissing(BitPanelDismissArgs args)
{
    guardedReason = args.Reason;
    args.Cancel = guardPanel && args.Reason is BitPanelDismissReason.Overlay
                                            or BitPanelDismissReason.Escape
                                            or BitPanelDismissReason.Swipe;
    guardedRefused = args.Cancel;
}";

    private readonly string example6RazorCode = @"
<style>
    .panel-body {
        gap: 1rem;
        width: 300px;
        display: flex;
        padding: 1rem;
        max-width: 100%;
        box-sizing: border-box;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitToggle @bind-Value=""a11yNoAutoFocus"" Label=""NoAutoFocus"" />
<BitToggle @bind-Value=""a11yNoFocusTrap"" Label=""NoFocusTrap"" />
<BitToggle @bind-Value=""a11yNoRestoreFocus"" Label=""NoRestoreFocus"" />
<BitToggle @bind-Value=""a11yIsAlert"" Label=""IsAlert"" />

<BitButton OnClick=""() => isA11yPanelOpen = true"">Open panel</BitButton>

<BitPanel @bind-IsOpen=""isA11yPanelOpen""
          TitleAriaId=""panel-a11y-title""
          SubtitleAriaId=""panel-a11y-subtitle""
          IsAlert=""a11yIsAlert""
          NoAutoFocus=""a11yNoAutoFocus""
          NoFocusTrap=""a11yNoFocusTrap""
          NoRestoreFocus=""a11yNoRestoreFocus"">
    <div class=""panel-body"">
        <h3 id=""panel-a11y-title"">Edit profile</h3>
        <div id=""panel-a11y-subtitle"">The second field takes the focus, since it is marked data-autofocus.</div>
        <BitTextField Label=""Name"" />
        <BitTextField Label=""Email"" InputHtmlAttributes=""@(new() { { ""data-autofocus"", """" } })"" />
        <BitButton OnClick=""() => isA11yPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>";
    private readonly string example6CsharpCode = @"
private bool a11yNoAutoFocus;
private bool a11yNoFocusTrap;
private bool a11yNoRestoreFocus;
private bool a11yIsAlert;
private bool isA11yPanelOpen;";

    private readonly string example7RazorCode = @"
<BitChoiceGroup @bind-Value=""scrollMode"" Horizontal Label=""Scroll handling""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Lock (default)"" Value=""@(""Lock"")"" />
    <BitChoiceGroupOption Text=""NoScrollLock"" Value=""@(""NoScrollLock"")"" />
    <BitChoiceGroupOption Text=""AutoToggleScroll"" Value=""@(""AutoToggleScroll"")"" />
</BitChoiceGroup>

<BitButton OnClick=""() => isScrollPanelOpen = true"">Open panel</BitButton>

<BitPanel @bind-IsOpen=""isScrollPanelOpen""
          NoScrollLock=""@(scrollMode == ""NoScrollLock"")""
          AutoToggleScroll=""@(scrollMode == ""AutoToggleScroll"")""
          HeaderText=""@scrollMode""
          ShowCloseButton>
    Try scrolling the page behind this panel.
</BitPanel>";
    private readonly string example7CsharpCode = @"
private string scrollMode = ""Lock"";
private bool isScrollPanelOpen;";

    private readonly string example8RazorCode = @"
<style>
    .no-swipe-strip {
        padding: 1rem;
        border: 1px dashed var(--bit-clr-brd-pri);
    }
</style>

<BitNumberField @bind-Value=""swipeTrigger"" Step=""0.05"" Min=""0.05"" Max=""1"" Mode=""BitSpinButtonMode.Inline"" Label=""SwipeTrigger"" />
<BitToggle @bind-Value=""noSwipe"" Label=""NoSwipe"" />

<BitButton OnClick=""() => isSwipePanelOpen = true"">Open panel</BitButton>

<BitPanel @bind-IsOpen=""isSwipePanelOpen""
          Size=""300""
          NoSwipe=""noSwipe""
          SwipeTrigger=""@((decimal)swipeTrigger)""
          HeaderText=""Swipe me away""
          ShowCloseButton
          OnSwipeStart=""v => { swipeStart = v; swipeDiff = 0; }""
          OnSwipeMove=""v => swipeDiff = v""
          OnSwipeEnd=""v => swipeDiff = v"">
    <div>Start: <b>@swipeStart</b></div>
    <div>Diff: <b>@swipeDiff</b></div>
    <br />
    <div class=""no-swipe-strip"" data-no-swipe>
        Dragging here does not move the panel (data-no-swipe).
    </div>
</BitPanel>";
    private readonly string example8CsharpCode = @"
private double swipeTrigger = 0.25;
private bool noSwipe;
private decimal swipeStart;
private decimal swipeDiff;
private bool isSwipePanelOpen;";

    private readonly string example9RazorCode = @"
<BitButton OnClick=""() => isOuterPanelOpen = true"">Open outer panel</BitButton>

<BitPanel @bind-IsOpen=""isOuterPanelOpen"" Size=""360"" ModeFull HeaderText=""Outer"" ShowCloseButton>
    <BitButton OnClick=""() => isInnerPanelOpen = true"">Open nested panel</BitButton>
    <BitButton OnClick=""() => isSiblingPanelOpen = true"">Open sibling panel</BitButton>

    <BitPanel @bind-IsOpen=""isInnerPanelOpen""
              Size=""280""
              ModeFull
              Position=""BitPanelPosition.Start""
              HeaderText=""Nested""
              ShowCloseButton>
        Declared inside the outer panel, so it covers it without a ZIndex.
    </BitPanel>
</BitPanel>

<BitPanel @bind-IsOpen=""isSiblingPanelOpen""
          Size=""280""
          ModeFull
          ZIndex=""1310""
          Position=""BitPanelPosition.Start""
          HeaderText=""Sibling""
          ShowCloseButton>
    Declared beside the outer panel and lifted over it by ZIndex.
</BitPanel>";
    private readonly string example9CsharpCode = @"
private bool isOuterPanelOpen;
private bool isInnerPanelOpen;
private bool isSiblingPanelOpen;";

    private readonly string example10RazorCode = @"
<style>
    .absolute-container {
        gap: 1rem;
        display: flex;
        padding: 1rem;
        overflow: hidden;
        position: relative;
        min-height: 200px;
        flex-flow: column nowrap;
        align-items: flex-start;
        border: 1px solid var(--bit-clr-brd-sec);
    }
</style>

<div class=""absolute-container"">
    <div>The panel opens inside this box.</div>
    <BitButton OnClick=""() => isAbsolutePanelOpen = true"">Open panel</BitButton>

    <BitPanel @bind-IsOpen=""isAbsolutePanelOpen""
              AbsolutePosition
              ModeFull
              Size=""220""
              HeaderText=""AbsolutePosition""
              ShowCloseButton>
        Contained by its box.
    </BitPanel>
</div>";
    private readonly string example10CsharpCode = @"
private bool isAbsolutePanelOpen;";

    private readonly string example11RazorCode = @"
<BitToggle @bind-Value=""keepMounted"" Label=""KeepMounted"" />

<BitButton OnClick=""() => isRenderPanelOpen = true"">Open panel</BitButton>
<div>Opened <b>@openCount</b> times, last toggled to <b>@lastToggleState</b>, settled at <b>@lastSettledState</b></div>

<BitPanel @bind-IsOpen=""isRenderPanelOpen""
          KeepMounted=""keepMounted""
          HeaderText=""@(keepMounted ? ""KeepMounted"" : ""Starts over"")""
          ShowCloseButton
          OnOpen=""() => openCount++""
          OnToggle=""v => lastToggleState = v""
          OnTransitionEnd=""v => lastSettledState = v"">
    <BitTextField Label=""Type something"" />
</BitPanel>";
    private readonly string example11CsharpCode = @"
private bool keepMounted;
private int openCount;
private bool lastToggleState;
private bool lastSettledState;
private bool isRenderPanelOpen;";

    private readonly string example12RazorCode = @"
<BitParams Parameters=""@panelParams"">
    <BitButton OnClick=""() => isCascadedPanelOpen = true"">Cascaded values</BitButton>
    <BitButton OnClick=""() => isOverridingPanelOpen = true"">Own Position</BitButton>

    <BitPanel @bind-IsOpen=""isCascadedPanelOpen"" HeaderText=""From the cascade"">
        Start edge, 320px, dimmed page and a close button - none of it set on this panel.
    </BitPanel>

    <BitPanel @bind-IsOpen=""isOverridingPanelOpen"" Position=""BitPanelPosition.End"" HeaderText=""Its own Position"">
        Slides in from the end, everything else from the cascade.
    </BitPanel>
</BitParams>";
    private readonly string example12CsharpCode = @"
private readonly BitPanelParams[] panelParams =
[
    new()
    {
        Position = BitPanelPosition.Start,
        Size = 320,
        ModeFull = true,
        ShowCloseButton = true,
    }
];
private bool isCascadedPanelOpen;
private bool isOverridingPanelOpen;";

    private readonly string example13RazorCode = @"
<!-- FontAwesome CSS -->
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitButton OnClick=""() => isExternalIconPanelOpen = true"">CloseIcon</BitButton>
<BitButton OnClick=""() => isIconNamePanelOpen = true"">CloseIconName</BitButton>

<BitPanel @bind-IsOpen=""isExternalIconPanelOpen""
          HeaderText=""CloseIcon""
          ShowCloseButton
          CloseIcon=""@BitIconInfo.Fa(""solid xmark"")"">
    The close button is drawn by FontAwesome.
</BitPanel>

<BitPanel @bind-IsOpen=""isIconNamePanelOpen""
          HeaderText=""CloseIconName""
          ShowCloseButton
          CloseIconName=""@BitIconName.ChromeBack"">
    The close button uses another built-in icon.
</BitPanel>";
    private readonly string example13CsharpCode = @"
private bool isExternalIconPanelOpen;
private bool isIconNamePanelOpen;";

    private readonly string example14RazorCode = @"
<style>
    .custom-container {
        border-end-start-radius: 1rem;
        border-start-start-radius: 1rem;
        border: 0.25rem solid #0054C6;
    }

    .custom-overlay {
        background-color: #ffbd5a66;
    }

    .custom-header-container {
        padding: 1.5rem;
        background-color: tomato;
    }

    .custom-body {
        color: black;
        background-color: lightseagreen;
    }

    .custom-footer {
        color: brown;
        padding: 1.5rem;
        background-color: tomato;
    }
</style>

<BitButton OnClick=""() => isStylesPanelOpen = true"">Styles</BitButton>
<BitButton OnClick=""() => isClassesPanelOpen = true"">Classes</BitButton>
<BitButton OnClick=""() => isCssVarsPanelOpen = true"">CSS variables</BitButton>

<BitPanel @bind-IsOpen=""isStylesPanelOpen""
          HeaderText=""Styles""
          ShowCloseButton
          Styles=""@(new() { Overlay = ""background-color: #4776f433;"",
                            Container = ""width: 30vw; min-width: 16rem; box-shadow: 0 0 1rem tomato;"",
                            Header = ""color: tomato;"" })"">
    A 30vw panel, set through the Container.
</BitPanel>

<BitPanel @bind-IsOpen=""isClassesPanelOpen""
          HeaderText=""Classes""
          FooterText=""A footer text""
          ShowCloseButton
          Classes=""@(new() { Container = ""custom-container"",
                             Overlay = ""custom-overlay"",
                             HeaderContainer = ""custom-header-container"",
                             Body = ""custom-body"",
                             Footer = ""custom-footer"" })"">
    Every part carries a class of its own.
</BitPanel>

<BitPanel @bind-IsOpen=""isCssVarsPanelOpen""
          ModeFull
          HeaderText=""CSS variables""
          ShowCloseButton
          Style=""--bit-Panel-size: 22rem; --bit-Panel-radius: 1.5rem; --bit-Panel-border-width: 2px; --bit-Panel-border-color: var(--bit-clr-pri); --bit-Panel-padding: 2rem; --bit-Panel-header-font-size: 1.75rem; --bit-Panel-overlay-background: #0006; --bit-Panel-overlay-backdrop-filter: blur(4px);"">
    Rounded inner corners, a primary edge, roomier padding and a darker, blurring overlay.
</BitPanel>";
    private readonly string example14CsharpCode = @"
private bool isStylesPanelOpen;
private bool isClassesPanelOpen;
private bool isCssVarsPanelOpen;";

    private readonly string example15RazorCode = @"
<div dir=""rtl"">
    <BitButton OnClick=""() => isRtlPanelOpenStart = true"">آغاز</BitButton>
    <BitButton OnClick=""() => isRtlPanelOpenEnd = true"">پایان</BitButton>
</div>

<BitPanel @bind-IsOpen=""isRtlPanelOpenStart""
          Dir=""BitDir.Rtl""
          Size=""320""
          HeaderText=""پنل آغاز""
          ShowCloseButton
          CloseButtonTitle=""بستن""
          Position=""BitPanelPosition.Start"">
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
</BitPanel>

<BitPanel @bind-IsOpen=""isRtlPanelOpenEnd""
          Dir=""BitDir.Rtl""
          Size=""320""
          HeaderText=""پنل پایان""
          ShowCloseButton
          CloseButtonTitle=""بستن""
          Position=""BitPanelPosition.End"">
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
</BitPanel>";
    private readonly string example15CsharpCode = @"
private bool isRtlPanelOpenStart;
private bool isRtlPanelOpenEnd;";
}
