using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Diagnostics.Metrics;

namespace Boilerplate.Server.Api.Features.Chatbot;

/// <summary>
/// The chatbot's instruments. Token usage goes to Microsoft.Extensions.AI's own gen_ai.client.token.usage (same name, unit,
/// buckets, attributes and token types as UseOpenTelemetry), but only what UseOpenTelemetry can't see: voice calls, and
/// the audio, cached and reasoning split the bill depends on.
/// </summary>
public static class ChatbotMetrics
{
    public static readonly UpDownCounter<long> ActiveConversations = Meter.Current.CreateUpDownCounter<long>("chatbot.active_conversations", "{conversation}", "Text chats open in the AI chat panel right now.");

    public static readonly UpDownCounter<long> ActiveVoiceCalls = Meter.Current.CreateUpDownCounter<long>("chatbot.active_voice_calls", "{call}", "Voice calls the server is in right now.");

    // Speech is billed by characters spoken and seconds heard, not by request.
    public static readonly Counter<long> SynthesizedCharacters = Meter.Current.CreateCounter<long>("chatbot.synthesized_characters", "{character}", "Characters handed to the text to speech provider.");

    public static readonly Histogram<long> TranscribedBytes = Meter.Current.CreateHistogram<long>("chatbot.transcribed_bytes", "By", "Size of each recording handed to the speech to text provider.");

    /// <summary>Only where the provider reports it, so thinner than <see cref="TranscribedBytes"/>.</summary>
    private static readonly Histogram<double> transcribedSeconds = Meter.Current.CreateHistogram<double>("chatbot.transcribed_seconds", "s", "Length of each recording handed to the speech to text provider, where it reports one.");

    private static readonly Histogram<long> tokenUsage = Meter.Current.CreateHistogram<long>("gen_ai.client.token.usage", "{token}", "Measures number of input and output tokens used.", tags: null,
        advice: new() { HistogramBucketBoundaries = [1, 4, 16, 64, 256, 1_024, 4_096, 16_384, 65_536, 262_144, 1_048_576, 4_194_304, 16_777_216, 67_108_864] });

    public static void RecordTranscribedSeconds(double seconds, string? model)
        => transcribedSeconds.Record(seconds, new KeyValuePair<string, object?>("gen_ai.request.model", model));

    /// <summary>The split of one text model call (chat, alt text); its input and output totals are UseOpenTelemetry's.</summary>
    public static void RecordChatUsage(UsageDetails? usage, string? agentName, string? model)
        => RecordSplit(usage, Tags("chat", model, agentName));

    /// <summary>See <see cref="RecordChatUsage"/>.</summary>
    public static void RecordSpeechToTextUsage(UsageDetails? usage, string? model)
        => RecordSplit(usage, Tags("generate_content", model, agentName: null));

    /// <summary>A voice call's response.done usage. Nothing else sees voice calls, so the totals are recorded too.</summary>
    public static void RecordRealtimeUsage(JsonNode? usage, string? model)
    {
        if (usage is null) return;

        var tags = Tags("realtime", model, agentName: null);

        Record((long?)usage["input_tokens"], "input", tags);
        Record((long?)usage["output_tokens"], "output", tags);
        Record((long?)usage["input_token_details"]?["text_tokens"], "input_text", tags);
        Record((long?)usage["input_token_details"]?["audio_tokens"], "input_audio", tags);
        Record((long?)usage["input_token_details"]?["image_tokens"], "input_image", tags);
        Record((long?)usage["input_token_details"]?["cached_tokens"], "input_cached", tags);
        Record((long?)usage["output_token_details"]?["text_tokens"], "output_text", tags);
        Record((long?)usage["output_token_details"]?["audio_tokens"], "output_audio", tags);
    }

    /// <summary>A voice call's input transcription, billed by tokens or by duration.</summary>
    public static void RecordRealtimeTranscriptionUsage(JsonNode? usage, string? model)
    {
        if (usage is null) return;

        if ((string?)usage["type"] is "duration")
        {
            RecordTranscribedSeconds((double?)usage["seconds"] ?? 0, model);
            return;
        }

        RecordRealtimeUsage(usage, model);
    }

#pragma warning disable MEAI001 // The audio and text counts of UsageDetails are experimental.
    private static void RecordSplit(UsageDetails? usage, TagList tags)
    {
        if (usage is null) return;

        // Adapters without the audio properties put audio in AdditionalCounts; whatever isn't audio or reasoning is text.
        var inputAudio = usage.InputAudioTokenCount ?? AdditionalCount(usage, "InputTokenDetails.AudioTokenCount");
        var outputAudio = usage.OutputAudioTokenCount ?? AdditionalCount(usage, "OutputTokenDetails.AudioTokenCount");

        Record(usage.InputTextTokenCount ?? usage.InputTokenCount - (inputAudio ?? 0), "input_text", tags);
        Record(inputAudio, "input_audio", tags);
        Record(usage.CachedInputTokenCount, "input_cached", tags);
        Record(usage.OutputTextTokenCount ?? usage.OutputTokenCount - (outputAudio ?? 0) - (usage.ReasoningTokenCount ?? 0), "output_text", tags);
        Record(outputAudio, "output_audio", tags);
        Record(usage.ReasoningTokenCount, "output_reasoning", tags);
    }
#pragma warning restore MEAI001

    private static long? AdditionalCount(UsageDetails usage, string key)
        => usage.AdditionalCounts?.TryGetValue(key, out var count) is true ? count : null;

    private static TagList Tags(string operation, string? model, string? agentName)
    {
        TagList tags = new()
        {
            { "gen_ai.operation.name", operation },
            { "gen_ai.provider.name", "openai" },
            { "gen_ai.request.model", model }
        };

        if (agentName is not null)
        {
            tags.Add("gen_ai.agent.name", agentName);
        }

        return tags;
    }

    private static void Record(long? tokens, string tokenType, TagList tags)
    {
        if (tokens is not > 0) return;

        tags.Add("gen_ai.token.type", tokenType);
        tokenUsage.Record(tokens.Value, tags);
    }
}
