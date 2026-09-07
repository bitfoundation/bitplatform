using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Client;

/// <summary>
/// Reading the deployment's background jobs through <c>/dev-mcp</c>, which is how a journey gets at the mail it sent.
/// </summary>
public static class McpHangfireExtensions
{
    /// <summary>ListHangfireJobs' own cap.</summary>
    private const int MaxTake = 50;

    extension(McpClient mcp)
    {
        /// <summary>The jobs already addressed to <paramref name="argumentContains"/>, to exclude from a later wait.</summary>
        public async Task<IReadOnlyCollection<string>> HangfireJobIds(string argumentContains, CancellationToken cancellationToken)
        {
            var payload = await mcp.ListHangfireJobs(argumentContains, cancellationToken);
            return [.. payload?["jobs"]?.AsArray().Select(job => job.JobId()) ?? []];
        }

        /// <summary>
        /// The next job for <paramref name="argumentContains"/> that is none of <paramref name="exceptJobIds"/> - pass what
        /// <see cref="HangfireJobIds"/> returned first. By id, not timestamp: the deployment's clock is not this machine's.
        /// </summary>
        public async Task<JsonNode> WaitForHangfireJob(string argumentContains, IReadOnlyCollection<string> exceptJobIds, CancellationToken cancellationToken)
        {
            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
            JsonNode? last = null;

            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                last = await mcp.ListHangfireJobs(argumentContains, cancellationToken);
                var job = last?["jobs"]?.AsArray().FirstOrDefault(item => exceptJobIds.Contains(item.JobId()) is false);
                if (job is not null)
                    return job;

                await Task.Delay(250, cancellationToken);
            }

            throw new TimeoutException($"No new Hangfire job whose arguments contain '{argumentContains}'. Already seen: [{string.Join(", ", exceptJobIds)}]. Last payload: {last}");
        }

        private async Task<JsonNode?> ListHangfireJobs(string argumentContains, CancellationToken cancellationToken)
        {
            return JsonNode.Parse(await mcp.CallText("ListHangfireJobs", new Dictionary<string, object?>
            {
                ["state"] = "any",
                ["argumentContains"] = argumentContains,
                ["take"] = MaxTake
            }, cancellationToken));
        }

        private async Task<string> CallText(string tool, Dictionary<string, object?> arguments, CancellationToken cancellationToken)
        {
            var result = await mcp.CallToolAsync(tool, arguments, cancellationToken: cancellationToken);
            var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "";
            Assert.AreNotEqual(true, result.IsError, $"Tool '{tool}' returned an error. Result: '{text}'.");
            return text;
        }
    }
}
