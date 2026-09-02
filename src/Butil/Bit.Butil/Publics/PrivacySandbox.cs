using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the browser-side halves of the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Privacy_Sandbox">Privacy Sandbox</see>:
/// the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Topics_API">Topics API</see>,
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Attribution_Reporting_API">Attribution Reporting</see>,
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Private_State_Token_API">Private State Tokens</see>
/// and <see href="https://developer.mozilla.org/en-US/docs/Web/API/Fenced_frame_API">fenced frames</see>.
/// </summary>
/// <remarks>
/// These are the replacements proposed for what third-party cookies used to do - interest signals,
/// conversion measurement, anti-fraud - designed so that the browser, not the site, holds the data.
/// Four separate APIs rather than one, grouped here because they are used together and none of them
/// is large enough on its own.
/// <br/>
/// Each needs more than a call to work: a permissions policy on the document, response headers from
/// the server, and for Attribution Reporting and Private State Tokens an issuer or reporting origin
/// that has been registered with the browser vendor. The calls here are the client-side half; the
/// rest is configuration you arrange elsewhere.
/// <br/>
/// Chromium-only, and genuinely volatile: shapes have changed between releases and parts have been
/// deprecated outright. Feature-detect every time, expect empty results, and don't make anything
/// depend on these.
/// </remarks>
[ButilService(typeof(PrivacySandbox))]
public class PrivacySandbox(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>document.browsingTopics</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsTopicsSupported() => js.Invoke<bool>("BitButil.privacySandbox.isTopicsSupported");

    /// <summary>
    /// The topics the browser will share about this user with this caller - at most three, and only
    /// ones it has already seen this caller present for.
    /// </summary>
    /// <param name="skipObservation">
    /// When true, reads the topics without recording this page visit as an observation. Pass true
    /// when you are only inspecting - observing is what earns the right to be told topics later, so
    /// it should follow from a real ad request rather than from a debug read.
    /// </param>
    /// <returns>
    /// The topics, or an empty array - which is the normal answer for a caller the browser has no
    /// observations for, as well as when the permissions policy blocks the call or the user turned
    /// the feature off.
    /// </returns>
    /// <remarks>
    /// Requires the <c>browsing-topics</c> permissions policy on the document, and a secure context.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BrowsingTopic))]
    public ValueTask<BrowsingTopic[]> GetTopics(bool skipObservation = false)
        => js.Invoke<BrowsingTopic[]>("BitButil.privacySandbox.getTopics", skipObservation);

    /// <summary>True when the runtime supports Attribution Reporting registrations.</summary>
    public ValueTask<bool> IsAttributionReportingSupported() => js.Invoke<bool>("BitButil.privacySandbox.isAttributionReportingSupported");

    /// <summary>
    /// Registers an attribution source or trigger by fetching a URL whose response carries the
    /// registration headers.
    /// </summary>
    /// <param name="url">
    /// The reporting origin's endpoint. The registration is in its response headers
    /// (<c>Attribution-Reporting-Register-Source</c> / <c>-Register-Trigger</c>), not in its body -
    /// nothing is returned to your code, and the browser stores what it read.
    /// </param>
    /// <param name="eventSourceEligible">Register a source: an impression or a click that may later convert.</param>
    /// <param name="triggerEligible">Register a trigger: the conversion itself.</param>
    /// <returns>False when the request failed outright. True only means the request completed - whether the browser accepted the registration is not observable from here, by design.</returns>
    /// <remarks>
    /// Requires the <c>attribution-reporting</c> permissions policy, and a reporting origin the
    /// browser has enrolled.
    /// </remarks>
    public ValueTask<bool> RegisterAttribution(string url, bool eventSourceEligible = true, bool triggerEligible = false)
        => js.Invoke<bool>("BitButil.privacySandbox.registerSource", url, eventSourceEligible, triggerEligible);

    /// <summary>
    /// Registers an attribution source the way an ad creative does: through an image whose response
    /// carries the registration headers. The image is never displayed.
    /// </summary>
    /// <param name="url">The reporting origin's endpoint.</param>
    /// <returns>False when the runtime has no <c>attributionSrc</c> support.</returns>
    public ValueTask<bool> RegisterAttributionImage(string url)
        => js.Invoke<bool>("BitButil.privacySandbox.registerSourceImage", url);

    /// <summary>True when the runtime exposes <c>document.hasPrivateToken</c>.</summary>
    public ValueTask<bool> IsPrivateStateTokensSupported() => js.Invoke<bool>("BitButil.privacySandbox.isPrivateStateTokensSupported");

    /// <summary>
    /// Whether this browser is holding any unspent token from an issuer - the "do I need to run the
    /// challenge again" check.
    /// </summary>
    /// <param name="issuer">The issuer's origin, e.g. <c>"https://issuer.example"</c>.</param>
    public ValueTask<bool> HasPrivateToken(string issuer)
        => js.Invoke<bool>("BitButil.privacySandbox.hasPrivateToken", issuer);

    /// <summary>
    /// Whether this browser already holds a redemption record from an issuer for this site - meaning
    /// the user has been vouched for here and nothing more needs to be spent.
    /// </summary>
    /// <param name="issuer">The issuer's origin.</param>
    public ValueTask<bool> HasRedemptionRecord(string issuer)
        => js.Invoke<bool>("BitButil.privacySandbox.hasRedemptionRecord", issuer);

    /// <summary>
    /// Runs one Private State Token operation as a fetch: asking for tokens, spending one, or
    /// attaching the resulting record to a request.
    /// </summary>
    /// <param name="url">The issuer endpoint to fetch.</param>
    /// <param name="operation">Which step of the token lifecycle this is.</param>
    /// <param name="version">The protocol version. 1 unless the issuer says otherwise.</param>
    /// <returns>False when the request failed. As with attribution, the token exchange itself is deliberately not observable from script.</returns>
    public ValueTask<bool> RequestToken(string url, PrivateStateTokenOperation operation, int version = 1)
        => js.Invoke<bool>("BitButil.privacySandbox.requestToken", url, ToName(operation), version);

    /// <summary>True when the runtime supports <c>&lt;fencedframe&gt;</c>.</summary>
    public ValueTask<bool> IsFencedFrameSupported() => js.Invoke<bool>("BitButil.privacySandbox.isFencedFrameSupported");

    /// <summary>
    /// Whether this document is itself running inside a fenced frame.
    /// </summary>
    /// <remarks>
    /// Worth knowing rather than assuming: inside one, storage is partitioned differently, there is
    /// no access to the embedder, navigation is restricted and the referrer is gone - so code that
    /// takes any of those for granted has to behave differently there.
    /// </remarks>
    public ValueTask<bool> IsInFencedFrame() => js.Invoke<bool>("BitButil.privacySandbox.isInFencedFrame");

    private static string ToName(PrivateStateTokenOperation operation) => operation switch
    {
        PrivateStateTokenOperation.TokenRedemption => "token-redemption",
        PrivateStateTokenOperation.SendRedemptionRecord => "send-redemption-record",
        _ => "token-request",
    };
}
