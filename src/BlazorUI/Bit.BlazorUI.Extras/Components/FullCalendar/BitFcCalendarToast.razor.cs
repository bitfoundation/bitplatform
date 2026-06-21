namespace Bit.BlazorUI;

public partial class BitFcCalendarToast : IAsyncDisposable
{
    private readonly List<ToastItem> _toasts = [];
    private readonly List<CancellationTokenSource> _removalTokens = [];
    private int _nextId;

    public void Show(string message, bool isError = false)
    {
        var item = new ToastItem { Id = _nextId++, Message = message, IsError = isError };
        _toasts.Add(item);
        StateHasChanged();

        var cts = new CancellationTokenSource();
        _removalTokens.Add(cts);
        _ = RemoveAfterDelay(item.Id, cts.Token);
    }

    private async Task RemoveAfterDelay(int id, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(3000, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Mutate the toast list on the renderer's dispatcher to avoid racing the template's foreach.
        await InvokeAsync(() =>
        {
            _toasts.RemoveAll(t => t.Id == id);
            StateHasChanged();
        });
    }

    public ValueTask DisposeAsync()
    {
        foreach (var cts in _removalTokens)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _removalTokens.Clear();
        return ValueTask.CompletedTask;
    }

    private class ToastItem
    {
        public int Id { get; set; }
        public string Message { get; set; } = "";
        public bool IsError { get; set; }
    }
}
