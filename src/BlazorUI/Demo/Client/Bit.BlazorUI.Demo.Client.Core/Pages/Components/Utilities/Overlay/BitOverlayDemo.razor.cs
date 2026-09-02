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
            Description = "When true, the Overlay will be positioned absolute instead of fixed, so that it covers the element it was declared inside of rather than the screen. That element has to establish a containing block of its own (position: relative).",
        },
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the scroll behavior of the scroller element behind the overlay will be disabled while the Overlay is open and handed back once it closes. The scroller is named by ScrollerElement, then by ScrollerSelector; when neither is set it is the scroller of the BitAppShell the Overlay is inside of, and the page (body) when it is inside none.",
        },
        new()
        {
            Name = "Center",
            Type = "bool",
            DefaultValue = "false",
            Description = "Centers the content of the Overlay horizontally and vertically. Without it the content stretches over the whole layer, which is the layout a surface of the consumer's own wants.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Overlay.",
        },
        new()
        {
            Name = "DefaultIsOpen",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The initial opening state of the Overlay in the uncontrolled mode, which is when the IsOpen parameter is not set.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Overlay and its content will be shown.",
        },
        new()
        {
            Name = "ModeFull",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the Overlay in full mode that gives it an opaque background using the theme's overlay background color. It is transparent otherwise, for the overlays that are a click catcher rather than a backdrop.",
        },
        new()
        {
            Name = "NoAutoClose",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Overlay will not be closed by clicking on it. The click is still reported through OnClick.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback that is called when the overlay is clicked, including the clicks a NoAutoClose Overlay refuses to be closed by, and before the Overlay closes.",
        },
        new()
        {
            Name = "ScrollerElement",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "The element reference of the scroller whose scrolling is taken away while the Overlay is open, for the layouts whose scroller cannot be named by a selector. Takes precedence over ScrollerSelector.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the scroller element whose scrolling is taken away while the Overlay is open, for AutoToggleScroll. An Overlay inside a BitAppShell holds the shell's scroller without being told to, since the shell cascades it; the page (body) is what is held when there is no shell and this is not set.",
        },
        new()
        {
            Name = "ZIndex",
            Type = "int?",
            DefaultValue = "null",
            Description = "The layer the Overlay is stacked at, which takes over from the one the whole library shares - for an Overlay that has to sit above (or below) another surface of the page.",
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Open()",
            Type = "() => Task",
            Description = "Opens the Overlay, unless it is disabled.",
        },
        new()
        {
            Name = "Close()",
            Type = "() => Task",
            Description = "Closes the Overlay. It closes whether or not the Overlay is enabled, so that an Overlay disabled while it was open can still be taken off the screen by the code that owns it.",
        },
        new()
        {
            Name = "Toggle()",
            Type = "() => Task",
            Description = "Opens the Overlay when it is closed, and closes it when it is open.",
        },
    ];



    private bool BasicIsOpen;
    private bool ModeFullIsOpen;
    private bool AutoCloseIsOpen;
    private bool AbsoluteIsOpen;
    private bool AutoToggleIsOpen;
    private bool EnabledScrollerIsOpen;
    private bool DisabledScrollerIsOpen;
    private bool EventsIsOpen;
    private int EventsClickCount;
    private BitOverlay overlayRef = default!;
    private bool StyledIsOpen;
    private bool ClassedIsOpen;
    private bool RtlIsOpen;

    private void HandleEventsShow()
    {
        EventsClickCount = 0;
        EventsIsOpen = true;
    }

    private void HandleOverlayClick(MouseEventArgs e)
    {
        EventsClickCount++;

        if (EventsClickCount >= 3)
        {
            EventsIsOpen = false;
        }
    }



    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => BasicIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""BasicIsOpen"" Center>
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>";
    private readonly string example1CsharpCode = @"
private bool BasicIsOpen;";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => ModeFullIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""ModeFullIsOpen"" Center ModeFull>
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>";
    private readonly string example2CsharpCode = @"
private bool ModeFullIsOpen;";

    private readonly string example3RazorCode = @"
<style>
    .content {
        width: 85%;
        height: 250px;
        display: flex;
        padding: 15px;
        overflow: auto;
        border-radius: 3px;
        background-color: white;
        flex-flow: column nowrap;
    }

    .close-button {
        right: 10px;
        position: absolute;
    }
</style>


<BitButton OnClick=""() => AutoCloseIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""AutoCloseIsOpen"" Center ModeFull NoAutoClose>
    <div class=""content"">
        <BitButton Class=""close-button"" Variant=""BitVariant.Text"" OnClick=@(() => AutoCloseIsOpen = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
        <h3>Short story</h3>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </div>
</BitOverlay>";
    private readonly string example3CsharpCode = @"
private bool AutoCloseIsOpen;";

    private readonly string example4RazorCode = @"
<style>
    .container {
        display: flex;
        height: 480px;
        position: relative;
        align-items: center;
        justify-content: center;
        border: 2px solid blue;
    }

    .show-button {
        gap: 5px;
        top: 15px;
        left: 10px;
        display: flex;
        width: fit-content;
        position: absolute;
        flex-flow: row wrap;
    }
</style>


<div class=""container"">
    <BitButton Class=""show-button"" OnClick=""() => AbsoluteIsOpen = true"">Show Overlay</BitButton>

    <BitOverlay @bind-IsOpen=""AbsoluteIsOpen""
                Center
                ModeFull
                AbsolutePosition>
        <BitProgress Circular Indeterminate Thickness=""10"" />
    </BitOverlay>

    <h3>This is Container</h3>
</div>";
    private readonly string example4CsharpCode = @"
private bool AbsoluteIsOpen;";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""() => AutoToggleIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""AutoToggleIsOpen"" Center ModeFull AutoToggleScroll>
    <BitStack Alignment=""BitAlignment.Center"">
        <BitText Style=""color: dodgerblue;"" Typography=""BitTypography.H3"">Please wait...</BitText>
        <BitProgress Indeterminate Thickness=""10"" Style=""width: 19rem;"" />
    </BitStack>
</BitOverlay>";
    private readonly string example5CsharpCode = @"
private bool AutoToggleIsOpen;";

    private readonly string example6RazorCode = @"
<style>
    .content {
        width: 87%;
        display: flex;
        padding: 15px;
        overflow: auto;
        max-height: 288px;
        border-radius: 3px;
        position: relative;
        background-color: white;
        flex-flow: column nowrap;
        border: dodgerblue solid 1.6px;
    }

    .scroller {
        height: 360px;
        padding: 15px;
        overflow: auto;
        margin-top: 15px;
        position: relative;
        border-radius: 3px;
        align-items: center;
        border: 2px solid green;
    }
</style>


<BitButton OnClick=""() => EnabledScrollerIsOpen = true"">Show with Enabled scrolling</BitButton>
<BitButton OnClick=""() => DisabledScrollerIsOpen = true"">Show with Disabled scrolling</BitButton>

<div class=""scroller"">
    <BitOverlay @bind-IsOpen=""EnabledScrollerIsOpen""
                Center
                ScrollerSelector="".scroller""
                AbsolutePosition>
        <div class=""content"">
            <h3>Short story</h3>
            <div>
                Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
                Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
                when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
                for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
            </div>
        </div>
    </BitOverlay>

    <BitOverlay @bind-IsOpen=""DisabledScrollerIsOpen""
                Center
                ModeFull
                ScrollerSelector="".scroller""
                AbsolutePosition
                AutoToggleScroll>
        <BitProgress Circular Indeterminate Thickness=""10"" />
    </BitOverlay>

    <h3>Short story</h3>
    <div>
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
        <br />
        Imagine this space as a window into the future empty yet alive with the energy of endless possibilities.
        These words stand as temporary guides, placeholders that whisper of what is to come.
        They hold the promise of stories waiting to unfold, ideas eager to take shape, and
        connections that will soon emerge to inspire and resonate. This is not an empty page;
        it is a canvas, rich with potential and ready to transform into something meaningful.
        <br />
        For now, these lines are here to remind you of the beauty of beginnings. They are the quiet before the symphony,
        the foundation upon which your creativity will build. Soon, this space will hold your thoughts, your visions,
        and your voice a reflection of who you are and what you wish to share with the world. Every sentence will carry
        purpose, every word will invite others to connect, to think, to feel. So take a moment to dream, to imagine
        what this blank slate can become. Whether it’s a story, an idea, or a message that matters, this is your
        starting point. The possibilities are endless, and the journey begins now.
    </div>
</div>";
    private readonly string example6CsharpCode = @"
private bool EnabledScrollerIsOpen;
private bool DisabledScrollerIsOpen;";

    private readonly string example7RazorCode = @"
<style>
    .content {
        width: 85%;
        height: 250px;
        display: flex;
        padding: 15px;
        overflow: auto;
        border-radius: 3px;
        background-color: white;
        flex-flow: column nowrap;
    }
</style>


<BitButton OnClick=""HandleEventsShow"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""EventsIsOpen"" Center ModeFull OnClick=""HandleOverlayClick"" NoAutoClose>
    <div class=""content"">
        <h3>Click anywhere on the overlay</h3>
        <div>The overlay has been clicked @EventsClickCount time(s). It closes on the third click.</div>
    </div>
</BitOverlay>";
    private readonly string example7CsharpCode = @"
private bool EventsIsOpen;
private int EventsClickCount;

private void HandleEventsShow()
{
    EventsClickCount = 0;
    EventsIsOpen = true;
}

private void HandleOverlayClick(MouseEventArgs e)
{
    EventsClickCount++;

    if (EventsClickCount >= 3)
    {
        EventsIsOpen = false;
    }
}";

    private readonly string example8RazorCode = @"
<style>
    .content {
        width: 85%;
        height: 250px;
        display: flex;
        padding: 15px;
        overflow: auto;
        border-radius: 3px;
        background-color: white;
        flex-flow: column nowrap;
    }
</style>


<BitButton OnClick=""() => overlayRef.Open()"">Open</BitButton>

<BitOverlay @ref=""overlayRef"" Center ModeFull>
    <div class=""content"">
        <h3>Driven by methods</h3>
        <div>This Overlay has no IsOpen binding of its own: it is opened through the reference to it, and a click anywhere on it still closes it.</div>
    </div>
</BitOverlay>";
    private readonly string example8CsharpCode = @"
private BitOverlay overlayRef = default!;";

    private readonly string example9RazorCode = @"
<style>
    .custom-overlay {
        backdrop-filter: blur(10px);
        background-color: rgba(0, 0, 0, 0.2);
    }
</style>


<BitButton OnClick=""() => StyledIsOpen = true"">Show styled Overlay</BitButton>
<BitButton OnClick=""() => ClassedIsOpen = true"">Show classed Overlay</BitButton>

<BitOverlay @bind-IsOpen=""StyledIsOpen""
            Center
            Style=""background: linear-gradient(135deg, rgba(78, 0, 142, 0.55), rgba(255, 0, 96, 0.35));"">
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>

<BitOverlay @bind-IsOpen=""ClassedIsOpen"" Center Class=""custom-overlay"">
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>";
    private readonly string example9CsharpCode = @"
private bool StyledIsOpen;
private bool ClassedIsOpen;";

    private readonly string example10RazorCode = @"
<style>
    .content {
        width: 85%;
        height: 250px;
        display: flex;
        padding: 15px;
        overflow: auto;
        border-radius: 3px;
        background-color: white;
        flex-flow: column nowrap;
    }
</style>


<BitButton Dir=""BitDir.Rtl"" OnClick=""() => RtlIsOpen = true"">نمایش روکش</BitButton>

<BitOverlay @bind-IsOpen=""RtlIsOpen"" Center ModeFull Dir=""BitDir.Rtl"">
    <div class=""content"">
        <h3>داستان کوتاه</h3>
        <div>
            روزی روزگاری، داستان‌ها میان مردم پیوند می‌ساختند؛ هم‌نوایی صداهایی که رویاهای مشترک می‌آفریدند.
            هر واژه معنایی داشت و هر درنگ، فهمی تازه به همراه می‌آورد. این متن جای‌نگهدار، یادآور لحظه‌ای است
            که امکان‌ها بی‌پایان‌اند و در انتظار محتوایی هستند تا شکل بگیرد. این‌جا جایی است که سفر آغاز می‌شود؛
            واژه‌های شما راه را نشان خواهند داد.
        </div>
    </div>
</BitOverlay>";
    private readonly string example10CsharpCode = @"
private bool RtlIsOpen;";
}
