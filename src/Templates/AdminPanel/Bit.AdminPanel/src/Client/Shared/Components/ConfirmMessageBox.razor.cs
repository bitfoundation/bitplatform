namespace AdminPanel.Client.Shared.Components;

public partial class ConfirmMessageBox : IDisposable
{
    private static event Func<string, string, Task<bool>> OnShow = default!;

    private bool _isOpen;
    private string? _title;
    private string? _message;
    private bool _disposed;

    public static async Task<bool> Show(string message, string title)
    {
        return await OnShow.Invoke(message, title);
    }

    protected override async Task OnInitAsync()
    {
        OnShow += ShowMessageBox;

        await Task.CompletedTask;
    }

    private TaskCompletionSource<bool>? _tsc;

    private async Task<bool> ShowMessageBox(string message, string title)
    {
        _tsc = new TaskCompletionSource<bool>();

        await InvokeAsync(() =>
        {
            _isOpen = true;
            
            _ = JsRuntime.SetToggleBodyOverflow(true);
            
            _title = title;
            _message = message;
        });

        return await _tsc.Task;
    }

    public async Task Confirm(bool value)
    {
        _isOpen = false;
        await JsRuntime.SetToggleBodyOverflow(false);
        _tsc?.SetResult(value);
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
