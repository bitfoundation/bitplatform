using Microsoft.AspNetCore.Components;

namespace Bit.Platform.WebSite.Web.Components;

public partial class NotFoundComponent
{
    [Inject] public NavigationManager NavigationManager { get; set; }

    private void BackToHome()
    {
        NavigationManager.NavigateTo("/");
    }
}
