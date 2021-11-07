using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components
{
    public partial class SideRail
    {
        [Inject] public IJSRuntime? JSRuntime { get; set; }

        [Parameter] public List<SideRailParameter> SideRailParameters { get; set; }


        private async Task ScrollToFragment(SideRailParameter targetElement)
        {
            SideRailParameters.ForEach(param => param.Class = string.Empty);
            targetElement.Class = "active";

            await JSRuntime!.ScrollToFragmentOnClickEvent(targetElement.TargetId);
        }
    }
}
