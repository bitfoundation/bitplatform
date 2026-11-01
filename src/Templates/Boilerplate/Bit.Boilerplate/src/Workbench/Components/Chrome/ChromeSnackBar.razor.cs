namespace Boilerplate.Workbench.Components.Chrome;

/// <summary>The app's snack bar in a view, showing the messages pages send through the pub-sub service.</summary>
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
