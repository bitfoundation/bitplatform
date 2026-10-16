namespace Bit.BlazorUI;

public class BitThrottler
{
    private bool _throttlePause;
    private CancellationTokenSource _cts = new();

    /// <summary>
    /// Runs <paramref name="func"/> once <paramref name="milliseconds"/> have passed, dropping every call made in the
    /// meantime. The function runs on the context the call was made from (a component's renderer, where it can render),
    /// and what it throws, a cancellation aside, reaches the awaiter of the call that ran it - the throttle opens again
    /// either way.
    /// </summary>
    public async Task Do(int milliseconds, Func<Task> func)
    {
        if (_throttlePause) return;

        _throttlePause = true;

        var token = _cts.Token;

        try
        {
            await Task.Delay(milliseconds, token);

            if (token.IsCancellationRequested) return;

            await func();
        }
        catch (OperationCanceledException) { }
        finally
        {
            // Only the run that closed the gate opens it again: a Reset in the meantime has already opened it, and
            // a newer run may have closed it since.
            if (token == _cts.Token)
            {
                _throttlePause = false;
            }
        }
    }

    /// <summary>
    /// Cancels the currently pending (trailing) invocation, if any, and resets the throttle gate.
    /// </summary>
    public void Reset()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new();
        _throttlePause = false;
    }
}
