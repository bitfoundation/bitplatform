namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Splitter;

public partial class BitSplitterDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "GutterSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The size of BitSplitter gutter in pixels.",
        },
        new()
        {
            Name = "GutterIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon for the BitSplitter gutter using BitIconInfo for external icon library support. Takes precedence over GutterIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "GutterIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in Fluent UI icon to render in the BitSplitter gutter. Ignored when GutterIcon is also set.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "FirstPanel",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content for the first panel.",
        },
        new()
        {
            Name = "FirstPanelSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The size of first panel.",
        },
        new()
        {
            Name = "FirstPanelMaxSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The max size of first panel.",
        },
        new()
        {
            Name = "FirstPanelMinSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The min size of first panel.",
        },
        new()
        {
            Name = "SecondPanel",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content for the second panel.",
        },
        new()
        {
            Name = "SecondPanelSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The size of second panel.",
        },
        new()
        {
            Name = "SecondPanelMaxSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The max size of second panel.",
        },
        new()
        {
            Name = "SecondPanelMinSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The min size of second panel.",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the orientation of BitSplitter to vertical.",
        },
    ];



    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        },
    ];



    private double gutterSize = 10;



    private readonly string example1RazorCode = @"
<BitSplitter>
    <FirstPanel>
        <div style=""padding: 4px;"">
            First Panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding: 4px;"">
            Second Panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example2RazorCode = @"
<BitSplitter Vertical>
    <FirstPanel>
        <div style=""padding: 4px;"">
            First Panel
            <div />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding: 4px;"">
            Second Panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example3RazorCode = @"
<BitSplitter FirstPanelMinSize=""128"" FirstPanelSize=""128"" SecondPanelMinSize=""64"">
    <FirstPanel>
        <div style=""padding: 4px;"">
            First Panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding: 4px;"">
            Second Panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example4RazorCode = @"
<BitSplitter>
    <FirstPanel>
        <div style=""padding: 4px;"">
            Root's first panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </FirstPanel>
    <SecondPanel>
        <BitSplitter Vertical>
            <FirstPanel>
                <div style=""padding: 4px;"">
                    Nested's first panel
                    <br />
                    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
                    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
                    when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
                    for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
                </div>
            </FirstPanel>
            <SecondPanel>
                <div style=""padding: 4px;"">
                    Nested's second panel
                    <br />
                    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
                    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
                    when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
                    for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
                </div>
            </SecondPanel>
        </BitSplitter>
    </SecondPanel>
</BitSplitter>";

    private readonly string example5RazorCode = @"
<BitSlider @bind-Value=""gutterSize"" Max=""50"" />

<BitSplitter GutterSize=""@((int)gutterSize)"">
    <FirstPanel>
        <div style=""padding: 4px;"">
            First Panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding: 4px;"">
            Second Panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </SecondPanel>
</BitSplitter>";
    private readonly string example5CsharpCode = @"
private double gutterSize = 10;
";

    private readonly string example6RazorCode = @"
<BitSplitter GutterIconName=""@BitIconName.GripperDotsVertical"">
    <FirstPanel>
        <div style=""padding: 4px;"">
            First Panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding: 4px;"">
            Second Panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        </div>
    </SecondPanel>
</BitSplitter>";

    private readonly string example7RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitSplitter GutterIcon=""@(""fa-solid fa-arrows-left-right"")"">
    <FirstPanel>
        <div style=""padding: 4px;"">
            First Panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding: 4px;"">
            Second Panel
            <br />
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            <br/>
            ""fa-solid fa-arrows-left-right""
        </div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterIcon=""@BitIconInfo.Css(""fa-solid fa-grip-vertical"")"">
    <FirstPanel>
        <div style=""padding: 4px;"">
            First Panel
            <br />
            BitIconInfo.Css(""fa-solid fa-grip-vertical"")
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding: 4px;"">
            Second Panel
            <br />
            BitIconInfo.Css(""fa-solid fa-grip-vertical"")
        </div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterIcon=""@BitIconInfo.Fa(""solid grip-lines-vertical"")"">
    <FirstPanel>
        <div style=""padding: 4px;"">
            First Panel
            <br />
            BitIconInfo.Fa(""solid grip-lines-vertical"")
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding: 4px;"">
            Second Panel
            <br />
            BitIconInfo.Fa(""solid grip-lines-vertical"")
        </div>
    </SecondPanel>
</BitSplitter>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitSplitter GutterIcon=""@(""bi bi-grip-vertical"")"">
    <FirstPanel>
        <div style=""padding: 4px;"">
            First Panel
            <br />
            Icon=@@(""bi bi-grip-vertical"")
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding: 4px;"">
            Second Panel
            <br />
            Icon=@@(""bi bi-grip-vertical"")
        </div>
    </SecondPanel>
</BitSplitter>

<BitSplitter GutterIcon=""@BitIconInfo.Bi(""arrow-left-right"")"">
    <FirstPanel>
        <div style=""padding: 4px;"">
            First Panel
            <br />
            BitIconInfo.Bi(""arrow-left-right"")
        </div>
    </FirstPanel>
    <SecondPanel>
        <div style=""padding: 4px;"">
            Second Panel
            <br />
            BitIconInfo.Bi(""arrow-left-right"")
        </div>
    </SecondPanel>
</BitSplitter>";

}
