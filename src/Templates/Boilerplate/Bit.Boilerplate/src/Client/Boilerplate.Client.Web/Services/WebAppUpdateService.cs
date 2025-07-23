
namespace Boilerplate.Client.Web.Services;

public partial class WebAppUpdateService : IAppUpdateService
{
    [AutoInject] private IJSRuntime jsRuntime = default!;

    public async Task ForceUpdate()
    {
        const bool autoReload = true;
        await jsRuntime.InvokeVoidAsync("tryUpdatePwa", autoReload);
    }
}
