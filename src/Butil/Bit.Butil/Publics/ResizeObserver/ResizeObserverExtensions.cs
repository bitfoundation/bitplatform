using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Extension methods that wire <c>ResizeObserver</c> onto an <see cref="ElementReference"/>.
/// </summary>
public static class ResizeObserverExtensions
{
    /// <summary>
    /// Observes resize events for the given element. Use the returned
    /// <see cref="ButilSubscription"/> to stop observing.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ResizeObserverEntry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ResizeObserverListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Rect))]
    public static async Task<ButilSubscription> ObserveResize(
        this ElementReference element,
        IJSRuntime js,
        Action<ResizeObserverEntry[]> handler,
        ResizeObserverBox box = ResizeObserverBox.ContentBox)
    {
        var listenerId = ResizeObserverListenersManager.AddListener(handler);

        var boxName = box switch
        {
            ResizeObserverBox.BorderBox => "border-box",
            ResizeObserverBox.DevicePixelContentBox => "device-pixel-content-box",
            _ => "content-box",
        };

        await js.InvokeVoid("BitButil.resizeObserver.observe",
            ResizeObserverListenersManager.InvokeMethodName,
            listenerId,
            element,
            boxName);

        return new ButilSubscription(listenerId, async () =>
        {
            ResizeObserverListenersManager.RemoveListener(listenerId);
            if (OperatingSystem.IsBrowser() is false) return;
            await js.InvokeVoid("BitButil.resizeObserver.unobserve", listenerId);
        });
    }
}
