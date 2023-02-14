using System.Text;

namespace Bit.Websites.Sales.Api.Services;

public partial class TelegramBotService
{
    [AutoInject] protected TelegramBotApiClient TelegramBotApiClient = default!;

    public async Task SendContactUsMessage(string? email, string? name, string? message, CancellationToken cancellationToken)
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.AppendLine(string.IsNullOrEmpty(email) ? $"📥 *Email:* `-`" : $"📥 *Email:* `{email.Trim()}`");
        messageBuilder.AppendLine(string.IsNullOrEmpty(name) ? $"📥 *Name:* `-`" : $"📥 *Name:* `{name.Trim()}`");
        messageBuilder.AppendLine($"📜 *Message*: {message}");

        await TelegramBotApiClient.SendMessageAsync(messageBuilder.ToString(), cancellationToken);
    }
}
