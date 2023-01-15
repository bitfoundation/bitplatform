namespace AdminPanel.Client.Shared;

public partial class MessageBox : IDisposable
{
    private bool _isOpen;
    private bool _disposed;
    private string _title = string.Empty;
    private string _body = string.Empty;

    private static event Func<string, string, Task> OnShow = default!;

    public static async Task Show(string message, string title = "")
    {
        if (OnShow is null) return;

        await OnShow.Invoke(message, title);
    }

    protected override void OnInitialized()
    {
        OnShow += ShowMessageBox;

        base.OnInitialized();
    }

    private async Task ShowMessageBox(string message, string title)
    {
        await InvokeAsync(() =>
        {
            _isOpen = true;
            _title = title;
            _body = message;
        });
    }

    private void OnCloseClick()
    {
        _isOpen = false;
    }

    private void OnOkClick()
    {
        _isOpen = false;
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
