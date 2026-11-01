using System.Text;
using System.Buffers.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Builds the Workbench's addresses and reads them back. A list of views travels as one base64url query value.
/// </summary>
public static class WorkbenchUrls
{
    public const string Home = "/";

    /// <summary>The query key of the view a preview belongs to, which keys its saved page state.</summary>
    public const string PaneQueryKey = "pane";

    /// <summary>The query key of a snapshot id, which makes a preview restore that page state.</summary>
    public const string SnapshotQueryKey = "snapshot";

    public const string Flows = "/flows";

    public static string Workbench(string pageKey) => $"/workbench/{pageKey}";

    /// <summary>The query key that starts the tour when the shell opens.</summary>
    public const string TourQueryKey = "tour";

    public static string Tour(string pageKey) => $"{Workbench(pageKey)}?{TourQueryKey}=true";

    public static string Flow(string flowId) => $"{Flows}/{flowId}";

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

    /// <summary>The address of one zip with the screenshots of several views.</summary>
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

    /// <summary>Splits a shots value into page keys and query strings; an invalid value gives none.</summary>
    public static (string PageKey, string Query)[] DecodeShots(string? value)
    {
        return [.. Decode(value).Select(entry =>
        {
            var queryStart = entry.IndexOf('?');
            return queryStart < 0 ? (entry, string.Empty) : (entry[..queryStart], entry[queryStart..]);
        })];
    }

    /// <summary>The shell's views as one query value, for a link that opens the same views.</summary>
    public static string EncodePanes(IEnumerable<WorkbenchScenario> scenarios) => Encode(scenarios.Select(s => s.ToQueryString()));

    public static WorkbenchScenario[] DecodePanes(string? value) => [.. Decode(value).Select(WorkbenchScenario.Parse)];

    /// <summary>
    /// The address this process listens on, with a wildcard host as localhost, preferring a loopback address and then http.
    /// The headless browser and the review relay call the Workbench through it.
    /// </summary>
    public static Uri LocalBaseAddress(IServer server)
    {
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];

        return addresses.Select(address => address.Replace("://+", "://localhost")
                                                  .Replace("://*", "://localhost")
                                                  .Replace("://0.0.0.0", "://localhost")
                                                  .Replace("://[::]", "://localhost"))
                        .Select(address => Uri.TryCreate(address.EndsWith('/') ? address : $"{address}/", UriKind.Absolute, out var uri) ? uri : null)
                        .OfType<Uri>()
                        .OrderBy(uri => uri.IsLoopback is false)
                        .ThenBy(uri => uri.Scheme == Uri.UriSchemeHttps)
                        .FirstOrDefault() ?? new Uri("http://localhost:5000/");
    }

    /// <summary>A query value of an absolute address, or null when it is missing or blank.</summary>
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
