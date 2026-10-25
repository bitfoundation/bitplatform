namespace Bit.BlazorUI.Tests.E2E.TestHost;

/// <summary>
/// What holds the requests of <c>/api/upload-slow</c>: each waits for a permit the test hands out, so an upload stays in
/// flight for exactly as long as a test needs it to - however long a loaded runner takes to get to its Pause or its
/// Cancel - rather than for a fixed time a slow click can outlast. <c>release?count=n</c> lets the next n requests
/// through (a permit handed out before its request arrives is kept for it), <c>open</c> lets every request through
/// until <c>close</c> holds them again, which every test of the page starts with. A request nobody lets through goes on
/// after <see cref="MaxHold"/>, so a test that forgets to let it go is slow rather than hung.
/// </summary>
public sealed class SlowUploadGate
{
    private static readonly TimeSpan MaxHold = TimeSpan.FromSeconds(30);

    private readonly object _lock = new();
    private SemaphoreSlim _permits = new(0);
    private bool _open;

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        SemaphoreSlim permits;

        lock (_lock)
        {
            if (_open) return;
            permits = _permits;
        }

        await permits.WaitAsync(MaxHold, cancellationToken);
    }

    public void Release(int count)
    {
        lock (_lock) _permits.Release(Math.Max(1, count));
    }

    public void Open()
    {
        lock (_lock)
        {
            _open = true;
            // The requests already waiting go on too.
            _permits.Release(short.MaxValue);
        }
    }

    public void Close()
    {
        lock (_lock)
        {
            _open = false;
            // A previous test's requests still waiting are let go with their permits, which go with them.
            var previous = _permits;
            _permits = new SemaphoreSlim(0);
            previous.Release(short.MaxValue);
        }
    }
}
