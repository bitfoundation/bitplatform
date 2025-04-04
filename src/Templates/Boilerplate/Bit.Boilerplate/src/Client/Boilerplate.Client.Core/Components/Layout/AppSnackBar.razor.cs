namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppSnackBar
{
    private Action? unsubscribe;
    private BitSnackBar snackbarRef = default!;

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        unsubscribe = PubSubService.Subscribe(ClientPubSubMessages.SHOW_SNACK, async args =>
        {
            var (title, body, color) = ((string, string, BitColor))args!;

            await snackbarRef.Show(title, body, color);
        });
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);
        unsubscribe?.Invoke();
    }
}
