using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.Belemetry.Tests.Hosting.Infrastructure;

/// <summary>Reads what <c>&lt;BelemetryScript /&gt;</c> renders out of a raw HTML response.</summary>
public static partial class RenderedHtml
{
    /// <summary>The parsed <c>#bit-belemetry-config</c> JSON, or null when the page has none.</summary>
    public static JsonDocument? BrowserConfig(string html)
    {
        var match = ConfigElement().Match(html);
        return match.Success ? JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value)) : null;
    }

    /// <summary>The <c>src</c> of the module script <c>&lt;BelemetryScript /&gt;</c> renders after the config.</summary>
    public static string? ModuleSource(string html)
    {
        var match = ModuleScript().Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    // The JSON escapes '<', '>' and '&', so the element's text never contains its own closing tag.
    [GeneratedRegex("<script type=\"application/json\" id=\"bit-belemetry-config\"[^>]*>(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex ConfigElement();

    [GeneratedRegex("<script type=\"module\" src=\"([^\"]*bit-belemetry[^\"]*\\.js)\"")]
    private static partial Regex ModuleScript();
}
