namespace Bit.Brouter;

/// <summary>
/// Cascaded to every route's rendered content (all routes, not just keep-alive ones) as the
/// delivery channel for the route lifecycle (<see cref="IBrouterRoute"/>). One stable instance
/// exists per content session - it is created when the content mounts and lives exactly as long
/// as that content instance, surviving keep-alive hide/show flips, so per-parameter keep-alive
/// entries each carry their own context. Components rendered directly by the router
/// (<see cref="Broute.Component"/>) are registered automatically when they implement
/// <see cref="IBrouterRoute"/>; anything else - <see cref="Broute.Content"/> fragments,
/// <see cref="Brouter.Found"/>-rendered pages, or descendants at any depth - takes this as a
/// <c>[CascadingParameter]</c> and calls <see cref="Register"/> (or simply derives from
/// <see cref="BrouterRouteBase"/>, which does both).
/// </summary>
/// <remarks>
/// The cascade is fixed (the instance never changes for the lifetime of its content), so consuming
/// it causes no re-render churn. <see cref="IsActive"/> is maintained by the navigation pipeline:
/// it flips to <c>false</c> when the content is deactivated (hidden or about to be disposed) and
/// back to <c>true</c> on activation. Handlers registered mid-session are not retroactively
/// activated - read <see cref="IsActive"/> for the current state and use the callbacks for
/// transitions. All members are invoked on the renderer's dispatcher.
/// </remarks>
public sealed class BrouterRouteContext
{
    private readonly List<IBrouterRoute> _handlers = [];

    // Cached auto-registration delegate for AddComponentReferenceCapture (a method-group
    // conversion would allocate a fresh delegate on every render of the routed component), and the
    // single auto-registered handler slot it maintains: the router instantiates at most one page
    // component per context, so a recapture (the page instance replaced within a surviving session,
    // e.g. by an error-boundary swap) displaces the previous - now disposed - instance instead of
    // accumulating dead handlers that would keep receiving callbacks.
    private Action<object>? _autoRegisterDelegate;
    private IBrouterRoute? _autoRegistered;

    // Contexts are created by the render that mounts their content, which only happens for the
    // matched (visible) pass - so IsActive is accurate from the very first render, including under
    // static prerendering where the activation callback itself never fires (no interactive render
    // completes there). HasEverActivated stays false until the first activation callback actually
    // runs, which is what keeps IsFirstActivation truthful.
    internal BrouterRouteContext(bool initiallyActive) => IsActive = initiallyActive;

    /// <summary>
    /// <c>true</c> while this content is the committed, visible route content; <c>false</c> while
    /// it is kept mounted but hidden (keep-alive) or is being torn down. Accurate from the
    /// content's first render on.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// <c>true</c> once this content's activation callback has fired at least once. While
    /// <c>false</c>, the next activation is the first (see
    /// <see cref="BrouterRouteActivation.IsFirstActivation"/>) - useful for late-registering
    /// handlers that need to know whether they missed it.
    /// </summary>
    public bool HasEverActivated { get; private set; }

    /// <summary>
    /// Registers a lifecycle handler. Idempotent per handler instance. Components that can outlive
    /// this context's content session never do so in practice (the context dies with its subtree),
    /// but a component disposed <em>within</em> the session (e.g. conditionally rendered content)
    /// should call <see cref="Unregister"/> from its <c>Dispose</c> -
    /// <see cref="BrouterRouteBase"/> handles both ends automatically.
    /// </summary>
    public void Register(IBrouterRoute handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (_handlers.Contains(handler)) return;
        _handlers.Add(handler);
    }

    /// <summary>Removes a previously registered lifecycle handler. No-op when not registered.</summary>
    public void Unregister(IBrouterRoute handler) => _handlers.Remove(handler);

    // Auto-registration hook for components the router instantiates itself (see the
    // AddComponentReferenceCapture calls in BrouterRouteRenderer/BrouterOutlet). One slot: a
    // recaptured (replaced) page instance displaces the previous registration - see _autoRegistered.
    internal Action<object> AutoRegisterDelegate => _autoRegisterDelegate ??= component =>
    {
        if (component is not IBrouterRoute handler) return;
        if (ReferenceEquals(_autoRegistered, handler)) return;
        if (_autoRegistered is not null) _handlers.Remove(_autoRegistered);
        _autoRegistered = handler;
        Register(handler);
    };

    /// <summary>
    /// Resolves an arrival on this context: a renavigation when the content was already active AND
    /// has received its activation (same instance re-committed), otherwise an activation - which
    /// covers both a fresh session's first arrival (contexts are born active, see the constructor)
    /// and a kept-alive reveal. Called post-render by the navigation pipeline.
    /// </summary>
    internal void FireArrival(BrouterLocation from, BrouterLocation to, Action<Exception> onError)
    {
        if (IsActive && HasEverActivated)
        {
            FireRenavigated(to, from, onError);
        }
        else
        {
            FireActivated(to, onError);
        }
    }

    internal void FireActivated(BrouterLocation location, Action<Exception> onError)
    {
        var first = HasEverActivated is false;
        IsActive = true;
        HasEverActivated = true;
        if (_handlers.Count == 0) return;

        var args = new BrouterRouteActivation(first, location);
        // Snapshot: a handler may register/unregister others (or itself) from its callback.
        foreach (var handler in _handlers.ToArray())
        {
            Invoke(handler.OnActivatedAsync, args, onError);
        }
    }

    internal void FireDeactivated(BrouterRouteDeactivationReason reason, BrouterLocation location, Action<Exception> onError)
    {
        // Genuinely idempotent at the type level: several pipeline paths can plausibly notify the
        // same departure (a pending-UI render followed by an error render, teardown after a normal
        // departure), and no caller should have to remember a guard to avoid double-firing user
        // callbacks.
        if (IsActive is false) return;
        IsActive = false;
        if (_handlers.Count == 0) return;

        var args = new BrouterRouteDeactivation(reason, location);
        foreach (var handler in _handlers.ToArray())
        {
            Invoke(handler.OnDeactivatedAsync, args, onError);
        }
    }

    internal void FireRenavigated(BrouterLocation location, BrouterLocation previousLocation, Action<Exception> onError)
    {
        if (_handlers.Count == 0) return;

        var args = new BrouterRouteRenavigation(location, previousLocation);
        foreach (var handler in _handlers.ToArray())
        {
            Invoke(handler.OnRenavigatedAsync, args, onError);
        }
    }

    // Lifecycle callbacks are started synchronously (so a Disposing deactivation's synchronous
    // prefix is guaranteed to run before the content is torn down) but never awaited by the
    // pipeline - navigation must not be delayed by user lifecycle code. The returned task is
    // observed so async failures still surface (through onError -> IBrouter.OnError) instead of
    // becoming unobserved-task noise.
    private static void Invoke<TArgs>(Func<TArgs, ValueTask> callback, TArgs args, Action<Exception> onError)
    {
        try
        {
            var task = callback(args);
            if (task.IsCompletedSuccessfully) return;
            _ = ObserveAsync(task, onError);
        }
        catch (Exception ex)
        {
            onError(ex);
        }
    }

    private static async Task ObserveAsync(ValueTask task, Action<Exception> onError)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            onError(ex);
        }
    }
}
