using System;
using System.Globalization;
using System.Text;

namespace Bit.Butil;

/// <summary>
/// One <c>document.cookie</c> entry, and the attributes it is written with.
/// <para>
/// Only <see cref="Name"/> and <see cref="Value"/> ever come back from a read: <c>document.cookie</c>
/// hands out nothing but name/value pairs, so a cookie produced by <see cref="Parse"/> has every
/// other member at its default. The rest exist for the write side, where <see cref="ToString"/>
/// renders them into a <c>Set-Cookie</c>-shaped string. Read
/// <see cref="CookieStore"/> instead when the attributes matter.
/// </para>
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/cookie">Document.cookie</see>
/// </summary>
public class ButilCookie
{
    /// <summary>The cookie's name. Percent-encoded on write, decoded on <see cref="Parse"/>.</summary>
    public string? Name { get; set; }

    /// <summary>The cookie's value. Percent-encoded on write, decoded on <see cref="Parse"/>.</summary>
    public string? Value { get; set; }

    /// <summary>
    /// The domain the cookie is sent to, subdomains included. Omitting it - the default - scopes
    /// the cookie to the exact host that set it, which is the narrower choice.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// When the cookie expires, written as an RFC 1123 date. <c>null</c> makes it a session cookie.
    /// A time in the past is how a cookie is deleted.
    /// </summary>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// Lifetime in seconds from now. Takes precedence over <see cref="Expires"/> wherever both are
    /// set, and unlike it, does not depend on the client's clock being right.
    /// </summary>
    public long? MaxAge { get; set; }

    /// <summary>
    /// Store the cookie under a partition key of the top-level site (CHIPS), so a third-party
    /// cookie survives third-party cookie blocking. Requires <see cref="Secure"/>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/Privacy/Guides/Privacy_sandbox/Partitioned_cookies">Cookies Having Independent Partitioned State</see>
    /// </summary>
    public bool Partitioned { get; set; }

    /// <summary>The path prefix the cookie is sent for. Defaults to the current path, not to <c>/</c>.</summary>
    public string? Path { get; set; }

    /// <summary>
    /// How far outside its own site the browser carries the cookie. <c>null</c> leaves the attribute
    /// off, which browsers now treat as <see cref="Bit.Butil.SameSite.Lax"/>.
    /// </summary>
    public SameSite? SameSite { get; set; }

    /// <summary>
    /// Send the cookie over HTTPS only. Required by <see cref="Partitioned"/> and by
    /// <see cref="Bit.Butil.SameSite.None"/>.
    /// </summary>
    public bool Secure { get; set; }

    /// <summary>
    /// Renders this cookie into the string <c>document.cookie</c> is assigned - name and value
    /// percent-encoded, then each attribute that is set. Returns an empty string when
    /// <see cref="Name"/> is empty.
    /// </summary>
    public override string ToString()
    {
        if (string.IsNullOrEmpty(Name)) return string.Empty;

        var sb = new StringBuilder();

        // Per RFC 6265, name and value must be encoded so that reserved characters
        // (=, ;, ,, whitespace, non-ASCII) don't break the cookie. Uri.EscapeDataString matches
        // the browser's encodeURIComponent semantics (e.g. space -> %20, not '+'), so cookies
        // round-trip correctly with values written/read by JS or the server.
        sb.Append(Uri.EscapeDataString(Name));
        sb.Append('=');
        if (Value is not null)
        {
            sb.Append(Uri.EscapeDataString(Value));
        }

        if (Domain is not null)
        {
            sb.Append(";domain=").Append(ValidateAttribute(Domain, nameof(Domain)));
        }

        if (Expires is not null)
        {
            // RFC 1123 / RFC 7231 IMF-fixdate: e.g. "Wed, 21 Oct 2015 07:28:00 GMT".
            sb.Append(";expires=")
              .Append(Expires.Value.UtcDateTime.ToString("R", CultureInfo.InvariantCulture));
        }

        if (MaxAge is not null)
        {
            sb.Append(";max-age=").Append(MaxAge.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (Partitioned)
        {
            sb.Append(";partitioned");
        }

        if (Path is not null)
        {
            sb.Append(";path=").Append(ValidateAttribute(Path, nameof(Path)));
        }

        if (SameSite is not null)
        {
            sb.Append(";samesite=").Append(SameSite.ToString()!.ToLowerInvariant());
        }

        if (Secure)
        {
            sb.Append(";secure");
        }

        return sb.ToString();
    }

    private static string ValidateAttribute(string value, string attributeName)
    {
        // Name and value are percent-encoded, but attributes like domain/path are appended
        // verbatim. Reject the separators (';' splits attributes, CR/LF could inject headers)
        // so a caller-supplied value can't smuggle extra cookie attributes.
        if (value.IndexOfAny([';', '\r', '\n']) >= 0)
            throw new FormatException(
                $"Cookie '{attributeName}' contains an invalid character (';', CR or LF): '{value}'.");

        return value;
    }

    /// <summary>
    /// Parses one <c>name=value</c> pair out of <c>document.cookie</c>, percent-decoding both
    /// halves. Returns <c>null</c> for anything without a name, and never fills in an attribute -
    /// a read simply does not carry them.
    /// </summary>
    public static ButilCookie? Parse(string rawCookie)
    {
        if (string.IsNullOrWhiteSpace(rawCookie)) return null;

        var trimmed = rawCookie.Trim();
        var eqIndex = trimmed.IndexOf('=');

        // A cookie with no '=' or with an empty name is not valid; skip it.
        if (eqIndex <= 0) return null;

        var name = trimmed.Substring(0, eqIndex).Trim();
        var value = trimmed.Substring(eqIndex + 1).Trim();

        return new ButilCookie
        {
            Name = Uri.UnescapeDataString(name),
            Value = Uri.UnescapeDataString(value),
        };
    }
}
