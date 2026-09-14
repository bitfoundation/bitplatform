using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Ink_API">Ink API</see>
/// (delegated ink trails): while the user draws, the compositor paints ahead of your rendering so the
/// line keeps up with the stylus.
/// </summary>
/// <remarks>
/// A drawing app renders a stroke when its input event reaches the app, which is at least a frame
/// behind the pointer - visible as a gap between the stylus tip and the ink. Delegated ink hands the
/// last known point to the compositor, which draws the missing segment itself, outside the app's
/// frame loop. The app still draws the real stroke; this only covers the gap until it does.
/// <br/>
/// The presenter has to be fed the browser's own <c>PointerEvent</c>, which is exactly what cannot
/// cross the interop boundary - by the time .NET saw one it would be a copy, and an untrusted one.
/// So the listener lives in JavaScript and C# only turns it on and off. That is also why this is a
/// trail you start on an element rather than a method you call per point.
/// <br/>
/// Chromium-only, and an enhancement by nature: when it is unavailable nothing is lost but the few
/// milliseconds of latency it was hiding.
/// </remarks>
[ButilService(typeof(Ink))]
public class Ink(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when the runtime exposes <c>navigator.ink.requestPresenter</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.ink.isSupported");

    /// <summary>
    /// Starts painting a delegated ink trail over an element as the pointer moves across it.
    /// </summary>
    /// <param name="presentationArea">
    /// The element the trail is drawn within - usually the canvas you are drawing on. The trail is
    /// clipped to it.
    /// </param>
    /// <param name="color">The trail's colour, as any CSS colour string.</param>
    /// <param name="diameter">The trail's width in pixels. Match it to your own stroke width, or the seam shows.</param>
    /// <returns>A handle - dispose it to stop - or null when the API is unavailable or the element can't present ink.</returns>
    /// <remarks>
    /// Keep drawing your own strokes as before. This does not replace your rendering; it only fills
    /// the gap between the pointer and the last frame you drew.
    /// </remarks>
    public async ValueTask<InkTrailHandle?> StartTrail(ElementReference presentationArea, string color = "black", double diameter = 3)
    {
        var id = Guid.NewGuid();
        var started = await js.Invoke<bool>("BitButil.ink.start", id, presentationArea, color, diameter);
        return started ? new InkTrailHandle(js, id) : null;
    }

    /// <summary>
    /// On scope/circuit teardown, stops any trail whose <see cref="InkTrailHandle"/> was never
    /// disposed, so an abandoned pointer listener can't outlive the component that started it.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await js.InvokeVoid("BitButil.ink.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed

        GC.SuppressFinalize(this);
    }
}
