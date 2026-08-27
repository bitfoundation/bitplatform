namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Modal;

public partial class BitModalDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaModal",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the Modal should be announced as modal to assistive technologies. It is also what decides whether the Modal keeps the keyboard inside itself: a Modal that is not announced as modal leaves the page behind it reachable with the keyboard the way it is reachable with the pointer.",
        },
        new()
        {
            Name = "Blocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay). Escape still dismisses it unless NoDismissOnEscape is set as well.",
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
            Name = "DefaultIsOpen",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The initial opening state of the Modal in the uncontrolled mode, which is when the IsOpen parameter is not set.",
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Modal height 100% of its parent container.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Modal width 100% of its parent container.",
        },
        new()
        {
            Name = "IsAlert",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Determines the ARIA role of the Modal (alertdialog/dialog).",
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
            Description = "Keeps the Modal in the page while it is closed instead of taking it out and building it again the next time it opens, so the content - and whatever state it holds - survives being closed. Nothing is rendered before the first time the Modal opens, and a kept Modal is inert and hidden from assistive technologies while it is closed.",
        },
        new()
        {
            Name = "NoAutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the Modal from moving the focus into itself when it opens. By default the focus lands on the first focusable element of the content, or on the element inside it marked with the data-autofocus attribute, or on the content itself when it holds nothing focusable.",
        },
        new()
        {
            Name = "NoDismissOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the Modal from being dismissed by pressing the Escape key.",
        },
        new()
        {
            Name = "NoFocusTrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the Modal from keeping the keyboard focus inside itself while it is open. The trap is only set up for a Modal that reports itself modal (see AriaModal) in the first place.",
        },
        new()
        {
            Name = "NoRestoreFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the Modal from handing the focus back to the element that had it before the Modal opened. The focus is only handed back when nothing else has taken it in the meantime.",
        },
        new()
        {
            Name = "NoScrollLock",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the Modal from holding the page still while it is open. By default the page behind an open Modal is held, with the room the scrollbar took added back as padding so that nothing shifts sideways; the holds are counted, so the page is only handed back once the last open Modal closes. A Modal that reports itself modeless (see AriaModal) never holds the page in the first place.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when the Modal is dismissed.",
        },
        new()
        {
            Name = "OnEscapeKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "A callback function for when the Escape key is pressed inside the Modal. It is invoked for every Escape, including the ones a Modal with NoDismissOnEscape refuses to be dismissed by, which makes it the counterpart of OnOverlayClick for the keyboard.",
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "A callback function for when the Modal is opened, invoked after it has rendered and its focus handling has run.",
        },
        new()
        {
            Name = "OnOverlayClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when somewhere on the overlay element of the Modal is clicked. It is invoked for every overlay click, including the ones a Blocking Modal refuses to be dismissed by.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the element whose scrolling the Modal holds while it is open. The page (body) is what is held when this is not set, which is the scroller of an ordinary page; an application shell that scrolls a region of its own names that region here.",
        },
        new()
        {
            Name = "ShowOverlay",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the overlay should be rendered. Without it the page behind the Modal keeps its own clicks, and there is no click left to light dismiss the Modal by.",
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
            Description = "ARIA id for the subtitle of the Modal, if any.",
        },
        new()
        {
            Name = "TitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the title of the Modal, if any.",
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Open",
            Type = "Task",
            Description = "Opens the Modal programmatically.",
        },
        new()
        {
            Name = "Close",
            Type = "Task",
            Description = "Closes the Modal programmatically.",
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
               }
            ]
        }
    ];



    private bool isOpenBasic;

    private bool isOpenCustomContent;

    private bool isOpenBlocking;

    private bool isOpenEscape;
    private bool isOpenNoEscape;

    private bool isOpenFocus;
    private bool isOpenNoFocus;
    private bool isOpenAutoFocus;
    private readonly Dictionary<string, object> autoFocusAttributes = new() { { "data-autofocus", true } };

    private bool isOpenScrollLock;
    private bool isOpenNoScrollLock;

    private bool isOpenFullWidth;
    private bool isOpenFullHeight;
    private bool isOpenFullSize;

    private bool isOpenNoOverlay;

    private bool isOpenAlert;
    private bool isOpenLabelled;

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
    private void HandleOnOverlayClick()
    {
        isOverlayClicked = true;
        _ = Task.Delay(2000).ContinueWith(_ =>
            {
                isOverlayClicked = false;
                InvokeAsync(StateHasChanged);
            });
    }
    private void HandleOnEscapeKeyDown()
    {
        isEscapePressed = true;
        _ = Task.Delay(2000).ContinueWith(_ =>
            {
                isEscapePressed = false;
                InvokeAsync(StateHasChanged);
            });
    }

    private BitModal refModal = default!;

    private bool isOpenStyle;
    private bool isOpenClass;
    private bool isOpenStyles;
    private bool isOpenClasses;

    private bool isOpenRtl;


    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => isOpenBasic = true"">Open Modal</BitButton>

<BitModal @bind-IsOpen=""isOpenBasic"">
    <div style=""padding:1rem;max-width:40rem"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
        shaped into meaning, and the emotions ready to resonate with every reader.
        <br />
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
        each word has the power to transform into something extraordinary. Here lies the start of something new-an
        opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
        idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
        begins here, in this quiet moment where everything is possible.
    </div>
</BitModal>";
    private readonly string example1CsharpCode = @"
private bool isOpenBasic;";

    private readonly string example2RazorCode = @"
<style>
    .modal-header {
        gap: 0.5rem;
        display: flex;
        font-size: 24px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        border-top: 4px solid #0054C6;
    }

    .modal-header-text {
        flex-grow: 1;
    }

    .modal-body {
        max-width: 960px;
        line-height: 20px;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>

<BitButton OnClick=""() => isOpenCustomContent = true"">Open Modal</BitButton>

<BitModal @bind-IsOpen=""isOpenCustomContent"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Story title</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenCustomContent = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
        for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        <br />
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>";
    private readonly string example2CsharpCode = @"
private bool isOpenCustomContent;";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""() => isOpenBlocking = true"">Open blocking Modal</BitButton>

<BitModal @bind-IsOpen=""isOpenBlocking"" Blocking>
    <div class=""modal-header"">
        <span class=""modal-header-text"">Blocking modal</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenBlocking = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>";
    private readonly string example3CsharpCode = @"
private bool isOpenBlocking;";

    private readonly string example4RazorCode = @"
<BitButton OnClick=""() => isOpenEscape = true"">Escape dismisses</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenNoEscape = true"">Escape does nothing</BitButton>

<BitModal @bind-IsOpen=""isOpenEscape"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Press Escape</BitText>
        <BitText>This Modal is dismissed by the Escape key, by a click on the overlay, or by the button below.</BitText>
        <BitButton OnClick=""() => isOpenEscape = false"">Close</BitButton>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenNoEscape"" Blocking NoDismissOnEscape>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Escape is off</BitText>
        <BitText>Neither Escape nor a click on the overlay dismisses this Modal, so the button below is the only way out of it.</BitText>
        <BitButton OnClick=""() => isOpenNoEscape = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example4CsharpCode = @"
private bool isOpenEscape;
private bool isOpenNoEscape;";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""() => isOpenFocus = true"">Default focus</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenAutoFocus = true"">data-autofocus</BitButton>
<BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenNoFocus = true"">NoAutoFocus</BitButton>

<BitModal @bind-IsOpen=""isOpenFocus"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">The focus is here</BitText>
        <BitText>Tab and Shift+Tab cycle between the two buttons here, Ok and Cancel, and never reach the page behind them.</BitText>
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
        <BitText>The close button above is the first focusable element, but the field below is the one the focus lands on.</BitText>
        <BitTextField Label=""Your name"" InputHtmlAttributes=""autoFocusAttributes"" />
        <BitButton OnClick=""() => isOpenAutoFocus = false"">Save</BitButton>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenNoFocus"" NoAutoFocus NoFocusTrap NoRestoreFocus>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">The focus stayed outside</BitText>
        <BitText>The focus is still on the button that opened this Modal, so Tab walks into the page behind it and Escape does nothing until something in here is focused.</BitText>
        <BitButton OnClick=""() => isOpenNoFocus = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example5CsharpCode = @"
private bool isOpenFocus;
private bool isOpenNoFocus;
private bool isOpenAutoFocus;
private readonly Dictionary<string, object> autoFocusAttributes = new() { { ""data-autofocus"", true } };";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""() => isOpenScrollLock = true"">Holds the page</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenNoScrollLock = true"">NoScrollLock</BitButton>

<BitModal @bind-IsOpen=""isOpenScrollLock"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">The page is held</BitText>
        <BitText>Try scrolling: the page behind this Modal stays where it was, and nothing shifted sideways when its scrollbar went away.</BitText>
        <BitButton OnClick=""() => isOpenScrollLock = false"">Close</BitButton>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenNoScrollLock"" NoScrollLock>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">The page still scrolls</BitText>
        <BitText>Try scrolling: the page behind this Modal moves along with the wheel.</BitText>
        <BitButton OnClick=""() => isOpenNoScrollLock = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example6CsharpCode = @"
private bool isOpenScrollLock;
private bool isOpenNoScrollLock;";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""() => isOpenFullWidth = true"">FullWidth</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenFullHeight = true"">FullHeight</BitButton>
<BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenFullSize = true"">Both</BitButton>

<BitModal @bind-IsOpen=""isOpenFullWidth"" FullWidth>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">FullWidth</BitText>
        <BitText>This Modal is as wide as the screen and as tall as its content.</BitText>
        <BitButton OnClick=""() => isOpenFullWidth = false"">Close</BitButton>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenFullHeight"" FullHeight>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">FullHeight</BitText>
        <BitText>This Modal is as tall as the screen and as wide as its content.</BitText>
        <BitButton OnClick=""() => isOpenFullHeight = false"">Close</BitButton>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenFullSize"" FullWidth FullHeight>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">FullWidth and FullHeight</BitText>
        <BitText>This Modal takes the whole screen.</BitText>
        <BitButton OnClick=""() => isOpenFullSize = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example7CsharpCode = @"
private bool isOpenFullWidth;
private bool isOpenFullHeight;
private bool isOpenFullSize;";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""() => isOpenNoOverlay = true"">No overlay</BitButton>

<BitModal @bind-IsOpen=""isOpenNoOverlay"" ShowOverlay=""false"" AriaModal=""false"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Modeless</BitText>
        <BitText>The page behind this Modal is still clickable, and this text can still be selected.</BitText>
        <BitButton OnClick=""() => isOpenNoOverlay = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example8CsharpCode = @"
private bool isOpenNoOverlay;";

    private readonly string example9RazorCode = @"
<BitButton OnClick=""() => isOpenLabelled = true"">Labelled dialog</BitButton>
<BitButton Variant=""BitVariant.Outline"" Color=""BitColor.Error"" OnClick=""() => isOpenAlert = true"">Alert dialog</BitButton>

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
    private readonly string example9CsharpCode = @"
private bool isOpenAlert;
private bool isOpenLabelled;";

    private readonly string example10RazorCode = @"
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
    private readonly string example10CsharpCode = @"
private bool isOpenKeptMounted;
private bool isOpenNotKeptMounted;";

    private readonly string example11RazorCode = @"
<BitButton OnClick=""() => isEventsOpen = true"">Open Modal</BitButton>

<div>Opened? [@isOpened]</div>
<div>Dismissed? [@isDismissed]</div>
<div>Overlay clicked? [@isOverlayClicked]</div>
<div>Escape pressed? [@isEscapePressed]</div>

<BitModal @bind-IsOpen=""isEventsOpen""
          OnOpen=""HandleOnOpen""
          OnDismiss=""HandleOnDismiss""
          OnEscapeKeyDown=""HandleOnEscapeKeyDown""
          OnOverlayClick=""HandleOnOverlayClick"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Events modal</span>
        <BitButton Title=""Close""
                   Variant=""BitVariant.Text""
                   OnClick=""() => isEventsOpen = false""
                   IconName=""@BitIconName.ChromeClose"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>";
    private readonly string example11CsharpCode = @"
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

private void HandleOnOverlayClick()
{
    isOverlayClicked = true;
    _ = Task.Delay(2000).ContinueWith(_ =>
    {
        isOverlayClicked = false;
        InvokeAsync(StateHasChanged);
    });
}

private void HandleOnEscapeKeyDown()
{
    isEscapePressed = true;
    _ = Task.Delay(2000).ContinueWith(_ =>
    {
        isEscapePressed = false;
        InvokeAsync(StateHasChanged);
    });
}";

    private readonly string example12RazorCode = @"
<BitButton OnClick=""() => refModal.Open()"">Open</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => refModal.Toggle()"">Toggle</BitButton>

<BitModal @ref=""refModal"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Driven by methods</BitText>
        <BitText>This Modal has no IsOpen of its own: it is opened and closed through the reference to it.</BitText>
        <BitButton OnClick=""() => refModal.Close()"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example12CsharpCode = @"
private BitModal refModal = default!;";

    private readonly string example13RazorCode = @"
<style>
    .custom-class {
        border: 0.5rem solid tomato;
        background-color: darkgoldenrod;
    }

    .custom-root {
        border: 0.25rem solid #0054C6;
    }

    .custom-overlay {
        background-color: #ffbd5a66;
    }

    .custom-content {
        margin: 1rem;
        box-shadow: 0 0 10rem purple;
        border-end-end-radius: 1rem;
        border-end-start-radius: 1rem;
    }
</style>

<BitButton OnClick=""() => isOpenStyle = true"">Open styled modal</BitButton>
<BitButton OnClick=""() => isOpenClass = true"">Open classed modal</BitButton>

<BitModal @bind-IsOpen=""isOpenStyle"" Style=""box-shadow: inset 0px 0px 1.5rem 1.5rem palevioletred;"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Styled modal</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenStyle = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenClass"" Class=""custom-class"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Classed modal</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenClass = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>

<BitButton OnClick=""() => isOpenStyles = true"">Open modal styles</BitButton>
<BitButton OnClick=""() => isOpenClasses = true"">Open modal classes</BitButton>

<BitModal @bind-IsOpen=""isOpenStyles"" Styles=""@(new() { Overlay = ""background-color: #4776f433;"", Content = ""box-shadow: 0 0 1rem tomato;"" })"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Modal styles</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenStyles = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenClasses"" Classes=""@(new() { Root = ""custom-root"", Overlay = ""custom-overlay"", Content = ""custom-content"" })"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Modal classes</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenClasses = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>";
    private readonly string example13CsharpCode = @"
private bool isOpenStyle;
private bool isOpenClass;
private bool isOpenStyles;
private bool isOpenClasses;";

    private readonly string example14RazorCode = @"
<div dir=""rtl"">
    <BitButton Dir=""BitDir.Rtl"" OnClick=""() => isOpenRtl = true"">باز کردن مُدال</BitButton>
</div>

<BitModal Dir=""BitDir.Rtl"" @bind-IsOpen=""isOpenRtl"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">لورم ایپسوم</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenRtl = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </p>
        <p>
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </p>
    </div>
</BitModal>";
    private readonly string example14CsharpCode = @"
private bool isOpenRtl;";
}
