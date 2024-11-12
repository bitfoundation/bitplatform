namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Text;

public partial class BitTextDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the text.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the text.",
            LinkType = LinkType.Link,
            Href = "#color-enum"

        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node.",
        },
        new()
        {
            Name = "Foreground",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The kind of the foreground color of the text.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum"
        },
        new()
        {
            Name = "Gutter",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the text will have a bottom margin.",
        },
        new()
        {
            Name = "NoWrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the text will not wrap, but instead will truncate with a text overflow ellipsis.",
        },
        new()
        {
            Name = "Typography",
            Type = "BitTypography?",
            DefaultValue = "null",
            Description = "The typography of the text.",
            LinkType = LinkType.Link,
            Href = "#typography-enum"
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Info Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                },
                new()
                {
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
            ]
        },
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
        },
        new()
        {
            Id = "typography-enum",
            Name = "BitTypography",
            Items =
            [
                new() { Name = "H1", Value = "0" },
                new() { Name = "H2", Value = "1" },
                new() { Name = "H3", Value = "2" },
                new() { Name = "H4", Value = "3" },
                new() { Name = "H5", Value = "4" },
                new() { Name = "H6", Value = "5" },
                new() { Name = "Subtitle1", Value = "6" },
                new() { Name = "Subtitle2", Value = "7" },
                new() { Name = "Body1", Value = "8" },
                new() { Name = "Body2", Value = "9" },
                new() { Name = "Button", Value = "10" },
                new() { Name = "Caption1", Value = "11" },
                new() { Name = "Caption2", Value = "12" },
                new() { Name = "Overline", Value = "13" },
                new() { Name = "Inherit", Value = "14" },
            ]
        }
    ];



    private string example1RazorCode = @"
<BitText>This is default (Subtitle1)</BitText>

<BitText Typography=""BitTypography.H1"">H1. Heading</BitText>
<BitText Typography=""BitTypography.H2"">H2. Heading</BitText>
<BitText Typography=""BitTypography.H3"">H3. Heading</BitText>
<BitText Typography=""BitTypography.H4"">H4. Heading</BitText>
<BitText Typography=""BitTypography.H5"">H5. Heading</BitText>
<BitText Typography=""BitTypography.H6"">H6. Heading</BitText>

<BitText Typography=""BitTypography.Subtitle1"">Subtitle1. Lorem ipsum dolor sit amet</BitText>
<BitText Typography=""BitTypography.Subtitle2"">Subtitle2. Lorem ipsum dolor sit amet</BitText>

<BitText Typography=""BitTypography.Body1"">Body1. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Quos blanditiis tenetur unde suscipit, quam beatae rerum inventore consectetur, neque doloribus, cupiditate numquam dignissimos laborum fugiat deleniti? Eum quasi quidem quibusdam.</BitText>
<BitText Typography=""BitTypography.Body2"">Body2. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Quos blanditiis tenetur unde suscipit, quam beatae rerum inventore consectetur, neque doloribus, cupiditate numquam dignissimos laborum fugiat deleniti? Eum quasi quidem quibusdam.</BitText>

<BitText Typography=""BitTypography.Button"">Button. Click Me</BitText>
<BitText Typography=""BitTypography.Caption1"">Caption1. Hello World!</BitText>
<BitText Typography=""BitTypography.Caption2"">Caption2. Hello World!</BitText>
<BitText Typography=""BitTypography.Overline"">Overline. this is overline text.</BitText>";

    private string example2RazorCode = @"
<BitText Align=""BitTextAlign.Start"">Start</BitText>
<BitText Align=""BitTextAlign.Center"">Center</BitText>
<BitText Align=""BitTextAlign.End"">End</BitText>";

    private string example3RazorCode = @"
<BitText Foreground=""BitColorKind.Primary"">Primary foreground</BitText>
<BitText Foreground=""BitColorKind.Secondary"">Secondary foreground</BitText>
<BitText Foreground=""BitColorKind.Tertiary"">Tertiary foreground</BitText>

<div style=""background:linear-gradient(blue, pink);background-clip:text;"">
    <BitText Foreground=""BitColorKind.Transparent"">Transparent foreground</BitText>
</div>";

    private string example4RazorCode = @"
<BitText Color=""BitColor.Primary"">Primary color</BitText>
<BitText Color=""BitColor.Secondary"">Secondary color</BitText>
<BitText Color=""BitColor.Tertiary"">Tertiary color</BitText>

<BitText Color=""BitColor.Info"">Info color</BitText>
<BitText Color=""BitColor.Success"">Success color</BitText>
<BitText Color=""BitColor.Warning"">Warning color</BitText>
<BitText Color=""BitColor.SevereWarning"">SevereWarning color</BitText>
<BitText Color=""BitColor.Error"">Error color</BitText>

<BitText Color=""BitColor.PrimaryBackground"">PrimaryBackground color</BitText>
<BitText Color=""BitColor.SecondaryBackground"">SecondaryBackground color</BitText>
<BitText Color=""BitColor.TertiaryBackground"">TertiaryBackground color</BitText>

<BitText Color=""BitColor.PrimaryForeground"">PrimaryForeground color</BitText>
<BitText Color=""BitColor.SecondaryForeground"">SecondaryForeground color</BitText>
<BitText Color=""BitColor.TertiaryForeground"">TertiaryForeground color</BitText>

<BitText Color=""BitColor.PrimaryBorder"">PrimaryBorder color</BitText>
<BitText Color=""BitColor.SecondaryBorder"">SecondaryBorder color</BitText>
<BitText Color=""BitColor.TertiaryBorder"">TertiaryBorder color</BitText>";
}
