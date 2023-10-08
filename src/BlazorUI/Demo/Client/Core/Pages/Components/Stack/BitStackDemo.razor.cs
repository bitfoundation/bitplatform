namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Stack;
public partial class BitStackDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Typography."
        },
        new()
        {
            Name = "As",
            Type = "string",
            DefaultValue = "div",
            Description = "Defines how to render the Stack."
        },
        new()
        {
            Name = "DisableShrink",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether Stack children should not shrink to fit the available space."
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
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
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
    };



    private bool isReversed;
    private bool isHorizontal;
    private BitStackAlignment verticalAlign;
    private BitStackAlignment horizontalAlign;

    private double? gap = 1;

    private double? stackHeight = 18;

    private double? shrinkStackHeight = 12;



    private readonly string example1RazorCode = @"
<BitStack Style=""background-color: dodgerblue;"">
    <div>Item 1</div>
    <div>Item 2</div>
    <div>Item 3</div>
</BitStack>

<BitStack Horizontal Style=""background-color: dodgerblue;"">
    <div>Item 1</div>
    <div>Item 2</div>
    <div>Item 3</div>
</BitStack>

<BitStack Reversed Style=""background-color: dodgerblue;"">
    <div>Item 1</div>
    <div>Item 2</div>
    <div>Item 3</div>
</BitStack>

<BitStack Horizontal Reversed Style=""background-color: dodgerblue;"">
    <div>Item 1</div>
    <div>Item 2</div>
    <div>Item 3</div>
</BitStack>";

    private readonly string example2RazorCode = @"

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

<BitSlider Max=""10""
           @bind-Value=""@gap""
           Label=""Change the gap between child items:"" />

<BitStack Gap=""@($""{gap}rem"")"" Style=""background-color: lightskyblue; min-height: 18rem;"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>";
    private readonly string example2CsharpCode = @"
private double? gap = 1;
";

    private readonly string example3RazorCode = @"
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

<BitStack Horizontal Gap=""1rem"" Style=""background-color: lightskyblue; height: 10.5rem"">
    <BitStack Horizontal Style=""background-color: #a19f9d; height: 100%;"">
        <div class=""item"">Item 1</div>
        <div class=""item"">Item 2</div>
        <div class=""item"">Item 3</div>
    </BitStack>
    <BitStack Horizontal Reversed Style=""background-color: #605e5c; height: 100%;"">
        <div class=""item"">Item 1</div>
        <div class=""item"">Item 2</div>
        <div class=""item"">Item 3</div>
    </BitStack>
    <BitStack Reversed Style=""background-color: #323130; min-width: 10.5rem;"">
        <div class=""item"">Item 1</div>
        <div class=""item"">Item 2</div>
        <div class=""item"">Item 3</div>
    </BitStack>
</BitStack>";

    private readonly string example4RazorCode = @"
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

<BitStack Style=""background-color: lightskyblue; height: 18rem;""
          Reversed=""isReversed""
          Horizontal=""isHorizontal""
          VerticalAlign=""verticalAlign""
          HorizontalAlign=""horizontalAlign"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>

<BitStack Horizontal Wrap Gap=""0.5rem"">
    <BitToggle @bind-Value=""isHorizontal"" DefaultText=""Is Horizontal"" />
    <BitToggle @bind-Value=""isReversed"" DefaultText=""Is Reversed"" />
</BitStack>

<BitChoiceGroup Label=""Horizontal Align""
                @bind-Value=""horizontalAlign""
                LayoutFlow=""@BitLayoutFlow.Horizontal""
                TItem=""BitChoiceGroupOption<BitStackAlignment>""
                TValue=""BitStackAlignment"">
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
                TItem=""BitChoiceGroupOption<BitStackAlignment>""
                TValue=""BitStackAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitStackAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitStackAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitStackAlignment.End"" />
    <BitChoiceGroupOption Text=""SpaceBetween"" Value=""BitStackAlignment.SpaceBetween"" />
    <BitChoiceGroupOption Text=""SpaceAround"" Value=""BitStackAlignment.SpaceAround"" />
    <BitChoiceGroupOption Text=""SpaceEvenly"" Value=""BitStackAlignment.SpaceEvenly"" />
    <BitChoiceGroupOption Text=""Baseline"" Value=""BitStackAlignment.Baseline"" />
    <BitChoiceGroupOption Text=""Stretch"" Value=""BitStackAlignment.Stretch"" />
</BitChoiceGroup>";
    private readonly string example4CsharpCode = @"
private bool isReversed;
private bool isHorizontal;
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

<BitSlider Max=""18""
           @bind-Value=""@stackHeight""
           Label=""Change the stack height to see how child items wrap onto multiple rows:"" />

<BitStack Wrap Style=""@($""height: {stackHeight}rem; background-color: lightskyblue; min-width: max-content;"")"">
    @for (int i = 0; i < 20; i++)
    {
        var index = i;
        <div class=""item"">Item @index</div>
    }
</BitStack>";
    private readonly string example5CsharpCode = @"
private double? stackHeight = 18;
";

    private readonly string example6RazorCode = @"
<style>
    .stack-item {
        height: 3.5rem;
        padding: 0 1rem;
        min-width: 3.5rem;
        white-space: nowrap;
        background-color: dodgerblue;
    }
</style>

<BitSlider Max=""12""
           @bind-Value=""@shrinkStackHeight""
           Label=""Change the stack height to see how child items shrink:"" />

<BitStack Gap=""0.5rem"" Style=""@($""height: {shrinkStackHeight}rem; background-color: lightskyblue; overflow:hidden;"")"">
    <BitStack Class=""stack-item""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        I will shrink
    </BitStack>
    <BitStack DisableShrink
              Class=""stack-item""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        I will not shrink
    </BitStack>
    <BitStack Class=""stack-item""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        I will shrink
    </BitStack>
</BitStack>";
    private readonly string example6CsharpCode = @"
private double? shrinkStackHeight = 12;
";

    private readonly string example7RazorCode = @"
<BitStack Gap=""0.5rem"" Style=""background-color: lightskyblue; padding: 0.5rem; height: 18rem;"">
    <BitStack Grow=""3""
              Style=""background-color: dodgerblue; width: 100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Grow is 3
    </BitStack>
    <BitStack Grow=""2""
              Style=""background-color: dodgerblue; width: 100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Grow is 2
    </BitStack>
    <BitStack Grow=""1""
              Style=""background-color: dodgerblue; width: 100%""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Grow is 1
    </BitStack>
</BitStack>

<BitStack Horizontal
          Gap=""0.5rem""
          Style=""background-color: lightskyblue; padding: 0.5rem; height: 18rem;"">
    <BitStack Style=""background-color: dodgerblue; height: 100%;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Item 3
    </BitStack>
    <BitStack Grows
              Style=""background-color: dodgerblue; height: 100%;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Item 2 (Grows)
    </BitStack>
    <BitStack Style=""background-color: dodgerblue; height: 100%;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Item 1
    </BitStack>
</BitStack>";
}
