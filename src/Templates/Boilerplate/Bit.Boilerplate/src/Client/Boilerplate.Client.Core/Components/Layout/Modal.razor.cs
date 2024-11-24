namespace Boilerplate.Client.Core.Components.Layout;

public partial class Modal
{
    private bool isOpen;
    private string? title;
    private Type? componentType;
    private Action? unsubscribe;
    private bool disposed = false;
    private IDictionary<string, object>? componentParameters;

    private TaskCompletionSource<bool>? tcs;

    protected override Task OnInitAsync()
    {
        unsubscribe = PubSubService.Subscribe(ClientPubSubMessages.SHOW_MODAL, async args =>
        {
            var data = (ModalData)args!;

            tcs = data.TaskCompletionSource;

            await Show(data.ComponentType, data.Parameters, data.Title);
        });

        return base.OnInitAsync();
    }

    private async Task Show(Type type, IDictionary<string, object>? parameters, string? title)
    {
        await InvokeAsync(() =>
        {
            isOpen = true;
            this.title = title;
            componentType = type;
            componentParameters = parameters;

            StateHasChanged();
        });
    }

    private async Task OnCloseClick()
    {
        isOpen = false;
        tcs?.SetResult(false);
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        if (disposed || disposing is false) return;

        tcs?.TrySetResult(false);
        tcs = null;

        unsubscribe?.Invoke();

        disposed = true;
    }
}
