using System.Text;
using System.Buffers.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchUrls
{
    public const string Home = "/";

    public const string Screenshots = "/screenshots";

    public static string Workbench(string pageKey) => $"/workbench/{pageKey}";

    public static string Preview(string pageKey, WorkbenchScenario scenario, WorkbenchComposition? composition = null) => WithComposition($"/preview/{pageKey}{scenario.ToQueryString()}", composition);

    public static string Screenshot(string pageKey, WorkbenchScenario scenario, WorkbenchComposition? composition = null) => WithComposition($"/api/screenshots/{pageKey}{scenario.ToQueryString()}", composition);

    public static string ScreenshotArchive(IEnumerable<(string PageKey, WorkbenchScenario Scenario, WorkbenchComposition? Composition)> shots)
    {
        var entries = shots.Select(shot => WithComposition($"{shot.PageKey}{shot.Scenario.ToQueryString()}", shot.Composition));

        return QueryHelpers.AddQueryString("/api/screenshots/zip", "shots", Encode(entries));
    }

    public static (string PageKey, string Query)[] DecodeShots(string? value)
    {
        return [.. Decode(value).Select(entry =>
        {
            var queryStart = entry.IndexOf('?');
            return queryStart < 0 ? (entry, string.Empty) : (entry[..queryStart], entry[queryStart..]);
        })];
    }

    public static string EncodePanes(IEnumerable<WorkbenchScenario> scenarios) => Encode(scenarios.Select(s => s.ToQueryString()));

    public static WorkbenchScenario[] DecodePanes(string? value) => [.. Decode(value).Select(WorkbenchScenario.Parse)];

    private static string WithComposition(string url, WorkbenchComposition? composition) => composition is null ? url : composition.AppendTo(url);

    private static string Encode(IEnumerable<string> entries) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(string.Join('|', entries)));

    private static string[] Decode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        try
        {
            return Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value)).Split('|');
        }
        catch (FormatException)
        {
            return [];
        }
    }
}
