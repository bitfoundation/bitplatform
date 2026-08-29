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
            Description = "When true, the Modal is positioned absolute instead of fixed, so that it covers the element it was declared inside of rather than the screen. That element has to establish a containing block of its own (position: relative).",
        },
        new()
        {
            Name = "AriaModal",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the Modal should be announced as modal to assistive technologies. It is also what decides whether the Modal keeps the keyboard inside itself: a Modal that is not announced as modal leaves the page behind it reachable with the keyboard the way it is reachable with the pointer.",
        },
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the auto scrollbar toggle behavior of the Modal, which takes the overflow off the scroller while it is open and hands it back once it closes. A Modal that does this holds its own scroller, so the hold it would otherwise take on the page is stood down for it. The scroller is the one named by ScrollerElement or ScrollerSelector, then the one of the BitAppShell the Modal is inside of, and the page when it is inside none.",
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
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the body section of the Modal, the alias of ChildContent, which it takes precedence over. This is what a Modal that also declares a Header or a Footer uses to keep the three of them side by side.",
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
            Description = "The title (and aria-label) of the close button for accessibility and localization. Defaults to \"Close\" when not set.",
        },
        new()
        {
            Name = "CloseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the close button, provided as custom CSS classes of an external icon library. Takes precedence over CloseIconName when both are set.",
        },
        new()
        {
            Name = "CloseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the close button, from the built-in Fluent UI icons. Defaults to Cancel when not set.",
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
            Name = "DragElementSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the drag element, which is the content of the Modal by default. Ignored by a Modal that is not Draggable.",
        },
        new()
        {
            Name = "Draggable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Modal can be dragged around.",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template used to render the footer section of the Modal.",
        },
        new()
        {
            Name = "FooterText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the footer section of the Modal.",
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
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Modal width and height 100% of its parent container, which is FullWidth and FullHeight in one parameter.",
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
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template used to render the header section of the Modal. Takes precedence over HeaderText when both are set.",
        },
        new()
        {
            Name = "HeaderText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the header section of the Modal.",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS height of the Modal (any CSS length). A Modal is as tall as its content when this is not set. It is written as an inline style on the content box, so it takes precedence over FullHeight, and it is capped by MaxHeight - or, when that is not set either, by the height of the screen.",
        },
        new()
        {
            Name = "IsAlert",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Determines the ARIA role of the Modal (alertdialog/dialog). A Blocking Modal that is not Modeless announces itself as an alertdialog when this is not set, since a surface that refuses to be dismissed by a click outside of it is one waiting to be answered.",
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
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS height the Modal is not to grow past, however long its content is (any CSS length). The height of the screen is the cap when this is not set, which is what keeps a Modal longer than the screen reachable: it scrolls inside itself rather than running off both ends of the page.",
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS width the Modal is not to grow past, however wide its content is (any CSS length). The width of the screen is the cap when this is not set, which leaves a Modal as wide as its content - and on a wide screen that can be a line of text too long to read comfortably.",
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
            Description = "Whether the Modal should be modeless (e.g. not dismiss when focusing/clicking outside of the Modal). If true: Blocking is ignored, there is no overlay, and the Modal neither reports itself modal nor holds the keyboard or the page.",
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
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default top border of the Modal.",
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
            Description = "Prevents the Modal from holding the page still while it is open. By default the page behind an open Modal is held, with the room the scrollbar took added back as padding so that nothing shifts sideways; the holds are counted, so the page is only handed back once the last open Modal closes. A Modeless Modal never holds the page in the first place, and a Modal that toggles the scroll itself (see AutoToggleScroll) holds its scroller instead. The gestures that land on a Modal that leaves the page scrolling are handed to the scroller behind it - the one ScrollerElement or ScrollerSelector names, or the application shell's - since the layer the Modal is drawn in is fixed to the viewport, where the wheel would else reach a document that does not scroll.",
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
            Name = "Position",
            Type = "BitPosition?",
            DefaultValue = "null",
            Description = "Position of the Modal on the screen. The Modal sits in the middle of the area it covers when this is not set.",
            LinkType = LinkType.Link,
            Href = "#position-enum",
        },
        new()
        {
            Name = "ScrollerElement",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "The element reference of the scroller the Modal holds while it is open. Takes precedence over ScrollerSelector and over the scroller a BitAppShell cascades, and is read by both holds: the one the Modal takes by default and the overflow toggle of AutoToggleScroll.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the element whose scrolling the Modal holds while it is open. A Modal inside a BitAppShell holds the shell's scroller without being told to, since the shell cascades it; the page (body) is what is held when there is no shell and this is not set. Any other layout that scrolls a region of its own names that region here, since holding a page that never scrolls holds nothing.",
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the close button of the Modal, which closes it without a handler of its own.",
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
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS width of the Modal (any CSS length). A Modal is as wide as its content when this is not set. It is written as an inline style on the content box, so it takes precedence over FullWidth, and it is capped by MaxWidth - or, when that is not set either, by the width of the screen.",
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
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "position-enum",
            Name = "BitPosition",
            Description = "Where the Modal sits inside the area it covers. The Start and End members are the direction-aware counterparts of Left and Right.",
            Items =
            [
                new() { Name = "TopLeft", Value = "0" },
                new() { Name = "TopCenter", Value = "1" },
                new() { Name = "TopRight", Value = "2" },
                new() { Name = "TopStart", Value = "3" },
                new() { Name = "TopEnd", Value = "4" },
                new() { Name = "CenterLeft", Value = "5" },
                new() { Name = "Center", Value = "6" },
                new() { Name = "CenterRight", Value = "7" },
                new() { Name = "CenterStart", Value = "8" },
                new() { Name = "CenterEnd", Value = "9" },
                new() { Name = "BottomLeft", Value = "10" },
                new() { Name = "BottomCenter", Value = "11" },
                new() { Name = "BottomRight", Value = "12" },
                new() { Name = "BottomStart", Value = "13" },
                new() { Name = "BottomEnd", Value = "14" }
            ]
        }
    ];



    private bool isOpenBasic;
    private bool isOpenNoBorder;

    private bool isOpenCustomContent;

    private bool isOpenHeaderText;
    private bool isOpenHeaderTemplate;
    private bool isOpenFooter;

    private bool isOpenBlocking;

    private bool isOpenEscape;
    private bool isOpenNoEscape;

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

    private bool isOpenOuter;
    private bool isOpenInner;
    private void HandleNestedDelete()
    {
        isOpenInner = false;
        isOpenOuter = false;
    }

    private bool isOpenExternalIcon;

    private bool isOpenStyle;
    private bool isOpenClass;
    private bool isOpenStyles;
    private bool isOpenClasses;
    private bool isOpenChromeClasses;

    private bool isOpenRtl;


    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => isOpenBasic = true"">Open Modal</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenNoBorder = true"">NoBorder</BitButton>

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
</BitModal>

<BitModal @bind-IsOpen=""isOpenNoBorder"" NoBorder>
    <div style=""padding:1rem;max-width:40rem"">
        The accent line along the top edge of the Modal is gone.
        <br />
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
private bool isOpenBasic;
private bool isOpenNoBorder;";

    private readonly string example2RazorCode = @"
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
<BitButton OnClick=""() => isOpenHeaderText = true"">HeaderText</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenHeaderTemplate = true"">Header template</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenFooter = true"">Header &amp; Footer</BitButton>

<BitModal @bind-IsOpen=""isOpenHeaderText"" MaxWidth=""32rem"" ShowCloseButton HeaderText=""Release notes"">
    <BitText>
        The title, the close button and the room around this text all come from the Modal itself, so
        nothing here has to lay them out.
    </BitText>
</BitModal>

<BitModal @bind-IsOpen=""isOpenHeaderTemplate"" MaxWidth=""32rem"" ShowCloseButton>
    <Header>
        <BitStack Gap=""0.5rem"" AutoHeight>
            <BitText Typography=""BitTypography.H6"">Search the docs</BitText>
            <BitSearchBox Placeholder=""Search here..."" />
        </BitStack>
    </Header>
    <Body>
        <BitText>A Header template takes whatever the title bar has to hold - here a search box under the title.</BitText>
    </Body>
</BitModal>

<BitModal @bind-IsOpen=""isOpenFooter"" MaxWidth=""32rem"" ShowCloseButton HeaderText=""Unsaved changes"">
    <Body>
        <BitText>The footer stays at the bottom of the Modal while this body scrolls between it and the header.</BitText>
    </Body>
    <Footer>
        <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
            <BitButton OnClick=""() => isOpenFooter = false"">Save</BitButton>
            <BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenFooter = false"">Discard</BitButton>
        </BitStack>
    </Footer>
</BitModal>";
    private readonly string example3CsharpCode = @"
private bool isOpenHeaderText;
private bool isOpenHeaderTemplate;
private bool isOpenFooter;";

    private readonly string example4RazorCode = @"
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
    private readonly string example4CsharpCode = @"
private bool isOpenBlocking;";

    private readonly string example5RazorCode = @"
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
    private readonly string example5CsharpCode = @"
private bool isOpenEscape;
private bool isOpenNoEscape;";

    private readonly string example6RazorCode = @"
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
    private readonly string example6CsharpCode = @"
private bool isOpenFocus;
private bool isOpenNoFocus;
private bool isOpenAutoFocus;
private readonly Dictionary<string, object> autoFocusAttributes = new() { { ""data-autofocus"", true } };";

    private readonly string example7RazorCode = @"
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
</BitModal>

<BitModal @bind-IsOpen=""isOpenAutoToggleScroll"" AutoToggleScroll MaxWidth=""30rem"" ShowCloseButton HeaderText=""AutoToggleScroll"">
    <BitText>The overflow of the page was taken away while this Modal is open, and is handed back when it closes.</BitText>
</BitModal>";
    private readonly string example7CsharpCode = @"
private bool isOpenScrollLock;
private bool isOpenNoScrollLock;
private bool isOpenAutoToggleScroll;";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""() => isOpenMaxWidth = true"">MaxWidth</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenFixedSize = true"">Width & Height</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenMaxHeight = true"">MaxHeight</BitButton>

<BitModal @bind-IsOpen=""isOpenMaxWidth"" MaxWidth=""32rem"">
    <div class=""modal-content modal-content-wide"">
        <BitText Typography=""BitTypography.H6"">MaxWidth</BitText>
        <BitText>
            However long this paragraph gets, the Modal stops growing at 32rem and the text wraps
            instead - which is what keeps a line short enough to be read on a wide screen.
        </BitText>
        <BitButton OnClick=""() => isOpenMaxWidth = false"">Close</BitButton>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenFixedSize"" Width=""24rem"" Height=""18rem"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Width & Height</BitText>
        <BitText>This Modal is 24rem by 18rem whatever is put in it, so it never resizes under the user.</BitText>
        <BitButton OnClick=""() => isOpenFixedSize = false"">Close</BitButton>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenMaxHeight"" MaxWidth=""30rem"" MaxHeight=""16rem"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">MaxHeight</BitText>
        <BitText>The Modal stops at 16rem and scrolls inside itself from there, short of the edge of the screen. ...</BitText>
        <BitButton OnClick=""() => isOpenMaxHeight = false"">Close</BitButton>
    </div>
</BitModal>

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

<BitModal @bind-IsOpen=""isOpenFullSize"" FullSize>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">FullSize</BitText>
        <BitText>This Modal takes the whole screen, which is FullWidth and FullHeight in one parameter.</BitText>
        <BitButton OnClick=""() => isOpenFullSize = false"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example8CsharpCode = @"
private bool isOpenMaxWidth;
private bool isOpenMaxHeight;
private bool isOpenFixedSize;
private bool isOpenFullWidth;
private bool isOpenFullHeight;
private bool isOpenFullSize;";

    private readonly string example9RazorCode = @"
<BitButton OnClick=""() => isOpenModeFull = true"">ModeFull</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenModeless = !isOpenModeless"">Modeless</BitButton>

<BitModal @bind-IsOpen=""isOpenModeFull"" ModeFull>
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">ModeFull</BitText>
        <BitText>The overlay behind this Modal is opaque, so the page under it is dimmed rather than merely covered.</BitText>
        <BitButton OnClick=""() => isOpenModeFull = false"">Close</BitButton>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenModeless"" Modeless ShowCloseButton HeaderText=""Modeless"" MaxWidth=""22rem"">
    <BitText>Carry on with the page while this one is open: it holds neither the pointer, nor the keyboard, nor the scroll.</BitText>
</BitModal>";
    private readonly string example9CsharpCode = @"
private bool isOpenModeFull;
private bool isOpenModeless;";

    private readonly string example10RazorCode = @"
<style>
    .relative-container {
        width: 100%;
        height: 20rem;
        overflow: auto;
        margin-top: 1rem;
        position: relative;
        border: 2px lightgreen solid;
    }
</style>

<BitButton OnClick=""() => OpenModalInPosition(BitPosition.TopLeft)"">Top Left</BitButton>
<BitButton OnClick=""() => OpenModalInPosition(BitPosition.Center)"">Center</BitButton>
<BitButton OnClick=""() => OpenModalInPosition(BitPosition.BottomRight)"">Bottom Right</BitButton>

<BitModal @bind-IsOpen=""isOpenPosition"" Position=""position"" ShowCloseButton MaxWidth=""24rem"">
    <Header>Position: @position</Header>
    <Body>
        <BitText>This Modal is placed by the Position parameter rather than by the middle of the screen.</BitText>
    </Body>
</BitModal>

<BitButton OnClick=""() => isOpenAbsolute = true"">AbsolutePosition</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenAbsoluteScroller = true"">AbsolutePosition with its own scroller</BitButton>

<div class=""relative-container"" id=""modal-scroller"">
    <BitModal @bind-IsOpen=""isOpenAbsolute"" AbsolutePosition ShowCloseButton HeaderText=""AbsolutePosition"" MaxWidth=""24rem"">
        <BitText>This Modal covers the bordered box it was declared inside of rather than the page.</BitText>
    </BitModal>

    <BitModal @bind-IsOpen=""isOpenAbsoluteScroller""
              AbsolutePosition
              AutoToggleScroll
              ShowCloseButton
              MaxWidth=""24rem""
              ScrollerSelector=""#modal-scroller""
              HeaderText=""AbsolutePosition and AutoToggleScroll"">
        <BitText>The box behind this Modal is the scroller that was held still, named by ScrollerSelector.</BitText>
    </BitModal>

    <div>Lorem ipsum dolor sit amet, consectetur adipiscing elit...</div>
</div>";
    private readonly string example10CsharpCode = @"
private bool isOpenPosition;
private BitPosition position = BitPosition.Center;
private void OpenModalInPosition(BitPosition positionValue)
{
    position = positionValue;
    isOpenPosition = true;
}

private bool isOpenAbsolute;
private bool isOpenAbsoluteScroller;";

    private readonly string example11RazorCode = @"
<BitButton OnClick=""() => isOpenDraggable = true"">Draggable</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenDragHandle = true"">DragElementSelector</BitButton>

<BitModal @bind-IsOpen=""isOpenDraggable"" Draggable ShowCloseButton HeaderText=""Drag me"" MaxWidth=""26rem"">
    <BitText>Press anywhere on this Modal and drag it around the screen.</BitText>
</BitModal>

<BitModal @bind-IsOpen=""isOpenDragHandle"" Draggable DragElementSelector=""#modal-drag-handle"" ShowCloseButton MaxWidth=""26rem"">
    <Header>
        <div id=""modal-drag-handle"" class=""modal-drag-handle"">Drag me by this bar</div>
    </Header>
    <Body>
        <BitText>Only the bar above drags this Modal, so this text can still be selected.</BitText>
    </Body>
</BitModal>";
    private readonly string example11CsharpCode = @"
private bool isOpenDraggable;
private bool isOpenDragHandle;";

    private readonly string example12RazorCode = @"
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
    private readonly string example12CsharpCode = @"
private bool isOpenAlert;
private bool isOpenLabelled;";

    private readonly string example13RazorCode = @"
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
    private readonly string example13CsharpCode = @"
private bool isOpenKeptMounted;
private bool isOpenNotKeptMounted;";

    private readonly string example14RazorCode = @"
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
    private readonly string example14CsharpCode = @"
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

    private readonly string example15RazorCode = @"
<BitButton OnClick=""() => refModal.Open()"">Open</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => refModal.Toggle()"">Toggle</BitButton>

<BitModal @ref=""refModal"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Driven by methods</BitText>
        <BitText>This Modal has no IsOpen of its own: it is opened and closed through the reference to it.</BitText>
        <BitButton OnClick=""() => refModal.Close()"">Close</BitButton>
    </div>
</BitModal>";
    private readonly string example15CsharpCode = @"
private BitModal refModal = default!;";

    private readonly string example16RazorCode = @"
<BitButton OnClick=""() => isOpenOuter = true"">Open Modal</BitButton>

<BitModal @bind-IsOpen=""isOpenOuter"" MaxWidth=""30rem"">
    <div class=""modal-content"">
        <BitText Typography=""BitTypography.H6"">Project settings</BitText>
        <BitTextField Label=""Project name"" />
        <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
            <BitButton OnClick=""() => isOpenOuter = false"">Save</BitButton>
            <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.Error"" OnClick=""() => isOpenInner = true"">Delete</BitButton>
        </BitStack>

        <BitModal @bind-IsOpen=""isOpenInner"" IsAlert Blocking AriaLabel=""Confirm the deletion"">
            <div class=""modal-content"">
                <BitText Typography=""BitTypography.H6"">Delete this project?</BitText>
                <BitText>Escape closes this one and leaves the settings behind it open.</BitText>
                <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
                    <BitButton Color=""BitColor.Error"" OnClick=""HandleNestedDelete"">Delete</BitButton>
                    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => isOpenInner = false"">Cancel</BitButton>
                </BitStack>
            </div>
        </BitModal>
    </div>
</BitModal>";
    private readonly string example16CsharpCode = @"
private bool isOpenOuter;
private bool isOpenInner;

private void HandleNestedDelete()
{
    isOpenInner = false;
    isOpenOuter = false;
}";

    private readonly string example17RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitButton OnClick=""() => isOpenExternalIcon = true"">Open Modal</BitButton>

<BitModal @bind-IsOpen=""isOpenExternalIcon""
          MaxWidth=""32rem""
          ShowCloseButton
          HeaderText=""External close icon""
          CloseIcon=""@BitIconInfo.Fa(&quot;solid xmark&quot;)"">
    <BitText>The close button of this Modal wears a FontAwesome icon.</BitText>
</BitModal>";
    private readonly string example17CsharpCode = @"
private bool isOpenExternalIcon;";

    private readonly string example18RazorCode = @"
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
</BitModal>

<BitModal @bind-IsOpen=""isOpenChromeClasses""
          MaxWidth=""32rem""
          ShowCloseButton
          HeaderText=""Classed chrome""
          FooterText=""This is a footer text!""
          Classes=""@(new() { HeaderContainer = ""custom-header-container"",
                             Header = ""custom-header"",
                             Body = ""custom-body"",
                             Footer = ""custom-footer"" })"">
    <BitText>Every part of the chrome - the header container, the header, the body and the footer - takes a class of its own.</BitText>
</BitModal>";
    private readonly string example18CsharpCode = @"
private bool isOpenStyle;
private bool isOpenClass;
private bool isOpenStyles;
private bool isOpenClasses;
private bool isOpenChromeClasses;";

    private readonly string example19RazorCode = @"
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
    private readonly string example19CsharpCode = @"
private bool isOpenRtl;";
}
