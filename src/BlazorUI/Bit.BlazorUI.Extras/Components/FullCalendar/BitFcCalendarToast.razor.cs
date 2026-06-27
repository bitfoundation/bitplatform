namespace Bit.BlazorUI;

public partial class BitFcCalendarToast : IAsyncDisposable
{
    private readonly List<ToastItem> _toasts = [];
    private readonly List<CancellationTokenSource> _removalTokens = [];
    private readonly object _removalTokensLock = new();
    private int _nextId;

    public void Show(string message, bool isError = false)
    {
        // Allocate the id atomically: Show can be invoked off the renderer thread (the list mutation
        // is marshalled below, but the id is assigned here on the caller's thread), so a plain
        // _nextId++ could hand out duplicate ids under concurrent calls and break RemoveAfterDelay's
        // per-id removal.
        var item = new ToastItem { Id = Interlocked.Increment(ref _nextId), Message = message, IsError = isError };

        var cts = new CancellationTokenSource();
        // Capture the token up front, before DisposeAsync (which can run concurrently and dispose
        // the source) gets a chance to. Reading cts.Token later inside RemoveAfterDelay would risk
        // an ObjectDisposedException if the component is torn down before the delay is queued.
        var token = cts.Token;
        lock (_removalTokensLock)
        {
            _removalTokens.Add(cts);
        }

        // Marshal the list mutation and render onto the renderer's dispatcher so the whole toast
        // lifecycle (add here, remove in RemoveAfterDelay) stays dispatcher-safe even when Show is
        // invoked from a non-renderer thread.
        _ = InvokeAsync(() =>
        {
            // DisposeAsync cancels every queued token; if it ran between scheduling this callback
            // and it executing, bail out before touching _toasts/StateHasChanged so no UI work (or
            // RemoveAfterDelay timer) starts after the component has been torn down.
            if (token.IsCancellationRequested)
                return;

            _toasts.Add(item);
            StateHasChanged();
            // Start the expiration timer only after the toast has actually been queued into the UI,
            // so the 3s lifetime begins from when it becomes visible rather than from when Show was
            // scheduled (which may run on a non-renderer thread before the add is dispatched).
            _ = RemoveAfterDelay(item.Id, cts, token);
        });
    }

    private async Task RemoveAfterDelay(int id, CancellationTokenSource cts, CancellationToken token)
    {
        try
        {
            try
            {
                await Task.Delay(3000, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Final cancellation check before the dispatcher hop: DisposeAsync may have cancelled the
            // token after Task.Delay completed but before we queue the render. Bail out so _toasts and
            // StateHasChanged are never touched once teardown has started.
            if (token.IsCancellationRequested)
                return;

            // Mutate the toast list on the renderer's dispatcher to avoid racing the template's foreach.
            await InvokeAsync(() =>
            {
                // Re-check inside the dispatcher callback: DisposeAsync can cancel after the check
                // above but before this lambda runs, so no-op once teardown has begun instead of
                // mutating _toasts / calling StateHasChanged on a disposed component.
                if (token.IsCancellationRequested)
                    return;

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
