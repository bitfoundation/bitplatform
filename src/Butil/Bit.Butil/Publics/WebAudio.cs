using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Lightweight wrapper over the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Audio_API">Web Audio API</see>.
/// </summary>
/// <remarks>
/// The wrapper deliberately exposes only the high-traffic operations (play a buffer, play a
/// tone, master gain). Build a richer node graph in JS and call into it via interop when you
/// need granular control.
/// </remarks>
public class WebAudio(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when the runtime exposes <c>AudioContext</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.webAudio.isSupported");

    /// <summary>
    /// Resumes a suspended <c>AudioContext</c>. Mobile Safari requires this on the first user
    /// interaction; calling it from a click/touch handler unblocks subsequent playback.
    /// </summary>
    public ValueTask Resume() => js.InvokeVoid("BitButil.webAudio.resume");

    /// <summary>Suspends the shared <c>AudioContext</c>.</summary>
    public ValueTask Suspend() => js.InvokeVoid("BitButil.webAudio.suspend");

    /// <summary>Sets the master gain (in [0, 1]) applied to every Butil-managed playback.</summary>
    public ValueTask SetMasterGain(double value) => js.InvokeVoid("BitButil.webAudio.setMasterGain", value);

    /// <summary>
    /// Decodes and plays the given audio bytes. Returns a handle for stop/gain control.
    /// </summary>
    public async ValueTask<AudioPlaybackHandle> PlayBuffer(byte[] data, double startGain = 1.0, bool loop = false)
    {
        var id = Guid.NewGuid();
        await js.InvokeVoid("BitButil.webAudio.playBuffer", id, data, startGain, loop);
        return new AudioPlaybackHandle(js, id);
    }

    /// <summary>
    /// Plays a sine/triangle/square/sawtooth oscillator at the given frequency for
    /// <paramref name="durationMs"/> milliseconds. Set <paramref name="durationMs"/> to 0
    /// for an open-ended tone you stop manually.
    /// </summary>
    public async ValueTask<AudioPlaybackHandle> PlayTone(double frequency,
                                                         double durationMs = 0,
                                                         string waveform = "sine",
                                                         double startGain = 0.5)
    {
        var id = Guid.NewGuid();
        await js.InvokeVoid("BitButil.webAudio.playTone", id, frequency, durationMs, waveform, startGain);
        return new AudioPlaybackHandle(js, id);
    }

    /// <summary>
    /// Closes the underlying <c>AudioContext</c> (releasing the browser audio thread) and stops
    /// any in-flight playback. Called automatically when the scoped service is disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try { await js.InvokeVoid("BitButil.webAudio.dispose"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        GC.SuppressFinalize(this);
    }
}
