namespace Boilerplate.Client.Core.Components.Layout;

public partial class ForceUpdateSnackBar
{
    [AutoInject] private IAppUpdateService appUpdateService = default!;

    private BitSnackBar bitSnackBar = default!;

    private Action? unsubscribe;
    private bool isVisible = false;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (InPrerenderSession) return;

        unsubscribe = PubSubService.Subscribe(ClientPubSubMessages.FORCE_UPDATE, async (_) =>
        {
            if (isVisible) return;
            isVisible = true;
            await bitSnackBar.Show(string.Empty);
        });
    }

    private async Task Update()
    {
        await appUpdateService.ForceUpdate();
    }

    private void OnDismiss()
    {
        isVisible = false;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        unsubscribe?.Invoke();

        await base.DisposeAsync(disposing);
    }
}
