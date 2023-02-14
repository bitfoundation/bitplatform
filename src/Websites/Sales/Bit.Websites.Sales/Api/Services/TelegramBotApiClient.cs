using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Bit.Websites.Sales.Api.Services;

public partial class TelegramBotApiClient
{
    private const int MAX_LENGTH_MESSAGE = 4096;

    [AutoInject] protected HttpClient HttpClient = default!;
    [AutoInject] protected IOptionsSnapshot<AppSettings> AppSettings = default!;

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(AppSettings.Value.TelegramBotSettings.Token) || AppSettings.Value.TelegramBotSettings.ChatIds.Length < 1 || string.IsNullOrEmpty(message))
        {
            return;
        }

        foreach (var chatId in AppSettings.Value.TelegramBotSettings.ChatIds)
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
        var apiUrl = new Uri(uriString: $"https://api.telegram.org/bot{AppSettings.Value.TelegramBotSettings.Token}/sendMessage");
        await HttpClient.PostAsync(requestUri: apiUrl,
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
