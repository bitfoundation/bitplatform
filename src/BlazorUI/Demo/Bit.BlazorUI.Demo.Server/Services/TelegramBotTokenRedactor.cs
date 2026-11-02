using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Bit.Belemetry;

namespace Bit.BlazorUI.Demo.Server.Services;

/// <summary>
/// The Telegram Bot API takes the bot token in the URL path (https://api.telegram.org/bot{token}/sendMessage), where
/// Belemetry's privacy rules - query strings and well-known secret shapes - never look. Without this, the url.full of
/// <see cref="TelegramBotApiClient"/>'s HttpClient spans and HttpClientFactory's request logs would hand the token to
/// anyone who can read the Application Insights resource.
/// </summary>
public partial class TelegramBotTokenRedactor : IBelemetryProcessor
{
    public bool OnSpan(BelemetrySpan span)
    {
        if (TryRedact(span.Name, out var name)) span.Name = name;
        if (TryRedact(span.StatusMessage, out var statusMessage)) span.StatusMessage = statusMessage;
        Redact(span.Attributes);
        foreach (var spanEvent in span.Events)
        {
            Redact(spanEvent.Attributes);
        }
        return true;
    }

    public bool OnLog(BelemetryLogRecord log)
    {
        if (TryRedact(log.Body, out var body)) log.Body = body;
        Redact(log.Attributes);
        return true;
    }

    private static void Redact(BelemetryAttributes attributes)
    {
        // Collected first: setting a value while enumerating the attributes would invalidate the enumerator.
        List<KeyValuePair<string, string>>? redacted = null;
        foreach (var (key, value) in attributes)
        {
            if (TryRedact(value as string, out var text)) (redacted ??= []).Add(new(key, text));
        }

        foreach (var (key, text) in redacted ?? [])
        {
            attributes.Set(key, text);
        }
    }

    private static bool TryRedact(string? text, [NotNullWhen(true)] out string? redacted)
    {
        redacted = text is not null && text.Contains("/bot", StringComparison.OrdinalIgnoreCase) && BotTokenRegex().IsMatch(text)
            ? BotTokenRegex().Replace(text, "/botREDACTED")
            : null;
        return redacted is not null;
    }

    [GeneratedRegex(@"/bot\d+(?::|%3A)[\w-]+", RegexOptions.IgnoreCase)]
    private static partial Regex BotTokenRegex();
}
