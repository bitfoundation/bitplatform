using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/FedCM_API">FedCM</see>
/// (<c>IdentityCredential</c>): federated sign-in the browser mediates itself, with no third-party
/// cookies and no popup window.
/// </summary>
/// <remarks>
/// The browser - not the page and not an iframe - fetches the provider's config and accounts, shows
/// the account chooser, and returns a token. So the relying party never sees the provider's session,
/// which is the point: it keeps "sign in with ..." working as third-party cookies go away.
/// <br/>
/// Secure context only, and it takes work on both sides: the provider hosts
/// <c>/.well-known/web-identity</c> and the config file named by
/// <see cref="FedCmProvider.ConfigUrl"/>, and calls <see cref="SetLoginStatus"/> as its own session
/// changes. Chromium ships it; Firefox and Safari are behind flags or absent.
/// </remarks>
[ButilService(typeof(FedCm))]
public class FedCm(IJSRuntime js)
{
    /// <summary>
    /// True when the runtime exposes <c>window.IdentityCredential</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/IdentityCredential">IdentityCredential</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.fedCm.isSupported");

    /// <summary>
    /// Starts a federated sign-in, resolving with the provider's token or <c>null</c> when the user
    /// dismissed the dialog, no account was found, or the browser refused.
    /// </summary>
    /// <remarks>
    /// Needs a user gesture unless the browser chooses to auto-reauthenticate a returning user. The
    /// dialog is the browser's own, so nothing about it can be styled or scripted from the page.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get">CredentialsContainer.get()</see>
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FedCmProvider))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FedCmCredential))]
    public ValueTask<FedCmCredential?> Get(FedCmOptions options)
        => js.Invoke<FedCmCredential?>("BitButil.fedCm.get", options.Providers, options.Context, CredentialMediations.ToName(options.Mediation));

    /// <summary>
    /// Severs the connection between this relying party and one account at the provider, so the
    /// next sign-in needs fresh consent. The provider's disconnect endpoint does the work; the
    /// browser only forwards the request.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/IdentityCredential/disconnect_static">IdentityCredential.disconnect()</see>
    /// </summary>
    /// <param name="configUrl">The provider's config URL, as in <see cref="FedCmProvider.ConfigUrl"/>.</param>
    /// <param name="clientId">This relying party's client identifier.</param>
    /// <param name="accountHint">Which account to disconnect - the identifier or email the provider knows it by.</param>
    public ValueTask<bool> Disconnect(string configUrl, string clientId, string accountHint)
        => js.Invoke<bool>("BitButil.fedCm.disconnect", configUrl, clientId, accountHint);

    /// <summary>
    /// True when the runtime exposes <c>navigator.login</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/NavigatorLogin">NavigatorLogin</see>
    /// </summary>
    public ValueTask<bool> IsLoginStatusSupported() => js.Invoke<bool>("BitButil.fedCm.isLoginStatusSupported");

    /// <summary>
    /// Tells the browser whether anyone is signed in <em>here</em>. Only an identity provider's own
    /// pages call this, and they call it on every session change: the browser refuses to look for
    /// accounts at a provider it believes is logged out.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/NavigatorLogin/setStatus">NavigatorLogin.setStatus()</see>
    /// </summary>
    public ValueTask<bool> SetLoginStatus(FedCmLoginStatus status)
        => js.Invoke<bool>("BitButil.fedCm.setLoginStatus", status == FedCmLoginStatus.LoggedIn ? "logged-in" : "logged-out");
}
