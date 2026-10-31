using System.Text;
using System.Buffers.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchUrls
{
    public const string Home = "/";

    public const string PaneQueryKey = "pane";

    public const string SnapshotQueryKey = "snapshot";

    public static string Workbench(string pageKey) => $"/workbench/{pageKey}";

    public static string Preview(string pageKey, WorkbenchScenario scenario, string? paneId = null, string? snapshotId = null)
    {
        var url = $"/preview/{pageKey}{scenario.ToQueryString()}";

        if (paneId is not null)
        {
            url = QueryHelpers.AddQueryString(url, PaneQueryKey, paneId);
        }

        return snapshotId is null ? url : QueryHelpers.AddQueryString(url, SnapshotQueryKey, snapshotId);
    }

    public static string Screenshot(string pageKey, WorkbenchScenario scenario, string? snapshotId = null)
    {
        var url = $"/api/screenshots/{pageKey}{scenario.ToQueryString()}";

        return snapshotId is null ? url : QueryHelpers.AddQueryString(url, SnapshotQueryKey, snapshotId);
    }

    public static string ScreenshotArchive(IEnumerable<(string PageKey, WorkbenchScenario Scenario, string? SnapshotId)> shots)
    {
        var entries = shots.Select(shot =>
        {
            var entry = $"{shot.PageKey}{shot.Scenario.ToQueryString()}";
            return shot.SnapshotId is null ? entry : QueryHelpers.AddQueryString(entry, SnapshotQueryKey, shot.SnapshotId);
        });

        return QueryHelpers.AddQueryString("/api/screenshots/zip", "shots", Encode(entries));
    }

    public static string ReviewSheet(string pageKey, IEnumerable<(string PageKey, WorkbenchScenario Scenario, string? SnapshotId)> shots, string format)
    {
        var archive = ScreenshotArchive(shots);
        var query = archive[archive.IndexOf('?')..];

        return QueryHelpers.AddQueryString($"/api/review-sheet/{pageKey}{query}", "format", format);
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

    public static Uri LocalBaseAddress(IServer server)
    {
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses
            .OrderBy(a => a.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault() ?? "http://localhost:5000";

        address = address.Replace("://+", "://localhost")
                         .Replace("://*", "://localhost")
                         .Replace("://0.0.0.0", "://localhost")
                         .Replace("://[::]", "://localhost");

        return new Uri(address.EndsWith('/') ? address : $"{address}/");
    }

    public static string? ReadQuery(string uri, string key)
    {
        var query = QueryHelpers.ParseQuery(new Uri(uri).Query);

        return query.TryGetValue(key, out var value) && string.IsNullOrWhiteSpace(value) is false ? value.ToString() : null;
    }

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
