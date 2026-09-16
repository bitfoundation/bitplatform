using System.Text.Json.Nodes;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.AI;
using Boilerplate.Server.Api.Features.Chatbot;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// <see cref="ChatbotMetrics"/> shares gen_ai.client.token.usage with UseOpenTelemetry, so a wrong token type either hides
/// part of the bill or counts it twice. Each test tags its own model, so parallel tests don't see each other's tokens.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class ChatbotMetricsTests
{
    [TestMethod]
    public void AChatCall_Should_RecordOnlyTheSplit_NotTheTotalsUseOpenTelemetryRecords()
    {
        var model = NewModel();
        using var recorded = new Recorded(model);

        ChatbotMetrics.RecordChatUsage(new UsageDetails
        {
            InputTokenCount = 1_000,
            OutputTokenCount = 300,
            CachedInputTokenCount = 800,
            ReasoningTokenCount = 100,
            AdditionalCounts = new() { ["OutputTokenDetails.AudioTokenCount"] = 50 }
        }, "SupportAgent", model);

        AssertTokens(new()
        {
            ["input_text"] = 1_000,
            ["input_cached"] = 800,
            ["output_text"] = 150,
            ["output_audio"] = 50,
            ["output_reasoning"] = 100
        }, recorded, "input and output are recorded by UseOpenTelemetry; recording them here too would double them.");

        Assert.AreEqual("chat", recorded.OperationName);
        Assert.AreEqual("SupportAgent", recorded.AgentName);
    }

    [TestMethod]
    public void AVoiceCallResponse_Should_RecordTotalsAndSplit()
    {
        var model = NewModel();
        using var recorded = new Recorded(model);

        ChatbotMetrics.RecordRealtimeUsage(JsonNode.Parse("""
            {
              "total_tokens": 1500,
              "input_tokens": 1200,
              "output_tokens": 300,
              "input_token_details": { "text_tokens": 1000, "audio_tokens": 200, "image_tokens": 0, "cached_tokens": 900 },
              "output_token_details": { "text_tokens": 60, "audio_tokens": 240 }
            }
            """), model);

        AssertTokens(new()
        {
            ["input"] = 1_200,
            ["output"] = 300,
            ["input_text"] = 1_000,
            ["input_audio"] = 200,
            ["input_cached"] = 900,
            ["output_text"] = 60,
            ["output_audio"] = 240
        }, recorded, "Nothing but this sees voice calls, so the totals are recorded too; a zero count isn't recorded.");

        Assert.AreEqual("realtime", recorded.OperationName);
    }

    [TestMethod]
    public void ADurationBilledTranscription_Should_RecordSeconds_NotTokens()
    {
        var model = NewModel();
        using var recorded = new Recorded(model);

        ChatbotMetrics.RecordRealtimeTranscriptionUsage(JsonNode.Parse("""{ "type": "duration", "seconds": 2.5 }"""), model);

        Assert.IsEmpty(recorded.TokensByType);
        Assert.AreEqual(2.5, recorded.Seconds);
    }


    private static string NewModel() => $"test-model-{Guid.NewGuid():N}";

    private static void AssertTokens(Dictionary<string, long> expected, Recorded recorded, string message)
        => Assert.AreSequenceEqual(expected.OrderBy(pair => pair.Key), recorded.TokensByType.OrderBy(pair => pair.Key), message);

    /// <summary>What the OpenTelemetry exporter reads too, narrowed to one model.</summary>
    private sealed class Recorded : IDisposable
    {
        private readonly MeterListener listener = new();

        public Dictionary<string, long> TokensByType { get; } = [];
        public string? OperationName { get; private set; }
        public string? AgentName { get; private set; }
        public double Seconds { get; private set; }

        public Recorded(string model)
        {
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Name is "gen_ai.client.token.usage" or "chatbot.transcribed_seconds")
                    meterListener.EnableMeasurementEvents(instrument);
            };

            listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
            {
                var tagValues = tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value);
                if (Equals(tagValues.GetValueOrDefault("gen_ai.request.model"), model) is false) return;

                TokensByType[(string)tagValues["gen_ai.token.type"]!] = measurement;
                OperationName = (string?)tagValues["gen_ai.operation.name"];
                AgentName = (string?)tagValues.GetValueOrDefault("gen_ai.agent.name");
            });

            listener.SetMeasurementEventCallback<double>((_, measurement, tags, _) =>
            {
                if (tags.ToArray().Any(tag => tag.Key is "gen_ai.request.model" && Equals(tag.Value, model)))
                    Seconds += measurement;
            });

            listener.Start();
        }

        public void Dispose() => listener.Dispose();
    }
}
