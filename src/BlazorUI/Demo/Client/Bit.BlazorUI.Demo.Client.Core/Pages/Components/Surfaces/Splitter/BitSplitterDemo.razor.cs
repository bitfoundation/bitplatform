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
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon of BitSplitter gutter.",
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
<BitSplitter GutterIcon=""@BitIconName.GripperDotsVertical"">
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
}
