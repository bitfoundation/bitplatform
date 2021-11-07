using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components
{
    public partial class SideRail
    {
        private string activeItem;

        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Parameter] public List<SideRailItems> SideRailItems { get; set; } = new List<SideRailItems>();

        protected override void OnInitialized()
        {
            activeItem = SideRailItems.FirstOrDefault().Id;

            base.OnInitialized();
        }

        private async Task ScrollToItem(SideRailItems targetItem)
        {
            activeItem = targetItem.Id;

            await JSRuntime.ScrollToElement(targetItem.Id);
        }
    }
}
