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
            Name = "Elevation",
            Type = "int?",
            DefaultValue = "null",
            Description = "Sets the shadow elevation level of the card (1-24). Maps to theme shadow variables (--bit-shd-1 to --bit-shd-24).",
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the card height 100% of its parent container.",
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the card width and height 100% of its parent container.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the card width 100% of its parent container.",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the height of the card explicitly.",
        },
        new()
        {
            Name = "NoPadding",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default padding of the card.",
        },
        new()
        {
            Name = "NoShadow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default shadow around the card.",
        },
        new()
        {
            Name = "Outlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the card with no shadow and a primary border.",
        },
        new()
        {
            Name = "Square",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the border-radius from the card, rendering it with sharp corners.",
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the width of the card explicitly.",
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



    private double elevation = 4;
    private double cardWidth = 300;
    private double cardHeight = 200;
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
<BitSlider @bind-Value=""elevation"" Min=""1"" Max=""24"" Step=""1"" Label=""Elevation"" />

<BitCard Elevation=""(int)elevation"">
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";
    private readonly string example2CsharpCode = @"
private double elevation = 4;";

    private readonly string example3RazorCode = @"
<BitCard NoShadow>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";

    private readonly string example4RazorCode = @"
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
    private readonly string example4CsharpCode = @"
private BitColorKind backgroundColorKind = BitColorKind.Primary;";

    private readonly string example5RazorCode = @"
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
    private readonly string example5CsharpCode = @"
private BitColorKind borderColorKind = BitColorKind.Primary;";

    private readonly string example6RazorCode = @"
<BitCard Outlined>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";

    private readonly string example7RazorCode = @"
<BitCard Square Outlined>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";

    private readonly string example8RazorCode = @"
<BitCard NoPadding Outlined>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";

    private readonly string example9RazorCode = @"
<BitSlider @bind-Value=""cardWidth"" Min=""100"" Max=""600"" Step=""10"" Label=""Width (px)"" />
<BitSlider @bind-Value=""cardHeight"" Min=""100"" Max=""400"" Step=""10"" Label=""Height (px)"" />

<BitCard Width=""@($""{(int)cardWidth}px"")"" Height=""@($""{(int)cardHeight}px"")"" Outlined>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";
    private readonly string example9CsharpCode = @"
private double cardWidth = 300;
private double cardHeight = 200;";

    private readonly string example10RazorCode = @"
<BitChoiceGroup @bind-Value=""size"" Horizontal
                TItem=""BitChoiceGroupOption<int>"" TValue=""int"">
    <BitChoiceGroupOption Text=""FullSize"" Value=""0"" />
    <BitChoiceGroupOption Text=""FullWidth"" Value=""1"" />
    <BitChoiceGroupOption Text=""FullHeight"" Value=""2"" />
</BitChoiceGroup>

<div style=""padding:2rem;background:gray;height:500px"">
    <BitCard FullSize=""size == 0"" FullWidth=""size == 1"" FullHeight=""size == 2"">
        <BitStack HorizontalAlign=""BitAlignment.Start"">
            <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
            <BitText Typography=""BitTypography.Body1"">
                bit BlazorUI components are native, easy-to-customize, and ...
            </BitText>
            <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
        </BitStack>
    </BitCard>
</div>";
    private readonly string example10CsharpCode = @"
private int size = 0;";
}
