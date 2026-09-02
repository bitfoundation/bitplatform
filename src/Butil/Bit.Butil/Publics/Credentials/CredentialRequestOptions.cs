namespace Bit.Butil;

/// <summary>
/// Which stored credentials <see cref="Credentials.Get"/> will accept, and how much the browser may
/// ask the user. Asking for neither passwords nor federated providers matches nothing.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get">CredentialsContainer.get()</see>
/// </summary>
public class CredentialRequestOptions
{
    /// <summary>Accept a stored password credential.</summary>
    public bool Password { get; set; } = true;

    /// <summary>
    /// The identity providers whose federated credentials are acceptable, as origins -
    /// <c>"https://accounts.google.com"</c>. Empty means no federated credential is accepted.
    /// </summary>
    public string[]? FederatedProviders { get; set; }

    /// <summary>
    /// The federation protocols to accept (<c>"openidconnect"</c>, and historically
    /// <c>"oidc"</c>). Rarely needed - the provider list is usually enough.
    /// </summary>
    public string[]? FederatedProtocols { get; set; }

    /// <summary>How much UI the browser may show. Defaults to <see cref="CredentialMediation.Optional"/>.</summary>
    public CredentialMediation Mediation { get; set; } = CredentialMediation.Optional;
}
