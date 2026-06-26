namespace Bit.BlazorUI;

public partial class BitFcCalendarToast : IAsyncDisposable
{
    private readonly List<ToastItem> _toasts = [];
    private readonly List<CancellationTokenSource> _removalTokens = [];
    private readonly object _removalTokensLock = new();
    private int _nextId;

    public void Show(string message, bool isError = false)
    {
        var item = new ToastItem { Id = _nextId++, Message = message, IsError = isError };

        var cts = new CancellationTokenSource();
        lock (_removalTokensLock)
        {
            _removalTokens.Add(cts);
        }

        // Marshal the list mutation and render onto the renderer's dispatcher so the whole toast
        // lifecycle (add here, remove in RemoveAfterDelay) stays dispatcher-safe even when Show is
        // invoked from a non-renderer thread.
        _ = InvokeAsync(() =>
        {
            _toasts.Add(item);
            StateHasChanged();
        });
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
            bool removed;
            lock (_removalTokensLock)
            {
                removed = _removalTokens.Remove(cts);
            }
            if (removed)
                cts.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        // Snapshot under the lock: RemoveAfterDelay also removes/disposes tokens as their timers
        // complete, so reading the live list here could race with that cleanup.
        CancellationTokenSource[] tokens;
        lock (_removalTokensLock)
        {
            tokens = _removalTokens.ToArray();
            _removalTokens.Clear();
        }

        foreach (var cts in tokens)
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
        return ValueTask.CompletedTask;
    }

    private class ToastItem
    {
        public int Id { get; set; }
        public string Message { get; set; } = "";
        public bool IsError { get; set; }
    }
}
