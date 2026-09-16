using System.Text.RegularExpressions;

namespace ButilTests.Hosting.Infrastructure;

/// <summary>Reads what the harness pages render out of a raw HTML response.</summary>
public static class RenderedHtml
{
    /// <summary>The value of <paramref name="attribute"/> on the element with <paramref name="id"/>, or null when either is missing.</summary>
    public static string? AttributeOf(string html, string id, string attribute)
    {
        var element = Regex.Match(html, $"<[a-zA-Z0-9-]+[^>]*\\bid=\"{Regex.Escape(id)}\"[^>]*>");
        if (element.Success is false) return null;

        var value = Regex.Match(element.Value, $"\\b{Regex.Escape(attribute)}=\"([^\"]*)\"");
        return value.Success ? value.Groups[1].Value : null;
    }

    /// <summary>The text of the element with <paramref name="id"/>, or null when there is none.</summary>
    public static string? TextOf(string html, string id)
    {
        var match = Regex.Match(html, $"\\bid=\"{Regex.Escape(id)}\"[^>]*>([^<]*)<");
        return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
    }
}
