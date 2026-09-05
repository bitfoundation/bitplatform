using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bit.Butil;

/// <summary>
/// The shape every subscribing API in the package shares: mint an id, record the handler under it,
/// ask JS to attach, and hand back a <see cref="ButilSubscription"/> that removes both again.
/// </summary>
/// <remarks>
/// The part worth having in one place is the rollback. A handler left in the dictionary after an
/// attach that refused or threw can never fire and can never be removed, and the six statements
/// that prevent it are identical everywhere - so a change of policy (whether the error channel is
/// raised before the throw, what happens when JS rejects) is one edit rather than eleven.
/// <br/>
/// Prerender/SSR counts as a successful attach, for the reason given on
/// <see cref="InternalJSRuntimeExtensions.InvokeRegister"/>: nothing is registered and nothing
/// failed, and the subscription is simply inert until the app is interactive.
/// </remarks>
internal static class ButilSubscriptionHelper
{
    /// <summary>
    /// Registers a listener whose JS side reports whether it attached by returning a boolean.
    /// </summary>
    /// <param name="handlers">The service's handler registry; the entry is removed again on failure.</param>
    /// <param name="handler">What to record under the new id.</param>
    /// <param name="register">Attaches the JS listener for the id. Normally an <c>InvokeRegister</c> call.</param>
    /// <param name="unregister">Detaches it again. Runs when the returned subscription is disposed.</param>
    /// <param name="failureMessage">What to raise when the JS side refuses.</param>
    /// <param name="onFailure">
    /// The caller's error channel, raised with <paramref name="failureMessage"/> before the throw so
    /// a service that has one reports the failure exactly once and through both routes.
    /// </param>
    /// <exception cref="InvalidOperationException">The JS side did not attach the listener.</exception>
    internal static async ValueTask<ButilSubscription> Register<TValue>(ConcurrentDictionary<Guid, TValue> handlers,
                                                                       TValue handler,
                                                                       Func<Guid, ValueTask<bool>> register,
                                                                       Func<Guid, ValueTask> unregister,
                                                                       string failureMessage,
                                                                       Action<string>? onFailure = null)
    {
        var id = Guid.NewGuid();
        handlers[id] = handler;

        bool registered;
        try
        {
            registered = await register(id);
        }
        catch
        {
            // Nothing is listening on the JS side, so the entry must not outlive the call.
            handlers.TryRemove(id, out _);
            throw;
        }

        if (registered is false)
        {
            handlers.TryRemove(id, out _);
            onFailure?.Invoke(failureMessage);
            throw new InvalidOperationException(failureMessage);
        }

        return Subscription(handlers, id, unregister);
    }

    /// <summary>
    /// <see cref="Register"/> for a JS side that reports failure by returning the reason as a
    /// string, and null when it attached - so the message on the exception is what the browser
    /// actually said rather than a guess made on this side.
    /// </summary>
    /// <exception cref="InvalidOperationException">The JS side did not attach the listener.</exception>
    internal static async ValueTask<ButilSubscription> RegisterOrError<TValue>(ConcurrentDictionary<Guid, TValue> handlers,
                                                                              TValue handler,
                                                                              Func<Guid, ValueTask<string?>> register,
                                                                              Func<Guid, ValueTask> unregister,
                                                                              Action<string>? onFailure = null)
    {
        var id = Guid.NewGuid();
        handlers[id] = handler;

        string? failure;
        try
        {
            failure = await register(id);
        }
        catch
        {
            // Nothing is listening on the JS side, so the entry must not outlive the call.
            handlers.TryRemove(id, out _);
            throw;
        }

        if (failure is not null)
        {
            // The reason comes back with the call rather than through the error callback, so it is
            // raised here exactly once - and with what the browser said, instead of a generic
            // message racing the dispatched one.
            handlers.TryRemove(id, out _);
            onFailure?.Invoke(failure);
            throw new InvalidOperationException(failure);
        }

        return Subscription(handlers, id, unregister);
    }

    private static ButilSubscription Subscription<TValue>(ConcurrentDictionary<Guid, TValue> handlers,
                                                          Guid id,
                                                          Func<Guid, ValueTask> unregister)
        // The entry goes first: once it is out, a callback still in flight from JS finds nothing to
        // invoke, which is what "disposed" has to mean even before the detach round-trip lands.
        => new(id, async () =>
        {
            handlers.TryRemove(id, out _);
            await unregister(id);
        });
}
