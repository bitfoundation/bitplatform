namespace System.Text.Json.Nodes;

/// <summary>
/// Reading one Hangfire job row of what <c>ListHangfireJobs</c> returned.
/// </summary>
public static class JsonNodeExtensions
{
    private static readonly Regex SixDigit = new(@"\b(\d{6})\b", RegexOptions.Compiled);
    private static readonly Regex Href = new(@"href\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    extension(JsonNode? job)
    {
        /// <summary>The job's id, or empty for a node that has none.</summary>
        public string JobId() => job?["id"]?.GetValue<string>() ?? "";

        /// <summary>Every argument joined and html-decoded - a mail job's recipient, subject and rendered body.</summary>
        public string DecodedArguments()
        {
            var joined = string.Join(" ", job?["arguments"]?.AsArray().Select(item => item?.ToString()) ?? []);
            return WebUtility.HtmlDecode(joined);
        }

        /// <summary>The first six-digit code in the arguments, which is where an OTP mail carries it.</summary>
        public string? SixDigitInArguments()
        {
            var match = SixDigit.Match(job.DecodedArguments());
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>The distinct http(s) links in the arguments, in the order the body lists them.</summary>
        public IReadOnlyList<string> HttpLinksInArguments()
        {
            return [.. Href.Matches(job.DecodedArguments())
                .Select(item => item.Groups[1].Value)
                .Where(link => link.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                               || link.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)];
        }
    }
}
