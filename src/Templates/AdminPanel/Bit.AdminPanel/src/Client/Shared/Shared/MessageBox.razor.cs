namespace AdminPanel.Client.Shared;

public partial class MessageBox : IDisposable
{
    private static event Func<string, string, Task> OnShow = default!;

    private bool _isOpen;
    private string? _title;
    private string? _body;
    private bool _disposed;

    private static TaskCompletionSource<object?>? _tsc;

    public static async Task Show(string message, string title = "")
    {
        _tsc = new TaskCompletionSource<object?>();

        await OnShow.Invoke(message, title);

        await _tsc.Task;
    }

    protected override Task OnInitAsync()
    {
        OnShow += ShowMessageBox;

        return base.OnInitAsync();
    }

    private async Task ShowMessageBox(string message, string title)
    {
        await InvokeAsync(() =>
        {
            _ = JsRuntime.SetBodyOverflow(true);

            _isOpen = true;
            _title = title;
            _body = message;

            StateHasChanged();
        });
    }

    private void OnCloseClick()
    {
        _isOpen = false;
        _tsc?.SetResult(null);
    }

    private void OnOkClick()
    {
        _isOpen = false;
        _tsc?.SetResult(null);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        OnShow -= ShowMessageBox;

        _disposed = true;
    }
}
