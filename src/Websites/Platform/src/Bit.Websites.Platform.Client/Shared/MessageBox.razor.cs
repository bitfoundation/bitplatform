namespace Bit.Websites.Platform.Client.Shared;

public partial class MessageBox : IDisposable
{
    private bool isOpen;
    private string? title;
    private string? body;

    private TaskCompletionSource<object?>? currentTcs;

    private async Task OnCloseClick()
    {
        isOpen = false;
        await JSRuntime.ToggleBodyOverflow(false);
        currentTcs?.SetResult(null);
        currentTcs = null;
    }

    private async Task OnOkClick()
    {
        isOpen = false;
        await JSRuntime.ToggleBodyOverflow(false);
        currentTcs?.SetResult(null);
        currentTcs = null;
    }

    Action? _dispose;
    bool _disposed = false;

    protected override Task OnInitAsync()
    {
        _dispose = PubSubService.Subscribe(PubSubMessages.SHOW_MESSAGE, async args =>
        {
            (var message, string title, TaskCompletionSource<object?> tcs) = ((string message, string title, TaskCompletionSource<object?> tsc))args!;
            await (currentTcs?.Task ?? Task.CompletedTask);
            currentTcs = tcs;
            await ShowMessageBox(message, title);
        });

        return base.OnInitAsync();
    }

    private async Task ShowMessageBox(string message, string title = "")
    {
        await InvokeAsync(() =>
        {
            _ = JSRuntime.ToggleBodyOverflow(true);

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
        if (_disposed || disposing is false) return;

        currentTcs?.TrySetResult(null);
        currentTcs = null;
        _dispose?.Invoke();

        _disposed = true;
    }
}
