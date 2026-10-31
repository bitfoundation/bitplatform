namespace Boilerplate.Workbench.Components.Chrome;

public partial class ChromeSnackBar
{
    private Action? unsubscribe;
    private BitSnackBar snackBarRef = default!;

    [AutoInject] private PubSubService pubSubService = default!;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender is false)
            return;

        unsubscribe = pubSubService.Subscribe(ClientAppMessages.SHOW_SNACK, async args =>
        {
            var (title, body, color) = ((string, string, BitColor))args!;
            await snackBarRef.Show(title, body, color);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        unsubscribe?.Invoke();
        await base.DisposeAsync();
    }
}
