using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/WebXR_Device_API">WebXR Device API</see>:
/// starts VR and AR sessions, reports where the user's head and controllers are, and tells you when
/// they act.
/// </summary>
/// <remarks>
/// Butil covers the parts of WebXR that make sense across an interop boundary - session lifecycle,
/// poses, input sources and events - and stops short of rendering. A headset runs at 90 Hz or more,
/// and marshalling every frame into .NET would cost more than the frames are worth; so
/// <see cref="XrSessionHandle.AttachCanvas"/> gives the session a WebGL layer to present, and what
/// is drawn into it stays in JavaScript.
/// <br/>
/// What .NET is good for here is the logic around the session: whether to offer the button at all
/// (<see cref="IsSessionSupported"/>), starting and ending it, reacting to a trigger pull, and
/// reading a pose at UI speed.
/// <br/>
/// An immersive session needs a user gesture, a secure context, and a device. An
/// <see cref="XrSessionMode.Inline"/> session needs none of those, which makes it the one worth
/// falling back to on a phone or a laptop.
/// </remarks>
[ButilService(typeof(WebXr))]
public class WebXr(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when the runtime exposes <c>navigator.xr</c>.</summary>
    /// <remarks>
    /// True doesn't mean a headset is connected - <see cref="IsSessionSupported"/> is the question
    /// worth asking before showing an "enter VR" button.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.webXr.isSupported");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRSystem/isSessionSupported">XRSystem.isSessionSupported()</see>:
    /// whether a session of this mode could be started on this device right now.
    /// </summary>
    /// <param name="mode">The mode to ask about.</param>
    /// <remarks>
    /// Needs no user gesture, so it is safe to call on page load to decide whether to offer the
    /// button at all.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSessionSupported(XrSessionMode mode)
        => js.Invoke<bool>("BitButil.webXr.isSessionSupported", ToName(mode));

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRSystem/requestSession">XRSystem.requestSession()</see>:
    /// starts a session and its reference space, and begins the frame loop that keeps poses current.
    /// </summary>
    /// <param name="mode">Inline, immersive VR, or immersive AR.</param>
    /// <param name="options">Required and optional features, the reference space, and how often to push poses.</param>
    /// <param name="onEnd">
    /// Called when the session ends - including when the user takes the headset off or presses the
    /// system button, which is the only way to learn about that.
    /// </param>
    /// <param name="onInput">Called on each select and squeeze event.</param>
    /// <param name="onPose">
    /// Called on the interval set by <see cref="XrSessionOptions.PoseIntervalMs"/>. Ignored when that
    /// is 0, which is the default.
    /// </param>
    /// <returns>
    /// The session handle, or <c>null</c> when there is no device, a required feature is missing, no
    /// user gesture was behind the call, or no usable reference space could be obtained.
    /// </returns>
    /// <remarks>
    /// An immersive session must be requested from a user-gesture handler such as a click; an inline
    /// one need not be. Give the session something to present with
    /// <see cref="XrSessionHandle.AttachCanvas"/> - an immersive session without a layer runs, but
    /// shows the user a black display.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(XrSessionJsOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(XrSessionJsInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(XrPose))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(XrView))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(XrRigidTransform))]
    public async ValueTask<XrSessionHandle?> RequestSession(XrSessionMode mode,
                                                            XrSessionOptions? options = null,
                                                            Action? onEnd = null,
                                                            Action<XrInputEvent>? onInput = null,
                                                            Action<XrPose>? onPose = null)
    {
        var id = Guid.NewGuid();
        var handle = new XrSessionHandle(js, id, onEnd, onInput, onPose);

        XrSessionJsInfo? info;
        try
        {
            info = await js.Invoke<XrSessionJsInfo?>("BitButil.webXr.requestSession",
                                                     id, ToName(mode), (options ?? new XrSessionOptions()).ToJsObject(),
                                                     handle.CallbackRef,
                                                     XrSessionHandle.EndMethodName, XrSessionHandle.InputMethodName, XrSessionHandle.PoseMethodName);
        }
        catch
        {
            // The handle owns a DotNetObjectReference from the moment it is constructed, so a throw
            // that never returns it to the caller has to release it here.
            await handle.DisposeAsync();
            throw;
        }

        if (info is null)
        {
            await handle.DisposeAsync();
            return null;
        }

        handle.Initialize(info);
        return handle;
    }

    /// <summary>
    /// On scope/circuit teardown, ends every session whose <see cref="XrSessionHandle"/> was never
    /// disposed - a session left running would hold the display after the page that started it is
    /// gone.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try { await js.InvokeVoid("BitButil.webXr.disposeAll"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        GC.SuppressFinalize(this);
    }

    private static string ToName(XrSessionMode mode) => mode switch
    {
        XrSessionMode.ImmersiveVr => "immersive-vr",
        XrSessionMode.ImmersiveAr => "immersive-ar",
        _ => "inline"
    };
}
