using System;
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
        if (Name is null) return string.Empty;

        var sb = new StringBuilder();

        sb.Append($"{Name}={Value}");

        if (Domain is not null)
        {
            sb.Append($";domain={Domain}");
        }

        if (Expires is not null)
        {
            sb.Append($";expires={Expires.Value.UtcDateTime.ToString("ddd, MMM dd yyyy HH:mm:ss \"GMT\"")}");
        }

        if (MaxAge is not null)
        {
            sb.Append($";max-age={MaxAge}");
        }

        if (Partitioned)
        {
            sb.Append(";partitioned");
        }

        if (Path is not null)
        {
            sb.Append($";path={Path}");
        }

        if (SameSite is not null)
        {
            sb.Append($";samesite={SameSite.ToString()!.ToLowerInvariant()}");
        }

        if (Secure)
        {
            sb.Append(";secure");
        }

        return sb.ToString();
    }

    public static ButilCookie Parse(string rawCookie)
    {
        var cookie = new ButilCookie();
        if (rawCookie.Contains('='))
        {
            var split = rawCookie.Split('=');
            cookie.Name = split[0].Trim();
            cookie.Value = split[1].Trim();
        }
        return cookie;
    }
}
