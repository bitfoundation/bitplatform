namespace Bit.Butil;

/// <summary>
/// One batch of cookie changes, as reported by the CookieStore <c>change</c> event.
/// </summary>
/// <remarks>
/// Overwriting a cookie shows up as a delete followed by a set, so both lists can be non-empty in
/// the same notification.
/// </remarks>
public class CookieStoreChange
{
    /// <summary>Cookies that were written or updated.</summary>
    public CookieStoreItem[] Changed { get; set; } = [];

    /// <summary>
    /// Cookies that were removed - by deletion or expiry. Only <c>Name</c> and the path/domain that
    /// identify the cookie are meaningful here; the value is blanked by the browser.
    /// </summary>
    public CookieStoreItem[] Deleted { get; set; } = [];
}
