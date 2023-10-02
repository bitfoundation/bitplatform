namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Separator;

public partial class BitSeparatorDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AlignContent",
            Type = "BitSeparatorAlignContent",
            DefaultValue = "BitSeparatorAlignContent.Center",
            Description = "Where the content should be aligned in the separator.",
            Href = "#separator-align-enum",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Separator, it can be any custom tag or text."
        },
        new()
        {
            Name = "IsVertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the element is a vertical separator."
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "separator-align-enum",
            Name = "BitSeparatorAlignContent",
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
                    Name = "Center",
                    Value = "1",
                },
                new()
                {
                    Name = "End",
                    Value = "2",
                },
            }
        }
    };



    private readonly string example1RazorCode = @"
<BitSeparator />
<BitSeparator>Text</BitSeparator>
<BitSeparator><BitIcon IconName=""Clock"" /></BitSeparator>";

    private readonly string example2RazorCode = @"
<style>
    .custom-horizontal-layout {
        gap: 1rem;
        height: 3rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }
</style>


<div class=""custom-horizontal-layout"">
    <span>Item 1</span>
    <BitSeparator IsVertical=""true"" />
    <span>Item 2</span>
    <BitSeparator IsVertical=""true"" />
    <span>Item 3</span>
    <BitSeparator IsVertical=""true"" />
    <span>Item 4</span>
    <BitSeparator IsVertical=""true"" />
    <span>Item 5</span>
</div>
";

    private readonly string example3RazorCode = @"
<BitSeparator AlignContent=""@BitSeparatorAlignContent.Center"">Center</BitSeparator>
<BitSeparator AlignContent=""@BitSeparatorAlignContent.Start"">Start</BitSeparator>
<BitSeparator AlignContent=""@BitSeparatorAlignContent.End"">End</BitSeparator>

<BitSeparator IsVertical=""true"" AlignContent=""@BitSeparatorAlignContent.Center"">Center</BitSeparator>
<BitSeparator IsVertical=""true"" AlignContent=""@BitSeparatorAlignContent.Start"">Start</BitSeparator>
<BitSeparator IsVertical=""true"" AlignContent=""@BitSeparatorAlignContent.End"">End</BitSeparator>";
}


