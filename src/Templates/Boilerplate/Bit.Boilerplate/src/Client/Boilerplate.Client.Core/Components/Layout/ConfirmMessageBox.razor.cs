namespace Boilerplate.Client.Core.Components.Layout;

public partial class ConfirmMessageBox
{
    private bool isOpen;
    private string? title;
    private string? message;

    public async Task<bool> Show(string message, string title)
    {
        if (tcs is not null)
            await tcs.Task;

        tcs = new TaskCompletionSource<bool>();

        await InvokeAsync(() =>
        {
            isOpen = true;
            this.title = title;
            this.message = message;

            StateHasChanged();
        });

        return await tcs.Task;
    }

    private TaskCompletionSource<bool>? tcs;

    public async Task Confirm(bool value)
    {
        isOpen = false;
        tcs?.SetResult(value);
    }
}
