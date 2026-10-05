using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.RateLimiting;
using ModelContextProtocol.Protocol;

namespace Bit.Websites.Platform.Server.Services.Mcp;

/// <summary>
/// The one tool of this site's /mcp endpoint that is neither a lookup nor proxied: it hands what an agent
/// wrote to the people who maintain the bit platform. <see cref="McpProxyService"/> merges it into the tool
/// list and dispatches to it, since no upstream provides it.
/// </summary>
public partial class McpFeedbackTool
{
    public const string ToolName = "SendBitPlatformFeedback";

    /// <summary>A report longer than this belongs in a GitHub issue; the chat would carry it as a dozen messages.</summary>
    private const int maxReportLength = 4000;

    private const int maxTitleLength = 200;

    /// <summary>
    /// The endpoint is public and unauthenticated and this tool reaches the team's chat rather than a cache,
    /// so it carries the budget the contact form carries - the "MessageSubmit" policy in
    /// <see cref="Startup.Services"/>. That policy is attached to endpoints and a tool is not one, so the same
    /// limit is applied here by hand, partitioned by caller address for the lifetime of the process.
    /// </summary>
    private static readonly PartitionedRateLimiter<string> rateLimiter = PartitionedRateLimiter.Create<string, string>(
        callerAddress => RateLimitPartition.GetFixedWindowLimiter(callerAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5)
        }));

    public static readonly Tool Definition = new()
    {
        Name = ToolName,
        Title = "Send feedback to the bit platform team",
        Description = """
            Sends a defect report, or any other feedback, about the bit platform straight to the team that maintains
            it: the BlazorUI, Bmotion, Butil, Bswup and Brouter libraries, the bit Boilerplate project template, and
            this endpoint itself. It reaches people in a chat rather than an issue tracker, so call it only once the
            user has agreed that the report should be sent - never to take a note, and never to ask whether something
            is a bug. Write the report so that it stands on its own without the conversation it came out of: what you
            did, what happened, what you expected instead, the versions involved and the shortest repro you have,
            marking plainly what you measured and what you are only inferring. Nobody can reply unless the user gives
            you an address to put in email.
            """,
        InputSchema = BuildInputSchema(),
        Annotations = new()
        {
            // Every other tool on this endpoint only reads. This one writes into a chat the team watches, so a
            // client is right to ask before it runs, and right to not retry it on its own.
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = true
        }
    };

    [AutoInject] private ILogger<McpFeedbackTool> logger = default!;
    [AutoInject] private AppSettings appSettings = default!;
    [AutoInject] private TelegramBotService telegramBotService = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    /// <summary>
    /// Sends what the agent wrote, stamped with the release it connected for - the only version this site is
    /// certain of, whatever the report itself claims.
    /// </summary>
    public async ValueTask<CallToolResult> Call(CallToolRequestParams request, McpVersion? version, CancellationToken cancellationToken)
    {
        var telegramBotSettings = appSettings.TelegramBotSettings;

        // Without these the send is a no-op that reports nothing, so the caller would be told a report reached
        // the team when it reached nobody. Deployments have them; a developer running this site locally does not.
        if (string.IsNullOrEmpty(telegramBotSettings.Token) || telegramBotSettings.ChatIds.Any(chatId => string.IsNullOrEmpty(chatId) is false) is false)
            return Failed("This deployment has no channel to the team configured, so nothing was sent. Open an issue on https://github.com/bitfoundation/bitplatform instead.");

        var title = Argument(request, "title");
        var report = Argument(request, "report");
        var email = Argument(request, "email");

        if (title is null || report is null)
            return Failed("Both title and report are required and neither may be empty. Nothing was sent.");

        if (title.Length > maxTitleLength)
            return Failed($"The title is {title.Length} characters long; keep it under {maxTitleLength}. Nothing was sent.");

        if (report.Length > maxReportLength)
            return Failed($"The report is {report.Length} characters long. Trim it under {maxReportLength}, or open an issue on https://github.com/bitfoundation/bitplatform if it genuinely needs more room. Nothing was sent.");

        var callerAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        using var lease = rateLimiter.AttemptAcquire(callerAddress);

        if (lease.IsAcquired is false)
            return Failed("Too many reports have been sent from this address. Wait a few minutes, or open an issue on https://github.com/bitfoundation/bitplatform. Nothing was sent.");

        // The agent will tell the user their report is with the team, so it is told whether it actually is -
        // the reason lands in this site's logs, where the caller could do nothing with it anyway.
        if (await telegramBotService.SendMcpFeedbackMessage(title, report, version?.Name, email, cancellationToken) is false)
            return Failed("The report could not be delivered to the team. Open an issue on https://github.com/bitfoundation/bitplatform instead.");

        logger.LogInformation("A report titled {FeedbackTitle} was sent to the team through the {ToolName} tool.", title, ToolName);

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = "The report reached the bit platform team. Tell the user it was sent, and that there will be no reply unless an address went with it." }]
        };
    }

    private static JsonElement BuildInputSchema() => JsonSerializer.SerializeToElement(new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["title"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "One line naming the defect, such as \"BitDropdown loses its selection after a virtualized scroll\"."
            },
            ["report"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = $"The report itself, at most {maxReportLength} characters."
            },
            ["email"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Optional, and only ever an address the user gave you: it is the single way back to them."
            }
        },
        ["required"] = new JsonArray("title", "report")
    });

    /// <summary>
    /// A tool that refuses reports its failure rather than throwing, so the caller reads why and can act on it
    /// - an exception would reach the agent as a protocol error with nothing to do about it.
    /// </summary>
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
}
