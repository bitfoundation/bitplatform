namespace Bit.Butil;

/// <summary>
/// A URL split into the components
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/URL">URL</see> exposes, as the browser
/// parsed it.
/// </summary>
public class UrlParts
{
    /// <summary>The whole URL, normalized - which is not always the string that went in.</summary>
    public string Href { get; set; } = string.Empty;

    /// <summary>Scheme, host and port, e.g. <c>https://example.com</c>. <c>"null"</c> for an opaque origin.</summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>The scheme including its colon, e.g. <c>https:</c>.</summary>
    public string Protocol { get; set; } = string.Empty;

    /// <summary>The username, or an empty string.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>The password, or an empty string.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Hostname and port together, e.g. <c>example.com:8080</c>.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>The host without the port.</summary>
    public string Hostname { get; set; } = string.Empty;

    /// <summary>The port, or an empty string when it is the scheme's default.</summary>
    public string Port { get; set; } = string.Empty;

    /// <summary>The path, starting with <c>/</c>.</summary>
    public string Pathname { get; set; } = string.Empty;

    /// <summary>The query including its leading <c>?</c>, or an empty string.</summary>
    public string Search { get; set; } = string.Empty;

    /// <summary>The fragment including its leading <c>#</c>, or an empty string.</summary>
    public string Hash { get; set; } = string.Empty;
}
