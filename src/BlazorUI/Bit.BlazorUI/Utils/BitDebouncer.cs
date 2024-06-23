namespace Bit.BlazorUI;

public class BitDebouncer
{
    private CancellationTokenSource _cts = new();

    public async Task Do(int milliseconds, Func<Task> func)
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new();

        await Task.Run(async () =>
        {
            await Task.Delay(milliseconds, _cts.Token);

            if (_cts.IsCancellationRequested) return;

            await func();
        }, _cts.Token);
    }
}
