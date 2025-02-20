namespace Boilerplate.Client.Core.Components.Layout;

public partial class SnackBar
{
    private Action? unsubscribe;
    private BitSnackBar snackbarRef = default!;

    protected override async Task OnAfterFirstRenderAsync()
    {
        unsubscribe = PubSubService.Subscribe(ClientPubSubMessages.SHOW_SNACK, async args =>
        {
            var (title, body, color) = ((string, string, BitColor))args!;

            await snackbarRef.Show(title, body, color);
        });

        await base.OnAfterFirstRenderAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        unsubscribe?.Invoke();
        await base.DisposeAsync(disposing);
    }
}
