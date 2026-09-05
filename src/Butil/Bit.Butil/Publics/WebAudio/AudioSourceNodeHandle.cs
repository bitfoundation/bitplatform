using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A node that produces sound and has to be started - an
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioScheduledSourceNode">AudioScheduledSourceNode</see>:
/// an oscillator, a buffer source, or a constant source.
/// </summary>
/// <remarks>
/// These are single-use. A source that has been started and stopped cannot be started again - build
/// a new one, which is cheap and is what the Web Audio design expects. Nothing is heard until the
/// source is both connected and started.
/// </remarks>
public sealed class AudioSourceNodeHandle : AudioNodeHandle
{
    internal AudioSourceNodeHandle(IJSRuntime js, Guid id) : base(js, id) { }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioScheduledSourceNode/start">start()</see>:
    /// begins playback, optionally at a scheduled moment.
    /// </summary>
    /// <param name="whenSeconds">How far from now to start. 0 is immediately.</param>
    /// <param name="offsetSeconds">Where in the buffer to start from. Buffer sources only.</param>
    /// <param name="durationSeconds">How much to play before stopping. 0 plays to the end.</param>
    /// <returns>False when the source was already started, or has been disposed.</returns>
    /// <remarks>
    /// Scheduling happens on the audio thread, so a sequence built with <paramref name="whenSeconds"/>
    /// stays in time in a way a .NET timer never can.
    /// </remarks>
    public ValueTask<bool> Start(double whenSeconds = 0, double offsetSeconds = 0, double durationSeconds = 0)
        => Js.Invoke<bool>("BitButil.webAudio.start", NodeId, whenSeconds, offsetSeconds, durationSeconds);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioScheduledSourceNode/stop">stop()</see>:
    /// ends playback, optionally at a scheduled moment.
    /// </summary>
    /// <param name="whenSeconds">How far from now to stop. 0 is immediately.</param>
    /// <returns>False when the source was never started, or has been disposed.</returns>
    /// <remarks>
    /// Stopping a source at full volume clicks. Ramp its gain down first and stop it a few
    /// milliseconds later.
    /// </remarks>
    public ValueTask<bool> Stop(double whenSeconds = 0)
        => Js.Invoke<bool>("BitButil.webAudio.stopNode", NodeId, whenSeconds);
}
