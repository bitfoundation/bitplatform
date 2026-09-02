using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to an <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRSession">XRSession</see>
/// started by <see cref="WebXr.RequestSession"/>. Dispose it to end the session and give the display
/// back.
/// </summary>
/// <remarks>
/// Butil runs the session's frame loop for you, which is what keeps a pose available to
/// <see cref="GetViewerPose"/> between calls - a pose otherwise exists only inside an XR frame
/// callback. Drawing is not part of that: <see cref="AttachCanvas"/> gives the session something to
/// present, and what gets rendered into it is WebGL code of your own.
/// </remarks>
public sealed class XrSessionHandle : IAsyncDisposable
{
    internal const string EndMethodName = nameof(InvokeXrSessionEnd);
    internal const string InputMethodName = nameof(InvokeXrInput);
    internal const string PoseMethodName = nameof(InvokeXrPose);

    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private readonly Action? _onEnd;
    private readonly Action<XrInputEvent>? _onInput;
    private readonly Action<XrPose>? _onPose;
    private DotNetObjectReference<XrSessionHandle>? _dotNetRef;
    private bool _disposed;

    internal XrSessionHandle(IJSRuntime js, Guid id, Action? onEnd, Action<XrInputEvent>? onInput, Action<XrPose>? onPose)
    {
        _js = js;
        _id = id;
        _onEnd = onEnd;
        _onInput = onInput;
        _onPose = onPose;
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    internal DotNetObjectReference<XrSessionHandle>? CallbackRef => _dotNetRef;

    internal void Initialize(XrSessionJsInfo? info)
    {
        if (info is null) return;   // prerender/SSR: no JS runtime ran, so there's nothing to record

        ReferenceSpaceType = info.ReferenceSpaceType switch
        {
            "viewer" => XrReferenceSpaceType.Viewer,
            "local" => XrReferenceSpaceType.Local,
            "bounded-floor" => XrReferenceSpaceType.BoundedFloor,
            "unbounded" => XrReferenceSpaceType.Unbounded,
            _ => XrReferenceSpaceType.LocalFloor
        };
    }

    /// <summary>The internal session id.</summary>
    public Guid Id => _id;

    /// <summary>
    /// The reference space the session actually got, which may be a fallback from the one that was
    /// asked for - poses mean different things in different spaces, so it is worth checking.
    /// </summary>
    public XrReferenceSpaceType ReferenceSpaceType { get; private set; } = XrReferenceSpaceType.LocalFloor;

    /// <summary>
    /// Gives the session a canvas to present through, by building an
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRWebGLLayer">XRWebGLLayer</see>
    /// over an XR-compatible WebGL context.
    /// </summary>
    /// <param name="canvas">A <c>&lt;canvas&gt;</c> that no other code has taken a context on yet.</param>
    /// <returns>False when the session is gone, WebGL is unavailable, or the canvas already has a non-XR context.</returns>
    /// <remarks>
    /// An immersive session with no base layer runs but shows the user nothing, so this is not
    /// optional for a VR or AR session. The canvas has to be fresh: a context that already exists
    /// cannot be made XR-compatible after the fact.
    /// <br/>
    /// Butil stops here deliberately. Drawing at headset frame rates belongs in WebGL on the JS side;
    /// what crosses into .NET is session lifecycle, poses and input.
    /// </remarks>
    public ValueTask<bool> AttachCanvas(ElementReference canvas)
        => _js.Invoke<bool>("BitButil.webXr.attachCanvas", _id, canvas);

    /// <summary>
    /// The most recent viewer pose from the session's frame loop - where the user's head is, and what
    /// each eye sees.
    /// </summary>
    /// <returns>The pose, or <c>null</c> when tracking is lost or the session has ended.</returns>
    /// <remarks>
    /// This is a snapshot taken at the last frame, not a fresh reading: it is meant for logic that
    /// runs at UI speed - a proximity check, a readout - rather than for rendering.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(XrPose))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(XrView))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(XrRigidTransform))]
    public ValueTask<XrPose?> GetViewerPose() => _js.Invoke<XrPose?>("BitButil.webXr.viewerPose", _id);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRSession/inputSources">XRSession.inputSources</see>:
    /// the controllers, hands and gaze sources the session currently knows about.
    /// </summary>
    /// <returns>The sources, or an empty array when there are none.</returns>
    /// <remarks>
    /// The list changes as the user picks controllers up and puts them down, so read it when it
    /// matters rather than caching it.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(XrInputSource))]
    public ValueTask<XrInputSource[]> GetInputSources() => _js.Invoke<XrInputSource[]>("BitButil.webXr.inputSources", _id);

    /// <summary>
    /// Invoked from JS when the session ends, however that happened. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(EndMethodName)]
    public void InvokeXrSessionEnd(Guid id)
    {
        if (id != _id) return;

        _onEnd?.Invoke();
    }

    /// <summary>
    /// Invoked from JS for each select/squeeze event. Public + <see cref="JSInvokableAttribute"/> so
    /// it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InputMethodName)]
    public void InvokeXrInput(Guid id, string type, string handedness, string targetRayMode)
    {
        if (id != _id || _onInput is null) return;

        _onInput.Invoke(new XrInputEvent(type switch
        {
            "selectstart" => XrInputEventType.SelectStart,
            "selectend" => XrInputEventType.SelectEnd,
            "squeeze" => XrInputEventType.Squeeze,
            "squeezestart" => XrInputEventType.SqueezeStart,
            "squeezeend" => XrInputEventType.SqueezeEnd,
            _ => XrInputEventType.Select
        }, handedness, targetRayMode));
    }

    /// <summary>
    /// Invoked from JS on the pose interval, when one was asked for. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(PoseMethodName)]
    public void InvokeXrPose(Guid id, XrPose pose)
    {
        if (id != _id) return;

        _onPose?.Invoke(pose);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRSession/end">XRSession.end()</see>:
    /// ends the session and gives the display back. Calling it again does nothing.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.webXr.end", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }
}
