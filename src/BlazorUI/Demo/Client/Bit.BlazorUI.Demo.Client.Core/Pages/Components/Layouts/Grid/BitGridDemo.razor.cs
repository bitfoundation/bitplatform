namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Grid;

public partial class BitGridDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Grid.",
        },
        new()
        {
            Name = "Columns",
            Type = "int",
            DefaultValue = "12",
            Description = "Defines the columns of Grid.",
        },
        new()
        {
            Name = "ColumnsXs",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the extra small breakpoint.",
        },
        new()
        {
            Name = "ColumnsSm",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the small breakpoint.",
        },
        new()
        {
            Name = "ColumnsMd",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the medium breakpoint.",
        },
        new()
        {
            Name = "ColumnsLg",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the large breakpoint.",
        },
        new()
        {
            Name = "ColumnsXl",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the extra large breakpoint.",
        },
        new()
        {
            Name = "ColumnsXxl",
            Type = "int?",
            DefaultValue = "null",
            Description = "Number of columns in the extra extra large breakpoint.",
        },
        new()
        {
            Name = "HorizontalAlign",
            Type = "BitAlignment",
            DefaultValue = "BitAlignment.Start",
            Description = "Defines whether to render Grid children horizontally.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "HorizontalSpacing",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the horizontal spacing between Grid children.",
        },
        new()
        {
            Name = "Spacing",
            Type = "string",
            DefaultValue = "4px",
            Description = "Defines the spacing between Grid children.",
        },
        new()
        {
            Name = "Span",
            Type = "int",
            DefaultValue = "1",
            Description = "Defines the span of Grid.",
        },
        new()
        {
            Name = "VerticalSpacing",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the vertical spacing between Grid children.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "bit-grid-item",
            Title = "BitGridItem",
            Parameters =
            [
               new()
               {
                   Name = "ChildContent",
                   Type = "RenderFragment?",
                   DefaultValue = "null",
                   Description = "The content of the Grid item.",
               },
               new()
               {
                   Name = "ColumnSpan",
                   Type = "int",
                   DefaultValue = "1",
                   Description = "Number of columns a grid item should fill.",
               },
               new()
               {
                   Name = "Xs",
                   Type = "int?",
                   DefaultValue = "null",
                   Description = "Number of columns in the extra small breakpoint.",
               },
               new()
               {
                   Name = "Sm",
                   Type = "int?",
                   DefaultValue = "null",
                   Description = "Number of columns in the small breakpoint.",
               },
               new()
               {
                   Name = "Md",
                   Type = "int?",
                   DefaultValue = "null",
                   Description = "Number of columns in the medium breakpoint.",
               },
               new()
               {
                   Name = "Lg",
                   Type = "int?",
                   DefaultValue = "null",
                   Description = "Number of columns in the large breakpoint.",
               },
               new()
               {
                   Name = "Xl",
                   Type = "int?",
                   DefaultValue = "null",
                   Description = "Number of columns in the extra large breakpoint.",
               },
               new()
               {
                   Name = "Xxl",
                   Type = "int?",
                   DefaultValue = "null",
                   Description = "Number of columns in the extra extra large breakpoint.",
               },
            ]
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



    private double verticalSpacing = 0.5;
    private double horizontalSpacing = 0.5;

    private BitAlignment horizontalAlign;



    private readonly string example1RazorCode = @"
<style>
    .grid-item {
        height: 75px;
        padding: 8px;
        min-width: 58px;
        text-align: center;
        border-radius: 2px;
        align-content: center;
        border: 1px solid gray;
    }
</style>


<BitGrid Columns=""4"">
    @for (int i = 0; i < 8; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">
            Grid Item@(item)
        </BitGridItem>
    }
</BitGrid>";

    private readonly string example2RazorCode = @"
<style>
    .grid-item {
        height: 75px;
        padding: 8px;
        min-width: 58px;
        text-align: center;
        border-radius: 2px;
        align-content: center;
        border: 1px solid gray;
    }
</style>


<BitGrid Columns=""4"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""4"">
        Column span 4
    </BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""2"">
        Column span 2
    </BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""2"">
        Column span 2
    </BitGridItem>
    <BitGridItem Class=""grid-item"">
        Column span 1
    </BitGridItem>
    <BitGridItem Class=""grid-item"">
        Column span 1
    </BitGridItem>
    <BitGridItem Class=""grid-item"">
        Column span 1
    </BitGridItem>
    <BitGridItem Class=""grid-item"">
        Column span 1
    </BitGridItem>
</BitGrid>";

    private readonly string example3RazorCode = @"
<style>
    .grid-item {
        height: 75px;
        padding: 8px;
        min-width: 58px;
        text-align: center;
        border-radius: 2px;
        align-content: center;
        border: 1px solid gray;
    }
</style>


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

<BitGrid Columns=""4"" HorizontalAlign=""horizontalAlign"">
    @for (int i = 0; i < 13; i++)
    {
        var item = i + 1;
        <BitGridItem Class=""grid-item"">
            Grid Item@(item)
        </BitGridItem>
    }
</BitGrid>";
    private readonly string example3CsharpCode = @"
private BitAlignment horizontalAlign;
";

    private readonly string example4RazorCode = @"
<style>
    .grid-item {
        height: 75px;
        padding: 8px;
        min-width: 58px;
        text-align: center;
        border-radius: 2px;
        align-content: center;
        border: 1px solid gray;
    }
</style>


<BitSlider Label=""Vertical spacing between items"" Max=""5"" ValueFormat=""0.0 rem"" Step=""0.1"" @bind-Value=""@verticalSpacing"" />

<BitSlider Label=""Horizontal spacing between items"" Max=""5"" ValueFormat=""0.0 rem"" Step=""0.1"" @bind-Value=""@horizontalSpacing"" />

<BitGrid Columns=""4""
         HorizontalAlign=""BitAlignment.Center""
         VerticalSpacing=""@($""{verticalSpacing}rem"")""
         HorizontalSpacing=""@($""{horizontalSpacing}rem"")"">
    @for (int i = 0; i < 16; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">
            Grid Item@(item)
        </BitGridItem>
    }
</BitGrid>";
    private readonly string example4CsharpCode = @"
private double verticalSpacing = 0.5;
private double horizontalSpacing = 0.5;
";

    private readonly string example5RazorCode = @"
<style>
    .grid-item {
        height: 75px;
        padding: 8px;
        min-width: 58px;
        text-align: center;
        border-radius: 2px;
        align-content: center;
        border: 1px solid gray;
    }
</style>


<BitGrid Columns=""4"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""4"" Md=""1"">
        Md = 1
    </BitGridItem>
    <BitGridItem Class=""grid-item"" Xs=""3"" Md=""2"">
        Xs = 3, Md = 2
    </BitGridItem>
    <BitGridItem Class=""grid-item"" Lg=""2"">
        Lg = 2
    </BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""2"" Xs=""1"" Lg=""1"">
        Xs = 1, Lg = 1
    </BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""2"" Xs=""3"">
        Xs = 3
    </BitGridItem>
    <BitGridItem Class=""grid-item"" Xs=""2"" Md=""3"">
        Xs = 2, Md = 3
    </BitGridItem>
    <BitGridItem Class=""grid-item"" Xs=""2"">
        Xs = 2
    </BitGridItem>
</BitGrid>

<BitGrid Columns=""5"" ColumnsMd=""4"" ColumnsSm=""3"" ColumnsXs=""2"">
    @for (int i = 0; i < 8; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">
            Grid Item@(item)
        </BitGridItem>
    }
</BitGrid>";
}
