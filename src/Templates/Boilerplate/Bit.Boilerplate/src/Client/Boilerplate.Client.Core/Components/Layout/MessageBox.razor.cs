namespace Boilerplate.Client.Core.Components.Layout;

public partial class MessageBox
{
    private bool isOpen;
    private string? body;
    private string? title;
    private Action? unsubscribe;
    private bool disposed = false;

    private TaskCompletionSource<bool>? tcs;

    protected override Task OnInitAsync()
    {
        unsubscribe = PubSubService.Subscribe(PubSubMessages.SHOW_MESSAGE, async args =>
        {
            var data = (MessageBoxData)args!;

            await (tcs?.Task ?? Task.CompletedTask);

            tcs = data.TaskCompletionSource;

            await ShowMessageBox(data.Message, data.Title);
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
        tcs?.SetResult(false);
        tcs = null;
    }

    private async Task OnOkClick()
    {
        isOpen = false;
        tcs?.SetResult(true);
        tcs = null;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(true);

        if (disposed || disposing is false) return;

        tcs?.TrySetResult(false);
        tcs = null;

        unsubscribe?.Invoke();

        disposed = true;
    }
}
