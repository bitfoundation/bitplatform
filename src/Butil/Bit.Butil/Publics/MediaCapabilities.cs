using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaCapabilities">Media Capabilities API</see>:
/// asks the engine whether a given codec, resolution and bitrate will decode (or encode) smoothly
/// and power-efficiently, before any bytes are committed to.
/// </summary>
/// <remarks>
/// This is the question <c>canPlayType()</c> never answered. <c>canPlayType()</c> says only
/// "probably", while <see cref="DecodingInfo"/> reports three separate things - supported at all,
/// smooth (no dropped frames), and power-efficient (hardware rather than CPU) - which is what an
/// adaptive player needs to pick a rung of its ladder, and what a laptop on battery needs to avoid
/// picking the wrong one.
/// <br/>
/// Every query is a full configuration: a content type <em>with</em> its codecs, plus the width,
/// height, bitrate and framerate the stream would actually use. A malformed configuration is
/// rejected by the specification rather than answered, which shows up here as <c>null</c>.
/// </remarks>
[ButilService(typeof(MediaCapabilities))]
public class MediaCapabilities(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.mediaCapabilities.decodingInfo</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.mediaCapabilities.isSupported");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaCapabilities/decodingInfo">MediaCapabilities.decodingInfo()</see>:
    /// whether this exact stream can be played, played smoothly, and played on hardware.
    /// </summary>
    /// <param name="configuration">The tracks to ask about, and how they reach the decoder.</param>
    /// <returns>
    /// The three flags, or <c>null</c> when the API is unavailable or the configuration was rejected
    /// as malformed - a missing codec in the content type, or neither a video nor an audio track.
    /// </returns>
    /// <remarks>
    /// Note that a query with <see cref="MediaDecodingConfiguration.KeySystemConfiguration"/> set can
    /// prompt the user or start a DRM component on some engines, so keep those queries to the point
    /// where the app is actually about to play protected content.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaCapabilitiesJsConfiguration))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VideoConfiguration))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AudioConfiguration))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaCapabilitiesKeySystemJsConfiguration))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaCapabilitiesInfo))]
    public ValueTask<MediaCapabilitiesInfo?> DecodingInfo(MediaDecodingConfiguration configuration)
        => js.Invoke<MediaCapabilitiesInfo?>("BitButil.mediaCapabilities.decodingInfo", configuration.ToJsObject());

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaCapabilities/encodingInfo">MediaCapabilities.encodingInfo()</see>:
    /// the same three flags for encoding - what to consult before choosing a
    /// <see cref="MediaRecorder"/> or <see cref="WebCodecs"/> configuration.
    /// </summary>
    /// <param name="configuration">The tracks to ask about, and what they are being encoded for.</param>
    /// <returns>
    /// The three flags, or <c>null</c> when the API is unavailable or the configuration was rejected
    /// as malformed.
    /// </returns>
    /// <remarks>
    /// Support is thinner than for <see cref="DecodingInfo"/> - not every engine that answers
    /// decoding questions answers encoding ones.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaCapabilitiesJsConfiguration))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VideoConfiguration))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AudioConfiguration))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaCapabilitiesInfo))]
    public ValueTask<MediaCapabilitiesInfo?> EncodingInfo(MediaEncodingConfiguration configuration)
        => js.Invoke<MediaCapabilitiesInfo?>("BitButil.mediaCapabilities.encodingInfo", configuration.ToJsObject());
}
