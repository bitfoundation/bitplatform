namespace Boilerplate.Client.Core.Components.Layout;

public partial class ForceUpdatePanel
{
    [AutoInject] private IAppUpdateService appUpdateService = default!;

    private BitSnackBar bitSnackBar = default!;

    private Action? unsubscribe;
    private bool isVisible = false;

    protected override async Task OnInitAsync()
    {
        if (InPrerenderSession is false)
        {
            unsubscribe = PubSubService.Subscribe(ClientPubSubMessages.FORCE_UPDATE, async (_) =>
            {
                if (isVisible)
                    return;
                isVisible = true;
                await bitSnackBar.Show(string.Empty);
            });
        }

        await base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        unsubscribe?.Invoke();

        await base.DisposeAsync(disposing);
    }

    private async Task OnDismiss()
    {
        if (isVisible)
        {
            await bitSnackBar.Show(string.Empty);
        }
    }
}
