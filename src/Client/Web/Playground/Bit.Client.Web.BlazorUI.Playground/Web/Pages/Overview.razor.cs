using Bit.Client.Web.BlazorUI.Playground.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class Overview
    {
        [Inject] public NavManuService NavManuService { get; set; }

        private void ToggleMenu()
        {
            NavManuService.ToggleMenu();
        }
    }
}
