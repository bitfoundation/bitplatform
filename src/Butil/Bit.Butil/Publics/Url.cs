using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/URL">URL</see>,
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/URLSearchParams">URLSearchParams</see>
/// and <see href="https://developer.mozilla.org/en-US/docs/Web/API/URLPattern">URLPattern</see> APIs.
/// </summary>
/// <remarks>
/// The first two overlap <see cref="System.Uri"/>, and where they do, <c>System.Uri</c> is the better
/// tool - it needs no interop call. Two reasons to come here anyway:
/// <list type="bullet">
/// <item>The browser follows the WHATWG URL standard, and <c>System.Uri</c> follows RFC 3986. They
/// disagree in ordinary cases - on trailing dots in a host, on how far <c>..</c> may climb, on which
/// characters get percent-encoded - so when a URL has to match what the browser will do with it
/// (a fetch, a redirect, an origin comparison), asking the browser is the only way to be sure.</item>
/// <item><see cref="TestPattern"/> and <see cref="MatchPattern"/> have no .NET equivalent at all.
/// <c>URLPattern</c> is a route matcher with named parameters, wildcards and per-component patterns -
/// <c>/books/:id</c> against a URL - which is a thing apps write by hand over and over.</item>
/// </list>
/// </remarks>
[ButilService(typeof(Url))]
public class Url(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>URL</c> - which is every runtime that runs Blazor.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.url.isSupported");

    /// <summary>
    /// Whether the browser can parse this URL - <c>URL.canParse()</c>, without the exception the
    /// constructor would throw.
    /// </summary>
    /// <param name="url">An absolute URL, or a reference to resolve against <paramref name="baseUrl"/>.</param>
    /// <param name="baseUrl">Optional base for a relative reference.</param>
    public ValueTask<bool> CanParse(string url, string? baseUrl = null)
        => js.Invoke<bool>("BitButil.url.canParse", url, baseUrl);

    /// <summary>
    /// Splits a URL into its components exactly as the browser does.
    /// </summary>
    /// <param name="url">An absolute URL, or a reference to resolve against <paramref name="baseUrl"/>.</param>
    /// <param name="baseUrl">Optional base for a relative reference.</param>
    /// <returns>The parts, or null when the URL doesn't parse.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UrlParts))]
    public ValueTask<UrlParts?> Parse(string url, string? baseUrl = null)
        => js.Invoke<UrlParts?>("BitButil.url.parse", url, baseUrl);

    /// <summary>
    /// Resolves a possibly relative reference against a base, the way the browser's own resolver
    /// does it (including the scheme-relative <c>//host/path</c> and over-climbing <c>..</c> cases).
    /// </summary>
    /// <returns>The absolute URL, or null when the result doesn't parse.</returns>
    public ValueTask<string?> Resolve(string url, string baseUrl)
        => js.Invoke<string?>("BitButil.url.resolve", url, baseUrl);

    /// <summary>
    /// Parses a query string into its parameters.
    /// </summary>
    /// <param name="query">A query string, with or without the leading <c>?</c>.</param>
    /// <returns>
    /// One entry per parameter, in order, keeping repeats: <c>?tag=a&amp;tag=b</c> is two entries,
    /// which is why this is a list rather than a dictionary.
    /// </returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UrlQueryParameter))]
    public ValueTask<UrlQueryParameter[]> ParseQuery(string query)
        => js.Invoke<UrlQueryParameter[]>("BitButil.url.parseQuery", query);

    /// <summary>
    /// Builds a query string from parameters, percent-encoding each one the way the browser does.
    /// </summary>
    /// <returns>The query string without a leading <c>?</c>.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UrlQueryParameter))]
    public ValueTask<string> BuildQuery(UrlQueryParameter[] parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        // new object?[] { parameters } wraps the array as a single JS argument; passing it bare would
        // spread it, since the params array is object?[] and an array of a reference type converts to it.
        return js.Invoke<string>("BitButil.url.buildQuery", new object?[] { parameters });
    }

    /// <summary>
    /// Every value of one repeated parameter, in order. Empty when the parameter isn't there.
    /// </summary>
    public ValueTask<string[]> GetQueryValues(string query, string key)
        => js.Invoke<string[]>("BitButil.url.getQueryValues", query, key);

    /// <summary>Replaces a URL's whole query string. Null when the URL doesn't parse.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UrlQueryParameter))]
    public ValueTask<string?> SetQuery(string url, UrlQueryParameter[] parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return js.Invoke<string?>("BitButil.url.setQuery", url, parameters);
    }

    /// <summary>
    /// Appends parameters to a URL's query, keeping any that are already there - including one of
    /// the same name.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UrlQueryParameter))]
    public ValueTask<string?> AppendQuery(string url, UrlQueryParameter[] parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return js.Invoke<string?>("BitButil.url.appendQuery", url, parameters);
    }

    /// <summary>Removes every parameter with any of these names from a URL's query.</summary>
    public ValueTask<string?> RemoveQuery(string url, params string[] keys)
        => js.Invoke<string?>("BitButil.url.removeQuery", url, keys ?? []);

    /// <summary>
    /// Sorts a URL's query parameters by name. Useful for a cache key or a signature, where two URLs
    /// that differ only in parameter order should come out identical.
    /// </summary>
    public ValueTask<string?> SortQuery(string url) => js.Invoke<string?>("BitButil.url.sortQuery", url);

    /// <summary>True when the runtime exposes <c>URLPattern</c>.</summary>
    /// <remarks>
    /// Newer than the rest of this class: Chromium and Safari have it, Firefox shipped it later.
    /// During prerender/SSR this returns false rather than throwing - see <see cref="IsSupported"/>.
    /// </remarks>
    public ValueTask<bool> IsPatternSupported() => js.Invoke<bool>("BitButil.url.isPatternSupported");

    /// <summary>
    /// Whether a pattern itself compiles. A malformed pattern and a URL that simply doesn't match
    /// both look like "no match" otherwise.
    /// </summary>
    public ValueTask<bool> IsPatternValid(string pattern, string? baseUrl = null)
        => js.Invoke<bool>("BitButil.url.isPatternValid", pattern, baseUrl);

    /// <summary>
    /// Whether a URL matches a pattern.
    /// </summary>
    /// <param name="pattern">
    /// A URL pattern: <c>"/books/:id"</c>, <c>"https://*.example.com/*"</c>,
    /// <c>"/files/:name.:ext"</c>. Named parameters start with <c>:</c>, <c>*</c> is a wildcard, and
    /// a group in <c>{}</c> can be made optional with <c>?</c>.
    /// </param>
    /// <param name="url">The URL to test.</param>
    /// <param name="baseUrl">Base the pattern is relative to - required for a path-only pattern like <c>"/books/:id"</c>.</param>
    /// <returns>False when the pattern is invalid or <c>URLPattern</c> isn't supported, as well as when it simply doesn't match.</returns>
    public ValueTask<bool> TestPattern(string pattern, string url, string? baseUrl = null)
        => js.Invoke<bool>("BitButil.url.patternTest", pattern, baseUrl, url);

    /// <summary>
    /// Matches a URL against a pattern and returns what the named parameters captured - the routing
    /// case: <c>"/books/:id"</c> against <c>"/books/42"</c> gives <c>id = "42"</c>.
    /// </summary>
    /// <param name="pattern">The pattern - see <see cref="TestPattern"/>.</param>
    /// <param name="url">The URL to match.</param>
    /// <param name="baseUrl">Base the pattern is relative to - required for a path-only pattern.</param>
    /// <returns>The match, or null when the URL doesn't match, the pattern is invalid, or <c>URLPattern</c> isn't supported.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UrlPatternMatch))]
    public ValueTask<UrlPatternMatch?> MatchPattern(string pattern, string url, string? baseUrl = null)
        => js.Invoke<UrlPatternMatch?>("BitButil.url.patternExec", pattern, baseUrl, url);
}
