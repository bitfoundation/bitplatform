using Bit.BlazorUI.Playground.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Playground.Web.Pages;

public partial class OverviewPage
{
    [Inject] public NavManuService NavManuService { get; set; }

    private void ToggleMenu()
    {
        NavManuService.ToggleMenu();
    }
}
