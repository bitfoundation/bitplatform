namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.ScrollablePane;

public partial class BitScrollablePaneDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of ScrollablePane, It can be Any custom tag or a text.",
        },
        new()
        {
            Name = "Height",
            Type = "double?",
            DefaultValue= "null",
            Description = "The height of the ScrollablePane.",
        },
        new()
        {
            Name = "OnScroll",
            Type = "EventCallback",
            Description = "Callback for when the ScrollablePane scrolled.",
        },
        new()
        {
            Name = "Overflow",
            Type = "BitOverflow?",
            DefaultValue= "null",
            Description = "Controls the visibility of scrollbars in the ScrollablePane.",
            Href = "#overflow-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "OverflowX",
            Type = "BitOverflow?",
            DefaultValue= "null",
            Description = "Controls the visibility of X-axis scrollbar in the ScrollablePane.",
            Href = "#overflow-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "OverflowY",
            Type = "BitOverflow?",
            DefaultValue= "null",
            Description = "Controls the visibility of Y-axis scrollbar in the ScrollablePane.",
            Href = "#overflow-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ScrollbarGutter",
            Type = "BitScrollbarGutter?",
            DefaultValue= "null",
            Description = "Allows to reserve space for the scrollbar, preventing unwanted layout changes as the content grows while also avoiding unnecessary visuals when scrolling isn't needed.",
            Href = "#scrollbarGutter-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Width",
            Type = "double?",
            DefaultValue= "null",
            Description = "The width of the ScrollablePane.",
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "overflow-enum",
            Name = "BitOverflow",
            Description = "",
            Items =
            [
                new() 
                {
                    Name = "Auto",
                    Value = "0",
                    Description = "Scrollbars are displayed automatically when needed based on the content size, and hidden when not needed."
                },
                new() 
                { 
                    Name = "Hidden",
                    Value = "1",
                    Description = "Scrollbars are always hidden, even if the content overflows the visible area."
                },
                new()
                {
                    Name = "Scroll",
                    Value = "2",
                    Description = "Scrollbars are always visible, allowing users to scroll through the content even if it doesn't overflow the visible area."
                },
                new()
                {
                    Name = "Visible",
                    Value = "3",
                    Description = "Overflow content is not clipped and may be visible outside the element's padding box."
                }
            ]
        },
        new()
        {
            Id = "scrollbarGutter-enum",
            Name = "BitScrollbarGutter",
            Description = "",
            Items =
            [
                new() 
                {
                    Name = "Auto",
                    Value = "0",
                    Description = "The initial value. Classic scrollbars create a gutter when overflow is scroll, or when overflow is auto and the box is overflowing. Overlay scrollbars do not consume space."
                },
                new() 
                { 
                    Name = "Stable",
                    Value = "1",
                    Description = "When using classic scrollbars, the gutter will be present if overflow is auto, scroll, or hidden even if the box is not overflowing.When using overlay scrollbars, the gutter will not be present."
                },
                new() 
                { 
                    Name = "BothEdges",
                    Value = "2",
                    Description = "If a gutter would be present on one of the inline start/end edges of the box, another will be present on the opposite edge as well."
                }
            ]
        }
    ];



    private double overflowItemsCount = 6;
    private BitOverflow overflow;

    private double gutterItemsCount = 6;
    private BitScrollbarGutter gutter;



    private readonly string example1RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid #999;
    }
</style>

<BitScrollablePane Style=""height:350px;"" Class=""pane"">
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
</BitScrollablePane>";

    private readonly string example2RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid #999;
    }
</style>

<BitScrollablePane Height=""15rem"" Class=""pane"">
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
</BitScrollablePane>

<BitScrollablePane Width=""300px"" Class=""pane"" Style=""white-space:nowrap"">
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
</BitScrollablePane>";

    private readonly string example3RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid #999;
    }

    .item {
        color: black;
        height: 2.75rem;
        margin: 0.5rem 0;
        background-color: #777;
        padding: 0.5rem 1.25rem;
    }
</style>
                    
<BitChoiceGroup @bind-Value=""overflow""
                Horizontal
                Label=""Overflow""
                TItem=""BitChoiceGroupOption<BitOverflow>"" TValue=""BitOverflow"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitOverflow.Auto"" />
    <BitChoiceGroupOption Text=""Hidden"" Value=""BitOverflow.Hidden"" />
    <BitChoiceGroupOption Text=""Scroll"" Value=""BitOverflow.Scroll"" />
    <BitChoiceGroupOption Text=""Visible"" Value=""BitOverflow.Visible"" />
</BitChoiceGroup>

<BitNumberField Label=""Items count"" Min=""4"" @bind-Value=""@overflowItemsCount"" />

<BitScrollablePane Overflow=""@overflow"" Height=""16rem"" Class=""pane"">
    @for (int i = 0; i < overflowItemsCount; i++)
    {
        var index = i;
        <div class=""item"">@index</div>
    }
</BitScrollablePane>";
    private readonly string example3CsharpCode = @"
private double overflowItemsCount = 6;
private BitOverflow overflow;
";

    private readonly string example4RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid #999;
    }

    .item {
        color: black;
        height: 2.75rem;
        margin: 0.5rem 0;
        background-color: #777;
        padding: 0.5rem 1.25rem;
    }
</style>
                    
<BitChoiceGroup @bind-Value=""gutter""
                Horizontal
                Label=""Scrollbar gutter""
                TItem=""BitChoiceGroupOption<BitScrollbarGutter>"" TValue=""BitScrollbarGutter"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitScrollbarGutter.Auto"" />
    <BitChoiceGroupOption Text=""Stable"" Value=""BitScrollbarGutter.Stable"" />
    <BitChoiceGroupOption Text=""BothEdges"" Value=""BitScrollbarGutter.BothEdges"" />
</BitChoiceGroup>

<BitNumberField Label=""Items count"" Min=""4"" @bind-Value=""@gutterItemsCount"" />

<BitScrollablePane Gutter=""@gutter"" Height=""16rem"" Class=""pane"">
    @for (int i = 0; i < gutterItemsCount; i++)
    {
        var index = i;
        <div class=""item"">@index</div>
    }
</BitScrollablePane>";
    private readonly string example4CsharpCode = @"
private double gutterItemsCount = 6;
private BitScrollbarGutter gutter;
";
}
