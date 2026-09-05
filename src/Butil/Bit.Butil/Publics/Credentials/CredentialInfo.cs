namespace Bit.Butil;

/// <summary>
/// A credential the browser's password manager handed back, flattened across the two shapes
/// <see cref="Credentials.Get"/> can return - <see cref="Type"/> says which one it is.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Credential">Credential</see>
/// </summary>
public class CredentialInfo
{
    /// <summary><c>"password"</c> or <c>"federated"</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>The account identifier - the username or email the credential was stored under.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The display name shown in the account chooser, when one was stored.</summary>
    public string? Name { get; set; }

    /// <summary>The avatar URL stored with the credential, when there is one.</summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// The password itself - only ever set on a <c>"password"</c> credential. Post it to your sign-in
    /// endpoint and let it go; do not keep it in component state or write it anywhere.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>The identity provider's origin - only set on a <c>"federated"</c> credential.</summary>
    public string? Provider { get; set; }

    /// <summary>The federation protocol, where the credential recorded one.</summary>
    public string? Protocol { get; set; }
}
