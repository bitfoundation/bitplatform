namespace Boilerplate.Client.Core.Shared;

public partial class MessageBox : IDisposable
{
    private bool isOpen;
    private string? title;
    private string? body;

    private TaskCompletionSource<object?>? tcs;

    private async Task OnCloseClick()
    {
        isOpen = false;
        await JSRuntime.SetBodyOverflow(false);
        tcs?.SetResult(null);
        tcs = null;
    }

    private async Task OnOkClick()
    {
        isOpen = false;
        await JSRuntime.SetBodyOverflow(false);
        tcs?.SetResult(null);
        tcs = null;
    }

    Action? dispose;
    bool disposed = false;

    protected override Task OnInitAsync()
    {
        dispose = PubSubService.Subscribe(PubSubMessages.SHOW_MESSAGE, async args =>
        {
            (var message, string title, TaskCompletionSource<object?> tcs) = ((string message, string title, TaskCompletionSource<object?> tcs))args!;
            await (this.tcs?.Task ?? Task.CompletedTask);
            this.tcs = tcs;
            await ShowMessageBox(message, title);
        });

        return base.OnInitAsync();
    }

    private async Task ShowMessageBox(string message, string title = "")
    {
        await InvokeAsync(() =>
        {
            _ = JSRuntime.SetBodyOverflow(true);

            isOpen = true;
            this.title = title;
            body = message;

            StateHasChanged();
        });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed || disposing is false) return;

        tcs?.TrySetResult(null);
        tcs = null;
        dispose?.Invoke();

        disposed = true;
    }
}
