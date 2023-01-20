namespace TodoTemplate.Client.Shared;

public partial class MessageBox : IDisposable
{
    private static event Func<string, string, Task> OnShow = default!;

    private bool _isOpen;
    private string _title = string.Empty;
    private string _body = string.Empty;

    public static async Task Show(string message, string title = "")
    {
        if (OnShow is not null)
        {
            await OnShow.Invoke(message, title);
        }
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

    private void OnCloseClick() => _isOpen = false;

    private void OnOkClick() => _isOpen = false;

    public void Dispose() => OnShow -= ShowMessageBox;
}
