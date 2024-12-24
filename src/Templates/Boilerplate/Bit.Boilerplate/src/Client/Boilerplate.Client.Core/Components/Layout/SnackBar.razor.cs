namespace Boilerplate.Client.Core.Components.Layout;

public partial class SnackBar : IDisposable
{
    private Action? unsubscribe;
    private BitSnackBar snackbarRef = default!;


    protected override Task OnInitAsync()
    {
        unsubscribe = PubSubService.Subscribe(ClientPubSubMessages.SHOW_SNACK, async args =>
        {
            var (title, body, color) = ((string, string, BitColor))args!;

            await snackbarRef.Show(title, body, color);
        });

        return base.OnInitAsync();
    }


    public void Dispose()
    {
        unsubscribe?.Invoke();
    }
}
