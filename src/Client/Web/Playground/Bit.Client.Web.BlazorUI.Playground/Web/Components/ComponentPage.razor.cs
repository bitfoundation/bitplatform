using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using Bit.Client.Web.BlazorUI.Playground.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class ComponentPage
    {
        private List<SiderailParameter> siderailParameters { get; set; } = new()
        {
            new SiderailParameter()
            {
                Title = "Overview",
                Class = "active",
                TargetId = "overview-section"
            },
            new SiderailParameter()
            {
                Title = "Usage",
                Class = "",
                TargetId = "usage-section"
            },
            new SiderailParameter()
            {
                Title = "Implementation",
                Class = "",
                TargetId = "implementation-section"
            }
        };

        [Inject] public NavManuService NavManuService { get; set; }

        [Parameter] public string ComponentName { get; set; }
        [Parameter] public string OverviewDesc { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public List<EnumParameter> EnumParameters { get; set; } = new();
        [Parameter] public List<ComponentParameter> ComponentParameters { get; set; } = new();
        [Parameter] public List<ComponentSubParameter> ComponentSubParameters { get; set; } = new();

        private void ToggleMenu()
        {
            NavManuService.ToggleMenu();
        }
    }
}
