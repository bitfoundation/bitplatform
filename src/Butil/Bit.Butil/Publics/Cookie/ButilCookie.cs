using System;
using System.Globalization;
using System.Net;
using System.Text;

namespace Bit.Butil;

public class ButilCookie
{
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? Domain { get; set; }
    public DateTimeOffset? Expires { get; set; }
    public long? MaxAge { get; set; }
    public bool Partitioned { get; set; }
    public string? Path { get; set; }
    public SameSite? SameSite { get; set; }
    public bool Secure { get; set; }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Name)) return string.Empty;

        var sb = new StringBuilder();

        // Per RFC 6265, name and value must be encoded so that reserved characters
        // (=, ;, ,, whitespace, non-ASCII) don't break the cookie.
        sb.Append(WebUtility.UrlEncode(Name));
        sb.Append('=');
        if (Value is not null)
        {
            sb.Append(WebUtility.UrlEncode(Value));
        }

        if (Domain is not null)
        {
            sb.Append(";domain=").Append(Domain);
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
            sb.Append(";path=").Append(Path);
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
            Name = WebUtility.UrlDecode(name),
            Value = WebUtility.UrlDecode(value),
        };
    }
}
