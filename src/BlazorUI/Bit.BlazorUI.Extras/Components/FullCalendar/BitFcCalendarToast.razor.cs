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
        _ = RemoveAfterDelay(item.Id, cts);
    }

    private async Task RemoveAfterDelay(int id, CancellationTokenSource cts)
    {
        try
        {
            try
            {
                await Task.Delay(3000, cts.Token);
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
        finally
        {
            // Drop the token as soon as its timer finishes (or is cancelled) so _removalTokens
            // doesn't grow unbounded on long-lived pages that show many toasts.
            if (_removalTokens.Remove(cts))
                cts.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        // Snapshot first: RemoveAfterDelay also removes/disposes tokens as their timers complete,
        // so iterating the live list here could race with that cleanup.
        foreach (var cts in _removalTokens.ToArray())
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed by RemoveAfterDelay's cleanup; nothing to do.
            }
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
