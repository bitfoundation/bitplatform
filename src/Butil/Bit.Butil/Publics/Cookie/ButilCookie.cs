using System;
using System.Globalization;
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
