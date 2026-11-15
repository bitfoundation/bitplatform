using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Wire;

/// <summary>
/// The wire format of <see cref="BitAiEvent"/>: every built-in event survives a round trip, and the reader ignores what
/// a newer producer may add.
/// </summary>
[TestClass]
public class BitAiWireFormatTests
{
    [TestMethod]
    public void TheSamplesCoverEveryBuiltInEvent()
    {
        var builtIn = typeof(BitAiEvent).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BitAiEvent)) && !t.IsAbstract)
            .ToHashSet();

        CollectionAssert.AreEquivalent(builtIn.ToArray(), BitAiWireSamples.All.Select(e => e.GetType()).ToArray());
        Assert.AreEqual(34, builtIn.Count);
    }

    [TestMethod]
    public void EveryBuiltInEventRoundTrips()
    {
        foreach (var evt in BitAiWireSamples.All)
        {
            var json = BitAiWireSerializer.Serialize(evt);
            var back = BitAiWireSerializer.Deserialize(json);

            Assert.IsNotNull(back, evt.Type);
            Assert.AreEqual(evt.GetType(), back.GetType(), evt.Type);
            Assert.AreEqual(evt.Type, back.Type);
            Assert.AreEqual(evt.Timestamp, back.Timestamp, evt.Type);
            Assert.AreEqual(evt.MessageId, back.MessageId, evt.Type);
            Assert.AreEqual(json, BitAiWireSerializer.Serialize(back), evt.Type);
        }
    }

    [TestMethod]
    public void EveryBuiltInEventRoundTripsThroughAJsonElement()
    {
        foreach (var evt in BitAiWireSamples.All)
        {
            var back = BitAiWireSerializer.Deserialize(BitAiWireSerializer.SerializeToElement(evt));
            Assert.AreEqual(BitAiWireSerializer.Serialize(evt), BitAiWireSerializer.Serialize(back!), evt.Type);
        }
    }

    [TestMethod]
    public void TheDiscriminatorIsTheEventsType()
    {
        foreach (var evt in BitAiWireSamples.All)
        {
            using var json = JsonDocument.Parse(BitAiWireSerializer.Serialize(evt));
            Assert.AreEqual(evt.Type, json.RootElement.GetProperty("type").GetString());
            Assert.AreEqual(1, json.RootElement.EnumerateObject().Count(p => p.Name == "type"), evt.Type);
        }
    }

    [TestMethod]
    public void AnEventOfAnUnknownTypeReadsAsNull()
    {
        Assert.IsNull(BitAiWireSerializer.Deserialize("""{"type":"hologram.started","timestamp":"2026-01-02T03:04:05+00:00","beam":1}"""));
    }

    [TestMethod]
    public void AListOfAnyCallerReadsUnknownEventsAsNull()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var events = JsonSerializer.Deserialize<List<BitAiEvent?>>(
            """[{"type":"text.delta","delta":"a"},{"type":"from.the.future"},{"type":"text.delta","delta":"b"}]""", options)!;

        Assert.AreEqual(3, events.Count);
        Assert.AreEqual("a", ((BitAiTextDeltaEvent)events[0]!).Delta);
        Assert.IsNull(events[1]);
        Assert.AreEqual("b", ((BitAiTextDeltaEvent)events[2]!).Delta);
    }

    [TestMethod]
    public void AnUnknownPropertyIsSkipped()
    {
        var evt = BitAiWireSerializer.Deserialize("""{"futureFlag":true,"delta":"x","type":"text.delta","nested":{"a":[1,2]}}""");

        Assert.AreEqual("x", ((BitAiTextDeltaEvent)evt!).Delta);
    }

    [TestMethod]
    public void TheTypeNeedNotComeFirst()
    {
        var evt = BitAiWireSerializer.Deserialize("""{"toolCallId":"t1","type":"tool_call.approval_requested"}""");

        var approval = (BitAiToolApprovalRequestedEvent)evt!;
        Assert.AreEqual("t1", approval.ToolCallId);
        Assert.IsTrue(approval.AllowApproveAlways, "A default the JSON leaves out is the event's own default.");
    }

    [TestMethod]
    public void AMissingTimestampIsTheTimeOfReading()
    {
        var before = DateTimeOffset.UtcNow;
        var evt = BitAiWireSerializer.Deserialize("""{"type":"part.completed"}""")!;

        Assert.IsTrue(evt.Timestamp >= before);
    }

    [TestMethod]
    public void AnOptionalValueThatDoesNotReadIsLeftOut()
    {
        var before = DateTimeOffset.UtcNow;

        var image = (BitAiImageEvent)BitAiWireSerializer.Deserialize("""{"type":"image","timestamp":"yesterday","url":"https://x/y.png","width":1.5,"height":3e10}""")!;
        var progress = (BitAiToolProgressEvent)BitAiWireSerializer.Deserialize("""{"type":"tool_call.progress","toolCallId":"t1","progress":{"fraction":1e400}}""")!;

        Assert.IsTrue(image.Timestamp >= before);
        Assert.IsNull(image.Width);
        Assert.IsNull(image.Height);
        Assert.IsNull(progress.Progress.Fraction);
    }

    [TestMethod]
    public void AnEventWithoutATypeFails()
    {
        Assert.ThrowsExactly<JsonException>(() => BitAiWireSerializer.Deserialize("""{"delta":"x"}"""));
    }

    [TestMethod]
    public void AnEventMissingWhatItsConstructorNeedsFails()
    {
        Assert.ThrowsExactly<JsonException>(() => BitAiWireSerializer.Deserialize("""{"type":"text.delta"}"""));
        Assert.ThrowsExactly<JsonException>(() => BitAiWireSerializer.Deserialize("""{"type":"tool_call.started","toolCallId":"t1"}"""));
        Assert.ThrowsExactly<JsonException>(() => BitAiWireSerializer.Deserialize("""{"type":"tool_call.started","toolCallId":" ","toolName":"x"}"""));
    }

    [TestMethod]
    public void AnEventTypeOfTheAppIsNotWritten()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => BitAiWireSerializer.Serialize(new AppEvent()));
    }

    [TestMethod]
    public void TheFormatIsTheSameWhateverTheSettingsOfTheCaller()
    {
        var pascal = new JsonSerializerOptions();
        var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        foreach (var evt in BitAiWireSamples.All)
        {
            var expected = BitAiWireSerializer.Serialize(evt);
            Assert.AreEqual(expected, JsonSerializer.Serialize<BitAiEvent>(evt, pascal), evt.Type);
            Assert.AreEqual(expected, JsonSerializer.Serialize<BitAiEvent>(evt, web), evt.Type);
            Assert.AreEqual(expected, BitAiWireSerializer.Serialize(JsonSerializer.Deserialize<BitAiEvent>(expected, pascal)!), evt.Type);
        }
    }

    [TestMethod]
    public void AnEventSerializedAsItsOwnTypeStillReadsBack()
    {
        // Declared as the record itself rather than as a BitAiEvent, an event is written by the caller's own contract,
        // which still names its type (the Type property), so the wire format reads it.
        var evt = new BitAiTextDeltaEvent("x") { Timestamp = BitAiWireSamples.At, PartId = "p1" };

        var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.AreEqual(BitAiWireSerializer.Serialize(evt), BitAiWireSerializer.Serialize(BitAiWireSerializer.Deserialize(json)!));
    }

    [TestMethod]
    public void AValueOfAnAppTypeNeedsTheAppsMetadata()
    {
        var evt = new BitAiCustomEvent("app.point", new AppPoint(1, 2));

        Assert.ThrowsExactly<NotSupportedException>(() => BitAiWireSerializer.Serialize(evt));

        var json = BitAiConversationSerializerOptions.CreateJsonOptions();
        json.TypeInfoResolverChain.Add(AppJsonContext.Default);
        var options = new BitAiConversationSerializerOptions { Json = json };

        var back = (BitAiCustomEvent)BitAiWireSerializer.Deserialize(BitAiWireSerializer.Serialize(evt, options), options)!;
        var data = (JsonElement)back.Data!;
        Assert.AreEqual(1, data.GetProperty("x").GetInt32());
        Assert.AreEqual(2, data.GetProperty("y").GetInt32());
    }

    [TestMethod]
    public void AMemberOfANewerVersionReadsAsTheOneThatSaysTheLeast()
    {
        // A newer server adds a member to an enumeration: an addition, which an older reader skips rather than failing on.
        var completed = (BitAiMessageCompletedEvent)BitAiWireSerializer.Deserialize("""{"type":"message.completed","finishReason":"paused"}""")!;
        Assert.AreEqual(BitAiFinishReason.Unknown, completed.FinishReason);

        var error = (BitAiErrorEvent)BitAiWireSerializer.Deserialize("""{"type":"error","error":{"message":"boom","kind":"quantum"}}""")!;
        Assert.AreEqual("boom", error.Error.Message);
        Assert.AreEqual(BitAiErrorKind.Unknown, error.Error.Kind);

        var step = (BitAiAgentStepCompletedEvent)BitAiWireSerializer.Deserialize("""{"type":"agent.step.completed","runId":"r","stepId":"s","status":"deferred"}""")!;
        Assert.AreEqual(BitAiAgentStepStatus.Completed, step.Status);

        var undefinedNumber = (BitAiMessageCompletedEvent)BitAiWireSerializer.Deserialize("""{"type":"message.completed","finishReason":99}""")!;
        Assert.AreEqual(BitAiFinishReason.Unknown, undefinedNumber.FinishReason);

        Assert.AreEqual(BitAiFinishReason.ContentFilter,
            ((BitAiMessageCompletedEvent)BitAiWireSerializer.Deserialize("""{"type":"message.completed","finishReason":"contentFilter"}""")!).FinishReason,
            "A member this version knows still reads as itself.");
    }

    [TestMethod]
    public void AnUnknownDecisionIsARejection()
    {
        var approval = BitAiWireSerializer.DeserializeApproval("""{"toolCallId":"t1","toolName":"delete","decision":"approvedOnce"}""");

        Assert.AreEqual(BitAiApprovalDecision.Rejected, approval.Decision);
    }

    [TestMethod]
    public void WhatIsNotWireDataIsLeftOut()
    {
        var error = new BitAiError("boom") { Exception = new InvalidOperationException("secret") };
        var attachment = new BitAiAttachment("f1", "a.txt") { ContentFactory = _ => throw new InvalidOperationException() };

        var errorJson = BitAiWireSerializer.Serialize(new BitAiErrorEvent(error));
        var fileJson = BitAiWireSerializer.Serialize(new BitAiFileEvent(attachment));

        Assert.IsFalse(errorJson.Contains("secret", StringComparison.Ordinal), errorJson);
        Assert.IsFalse(errorJson.Contains("exception", StringComparison.OrdinalIgnoreCase), errorJson);
        Assert.IsFalse(fileJson.Contains("contentFactory", StringComparison.OrdinalIgnoreCase), fileJson);
        Assert.IsNull(((BitAiErrorEvent)BitAiWireSerializer.Deserialize(errorJson)!).Error.Exception);
    }

    [TestMethod]
    public void AFailedToolOutcomeRoundTrips()
    {
        var evt = new BitAiToolResultEvent("t1", BitAiToolOutcome.Failure(new BitAiError("Nope") { Code = "denied", Kind = BitAiErrorKind.Authorization }));

        var back = (BitAiToolResultEvent)BitAiWireSerializer.Deserialize(BitAiWireSerializer.Serialize(evt))!;

        Assert.IsFalse(back.Outcome.IsSuccess);
        Assert.AreEqual("Nope", back.Outcome.Error!.Message);
        Assert.AreEqual(BitAiErrorKind.Authorization, back.Outcome.Error.Kind);
        Assert.AreEqual("denied", back.Outcome.Error.Code);
    }

    [TestMethod]
    public void ATextToolOutcomeRoundTrips()
    {
        var back = (BitAiToolResultEvent)BitAiWireSerializer.Deserialize(
            BitAiWireSerializer.Serialize(new BitAiToolResultEvent("t1", BitAiToolOutcome.FromText("done"))))!;

        Assert.IsTrue(back.Outcome.IsSuccess);
        Assert.AreEqual("done", back.Outcome.GetDisplayText());
    }

    [TestMethod]
    public void AStructuredValueOfNullRoundTrips()
    {
        var evt = new BitAiStructuredDataEvent(JsonDocument.Parse("null").RootElement.Clone());

        var back = (BitAiStructuredDataEvent)BitAiWireSerializer.Deserialize(BitAiWireSerializer.Serialize(evt))!;

        Assert.AreEqual(JsonValueKind.Null, back.Value.ValueKind);
    }

    [TestMethod]
    public void AToolChoiceRoundTrips()
    {
        var options = BitAiConversationSerializerOptions.CreateJsonOptions();

        foreach (var choice in new[] { BitAiToolChoice.Auto, BitAiToolChoice.None, BitAiToolChoice.Required, BitAiToolChoice.Specific("search") })
        {
            Assert.AreEqual(choice, JsonSerializer.Deserialize<BitAiToolChoice>(JsonSerializer.Serialize(choice, options), options));
        }

        Assert.AreEqual(BitAiToolChoice.None, JsonSerializer.Deserialize<BitAiToolChoice>("\"none\"", options));
    }

    private sealed record AppEvent : BitAiEvent
    {
        public override string Type => "app.event";
    }
}

internal sealed record AppPoint(int X, int Y);

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(AppPoint))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
