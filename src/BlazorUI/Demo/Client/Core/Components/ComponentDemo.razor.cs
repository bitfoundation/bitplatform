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
}
