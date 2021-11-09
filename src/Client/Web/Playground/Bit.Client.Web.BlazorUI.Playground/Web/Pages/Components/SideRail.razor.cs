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
        [Parameter] public List<SideRailItem> Items { get; set; } = new List<SideRailItem>();

        protected override void OnInitialized()
        {
            activeItem = Items.FirstOrDefault().Id;

            base.OnInitialized();
        }

        private async Task ScrollToItem(SideRailItem targetItem)
        {
            activeItem = targetItem.Id;

            await JSRuntime.ScrollToElement(targetItem.Id);
        }
    }
}
