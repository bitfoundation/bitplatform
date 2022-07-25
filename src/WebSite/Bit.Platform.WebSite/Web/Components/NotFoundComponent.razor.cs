using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Platform.WebSite.Web.Components;

public partial class NotFoundComponent
{
    [AutoInject] public NavigationManager NavigationManager { get; set; }

    private void BackToHome()
    {
        NavigationManager.NavigateTo("/");
    }
}
