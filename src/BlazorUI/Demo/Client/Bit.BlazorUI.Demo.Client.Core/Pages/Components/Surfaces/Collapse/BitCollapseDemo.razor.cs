namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Collapse;

public partial class BitCollapseDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitCollapseClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the collapse.",
            LinkType = LinkType.Link,
            Href = "#collapse-class-styles"
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the collapse."
        },
        new()
        {
            Name = "Expanded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the BitCollapse is expanded or collapsed."
        },
        new()
        {
            Name = "Styles",
            Type = "BitCollapseClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the collapse.",
            LinkType = LinkType.Link,
            Href = "#collapse-class-styles"
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "collapse-class-styles",
            Title = "BitCollapseClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCollapse."
                },
                new()
                {
                    Name = "Expanded",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the expanded state of the BitCollapse."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content of the BitCollapse."
                }
            ]
        }
    ];

    private bool expanded = true;
    private bool expandedRtl = true;
    private bool expandedClass = true;
    private bool expandedStyle = true;
}
