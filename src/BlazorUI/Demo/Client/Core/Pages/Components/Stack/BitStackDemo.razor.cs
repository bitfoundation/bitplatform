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
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the height of the Stack."
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
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the maximum height that the Stack can take."
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the maximum width that the Stack can take."
        },
        new()
        {
            Name = "MinHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the minimum height that the Stack can take."
        },
        new()
        {
            Name = "MinWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the minimum width that the Stack can take."
        },
        new()
        {
            Name = "Padding",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the inner padding of the Stack."
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
            Name = "VerticalFill",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether the Stack should take up 100% of the height of its parent. This property is required to be set to true when using the grow flag on children in vertical oriented Stacks. Stacks are rendered as block elements and grow horizontally to the container already."
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the width of the Stack."
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
    .stack-item {
        width: 3.5rem;
        height: 3.5rem;
        background-color: dodgerblue;
    }
</style>

<BitStack Style=""background-color: lightskyblue;"">
    <BitStack Class=""stack-item"">Item 1</BitStack>
    <BitStack Class=""stack-item"">Item 2</BitStack>
    <BitStack Class=""stack-item"">Item 3</BitStack>
</BitStack>";

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

<BitStack Gap=""1rem"" Style=""background-color: lightskyblue; height: 18rem;"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>";

    private readonly string example4RazorCode = @"
<BitStack Height=""18rem"" Gap=""0.5rem"" Style=""background-color: lightskyblue;"">
    <BitStack Width=""5rem"" Style=""background-color: dodgerblue;"">Item 1</BitStack>
    <BitStack MinWidth=""50%"" Style=""background-color: dodgerblue;"">Item 2</BitStack>
    <BitStack MinHeight=""48px"" Style=""background-color: dodgerblue;"">Item 3</BitStack>
    <BitStack Width=""100%"" MaxWidth=""8rem"" Style=""background-color: dodgerblue;"">Item 5</BitStack>
    <BitStack Height=""100%"" MaxHeight=""80px"" Style=""background-color: dodgerblue;"">Item 4</BitStack>
</BitStack>";

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

<BitStack Style=""background-color: lightskyblue;""
          Height=""18rem""
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
    private readonly string example5CsharpCode = @"
private bool isReversed;
private bool isHorizontal;
private BitStackAlignment verticalAlign;
private BitStackAlignment horizontalAlign;
";

    private readonly string example6RazorCode = @"
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

<BitStack Padding=""1rem"" Height=""18rem"" Style=""background-color: lightskyblue;"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>";

    private readonly string example7RazorCode = @"
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

<BitStack Wrap
          Padding=""1rem""
          MinWidth=""max-content""
          Height=""@($""{stackHeight}rem"")""
          Style=""background-color: lightskyblue;"">
    @for (int i = 0; i < 20; i++)
    {
        var index = i;
        <div class=""item"">Item @index</div>
    }
</BitStack>";
    private readonly string example7CsharpCode = @"
private double? stackHeight = 18;
";

    private readonly string example8RazorCode = @"
<BitStack Gap=""1rem"" Height=""18rem"" Style=""background-color: lightskyblue;"">
    <BitStack HorizontalFill
              Padding=""0.5rem""
              Style=""background-color: dodgerblue;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Horizontal fill
    </BitStack>
    <BitStack Padding=""0.5rem""
              Style=""background-color: dodgerblue;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Normal
    </BitStack>
    <BitStack VerticalFill
              Padding=""0.5rem""
              Style=""background-color: dodgerblue;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Vertical fill
    </BitStack>
</BitStack>";

    private readonly string example9RazorCode = @"
<style>
    .stack-item {
        width: 3.5rem;
        height: 3.5rem;
        background-color: dodgerblue;
    }
</style>

<BitSlider Max=""12""
           @bind-Value=""@shrinkStackHeight""
           Label=""Change the stack height to see how child items shrink:"" />
<br /><br />
<BitStack Gap=""0.5rem""
          Height=""@($""{shrinkStackHeight}rem"")""
          Style=""background-color: lightskyblue; overflow:hidden;"">
    <BitStack HorizontalFill
              Class=""stack-item""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        I will shrink
    </BitStack>
    <BitStack HorizontalFill DisableShrink
              Class=""stack-item""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        I will not shrink
    </BitStack>
    <BitStack HorizontalFill
              Class=""stack-item""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        I will shrink
    </BitStack>
</BitStack>";
    private readonly string example9CsharpCode = @"
private double? shrinkStackHeight = 12;
";

    private readonly string example10RazorCode = @"
<BitStack Gap=""0.5rem""
          Height=""18rem""
          Padding=""0.5rem""
          Style=""background-color: lightskyblue;"">
    <BitStack HorizontalFill
              Grow=""3""
              Style=""background-color: dodgerblue;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Grow is 3
    </BitStack>
    <BitStack HorizontalFill
              Grow=""2""
              Style=""background-color: dodgerblue;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Grow is 2
    </BitStack>
    <BitStack HorizontalFill
              Grow=""1""
              Style=""background-color: dodgerblue;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Grow is 1
    </BitStack>
</BitStack>

<BitStack Horizontal
          Gap=""0.5rem""
          Height=""18rem""
          Padding=""0.5rem""
          Style=""background-color: lightskyblue;"">
    <BitStack VerticalFill
              Style=""background-color: dodgerblue;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Item 3
    </BitStack>
    <BitStack Grows VerticalFill
              Style=""background-color: dodgerblue;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Item 2 (Grows)
    </BitStack>
    <BitStack VerticalFill
              Style=""background-color: dodgerblue;""
              VerticalAlign=""BitStackAlignment.Center""
              HorizontalAlign=""BitStackAlignment.Center"">
        Item 1
    </BitStack>
</BitStack>";
}
