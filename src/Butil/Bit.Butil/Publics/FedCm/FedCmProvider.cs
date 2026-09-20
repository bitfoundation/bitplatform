namespace Bit.Butil;

/// <summary>
/// One identity provider a FedCM sign-in may use. The browser fetches
/// <see cref="ConfigUrl"/> itself and drives the whole exchange - the page never sees the provider's
/// endpoints, and no third-party cookie is involved.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/IdentityCredentialRequestOptions">IdentityCredentialRequestOptions</see>
/// </summary>
public class FedCmProvider
{
    /// <summary>
    /// The provider's well-known config file, e.g.
    /// <c>"https://idp.example/fedcm.json"</c>. Must be https.
    /// </summary>
    public required string ConfigUrl { get; set; }

    /// <summary>The client identifier the provider issued to this relying party.</summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// A server-generated one-time value bound into the returned token, so a token replayed from
    /// another session can be rejected. Generate it server-side and verify it there.
    /// </summary>
    public string? Nonce { get; set; }

    /// <summary>The account to suggest, when your page already knows which one the user wants.</summary>
    public string? LoginHint { get; set; }

    /// <summary>Restricts the accounts offered to one email domain - an enterprise sign-in.</summary>
    public string? DomainHint { get; set; }

    /// <summary>
    /// The account fields to request, e.g. <c>"name"</c>, <c>"email"</c>, <c>"picture"</c>. The
    /// browser shows the user what is being asked for before it hands anything over.
    /// </summary>
    public string[]? Fields { get; set; }

    /// <summary>
    /// Extra provider-defined parameters, serialized as they stand. An anonymous object is the
    /// usual way to write one; see the note on <see cref="PaymentMethod.Data"/> about trimming.
    /// </summary>
    public object? Parameters { get; set; }
}
