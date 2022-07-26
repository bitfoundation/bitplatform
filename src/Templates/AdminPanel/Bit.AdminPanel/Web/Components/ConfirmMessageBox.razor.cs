namespace AdminPanel.App.Components;

public partial class ConfirmMessageBox : IDisposable
{
    private static TaskCompletionSource<bool>? taskSource;

    private static event Action<string, string,string> OnShow = default!;

    public static Task<bool> Show(string message, string context, string title)
    {
        taskSource = new TaskCompletionSource<bool>();
        OnShow?.Invoke(message, context, title);
        return taskSource.Task;
    }

    protected override void OnInitialized()
    {
        ConfirmMessageBox.OnShow += ShowMessageBox;

        base.OnInitialized();
    }

    private void ShowMessageBox(string message, string context, string title)
    {
        InvokeAsync(() =>
        {
            IsOpen = true;
            Title = title;
            Message = message;
            Context = context;
            StateHasChanged();
        });
    }

    // ========================================================================

    private bool IsOpen { get; set; }
    private string Title { get; set; } = string.Empty;
    private string Message { get; set; } = string.Empty;

    private string Context { get; set; } = string.Empty;

    public void Confirmation(bool value)
    {
        IsOpen = false;
        taskSource?.SetResult(value);
    }

    public void Dispose()
    {
        OnShow -= ShowMessageBox;
    }
}
