using System;

namespace Bit.Butil;

/// <summary>
/// Cookie returned by <see href="https://developer.mozilla.org/en-US/docs/Web/API/CookieStore">CookieStore</see>.
/// Unlike <see cref="ButilCookie"/>, this carries all attributes the browser knows.
/// </summary>
public class CookieStoreItem
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Path { get; set; }

    /// <summary>Expiration time. Null for session cookies.</summary>
    public DateTimeOffset? Expires { get; set; }

    public bool Secure { get; set; }

    /// <summary>One of <c>"strict"</c>, <c>"lax"</c>, <c>"none"</c>, or null.</summary>
    public string? SameSite { get; set; }

    public bool? Partitioned { get; set; }
}
