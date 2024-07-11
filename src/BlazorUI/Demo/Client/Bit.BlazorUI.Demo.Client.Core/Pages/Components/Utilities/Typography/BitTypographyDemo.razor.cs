namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Typography;

public partial class BitTypographyDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Typography.",
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
            Name = "NoWrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the text will not wrap, but instead will truncate with a text overflow ellipsis.",
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
            Name = "Variant",
            Type = "BitTypographyVariant",
            DefaultValue = "BitTypographyVariant.Subtitle1",
            Description = "The variant of the Typography.",
            LinkType = LinkType.Link,
            Href = "#typography-variant-enum"
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "typography-variant-enum",
            Name = "BitTypographyVariant",
            Items =
            [
                new() { Name = "Body1", Value = "0" },
                new() { Name = "Body2", Value = "1" },
                new() { Name = "Button", Value = "2" },
                new() { Name = "Caption", Value = "3" },
                new() { Name = "H1", Value = "4" },
                new() { Name = "H2", Value = "5" },
                new() { Name = "H3", Value = "6" },
                new() { Name = "H4", Value = "7" },
                new() { Name = "H5", Value = "8" },
                new() { Name = "H6", Value = "9" },
                new() { Name = "Inherit", Value = "10" },
                new() { Name = "Overline", Value = "11" },
                new() { Name = "Subtitle1", Value = "12" },
                new() { Name = "Subtitle2", Value = "13" },
            ]
        }
    ];



    private string example1RazorCode = @"
<BitTypography>This is default (Subtitle1)</BitTypography>

<BitTypography Variant=""BitTypographyVariant.H1"">H1. Heading</BitTypography>
<BitTypography Variant=""BitTypographyVariant.H2"">H2. Heading</BitTypography>
<BitTypography Variant=""BitTypographyVariant.H3"">H3. Heading</BitTypography>
<BitTypography Variant=""BitTypographyVariant.H4"">H4. Heading</BitTypography>
<BitTypography Variant=""BitTypographyVariant.H5"">H5. Heading</BitTypography>
<BitTypography Variant=""BitTypographyVariant.H6"">H6. Heading</BitTypography>

<BitTypography Variant=""BitTypographyVariant.Subtitle1"">Subtitle1. Lorem ipsum dolor sit amet</BitTypography>
<BitTypography Variant=""BitTypographyVariant.Subtitle2"">Subtitle2. Lorem ipsum dolor sit amet</BitTypography>

<BitTypography Variant=""BitTypographyVariant.Body1"">Body1. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Quos blanditiis tenetur unde suscipit, quam beatae rerum inventore consectetur, neque doloribus, cupiditate numquam dignissimos laborum fugiat deleniti? Eum quasi quidem quibusdam.</BitTypography>
<BitTypography Variant=""BitTypographyVariant.Body2"">Body2. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Quos blanditiis tenetur unde suscipit, quam beatae rerum inventore consectetur, neque doloribus, cupiditate numquam dignissimos laborum fugiat deleniti? Eum quasi quidem quibusdam.</BitTypography>

<BitTypography Variant=""BitTypographyVariant.Button"">Button. Click Me</BitTypography>
<BitTypography Variant=""BitTypographyVariant.Caption"">Caption. Hello World!</BitTypography>
<BitTypography Variant=""BitTypographyVariant.Overline"">Overline. this is overline text.</BitTypography>";
}
