namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Separator;

public partial class BitSeparatorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AlignContent",
            Type = "BitSeparatorAlignContent",
            DefaultValue = "BitSeparatorAlignContent.Center",
            Description = "Where the content should be aligned in the separator.",
            LinkType = LinkType.Link,
            Href = "#separator-align-enum",
        },
        new()
        {
            Name = "AutoSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the separator with auto width or height."
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
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the element is a vertical separator."
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "separator-align-enum",
            Name = "BitSeparatorAlignContent",
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
                    Name = "Center",
                    Value = "1",
                },
                new()
                {
                    Name = "End",
                    Value = "2",
                },
            ]
        }
    ];



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
    <BitSeparator Vertical />
    <span>Item 2</span>
    <BitSeparator Vertical />
    <span>Item 3</span>
    <BitSeparator Vertical />
    <span>Item 4</span>
    <BitSeparator Vertical />
    <span>Item 5</span>
</div>
";

    private readonly string example3RazorCode = @"
<BitSeparator AlignContent=""@BitSeparatorAlignContent.Center"">Center</BitSeparator>
<BitSeparator AlignContent=""@BitSeparatorAlignContent.Start"">Start</BitSeparator>
<BitSeparator AlignContent=""@BitSeparatorAlignContent.End"">End</BitSeparator>

<div style=""height: 13rem"">
    <BitSeparator Vertical AlignContent=""@BitSeparatorAlignContent.Center"">Center</BitSeparator>
    <BitSeparator Vertical AlignContent=""@BitSeparatorAlignContent.Start"">Start</BitSeparator>
    <BitSeparator Vertical AlignContent=""@BitSeparatorAlignContent.End"">End</BitSeparator>
</div>";

    private readonly string example4RazorCode = @"
<div style=""display:flex;flex-direction:column;align-items:center"">
    <BitSeparator>Default</BitSeparator>
    <BitSeparator AutoSize>AutoSize</BitSeparator>
</div>";

    private readonly string example5RazorCode = @"
<BitSeparator Background=""BitColorKind.Primary"">Primary</BitSeparator>
<BitSeparator Background=""BitColorKind.Secondary"">Secondary</BitSeparator>
<BitSeparator Background=""BitColorKind.Tertiary"">Tertiary</BitSeparator>
<BitSeparator Background=""BitColorKind.Transparent"">Transparent</BitSeparator>

<BitSeparator Border=""BitColorKind.Primary"">Primary</BitSeparator>
<BitSeparator Border=""BitColorKind.Secondary"">Secondary</BitSeparator>
<BitSeparator Border=""BitColorKind.Tertiary"">Tertiary</BitSeparator>
<BitSeparator Border=""BitColorKind.Transparent"">Transparent</BitSeparator>";
}


