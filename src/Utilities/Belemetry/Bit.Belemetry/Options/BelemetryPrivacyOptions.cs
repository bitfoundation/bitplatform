namespace Bit.Belemetry;

/// <summary>Data-minimization rules applied to every record, on both the browser and the .NET side, before it is queued.</summary>
public sealed class BelemetryPrivacyOptions
{
    /// <summary>
    /// Query-string parameters (case-insensitive) whose values are replaced with <c>REDACTED</c> in every URL attribute.
    /// The defaults include the signatures and credentials of pre-signed storage URLs (Amazon S3, Google Cloud
    /// Storage, Azure SAS <c>sig</c>), as the OpenTelemetry URL conventions redact them.
    /// </summary>
    public List<string> RedactQueryParameters { get; } =
    [
        "token", "code", "password", "pwd", "secret", "key", "apikey", "api_key", "auth", "authorization",
        "session", "sid", "sig", "signature", "email", "access_token", "id_token", "refresh_token",
        "x-amz-signature", "x-amz-credential", "x-amz-security-token", "x-goog-signature", "x-goog-credential",
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

    /// <summary>
    /// Attribute names whose values are replaced with <c>REDACTED</c> in every record and span event, whatever the
    /// value looks like. A name matches an attribute key whole or its last dot-separated segment, case-insensitively -
    /// so <c>password</c> covers a structured log value <c>{Password}</c>, <c>user.password</c> and
    /// <c>http.request.header.authorization</c> alike, but not <c>password_policy</c>; a trailing <c>*</c> matches a
    /// key prefix instead (<c>http.request.header.*</c>). Defaults to credentials and cookies.
    /// </summary>
    public List<string> RedactedAttributes { get; } =
    [
        "password", "passwd", "pwd", "secret", "client_secret", "token", "access_token", "refresh_token", "id_token",
        "apikey", "api_key", "x-api-key", "authorization", "proxy-authorization", "cookie", "set-cookie",
    ];
}
