namespace Bit.Bmotion;

/// <summary>
/// Brings a <see cref="Bm.Velocity"/> value back to zero once its source stops changing. A motion value reports only
/// its changes, so a source that stops - a scroll that ends - sends nothing more, and the velocity derived from its
/// last change would hold that speed forever. Each change re-arms one timer for the moment the source would count
/// as standing still (<see cref="BmValue{T}.StaleAfterMs"/>); when it fires with no change since, the velocity settles
/// on what <see cref="BmValue{T}.GetVelocity"/> now reports: zero.
/// </summary>
/// <remarks>
/// The settle runs where the change that armed it ran - the circuit's synchronization context on Blazor Server, the
/// one thread of the browser on WebAssembly - so whatever the velocity drives is updated from the thread it always is.
/// The velocity is held weakly, like every derived value (see <see cref="BmValue{T}.Transform{TOut}"/>): the source
/// must not keep it alive.
/// </remarks>
internal sealed class BmVelocitySettle : IDisposable
{
    private readonly BmValue<double> _source;
    private readonly WeakReference<BmValue<double>> _velocity;
    private readonly Timer _timer;
    private readonly IDisposable _subscription;
    private SynchronizationContext? _context;
    private bool _disposed;

    public BmVelocitySettle(BmValue<double> source, BmValue<double> velocity)
    {
        _source = source;
        _velocity = new(velocity);
        _timer = new Timer(_ => Due(), null, Timeout.Infinite, Timeout.Infinite);
        _subscription = source.Subscribe(_ => Arm());
    }

    private void Arm()
    {
        if (_disposed) return;
        _context = SynchronizationContext.Current;
        // A millisecond past the window, so the check finds the source stale rather than on the boundary.
        _timer.Change(_source.StaleAfterMs + 1, Timeout.Infinite);
    }

    private void Due()
    {
        if (_context is { } context) context.Post(_ => Settle(), null);
        else Settle();
    }

    private void Settle()
    {
        if (_disposed) return;
        if (!_velocity.TryGetTarget(out var velocity))
        {
            Dispose();   // the velocity was dropped without being disposed: stop listening to the source
            return;
        }
        // A change since the timer was armed re-armed it, so this normally finds the source still; GetVelocity is the
        // judge either way, and a source still moving keeps its velocity.
        if (_source.GetVelocity() == 0 && velocity.Value != 0) velocity.SetSync(0);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _subscription.Dispose();
        _timer.Dispose();
    }
}
