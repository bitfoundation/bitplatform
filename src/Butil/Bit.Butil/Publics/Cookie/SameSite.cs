namespace Bit.Butil;

/// <summary>
/// A cookie's <c>SameSite</c> attribute: how far outside its own site the browser will carry it.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Set-Cookie#samesitesamesite-value">Set-Cookie SameSite</see>
/// </summary>
public enum SameSite
{
    /// <summary>
    /// Explicitly states no restrictions will be applied.
    /// The cookie will be sent in all requests, both cross-site and same-site.
    /// Browsers reject this unless the cookie is also marked <c>Secure</c>.
    /// </summary>
    None,

    /// <summary>
    /// Send the cookie for all same-site requests and top-level navigation GET requests.
    /// </summary>
    Lax,

    /// <summary>
    /// Prevent the cookie from being sent by the browser to the target site in 
    /// all cross-site browsing contexts, even when following a regular link.
    /// </summary>
    Strict,
}
