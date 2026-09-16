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
    /// <param name="element">The element to observe.</param>
    /// <param name="js">The JavaScript runtime to observe through.</param>
    /// <param name="handler">Called with each batch of <see cref="ResizeObserverEntry"/> values.</param>
    /// <param name="box">Which box model the reported sizes describe.</param>
    /// <param name="minInterval">
    /// The shortest time allowed between two calls into <paramref name="handler"/>. <c>null</c> or
    /// <see cref="TimeSpan.Zero"/> - the default - forwards every batch of entries.
    /// <br/>
    /// This is the observer most worth gating: a window drag or a flex reflow delivers an entry
    /// every frame for as long as it lasts, and each one is otherwise an interop round trip. The
    /// gate runs in JavaScript, leading-edge with a trailing send, so the size the element
    /// <em>settles</em> at is always the last thing the handler is told about.
    /// </param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ResizeObserverEntry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Rect))]
    public static async Task<ButilSubscription> ObserveResize(
        this ElementReference element,
        IJSRuntime js,
        Action<ResizeObserverEntry[]> handler,
        ResizeObserverBox box = ResizeObserverBox.ContentBox,
        TimeSpan? minInterval = null)
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
            boxName,
            minInterval?.TotalMilliseconds ?? 0);

        return new ButilSubscription(listenerId, async () =>
        {
            try { await js.InvokeVoid("BitButil.resizeObserver.unobserve", listenerId); }
            finally { host.Dispose(); }
        });
    }
}
