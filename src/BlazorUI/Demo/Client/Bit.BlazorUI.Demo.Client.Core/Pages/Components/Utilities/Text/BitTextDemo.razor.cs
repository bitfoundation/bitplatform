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
}
