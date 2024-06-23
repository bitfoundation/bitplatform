namespace Bit.BlazorUI;

public class BitThrottler
{
    private bool _throttlePause;

    public async Task Do(int milliseconds, Func<Task> func)
    {
        if (_throttlePause) return;

        _throttlePause = true;

        await Task.Run(async () =>
        {
            await Task.Delay(milliseconds);

            await func();

            _throttlePause = false;
        });
    }
}
