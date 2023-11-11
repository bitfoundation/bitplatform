namespace BlazorWeb.Client.Shared;

public partial class ConfirmMessageBox
{
    private bool _isOpen;
    private string? _title;
    private string? _message;

    public async Task<bool> Show(string message, string title)
    {
        if (_tsc is not null)
            await _tsc.Task;

        _tsc = new TaskCompletionSource<bool>();

        await InvokeAsync(() =>
        {
            _ = JSRuntime.SetBodyOverflow(true);
            _isOpen = true;
            _title = title;
            _message = message;
            StateHasChanged();
        });

        return await _tsc.Task;
    }

    private TaskCompletionSource<bool>? _tsc;

    public async Task Confirm(bool value)
    {
        _isOpen = false;
        await JSRuntime.SetBodyOverflow(false);
        _tsc?.SetResult(value);
    }
}
