using System.Text;

namespace Bit.Websites.Platform.Api.Services;

public partial class TelegramBotService
{
    [AutoInject] protected TelegramBotApiClient TelegramBotApiClient = default!;

    public async Task SendContactUsMessage(string email, string message, CancellationToken cancellationToken)
    {
        var messsageBuilder = new StringBuilder();
        if (string.IsNullOrEmpty(email))
        {
            messsageBuilder.AppendLine($"📥 *Email:* `-`");
        }
        else
        {
            messsageBuilder.AppendLine($"📥 *Email:* `{email.Trim()}`");
        }

        messsageBuilder.AppendLine($"📜 *Message*: {message.Trim()}");

        await TelegramBotApiClient.SendMessageAsync(messsageBuilder.ToString(), cancellationToken);
    }

    public async Task SendBuyPackageMessage(string packageTitle, string email, string message, CancellationToken cancellationToken)
    {
        var messsageBuilder = new StringBuilder();
        messsageBuilder.AppendLine($"📥 *Email:* `{email.Trim()}`");
        messsageBuilder.AppendLine($"💻 *Support pacakge:* `{packageTitle.Trim()}`");
        if (string.IsNullOrEmpty(message))
        {
            messsageBuilder.AppendLine($"📜 *Message:* `-`");
        }
        else
        {
            messsageBuilder.AppendLine($"📜 *Message*: {message.Trim()}");
        }

        await TelegramBotApiClient.SendMessageAsync(messsageBuilder.ToString(), cancellationToken);
    }
}
