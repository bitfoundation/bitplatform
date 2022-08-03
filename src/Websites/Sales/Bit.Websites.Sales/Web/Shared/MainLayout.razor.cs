using Microsoft.AspNetCore.Components.Web;

namespace Bit.Websites.Sales.Web.Shared;

public partial class MainLayout
{
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    public bool RemovePadding { get; set; } = false;
    public bool SmallPadding { get; set; } = false;

    protected override void OnParametersSet()
    {
        // TODO: we can try to recover from exception after rendering the ErrorBoundary with this line.
        // but for now it's better to persist the error ui until a force refresh.
        // ErrorBoundaryRef.Recover();

        base.OnParametersSet();
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var currentUrl = navigationManager.Uri.Replace(navigationManager.BaseUri, "/", StringComparison.Ordinal);
            if (currentUrl == "/process" || currentUrl == "/services" || currentUrl == "/about")
            {
                RemovePadding = true;
            }

            if (currentUrl.Contains("case"))
            {
                SmallPadding = true;
            }

            await base.OnInitializedAsync();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }
}
