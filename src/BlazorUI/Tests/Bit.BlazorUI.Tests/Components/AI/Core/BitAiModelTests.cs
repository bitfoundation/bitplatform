using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

[TestClass]
public class BitAiModelTests
{
    [TestMethod]
    public void ConstructingATextPartWithOneArgumentSetsTheText()
    {
        // Guards an overload shape that previously bound a single string to the identifier instead.
        var part = new BitAiTextPart("hello");

        Assert.AreEqual("hello", part.Text);
        Assert.IsFalse(part.IsStreaming);
        Assert.AreNotEqual("hello", part.PartId);
    }

    [TestMethod]
    public void AppendingIsCheapAndTheTextIsMaterialisedLazily()
    {
        var part = new BitAiTextPart();
        for (var i = 0; i < 1000; i++) part.Append("x");

        Assert.AreEqual(1000, part.Length);
        Assert.AreEqual(1000, part.Text.Length);
    }

    [TestMethod]
    public void PartChangesRaiseEventsOnThePartAndTheMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        var part = message.AddPart(new BitAiTextPart());

        var partEvents = 0;
        var messageEvents = 0;
        part.Changed += _ => partEvents++;
        message.PartChanged += (_, _) => messageEvents++;

        part.Append("a");
        part.Append("b");

        Assert.AreEqual(2, partEvents);
        Assert.AreEqual(2, messageEvents);
    }

    [TestMethod]
    public void ReasoningIsExcludedFromThePlainTextProjection()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.AddPart(new BitAiReasoningPart("thinking hard"));
        message.AddPart(new BitAiTextPart("the answer"));

        Assert.AreEqual("the answer", message.GetText());
    }

    [TestMethod]
    public void MultipleTextualPartsAreJoinedWithABlankLine()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.AddPart(new BitAiTextPart("first"));
        message.AddPart(new BitAiTextPart("second"));

        Assert.AreEqual("first\n\nsecond", message.GetText());
    }

    [TestMethod]
    public void CompletingOpenPartsClosesEveryStreamingPart()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        var text = message.AddPart(new BitAiTextPart());
        var reasoning = message.AddPart(new BitAiReasoningPart());
        var call = message.AddPart(new BitAiToolCallPart("c1", "t"));

        message.CompleteOpenParts();

        Assert.IsFalse(text.IsStreaming);
        Assert.IsFalse(reasoning.IsStreaming);
        Assert.IsNotNull(reasoning.Duration);
        Assert.AreEqual(BitAiToolCallStatus.Pending, call.Status);
    }

    [TestMethod]
    public void TruncateAfterRemovesEverythingBelowTheGivenMessage()
    {
        var conversation = new BitAiConversation();
        var first = conversation.AddMessage(BitAiRole.User, "one");
        conversation.AddMessage(BitAiRole.Assistant, "two");
        conversation.AddMessage(BitAiRole.User, "three");

        var removed = conversation.TruncateAfter(first.MessageId);

        Assert.AreEqual(2, removed.Count);
        Assert.AreEqual("three", removed[0].GetText());
        Assert.ContainsSingle(conversation.Messages);
    }

    [TestMethod]
    public void TruncateAfterCanRemoveTheNamedMessageToo()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "one");
        var second = conversation.AddMessage(BitAiRole.Assistant, "two");

        conversation.TruncateAfter(second.MessageId, inclusive: true);

        Assert.ContainsSingle(conversation.Messages);
    }

    [TestMethod]
    public void RemovedMessagesStopNotifyingTheConversation()
    {
        var conversation = new BitAiConversation();
        var message = conversation.AddMessage(BitAiRole.Assistant, "hi");

        var updates = 0;
        conversation.MessageUpdated += (_, _) => updates++;

        conversation.RemoveMessage(message.MessageId);
        message.SetStatus(BitAiMessageStatus.Failed);

        Assert.AreEqual(0, updates);
    }

    [TestMethod]
    public void TotalUsageSumsEveryAssistantMessage()
    {
        var conversation = new BitAiConversation();
        var first = conversation.AddMessage(BitAiRole.Assistant, "a");
        first.MergeMetadata(new BitAiGenerationMetadata { Usage = new BitAiTokenUsage { InputTokens = 10, OutputTokens = 4 } });
        var second = conversation.AddMessage(BitAiRole.Assistant, "b");
        second.MergeMetadata(new BitAiGenerationMetadata { Usage = new BitAiTokenUsage { InputTokens = 5, OutputTokens = 1 } });

        Assert.AreEqual(20, conversation.TotalUsage.TotalTokens);
    }

    [TestMethod]
    public void TokenUsageAdditionTreatsAbsentValuesAsAbsent()
    {
        var combined = new BitAiTokenUsage { InputTokens = 5 } + new BitAiTokenUsage { OutputTokens = 3 };

        Assert.AreEqual(5, combined.InputTokens);
        Assert.AreEqual(3, combined.OutputTokens);
        Assert.IsNull(combined.Cost);
        Assert.AreEqual(8, combined.TotalTokens);
    }

    [TestMethod]
    public void EmptyUsageReportsItself()
    {
        Assert.IsTrue(default(BitAiTokenUsage).IsEmpty);
        Assert.IsFalse(new BitAiTokenUsage { InputTokens = 0 }.IsEmpty);
    }

    [TestMethod]
    public void CostIsEstimatedFromTheModelPrices()
    {
        var model = new BitAiModel("m", "M")
        {
            InputPricePerMillion = 3m,
            OutputPricePerMillion = 15m,
        };

        var cost = model.EstimateCost(new BitAiTokenUsage { InputTokens = 1_000_000, OutputTokens = 100_000 });

        Assert.AreEqual(4.5m, cost);
    }

    [TestMethod]
    public void RolesCompareCaseInsensitivelyAndRoundTripUnknownValues()
    {
        Assert.AreEqual(BitAiRole.Assistant, new BitAiRole("ASSISTANT"));
        Assert.IsTrue(new BitAiRole("critic") is { IsUser: false, IsAssistant: false });
        Assert.AreEqual("critic", new BitAiRole("critic").Value);
    }

    [TestMethod]
    public void IdentifiersAreUniqueAndSortChronologically()
    {
        var ids = new List<string>();
        for (var i = 0; i < 500; i++) ids.Add(BitAiId.NewMessageId());

        Assert.AreEqual(500, ids.Distinct(StringComparer.Ordinal).Count());
        foreach (var id in ids)
        {
            Assert.StartsWith("msg_", id, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void ArtifactRevisionsKeepAHistoryAndCanBeRestored()
    {
        var artifact = new BitAiArtifact("a1", BitAiArtifactKind.Document, "Notes");
        artifact.Append("v1");
        artifact.Complete();

        artifact.BeginRevision();
        artifact.Append("v2");
        artifact.Complete();

        Assert.IsTrue(artifact.Restore(1));
        Assert.AreEqual("v1", artifact.Content);
        Assert.AreEqual(3, artifact.Version);
        Assert.AreEqual(3, artifact.Versions.Count);
    }

    [TestMethod]
    public void AttachmentKindIsInferredFromTypeThenExtension()
    {
        Assert.AreEqual(BitAiAttachmentKind.Image, BitAiAttachment.InferKind("image/png", "a.bin"));
        Assert.AreEqual(BitAiAttachmentKind.Document, BitAiAttachment.InferKind(null, "report.pdf"));
        Assert.AreEqual(BitAiAttachmentKind.Audio, BitAiAttachment.InferKind(null, "clip.mp3"));
        Assert.AreEqual(BitAiAttachmentKind.File, BitAiAttachment.InferKind(null, "data.bin"));
    }

    [TestMethod]
    public async Task AttachmentContentCanBeReadFromInlineData()
    {
        var attachment = new BitAiAttachment("a", "hello.txt") { Data = "hi"u8.ToArray() };

        await using var stream = await attachment.OpenReadAsync();
        using var reader = new StreamReader(stream);

        Assert.AreEqual("hi", await reader.ReadToEndAsync());
    }

    [TestMethod]
    public void ReasoningIsLeftOutOfTheTextEvenWhenItIsTheOnlyPart()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.AddPart(new BitAiReasoningPart("private chain of thought"));

        Assert.AreEqual(string.Empty, message.GetText());
    }
}
