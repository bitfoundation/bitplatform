using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the password and federated half of the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Credential_Management_API">Credential Management API</see>
/// (<c>navigator.credentials</c>): store a sign-in in the browser's password manager, and get it
/// back on the next visit.
/// </summary>
/// <remarks>
/// The public-key half of the same container is <see cref="WebAuthn"/>, the federated-without-cookies
/// half is <see cref="FedCm"/>, and the SMS one-time-code half is <see cref="WebOtp"/>.
/// <br/>
/// Secure context only. Chromium implements <c>PasswordCredential</c> and <c>FederatedCredential</c>;
/// Firefox and Safari expose <c>navigator.credentials</c> for public-key credentials but not these,
/// so <see cref="IsPasswordSupported"/> is the check worth branching on, not <see cref="IsSupported"/>.
/// </remarks>
[ButilService(typeof(Credentials))]
public class Credentials(IJSRuntime js)
{
    /// <summary>
    /// True when the runtime exposes <c>navigator.credentials</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer">CredentialsContainer</see>
    /// </summary>
    /// <remarks>
    /// True in every current engine - it is also how WebAuthn is reached. For the password store
    /// specifically, ask <see cref="IsPasswordSupported"/>.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.credentials.isSupported");

    /// <summary>
    /// True when the runtime exposes <c>window.PasswordCredential</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PasswordCredential">PasswordCredential</see>
    /// </summary>
    public ValueTask<bool> IsPasswordSupported() => js.Invoke<bool>("BitButil.credentials.isPasswordSupported");

    /// <summary>
    /// True when the runtime exposes <c>window.FederatedCredential</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/FederatedCredential">FederatedCredential</see>
    /// </summary>
    public ValueTask<bool> IsFederatedSupported() => js.Invoke<bool>("BitButil.credentials.isFederatedSupported");

    /// <summary>
    /// Offers a username/password pair to the browser's password manager, which decides for itself
    /// whether to prompt the user to save it. Returns false when the browser refused or the API is
    /// not implemented.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/store">CredentialsContainer.store()</see>
    /// </summary>
    /// <param name="id">The account identifier - the username or email signed in with.</param>
    /// <param name="password">The password. It goes straight to the browser and is not kept here.</param>
    /// <param name="name">The display name for the account chooser.</param>
    /// <param name="iconUrl">An avatar URL for the account chooser. Must be a secure URL.</param>
    /// <remarks>
    /// Call it <em>after</em> the server has confirmed the sign-in, never before - storing a
    /// credential the server rejected teaches the password manager a wrong answer.
    /// </remarks>
    public ValueTask<bool> StorePassword(string id, string password, string? name = null, string? iconUrl = null)
        => js.Invoke<bool>("BitButil.credentials.storePassword", id, password, name, iconUrl);

    /// <summary>
    /// Records that this account signs in through an identity provider, so the account chooser can
    /// offer "continue with ..." next time. No secret is stored.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/store">CredentialsContainer.store()</see>
    /// </summary>
    /// <param name="id">The account identifier at the provider.</param>
    /// <param name="provider">The provider's origin, e.g. <c>"https://accounts.google.com"</c>.</param>
    /// <param name="name">The display name for the account chooser.</param>
    /// <param name="iconUrl">An avatar URL for the account chooser. Must be a secure URL.</param>
    /// <param name="protocol">The federation protocol, when the provider needs it named.</param>
    public ValueTask<bool> StoreFederated(string id, string provider, string? name = null, string? iconUrl = null, string? protocol = null)
        => js.Invoke<bool>("BitButil.credentials.storeFederated", id, provider, name, iconUrl, protocol);

    /// <summary>
    /// Asks the browser for a stored credential, returning <c>null</c> when there is none or the
    /// user dismissed the chooser.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get">CredentialsContainer.get()</see>
    /// </summary>
    /// <param name="options">
    /// Which credentials are acceptable. At least one kind has to be:
    /// <see cref="CredentialRequestOptions.Password"/>, or a non-empty
    /// <see cref="CredentialRequestOptions.FederatedProviders"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Neither passwords nor any federated provider was asked for. The browser would match every
    /// credential type rather than none, so the request cannot be made - and returning <c>null</c>
    /// would be indistinguishable from the user having nothing stored.
    /// </exception>
    /// <remarks>
    /// With <see cref="CredentialMediation.Silent"/> this shows no UI at all, which makes it safe to
    /// call on page load: either a credential comes back and you can sign the user in, or nothing
    /// does and the visit continues undisturbed.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CredentialInfo))]
    public ValueTask<CredentialInfo?> Get(CredentialRequestOptions? options = null)
    {
        options ??= new CredentialRequestOptions();

        if (options.Password is false && (options.FederatedProviders is null || options.FederatedProviders.Length == 0))
            throw new ArgumentException($"Set {nameof(CredentialRequestOptions.Password)} or at least one {nameof(CredentialRequestOptions.FederatedProviders)} - a request for neither matches nothing.", nameof(options));

        return js.Invoke<CredentialInfo?>("BitButil.credentials.get",
            options.Password, options.FederatedProviders, options.FederatedProtocols, CredentialMediations.ToName(options.Mediation));
    }

    /// <summary>
    /// Turns off silent access until the user next signs in deliberately. Call it on sign-out, or
    /// the next visit signs them straight back in.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/preventSilentAccess">CredentialsContainer.preventSilentAccess()</see>
    /// </summary>
    public ValueTask PreventSilentAccess() => js.InvokeVoid("BitButil.credentials.preventSilentAccess");
}
