namespace System.Threading;

public static class CancellationTokenSourceExtensions
{
    extension(CancellationTokenSource source)
    {
        /// <summary>
        /// Tries to cancel the <see cref="CancellationTokenSource"/> without throwing an exception if it has already been disposed.
        /// </summary>
        public async Task<bool> TryCancel()
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
}
