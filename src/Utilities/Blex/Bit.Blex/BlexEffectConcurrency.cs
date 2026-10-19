namespace Bit.Blex;

/// <summary>
/// How overlapping invocations of the same <see cref="BlexEffectAttribute">effect</see> are handled,
/// mirroring the RxJS flattening operators used by NgRx effects.
/// </summary>
public enum BlexEffectConcurrency
{
    /// <summary>All invocations run concurrently (<c>mergeMap</c>). The default.</summary>
    Parallel = 0,

    /// <summary>
    /// A new invocation cancels the previous one (<c>switchMap</c> / "take latest"). Ideal for
    /// type-ahead search. Requires a <see cref="System.Threading.CancellationToken"/> parameter on
    /// the effect method for the running body to actually observe the cancellation. A superseded
    /// run can never overwrite the newest run's error state. An invocation vetoed by middleware
    /// does not supersede anything: the run already in flight keeps going.
    /// </summary>
    Latest = 1,

    /// <summary>
    /// New invocations are ignored while one is running (<c>exhaustMap</c> / "take leading").
    /// Ideal for guarding against double-clicked submit buttons. An invocation dropped this way
    /// never reaches the middleware pipeline -- nothing was dispatched.
    /// </summary>
    Drop = 2,

    /// <summary>
    /// Invocations run one at a time in arrival order (<c>concatMap</c>). Ideal for writes where
    /// ordering matters. A Queue effect must never invoke itself (directly or via a subscriber
    /// its body triggers) -- the nested call would wait on its own queue slot and deadlock.
    /// Middleware sees an invocation when it is made, not when it reaches the front of the queue,
    /// so a vetoed invocation never takes a slot and cannot delay the ones behind it.
    /// </summary>
    Queue = 3,
}
