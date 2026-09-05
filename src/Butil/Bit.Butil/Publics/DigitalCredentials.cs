using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Digital_Credentials_API">Digital Credentials API</see>
/// (<c>navigator.credentials.get({ digital })</c>): asks the user's wallet to present a verifiable
/// credential - a mobile driving licence, an ID card, a diploma - and hands back what it signed.
/// </summary>
/// <remarks>
/// The browser mediates: it lists the wallets that hold something matching, the user picks one and
/// sees exactly which claims are being asked for, and only then is anything released. The page never
/// enumerates what the user holds, and a request the user declines is indistinguishable from having
/// nothing.
/// <br/>
/// Secure context, top-level or an iframe with <c>allow="digital-credentials-get"</c>, and a user
/// gesture. Experimental: Chromium on Android leads, the protocols are still moving, and
/// <see cref="IsProtocolSupported"/> is the check to write against rather than a version test.
/// </remarks>
[ButilService(typeof(DigitalCredentials))]
public class DigitalCredentials(IJSRuntime js)
{
    // Per-instance handle for the pending exchanges, so one circuit's Abort cannot cancel another's.
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    /// <summary>
    /// True when the runtime exposes <c>window.DigitalCredential</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DigitalCredential">DigitalCredential</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.digitalCredentials.isSupported");

    /// <summary>
    /// Whether the browser will speak an exchange protocol - <c>"openid4vp"</c>, say. The set is
    /// expected to change, which is why it is asked rather than assumed.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DigitalCredential/userAgentAllowsProtocol_static">DigitalCredential.userAgentAllowsProtocol()</see>
    /// </summary>
    public ValueTask<bool> IsProtocolSupported(string protocol)
        => js.Invoke<bool>("BitButil.digitalCredentials.isProtocolSupported", protocol);

    /// <summary>
    /// Asks the wallet to present a credential, resolving with what it signed or <c>null</c> when
    /// the user declined, no wallet matched, or the browser refused. Must be called from a
    /// user-gesture handler.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get">CredentialsContainer.get()</see>
    /// </summary>
    /// <param name="requests">
    /// The acceptable protocols and their request objects. More than one entry offers the wallet
    /// alternatives; the response says which was used.
    /// </param>
    /// <param name="mediation">
    /// How much the browser may skip. <see cref="CredentialMediation.Required"/> - the default here -
    /// always shows the wallet chooser, which is what presenting an identity document warrants.
    /// </param>
    /// <param name="cancellationToken">Cancelling it aborts the exchange, the same as <see cref="Abort"/>.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DigitalCredentialRequest))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DigitalCredentialResponse))]
    public async ValueTask<DigitalCredentialResponse?> Get(
        DigitalCredentialRequest[] requests,
        CredentialMediation mediation = CredentialMediation.Required,
        CancellationToken cancellationToken = default)
    {
        // A handle per call, not per instance: a presentation and an issuance can be in flight
        // together, and an already-cancelled token fires its abort before the get below is
        // dispatched - the JS side holds that abort against this handle alone, so this exchange
        // never opens the wallet chooser and no other call is disturbed.
        var requestId = Guid.NewGuid().ToString("N");

        using var registration = js.RegisterJsAbort(cancellationToken, "BitButil.digitalCredentials.abort", _instanceId, requestId);

        return await js.Invoke<DigitalCredentialResponse?>("BitButil.digitalCredentials.get",
            _instanceId, requestId, requests, CredentialMediations.ToName(mediation));
    }

    /// <summary>
    /// Issues a credential <em>into</em> the wallet - the other direction, used by an issuer once it
    /// has established who the user is. Returns <c>null</c> when the user declined or no wallet
    /// accepted it. Must be called from a user-gesture handler.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/create">CredentialsContainer.create()</see>
    /// </summary>
    /// <remarks>Newer than <see cref="Get"/> and thinner on the ground - check for it before offering it.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DigitalCredentialRequest))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DigitalCredentialResponse))]
    public async ValueTask<DigitalCredentialResponse?> Create(
        DigitalCredentialRequest[] requests,
        CancellationToken cancellationToken = default)
    {
        // Its own handle, for the same reasons as Get.
        var requestId = Guid.NewGuid().ToString("N");

        using var registration = js.RegisterJsAbort(cancellationToken, "BitButil.digitalCredentials.abort", _instanceId, requestId);

        return await js.Invoke<DigitalCredentialResponse?>("BitButil.digitalCredentials.create", _instanceId, requestId, requests);
    }

    /// <summary>
    /// Ends the exchanges this instance started, dismissing the wallet chooser. Returns false when
    /// nothing was pending.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AbortController/abort">AbortController.abort()</see>
    /// </summary>
    public ValueTask<bool> Abort() => js.Invoke<bool>("BitButil.digitalCredentials.abort", _instanceId, null);
}
