using Bit.BlazorUI.Demo.Client.Shared.Models;
using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.Typography;

public partial class BitTypographyDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of the Typography.",
        },
        new()
        {
            Name = "Component",
            Type = "string?",
            Description = "The component used for the root node.",
        },
        new()
        {
            Name = "NoWrap",
            Type = "bool",
            Description = "If true, the text will not wrap, but instead will truncate with a text overflow ellipsis.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitTypographyVariant",
            Description = "The variant of the Typography.",
            LinkType = LinkType.Link,
            Href = "typography-variant-enum"
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "typography-variant-enum",
            Name = "BitTypographyVariant",
            Items = new List<ComponentEnumItem>()
            {
                new() { Name = "Body1" },
                new() { Name = "Body2" },
                new() { Name = "Button" },
                new() { Name = "Caption" },
                new() { Name = "H1" },
                new() { Name = "H2" },
                new() { Name = "H3" },
                new() { Name = "H4" },
                new() { Name = "H5" },
                new() { Name = "H6" },
                new() { Name = "Inherit" },
                new() { Name = "Overline" },
                new() { Name = "Subtitle1" },
                new() { Name = "Subtitle2" },
            }
        }
    };



    private string example1HTMLCode = @"
<BitTypography>This is default (Subtitle1)</BitTypography>
<br />
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
