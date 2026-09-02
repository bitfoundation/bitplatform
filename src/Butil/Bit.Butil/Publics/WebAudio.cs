using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Audio_API">Web Audio API</see>:
/// the shared audio context, the one-line ways to make a sound, and the node graph everything
/// interesting is actually built from.
/// </summary>
/// <remarks>
/// Two levels live here. <see cref="PlayTone"/> and <see cref="PlayBuffer"/> are the shortcuts - a
/// beep, a sound effect - and need no graph at all. Everything else builds one: sources
/// (oscillators, buffers, a media element, a microphone), things that shape the signal (gain,
/// filters, delay, compression, convolution reverb, panning), and things that read it
/// (<see cref="CreateAnalyser"/>, which is what every visualiser is made of). Nodes are connected
/// with <see cref="AudioNodeHandle.Connect"/> and reach the speakers through
/// <see cref="AudioNodeHandle.ConnectToDestination"/>.
/// <br/>
/// Everything Butil-managed passes through one master gain, so <see cref="SetMasterGain"/> is an
/// app-wide volume control that needs no bookkeeping.
/// <br/>
/// The context starts suspended under every browser's autoplay policy. Call <see cref="Resume"/>
/// from a click or key handler before the first sound - a graph built and started without that
/// stays silent, which is the single most common reason "nothing plays".
/// <br/>
/// Custom per-sample processing is <see cref="CreateWorkletNode"/>: the processor is JavaScript,
/// because the audio thread cannot call into .NET, and .NET talks to it over its message port.
/// </remarks>
[ButilService(typeof(WebAudio))]
public class WebAudio(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when the runtime exposes <c>AudioContext</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.webAudio.isSupported");

    /// <summary>True when the runtime exposes <c>AudioWorkletNode</c>, needed by <see cref="CreateWorkletNode"/>.</summary>
    /// <remarks>
    /// Worklets also require a secure context - an http:// page has the constructor but cannot load a
    /// module.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsWorkletSupported() => js.Invoke<bool>("BitButil.webAudio.isWorkletSupported");

    /// <summary>
    /// Resumes a suspended <c>AudioContext</c>. Every browser creates it suspended, so this is what
    /// unblocks the first sound - and it only works from inside a user gesture such as a click.
    /// </summary>
    public ValueTask Resume() => js.InvokeVoid("BitButil.webAudio.resume");

    /// <summary>
    /// Suspends the shared <c>AudioContext</c>: the graph is kept intact, but the audio hardware is
    /// released and nothing is processed.
    /// </summary>
    public ValueTask Suspend() => js.InvokeVoid("BitButil.webAudio.suspend");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BaseAudioContext/state">AudioContext.state</see>:
    /// whether the context is actually processing audio.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async ValueTask<AudioContextState> GetState() => ToState(await js.Invoke<string>("BitButil.webAudio.state"));

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BaseAudioContext/currentTime">AudioContext.currentTime</see>
    /// in seconds - the clock every scheduled start, stop and ramp is measured against.
    /// </summary>
    /// <remarks>
    /// It only moves while the context is running, and it never goes backwards. Read it to line
    /// events up with each other, not to measure wall-clock time.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<double> GetCurrentTime() => js.Invoke<double>("BitButil.webAudio.currentTime");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BaseAudioContext/sampleRate">AudioContext.sampleRate</see>
    /// in samples per second - the device's rate, which everything decoded is resampled to.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<double> GetSampleRate() => js.Invoke<double>("BitButil.webAudio.sampleRate");

    /// <summary>Sets the master gain (in [0, 1]) applied to everything Butil-managed.</summary>
    /// <remarks>
    /// Applies to the shortcut playbacks and to every graph that ends at
    /// <see cref="AudioNodeHandle.ConnectToDestination"/> - so one call is an app-wide volume or mute.
    /// </remarks>
    public ValueTask SetMasterGain(double value) => js.InvokeVoid("BitButil.webAudio.setMasterGain", value);

    /// <summary>The current master gain.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<double> GetMasterGain() => js.Invoke<double>("BitButil.webAudio.masterGain");

    /// <summary>
    /// Decodes and plays the given audio bytes. Returns a handle for stop/gain control.
    /// </summary>
    /// <remarks>
    /// The shortcut: no graph, no handles to wire up. When the same sound is played repeatedly,
    /// decode it once with <see cref="DecodeAudioData"/> and build a
    /// <see cref="CreateBufferSource"/> per playback instead - decoding is the expensive part.
    /// </remarks>
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
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BaseAudioContext/decodeAudioData">decodeAudioData()</see>:
    /// decodes any container the browser understands into samples that can be played over and over.
    /// </summary>
    /// <param name="data">The encoded bytes - wav, mp3, ogg, m4a, whatever the engine supports.</param>
    /// <returns>The decoded buffer, or <c>null</c> when the bytes could not be decoded.</returns>
    /// <remarks>
    /// Decoding is the expensive step and the samples are large, so decode once, keep the handle, and
    /// build a cheap <see cref="CreateBufferSource"/> for each playback. That is how a game plays the
    /// same effect fifty times without fifty decodes.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AudioBufferJsInfo))]
    public async ValueTask<AudioBufferHandle?> DecodeAudioData(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var id = Guid.NewGuid();
        var info = await js.Invoke<AudioBufferJsInfo?>("BitButil.webAudio.decodeAudioData", id, data);

        return info is null ? null : new AudioBufferHandle(js, id, info.Duration, info.SampleRate, info.NumberOfChannels, info.Length);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/GainNode">GainNode</see>: a volume
    /// control, and the node to reach for whenever something needs to fade.
    /// </summary>
    /// <param name="gain">Starting gain. 1 is unchanged, 0 is silence; above 1 amplifies and can clip.</param>
    /// <returns>The node, or <c>null</c> when there is no audio context.</returns>
    /// <remarks>
    /// Fade with <see cref="AudioNodeHandle.RampParam"/> over <c>"gain"</c> rather than setting it -
    /// an instant change to a running signal is audible as a click.
    /// </remarks>
    public ValueTask<AudioNodeHandle?> CreateGain(double gain = 1)
        => CreateNode("BitButil.webAudio.createGain", id => new AudioNodeHandle(js, id), gain);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BiquadFilterNode">BiquadFilterNode</see>:
    /// one band of equalisation - a low-pass, a shelf, a notch.
    /// </summary>
    /// <param name="type">Which shape of filter.</param>
    /// <param name="frequency">The cutoff or centre frequency in hertz.</param>
    /// <param name="q">Resonance - how narrow the effect is around the frequency.</param>
    /// <param name="gain">Boost or cut in decibels. Only the shelf and peaking types use it.</param>
    /// <param name="detune">Fine offset of the frequency, in cents.</param>
    /// <returns>The node, or <c>null</c> when there is no audio context.</returns>
    /// <remarks>
    /// Chain several for a graphic equaliser; sweep <c>"frequency"</c> with
    /// <see cref="AudioNodeHandle.RampParam"/> for a filter sweep.
    /// </remarks>
    public ValueTask<AudioNodeHandle?> CreateBiquadFilter(BiquadFilterType type, double frequency, double q = 1, double gain = 0, double detune = 0)
        => CreateNode("BitButil.webAudio.createBiquadFilter", id => new AudioNodeHandle(js, id), ToName(type), frequency, q, gain, detune);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AnalyserNode">AnalyserNode</see>:
    /// reads the signal without changing it. The basis of every meter, spectrum and oscilloscope.
    /// </summary>
    /// <param name="fftSize">Samples per analysis - a power of two from 32 to 32768.</param>
    /// <param name="smoothingTimeConstant">How much of the previous reading carries over, 0 to 1.</param>
    /// <param name="minDecibels">The bottom of the range the byte spectrum is scaled to.</param>
    /// <param name="maxDecibels">The top of that range.</param>
    /// <returns>The node, or <c>null</c> when there is no audio context.</returns>
    /// <remarks>
    /// Connect a source to it as a second connection that goes nowhere else: an analyser passes audio
    /// through, so it measures without being in the way of anything.
    /// </remarks>
    public ValueTask<AnalyserNodeHandle?> CreateAnalyser(int fftSize = 2048,
                                                          double smoothingTimeConstant = 0.8,
                                                          double minDecibels = -100,
                                                          double maxDecibels = -30)
        => CreateNode("BitButil.webAudio.createAnalyser", id => new AnalyserNodeHandle(js, id), fftSize, smoothingTimeConstant, minDecibels, maxDecibels);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ConvolverNode">ConvolverNode</see>:
    /// convolution reverb - it makes the signal sound as though it were played in whatever space the
    /// impulse response was recorded in.
    /// </summary>
    /// <param name="impulseResponse">A decoded impulse response - a recording of a room's answer to a click.</param>
    /// <param name="normalize">
    /// True to scale the impulse so the reverb doesn't change the overall level. Turn it off only
    /// when the impulse is already scaled, since a raw one can be very loud.
    /// </param>
    /// <returns>The node, or <c>null</c> when there is no audio context or the buffer is gone.</returns>
    /// <remarks>
    /// The expensive node in the set - the cost grows with the length of the impulse - so a long hall
    /// reverb is normally shared by connecting several sources into one convolver rather than giving
    /// each source its own.
    /// </remarks>
    public ValueTask<AudioNodeHandle?> CreateConvolver(AudioBufferHandle impulseResponse, bool normalize = true)
    {
        ArgumentNullException.ThrowIfNull(impulseResponse);

        return CreateNode("BitButil.webAudio.createConvolver", id => new AudioNodeHandle(js, id), impulseResponse.Id, normalize);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PannerNode">PannerNode</see>:
    /// places a sound at a point in space relative to the listener.
    /// </summary>
    /// <param name="options">Where the source is, which way it faces, and how it fades with distance.</param>
    /// <returns>The node, or <c>null</c> when there is no audio context.</returns>
    /// <remarks>
    /// Move it afterwards with <see cref="AudioNodeHandle.SetParam"/> over <c>"positionX"</c>,
    /// <c>"positionY"</c> and <c>"positionZ"</c>, and move the ears with
    /// <see cref="SetListener"/>. For plain left/right, <see cref="CreateStereoPanner"/> is far
    /// cheaper.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AudioPannerJsOptions))]
    public ValueTask<AudioNodeHandle?> CreatePanner(AudioPannerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return CreateNode("BitButil.webAudio.createPanner", id => new AudioNodeHandle(js, id), options.ToJsObject());
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/StereoPannerNode">StereoPannerNode</see>:
    /// simple left/right placement.
    /// </summary>
    /// <param name="pan">-1 for hard left, 0 for centre, 1 for hard right.</param>
    /// <returns>The node, or <c>null</c> when the engine doesn't implement it.</returns>
    /// <remarks>
    /// What a mixer's pan knob does, at a fraction of the cost of a full 3D panner. Automate it with
    /// <see cref="AudioNodeHandle.RampParam"/> over <c>"pan"</c>.
    /// </remarks>
    public ValueTask<AudioNodeHandle?> CreateStereoPanner(double pan = 0)
        => CreateNode("BitButil.webAudio.createStereoPanner", id => new AudioNodeHandle(js, id), pan);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DelayNode">DelayNode</see>: holds
    /// the signal back by a set time.
    /// </summary>
    /// <param name="maxDelaySeconds">The longest delay this node will ever be asked for. It cannot be raised later.</param>
    /// <param name="delaySeconds">The delay to start with.</param>
    /// <returns>The node, or <c>null</c> when there is no audio context.</returns>
    /// <remarks>
    /// An echo is this node fed back into itself through a gain of less than 1 - which is also the
    /// one place a Web Audio graph is allowed to contain a cycle.
    /// </remarks>
    public ValueTask<AudioNodeHandle?> CreateDelay(double maxDelaySeconds = 1, double delaySeconds = 0)
        => CreateNode("BitButil.webAudio.createDelay", id => new AudioNodeHandle(js, id), maxDelaySeconds, delaySeconds);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DynamicsCompressorNode">DynamicsCompressorNode</see>:
    /// pulls loud passages down so the quiet ones can be heard.
    /// </summary>
    /// <param name="threshold">Decibels above which compression starts.</param>
    /// <param name="knee">How gradually it comes in around the threshold, in decibels.</param>
    /// <param name="ratio">How much the signal above the threshold is reduced.</param>
    /// <param name="attack">Seconds to react to a signal getting louder.</param>
    /// <param name="release">Seconds to relax once it is quieter again.</param>
    /// <returns>The node, or <c>null</c> when there is no audio context.</returns>
    /// <remarks>
    /// Also the standard guard against clipping when several sources are mixed together: put one
    /// before the destination and loud moments distort far less.
    /// </remarks>
    public ValueTask<AudioNodeHandle?> CreateDynamicsCompressor(double threshold = -24,
                                                                 double knee = 30,
                                                                 double ratio = 12,
                                                                 double attack = 0.003,
                                                                 double release = 0.25)
        => CreateNode("BitButil.webAudio.createDynamicsCompressor", id => new AudioNodeHandle(js, id), threshold, knee, ratio, attack, release);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/WaveShaperNode">WaveShaperNode</see>:
    /// maps every sample through a curve - distortion, saturation, bit-crushing.
    /// </summary>
    /// <param name="curve">The transfer curve, sampled across the input range from -1 to 1.</param>
    /// <param name="oversample">
    /// <c>"none"</c>, <c>"2x"</c> or <c>"4x"</c>. Oversampling costs CPU and removes the aliasing that
    /// otherwise makes hard distortion sound harsh.
    /// </param>
    /// <returns>The node, or <c>null</c> when there is no audio context.</returns>
    public ValueTask<AudioNodeHandle?> CreateWaveShaper(double[] curve, string oversample = "none")
    {
        ArgumentNullException.ThrowIfNull(curve);

        return CreateNode("BitButil.webAudio.createWaveShaper", id => new AudioNodeHandle(js, id), curve, oversample);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/OscillatorNode">OscillatorNode</see>:
    /// a tone generator.
    /// </summary>
    /// <param name="type">The waveform.</param>
    /// <param name="frequency">Frequency in hertz.</param>
    /// <param name="detune">Fine offset in cents.</param>
    /// <returns>The source, or <c>null</c> when there is no audio context.</returns>
    /// <remarks>
    /// Connect it, then <see cref="AudioSourceNodeHandle.Start"/> it. A stopped oscillator cannot be
    /// restarted - build another, which costs almost nothing.
    /// </remarks>
    public ValueTask<AudioSourceNodeHandle?> CreateOscillator(AudioOscillatorType type, double frequency, double detune = 0)
        => CreateNode("BitButil.webAudio.createOscillator", id => new AudioSourceNodeHandle(js, id), ToName(type), frequency, detune);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioBufferSourceNode">AudioBufferSourceNode</see>:
    /// plays a decoded buffer.
    /// </summary>
    /// <param name="buffer">The samples to play, from <see cref="DecodeAudioData"/>.</param>
    /// <param name="loop">True to repeat until stopped.</param>
    /// <param name="loopStartSeconds">Where the loop starts. 0 uses the beginning of the buffer.</param>
    /// <param name="loopEndSeconds">Where the loop ends. 0 uses the end of the buffer.</param>
    /// <param name="playbackRate">Speed, which also changes pitch. 1 is normal.</param>
    /// <param name="detune">Fine pitch offset in cents, where the engine supports it.</param>
    /// <returns>The source, or <c>null</c> when there is no audio context or the buffer is gone.</returns>
    /// <remarks>
    /// A source is single-use: one per playback, over as many sources as you like sharing one buffer.
    /// </remarks>
    public ValueTask<AudioSourceNodeHandle?> CreateBufferSource(AudioBufferHandle buffer,
                                                                 bool loop = false,
                                                                 double loopStartSeconds = 0,
                                                                 double loopEndSeconds = 0,
                                                                 double playbackRate = 1,
                                                                 double detune = 0)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        return CreateNode("BitButil.webAudio.createBufferSource", id => new AudioSourceNodeHandle(js, id),
                          buffer.Id, loop, loopStartSeconds, loopEndSeconds, playbackRate, detune);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ConstantSourceNode">ConstantSourceNode</see>:
    /// emits a steady value rather than a sound.
    /// </summary>
    /// <param name="offset">The value to emit.</param>
    /// <returns>The source, or <c>null</c> when the engine doesn't implement it.</returns>
    /// <remarks>
    /// Its use is driving several AudioParams from one place: connect it to each of them and they
    /// move together, sample-accurately, from a single ramp.
    /// </remarks>
    public ValueTask<AudioSourceNodeHandle?> CreateConstantSource(double offset = 1)
        => CreateNode("BitButil.webAudio.createConstantSource", id => new AudioSourceNodeHandle(js, id), offset);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaElementAudioSourceNode">MediaElementAudioSourceNode</see>:
    /// takes over the audio of a <c>&lt;video&gt;</c> or <c>&lt;audio&gt;</c> element so it can be
    /// filtered, analysed or panned.
    /// </summary>
    /// <param name="mediaElement">The element whose audio to route.</param>
    /// <returns>The node, or <c>null</c> when the element already has a source node, or there is no audio context.</returns>
    /// <remarks>
    /// Two consequences worth knowing. The element's audio now reaches the speakers <em>only</em>
    /// through this graph, so a node chain that isn't connected to the destination silences it. And
    /// an element can be the source of exactly one node, ever - a second attempt fails rather than
    /// replacing the first.
    /// <br/>
    /// The element's media must be same-origin or properly CORS-enabled; a cross-origin file is
    /// routed as silence rather than refused.
    /// </remarks>
    public ValueTask<AudioNodeHandle?> CreateMediaElementSource(ElementReference mediaElement)
        => CreateNode("BitButil.webAudio.createMediaElementSource", id => new AudioNodeHandle(js, id), mediaElement);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaStreamAudioSourceNode">MediaStreamAudioSourceNode</see>:
    /// feeds a live stream - a microphone, a screen share's audio - into the graph.
    /// </summary>
    /// <param name="stream">A stream from <see cref="MediaDevices.GetUserMedia"/> or <see cref="MediaDevices.GetDisplayMedia"/>.</param>
    /// <returns>The node, or <c>null</c> when the stream is gone or has no audio track.</returns>
    /// <remarks>
    /// What a level meter, a noise gate or a live effect on a microphone is built on. Don't connect
    /// a microphone to the destination without thinking: on speakers, that is feedback.
    /// </remarks>
    public ValueTask<AudioNodeHandle?> CreateMediaStreamSource(MediaStreamHandle stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return CreateNode("BitButil.webAudio.createMediaStreamSource", id => new AudioNodeHandle(js, id), stream.Id);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaStreamAudioDestinationNode">MediaStreamAudioDestinationNode</see>:
    /// ends a graph in a <c>MediaStream</c> instead of the speakers.
    /// </summary>
    /// <returns>The node, or <c>null</c> when there is no audio context.</returns>
    /// <remarks>
    /// The way processed audio leaves Web Audio: hand
    /// <see cref="MediaStreamAudioDestinationHandle.GetStream"/> to <see cref="MediaRecorder"/> to
    /// record the graph's output, or attach it to an element to play it.
    /// </remarks>
    public async ValueTask<MediaStreamAudioDestinationHandle?> CreateMediaStreamDestination()
    {
        var id = Guid.NewGuid();
        var streamId = Guid.NewGuid();
        var created = await js.Invoke<bool>("BitButil.webAudio.createMediaStreamDestination", id, streamId);

        return created ? new MediaStreamAudioDestinationHandle(js, id, streamId) : null;
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Worklet/addModule">audioWorklet.addModule()</see>:
    /// loads a JavaScript module that registers one or more audio worklet processors.
    /// </summary>
    /// <param name="moduleUrl">URL of the processor module, e.g. <c>"js/my-processor.js"</c>.</param>
    /// <returns>False when worklets are unavailable, the file could not be loaded, or the module threw while registering.</returns>
    /// <remarks>
    /// Needs a secure context. The module has to be loaded before
    /// <see cref="CreateWorkletNode"/> can name a processor from it, and loading the same module
    /// twice is harmless.
    /// </remarks>
    public ValueTask<bool> AddWorkletModule(string moduleUrl)
        => js.Invoke<bool>("BitButil.webAudio.addModule", moduleUrl);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioWorkletNode">AudioWorkletNode</see>:
    /// puts your own DSP code, running on the audio thread, into the graph.
    /// </summary>
    /// <param name="processorName">The name the processor registered itself under, in a module loaded by <see cref="AddWorkletModule"/>.</param>
    /// <param name="options">Channel counts, initial parameter values, and anything the processor's constructor needs.</param>
    /// <param name="onMessage">
    /// Called with each message the processor posts back. Messages that aren't strings arrive as
    /// their JSON text.
    /// </param>
    /// <returns>The node, or <c>null</c> when no processor is registered under that name.</returns>
    /// <remarks>
    /// The processor is JavaScript by necessity: the audio thread cannot call into .NET, and anything
    /// that blocks it is an audible dropout. .NET drives it through parameters
    /// (<see cref="AudioNodeHandle.SetParam"/>, sample-accurate) and messages
    /// (<see cref="AudioWorkletNodeHandle.PostMessage"/>, whenever the thread gets to them).
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AudioWorkletNodeOptions))]
    public async ValueTask<AudioWorkletNodeHandle?> CreateWorkletNode(string processorName,
                                                                      AudioWorkletNodeOptions? options = null,
                                                                      Action<string>? onMessage = null)
    {
        var id = Guid.NewGuid();
        var handle = new AudioWorkletNodeHandle(js, id, onMessage);

        var created = await js.Invoke<bool>("BitButil.webAudio.createWorkletNode",
                                            id, processorName, options ?? new AudioWorkletNodeOptions(),
                                            handle.CallbackRef, AudioWorkletNodeHandle.MessageMethodName);
        if (created is false)
        {
            await handle.DisposeAsync();
            return null;
        }

        return handle;
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioListener">AudioListener</see>:
    /// where the ears are and which way they face, which is what every
    /// <see cref="CreatePanner"/> is measured against.
    /// </summary>
    /// <param name="x">Listener position, x.</param>
    /// <param name="y">Listener position, y.</param>
    /// <param name="z">Listener position, z.</param>
    /// <param name="forwardX">Facing direction, x. Defaults to looking along negative z.</param>
    /// <param name="forwardY">Facing direction, y.</param>
    /// <param name="forwardZ">Facing direction, z.</param>
    /// <param name="upX">Up direction, x.</param>
    /// <param name="upY">Up direction, y.</param>
    /// <param name="upZ">Up direction, z.</param>
    /// <returns>False when there is no audio context.</returns>
    /// <remarks>
    /// Only the relationship between listener and panners matters, so a scene can equally well move
    /// the listener or move the sources. Butil uses whichever listener interface the engine has - the
    /// AudioParam one where it exists, the older setter pair otherwise.
    /// </remarks>
    public ValueTask<bool> SetListener(double x, double y, double z,
                                       double forwardX = 0, double forwardY = 0, double forwardZ = -1,
                                       double upX = 0, double upY = 1, double upZ = 0)
        => js.Invoke<bool>("BitButil.webAudio.setListener", x, y, z, forwardX, forwardY, forwardZ, upX, upY, upZ);

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

    // Every node factory follows the same shape: a fresh id, a JS call that answers whether the node
    // was built, and a typed handle over that id. Interop identifiers stay literal at each call site
    // (the publish-time script trimmer reads them), so the identifier is passed in rather than built.
    private async ValueTask<THandle?> CreateNode<THandle>(string identifier, Func<Guid, THandle> factory, params object?[] args)
        where THandle : AudioNodeHandle
    {
        var id = Guid.NewGuid();
        var call = new object?[args.Length + 1];
        call[0] = id;
        args.CopyTo(call, 1);

        var created = await js.Invoke<bool>(identifier, call);

        return created ? factory(id) : null;
    }

    private static AudioContextState ToState(string? raw) => raw switch
    {
        "running" => AudioContextState.Running,
        "closed" => AudioContextState.Closed,
        _ => AudioContextState.Suspended
    };

    private static string ToName(BiquadFilterType type) => type switch
    {
        BiquadFilterType.Highpass => "highpass",
        BiquadFilterType.Bandpass => "bandpass",
        BiquadFilterType.Lowshelf => "lowshelf",
        BiquadFilterType.Highshelf => "highshelf",
        BiquadFilterType.Peaking => "peaking",
        BiquadFilterType.Notch => "notch",
        BiquadFilterType.Allpass => "allpass",
        _ => "lowpass"
    };

    private static string ToName(AudioOscillatorType type) => type switch
    {
        AudioOscillatorType.Square => "square",
        AudioOscillatorType.Sawtooth => "sawtooth",
        AudioOscillatorType.Triangle => "triangle",
        _ => "sine"
    };
}
