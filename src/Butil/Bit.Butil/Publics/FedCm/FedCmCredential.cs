namespace Bit.Butil;

/// <summary>
/// The result of a FedCM sign-in: a token minted by the identity provider for this relying party.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/IdentityCredential">IdentityCredential</see>
/// </summary>
public class FedCmCredential
{
    /// <summary>The account identifier the provider returned, where it sets one.</summary>
    public string? Id { get; set; }

    /// <summary>
    /// The provider's token - usually a signed JWT. It is a bearer credential: send it to your
    /// server and validate it there against the provider's keys and the nonce you issued. A
    /// client-side reading of it proves nothing.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// True when the browser signed the user in without asking, because they had granted this
    /// provider before. A page that treats an auto sign-in as a fresh consent gets consent wrong.
    /// </summary>
    public bool IsAutoSelected { get; set; }

    /// <summary>
    /// The <see cref="FedCmProvider.ConfigUrl"/> the token came from, where the browser reports it.
    /// Null when it does not - it is not inferred from the request, which would name the wrong
    /// provider as soon as more than one was offered.
    /// </summary>
    public string? ConfigUrl { get; set; }
}
