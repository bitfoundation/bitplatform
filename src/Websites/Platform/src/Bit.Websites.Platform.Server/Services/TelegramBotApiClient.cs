using System.Text;
using System.Text.Json;

namespace Bit.Websites.Platform.Server.Services;

public partial class TelegramBotApiClient
{
    private const int MAX_LENGTH_MESSAGE = 4096;

    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private AppSettings appSettings = default!;
    [AutoInject] private ILogger<TelegramBotApiClient> logger = default!;

    /// <returns>
    /// Whether the message reached every chat it was meant for - false when nothing was sent at all. A caller
    /// that tells someone their message is on its way has to know this; the ones that do not may ignore it.
    /// </returns>
    public async Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(appSettings.TelegramBotSettings.Token) || appSettings.TelegramBotSettings.ChatIds.Length < 1 || string.IsNullOrEmpty(message))
        {
            return false;
        }

        var delivered = true;

        foreach (var chatId in appSettings.TelegramBotSettings.ChatIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            var splitMesasge = GetSplitMessage(message);
            foreach (var item in splitMesasge)
            {
                delivered &= await SendMessageAsync(chatId, item, cancellationToken);
            }
        }

        return delivered;
    }

    private async Task<bool> SendMessageAsync(string chatId, string message, CancellationToken cancellationToken)
    {
        var payload = new
        {
            chat_id = chatId,
            text = message,
            parse_mode = "Markdown"
        };
        var json = JsonSerializer.Serialize(value: payload);
        var apiUrl = new Uri(uriString: $"https://api.telegram.org/bot{appSettings.TelegramBotSettings.Token}/sendMessage");
        var response = await httpClient.PostAsync(requestUri: apiUrl,
            content: new StringContent(content: json, encoding: Encoding.UTF8, mediaType: "application/json"), cancellationToken);

        if (response.IsSuccessStatusCode) return true;

        // The body names the reason - a revoked token, a chat the bot was removed from, a markdown entity it
        // could not parse - and without it a message that never arrived leaves nothing behind to look at.
        logger.LogError("Sending a message to the {TelegramChatId} Telegram chat failed with {StatusCode}: {TelegramResponse}",
            chatId, response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

        return false;
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
