using System.Text;

namespace Bit.Websites.Platform.Server.Services;

public partial class TelegramBotService
{
    [AutoInject] private TelegramBotApiClient telegramBotApiClient = default!;

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
}
