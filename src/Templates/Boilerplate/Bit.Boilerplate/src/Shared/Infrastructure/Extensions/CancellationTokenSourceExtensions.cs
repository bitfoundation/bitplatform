namespace System.Threading;

public static class CancellationTokenSourceExtensions
{
    /// <summary>
    /// Tries to cancel the <see cref="CancellationTokenSource"/> without throwing an exception if it has already been disposed.
    /// </summary>
    public static async Task<bool> TryCancel(this CancellationTokenSource source)
    {
        try
        {
            await source.CancelAsync();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }
}
