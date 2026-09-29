namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Modal;

public partial class BitModalDemo
{
    private readonly string example1RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitButton OnClick=""() => isOpenBasic = true"">Open Modal</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenNoBorder = true"">NoBorder</BitButton>
<BitModal @bind-IsOpen=""isOpenBasic"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Hello!</BitText>
        <BitText>Press Escape, click outside, or use the button below to close this Modal.</BitText>
        <BitButton OnClick=""() => isOpenBasic = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenNoBorder"" NoBorder>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">No border</BitText>
        <BitText>The accent line along the top edge is gone.</BitText>
        <BitButton OnClick=""() => isOpenNoBorder = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example1CsharpCode = @"
private bool isOpenBasic;
private bool isOpenNoBorder;";

    private readonly string example2RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }

    .modal-content-wide {
        max-width: none;
    }
</style>

<BitButton OnClick=""() => isOpenMaxWidth = true"">MaxWidth</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenFixedSize = true"">Width &amp; Height</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenMaxHeight = true"">MaxHeight</BitButton>
<BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenFullWidth = true"">FullWidth</BitButton>
<BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenFullHeight = true"">FullHeight</BitButton>
<BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenFullSize = true"">FullSize</BitButton>
<BitModal @bind-IsOpen=""isOpenMaxWidth"" MaxWidth=""32rem"">
    <div class=""modal-content modal-content-wide"">
        <BitText Typography=""BitTypography.H6"">MaxWidth</BitText>
        <BitText>
            However long this paragraph gets, the Modal stops growing at 32rem and the text wraps instead,
            which keeps a line short enough to be read comfortably on a wide screen.
        </BitText>
        <BitButton OnClick=""() => isOpenMaxWidth = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenFixedSize"" Width=""24rem"" Height=""18rem"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Width &amp; Height</BitText>
        <BitText>24rem by 18rem whatever is put in it, so it never resizes under the user.</BitText>
        <BitButton OnClick=""() => isOpenFixedSize = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenMaxHeight"" MaxWidth=""30rem"" MaxHeight=""16rem"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">MaxHeight</BitText>
        <BitText>
            The Modal stops at 16rem and scrolls inside itself from there. Every story starts with a blank
            canvas, a quiet space waiting to be filled with ideas, emotions and dreams. These placeholder
            words stand for the beginning - a moment of possibility where creativity has yet to take shape.
            Soon they will turn into narratives that provoke thought, spark emotion and resonate with those
            who read them.
        </BitText>
        <BitButton OnClick=""() => isOpenMaxHeight = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenFullWidth"" FullWidth>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">FullWidth</BitText>
        <BitButton OnClick=""() => isOpenFullWidth = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenFullHeight"" FullHeight>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">FullHeight</BitText>
        <BitButton OnClick=""() => isOpenFullHeight = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenFullSize"" FullSize>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">FullSize</BitText>
        <BitButton OnClick=""() => isOpenFullSize = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example2CsharpCode = @"
private bool isOpenMaxWidth;
private bool isOpenFixedSize;
private bool isOpenMaxHeight;
private bool isOpenFullWidth;
private bool isOpenFullHeight;
private bool isOpenFullSize;";

    private readonly string example3RazorCode = @"
<style>
    .modal-header {
        gap: 0.5rem;
        display: flex;
        font-size: 24px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
    }

    .modal-header-text {
        flex-grow: 1;
    }

    .modal-body {
        max-width: 960px;
        line-height: 20px;
        padding: 0 24px 24px;
    }
</style>

<BitButton OnClick=""() => isOpenCustomContent = true"">Own markup</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenHeaderText = true"">HeaderText</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenHeaderTemplate = true"">Header template</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenFooter = true"">Footer</BitButton>
<BitModal @bind-IsOpen=""isOpenCustomContent"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Story title</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenCustomContent = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Everything in this Modal is its own markup: the title, the close button and the room around them.
        Content taller than the screen scrolls inside the Modal rather than running off the edge of it.
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenHeaderText"" MaxWidth=""32rem"" ShowCloseButton HeaderText=""Release notes"">
    <BitText>The title, the close button and the padding all come from the Modal.</BitText>
</BitModal>
<BitModal @bind-IsOpen=""isOpenHeaderTemplate"" MaxWidth=""32rem"" ShowCloseButton>
    <Header>
        <BitStack Gap=""0.5rem"" AutoHeight>
            <BitText Typography=""BitTypography.H6"">Search the docs</BitText>
            <BitSearchBox Placeholder=""Search here..."" />
        </BitStack>
    </Header>
    <Body>
        <BitText>A Header template holds anything - here a search box under the title.</BitText>
    </Body>
</BitModal>
<BitModal @bind-IsOpen=""isOpenFooter"" MaxWidth=""32rem"" ShowCloseButton HeaderText=""Unsaved changes"">
    <Body>
        <BitText>The footer stays at the bottom while the body scrolls, and lays its actions out the way the theme does.</BitText>
    </Body>
    <Footer>
        <BitButton OnClick=""() => isOpenFooter = false"">Save</BitButton>
        <BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenFooter = false"">Discard</BitButton>
    </Footer>
</BitModal>";
    private readonly string example3CsharpCode = @"
private bool isOpenCustomContent;
private bool isOpenHeaderText;
private bool isOpenHeaderTemplate;
private bool isOpenFooter;";

    private readonly string example4RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitButton OnClick=""() => isOpenBlocking = true"">Blocking</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenNoEscape = true"">Blocking + NoDismissOnEscape</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""OpenCanClose"">CanClose</BitButton>
<BitModal @bind-IsOpen=""isOpenBlocking"" Blocking>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Blocking</BitText>
        <BitText>A click on the overlay does nothing; Escape or the button below closes this Modal.</BitText>
        <BitButton OnClick=""() => isOpenBlocking = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenNoEscape"" Blocking NoDismissOnEscape>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Only the button closes this one</BitText>
        <BitText>Neither Escape nor a click on the overlay dismisses this Modal.</BitText>
        <BitButton OnClick=""() => isOpenNoEscape = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenCanClose"" CanClose=""CanCloseEditor"" ShowCloseButton HeaderText=""Edit profile"" MaxWidth=""26rem"">
    <BitStack Gap=""1rem"" AutoHeight>
        <BitTextField Label=""Name"" @bind-Value=""editorName"" Immediate />
        <BitText>Type something, then try to close: the Modal stays open until the field is cleared or the changes are discarded.</BitText>
        <BitButton Variant=""BitVariant.Outline"" OnClick=""DiscardEditor"">Discard changes</BitButton>
    </BitStack>
</BitModal>";
    private readonly string example4CsharpCode = @"
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
}";

    private readonly string example5RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }

    .modal-content-header {
        gap: 1rem;
        width: 100%;
        display: flex;
        align-items: center;
        justify-content: space-between;
    }
</style>

<BitButton OnClick=""() => isOpenFocus = true"">Default focus</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenAutoFocus = true"">data-autofocus</BitButton>
<BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenNoFocus = true"">NoAutoFocus</BitButton>
<BitModal @bind-IsOpen=""isOpenFocus"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">The focus is here</BitText>
        <BitText>Tab and Shift+Tab cycle between Ok and Cancel and never reach the page behind them.</BitText>
        <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
            <BitButton OnClick=""() => isOpenFocus = false"">Ok</BitButton>
            <BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenFocus = false"">Cancel</BitButton>
        </BitStack>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenAutoFocus"">
    <div class=""modal-content"">
        <div class=""modal-content-header"">
            <BitText Typography=""BitTypography.H6"">Named starting point</BitText>
            <BitButton IconOnly
                       Title=""Close""
                       Variant=""BitVariant.Text""
                       IconName=""@BitIconName.ChromeClose""
                       OnClick=""() => isOpenAutoFocus = false"" />
        </div>
        <BitText>The close button comes first, but the field below takes the focus.</BitText>
        <BitTextField Label=""Your name"" InputHtmlAttributes=""autoFocusAttributes"" />
        <BitButton OnClick=""() => isOpenAutoFocus = false"">Save</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenNoFocus"" NoAutoFocus NoFocusTrap NoRestoreFocus>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">The focus stayed outside</BitText>
        <BitText>Tab walks into the page behind, and Escape does nothing until something in here is focused.</BitText>
        <BitButton OnClick=""() => isOpenNoFocus = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example5CsharpCode = @"
private bool isOpenFocus;
private bool isOpenAutoFocus;
private bool isOpenNoFocus;
private readonly Dictionary<string, object> autoFocusAttributes = new() { { ""data-autofocus"", true } };";

    private readonly string example6RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitButton OnClick=""() => isOpenScrollLock = true"">Holds the page</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenNoScrollLock = true"">NoScrollLock</BitButton>
<BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenAutoToggleScroll = true"">AutoToggleScroll</BitButton>
<BitModal @bind-IsOpen=""isOpenScrollLock"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">The page is held</BitText>
        <BitText>Try scrolling: the page behind this Modal stays where it was.</BitText>
        <BitButton OnClick=""() => isOpenScrollLock = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenNoScrollLock"" NoScrollLock>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">The page still scrolls</BitText>
        <BitText>Try scrolling: the page behind this Modal moves with the wheel.</BitText>
        <BitButton OnClick=""() => isOpenNoScrollLock = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenAutoToggleScroll"" AutoToggleScroll MaxWidth=""30rem"" ShowCloseButton HeaderText=""AutoToggleScroll"">
    <BitText>The overflow of the page is taken away while this Modal is open.</BitText>
</BitModal>";
    private readonly string example6CsharpCode = @"
private bool isOpenScrollLock;
private bool isOpenNoScrollLock;
private bool isOpenAutoToggleScroll;";

    private readonly string example7RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitButton OnClick=""() => isOpenModeFull = true"">ModeFull</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenModeless = !isOpenModeless"">Modeless</BitButton>
<BitModal @bind-IsOpen=""isOpenModeFull"" ModeFull>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">ModeFull</BitText>
        <BitText>The page under this Modal is dimmed rather than merely covered.</BitText>
        <BitButton OnClick=""() => isOpenModeFull = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenModeless"" Modeless ShowCloseButton HeaderText=""Modeless"" MaxWidth=""22rem"">
    <BitText>Carry on with the page: this Modal holds neither the pointer, nor the keyboard, nor the scroll.</BitText>
</BitModal>";
    private readonly string example7CsharpCode = @"
private bool isOpenModeFull;
private bool isOpenModeless;";

    private readonly string example8RazorCode = @"
<style>
    .position-btn {
        gap: 1rem;
        display: flex;
        flex-flow: column nowrap;
    }

    .position-btn div {
        display: flex;
        justify-content: space-between;
    }

    .position-button {
        width: 130px;
    }

    .relative-container {
        width: 100%;
        height: 20rem;
        overflow: auto;
        position: relative;
        border: 2px solid lightgreen;
    }
</style>

<div class=""position-btn"">
    <div>
        <BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.TopLeft)"">Top Left</BitButton>
        <BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.TopCenter)"">Top Center</BitButton>
        <BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.TopRight)"">Top Right</BitButton>
    </div>
    <div>
        <BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.CenterLeft)"">Center Left</BitButton>
        <BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.Center)"">Center</BitButton>
        <BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.CenterRight)"">Center Right</BitButton>
    </div>
    <div>
        <BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.BottomLeft)"">Bottom Left</BitButton>
        <BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.BottomCenter)"">Bottom Center</BitButton>
        <BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.BottomRight)"">Bottom Right</BitButton>
    </div>
</div>
<BitModal @bind-IsOpen=""isOpenPosition"" Position=""position"" ShowCloseButton MaxWidth=""24rem"" HeaderText=""@($""Position: {position}"")"">
    <Body>
        <BitText>This Modal is placed by the Position parameter.</BitText>
    </Body>
</BitModal>

<br /><br />

<BitButton OnClick=""() => isOpenAbsolute = true"">AbsolutePosition</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenAbsoluteScroller = true"">AbsolutePosition + AutoToggleScroll</BitButton>

<br />

<div class=""relative-container"" id=""modal-scroller"">
    <BitModal @bind-IsOpen=""isOpenAbsolute"" AbsolutePosition ShowCloseButton HeaderText=""AbsolutePosition"" MaxWidth=""24rem"">
        <BitText>This Modal covers the bordered box rather than the page.</BitText>
    </BitModal>

    <BitModal @bind-IsOpen=""isOpenAbsoluteScroller""
              AbsolutePosition
              AutoToggleScroll
              ShowCloseButton
              MaxWidth=""24rem""
              ScrollerSelector=""#modal-scroller""
              HeaderText=""AutoToggleScroll"">
        <BitText>The box behind this Modal, named by ScrollerSelector, is held still.</BitText>
    </BitModal>

    <div>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
        Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
        Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
        efficitur.
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
    </div>
</div>";
    private readonly string example8CsharpCode = @"
private bool isOpenPosition;
private BitPosition position = BitPosition.Center;

private void OpenModalInPosition(BitPosition positionValue)
{
    position = positionValue;
    isOpenPosition = true;
}

private bool isOpenAbsolute;
private bool isOpenAbsoluteScroller;";

    private readonly string example9RazorCode = @"
<style>
    .modal-drag-handle {
        width: 100%;
        color: white;
        padding: 1rem;
        cursor: move;
        background: brown;
        border-radius: 0.25rem;
    }
</style>

<BitButton OnClick=""() => isOpenDraggable = true"">Draggable</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenDragHandle = true"">DragElementSelector</BitButton>
<BitModal @bind-IsOpen=""isOpenDraggable"" Draggable ShowCloseButton HeaderText=""Drag me"" MaxWidth=""26rem"">
    <BitText>Press anywhere on this Modal and drag it around.</BitText>
</BitModal>
<BitModal @bind-IsOpen=""isOpenDragHandle"" Draggable DragElementSelector=""#modal-drag-handle"" ShowCloseButton MaxWidth=""26rem"">
    <Header>
        <div id=""modal-drag-handle"" class=""modal-drag-handle"">Drag me by this bar</div>
    </Header>
    <Body>
        <BitText>Only the bar drags this Modal, so this text can still be selected.</BitText>
    </Body>
</BitModal>";
    private readonly string example9CsharpCode = @"
private bool isOpenDraggable;
private bool isOpenDragHandle;";

    private readonly string example10RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitButton OnClick=""() => isOpenAutoNamed = true"">Named by its header</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenLabelled = true"">TitleAriaId</BitButton>
<BitButton Variant=""BitVariant.Outline"" Color=""BitColor.Error"" OnClick=""() => isOpenAlert = true"">Alert dialog</BitButton>
<BitModal @bind-IsOpen=""isOpenAutoNamed"" ShowCloseButton HeaderText=""Privacy settings"" MaxWidth=""26rem"">
    <BitText>A screen reader announces this dialog as ""Privacy settings"".</BitText>
</BitModal>
<BitModal @bind-IsOpen=""isOpenLabelled"" TitleAriaId=""modal-title"" SubtitleAriaId=""modal-subtitle"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"" Id=""modal-title"">Terms of service</BitText>
        <BitText Id=""modal-subtitle"">Read these before carrying on.</BitText>
        <BitButton OnClick=""() => isOpenLabelled = false"">Accept</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenAlert"" IsAlert Blocking AriaLabel=""Delete the project"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Delete the project?</BitText>
        <BitText>This cannot be undone.</BitText>
        <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
            <BitButton Color=""BitColor.Error"" OnClick=""() => isOpenAlert = false"">Delete</BitButton>
            <BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenAlert = false"">Cancel</BitButton>
        </BitStack>
    </div>
</BitModal>";
    private readonly string example10CsharpCode = @"
private bool isOpenAutoNamed;
private bool isOpenLabelled;
private bool isOpenAlert;";

    private readonly string example11RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitButton OnClick=""() => isOpenKeptMounted = true"">KeepMounted</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenNotKeptMounted = true"">Built again each time</BitButton>
<BitModal @bind-IsOpen=""isOpenKeptMounted"" KeepMounted>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Kept in the page</BitText>
        <BitTextField Label=""Your name"" />
        <BitButton OnClick=""() => isOpenKeptMounted = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenNotKeptMounted"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Built again each time</BitText>
        <BitTextField Label=""Your name"" />
        <BitButton OnClick=""() => isOpenNotKeptMounted = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example11CsharpCode = @"
private bool isOpenKeptMounted;
private bool isOpenNotKeptMounted;";

    private readonly string example12RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitButton OnClick=""() => isEventsOpen = true"">Open Modal</BitButton>
<br /><br />
<div>Opened? [@isOpened]</div>
<div>Dismissed? [@isDismissed]</div>
<div>Overlay clicked? [@isOverlayClicked]</div>
<div>Escape pressed? [@isEscapePressed]</div>
<BitModal @bind-IsOpen=""isEventsOpen""
          OnOpen=""HandleOnOpen""
          OnDismiss=""HandleOnDismiss""
          OnEscapeKeyDown=""HandleOnEscapeKeyDown""
          OnOverlayClick=""HandleOnOverlayClick"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Events</BitText>
        <BitText>Click the overlay or press Escape, then watch the flags on the page.</BitText>
        <BitButton OnClick=""() => isEventsOpen = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example12CsharpCode = @"
private bool isEventsOpen;
private bool isOpened;
private int openedVersion;
private bool isDismissed;
private bool isOverlayClicked;
private bool isEscapePressed;

private async Task HandleOnOpen()
{
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
}";

    private readonly string example13RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitButton OnClick=""() => refModal.Open()"">Open</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => refModal.Toggle()"">Toggle</BitButton>
<BitModal @ref=""refModal"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Driven by methods</BitText>
        <BitText>This Modal has no IsOpen: it is opened and closed through its reference.</BitText>
        <BitButton OnClick=""() => refModal.Close()"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example13CsharpCode = @"
private BitModal refModal = default!;";

    private readonly string example14RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitButton OnClick=""() => isOpenOuter = true"">Open Modal</BitButton>
<BitModal @bind-IsOpen=""isOpenOuter"" MaxWidth=""30rem"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Project settings</BitText>
        <BitTextField Label=""Project name"" />
        <BitDropdown Label=""Visibility"" TItem=""BitDropdownOption<string>"" TValue=""string"" DefaultValue=""@(""private"")"">
            <BitDropdownOption Text=""Private"" Value=""@(""private"")"" />
            <BitDropdownOption Text=""Team"" Value=""@(""team"")"" />
            <BitDropdownOption Text=""Public"" Value=""@(""public"")"" />
        </BitDropdown>
        <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
            <BitButton OnClick=""() => isOpenOuter = false"">Save</BitButton>
            <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.Error"" OnClick=""() => isOpenInner = true"">Delete</BitButton>
        </BitStack>

        <BitModal @bind-IsOpen=""isOpenInner"" IsAlert Blocking AriaLabel=""Confirm the deletion"">
            <div class=""modal-content"">
                <BitText Typography=""BitTypography.H6"">Delete this project?</BitText>
                <BitText>Escape closes this one and leaves the settings open.</BitText>
                <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
                    <BitButton Color=""BitColor.Error"" OnClick=""HandleNestedDelete"">Delete</BitButton>
                    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenInner = false"">Cancel</BitButton>
                </BitStack>
            </div>
        </BitModal>
    </div>
</BitModal>";
    private readonly string example14CsharpCode = @"
private bool isOpenOuter;
private bool isOpenInner;

private void HandleNestedDelete()
{
    isOpenInner = false;
    isOpenOuter = false;
}";

    private readonly string example15RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }
</style>

<BitButton OnClick=""() => isOpenCascaded = true"">Takes the cascade</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenCascadedOwn = true"">Its own position</BitButton>
<BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenUncascaded = true"">Outside the cascade</BitButton>
<BitParams Parameters=""modalParams"">
    <BitModal @bind-IsOpen=""isOpenCascaded"" HeaderText=""Cascaded"">
        <BitText>Top center, dimmed page, close button and a 26rem cap - all from BitParams.</BitText>
    </BitModal>
    <BitModal @bind-IsOpen=""isOpenCascadedOwn"" HeaderText=""Own position"" Position=""BitPosition.BottomCenter"">
        <BitText>This one keeps its own Position and takes the rest from BitParams.</BitText>
    </BitModal>
</BitParams>
<BitModal @bind-IsOpen=""isOpenUncascaded"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Outside the cascade</BitText>
        <BitText>Back to the defaults.</BitText>
        <BitButton OnClick=""() => isOpenUncascaded = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example15CsharpCode = @"
private bool isOpenCascaded;
private bool isOpenCascadedOwn;
private bool isOpenUncascaded;

private readonly BitModalParams[] modalParams =
[
    new()
    {
        ModeFull = true,
        ShowCloseButton = true,
        MaxWidth = ""26rem"",
        Position = BitPosition.TopCenter,
    }
];";

    private readonly string example16RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<BitButton OnClick=""() => isOpenExternalIcon = true"">Open Modal</BitButton>
<BitModal @bind-IsOpen=""isOpenExternalIcon""
          MaxWidth=""32rem""
          ShowCloseButton
          HeaderText=""External close icon""
          CloseIcon=""@BitIconInfo.Fa(""solid xmark"")"">
    <BitText>The close button of this Modal wears a FontAwesome icon.</BitText>
</BitModal>";
    private readonly string example16CsharpCode = @"
private bool isOpenExternalIcon;";

    private readonly string example17RazorCode = @"
<style>
    .modal-content {
        gap: 1rem;
        display: flex;
        padding: 1.5rem;
        max-width: 420px;
        flex-flow: column nowrap;
        align-items: flex-start;
    }

    .custom-class {
        border: 0.5rem solid tomato;
        background-color: darkgoldenrod;
    }

    .custom-overlay {
        background-color: #ffbd5a66;
    }

    .custom-header-container {
        background-color: tomato;
    }

    .custom-body {
        color: black;
        background-color: lightseagreen;
    }

    .custom-footer {
        color: brown;
        font-size: 1.5rem;
        padding-top: 1.5rem;
        background-color: tomato;
    }
</style>

<BitButton OnClick=""() => isOpenStyle = true"">Style</BitButton>
<BitButton OnClick=""() => isOpenClass = true"">Class</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenStyles = true"">Styles</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenClasses = true"">Classes</BitButton>
<BitModal @bind-IsOpen=""isOpenStyle"" Style=""box-shadow: inset 0px 0px 1.5rem 1.5rem palevioletred;"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Styled root</BitText>
        <BitButton OnClick=""() => isOpenStyle = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenClass"" Class=""custom-class"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Classed root</BitText>
        <BitButton OnClick=""() => isOpenClass = false"">Close</BitButton>
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenStyles""
          MaxWidth=""32rem""
          ShowCloseButton
          HeaderText=""Styled parts""
          Styles=""@(new() { Overlay = ""background-color: #4776f433;"",
                            Content = ""box-shadow: 0 0 1rem tomato;"",
                            Header = ""color: tomato;"" })"">
    <BitText>The overlay, the content and the header each take a style of their own.</BitText>
</BitModal>
<BitModal @bind-IsOpen=""isOpenClasses""
          MaxWidth=""32rem""
          ShowCloseButton
          HeaderText=""Classed parts""
          FooterText=""This is a footer text!""
          Classes=""@(new() { Overlay = ""custom-overlay"",
                             HeaderContainer = ""custom-header-container"",
                             Body = ""custom-body"",
                             Footer = ""custom-footer"" })"">
    <BitText>The overlay, the header container, the body and the footer each take a class of their own.</BitText>
</BitModal>
<br /><br />
<BitButton OnClick=""() => isOpenCssVars = true"">CSS variables</BitButton>
<BitModal @bind-IsOpen=""isOpenCssVars""
          ShowCloseButton
          ModeFull
          Position=""BitPosition.TopCenter""
          HeaderText=""CSS variables""
          FooterText=""Restyled without a single class.""
          Style=""--bit-Modal-background: #1e1b4b; --bit-Modal-color: #e0e7ff; --bit-Modal-border-color: #a5b4fc; --bit-Modal-radius: 1rem; --bit-Modal-padding: 1.5rem; --bit-Modal-offset: 2rem; --bit-Modal-max-width: 30rem; --bit-Modal-overlay-background: #1e1b4b99; --bit-Modal-overlay-backdrop-filter: blur(4px); --bit-Modal-header-font-size: 1.5rem;"">
    <BitText>Fill, text, accent, corners, padding, header type, blurred overlay, cap and offset all come from variables.</BitText>
</BitModal>";
    private readonly string example17CsharpCode = @"
private bool isOpenStyle;
private bool isOpenClass;
private bool isOpenStyles;
private bool isOpenClasses;
private bool isOpenCssVars;";

    private readonly string example18RazorCode = @"
<div dir=""rtl"">
    <BitButton Dir=""BitDir.Rtl"" OnClick=""() => isOpenRtl = true"">باز کردن مُدال</BitButton>
</div>
<BitModal Dir=""BitDir.Rtl""
          @bind-IsOpen=""isOpenRtl""
          ShowCloseButton
          MaxWidth=""30rem""
          Position=""BitPosition.TopStart""
          HeaderText=""لورم ایپسوم""
          CloseButtonTitle=""بستن"">
    <BitText>
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
    </BitText>
</BitModal>";
    private readonly string example18CsharpCode = @"
private bool isOpenRtl;";
}
