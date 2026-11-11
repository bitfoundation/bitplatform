using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Bit.BlazorUI.Tests.Components.AI.Wire;

/// <summary>
/// One instance of every built-in event, with every member it has set, at a fixed time - what the wire format tests
/// round-trip and pin.
/// </summary>
internal static class BitAiWireSamples
{
    public static readonly DateTimeOffset At = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    public static IReadOnlyList<BitAiEvent> All { get; } =
    [
        new BitAiMessageStartedEvent(BitAiRole.Assistant) { Timestamp = At, MessageId = "m1" },
        new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls) { Timestamp = At },
        new BitAiPartStartedEvent("code") { Timestamp = At, PartId = "p1", Attributes = new Dictionary<string, object?> { ["language"] = "csharp" } },
        new BitAiPartCompletedEvent { Timestamp = At, PartId = "p1" },
        new BitAiTextDeltaEvent("Hello \"world\"\n") { Timestamp = At, PartId = "p2" },
        new BitAiReasoningDeltaEvent("Thinking") { Timestamp = At, Signature = "sig" },
        new BitAiStatusEvent("Searching the web") { Timestamp = At },
        new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 10, OutputTokens = 20, CachedInputTokens = 3, ReasoningTokens = 4, Cost = 0.5m, Currency = "USD" }) { Timestamp = At },
        new BitAiMetadataEvent(new BitAiGenerationMetadata { ModelId = "gpt", ProviderId = "openai", FinishReason = BitAiFinishReason.Stop, Temperature = 0.2 }) { Timestamp = At },
        new BitAiErrorEvent(new BitAiError("Too many requests") { Code = "429", Kind = BitAiErrorKind.RateLimit, IsRetryable = true, RetryAfter = TimeSpan.FromSeconds(5) }, isTerminal: false) { Timestamp = At },
        new BitAiCustomEvent("app.progress", Json("""{"step":2}""")) { Timestamp = At },
        new BitAiCitationEvent(new BitAiCitation("c1", new BitAiCitationSource("s1", "Docs") { Kind = BitAiSourceKind.Web, Url = "https://example.com" }) { Quote = "q", Number = 1 }) { Timestamp = At },
        new BitAiFollowUpsEvent(["Tell me more", "Why?"]) { Timestamp = At },
        new BitAiImageEvent("https://example.com/a.png") { Timestamp = At, PartId = "img", Alt = "A chart", Width = 640, Height = 480 },
        new BitAiFileEvent(new BitAiAttachment("f1", "report.pdf") { ContentType = "application/pdf", Kind = BitAiAttachmentKind.Document, Size = 3, Data = new byte[] { 1, 2, 3 } }) { Timestamp = At },
        new BitAiArtifactCreatedEvent("a1", BitAiArtifactKind.Code, "Program.cs") { Timestamp = At, Language = "csharp", Content = "//", Url = "https://example.com/a1", Metadata = new Dictionary<string, object?> { ["lines"] = 1 } },
        new BitAiArtifactDeltaEvent("a1", "class A {}") { Timestamp = At },
        new BitAiArtifactCompletedEvent("a1") { Timestamp = At, Summary = "Added A" },
        new BitAiAgentRunStartedEvent("r1") { Timestamp = At, AgentName = "Researcher", Goal = "Find it" },
        new BitAiAgentPlanEvent("r1", [new BitAiAgentPlanStep("s1", "Search", "the web"), new BitAiAgentPlanStep("s2", "Summarize")]) { Timestamp = At },
        new BitAiAgentStepStartedEvent("r1", "s1", "Search") { Timestamp = At },
        new BitAiAgentStepCompletedEvent("r1", "s1", BitAiAgentStepStatus.Failed) { Timestamp = At, Summary = "No luck", Error = new BitAiError("Offline") { Kind = BitAiErrorKind.Network } },
        new BitAiAgentRunCompletedEvent("r1", BitAiAgentRunStatus.Cancelled) { Timestamp = At },
        new BitAiAgentInputRequestedEvent("r1", "Which one?") { Timestamp = At, Suggestions = ["A", "B"] },
        new BitAiStructuredDataDeltaEvent("{\"a\":") { Timestamp = At, PartId = "sd", SchemaName = "Thing" },
        new BitAiStructuredDataEvent(Json("""{"a":1}""")) { Timestamp = At, PartId = "sd", SchemaName = "Thing" },
        new BitAiGenerativeUiEvent("WeatherCard", Json("""{"city":"Paris"}""")) { Timestamp = At, PartId = "ui", IsPartial = true },
        new BitAiGenerativeUiDeltaEvent("WeatherCard", "{\"ci") { Timestamp = At, PartId = "ui" },
        new BitAiToolCallStartedEvent("t1", "search") { Timestamp = At, Arguments = Json("""{"q":"x"}"""), Definition = new BitAiToolDefinition("search") { Title = "Search", Risk = BitAiToolRisk.Write, RequiresApproval = true } },
        new BitAiToolCallDeltaEvent("t1", "{\"q\":") { Timestamp = At },
        new BitAiToolCallCompletedEvent("t1", Json("""{"q":"x"}""")) { Timestamp = At },
        new BitAiToolProgressEvent("t1", new BitAiToolProgress(0.5, "Half way")) { Timestamp = At },
        new BitAiToolResultEvent("t1", BitAiToolOutcome.Success(Json("""{"hits":2}"""), "2 hits") with { Duration = TimeSpan.FromMilliseconds(1500) }) { Timestamp = At },
        new BitAiToolApprovalRequestedEvent("t1") { Timestamp = At, Reason = "Deletes files", AllowApproveAlways = false },
    ];
}
