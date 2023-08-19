using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class ComponentDemo
{
    [Parameter] public string? ComponentName { get; set; }
    [Parameter] public string? ComponentDescription { get; set; }
    [Parameter] public string? Notes { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public List<ComponentParameter> ComponentParameters { get; set; } = new();
    [Parameter] public List<ComponentSubClass> ComponentSubClasses { get; set; } = new();
    [Parameter] public List<ComponentSubEnum> ComponentSubEnums { get; set; } = new();


    private readonly List<ComponentParameter> _baseComponentParameters = new()
    {
        new()
        {
            Name = "Class",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom CSS class for the root element of the component.",
        },
        new()
        {
            Name = "IsEnabled",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether or not the component is enabled.",
        },
        new()
        {
            Name = "Style",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom CSS style for the root element of the component.",
        },
        new()
        {
            Name = "Visibility",
            Type = "BitVisibility",
            DefaultValue = "BitVisibility.Visible",
            Description = "Whether the component is visible, hidden or collapsed.",
            LinkType = LinkType.Link,
            Href = "#component-visibility",
        },
    };

    private readonly List<ComponentSubEnum> _baseComponentSubEnums = new()
    {
        new()
        {
            Id = "component-visibility",
            Name = "BitVisibility",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Visible",
                    Value="0",
                    Description="The content of the component is visible.",
                },
                new()
                {
                    Name= "Hidden",
                    Value="1",
                    Description="The content of the component is hidden, but the space it takes on the page remains (visibility:hidden).",
                },
                new()
                {
                    Name= "Collapsed",
                    Value="2",
                    Description="The component is hidden (display:none).",
                }
            }
        }
    };
}
