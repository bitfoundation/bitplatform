namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Modal;

public partial class BitModalDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the auto scrollbar toggle behavior of the Modal.",
        },
        new()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Modal will be positioned absolute instead of fixed.",
        },
        new()
        {
            Name = "Blocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Modal can be light dismissed by clicking outside the Modal (on the overlay).",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of Modal, It can be Any custom tag or a text.",
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
            Name = "DragElementSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the drag element. by default the Modal container is the drag element.",
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
            Description = "Makes the Modal width and height 100% of its parent container.",
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
            Description = "Determines the ARIA role of the Modal (alertdialog/dialog). If this is set, it will override the ARIA role determined by Blocking and Modeless.",
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
            Name = "Modeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Modal should be modeless (e.g. not dismiss when focusing/clicking outside of the Modal). if true: Blocking is ignored, there will be no overlay.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when the Modal is dismissed.",
        },
        new()
        {
            Name = "OnOverlayClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when somewhere on the overlay element of the Modal is clicked.",
        },
        new()
        {
            Name = "Position",
            Type = "BitPosition?",
            DefaultValue = "null",
            Description = "Position of the Modal on the screen.",
            LinkType = LinkType.Link,
            Href = "#position-enum",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string",
            DefaultValue = "body",
            Description = "Set the element selector for which the Modal disables its scroll if applicable.",
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

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "position-enum",
            Name = "BitPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "TopLeft",
                    Value = "0"
                },
                new()
                {
                    Name = "TopCenter",
                    Value = "1"
                },
                new()
                {
                    Name = "TopRight",
                    Value = "2"
                },
                new()
                {
                    Name = "TopStart",
                    Value = "3"
                },
                new()
                {
                    Name = "TopEnd",
                    Value = "4"
                },
                new()
                {
                    Name = "CenterLeft",
                    Value = "5"
                },
                new()
                {
                    Name = "Center",
                    Value = "6"
                },
                new()
                {
                    Name = "CenterRight",
                    Value = "7"
                },
                new()
                {
                    Name = "CenterStart",
                    Value = "8"
                },
                new()
                {
                    Name = "CenterEnd",
                    Value = "9"
                },
                new()
                {
                    Name = "BottomLeft",
                    Value = "10"
                },
                new()
                {
                    Name = "BottomCenter",
                    Value = "11"
                },
                new()
                {
                    Name = "BottomRight",
                    Value = "12"
                },
                new()
                {
                    Name = "BottomStart",
                    Value = "13"
                },
                new()
                {
                    Name = "BottomEnd",
                    Value = "14"
                }
            ]
        }
    ];



    private bool isOpenBasic;

    private bool isOpenCustomContent;

    private bool isOpenBlocking;
    private bool isOpenAutoToggleScroll;
    private bool isOpenModeless;

    private bool isOpenAbsolutePosition;
    private bool isOpenScrollerSelector;

    private bool isOpenPosition;
    private BitPosition position;
    private void OpenModalInPosition(BitPosition positionValue)
    {
        isOpenPosition = true;
        position = positionValue;
    }

    private bool isOpenDraggable;
    private bool isOpenDraggableSelector;

    private bool isOpenFullSize;
    private bool isFullSize;

    private bool isEventsOpen;
    private bool isDismissed;
    private bool isOverlayClicked;
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


    private bool isOpenStyle;
    private bool isOpenClass;
    private bool isOpenStyles;
    private bool isOpenClasses;

    private bool isOpenRtl;


    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => isOpenBasic = true"">Open Modal</BitButton>

<BitModal @bind-IsOpen=""isOpenBasic"">
    <div style=""padding:1rem; max-width:40rem"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
        shaped into meaning, and the emotions ready to resonate with every reader.
        <br />
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
        each word has the power to transform into something extraordinary. Here lies the start of something new—an
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
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        <br />
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
        shaped into meaning, and the emotions ready to resonate with every reader.
        <br />
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
        each word has the power to transform into something extraordinary. Here lies the start of something new—an
        opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
        idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
        begins here, in this quiet moment where everything is possible.
    </div>
</BitModal>";
    private readonly string example2CsharpCode = @"
private bool isOpenCustomContent;";

    private readonly string example3RazorCode = @"
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


<BitButton OnClick=""() => isOpenBlocking = true"">Open Modal (Blocking)</BitButton>
<BitButton OnClick=""() => isOpenAutoToggleScroll = true"">Open Modal (AutoToggleScroll)</BitButton>
<BitButton OnClick=""() => isOpenModeless = true"">Open Modal (Modeless)</BitButton>

<BitModal @bind-IsOpen=""isOpenBlocking"" Blocking>
    <div class=""modal-header"">
        <span class=""modal-header-text"">Blocking</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenBlocking = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            In Blocking mode, the modal won't close by clicking outside (on the overlay).
        </p>
        <br />
        <p>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </p>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenAutoToggleScroll"" AutoToggleScroll>
    <div class=""modal-header"">
        <span class=""modal-header-text"">AutoToggleScroll</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenAutoToggleScroll = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            In AutoToggleScroll mode, the scrollbar of the scroll element 
            (body by default and customizable with the ScrollerSelector parameter)
            will be removed when the modal opens.
        </p>
        <br />
        <p>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </p>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""isOpenModeless"" Modeless>
    <div class=""modal-header"">
        <span class=""modal-header-text"">Modeless</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenModeless = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            In Modeless mode, the overlay element won't render.
        </p>
        <br />
        <p>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </p>
    </div>
</BitModal>";
    private readonly string example3CsharpCode = @"
private bool isOpenBlocking;
private bool isOpenAutoToggleScroll;
private bool isOpenModeless;";

    private readonly string example4RazorCode = @"
<style>
    .relative-container {
        width: 100%;
        height: 400px;
        overflow: auto;
        margin-top: 1rem;
        position: relative;
        background-color: #eee;
        border: 2px lightgreen solid;
    }

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


<BitButton OnClick=""() => isOpenAbsolutePosition = true"">Open Modal (AbsolutePosition)</BitButton>
<BitButton OnClick=""() => isOpenScrollerSelector = true"">Open Modal (ScrollerSelector)</BitButton>

<div class=""relative-container"">
    <BitModal @bind-IsOpen=""isOpenAbsolutePosition"" AbsolutePosition Modeless>
        <div class=""modal-header"">
            <span class=""modal-header-text"">AbsolutePosition & Modeless</span>
            <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenAbsolutePosition = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
        </div>
        <div class=""modal-body"">
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitModal>

    <BitModal @bind-IsOpen=""isOpenScrollerSelector"" AutoToggleScroll AbsolutePosition ScrollerSelector="".relative-container"">
        <div class=""modal-header"">
            <span class=""modal-header-text"">ScrollerSelector</span>
            <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenScrollerSelector = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
        </div>
        <div class=""modal-body"">
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitModal>

    <div>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
        for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        <br />
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        <br />
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
        shaped into meaning, and the emotions ready to resonate with every reader.
        <br />
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
        each word has the power to transform into something extraordinary. Here lies the start of something new—an
        opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
        idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
        begins here, in this quiet moment where everything is possible.
        <br />
        Imagine this space as a window into the future—empty yet alive with the energy of endless possibilities.
        These words stand as temporary guides, placeholders that whisper of what is to come.
        They hold the promise of stories waiting to unfold, ideas eager to take shape, and connections that
        will soon emerge to inspire and resonate. This is not an empty page; it is a canvas, rich with potential
        and ready to transform into something meaningful.
    </div>
</div>";
    private readonly string example4CsharpCode = @"
private bool isOpenAbsolutePosition;
private bool isOpenScrollerSelector;";

    private readonly string example5RazorCode = @"
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


<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.TopLeft)"">Top Left</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.TopCenter)"">Top Center</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.TopRight)"">Top Right</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.TopStart)"">Top Start</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.TopEnd)"">Top End</BitButton>

<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.CenterLeft)"">Center Left</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.Center)"">Center</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.CenterRight)"">Center Right</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.CenterStart)"">Center Start</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.CenterEnd)"">Center End</BitButton>

<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.BottomLeft)"">Bottom Left</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.BottomCenter)"">Bottom Center</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.BottomRight)"">Bottom Right</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.BottomStart)"">Bottom Start</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.BottomEnd)"">Bottom End</BitButton>

<BitModal @bind-IsOpen=""isOpenPosition"" Position=""position"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Modal positioning</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenPosition = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        BitModal with custom positioning. Once upon a time, stories wove connections between people.
    </div>
</BitModal>";
    private readonly string example5CsharpCode = @"
private bool isOpenPosition;
private BitPosition position;

private void OpenModalInPosition(BitPosition positionValue)
{
    isOpenPosition = true;
    position = positionValue;
}";

    private readonly string example6RazorCode = @"
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


<BitButton OnClick=""() => isOpenDraggable = true"">Open Modal</BitButton>
<BitModal @bind-IsOpen=""isOpenDraggable"" Draggable>
    <div class=""modal-header"">
        <span class=""modal-header-text"">Draggble Modal</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenDraggable = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>


<BitButton OnClick=""() => isOpenDraggableSelector = true"">Open Modal</BitButton>
<BitModal @bind-IsOpen=""isOpenDraggableSelector"" Draggable DragElementSelector="".modal-header-drag"">
    <div class=""modal-header modal-header-drag"">
        <span class=""modal-header-text"">Draggble Modal with custom drag element</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenDraggableSelector = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>";
    private readonly string example6CsharpCode = @"
private bool isOpenDraggable;
private bool isOpenDraggableSelector;";

    private readonly string example7RazorCode = @"
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


<BitButton OnClick=""() => isOpenFullSize = true"">Open Modal</BitButton>
<BitModal @bind-IsOpen=""isOpenFullSize"" FullSize=""isFullSize"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Full size modal</span>
        <BitButton Variant=""BitVariant.Text""
                   OnClick=""() => isFullSize = !isFullSize""
                   IconName=""@(isFullSize ? BitIconName.BackToWindow : BitIconName.ChromeFullScreen)""
                   Title=""@(isFullSize ? ""Exit FullScreen"" : ""FullScreen"")"" />
        <BitButton Variant=""BitVariant.Text""
                   OnClick=""() => isOpenFullSize = false""
                   IconName=""@BitIconName.ChromeClose""
                   Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>";
    private readonly string example7CsharpCode = @"
private bool isOpenFullSize;
private bool isFullSize;";

    private readonly string example8RazorCode = @"
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


<BitButton OnClick=""() => isEventsOpen = true"">Open Modal</BitButton>

<div>Dismissed? [@isDismissed]</div>

<div>Overlay clicked? [@isOverlayClicked]</div>

<BitModal @bind-IsOpen=""isEventsOpen""
          Draggable
          OnDismiss=""HandleOnDismiss""
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
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>";
    private readonly string example8CsharpCode = @"
private bool isEventsOpen;
private bool isDismissed;
private bool isOverlayClicked;

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
}";

    private readonly string example9RazorCode = @"
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
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
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
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
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
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
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
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>";
    private readonly string example9CsharpCode = @"
private bool isOpenStyle;
private bool isOpenClass;
private bool isOpenStyles;
private bool isOpenClasses;";

    private readonly string example10RazorCode = @"
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


<BitButton Dir=""BitDir.Rtl"" OnClick=""() => isOpenRtl = true"">باز کردن مُدال</BitButton>
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
        <p>
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </p>
    </div>
</BitModal>";
    private readonly string example10CsharpCode = @"
private bool isOpenRtl;";
}
