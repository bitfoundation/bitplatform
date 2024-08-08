namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Stack;
public partial class BitStackDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Typography."
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
            Type = "BitStackAlignment",
            DefaultValue = "BitStackAlignment.Start",
            Description = "Defines whether to render Stack children horizontally.",
            Href = "#bitStackAlignment-enum",
            LinkType = LinkType.Link
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
            Type = "BitStackAlignment",
            DefaultValue = "BitStackAlignment.Start",
            Description = "Defines whether to render Stack children vertically.",
            Href = "#bitStackAlignment-enum",
            LinkType = LinkType.Link
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
            Id = "bitStackAlignment-enum",
            Name = "BitStackAlignment",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
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
            }
        }
    ];



    private double gap = 1;

    private bool isReversed;
    private bool isHorizontal;
    private BitDir direction;
    private BitStackAlignment verticalAlign;
    private BitStackAlignment horizontalAlign;
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

<BitStack Horizontal HorizontalAlign=""BitStackAlignment.SpaceAround"" Style=""background:#71afe5"">
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
    <BitToggle @bind-Value=""isHorizontal"" DefaultText=""Horizontal"" />
    <BitToggle @bind-Value=""isReversed"" DefaultText=""Reversed"" />
</BitStack>

<BitChoiceGroup Label=""Direction""
                @bind-Value=""direction""
                LayoutFlow=""@BitLayoutFlow.Horizontal""
                TItem=""BitChoiceGroupOption<BitDir>"" TValue=""BitDir"">
    <BitChoiceGroupOption Text=""LTR"" Value=""BitDir.Ltr"" />
    <BitChoiceGroupOption Text=""RTL"" Value=""BitDir.Rtl"" />
    <BitChoiceGroupOption Text=""Auto"" Value=""BitDir.Auto"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Horizontal Align""
                @bind-Value=""horizontalAlign""
                LayoutFlow=""@BitLayoutFlow.Horizontal""
                TItem=""BitChoiceGroupOption<BitStackAlignment>"" TValue=""BitStackAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitStackAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitStackAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitStackAlignment.End"" />
    <BitChoiceGroupOption Text=""SpaceBetween"" Value=""BitStackAlignment.SpaceBetween"" />
    <BitChoiceGroupOption Text=""SpaceAround"" Value=""BitStackAlignment.SpaceAround"" />
    <BitChoiceGroupOption Text=""SpaceEvenly"" Value=""BitStackAlignment.SpaceEvenly"" />
    <BitChoiceGroupOption Text=""Baseline"" Value=""BitStackAlignment.Baseline"" />
    <BitChoiceGroupOption Text=""Stretch"" Value=""BitStackAlignment.Stretch"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Vertical Align""
                @bind-Value=""verticalAlign""
                LayoutFlow=""@BitLayoutFlow.Horizontal""
                TItem=""BitChoiceGroupOption<BitStackAlignment>"" TValue=""BitStackAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitStackAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitStackAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitStackAlignment.End"" />
    <BitChoiceGroupOption Text=""SpaceBetween"" Value=""BitStackAlignment.SpaceBetween"" />
    <BitChoiceGroupOption Text=""SpaceAround"" Value=""BitStackAlignment.SpaceAround"" />
    <BitChoiceGroupOption Text=""SpaceEvenly"" Value=""BitStackAlignment.SpaceEvenly"" />
    <BitChoiceGroupOption Text=""Baseline"" Value=""BitStackAlignment.Baseline"" />
    <BitChoiceGroupOption Text=""Stretch"" Value=""BitStackAlignment.Stretch"" />
</BitChoiceGroup>

<BitStack Style=""background:#71afe5;height:15rem""
          Dir=""direction""
          Reversed=""isReversed""
          Horizontal=""isHorizontal""
          VerticalAlign=""verticalAlign""
          HorizontalAlign=""horizontalAlign"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>";
    private readonly string example4CsharpCode = @"
private bool isReversed;
private bool isHorizontal;
private BitDir direction;
private BitStackAlignment verticalAlign;
private BitStackAlignment horizontalAlign;
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
              Class=""item"" Style=""width:100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Grow is 3
    </BitStack>
    <BitStack Grow=""2""
              Class=""item"" Style=""width:100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Grow is 2
    </BitStack>
    <BitStack Grow=""1""
              Class=""item"" Style=""width:100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Grow is 1
    </BitStack>
</BitStack>

<br /><br />

<BitStack Gap=""0.5rem"" Style=""background:#71afe5;height:15rem"">
    <BitStack Class=""item"" Style=""width:100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Normal
    </BitStack>
    <BitStack Grows
              Class=""item"" Style=""width:100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Grows
    </BitStack>
    <BitStack Class=""item"" Style=""width:100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Normal
    </BitStack>
</BitStack>

<br /><br />

<BitStack Horizontal
          Gap=""0.5rem""
          Style=""background:#71afe5;height:15rem"">
    <BitStack Class=""item"" Style=""height:100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Normal
    </BitStack>
    <BitStack Grows
              Class=""item"" Style=""height:100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Grows
    </BitStack>
    <BitStack Class=""item"" Style=""height:100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Normal
    </BitStack>
</BitStack>";
}
