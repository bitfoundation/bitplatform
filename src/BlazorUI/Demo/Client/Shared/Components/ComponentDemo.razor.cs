using Bit.BlazorUI.Demo.Client.Shared.Models;
using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Components;

public partial class ComponentDemo
{
    private List<SideRailItem> items { get; set; } = new()
    {
        new SideRailItem()
        {
            Title = "Overview",
            Id = "overview-section"
        },
        new SideRailItem()
        {
            Title = "Usage",
            Id = "usage-section"
        },
        new SideRailItem()
        {
            Title = "Implementation",
            Id = "implementation-section"
        }
    };

    [Parameter] public string ComponentName { get; set; }
    [Parameter] public string ComponentDescription { get; set; }
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public List<EnumParameter> EnumParameters { get; set; } = new();
    [Parameter] public List<ComponentParameter> ComponentParameters { get; set; } = new();
    [Parameter] public List<ComponentSubParameter> ComponentSubParameters { get; set; } = new();
}
