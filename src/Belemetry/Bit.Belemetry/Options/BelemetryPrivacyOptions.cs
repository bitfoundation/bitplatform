namespace Bit.Belemetry;

/// <summary>Data-minimization rules applied to every record, on both the browser and the .NET side, before it is queued.</summary>
public sealed class BelemetryPrivacyOptions
{
    /// <summary>
    /// Query-string parameters (case-insensitive) whose values are replaced with <c>REDACTED</c> in every URL attribute.
    /// </summary>
    public List<string> RedactQueryParameters { get; } =
    [
        "token", "code", "password", "pwd", "secret", "key", "apikey", "api_key", "auth", "authorization",
        "session", "sid", "sig", "signature", "email", "access_token", "id_token", "refresh_token",
    ];

    /// <summary>Whether to drop query strings from URLs entirely. Defaults to <see langword="false"/>.</summary>
    public bool StripQueryString { get; set; }

    /// <summary>Whether to drop URL fragments (which may carry OAuth tokens). Defaults to <see langword="true"/>.</summary>
    public bool StripFragment { get; set; } = true;

    /// <summary>
    /// Whether messages, stack traces and URLs are scrubbed of e-mail addresses, JWTs, bearer tokens, card numbers
    /// (Luhn-checked) and IPv4 addresses. Defaults to <see langword="true"/>.
    /// </summary>
    public bool ScrubPatterns { get; set; } = true;

    /// <summary>Whether the browser's user agent string is part of the browser resource. Defaults to <see langword="true"/>.</summary>
    public bool CollectUserAgent { get; set; } = true;

    /// <summary>Attribute keys removed from every record (exact match).</summary>
    public List<string> BlockedAttributes { get; } = [];
}
