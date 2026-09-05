using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// An <see href="https://developer.mozilla.org/en-US/docs/Web/API/AnalyserNode">AnalyserNode</see>:
/// reads the signal passing through it without changing it. Every level meter, spectrum bar chart
/// and oscilloscope is built on one of these.
/// </summary>
/// <remarks>
/// The node is a pass-through, so it can sit anywhere in a chain - or hang off a source as a second
/// connection that goes nowhere, which is the usual way to measure a signal without affecting it.
/// <br/>
/// Reads are snapshots of the moment they are taken: poll them from a render loop or a timer at the
/// rate the UI updates, not faster. Each read crosses the interop boundary, so a 60 Hz meter over a
/// Blazor Server circuit is a real cost - prefer a slower poll there.
/// </remarks>
public sealed class AnalyserNodeHandle : AudioNodeHandle
{
    internal AnalyserNodeHandle(IJSRuntime js, Guid id) : base(js, id) { }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AnalyserNode/getByteFrequencyData">getByteFrequencyData()</see>:
    /// the current spectrum, one byte per frequency bin.
    /// </summary>
    /// <returns>
    /// <c>fftSize / 2</c> values from 0 to 255, scaled between the analyser's minimum and maximum
    /// decibels, or <c>null</c> when the node is gone.
    /// </returns>
    /// <remarks>
    /// The bins are linear in frequency, so a bar chart drawn straight from them devotes most of its
    /// width to treble nobody notices - a logarithmic mapping is what makes a spectrum display look
    /// right.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<byte[]?> GetByteFrequencyData() => Js.Invoke<byte[]?>("BitButil.webAudio.byteFrequencyData", NodeId);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AnalyserNode/getByteTimeDomainData">getByteTimeDomainData()</see>:
    /// the current waveform, one byte per sample.
    /// </summary>
    /// <returns><c>fftSize</c> values around 128, which is silence, or <c>null</c> when the node is gone.</returns>
    /// <remarks>
    /// This is what an oscilloscope draws. It is also the cheapest level meter there is: the distance
    /// of the extremes from 128 is the amplitude.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<byte[]?> GetByteTimeDomainData() => Js.Invoke<byte[]?>("BitButil.webAudio.byteTimeDomainData", NodeId);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AnalyserNode/getFloatFrequencyData">getFloatFrequencyData()</see>:
    /// the current spectrum in decibels, unscaled.
    /// </summary>
    /// <returns><c>fftSize / 2</c> decibel values (negative, typically -100 to 0), or <c>null</c> when the node is gone.</returns>
    /// <remarks>
    /// The one to use when the numbers are the point - a measurement, a threshold - rather than a
    /// picture. It costs more than the byte version, which is already scaled for drawing.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<double[]?> GetFloatFrequencyData() => Js.Invoke<double[]?>("BitButil.webAudio.floatFrequencyData", NodeId);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AnalyserNode/fftSize">fftSize</see>:
    /// how many samples each analysis looks at.
    /// </summary>
    /// <param name="fftSize">A power of two from 32 to 32768.</param>
    /// <returns>False when the node is gone; an invalid size leaves the current one in place.</returns>
    /// <remarks>
    /// The trade is resolution against latency: a larger size resolves low frequencies better and
    /// reacts more slowly. 2048 is the usual compromise for a visualiser.
    /// </remarks>
    public ValueTask<bool> SetFftSize(int fftSize) => SetProperty("fftSize", fftSize);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AnalyserNode/smoothingTimeConstant">smoothingTimeConstant</see>:
    /// how much of the previous analysis is blended into the next one, from 0 to 1.
    /// </summary>
    /// <param name="value">0 for raw and jittery, 0.8 for the default, closer to 1 for slow and smooth.</param>
    /// <returns>False when the node is gone.</returns>
    public ValueTask<bool> SetSmoothingTimeConstant(double value) => SetProperty("smoothingTimeConstant", value);
}
