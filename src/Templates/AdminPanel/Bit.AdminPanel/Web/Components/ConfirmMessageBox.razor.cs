namespace AdminPanel.App.Components;

public partial class ConfirmMessageBox : IDisposable
{
    private static event Func<string, string, string, Task<bool>> OnShow = default!;

    public static async Task<bool> Show(string message, string context, string title)
    {
        return await OnShow.Invoke(message, context, title);
    }

    protected override async Task OnInitAsync()
    {
        OnShow += ShowMessageBox;

        await base.OnInitAsync();
    }

    private TaskCompletionSource<bool>? _tsc;

    private async Task<bool> ShowMessageBox(string message, string context, string title)
    {
        _tsc = new TaskCompletionSource<bool>();

        await InvokeAsync(() =>
        {
            IsOpen = true;
            Title = title;
            Message = message;
            Context = context;

            StateHasChanged();
        });

        return await _tsc.Task;
    }

    // ========================================================================

    private bool IsOpen { get; set; }
    private string Title { get; set; } = string.Empty;
    private string Message { get; set; } = string.Empty;

    private string Context { get; set; } = string.Empty;

    public void Confirmation(bool value)
    {
        IsOpen = false;
        _tsc?.SetResult(value);
    }

    public void Dispose()
    {
        OnShow -= ShowMessageBox;
    }
}
