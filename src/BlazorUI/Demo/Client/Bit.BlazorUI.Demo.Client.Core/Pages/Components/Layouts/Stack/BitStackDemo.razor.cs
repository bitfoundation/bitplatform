namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Stack;
public partial class BitStackDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alignment",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Defines whether to render Stack children both horizontally and vertically.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "AutoHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Make the height of the stack auto."
        },
        new()
        {
            Name = "AutoSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Make the width and height of the stack auto."
        },
        new()
        {
            Name = "AutoWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Make the width of the stack auto."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the stack."
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node. The default is \"div\"."
        },
        new()
        {
            Name = "FillContent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expand the direct children to occupy all of the root element's width."
        },
        new()
        {
            Name = "FitHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the height of the stack to fit its content."
        },
        new()
        {
            Name = "FitSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the width and height of the stack to fit its content."
        },
        new()
        {
            Name = "FitWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the width of the stack to fit its content."
        },
        new()
        {
            Name = "Gap",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the spacing between Stack children. The property is specified as a value for 'row gap', followed optionally by a value for 'column gap'. If 'column gap' is omitted, it's set to the same value as 'row gap'."
        },
        new()
        {
            Name = "Grow",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines how much to grow the Stack in proportion to its siblings."
        },
        new()
        {
            Name = "Grows",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes grow the Stack in proportion to its siblings."
        },
        new()
        {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether to render Stack children horizontally."
        },
        new()
        {
            Name = "HorizontalAlign",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Defines whether to render Stack children horizontally.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether to render Stack children in the opposite direction (bottom-to-top if it's a vertical Stack and right-to-left if it's a horizontal Stack)."
        },
        new()
        {
            Name = "VerticalAlign",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Defines whether to render Stack children vertically.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "Wrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether Stack children should wrap onto multiple rows or columns when they are about to overflow the size of the Stack."
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "alignment-enum",
            Name = "BitAlignment",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Value = "0",
                },
                new()
                {
                    Name = "End",
                    Value = "1",
                },
                new()
                {
                    Name = "Center",
                    Value = "2",
                },
                new()
                {
                    Name = "SpaceBetween",
                    Value = "3",
                },
                new()
                {
                    Name = "SpaceAround",
                    Value = "4",
                },
                new()
                {
                    Name = "SpaceEvenly",
                    Value = "5",
                },
                new()
                {
                    Name = "Baseline",
                    Value = "6",
                },
                new()
                {
                    Name = "Stretch",
                    Value = "7",
                }
            ]
        }
    ];



    private double gap = 1;

    private bool isReversed;
    private bool isHorizontal;
    private BitDir direction;
    private BitAlignment verticalAlign;
    private BitAlignment horizontalAlign;
    private double stackHeight = 15;



    private readonly string example1RazorCode = @"
<BitStack Style=""background:#71afe5"">
    <div style=""color:black"">Item 1</div>
    <div style=""color:black"">Item 2</div>
    <div style=""color:black"">Item 3</div>
</BitStack>

<BitStack Horizontal Style=""background:#71afe5"">
    <div style=""color:black"">Item 1</div>
    <div style=""color:black"">Item 2</div>
    <div style=""color:black"">Item 3</div>
</BitStack>

<BitStack Reversed Style=""background:#71afe5"">
    <div style=""color:black"">Item 1</div>
    <div style=""color:black"">Item 2</div>
    <div style=""color:black"">Item 3</div>
</BitStack>

<BitStack Horizontal Reversed Style=""background:#71afe5"">
    <div style=""color:black"">Item 1</div>
    <div style=""color:black"">Item 2</div>
    <div style=""color:black"">Item 3</div>
</BitStack>";

    private readonly string example2RazorCode = @"
<style>
    .item {
        color: white;
        padding: 0.5rem;
        background-color: #0078d4;
    }
</style>

<BitSlider Label=""Gap between items"" Max=""5"" ValueFormat=""0.0 rem"" Step=""0.1"" @bind-Value=""@gap"" />

<BitStack Gap=""@($""{gap}rem"")"" Style=""background:#71afe5"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>";
    private readonly string example2CsharpCode = @"
private double gap = 1;
";

    private readonly string example3RazorCode = @"
<style>
    .item {
        color: white;
        padding: 0.5rem;
        background-color: #0078d4;
    }
</style>

<BitStack Horizontal HorizontalAlign=""BitAlignment.SpaceAround"" Style=""background:#71afe5"">
    <div class=""item"">Item 1</div>
    <BitStack>
        <div class=""item"">Item 2-1</div>
        <div class=""item"">Item 2-2</div>
        <div class=""item"">Item 2-3</div>
    </BitStack>
    <BitStack Horizontal>
        <div class=""item"">Item 3-1</div>
        <div class=""item"">Item 3-2</div>
        <div class=""item"">Item 3-3</div>
    </BitStack>
</BitStack>";

    private readonly string example4RazorCode = @"
<style>
    .item {
        color: white;
        padding: 0.5rem;
        background-color: #0078d4;
    }
</style>

<BitStack Horizontal Wrap Gap=""2rem"">
    <BitToggle @bind-Value=""isHorizontal"" Text=""Horizontal"" />
    <BitToggle @bind-Value=""isReversed"" Text=""Reversed"" />
</BitStack>

<BitChoiceGroup @bind-Value=""direction""
                Horizontal
                Label=""Direction""
                TItem=""BitChoiceGroupOption<BitDir>"" TValue=""BitDir"">
    <BitChoiceGroupOption Text=""LTR"" Value=""BitDir.Ltr"" />
    <BitChoiceGroupOption Text=""RTL"" Value=""BitDir.Rtl"" />
    <BitChoiceGroupOption Text=""Auto"" Value=""BitDir.Auto"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""horizontalAlign""
                Horizontal
                Label=""Horizontal Align""
                TItem=""BitChoiceGroupOption<BitAlignment>"" TValue=""BitAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitAlignment.End"" />
    <BitChoiceGroupOption Text=""SpaceBetween"" Value=""BitAlignment.SpaceBetween"" />
    <BitChoiceGroupOption Text=""SpaceAround"" Value=""BitAlignment.SpaceAround"" />
    <BitChoiceGroupOption Text=""SpaceEvenly"" Value=""BitAlignment.SpaceEvenly"" />
    <BitChoiceGroupOption Text=""Baseline"" Value=""BitAlignment.Baseline"" />
    <BitChoiceGroupOption Text=""Stretch"" Value=""BitAlignment.Stretch"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""verticalAlign""
                Horizontal
                Label=""Vertical Align""
                TItem=""BitChoiceGroupOption<BitAlignment>"" TValue=""BitAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitAlignment.End"" />
    <BitChoiceGroupOption Text=""SpaceBetween"" Value=""BitAlignment.SpaceBetween"" />
    <BitChoiceGroupOption Text=""SpaceAround"" Value=""BitAlignment.SpaceAround"" />
    <BitChoiceGroupOption Text=""SpaceEvenly"" Value=""BitAlignment.SpaceEvenly"" />
    <BitChoiceGroupOption Text=""Baseline"" Value=""BitAlignment.Baseline"" />
    <BitChoiceGroupOption Text=""Stretch"" Value=""BitAlignment.Stretch"" />
</BitChoiceGroup>

<BitStack Dir=""direction""
            Reversed=""isReversed""
            Horizontal=""isHorizontal""
            VerticalAlign=""verticalAlign""
            HorizontalAlign=""horizontalAlign""
            Style=""background:#71afe5;height:15rem"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>";
    private readonly string example4CsharpCode = @"
private bool isReversed;
private bool isHorizontal;
private BitDir direction;
private BitAlignment verticalAlign;
private BitAlignment horizontalAlign;
";

    private readonly string example5RazorCode = @"
<style>
    .item {
        display: flex;
        width: 3.5rem;
        height: 3.5rem;
        align-items: center;
        justify-content: center;
        background-color: dodgerblue;
    }
</style>

<BitSlider Label=""Stack height"" Min=""10"" Max=""20"" Step=""0.1"" ValueFormat=""0.0 rem"" @bind-Value=""@stackHeight"" />

<BitStack Wrap Style=""@($""height:{stackHeight}rem;background:#71afe5"")"">
    @for (int i = 0; i < 20; i++)
    {
        var index = i;
        <div class=""item"">Item @index</div>
    }
</BitStack>";
    private readonly string example5CsharpCode = @"
private double stackHeight = 15;
";

    private readonly string example6RazorCode = @"
<BitStack Gap=""0.5rem"" Style=""background:#71afe5;height:15rem"">
    <BitStack Grow=""3""
              Class=""item"" AutoHeight
              VerticalAlign=""BitAlignment.Center""
              HorizontalAlign=""BitAlignment.Center"">
        Grow is 3
    </BitStack>
    <BitStack Grow=""2""
              Class=""item"" AutoHeight
              VerticalAlign=""BitAlignment.Center""
              HorizontalAlign=""BitAlignment.Center"">
        Grow is 2
    </BitStack>
    <BitStack Grow=""1""
              Class=""item"" AutoHeight
              VerticalAlign=""BitAlignment.Center""
              HorizontalAlign=""BitAlignment.Center"">
        Grow is 1
    </BitStack>
</BitStack>


<BitStack Gap=""0.5rem"" Style=""background:#71afe5;height:15rem"">
    <BitStack Class=""item"" AutoHeight
              VerticalAlign=""BitAlignment.Center""
              HorizontalAlign=""BitAlignment.Center"">
        Normal
    </BitStack>
    <BitStack Grows
              Class=""item"" AutoHeight
              VerticalAlign=""BitAlignment.Center""
              HorizontalAlign=""BitAlignment.Center"">
        Grows
    </BitStack>
    <BitStack Class=""item"" AutoHeight
              VerticalAlign=""BitAlignment.Center""
              HorizontalAlign=""BitAlignment.Center"">
        Normal
    </BitStack>
</BitStack>


<BitStack Horizontal
          Gap=""0.5rem""
          Style=""background:#71afe5;height:15rem"">
    <BitStack Class=""item"" AutoWidth
              VerticalAlign=""BitAlignment.Center""
              HorizontalAlign=""BitAlignment.Center"">
        Normal
    </BitStack>
    <BitStack Grows
              Class=""item"" AutoWidth
              VerticalAlign=""BitAlignment.Center""
              HorizontalAlign=""BitAlignment.Center"">
        Grows
    </BitStack>
    <BitStack Class=""item"" AutoWidth
              VerticalAlign=""BitAlignment.Center""
              HorizontalAlign=""BitAlignment.Center"">
        Normal
    </BitStack>
</BitStack>";
}
