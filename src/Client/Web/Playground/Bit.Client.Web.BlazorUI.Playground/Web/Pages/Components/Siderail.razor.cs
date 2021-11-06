using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components
{
    public partial class Siderail
    {
        [Inject] public IJSRuntime? JSRuntime { get; set; }

        [Parameter] public List<SiderailParameter> SiderailParameters { get; set; }


        private async Task ScrollToFragment(SiderailParameter targetElement)
        {
            SiderailParameters.ForEach(param => param.Class = string.Empty);
            targetElement.Class = "active";

            await JSRuntime!.ScrollToFragmentOnClickEvent(targetElement.TargetId);
        }
    }
}
