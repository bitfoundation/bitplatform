using System.ClientModel;
using System.ClientModel.Primitives;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

namespace Bit.Cli.Translation;

public sealed record TranslationBatch(CultureInfo Source, CultureInfo Target, IReadOnlyList<string> Values, bool Reminder);

public sealed record TranslationOutput(IReadOnlyList<string> Translations, long? InputTokens, long? OutputTokens);

public interface ITranslationClient
{
    Task<TranslationOutput> TranslateAsync(TranslationBatch batch, CancellationToken cancellationToken);
}

public sealed record TranslationBatchResponse
{
    public required string[] Translations { get; set; }
}

public sealed partial class ChatTranslationClient(IChatClient chatClient, ChatOptions? options = null) : ITranslationClient
{
    public static ChatTranslationClient Create(ResxTranslatorSettings settings)
    {
        var handler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true, EnableMultipleHttp3Connections = true };
        var httpClient = new HttpClient(handler) { DefaultRequestVersion = HttpVersion.Version20, DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher, Timeout = TimeSpan.FromMinutes(10) };

        var client = new ChatClient(
            model: settings.OpenAI.Model ?? throw new InvalidOperationException("OpenAI:Model isn't set in " + ResxTranslatorSettings.FileName + "."),
            credential: new ApiKeyCredential(settings.OpenAI.ApiKey!),
            options: new OpenAIClientOptions { Endpoint = settings.OpenAI.Endpoint, Transport = new HttpClientPipelineTransport(httpClient) });

        return new ChatTranslationClient(client.AsIChatClient(), settings.ChatOptions);
    }

    public async Task<TranslationOutput> TranslateAsync(TranslationBatch batch, CancellationToken cancellationToken)
    {
        var chatOptions = options?.Clone() ?? new ChatOptions();
        chatOptions.ResponseFormat = ChatResponseFormat.ForJsonSchema<TranslationBatchResponse>();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, BuildSystemPrompt(batch)),
            new(ChatRole.User, $"Translate the following strings to {batch.Target.NativeName}:\n\n{JsonSerializer.Serialize(batch.Values)}")
        };

        var response = await chatClient.GetResponseAsync<TranslationBatchResponse>(messages, chatOptions, cancellationToken: cancellationToken);

        return new TranslationOutput(response.Result.Translations ?? [], response.Usage?.InputTokenCount, response.Usage?.OutputTokenCount);
    }

    public static string BuildSystemPrompt(TranslationBatch batch)
    {
        var prompt = $"""
            Act as a translator for software resource files.
            Your task is to translate strings from [{batch.Source.NativeName} - {batch.Source.EnglishName}] to [{batch.Target.NativeName} - {batch.Target.EnglishName}].
            These strings are used in a software application and may contain placeholders such as {"{0}"}, {"{1}"}, etc.
            Translate each string ensuring that any placeholders are preserved exactly as they are, including their numbers, and are placed correctly in the translated sentence according to the target language's grammar and syntax.
            The translation should be accurate and suitable for a software application context.
            Return the translations in the same order as the source strings.
            """;

        return batch.Reminder
            ? prompt + "\nA previous answer changed or dropped placeholders. Keep every {n} placeholder of each source string, exactly once, in its translation."
            : prompt;
    }

    public static bool PlaceholdersMatch(string source, string translation)
    {
        static string[] Placeholders(string text) => [.. PlaceholderRegex().Matches(text).Select(m => m.Groups["index"].Value).Order(StringComparer.Ordinal)];

        return Placeholders(source).SequenceEqual(Placeholders(translation));
    }

    [GeneratedRegex(@"(?<!\{)\{(?<index>\d+)(,[^}]*)?(:[^}]*)?\}(?!\})")]
    private static partial Regex PlaceholderRegex();
}
