using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Media_Source_Extensions_API">Media Source Extensions</see>:
/// feeds a <c>&lt;video&gt;</c> or <c>&lt;audio&gt;</c> element with media segments the app fetches
/// itself, instead of pointing the element at a URL and letting the browser do the fetching.
/// </summary>
/// <remarks>
/// This is the foundation every adaptive player (HLS, DASH) is built on: the app decides which
/// representation to fetch next, appends the bytes to a <see cref="SourceBufferHandle"/>, and the
/// element plays what has been appended. Switching quality mid-stream, splicing ads, and buffering
/// ahead of a live edge are all consequences of owning that loop.
/// <br/>
/// The pieces fit together in a fixed order: <see cref="Open"/> attaches a media source to the
/// element and hands back a <see cref="MediaSourceHandle"/> only once the element has adopted it,
/// <see cref="MediaSourceHandle.AddSourceBuffer"/> creates one buffer per track group, and
/// <see cref="SourceBufferHandle.Append"/> feeds it. An initialization segment has to be the first
/// thing appended to a buffer; media segments after that.
/// <br/>
/// Everything here is Butil-managed: the media source, its object URL and its buffers live in JS
/// keyed by the handle's id, and disposing the handle is what releases them.
/// </remarks>
[ButilService(typeof(MediaSource))]
public class MediaSource(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>
    /// Segment types worth probing on today's engines, most broadly supported first. Chromium and
    /// Firefox take fragmented WebM and fragmented MP4; Safari only takes fragmented MP4, so a list
    /// rather than one hard-coded string is what works everywhere.
    /// </summary>
    public static readonly string[] CommonTypes =
    [
        "video/mp4;codecs=\"avc1.42E01E,mp4a.40.2\"",
        "video/mp4;codecs=\"avc1.42E01E\"",
        "video/webm;codecs=\"vp9,opus\"",
        "video/webm;codecs=\"vp8,vorbis\"",
        "audio/mp4;codecs=\"mp4a.40.2\"",
        "audio/webm;codecs=\"opus\"",
    ];

    /// <summary>True when the runtime exposes <c>MediaSource</c> or <c>ManagedMediaSource</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.mediaSource.isSupported");

    /// <summary>
    /// True when the runtime exposes
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ManagedMediaSource">ManagedMediaSource</see>,
    /// the variant iOS Safari offers - and the only Media Source Extensions on an iPhone.
    /// </summary>
    /// <remarks>
    /// A managed source lets the engine decide when the page should buffer and when it should stop,
    /// which is what makes it acceptable on a battery-powered device. Pass
    /// <c>preferManagedMediaSource: true</c> to <see cref="Open"/> to use it where it exists.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsManagedSupported() => js.Invoke<bool>("BitButil.mediaSource.isManagedSupported");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSource/isTypeSupported_static">MediaSource.isTypeSupported()</see>:
    /// true when this engine can play segments of the given container and codecs.
    /// </summary>
    /// <param name="mimeType">A container with its codecs, e.g. <c>video/mp4;codecs="avc1.42E01E"</c>. A type without codecs is answered conservatively.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsTypeSupported(string mimeType)
        => js.Invoke<bool>("BitButil.mediaSource.isTypeSupported", mimeType);

    /// <summary>
    /// Filters <paramref name="candidates"/> down to the types this engine accepts, preserving the
    /// order you passed them in - so the first entry is your best available choice.
    /// </summary>
    /// <param name="candidates">The MIME types to probe. Defaults to <see cref="CommonTypes"/>.</param>
    /// <returns>An empty array when nothing matches, or when Media Source Extensions are unavailable.</returns>
    /// <remarks>
    /// <c>isTypeSupported</c> answers whether the engine can parse the container and decode the
    /// codec, not whether playback will be smooth - ask <see cref="MediaCapabilities.DecodingInfo"/>
    /// for that.
    /// </remarks>
    public ValueTask<string[]> GetSupportedTypes(string[]? candidates = null)
        => js.Invoke<string[]>("BitButil.mediaSource.supportedTypes", (object)(candidates ?? CommonTypes));

    /// <summary>
    /// Creates a media source, attaches it to <paramref name="mediaElement"/>, and waits for the
    /// element to adopt it.
    /// </summary>
    /// <param name="mediaElement">The <c>&lt;video&gt;</c> or <c>&lt;audio&gt;</c> that will play the appended segments.</param>
    /// <param name="preferManagedMediaSource">
    /// True to use <c>ManagedMediaSource</c> where the engine has one (iOS Safari), falling back to
    /// the classic <c>MediaSource</c> where it doesn't.
    /// </param>
    /// <returns>
    /// A handle once the source is open and ready for buffers, or <c>null</c> when the API is
    /// missing, the attachment failed, or the element never got as far as opening the source.
    /// </returns>
    /// <remarks>
    /// The returned handle is already in the <see cref="MediaSourceReadyState.Open"/> state, which
    /// is the only state in which buffers can be added - so there is no event to wait for first.
    /// <br/>
    /// An element that is never given a chance to load (detached from the document, or with
    /// <c>preload="none"</c> and no play attempt) never opens its source; that case comes back as
    /// <c>null</c> after a few seconds rather than hanging.
    /// <br/>
    /// Dispose the handle when the player is torn down - it revokes the object URL, which is what
    /// lets the buffered media actually be freed.
    /// </remarks>
    public async ValueTask<MediaSourceHandle?> Open(ElementReference mediaElement, bool preferManagedMediaSource = false)
    {
        var id = Guid.NewGuid();
        var opened = await js.Invoke<bool>("BitButil.mediaSource.open", id, mediaElement, preferManagedMediaSource);

        return opened ? new MediaSourceHandle(js, id) : null;
    }

    /// <summary>
    /// On scope/circuit teardown, closes every media source whose <see cref="MediaSourceHandle"/>
    /// was never disposed, revoking its object URL and detaching it from its element.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try { await js.InvokeVoid("BitButil.mediaSource.disposeAll"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        GC.SuppressFinalize(this);
    }
}
