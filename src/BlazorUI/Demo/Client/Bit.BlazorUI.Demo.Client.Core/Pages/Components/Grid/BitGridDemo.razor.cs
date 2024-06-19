namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Grid;

public partial class BitGridDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
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
            Name = "HorizontalAlign",
            Type = "BitGridAlignment",
            DefaultValue = "BitGridAlignment.Start",
            Description = "Defines whether to render Grid children horizontally.",
            Href = "#bitGridAlignment-enum",
            LinkType = LinkType.Link
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
            Name = "VerticalAlign",
            Type = "BitGridAlignment",
            DefaultValue = "BitGridAlignment.Start",
            Description = "Defines whether to render Grid children vertically.",
            Href = "#bitGridAlignment-enum",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "VerticalSpacing",
            Type = "string?",
            DefaultValue = "null",
            Description = "Defines the vertical spacing between Grid children.",
        },
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "bit-grid-item",
            Title = "BitGridItem",
            Parameters = new()
            {
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
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "bitGridAlignment-enum",
            Name = "BitGridAlignment",
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



    private string example1RazorCode = @"
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


<BitGrid>
    @for (int i = 0; i < 7; i++)
    {
        <BitGridItem Class=""grid-item"">
            Grid Item
        </BitGridItem>
    }
</BitGrid>";

    private string example2RazorCode = @"
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


<BitGrid Style=""height: 136px;"" HorizontalAlign=""BitGridAlignment.Center"" VerticalAlign=""BitGridAlignment.Center"">
    @for (int i = 0; i < 7; i++)
    {
        <BitGridItem Class=""grid-item"">
            Grid Item
        </BitGridItem>
    }
</BitGrid>";

    private string example3RazorCode = @"
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


<BitGrid VerticalSpacing=""18px"" HorizontalSpacing=""10px"">
    @for (int i = 0; i < 16; i++)
    {
        <BitGridItem Class=""grid-item"">
            Grid Item
        </BitGridItem>
    }
</BitGrid>";

    private string example4RazorCode = @"
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
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""2"">
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""2"">
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"">
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"">
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"">
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"">
        Grid Item
    </BitGridItem>
</BitGrid>";

    private string example5RazorCode = @"
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
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"" Xs=""3"" Md=""2"">
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"" Lg=""2"">
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""2"" Lg=""1"" Xs=""1"">
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""2"" Xs=""3"">
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"" Xs=""2"" Md=""3"">
        Grid Item
    </BitGridItem>
    <BitGridItem Class=""grid-item"" Xs=""2"">
        Grid Item
    </BitGridItem>
</BitGrid>";
}
