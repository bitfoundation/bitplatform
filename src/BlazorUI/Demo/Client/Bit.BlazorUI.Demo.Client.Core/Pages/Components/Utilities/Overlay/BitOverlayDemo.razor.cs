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
            Description = "When true, the scroll behavior of the scroller element behind the overlay will be disabled while the Overlay is open and handed back once it closes, with the room the scrollbar took given back as padding so nothing shifts sideways. The holds are counted, so a scroller two Overlays cover is only handed back once the last of them closes - the same hold a BitModal takes on the page, asked for the other way round. The scroller is named by ScrollerElement, then by ScrollerSelector; when neither is set it is the scroller of the BitAppShell the Overlay is inside of, and the page (body) when it is inside none.",
        },
        new()
        {
            Name = "Blocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "When enabled, prevents the Overlay from being light dismissed by clicking on the layer, for the overlays whose content has to be dealt with before the page comes back. The click is still reported through OnClick.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Overlay. A click on it never closes the Overlay - only a click on the layer around it does - so a surface hosted here keeps its own buttons, its own text selection and its own scrolling.",
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
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback that is called when the overlay is clicked, including the clicks on its content and the ones a Blocking Overlay refuses to be closed by, and before the Overlay closes.",
        },
        new()
        {
            Name = "OnClose",
            Type = "EventCallback",
            Description = "Callback that is called when the Overlay has closed, however it was closed - a click on the layer, the IsOpen binding, Close or Toggle - and after the scroller it was holding has been handed back.",
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "Callback that is called when the Overlay has opened, however it was opened - the IsOpen binding, Open, Toggle, or the first render of one that starts open through DefaultIsOpen.",
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
            Description = "The CSS selector of the scroller element whose scrolling is taken away while the Overlay is open, for AutoToggleScroll. An Overlay inside a BitAppShell holds the shell's scroller without being told to, since the shell cascades it; the page (body) is what is held when there is no shell and this is not set. The named scroller is also where an Overlay that is not holding it hands the wheel and the touch drag it catches, since a fixed layer would otherwise chain them to a document that never scrolls.",
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
    private bool ContentIsOpen;
    private int ContentClickCount;
    private bool BlockingIsOpen;
    private bool AbsoluteIsOpen;
    private bool AutoToggleIsOpen;
    private bool EnabledScrollerIsOpen;
    private bool DisabledScrollerIsOpen;
    private bool EventsIsOpen;
    private int EventsClickCount;
    private int EventsOpenCount;
    private int EventsCloseCount;
    private BitOverlay overlayRef = default!;
    private bool StyledIsOpen;
    private bool ClassedIsOpen;
    private bool RtlIsOpen;

    private void HandleOverlayClick(MouseEventArgs e)
    {
        EventsClickCount++;

        if (EventsClickCount >= 3)
        {
            EventsIsOpen = false;
        }
    }

    private void HandleOverlayOpen()
    {
        EventsClickCount = 0;
        EventsOpenCount++;
    }

    private void HandleOverlayClose()
    {
        EventsCloseCount++;
    }



    private readonly string example1RazorCode = @"
<style>
    .centered-overlay {
        align-items: center;
        justify-content: center;
    }
</style>


<BitButton OnClick=""() => BasicIsOpen = true"">Show Overlay</BitButton>
<BitButton OnClick=""() => ModeFullIsOpen = true"">Show dimmed Overlay</BitButton>

<BitOverlay @bind-IsOpen=""BasicIsOpen"" Class=""centered-overlay"">
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>

<BitOverlay @bind-IsOpen=""ModeFullIsOpen"" Class=""centered-overlay"" ModeFull>
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>";
    private readonly string example1CsharpCode = @"
private bool BasicIsOpen;
private bool ModeFullIsOpen;";

    private readonly string example2RazorCode = @"
<style>
    .centered-overlay {
        align-items: center;
        justify-content: center;
    }

    .content {
        width: 87%;
        display: flex;
        padding: 15px;
        overflow: auto;
        max-width: 960px;
        max-height: 288px;
        border-radius: 3px;
        position: relative;
        background-color: white;
        flex-flow: column nowrap;
        border: dodgerblue solid 1.6px;
    }

    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }
</style>


<BitButton OnClick=""() => ContentIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""ContentIsOpen"" Class=""centered-overlay"" ModeFull>
    <div class=""content"">
        <h3>Short story</h3>
        <div>
            Try it: select this text and let go of the button outside the box, or press one of the
            buttons below. The Overlay stays where it is. Clicking the dimmed layer around the box
            closes it.
        </div>
        <br />
        <div class=""btn-container"">
            <BitButton OnClick=""() => ContentClickCount++"">Clicked @ContentClickCount time(s)</BitButton>
            <BitButton Variant=""BitVariant.Outline"" OnClick=""() => ContentIsOpen = false"">Close</BitButton>
        </div>
    </div>
</BitOverlay>";
    private readonly string example2CsharpCode = @"
private bool ContentIsOpen;
private int ContentClickCount;";

    private readonly string example3RazorCode = @"
<style>
    .centered-overlay {
        align-items: center;
        justify-content: center;
    }

    .content {
        width: 87%;
        display: flex;
        padding: 15px;
        overflow: auto;
        max-width: 960px;
        max-height: 288px;
        border-radius: 3px;
        position: relative;
        background-color: white;
        flex-flow: column nowrap;
        border: dodgerblue solid 1.6px;
    }

    .close-button {
        right: 10px;
        position: absolute;
    }
</style>


<BitButton OnClick=""() => BlockingIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""BlockingIsOpen"" Class=""centered-overlay"" ModeFull Blocking>
    <div class=""content"">
        <BitButton Class=""close-button"" Variant=""BitVariant.Text"" OnClick=@(() => BlockingIsOpen = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
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
private bool BlockingIsOpen;";

    private readonly string example4RazorCode = @"
<style>
    .centered-overlay {
        align-items: center;
        justify-content: center;
    }

    .container {
        display: flex;
        height: 480px;
        position: relative;
        align-items: center;
        justify-content: center;
        border: 2px solid blue;
    }
</style>


<BitButton OnClick=""() => AbsoluteIsOpen = true"">Show Overlay</BitButton>

<div class=""container"">
    <BitOverlay @bind-IsOpen=""AbsoluteIsOpen""
                Class=""centered-overlay""
                ModeFull
                AbsolutePosition>
        <BitProgress Circular Indeterminate Thickness=""10"" />
    </BitOverlay>

    <h3>This is Container</h3>
</div>";
    private readonly string example4CsharpCode = @"
private bool AbsoluteIsOpen;";

    private readonly string example5RazorCode = @"
<style>
    .centered-overlay {
        align-items: center;
        justify-content: center;
    }
</style>


<BitButton OnClick=""() => AutoToggleIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""AutoToggleIsOpen"" Class=""centered-overlay"" ModeFull AutoToggleScroll>
    <BitStack FitSize Alignment=""BitAlignment.Center"">
        <BitText Style=""color: dodgerblue;"" Typography=""BitTypography.H3"">Please wait...</BitText>
        <BitProgress Indeterminate Thickness=""10"" Style=""width: 19rem;"" />
    </BitStack>
</BitOverlay>";
    private readonly string example5CsharpCode = @"
private bool AutoToggleIsOpen;";

    private readonly string example6RazorCode = @"
<style>
    .centered-overlay {
        align-items: center;
        justify-content: center;
    }

    .content {
        width: 87%;
        display: flex;
        padding: 15px;
        overflow: auto;
        max-width: 960px;
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
                Class=""centered-overlay""
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
                Class=""centered-overlay""
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
    .centered-overlay {
        align-items: center;
        justify-content: center;
    }

    .content {
        width: 87%;
        display: flex;
        padding: 15px;
        overflow: auto;
        max-width: 960px;
        max-height: 288px;
        border-radius: 3px;
        position: relative;
        background-color: white;
        flex-flow: column nowrap;
        border: dodgerblue solid 1.6px;
    }
</style>


<BitButton OnClick=""() => EventsIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""EventsIsOpen""
            Class=""centered-overlay""
            ModeFull
            Blocking
            OnClick=""HandleOverlayClick""
            OnOpen=""HandleOverlayOpen""
            OnClose=""HandleOverlayClose"">
    <div class=""content"">
        <h3>Click anywhere on the dimmed layer</h3>
        <div>The overlay has been clicked @EventsClickCount time(s). It closes on the third click.</div>
    </div>
</BitOverlay>

<div>Opened @EventsOpenCount time(s), closed @EventsCloseCount time(s).</div>";
    private readonly string example7CsharpCode = @"
private bool EventsIsOpen;
private int EventsClickCount;
private int EventsOpenCount;
private int EventsCloseCount;

private void HandleOverlayClick(MouseEventArgs e)
{
    EventsClickCount++;

    if (EventsClickCount >= 3)
    {
        EventsIsOpen = false;
    }
}

private void HandleOverlayOpen()
{
    EventsClickCount = 0;
    EventsOpenCount++;
}

private void HandleOverlayClose()
{
    EventsCloseCount++;
}";

    private readonly string example8RazorCode = @"
<style>
    .centered-overlay {
        align-items: center;
        justify-content: center;
    }

    .content {
        width: 87%;
        display: flex;
        padding: 15px;
        overflow: auto;
        max-width: 960px;
        max-height: 288px;
        border-radius: 3px;
        position: relative;
        background-color: white;
        flex-flow: column nowrap;
        border: dodgerblue solid 1.6px;
    }
</style>


<BitButton OnClick=""() => overlayRef.Open()"">Open</BitButton>

<BitOverlay @ref=""overlayRef"" Class=""centered-overlay"" ModeFull>
    <div class=""content"">
        <h3>Driven by methods</h3>
        <div>This Overlay has no IsOpen binding of its own: it is opened through the reference to it, and a click on the layer around this box still closes it.</div>
    </div>
</BitOverlay>";
    private readonly string example8CsharpCode = @"
private BitOverlay overlayRef = default!;";

    private readonly string example9RazorCode = @"
<style>
    .custom-overlay {
        align-items: center;
        justify-content: center;
        backdrop-filter: blur(10px);
        background-color: rgba(0, 0, 0, 0.2);
    }
</style>


<BitButton OnClick=""() => StyledIsOpen = true"">Show styled Overlay</BitButton>
<BitButton OnClick=""() => ClassedIsOpen = true"">Show classed Overlay</BitButton>

<BitOverlay @bind-IsOpen=""StyledIsOpen""
            Style=""align-items: center; justify-content: center; background: linear-gradient(135deg, rgba(78, 0, 142, 0.55), rgba(255, 0, 96, 0.35));"">
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>

<BitOverlay @bind-IsOpen=""ClassedIsOpen"" Class=""custom-overlay"">
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>";
    private readonly string example9CsharpCode = @"
private bool StyledIsOpen;
private bool ClassedIsOpen;";

    private readonly string example10RazorCode = @"
<style>
    .centered-overlay {
        align-items: center;
        justify-content: center;
    }

    .content {
        width: 87%;
        display: flex;
        padding: 15px;
        overflow: auto;
        max-width: 960px;
        max-height: 288px;
        border-radius: 3px;
        position: relative;
        background-color: white;
        flex-flow: column nowrap;
        border: dodgerblue solid 1.6px;
    }
</style>


<BitButton Dir=""BitDir.Rtl"" OnClick=""() => RtlIsOpen = true"">نمایش روکش</BitButton>

<BitOverlay @bind-IsOpen=""RtlIsOpen"" Class=""centered-overlay"" ModeFull Dir=""BitDir.Rtl"">
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
