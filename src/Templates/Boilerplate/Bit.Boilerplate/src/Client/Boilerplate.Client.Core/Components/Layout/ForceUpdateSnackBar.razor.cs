namespace Boilerplate.Client.Core.Components.Layout;

public partial class ForceUpdateSnackBar
{
    [AutoInject] private IAppUpdateService appUpdateService = default!;


    private bool isShown;
    private Action? unsubscribe;
    private BitSnackBar bitSnackBar = default!;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (InPrerenderSession) return;

        unsubscribe = PubSubService.Subscribe(ClientAppMessages.FORCE_UPDATE, async (_) =>
        {
            if (isShown) return;

            isShown = true;
            await bitSnackBar.Error(string.Empty);
        });
    }


    private async Task Update()
    {
        await appUpdateService.ForceUpdate();
    }


    protected override async ValueTask DisposeAsyncCore()
    {
        unsubscribe?.Invoke();

        await base.DisposeAsyncCore();
    }
}
