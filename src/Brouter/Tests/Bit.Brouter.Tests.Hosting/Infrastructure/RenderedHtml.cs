using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.Brouter.Tests.Hosting.Infrastructure;

/// <summary>Reads what the harness pages render out of a raw HTML response.</summary>
public static partial class RenderedHtml
{
    /// <summary>The text of the element with <paramref name="id"/>, or null when there is none.</summary>
    public static string? TextOf(string html, string id)
    {
        var match = Regex.Match(html, $"id=\"{Regex.Escape(id)}\"[^>]*>([^<]*)<");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    /// <summary>
    /// The keys of the state persisted for the WebAssembly runtime. Unlike the Server state, which is
    /// protected with Data Protection, this is plain base64 JSON, so a test can see what was persisted.
    /// </summary>
    public static IReadOnlyDictionary<string, string> WebAssemblyPersistedState(string html)
    {
        var match = WebAssemblyStateComment().Match(html);
        if (match.Success is false) return new Dictionary<string, string>();

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups[1].Value));
        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];

        // Each value is itself base64 of the JSON the persisting component wrote.
        return entries.ToDictionary(e => e.Key, e => Encoding.UTF8.GetString(Convert.FromBase64String(e.Value)));
    }

    [GeneratedRegex("<!--Blazor-WebAssembly-Component-State:([A-Za-z0-9+/=]+)-->")]
    private static partial Regex WebAssemblyStateComment();
}
