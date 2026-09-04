using System;

namespace Bit.Butil;

/// <summary>
/// Cookie returned by <see href="https://developer.mozilla.org/en-US/docs/Web/API/CookieStore">CookieStore</see>.
/// Unlike <see cref="ButilCookie"/>, this carries all attributes the browser knows.
/// </summary>
public class CookieStoreItem
{
    /// <summary>The cookie's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The cookie's value.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>The domain the cookie is scoped to, or null when it is host-only.</summary>
    public string? Domain { get; set; }

    /// <summary>The path prefix the cookie is sent for.</summary>
    public string? Path { get; set; }

    /// <summary>Expiration time. Null for session cookies.</summary>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>Whether the cookie is sent over HTTPS only.</summary>
    public bool Secure { get; set; }

    /// <summary>One of <c>"strict"</c>, <c>"lax"</c>, <c>"none"</c>, or null.</summary>
    public string? SameSite { get; set; }

    /// <summary>
    /// Whether the cookie is partitioned by top-level site (CHIPS). Null in engines that do not
    /// report the attribute.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/Privacy/Guides/Privacy_sandbox/Partitioned_cookies">Cookies Having Independent Partitioned State</see>
    /// </summary>
    public bool? Partitioned { get; set; }
}
