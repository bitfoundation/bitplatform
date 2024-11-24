namespace Boilerplate.Client.Core.Components.Layout;

public partial class Modal
{
    private bool isOpen;
    private string? title;
    private Type? componentType;
    private bool disposed = false;
    private Action? unsubscribeShow;
    private Action? unsubscribeClose;
    private IDictionary<string, object>? componentParameters;

    private TaskCompletionSource? tcs;

    protected override Task OnInitAsync()
    {
        unsubscribeShow = PubSubService.Subscribe(ClientPubSubMessages.SHOW_MODAL, async args =>
        {
            var data = (ModalData)args!;

            tcs = data.TaskCompletionSource;

            await InvokeAsync(() =>
            {
                isOpen = true;
                title = data.Title;
                componentType = data.ComponentType;
                componentParameters = data.Parameters;

                StateHasChanged();
            });
        });

        unsubscribeClose = PubSubService.Subscribe(ClientPubSubMessages.CLOSE_MODAL, async _ =>
        {
            await CloseModal();
            await InvokeAsync(StateHasChanged);
        });

        return base.OnInitAsync();
    }

    private async Task CloseModal()
    {
        isOpen = false;
        tcs?.SetResult();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        if (disposed || disposing is false) return;

        tcs?.TrySetResult();
        tcs = null;

        unsubscribeShow?.Invoke();
        unsubscribeClose?.Invoke();

        disposed = true;
    }
}
