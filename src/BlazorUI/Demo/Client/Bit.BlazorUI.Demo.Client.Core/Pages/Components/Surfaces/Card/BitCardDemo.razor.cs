namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Card;

public partial class BitCardDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the background of the card.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the border of the card.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "The content of the card.",
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "null",
            Description = "Makes the card height 100% of its parent container.",
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "null",
            Description = "Makes the card width and height 100% of its parent container.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "null",
            Description = "Makes the card width 100% of its parent container.",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-kind-enum",
            Name = "BitColorKind",
            Description = "Defines the color kinds available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name = "Primary",
                    Description = "The primary color kind.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "The secondary color kind.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "The tertiary color kind.",
                    Value = "2",
                },
                new()
                {
                    Name = "Transparent",
                    Description = "The transparent color kind.",
                    Value = "3",
                },
            ]
        }
    ];



    private BitColorKind backgroundColorKind = BitColorKind.Primary;
    private BitColorKind borderColorKind = BitColorKind.Primary;
    private int size = 0;



    private readonly string example1RazorCode = @"
<BitCard>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";

    private readonly string example2RazorCode = @"
<BitChoiceGroup @bind-Value=""backgroundColorKind"" Horizontal
                TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>

<div style=""padding:2rem;background:gray"">
    <BitCard Background=""backgroundColorKind"">
        <BitStack HorizontalAlign=""BitAlignment.Start"">
            <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
            <BitText Typography=""BitTypography.Body1"">
                bit BlazorUI components are native, easy-to-customize, and ...
            </BitText>
            <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
        </BitStack>
    </BitCard>
</div>";
    private readonly string example2CSharpCode = @"
private BitColorKind backgroundColorKind = BitColorKind.Primary;";

    private readonly string example3RazorCode = @"
<BitChoiceGroup @bind-Value=""borderColorKind"" Horizontal
                TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>

<BitCard Border=""borderColorKind"">
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";
    private readonly string example3CSharpCode = @"
private BitColorKind borderColorKind = BitColorKind.Primary;";

    private readonly string example4RazorCode = @"
<BitChoiceGroup @bind-Value=""size"" Horizontal
                TItem=""BitChoiceGroupOption<int>"" TValue=""int"">
    <BitChoiceGroupOption Text=""FullSize"" Value=""0"" />
    <BitChoiceGroupOption Text=""FullWidth"" Value=""1"" />
    <BitChoiceGroupOption Text=""FullHeight"" Value=""2"" />
</BitChoiceGroup>

<div style=""padding:2rem;background:gray;height:500px"">
    <BitCard FullSize=""size==0"" FullWidth=""size==1"" FullHeight=""size==2"">
        <BitStack HorizontalAlign=""BitAlignment.Start"">
            <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
            <BitText Typography=""BitTypography.Body1"">
                bit BlazorUI components are native, easy-to-customize, and ...
            </BitText>
            <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
        </BitStack>
    </BitCard>
</div>";
    private readonly string example4CSharpCode = @"
private int size = 0;";
}
