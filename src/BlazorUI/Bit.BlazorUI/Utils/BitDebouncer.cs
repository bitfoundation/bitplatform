namespace Bit.BlazorUI;

public class BitDebouncer
{
    private CancellationTokenSource _cts = new();

    public async Task Do(int milliseconds, Func<Task> func)
    {
        Cancel();

        var token = _cts.Token;

        try
        {
            await Task.Run(async () =>
            {
                await Task.Delay(milliseconds, token);

                if (token.IsCancellationRequested) return;

                await func();
            }, token);
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
