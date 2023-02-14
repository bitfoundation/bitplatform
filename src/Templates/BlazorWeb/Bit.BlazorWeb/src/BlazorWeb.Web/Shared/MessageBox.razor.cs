namespace BlazorWeb.Web;

public partial class MessageBox : IDisposable
{
    private static event Func<string, string, Task> OnShow = default!;

    private bool _isOpen;
    private string _title = string.Empty;
    private string _body = string.Empty;

    private static TaskCompletionSource<object?>? _tsc;

    public static async Task Show(string message, string title = "")
    {
        _tsc = new TaskCompletionSource<object?>();

        await OnShow.Invoke(message, title);

        await _tsc.Task;
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

    public void Dispose() => OnShow -= ShowMessageBox;
}
