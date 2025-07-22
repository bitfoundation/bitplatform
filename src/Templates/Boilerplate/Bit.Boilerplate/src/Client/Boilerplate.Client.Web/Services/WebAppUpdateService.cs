
namespace Boilerplate.Client.Web.Services;

public partial class WebAppUpdateService : IAppUpdateService
{
    [AutoInject] private IJSRuntime jsRuntime = default!;

    public async Task ForceUpdate()
    {
        await jsRuntime.InvokeVoidAsync("tryUpdatePwa");
    }
}
