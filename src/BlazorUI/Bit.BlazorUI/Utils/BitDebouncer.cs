namespace Bit.BlazorUI;

public class BitDebouncer
{
    private CancellationTokenSource _cts = new();

    /// <summary>
    /// Runs <paramref name="func"/> once <paramref name="milliseconds"/> have passed without another call, cancelling the
    /// pending run of the previous call. The function runs on the context the call was made from (a component's
    /// renderer, where it can render), and what it throws, a cancellation aside, reaches the awaiter of the call that
    /// ran it.
    /// </summary>
    public async Task Do(int milliseconds, Func<Task> func)
    {
        Cancel();

        var token = _cts.Token;

        try
        {
            await Task.Delay(milliseconds, token);

            if (token.IsCancellationRequested) return;

            await func();
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Cancels the currently pending (delayed) invocation, if any.
    /// </summary>
    public void Cancel()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new();
    }
}
