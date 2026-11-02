namespace Bit.Bmotion;

/// <summary>
/// Brings a <see cref="Bm.Velocity"/> value back to zero once its source stops changing. A motion value reports only
/// its changes, so a source that stops - a scroll that ends - sends nothing more, and the velocity derived from its
/// last change would hold that speed forever. A change schedules one check for the moment the source would count as
/// standing still (<see cref="BmValue{T}.MsUntilStill"/>); the check finds it so and settles the velocity on zero, or -
/// the source changed since, or the timer ran early by the source's clock - checks again when the new moment comes.
/// </summary>
/// <remarks>
/// The check runs where the change that scheduled it ran - the circuit's synchronization context on Blazor Server, the
/// one thread of the browser on WebAssembly - so whatever the velocity drives is updated from the thread it always is.
/// A source changed from a thread with no way back to it (no context, off WebAssembly) schedules nothing: a check from a
/// timer's thread would race the source's own updates, so there the velocity keeps its last speed until the source
/// changes again (the source's <see cref="BmValue{T}.GetVelocity"/> reads 0 meanwhile). The velocity is held weakly,
/// like every derived value (see <see cref="BmValue{T}.Transform{TOut}"/>): the source must not keep it alive.
/// </remarks>
internal sealed class BmVelocitySettle : IDisposable
{
    private readonly BmValue<double> _source;
    private readonly WeakReference<BmValue<double>> _velocity;
    private readonly IDisposable _subscription;
    private bool _scheduled;
    private volatile bool _disposed;

    public BmVelocitySettle(BmValue<double> source, BmValue<double> velocity)
    {
        _source = source;
        _velocity = new(velocity);
        // One check at a time: a change while one is scheduled only moves the moment that check looks for.
        _subscription = source.Subscribe(_ => { if (!_scheduled) Schedule(); });
        // A source already moving starts the velocity off its speed, and may stop before it changes again.
        if (velocity.Value != 0) Schedule();
    }

    // A millisecond past the moment, so the check finds the source still rather than on the boundary.
    private void Schedule()
    {
        if (_disposed) return;
        _scheduled = BmotionContinuations.AfterDelayOnCallerContext(_source.MsUntilStill() + 1, Check);
    }

    private void Check()
    {
        _scheduled = false;
        if (_disposed) return;
        if (!_velocity.TryGetTarget(out var velocity))
        {
            Dispose();   // the velocity was dropped without being disposed: stop listening to the source
            return;
        }
        if (_source.MsUntilStill() >= 0) Schedule();
        else if (velocity.Value != 0) velocity.SetSync(0);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _subscription.Dispose();
    }
}
