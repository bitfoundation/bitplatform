using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Extension methods that wire <c>IntersectionObserver</c> onto an <see cref="ElementReference"/>.
/// Use the returned <see cref="ButilSubscription"/> to stop observing.
/// </summary>
public static class IntersectionObserverExtensions
{
    /// <summary>
    /// Observes intersection events for the given element. The handler receives the
    /// most recent batch of <see cref="IntersectionObserverEntry"/> values.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IntersectionObserverEntry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IntersectionObserverOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IntersectionObserverListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Rect))]
    public static async Task<ButilSubscription> ObserveIntersection(
        this ElementReference element,
        IJSRuntime js,
        Action<IntersectionObserverEntry[]> handler,
        IntersectionObserverOptions? options = null)
    {
        var listenerId = IntersectionObserverListenersManager.AddListener(handler);

        await js.InvokeVoid("BitButil.intersectionObserver.observe",
            IntersectionObserverListenersManager.InvokeMethodName,
            listenerId,
            element,
            options);

        return new ButilSubscription(listenerId, async () =>
        {
            IntersectionObserverListenersManager.RemoveListener(listenerId);
            if (OperatingSystem.IsBrowser() is false) return;
            await js.InvokeVoid("BitButil.intersectionObserver.unobserve", listenerId);
        });
    }
}
