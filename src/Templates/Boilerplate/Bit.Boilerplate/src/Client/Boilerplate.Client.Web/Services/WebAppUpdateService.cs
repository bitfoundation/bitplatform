
namespace Boilerplate.Client.Web.Services;

public partial class WebAppUpdateService : IAppUpdateService
{
    [AutoInject] private IJSRuntime jsRuntime = default!;

    public async Task Update()
    {
        await jsRuntime.InvokeVoidAsync("BitBswup.checkForUpdate"); // Call bit Bswup update.
    }
}
