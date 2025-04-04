namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Overlay;

public partial class BitOverlayDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the scroll behavior of the Scroller element behind the overlay will be disabled.",
        },
        new()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Overlay will be positioned absolute instead of fixed.",
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
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Overlay and its content will be shown.",
        },
        new()
        {
            Name = "NoAutoClose",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Overlay will be closed by clicking on it.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the toggle button is clicked.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string",
            DefaultValue = "body",
            Description = "Set the selector of the Selector element for the Overlay to disable its scroll if applicable.",
        }
    ];



    private bool BasicIsOpen;
    private bool AutoCloseIsOpen;
    private bool AbsoluteIsOpen;
    private bool AutoToggleIsOpen;
    private bool EventOnCloseIsOpen;
    private bool EnabledScrollerIsOpen;
    private bool DisabledScrollerIsOpen;



    private readonly string example1RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }
</style>


<BitButton OnClick=""() => BasicIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""BasicIsOpen"" Class=""overlay"">
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>";
    private readonly string example1CsharpCode = @"
private bool BasicIsOpen;";

    private readonly string example2RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

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

<BitOverlay @bind-IsOpen=""AutoCloseIsOpen"" Class=""overlay"" NoAutoClose>
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
    private readonly string example2CsharpCode = @"
private bool AutoCloseIsOpen;";

    private readonly string example3RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
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


<BitButton Class=""show-button"" OnClick=""() => AbsoluteIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""AbsoluteIsOpen""
            Class=""overlay""
            AbsolutePosition>
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>

<h3>This is Container</h3>";
    private readonly string example3CsharpCode = @"
private bool AbsoluteIsOpen;";

    private readonly string example4RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }
</style>


<BitButton OnClick=""() => AutoToggleIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""AutoToggleIsOpen"" Class=""overlay"" AutoToggleScroll>
    <BitStack Alignment=""BitAlignment.Center"">
        <BitText Style=""color: dodgerblue;"" Typography=""BitTypography.H3"">Please wait...</BitText>
        <BitProgress Indeterminate Thickness=""10"" Style=""width: 19rem;""/>
    </BitStack>
</BitOverlay>";
    private readonly string example4CsharpCode = @"
private bool AutoToggleIsOpen;";

    private readonly string example5RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

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
                Class=""overlay""
                Style=""background-color:unset""
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
                Class=""overlay""
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
    private readonly string example5CsharpCode = @"
private bool EnabledScrollerIsOpen;
private bool DisabledScrollerIsOpen;";

    private readonly string example6RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

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


<BitButton OnClick=""() => EventOnCloseIsOpen = true"">Show Overlay</BitButton>
<BitOverlay @bind-IsOpen=""EventOnCloseIsOpen"" Class=""overlay"" OnClick=@(() => EventOnCloseIsOpen = false) NoAutoClose>
    <div class=""content"">
        <h3>Short story</h3>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </div>
</BitOverlay>";
    private readonly string example6CsharpCode = @"
private bool EventOnCloseIsOpen;";
}
