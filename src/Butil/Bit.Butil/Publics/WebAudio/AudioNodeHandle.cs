using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to one node in the Web Audio graph - an
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioNode">AudioNode</see> that lives
/// in JS and is wired up from .NET.
/// </summary>
/// <remarks>
/// Nodes do nothing until they are connected: a source reaches the speakers only through a chain
/// that ends at <see cref="ConnectToDestination"/>. Everything Butil-managed ends at the shared
/// master gain rather than at the raw destination, so <see cref="WebAudio.SetMasterGain"/> ducks the
/// whole app at once.
/// <br/>
/// The continuously varying values (gain, frequency, delay time) are
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioParam">AudioParam</see>s, reached
/// by name through <see cref="SetParam"/> and <see cref="RampParam"/> - which is what keeps this one
/// handle type usable for every kind of node. The plain, non-varying ones (a filter's type, a
/// panner's model) go through <see cref="SetProperty(string, string)"/>.
/// <br/>
/// Dispose a node when it is out of the graph for good: that stops it if it is a source, and
/// disconnects it so the engine can collect it.
/// </remarks>
public class AudioNodeHandle : IAsyncDisposable
{
    private protected readonly IJSRuntime Js;
    private protected readonly Guid NodeId;
    private bool _disposed;

    internal AudioNodeHandle(IJSRuntime js, Guid id)
    {
        Js = js;
        NodeId = id;
    }

    /// <summary>The internal node id.</summary>
    public Guid Id => NodeId;

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioNode/connect">AudioNode.connect()</see>:
    /// routes this node's output into another node.
    /// </summary>
    /// <param name="destination">The node to feed.</param>
    /// <returns>False when either node is gone, or they belong to different contexts.</returns>
    /// <remarks>
    /// Connecting to several destinations is allowed and is how a signal is split - feeding an
    /// analyser alongside the speakers, for instance, costs nothing but the connection.
    /// </remarks>
    public ValueTask<bool> Connect(AudioNodeHandle destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        return Js.Invoke<bool>("BitButil.webAudio.connect", NodeId, destination.Id);
    }

    /// <summary>
    /// Connects this node to the output - through Butil's shared master gain, so
    /// <see cref="WebAudio.SetMasterGain"/> still applies.
    /// </summary>
    /// <returns>False when the node is gone, or there is no audio context.</returns>
    public ValueTask<bool> ConnectToDestination() => Js.Invoke<bool>("BitButil.webAudio.connectToDestination", NodeId);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioNode/disconnect">AudioNode.disconnect()</see>:
    /// detaches every connection this node's output makes.
    /// </summary>
    /// <returns>False when the node is gone.</returns>
    public ValueTask<bool> Disconnect() => Js.Invoke<bool>("BitButil.webAudio.disconnect", NodeId);

    /// <summary>
    /// Sets an <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioParam">AudioParam</see>
    /// by name - <c>"gain"</c>, <c>"frequency"</c>, <c>"Q"</c>, <c>"detune"</c>, <c>"delayTime"</c>,
    /// <c>"pan"</c>, <c>"positionX"</c>, and so on.
    /// </summary>
    /// <param name="name">The parameter's name, exactly as the Web Audio interface spells it.</param>
    /// <param name="value">The new value.</param>
    /// <param name="afterSeconds">
    /// Schedule the change this many seconds from now instead of applying it immediately - the
    /// sample-accurate path, which a timer in .NET cannot match.
    /// </param>
    /// <returns>False when the node has no such parameter.</returns>
    /// <remarks>
    /// Setting a gain instantly produces an audible click. Use <see cref="RampParam"/> over a few
    /// milliseconds for anything the user will hear.
    /// </remarks>
    public ValueTask<bool> SetParam(string name, double value, double afterSeconds = 0)
        => Js.Invoke<bool>("BitButil.webAudio.setParam", NodeId, name, value, afterSeconds);

    /// <summary>
    /// Moves an <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioParam">AudioParam</see>
    /// smoothly to a value over a span of time, on the audio thread.
    /// </summary>
    /// <param name="name">The parameter's name.</param>
    /// <param name="value">Where to end up.</param>
    /// <param name="overSeconds">How long the move takes.</param>
    /// <param name="exponential">
    /// True for an exponential ramp, which is how loudness and pitch are actually perceived - a
    /// linear fade to silence sounds like it stops abruptly at the end. A target of zero is nudged to
    /// a value below hearing, since an exponential ramp cannot reach zero.
    /// </param>
    /// <returns>False when the node has no such parameter.</returns>
    /// <remarks>
    /// The ramp starts from the parameter's current value and cancels anything previously scheduled,
    /// so repeated calls behave the way a fader does rather than fighting each other.
    /// </remarks>
    public ValueTask<bool> RampParam(string name, double value, double overSeconds, bool exponential = false)
        => Js.Invoke<bool>("BitButil.webAudio.rampParam", NodeId, name, value, overSeconds, exponential);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioParam/cancelScheduledValues">AudioParam.cancelScheduledValues()</see>:
    /// drops everything scheduled on a parameter from now on.
    /// </summary>
    /// <param name="name">The parameter's name.</param>
    /// <returns>False when the node has no such parameter.</returns>
    public ValueTask<bool> CancelScheduledParam(string name)
        => Js.Invoke<bool>("BitButil.webAudio.cancelScheduledParam", NodeId, name);

    /// <summary>
    /// Sets a plain string property of the node - a filter's <c>"type"</c>, a panner's
    /// <c>"panningModel"</c> or <c>"distanceModel"</c>, a wave shaper's <c>"oversample"</c>.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value, in the specification's own vocabulary.</param>
    /// <returns>False when the node is gone or rejected the value.</returns>
    public ValueTask<bool> SetProperty(string name, string value)
        => Js.Invoke<bool>("BitButil.webAudio.setProperty", NodeId, name, value);

    /// <summary>
    /// Sets a plain numeric property of the node - an analyser's <c>"fftSize"</c> or
    /// <c>"smoothingTimeConstant"</c>, a panner's <c>"refDistance"</c>.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <returns>False when the node is gone or rejected the value.</returns>
    public ValueTask<bool> SetProperty(string name, double value)
        => Js.Invoke<bool>("BitButil.webAudio.setProperty", NodeId, name, value);

    /// <summary>
    /// Sets a plain boolean property of the node - a buffer source's <c>"loop"</c>, a convolver's
    /// <c>"normalize"</c>.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <returns>False when the node is gone or rejected the value.</returns>
    public ValueTask<bool> SetProperty(string name, bool value)
        => Js.Invoke<bool>("BitButil.webAudio.setProperty", NodeId, name, value);

    /// <summary>
    /// Stops the node if it is a source, disconnects it, and drops it. Calling it again does nothing.
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await Js.InvokeVoid("BitButil.webAudio.releaseNode", NodeId); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        GC.SuppressFinalize(this);
    }
}
