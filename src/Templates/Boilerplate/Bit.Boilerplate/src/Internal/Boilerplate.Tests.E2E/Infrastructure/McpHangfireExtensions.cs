using System.Net;
using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Boilerplate.Tests.E2E.Infrastructure;

public static class McpHangfireExtensions
{
    private static readonly Regex SixDigit = new(@"\b(\d{6})\b", RegexOptions.Compiled);
    private static readonly Regex Href = new(@"href\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>The jobs already addressed to <paramref name="argumentContains"/>, to exclude from a later wait.</summary>
    public static async Task<IReadOnlyCollection<string>> HangfireJobIds(this McpClient mcp, string argumentContains, CancellationToken cancellationToken)
    {
        return [.. JobIdsIn(await List(mcp, argumentContains, cancellationToken))];
    }

    /// <summary>
    /// The next job addressed to <paramref name="argumentContains"/> that is none of <paramref name="knownJobIds"/>.
    /// Job ids rather than a timestamp: one address collects several mails in a journey, and the deployment's clock is
    /// not this machine's.
    /// </summary>
    public static async Task<JsonNode> WaitForHangfireJob(this McpClient mcp, string argumentContains, IReadOnlyCollection<string> knownJobIds, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
        JsonNode? last = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            last = await List(mcp, argumentContains, cancellationToken);
            var job = last?["jobs"]?.AsArray().FirstOrDefault(item => knownJobIds.Contains(JobId(item)) is false);
            if (job is not null)
                return job;

            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException($"No new Hangfire job whose arguments contain '{argumentContains}'. Already seen: [{string.Join(", ", knownJobIds)}]. Last payload: {last}");
    }

    private static async Task<JsonNode?> List(McpClient mcp, string argumentContains, CancellationToken cancellationToken)
    {
        return JsonNode.Parse(await CallText(mcp, "ListHangfireJobs", new Dictionary<string, object?>
        {
            ["state"] = "any",
            ["argumentContains"] = argumentContains,
            ["take"] = 50 // ListHangfireJobs' own cap.
        }, cancellationToken));
    }

    private static IEnumerable<string> JobIdsIn(JsonNode? payload) => payload?["jobs"]?.AsArray().Select(JobId) ?? [];

    private static string JobId(JsonNode? job) => job?["id"]?.GetValue<string>() ?? "";

    public static string? SixDigitInArguments(this JsonNode job)
    {
        var match = SixDigit.Match(DecodedArguments(job));
        return match.Success ? match.Groups[1].Value : null;
    }

    public static IReadOnlyList<string> HttpLinksInArguments(this JsonNode job)
    {
        return [.. Href.Matches(DecodedArguments(job))
            .Select(item => item.Groups[1].Value)
            .Where(link => link.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                           || link.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    public static string DecodedArguments(this JsonNode job)
    {
        var joined = string.Join(" ", job["arguments"]?.AsArray().Select(item => item?.ToString()) ?? []);
        return WebUtility.HtmlDecode(joined);
    }

    private static async Task<string> CallText(McpClient mcp, string tool, Dictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var result = await mcp.CallToolAsync(tool, arguments, cancellationToken: cancellationToken);
        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "";
        Assert.AreNotEqual(true, result.IsError, $"Tool '{tool}' returned an error. Result: '{text}'.");
        return text;
    }
}
