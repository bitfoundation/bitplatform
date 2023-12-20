namespace Bit.Butil;

public enum SameSite
{
    /// <summary>
    /// Explicitly states no restrictions will be applied. 
    /// The cookie will be sent in all requests—both cross-site and same-site.
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
