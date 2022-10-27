namespace TodoTemplate.Client.Shared.Shared;

public partial class MessageBox : IDisposable
{
    private static event Func<string, string, Task> OnShow = default!;

    public static async Task Show(string message, string title = "")
    {
        if (OnShow is not null)
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
            IsOpen = true;

            Title = title;
            Body = message;

            StateHasChanged();
        });
    }

    // ========================================================================

    private bool IsOpen { get; set; }
    private string Title { get; set; } = string.Empty;
    private string Body { get; set; } = string.Empty;

    private void OnCloseClick()
    {
        IsOpen = false;
    }

    private void OnOkClick()
    {
        IsOpen = false;
    }

    public void Dispose()
    {
        OnShow -= ShowMessageBox;
    }
}
