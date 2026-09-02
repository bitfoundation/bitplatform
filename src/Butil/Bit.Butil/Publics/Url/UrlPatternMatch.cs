using System.Collections.Generic;

namespace Bit.Butil;

/// <summary>
/// The result of matching a URL against a
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/URLPattern">URLPattern</see>: what each
/// component of the URL was, and what the pattern's named parameters captured.
/// </summary>
public class UrlPatternMatch
{
    /// <summary>The matched scheme including its colon, e.g. <c>https:</c>.</summary>
    public string Protocol { get; set; } = string.Empty;

    /// <summary>The matched host without the port.</summary>
    public string Hostname { get; set; } = string.Empty;

    /// <summary>The matched port, or an empty string.</summary>
    public string Port { get; set; } = string.Empty;

    /// <summary>The matched path.</summary>
    public string Pathname { get; set; } = string.Empty;

    /// <summary>The matched query, without its leading <c>?</c>.</summary>
    public string Search { get; set; } = string.Empty;

    /// <summary>The matched fragment, without its leading <c>#</c>.</summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// What the pattern's parameters captured: <c>"/books/:id"</c> against <c>"/books/42"</c> gives
    /// <c>id = "42"</c>. Unnamed groups (a bare <c>*</c> or a parenthesised group) are keyed by their
    /// position - <c>"0"</c>, <c>"1"</c> - and an optional group that didn't participate has a null value.
    /// </summary>
    /// <remarks>
    /// The groups of every component are merged into this one map. A pattern names a parameter once
    /// across the whole URL, so nothing is lost; positional keys repeat per component, and the
    /// left-most component's wins.
    /// </remarks>
    public Dictionary<string, string?> Groups { get; set; } = [];
}
