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
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Rect))]
    public static async Task<ButilSubscription> ObserveResize(
        this ElementReference element,
        IJSRuntime js,
        Action<ResizeObserverEntry[]> handler,
        ResizeObserverBox box = ResizeObserverBox.ContentBox)
    {
        var host = new ResizeObserverInterop(handler);
        var listenerId = Guid.NewGuid();

        var boxName = box switch
        {
            ResizeObserverBox.BorderBox => "border-box",
            ResizeObserverBox.DevicePixelContentBox => "device-pixel-content-box",
            _ => "content-box",
        };

        await js.InvokeVoid("BitButil.resizeObserver.observe",
            host.DotNetRef,
            listenerId,
            element,
            boxName);

        return new ButilSubscription(listenerId, async () =>
        {
            try { await js.InvokeVoid("BitButil.resizeObserver.unobserve", listenerId); }
            finally { host.Dispose(); }
        });
    }
}
