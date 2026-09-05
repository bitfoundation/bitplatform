using System.Net;
using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Boilerplate.Tests.E2E.Infrastructure;

public static class McpHangfireExtensions
{
    private static readonly Regex SixDigit = new(@"\b(\d{6})\b", RegexOptions.Compiled);
    private static readonly Regex Href = new(@"href\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static async Task<JsonNode> WaitForHangfireJob(this McpClient mcp, string argumentContains, DateTimeOffset? fromUtc, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
        JsonNode? last = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var arguments = new Dictionary<string, object?>
            {
                ["state"] = "any",
                ["argumentContains"] = argumentContains,
                ["take"] = 10
            };
            if (fromUtc is not null)
                arguments["fromUtc"] = fromUtc.Value.UtcDateTime;

            last = JsonNode.Parse(await CallText(mcp, "ListHangfireJobs", arguments, cancellationToken));
            var jobs = last?["jobs"]?.AsArray();
            if (jobs is { Count: > 0 })
                return jobs[0]!;

            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException($"No Hangfire job whose arguments contain '{argumentContains}' since {fromUtc:o}. Last payload: {last}");
    }

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
