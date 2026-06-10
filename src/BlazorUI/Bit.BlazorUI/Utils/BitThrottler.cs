namespace Bit.BlazorUI;

public class BitThrottler
{
    private bool _throttlePause;
    private CancellationTokenSource _cts = new();

    public async Task Do(int milliseconds, Func<Task> func)
    {
        if (_throttlePause) return;

        _throttlePause = true;

        var token = _cts.Token;

        try
        {
            await Task.Run(async () =>
            {
                await Task.Delay(milliseconds, token);

                if (token.IsCancellationRequested) return;

                await func();

                _throttlePause = false;
            }, token);
        }
        catch (OperationCanceledException)
        {
            _throttlePause = false;
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
