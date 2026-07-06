using System.Text.Json;

namespace Bit.Butil;

/// <summary>
/// Mirrors a <see href="https://developer.mozilla.org/en-US/docs/Web/API/Reporting_API/Using_the_Reporting_API">Report</see>.
/// The body shape varies per <see cref="Type"/>, so it's surfaced as a <see cref="JsonElement"/>.
/// </summary>
public class BrowserReport
{
    /// <summary>
    /// Report type - typical values include <c>"deprecation"</c>, <c>"intervention"</c>,
    /// <c>"crash"</c>, <c>"csp-violation"</c>.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>URL the report applies to.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>The full body as supplied by the browser.</summary>
    public JsonElement Body { get; set; }
}
