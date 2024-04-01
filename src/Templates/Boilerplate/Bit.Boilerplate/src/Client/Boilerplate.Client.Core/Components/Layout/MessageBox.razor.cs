namespace Boilerplate.Client.Core.Components.Layout;

public partial class MessageBox
{
    private bool isOpen;
    private string? body;
    private string? title;
    private Action? unsubscribe;
    private bool disposed = false;

    private TaskCompletionSource<object?>? tcs;

    protected override Task OnInitAsync()
    {
        unsubscribe = PubSubService.Subscribe(PubSubMessages.SHOW_MESSAGE, async args =>
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
            isOpen = true;
            this.title = title;
            body = message;

            StateHasChanged();
        });
    }

    private async Task OnCloseClick()
    {
        isOpen = false;
        tcs?.SetResult(null);
        tcs = null;
    }

    private async Task OnOkClick()
    {
        isOpen = false;
        tcs?.SetResult(null);
        tcs = null;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(true);

        if (disposed || disposing is false) return;

        tcs?.TrySetResult(null);
        tcs = null;
        unsubscribe?.Invoke();

        disposed = true;
    }
}
