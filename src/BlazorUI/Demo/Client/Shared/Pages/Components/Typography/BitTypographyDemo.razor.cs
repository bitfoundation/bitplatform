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

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "typography-variant-enum",
            Title = "BitTypographyVariant Enum",
            EnumList = new List<EnumItem>()
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



    private string example1HTMLCode = @"";
}
