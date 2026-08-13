using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Butil;

/// <summary>
/// Controls an <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element from C# -
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLMediaElement">HTMLMediaElement</see>.
/// </summary>
/// <remarks>
/// Blazor can render a media element and bind its events, but playback is imperative: there is no
/// markup for "play now" or "seek to 30 seconds". These extensions fill that gap for any
/// <see cref="ElementReference"/> pointing at a media element.
/// <br/>
/// Every method is a no-op (or returns a falsy result) when the reference isn't a media element,
/// so a stale <see cref="ElementReference"/> can't throw its way out of an event handler.
/// </remarks>
public static class ElementReferenceMediaExtensions
{
    /// <summary>
    /// Starts playback.
    /// </summary>
    /// <param name="element">An <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element.</param>
    /// <returns>
    /// False when the browser refused - autoplay blocked outside a user gesture, or a source it
    /// can't decode. Refusal is a normal outcome here, so it is reported rather than thrown.
    /// </returns>
    /// <remarks>
    /// A muted video is allowed to autoplay on most engines; an unmuted one generally is not
    /// without a gesture.
    /// </remarks>
    public static ValueTask<bool> Play(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.mediaElement.play", element);

    /// <summary>Pauses playback. No-op when already paused.</summary>
    /// <param name="element">An <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element.</param>
    public static ValueTask Pause(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.mediaElement.pause", element);

    /// <summary>
    /// Resets the element and reloads its source - what you call after changing <c>src</c> by hand.
    /// </summary>
    /// <param name="element">An <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element.</param>
    public static ValueTask Load(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.mediaElement.load", element);

    /// <summary>
    /// Reads the element's whole playback state in one round trip.
    /// </summary>
    /// <param name="element">An <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element.</param>
    /// <returns>The state, or null when the reference isn't a media element.</returns>
    /// <remarks>
    /// One call rather than a property each, because every read is an interop hop - polling a
    /// dozen properties individually to draw a transport bar is a dozen messages per frame.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaElementState))]
    public static ValueTask<MediaElementState?> GetMediaState(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<MediaElementState?>("BitButil.mediaElement.getState", element);

    /// <summary>
    /// Seeks to a position.
    /// </summary>
    /// <param name="element">An <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element.</param>
    /// <param name="seconds">The target position in seconds.</param>
    /// <returns>False when the media isn't seekable yet - typically because its metadata hasn't loaded.</returns>
    public static ValueTask<bool> SetCurrentTime(this ElementReference element, double seconds)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.mediaElement.setCurrentTime", element, seconds);

    /// <summary>
    /// Sets the volume.
    /// </summary>
    /// <param name="element">An <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element.</param>
    /// <param name="volume">0 (silent) to 1 (full). Values outside the range are clamped rather than rejected.</param>
    /// <remarks>
    /// iOS ignores this entirely - volume there is a hardware control. Use
    /// <see cref="SetMuted"/> for a mute toggle that works everywhere.
    /// </remarks>
    public static ValueTask SetVolume(this ElementReference element, double volume)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.mediaElement.setVolume", element, volume);

    /// <summary>Mutes or unmutes, without changing the volume.</summary>
    /// <param name="element">An <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element.</param>
    /// <param name="muted">True to mute.</param>
    public static ValueTask SetMuted(this ElementReference element, bool muted)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.mediaElement.setMuted", element, muted);

    /// <summary>Turns looping on or off.</summary>
    /// <param name="element">An <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element.</param>
    /// <param name="loop">True to restart on reaching the end.</param>
    public static ValueTask SetLoop(this ElementReference element, bool loop)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.mediaElement.setLoop", element, loop);

    /// <summary>
    /// Sets the playback speed.
    /// </summary>
    /// <param name="element">An <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element.</param>
    /// <param name="rate">1 is normal speed; 0.5 is half, 2 is double. Negative rates are not widely supported.</param>
    /// <returns>False when the engine rejected the rate as outside what it can resample.</returns>
    public static ValueTask<bool> SetPlaybackRate(this ElementReference element, double rate)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.mediaElement.setPlaybackRate", element, rate);

    /// <summary>
    /// Points the element at a new source. Follow with <see cref="Load"/> to make it take effect
    /// immediately rather than at the next natural reload.
    /// </summary>
    /// <param name="element">An <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element.</param>
    /// <param name="src">A URL - including a <c>blob:</c> one from <see cref="ObjectUrls"/> or a finished recording.</param>
    public static ValueTask SetMediaSource(this ElementReference element, string src)
        => ElementReferenceExtensions.GetRuntime(element).InvokeVoid("BitButil.mediaElement.setSrc", element, src);

    /// <summary>
    /// Asks whether the browser thinks it can play a given type.
    /// </summary>
    /// <param name="element">An <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element.</param>
    /// <param name="mimeType">A container, optionally with codecs, e.g. <c>"video/webm;codecs=vp9"</c>.</param>
    /// <returns>
    /// The browser's own three-valued answer, kept as-is: <c>"probably"</c>, <c>"maybe"</c>, or the
    /// empty string for no. It is deliberately not a bool - <c>"maybe"</c> means the browser can't
    /// tell without trying.
    /// </returns>
    public static ValueTask<string> CanPlayType(this ElementReference element, string mimeType)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string>("BitButil.mediaElement.canPlayType", element, mimeType);
}
