namespace Boilerplate.Client.Core.Components.Layout;

public partial class ForceUpdateSnackBar
{
    [AutoInject] private IAppUpdateService appUpdateService = default!;

    private BitSnackBar bitSnackBar = default!;

    private Action? unsubscribe;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (InPrerenderSession) return;

        unsubscribe = PubSubService.Subscribe(ClientPubSubMessages.FORCE_UPDATE, async (_) =>
        {
            await bitSnackBar.Show(string.Empty);
        });
    }

    private async Task Update()
    {
        await appUpdateService.ForceUpdate();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        unsubscribe?.Invoke();

        await base.DisposeAsync(disposing);
    }
}
