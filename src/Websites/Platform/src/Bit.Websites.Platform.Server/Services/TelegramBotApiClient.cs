using System.Text;
using System.Text.Json;

namespace Bit.Websites.Platform.Server.Services;

public partial class TelegramBotApiClient
{
    private const int MAX_LENGTH_MESSAGE = 4096;

    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private AppSettings appSettings = default!;

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(appSettings.TelegramBotSettings.Token) || appSettings.TelegramBotSettings.ChatIds.Length < 1 || string.IsNullOrEmpty(message))
        {
            return;
        }

        foreach (var chatId in appSettings.TelegramBotSettings.ChatIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            var splitMesasge = GetSplitMessage(message);
            foreach (var item in splitMesasge)
            {
                await SendMessageAsync(chatId, item, cancellationToken);
            }
        }
    }

    private async Task SendMessageAsync(string chatId, string message, CancellationToken cancellationToken)
    {
        var payload = new
        {
            chat_id = chatId,
            text = message,
            parse_mode = "Markdown"
        };
        var json = JsonSerializer.Serialize(value: payload);
        var apiUrl = new Uri(uriString: $"https://api.telegram.org/bot{appSettings.TelegramBotSettings.Token}/sendMessage");
        await httpClient.PostAsync(requestUri: apiUrl,
            content: new StringContent(content: json, encoding: Encoding.UTF8, mediaType: "application/json"), cancellationToken);
    }

    private IEnumerable<string> GetSplitMessage(string message)
    {
        int index = 0;
        while (index < message.Length)
        {
            if (index + MAX_LENGTH_MESSAGE < message.Length)
                yield return message.Substring(index, MAX_LENGTH_MESSAGE);
            else
                yield return message.Substring(index);

            index += MAX_LENGTH_MESSAGE;
        }
    }
}
