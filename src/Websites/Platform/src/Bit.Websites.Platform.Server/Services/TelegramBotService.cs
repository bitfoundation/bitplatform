using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Websites.Platform.Server.Services;

public partial class TelegramBotService
{
    [AutoInject] private TelegramBotApiClient telegramBotApiClient = default!;

    /// <summary>
    /// A report an AI agent sent through the feedback tool of this site's /mcp endpoint. Marked as one, because
    /// unlike a contact form message there may be nobody on the other end waiting for an answer, and because the
    /// release the agent was connected for is worth reading next to what it reported.
    /// </summary>
    /// <returns>Whether it was delivered, which the agent is told rather than left to assume.</returns>
    public async Task<bool> SendMcpFeedbackMessage(string title, string report, string? version, string? email, CancellationToken cancellationToken)
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.AppendLine($"🤖 *Feedback via MCP* `{EscapeMarkdown(version ?? "-")}`");
        messageBuilder.AppendLine($"📥 *Email:* `{EscapeMarkdown(string.IsNullOrEmpty(email) ? "-" : email.Trim())}`");
        messageBuilder.AppendLine($"🐞 *{EscapeMarkdown(title)}*");
        messageBuilder.AppendLine($"📜 *Report*: {EscapeMarkdown(report)}");

        return await telegramBotApiClient.SendMessageAsync(messageBuilder.ToString(), cancellationToken);
    }

    /// <summary>
    /// The messages are sent with Markdown parsing on, so a stray _, *, ` or [ makes Telegram reject the whole
    /// message. A bug report is mostly code, so it would hit that on almost every send.
    /// </summary>
    private static string EscapeMarkdown(string text) => MarkdownSpecialCharacters().Replace(text, @"\$0");

    [GeneratedRegex(@"[_*`\[]")]
    private static partial Regex MarkdownSpecialCharacters();

    public async Task SendContactUsMessage(string? email, string? message, CancellationToken cancellationToken)
    {
        var messageBuilder = new StringBuilder();

        if (string.IsNullOrEmpty(email))
        {
            messageBuilder.AppendLine($"📥 *Email:* `-`");
        }
        else
        {
            messageBuilder.AppendLine($"📥 *Email:* `{email.Trim()}`");
        }

        messageBuilder.AppendLine($"📜 *Message*: {message?.Trim()}");

        await telegramBotApiClient.SendMessageAsync(messageBuilder.ToString(), cancellationToken);
    }

    public async Task SendBuyPackageMessage(string packageTitle, string email, string message, CancellationToken cancellationToken)
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.AppendLine($"📥 *Email:* `{email.Trim()}`");
        messageBuilder.AppendLine($"💻 *Support package:* `{packageTitle.Trim()}`");

        if (string.IsNullOrEmpty(message))
        {
            messageBuilder.AppendLine($"📜 *Message:* `-`");
        }
        else
        {
            messageBuilder.AppendLine($"📜 *Message*: {message.Trim()}");
        }

        await telegramBotApiClient.SendMessageAsync(messageBuilder.ToString(), cancellationToken);
    }
}
