namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Sticky;

public partial class BitStickyDemo
{
    private bool isStuck;
    private bool isStickyEnabled = true;

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Bottom",
            Type = "string?",
            DefaultValue = "null",
            Description = "The vertical offset the element pins at from the bottom edge. A bare number is read as a pixel count; anything else is used as written, so any CSS length is accepted."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Sticky, it can be any custom tag or text."
        },
        new()
        {
            Name = "Left",
            Type = "string?",
            DefaultValue = "null",
            Description = "The horizontal offset the element pins at from the left edge, for a container that scrolls horizontally. A bare number is read as a pixel count; any CSS length is accepted."
        },
        new()
        {
            Name = "OnStuckChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Callback for when the stuck state changes: true while the element is pinned to an edge of its scrolling container. Using it (or StuckClass/StuckStyle) attaches the stuck detection."
        },
        new()
        {
            Name = "Position",
            Type = "BitStickyPosition?",
            DefaultValue = "null",
            Description = "The edge of the scrolling container the element pins to. Start and End follow the reading direction. When neither a Position nor any offset is set, the component sticks to the top.",
            Href = "#sticky-position-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Right",
            Type = "string?",
            DefaultValue = "null",
            Description = "The horizontal offset the element pins at from the right edge, for a container that scrolls horizontally. A bare number is read as a pixel count; any CSS length is accepted."
        },
        new()
        {
            Name = "StuckClass",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS class applied to the root element only while the component is stuck - a shadow, an opaque background, a border once content passes underneath. The bit-stk-stc class accompanies it."
        },
        new()
        {
            Name = "StuckStyle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS style applied to the root element only while the component is stuck, the inline counterpart of StuckClass."
        },
        new()
        {
            Name = "Top",
            Type = "string?",
            DefaultValue = "null",
            Description = "The vertical offset the element pins at from the top edge. A bare number is read as a pixel count; anything else is used as written, so any CSS length is accepted."
        },
        new()
        {
            Name = "ZIndex",
            Type = "int?",
            DefaultValue = "null",
            Description = "The z-index of the root element. When not set, the component keeps a z-index of 1 - enough to stay above the plain flowing content it sticks over without covering popups and overlays."
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "IsStuck",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the component is currently stuck to an edge of its scrolling container. Always false unless OnStuckChanged, StuckClass or StuckStyle is used, since those are what attach the stuck detection."
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "sticky-position-enum",
            Name = "BitStickyPosition",
            Description = "The edges of the scrolling container a BitSticky pins itself to.",
            Items =
            [
                new()
                {
                    Name = "Top",
                    Value = "0",
                    Description = "Sticks to the top edge while the container scrolls vertically."
                },
                new()
                {
                    Name = "Bottom",
                    Value = "1",
                    Description = "Sticks to the bottom edge while the container scrolls vertically."
                },
                new()
                {
                    Name = "TopAndBottom",
                    Value = "2",
                    Description = "Sticks to whichever vertical edge the scroll carries it to: the top while scrolling down past it, the bottom while it is still below the fold."
                },
                new()
                {
                    Name = "Start",
                    Value = "3",
                    Description = "Sticks to the start edge while the container scrolls horizontally - the left edge in LTR, the right edge in RTL."
                },
                new()
                {
                    Name = "End",
                    Value = "4",
                    Description = "Sticks to the end edge while the container scrolls horizontally - the right edge in LTR, the left edge in RTL."
                },
                new()
                {
                    Name = "StartAndEnd",
                    Value = "5",
                    Description = "Sticks to whichever horizontal edge the scroll carries it to, following the reading direction the way Start and End do."
                }
            ]
        }
    ];



    private readonly string example1RazorCode = @"
<style>
    .vertical-container {
        height: 16rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid gray;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }
</style>


<div class=""vertical-container"">

    <BitSticky Class=""sticky"">Basic Sticky</BitSticky>

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
    </div>
</div>";

    private readonly string example2RazorCode = @"
<style>
    .vertical-container {
        height: 16rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid gray;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }
</style>


<div class=""vertical-container"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </p>

    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.Top"">Stick to Top</BitSticky>

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
    </div>
</div>


<div class=""vertical-container"">
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
    </div>

    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.Bottom"">Stick to Bottom</BitSticky>

    <div>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</div>


<div class=""vertical-container"">
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
    </div>

    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.TopAndBottom"">Stick to Top and Bottom</BitSticky>

    <div>
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
    </div>
</div>";

    private readonly string example3RazorCode = @"
<style>
    .horizontal-container {
        gap: 1rem;
        height: 5rem;
        display: flex;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        white-space: nowrap;
        border: 1px solid gray;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }
</style>


<div class=""horizontal-container"">
    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>

    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.Start"">Stick to Start</BitSticky>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>
</div>


<div class=""horizontal-container"">
    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>

    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.End"">Stick to End</BitSticky>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>
</div>


<div class=""horizontal-container"">
    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>

    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.StartAndEnd"">Stick to Start and End</BitSticky>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>
</div>";

    private readonly string example4RazorCode = @"
<style>
    .vertical-container {
        height: 16rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid gray;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }
</style>


<div class=""vertical-container"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </p>

    <BitSticky Class=""sticky"" Top=""20px"">Top customized</BitSticky>

    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        <br />
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        <br />
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </p>
</div>


<div class=""vertical-container"">
    <p>
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
    </p>

    <BitSticky Class=""sticky"" Bottom=""2rem"">Bottom customized</BitSticky>

    <p>
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
    </p>
</div>


<div class=""vertical-container"">
    <p>
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
    </p>

    <BitSticky Class=""sticky"" Top=""2rem"" Bottom=""20px"">Top and Bottom customized</BitSticky>

    <p>
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
    </p>
</div>";

    private readonly string example5RazorCode = @"
<style>
    .horizontal-container {
        gap: 1rem;
        height: 5rem;
        display: flex;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        white-space: nowrap;
        border: 1px solid gray;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }
</style>


<div class=""horizontal-container"">
    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>

    <BitSticky Class=""sticky"" Left=""20px"">Left customized</BitSticky>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>
</div>


<div class=""horizontal-container"">
    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>

    <BitSticky Class=""sticky"" Right=""2rem"">Right customized</BitSticky>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>
</div>


<div class=""horizontal-container"">
    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>

    <BitSticky Class=""sticky"" Left=""2rem"" Right=""20px"">Left and Right customized</BitSticky>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </p>
</div>";

    private readonly string example6RazorCode = @"
<style>
    .vertical-container {
        height: 16rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid gray;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }

    .stuck-shadow {
        box-shadow: 0 4px 8px rgba(0, 0, 0, 0.5);
    }
</style>


<div>Currently stuck: <b>@isStuck</b></div>

<div class=""vertical-container"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them.
    </p>

    <BitSticky Class=""sticky"" StuckClass=""stuck-shadow"" OnStuckChanged=""v => isStuck = v"">
        @(isStuck ? ""Stuck!"" : ""Not stuck yet"")
    </BitSticky>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
        for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        <br />
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
        shaped into meaning, and the emotions ready to resonate with every reader.
    </p>
</div>


<div class=""vertical-container"">
    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them.
    </p>

    <BitSticky Class=""sticky"" StuckStyle=""background-color: tomato; color: white"">
        Tinted only while stuck
    </BitSticky>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
        for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        <br />
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
        shaped into meaning, and the emotions ready to resonate with every reader.
    </p>
</div>";
    private readonly string example6CsharpCode = @"
private bool isStuck;";

    private readonly string example7RazorCode = @"
<style>
    .vertical-container {
        height: 16rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid gray;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }

    .positioned-box {
        z-index: 2;
        color: black;
        padding: 0.5rem;
        position: relative;
        background-color: cadetblue;
    }
</style>


<div class=""vertical-container"">
    <BitSticky Class=""sticky"">Default z-index</BitSticky>

    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>

    <div class=""positioned-box"">A positioned box with z-index: 2</div>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
        for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        <br />
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead.
    </p>
</div>


<div class=""vertical-container"">
    <BitSticky Class=""sticky"" ZIndex=""3"">ZIndex of 3</BitSticky>

    <p>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built.
    </p>

    <div class=""positioned-box"">A positioned box with z-index: 2</div>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
        for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        <br />
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead.
    </p>
</div>";

    private readonly string example8RazorCode = @"
<style>
    .vertical-container {
        height: 16rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid gray;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }
</style>


<BitToggle @bind-Value=""isStickyEnabled"" Text=""Sticky enabled"" />

<div class=""vertical-container"">
    <BitSticky Class=""sticky"" IsEnabled=""isStickyEnabled"">
        @(isStickyEnabled ? ""Sticking to the top"" : ""Scrolling away with the content"")
    </BitSticky>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
        for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        <br />
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them.
        <br />
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
        shaped into meaning, and the emotions ready to resonate with every reader.
    </p>
</div>";
    private readonly string example8CsharpCode = @"
private bool isStickyEnabled = true;";

    private readonly string example9RazorCode = @"
<style>
    .vertical-container {
        height: 16rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid gray;
    }

    .custom-class {
        color: white;
        padding: 0.5rem;
        border-radius: 0.5rem;
        background-color: darkslateblue;
        border: 2px dashed mediumpurple;
    }
</style>


<div class=""vertical-container"">
    <BitSticky Style=""color: darkred; padding: 0.5rem; background-color: goldenrod; border-radius: 0.5rem;"">
        Styled Sticky
    </BitSticky>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
        for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        <br />
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
        shaped into meaning, and the emotions ready to resonate with every reader.
    </p>
</div>


<div class=""vertical-container"">
    <BitSticky Class=""custom-class"">Classed Sticky</BitSticky>

    <p>
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
        for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        <br />
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
        shaped into meaning, and the emotions ready to resonate with every reader.
    </p>
</div>";

    private readonly string example10RazorCode = @"
<style>
    .horizontal-container {
        gap: 1rem;
        height: 5rem;
        display: flex;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        white-space: nowrap;
        border: 1px solid gray;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }
</style>


<div dir=""rtl"" class=""horizontal-container"">
    <p>
        روزی روزگاری، داستان‌ها میان مردم پیوند می‌ساختند؛ هم‌نوایی صداهایی که رویاهای مشترک می‌آفریدند.
    </p>

    <BitSticky Dir=""BitDir.Rtl"" Class=""sticky"" Position=""@BitStickyPosition.Start"">چسبیده به آغاز</BitSticky>

    <p>
        روزی روزگاری، داستان‌ها میان مردم پیوند می‌ساختند؛ هم‌نوایی صداهایی که رویاهای مشترک می‌آفریدند.
    </p>
</div>


<div dir=""rtl"" class=""horizontal-container"">
    <p>
        روزی روزگاری، داستان‌ها میان مردم پیوند می‌ساختند؛ هم‌نوایی صداهایی که رویاهای مشترک می‌آفریدند.
    </p>

    <BitSticky Dir=""BitDir.Rtl"" Class=""sticky"" Position=""@BitStickyPosition.End"">چسبیده به پایان</BitSticky>

    <p>
        روزی روزگاری، داستان‌ها میان مردم پیوند می‌ساختند؛ هم‌نوایی صداهایی که رویاهای مشترک می‌آفریدند.
    </p>
</div>";
}
