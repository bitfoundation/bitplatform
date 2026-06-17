using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Element-scoped DOM event subscriptions. Returns an <see cref="IAsyncDisposable"/> handle
/// (<see cref="ButilSubscription"/>) so callers can <c>await using</c> for the lifetime of a
/// component without hand-rolling Add/Remove pairs.
/// </summary>
/// <remarks>
/// Each subscription owns a per-subscription <see cref="DotNetObjectReference{T}"/> (there is no
/// long-lived service instance to host it, since these are extension methods). The reference - and
/// therefore all captured component state - is released when the returned subscription is disposed,
/// so there is no static state and no cross-circuit bleed.
/// </remarks>
public static class ElementReferenceEventExtensions
{
    /// <summary>
    /// Subscribes to a DOM event on the given element. The returned handle detaches the listener on dispose.
    /// </summary>
    /// <remarks>
    /// <b>You must dispose the returned <see cref="ButilSubscription"/>.</b> Unlike the
    /// <see cref="Window"/>/<see cref="Document"/> services, this extension has no owning scoped
    /// instance to drain on circuit teardown, so each call allocates its own
    /// <see cref="DotNetObjectReference{T}"/> and a JS-side handler entry that live until the handle
    /// is disposed. Failing to dispose leaks both (plus any state your <paramref name="listener"/>
    /// captures) for the lifetime of the circuit. Prefer <c>await using</c>, or store the handle and
    /// dispose it in the component's <c>DisposeAsync</c>.
    /// </remarks>
    public static Task<ButilSubscription> SubscribeEvent<T>(
        this ElementReference element,
        IJSRuntime js,
        string domEvent,
        Action<T> listener,
        bool useCapture = false,
        bool preventDefault = false,
        bool stopPropagation = false)
        => SubscribeEventCore(element, js, domEvent, listener, useCapture, false, false, preventDefault, stopPropagation);

    /// <summary>
    /// <see cref="ButilEventListenerOptions"/> variant of
    /// <see cref="SubscribeEvent{T}(ElementReference, IJSRuntime, string, Action{T}, bool, bool, bool)"/>,
    /// adding <c>passive</c> and <c>once</c> control on top of <c>capture</c>. The same disposal
    /// requirement applies.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsAddEventListenerOptions))]
    public static Task<ButilSubscription> SubscribeEvent<T>(
        this ElementReference element,
        IJSRuntime js,
        string domEvent,
        Action<T> listener,
        ButilEventListenerOptions options,
        bool preventDefault = false,
        bool stopPropagation = false)
        => SubscribeEventCore(element, js, domEvent, listener, options.Capture, options.Passive, options.Once, preventDefault, stopPropagation);

    private static async Task<ButilSubscription> SubscribeEventCore<T>(
        ElementReference element,
        IJSRuntime js,
        string domEvent,
        Action<T> listener,
        bool useCapture,
        bool passive,
        bool once,
        bool preventDefault,
        bool stopPropagation)
    {
        var argType = typeof(T);
        var eventType = DomEventArgs.TypeOf(domEvent);
        if (argType != eventType)
            throw new InvalidOperationException($"Invalid listener type ({argType}) for this dom event type ({eventType})");

        // Each element gets a generated id so the JS side can target it directly.
        var elementId = Guid.NewGuid().ToString("N");
        var host = new DomEventsInterop();
        var (listenerId, methodName, members, dotNetRef) = host.Register(listener, elementId, domEvent, useCapture);

        // Bare boolean for the capture-only case; full options object when passive/once are set.
        object options = (passive || once)
            ? new JsAddEventListenerOptions { Capture = useCapture, Passive = passive, Once = once }
            : useCapture;

        await js.InvokeVoid("BitButil.element.subscribeEvent",
            element,
            elementId,
            domEvent,
            methodName,
            dotNetRef,
            listenerId,
            members,
            options,
            preventDefault,
            stopPropagation);

        return new ButilSubscription(listenerId, async () =>
        {
            host.Unregister(listenerId);
            try
            {
                await js.InvokeVoid("BitButil.element.unsubscribeEvent", elementId, domEvent, listenerId, options);
            }
            finally
            {
                host.Dispose();
            }
        });
    }
}
