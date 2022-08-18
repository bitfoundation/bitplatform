namespace AdminPanel.App.Components;

public partial class ConfirmMessageBox : IDisposable
{
    private static event Func<string, string, Task<bool>> OnShow = default!;

    public static async Task<bool> Show(string message, string title)
    {
        return await OnShow.Invoke(message, title);
    }

    protected override async Task OnInitAsync()
    {
        OnShow += ShowMessageBox;

        await base.OnInitAsync();
    }

    private TaskCompletionSource<bool>? _tsc;

    private async Task<bool> ShowMessageBox(string message, string title)
    {
        _tsc = new TaskCompletionSource<bool>();

        await InvokeAsync(async () =>
        {
            IsOpen = true;
            await JsRuntime.SetToggleBodyOverflow(true);
            Title = title;
            Message = message;

            StateHasChanged();
        });

        return await _tsc.Task;
    }

    // ========================================================================

    private bool IsOpen { get; set; }
    private string Title { get; set; } = string.Empty;
    private string Message { get; set; } = string.Empty;

    public async Task Confirm(bool value)
    {
        IsOpen = false;
        await JsRuntime.SetToggleBodyOverflow(false);
        _tsc?.SetResult(value);
    }

    public void Dispose()
    {
        OnShow -= ShowMessageBox;
    }
}
