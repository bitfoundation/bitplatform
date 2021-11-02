using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class ComponentPage
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public string ComponentName { get; set; }
        [Parameter] public string OverviewDesc { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public List<EnumParameter> EnumParameters { get; set; } = new();
        [Parameter] public List<ComponentParameter> ComponentParameters { get; set; } = new();
        [Parameter] public List<ComponentSubParameter> ComponentSubParameters { get; set; } = new();

        private async Task HandleClick()
        {
            await JSRuntime.ShowHideNavbar();
        }
    }
}
