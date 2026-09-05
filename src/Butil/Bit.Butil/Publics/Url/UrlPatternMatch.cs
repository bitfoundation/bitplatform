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

    /// <summary>The matched username, or an empty string. A URL rarely carries one.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>The matched password, or an empty string. A URL rarely carries one.</summary>
    public string Password { get; set; } = string.Empty;

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
    /// <c>id = "42"</c>. Unnamed groups (a bare <c>*</c> or a parenthesised group) are keyed by the
    /// component and their position within it - <c>"pathname.0"</c>, <c>"hostname.0"</c> - and an
    /// optional group that didn't participate has a null value.
    /// </summary>
    /// <remarks>
    /// The groups of every component are merged into this one map. A pattern names a parameter once
    /// across the whole URL, so a named group keeps its bare name; positional groups are numbered
    /// from zero again in every component, which is why those carry their component as a prefix -
    /// otherwise a hostname's <c>*</c> and a pathname's <c>*</c> would both be <c>"0"</c> and one
    /// would be lost.
    /// </remarks>
    public Dictionary<string, string?> Groups { get; set; } = [];
}
