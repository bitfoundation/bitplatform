using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ModelContextProtocol.Protocol;

namespace Bit.Websites.Platform.Server.Services.Mcp;

public partial class McpReleasesTool
{
    public const string ToolName = "GetBitPlatformReleases";

    private const string releasesUrl = "https://api.github.com/repos/bitfoundation/bitplatform/releases?per_page=100";

    private const int maxNotesLength = 24_000;

    private const int maxReleaseNotesLength = 6_000;

    private static readonly TimeSpan cacheLifetime = TimeSpan.FromMinutes(15);

    [GeneratedRegex(@"^v-(?<version>\d+\.\d+\.\d+)$")]
    private static partial Regex ReleaseTagRegex();

    [GeneratedRegex(@"^v?-?(?<version>\d+\.\d+\.\d+)(?<prerelease>-\S+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex VersionRegex();

    public static readonly Tool Definition = new()
    {
        Name = ToolName,
        Title = "Check for a newer bit platform release",
        Description = """
            Tells whether a bit platform version is the newest release and, when it is not, returns the release notes of
            every newer release, newest first. bit Boilerplate, bit BlazorUI and every other bit library ship together
            under one version number, so one answer covers all of them. Pass the version a project is on as its bit.json
            records it; without one, the release this connection asked for (the v query of this endpoint) is checked.
            Only stable releases count, so a prerelease ahead of the newest release is up to date. The notes come from the
            GitHub releases of bitfoundation/bitplatform.
            """,
        InputSchema = JsonSerializer.SerializeToElement(new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["version"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "The version to check, such as 10.6.0 or 10.7.0-pre-03."
                }
            }
        }),
        Annotations = new()
        {
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = true
        }
    };

    [AutoInject] private ILogger<McpReleasesTool> logger = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;

    private readonly SemaphoreSlim sync = new(1, 1);

    private Release[]? releases;

    private DateTimeOffset expiresAt;

    public async ValueTask<CallToolResult> Call(CallToolRequestParams request, string? requestedVersion, CancellationToken cancellationToken)
    {
        var version = Argument(request, "version") ?? requestedVersion?.Trim();

        if (string.IsNullOrEmpty(version))
            return Failed("Pass version: the bit platform version the project is on, as its bit.json records it.");

        if (VersionRegex().Match(version) is not { Success: true } match)
            return Failed($"{version} is not a bit platform version. Pass it as bit.json records it, such as 10.6.0 or 10.7.0-pre-03.");

        var number = Version.Parse(match.Groups["version"].Value);
        var isPrerelease = match.Groups["prerelease"].Success;

        var all = await Releases(cancellationToken);

        if (all is not { Length: > 0 })
            return Failed("The release list could not be read from GitHub. Read https://github.com/bitfoundation/bitplatform/releases instead.");

        var latest = all[0];
        var newer = all.Where(release => release.Number > number || (release.Number == number && isPrerelease)).ToArray();

        if (newer.Length is 0)
            return Succeeded($"{version} is up to date: the newest release is {latest.Name}, published {latest.PublishedOn}, {latest.Url}");

        StringBuilder text = new($"{version} is behind: {newer.Length} newer release{(newer.Length is 1 ? "" : "s")}, the newest being {latest.Name}, published {latest.PublishedOn}. Release notes, newest first:");

        foreach (var release in newer)
        {
            text.AppendLine().AppendLine();

            if (text.Length > maxNotesLength)
            {
                text.Append($"{release.Name}, published {release.PublishedOn}: {release.Url}");
                continue;
            }

            text.AppendLine($"# {release.Name}, published {release.PublishedOn}, {release.Url}").AppendLine();
            text.Append(release.Notes.Length > maxReleaseNotesLength ? $"{release.Notes[..maxReleaseNotesLength]}\n\n(cut short, the full notes are at {release.Url})" : release.Notes);
        }

        return Succeeded(text.ToString());
    }

    private async Task<Release[]?> Releases(CancellationToken cancellationToken)
    {
        if (DateTimeOffset.UtcNow < expiresAt) return releases;

        await sync.WaitAsync(cancellationToken);

        try
        {
            if (DateTimeOffset.UtcNow < expiresAt) return releases;

            using var httpClient = httpClientFactory.CreateClient();
            using HttpRequestMessage request = new(HttpMethod.Get, releasesUrl);
            request.Headers.UserAgent.ParseAdd("bitplatform.dev");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var listed = JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken))!.AsArray();

            releases = [.. listed.Select(release => (Release: release!, Match: ReleaseTagRegex().Match(release!["tag_name"]!.GetValue<string>())))
                                 .Where(listing => listing.Match.Success && listing.Release["draft"]!.GetValue<bool>() is false && listing.Release["prerelease"]!.GetValue<bool>() is false)
                                 .Select(listing => new Release(Version.Parse(listing.Match.Groups["version"].Value),
                                                                listing.Release["html_url"]!.GetValue<string>(),
                                                                DateTimeOffset.Parse(listing.Release["published_at"]!.GetValue<string>(), CultureInfo.InvariantCulture),
                                                                listing.Release["body"]?.GetValue<string>()?.Trim() ?? ""))
                                 .OrderByDescending(release => release.Number)];

            expiresAt = DateTimeOffset.UtcNow + cacheLifetime;
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            logger.LogError(exp, "Reading the bit platform releases from GitHub failed.");

            expiresAt = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
        }
        finally
        {
            sync.Release();
        }

        return releases;
    }

    private static CallToolResult Succeeded(string text)
        => new() { Content = [new TextContentBlock { Text = text }] };

    private static CallToolResult Failed(string message)
        => new() { IsError = true, Content = [new TextContentBlock { Text = message }] };

    private static string? Argument(CallToolRequestParams request, string name)
    {
        if (request.Arguments is null ||
            request.Arguments.TryGetValue(name, out var value) is false ||
            value.ValueKind is not JsonValueKind.String)
            return null;

        return value.GetString()?.Trim() is { Length: > 0 } text ? text : null;
    }

    private sealed record Release(Version Number, string Url, DateTimeOffset PublishedAt, string Notes)
    {
        public string Name { get; } = Number.ToString(3);

        public string PublishedOn => PublishedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
