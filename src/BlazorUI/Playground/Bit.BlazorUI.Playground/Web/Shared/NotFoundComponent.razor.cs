using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Playground.Web.Shared;

public partial class NotFoundComponent
{
    [Inject] public NavigationManager NavigationManager { get; set; }

    private void BackToHome()
    {
        NavigationManager.NavigateTo("/");
    }
}
