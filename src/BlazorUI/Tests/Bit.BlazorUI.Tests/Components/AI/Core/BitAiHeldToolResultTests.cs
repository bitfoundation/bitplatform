using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

/// <summary>
/// A tool result that arrives before its call - parallel streams merged out of order - is held for that call rather than
/// shown on its own: given to the call once it has started, or dropped once the hold time is up, leaving an error part on
/// its message and raising ToolResultDropped. Timed with a clock the tests move on by hand, so the timer of the processor
/// is told apart from the events that drop a result too.
/// </summary>
[TestClass]
public class BitAiHeldToolResultTests
{
    private const string DroppedGhostMessage =
        "A result arrived for the tool call 'ghost', but no call with that id started within 10 seconds, so the result was dropped.";

    private static readonly TimeSpan HoldTime = BitAiConversationStreamProcessor.DefaultToolResultHoldTime;

    private static readonly TimeSpan OneTick = TimeSpan.FromTicks(1);

    private static JsonElement NoArguments => JsonDocument.Parse("{}").RootElement;

    private static (BitAiConversation Conversation, BitAiConversationStreamProcessor Processor, BitAiManualTimeProvider Clock) Create()
    {
        var conversation = new BitAiConversation();
        var clock = new BitAiManualTimeProvider();
        return (conversation, new BitAiConversationStreamProcessor(conversation, clock), clock);
    }

    private static BitAiToolResultEvent Result(string toolCallId, string text = "server") =>
        new(toolCallId, BitAiToolOutcome.FromText(text));

    private static List<BitAiOrphanedToolResultException> RecordDrops(BitAiConversationStreamProcessor processor)
    {
        var dropped = new List<BitAiOrphanedToolResultException>();
        processor.ToolResultDropped += dropped.Add;
        return dropped;
    }



    [TestMethod]
    public void TheHoldTimeIsTenSecondsByDefaultAndNeverNegative()
    {
        var (_, processor, _) = Create();

        Assert.AreEqual(TimeSpan.FromSeconds(10), BitAiConversationStreamProcessor.DefaultToolResultHoldTime);
        Assert.AreEqual(BitAiConversationStreamProcessor.DefaultToolResultHoldTime, processor.ToolResultHoldTime);
        Assert.AreEqual(HoldTime, new BitAiConversationStreamProcessor(new BitAiConversation()).ToolResultHoldTime);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => processor.ToolResultHoldTime = TimeSpan.FromTicks(-1));
        Assert.AreEqual(HoldTime, processor.ToolResultHoldTime);

        processor.ToolResultHoldTime = TimeSpan.Zero;
        Assert.AreEqual(TimeSpan.Zero, processor.ToolResultHoldTime);
    }

    [TestMethod]
    public void AResultAheadOfACallThatStartsWithItsArgumentsIsGivenToThatCallBeforeItIsReady()
    {
        var (conversation, processor, clock) = Create();
        var dropped = RecordDrops(processor);
        processor.BeginGeneration();

        var ready = 0;
        BitAiToolOutcome? resultWhenReady = null;
        processor.ToolCallReady += call =>
        {
            ready++;
            resultWhenReady = call.Result;
        };

        var result = Result("c1");
        processor.Apply(result);

        // Held: nothing of it shows yet - no part of its own, no error - and one timer waits for it.
        var message = Assert.ContainsSingle(conversation.Messages);
        Assert.IsEmpty(message.Parts);
        Assert.AreEqual(1, clock.ActiveTimers);

        processor.Apply(new BitAiToolCallStartedEvent("c1", "search") { Arguments = NoArguments });

        var call = message.FindToolCall("c1")!;
        Assert.AreSame(result.Outcome, call.Result);
        Assert.AreEqual(BitAiToolCallStatus.Completed, call.Status);

        // Given before the call is reported ready, so a session that runs the ready calls sees it answered already.
        Assert.AreEqual(1, ready);
        Assert.AreSame(result.Outcome, resultWhenReady);

        Assert.IsEmpty(message.PartsOf<BitAiToolResultPart>());
        Assert.IsEmpty(message.PartsOf<BitAiErrorPart>());
        Assert.AreEqual(0, clock.ActiveTimers);

        // Given once: the time running out afterwards drops nothing.
        clock.Advance(HoldTime);
        Assert.IsEmpty(dropped);
        Assert.IsEmpty(message.PartsOf<BitAiErrorPart>());
    }

    [TestMethod]
    public void AResultAheadOfAStreamingCallIsGivenToItOnceItsArgumentsAreComplete()
    {
        var (conversation, processor, _) = Create();
        processor.BeginGeneration();

        var ready = 0;
        BitAiToolOutcome? resultWhenReady = null;
        processor.ToolCallReady += call =>
        {
            ready++;
            resultWhenReady = call.Result;
        };

        var failure = BitAiToolOutcome.Failure(new BitAiError("The service is down."));
        processor.Apply(new BitAiToolResultEvent("c1", failure));
        processor.Apply(new BitAiToolCallStartedEvent("c1", "search"));
        processor.Apply(new BitAiToolCallDeltaEvent("c1", """{"q":"x"}"""));

        // Still streaming its arguments: the result waits for them.
        var call = conversation.Messages[0].FindToolCall("c1")!;
        Assert.IsNull(call.Result);
        Assert.AreEqual(BitAiToolCallStatus.Streaming, call.Status);
        Assert.AreEqual(0, ready);

        processor.Apply(new BitAiToolCallCompletedEvent("c1"));

        Assert.AreSame(failure, call.Result);
        Assert.AreEqual(BitAiToolCallStatus.Failed, call.Status);
        Assert.AreEqual("x", call.Arguments!.Value.GetProperty("q").GetString());
        Assert.AreEqual(1, ready);
        Assert.AreSame(failure, resultWhenReady);
    }

    [TestMethod]
    public void AResultWhoseCallNeverStartsIsDroppedWhenTheHoldTimeIsUpWithNoFurtherEvent()
    {
        var (conversation, processor, clock) = Create();
        var dropped = RecordDrops(processor);

        processor.BeginGeneration();
        var result = Result("ghost");
        processor.Apply(result);
        processor.Apply(new BitAiTextDeltaEvent("Done."));
        processor.EndGeneration();

        var message = Assert.ContainsSingle(conversation.Messages);

        // A tick short of the hold time nothing has happened.
        clock.Advance(HoldTime - OneTick);
        Assert.IsEmpty(dropped);
        Assert.IsEmpty(message.PartsOf<BitAiErrorPart>());

        // The turn is over and no event is coming: the timer of the processor drops it.
        clock.Advance(OneTick);

        var error = Assert.ContainsSingle(message.PartsOf<BitAiErrorPart>()).Error;
        var exception = Assert.IsExactInstanceOfType<BitAiOrphanedToolResultException>(error.Exception);

        Assert.AreEqual("orphaned_tool_result", BitAiOrphanedToolResultException.ErrorCode);
        Assert.AreEqual(BitAiOrphanedToolResultException.ErrorCode, error.Code);
        Assert.AreEqual(BitAiErrorKind.Tool, error.Kind);
        Assert.AreEqual(DroppedGhostMessage, error.Message);
        Assert.AreEqual(DroppedGhostMessage, exception.Message);
        Assert.AreEqual("ghost", exception.ToolCallId);
        Assert.AreSame(result.Outcome, exception.Outcome);
        Assert.AreEqual(HoldTime, exception.HoldTime);
        Assert.AreSame(exception, Assert.ContainsSingle(dropped));

        // An error part beside the answer, not a failure of it, and never a result part.
        Assert.AreSame(error, ((BitAiErrorPart)message.Parts[^1]).Error);
        Assert.AreEqual(BitAiMessageStatus.Complete, message.Status);
        Assert.AreEqual("Done.", message.GetText());
        Assert.IsEmpty(message.PartsOf<BitAiToolResultPart>());

        // Dropped once: nothing is held any more, and no timer is left behind.
        Assert.AreEqual(0, clock.ActiveTimers);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.HasCount(1, dropped);
        Assert.ContainsSingle(message.PartsOf<BitAiErrorPart>());
    }

    [TestMethod]
    public void AResultWhoseTimeIsUpIsDroppedByTheNextEventEvenBeforeTheTimerRuns()
    {
        var (conversation, processor, clock) = Create();
        var dropped = RecordDrops(processor);
        processor.BeginGeneration();

        var ready = 0;
        BitAiToolOutcome? resultWhenReady = null;
        processor.ToolCallReady += call =>
        {
            ready++;
            resultWhenReady = call.Result;
        };

        processor.Apply(Result("c1"));

        // The time is up, but the timer has not run.
        clock.Skip(HoldTime);
        Assert.IsEmpty(dropped);

        processor.Apply(new BitAiToolCallStartedEvent("c1", "search") { Arguments = NoArguments });

        // Dropped ahead of the event, so the call arriving too late never takes it: it is a call like any other.
        var message = conversation.Messages[0];
        var call = message.FindToolCall("c1")!;
        Assert.IsNull(call.Result);
        Assert.AreEqual(BitAiToolCallStatus.Pending, call.Status);
        Assert.AreEqual(1, ready);
        Assert.IsNull(resultWhenReady);

        var exception = Assert.ContainsSingle(dropped);
        Assert.AreEqual("c1", exception.ToolCallId);
        Assert.HasCount(2, message.Parts);
        Assert.AreSame(exception, Assert.IsExactInstanceOfType<BitAiErrorPart>(message.Parts[0]).Error.Exception);
        Assert.AreSame(call, message.Parts[1]);

        // The timer that was due finds nothing left to drop.
        Assert.AreEqual(0, clock.ActiveTimers);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.HasCount(1, dropped);
    }

    [TestMethod]
    public void AHoldTimeOfZeroDropsAResultAtOnce()
    {
        var (conversation, processor, clock) = Create();
        var dropped = RecordDrops(processor);
        processor.ToolResultHoldTime = TimeSpan.Zero;
        processor.BeginGeneration();

        processor.Apply(Result("ghost"));

        var exception = Assert.ContainsSingle(dropped);
        Assert.AreEqual(TimeSpan.Zero, exception.HoldTime);
        Assert.AreEqual(
            "A result arrived for the tool call 'ghost', but no call with that id started within 0 seconds, so the result was dropped.",
            exception.Message);
        Assert.AreSame(exception, Assert.ContainsSingle(conversation.Messages[0].PartsOf<BitAiErrorPart>()).Error.Exception);
        Assert.AreEqual(0, clock.ActiveTimers);

        // A call of that id starting afterwards is a call like any other.
        processor.Apply(new BitAiToolCallStartedEvent("ghost", "search") { Arguments = NoArguments });
        Assert.IsNull(conversation.Messages[0].FindToolCall("ghost")!.Result);
    }

    [TestMethod]
    public void TheCitationsOfAHeldResultAreAddedWhenItIsGivenToItsCall()
    {
        var (conversation, processor, _) = Create();
        processor.BeginGeneration();

        var outcome = BitAiToolOutcome.FromText("found") with
        {
            Citations = [new BitAiCitation("cit1", new BitAiCitationSource("s1", "Docs"))],
        };
        processor.Apply(new BitAiToolResultEvent("c1", outcome));

        // Not while it is held: the call they came from may never start.
        var message = conversation.Messages[0];
        Assert.IsEmpty(message.Citations);

        processor.Apply(new BitAiToolCallStartedEvent("c1", "search") { Arguments = NoArguments });

        var citation = Assert.ContainsSingle(message.Citations);
        Assert.AreEqual("cit1", citation.CitationId);
        Assert.AreEqual(1, citation.Number);
    }

    [TestMethod]
    public void TheCitationsOfADroppedResultAreNeverAdded()
    {
        var (conversation, processor, clock) = Create();
        processor.BeginGeneration();

        var outcome = BitAiToolOutcome.FromText("found") with
        {
            Citations = [new BitAiCitation("cit1", new BitAiCitationSource("s1", "Docs"))],
        };
        processor.Apply(new BitAiToolResultEvent("ghost", outcome));
        processor.EndGeneration();

        clock.Advance(HoldTime);

        var message = conversation.Messages[0];
        Assert.ContainsSingle(message.PartsOf<BitAiErrorPart>());
        Assert.IsEmpty(message.Citations);
    }

    [TestMethod]
    public void ACallThatHasStartedWaitsForItsArgumentsHoweverLongTheyStream()
    {
        var (conversation, processor, clock) = Create();
        var dropped = RecordDrops(processor);
        processor.BeginGeneration();

        BitAiToolCallPart? ready = null;
        processor.ToolCallReady += call => ready = call;

        var result = Result("c1");
        processor.Apply(result);
        processor.Apply(new BitAiToolCallStartedEvent("c1", "search"));
        processor.Apply(new BitAiToolCallDeltaEvent("c1", """{"q":"""));

        // The call of its id has started, so the result is its own rather than an orphan's - and the clock no longer
        // counts: a call whose result is set takes no more of its arguments, so it waits for them.
        clock.Advance(HoldTime * 3);
        processor.Apply(new BitAiToolCallDeltaEvent("c1", "\"docs\"}"));

        var message = conversation.Messages[0];
        var call = message.FindToolCall("c1")!;
        Assert.IsNull(call.Result);
        Assert.IsTrue(call.IsStreaming);
        Assert.IsEmpty(dropped);
        Assert.IsEmpty(message.PartsOf<BitAiErrorPart>());

        processor.Apply(new BitAiToolCallCompletedEvent("c1"));

        Assert.AreEqual("docs", call.Arguments!.Value.GetProperty("q").GetString());
        Assert.AreSame(result.Outcome, call.Result);
        Assert.AreSame(call, ready);
        Assert.AreEqual(0, clock.ActiveTimers);
    }

    [TestMethod]
    public void ACallWhoseArgumentsNeverCompleteGetsTheResultWhenTheGenerationEnds()
    {
        var (conversation, processor, _) = Create();
        processor.BeginGeneration();

        var result = Result("c1");
        processor.Apply(result);
        processor.Apply(new BitAiToolCallStartedEvent("c1", "search"));
        processor.Apply(new BitAiToolCallDeltaEvent("c1", """{"q":"docs"}"""));

        processor.EndGeneration();

        var call = conversation.Messages[0].FindToolCall("c1")!;
        Assert.AreEqual("docs", call.Arguments!.Value.GetProperty("q").GetString());
        Assert.AreSame(result.Outcome, call.Result);
        Assert.IsEmpty(conversation.Messages[0].PartsOf<BitAiErrorPart>());
    }

    [TestMethod]
    public void AResultForACallStillStreamingItsArgumentsLeavesThemWhole()
    {
        var (conversation, processor, _) = Create();
        processor.BeginGeneration();

        BitAiToolCallPart? ready = null;
        processor.ToolCallReady += call => ready = call;

        processor.Apply(new BitAiToolCallStartedEvent("c1", "search"));
        processor.Apply(new BitAiToolCallDeltaEvent("c1", """{"q":"""));

        var result = Result("c1");
        processor.Apply(result);

        var call = conversation.Messages[0].FindToolCall("c1")!;
        Assert.IsNull(call.Result);

        processor.Apply(new BitAiToolCallDeltaEvent("c1", "\"docs\"}"));
        processor.Apply(new BitAiToolCallCompletedEvent("c1"));

        Assert.AreEqual("docs", call.Arguments!.Value.GetProperty("q").GetString());
        Assert.AreSame(result.Outcome, call.Result);
        Assert.AreSame(call, ready);
    }

    [TestMethod]
    public void DiscardingTheHeldResultsDropsNothingAndStopsTheTimer()
    {
        var (conversation, processor, clock) = Create();
        var dropped = RecordDrops(processor);
        processor.BeginGeneration();

        processor.Apply(Result("ghost"));
        Assert.AreEqual(1, clock.ActiveTimers);

        processor.DiscardHeldToolResults();
        clock.Advance(HoldTime);

        Assert.AreEqual(0, clock.ActiveTimers);
        Assert.IsEmpty(dropped);
        Assert.IsEmpty(conversation.Messages[0].PartsOf<BitAiErrorPart>());

        // A call of that id starting afterwards is an ordinary call: it has no result to be given.
        processor.Apply(new BitAiToolCallStartedEvent("ghost", "search") { Arguments = NoArguments });
        Assert.IsNull(conversation.Messages[0].FindToolCall("ghost")!.Result);
    }

    [TestMethod]
    public void AChangedHoldTimeAppliesToTheResultsHeldFromThenOn()
    {
        var (_, processor, clock) = Create();
        var dropped = RecordDrops(processor);
        processor.BeginGeneration();

        processor.Apply(Result("early"));
        processor.ToolResultHoldTime = TimeSpan.FromSeconds(2);
        processor.Apply(Result("late"));

        clock.Advance(TimeSpan.FromSeconds(2));

        var late = Assert.ContainsSingle(dropped);
        Assert.AreEqual("late", late.ToolCallId);
        Assert.AreEqual(TimeSpan.FromSeconds(2), late.HoldTime);

        // The one held before the change keeps the time it was held with.
        clock.Advance(TimeSpan.FromSeconds(8) - OneTick);
        Assert.HasCount(1, dropped);

        clock.Advance(OneTick);
        Assert.HasCount(2, dropped);
        Assert.AreEqual("early", dropped[1].ToolCallId);
        Assert.AreEqual(HoldTime, dropped[1].HoldTime);
    }

    [TestMethod]
    public void ADroppedResultLeavesItsErrorOnTheMessageItArrivedFor()
    {
        var (conversation, processor, clock) = Create();

        processor.BeginGeneration();
        processor.Apply(new BitAiTextDeltaEvent("First."));
        processor.EndGeneration();
        var first = conversation.Messages[0];

        processor.BeginGeneration();
        processor.Apply(Result("ghost") with { MessageId = first.MessageId });
        processor.Apply(new BitAiTextDeltaEvent("Second."));
        processor.EndGeneration();

        clock.Advance(HoldTime);

        Assert.ContainsSingle(first.PartsOf<BitAiErrorPart>());
        Assert.IsEmpty(conversation.Messages[1].PartsOf<BitAiErrorPart>());
    }

    [TestMethod]
    public void TheMessageOfADroppedResultReadsTheSameInEveryCulture()
    {
        var (_, processor, clock) = Create();
        var dropped = RecordDrops(processor);
        processor.ToolResultHoldTime = TimeSpan.FromMilliseconds(1500);
        processor.BeginGeneration();

        var culture = CultureInfo.CurrentCulture;
        try
        {
            // A culture that writes a decimal comma.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            processor.Apply(Result("ghost"));
            clock.Advance(TimeSpan.FromMilliseconds(1500));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        Assert.AreEqual(
            "A result arrived for the tool call 'ghost', but no call with that id started within 1.5 seconds, so the result was dropped.",
            Assert.ContainsSingle(dropped).Message);
    }

    [TestMethod]
    public void TheExceptionOfADroppedResultNamesItsCallAndCarriesItsResult()
    {
        var outcome = BitAiToolOutcome.FromText("server");

        var exception = new BitAiOrphanedToolResultException("c1", outcome, TimeSpan.FromSeconds(2.25));

        Assert.AreEqual("c1", exception.ToolCallId);
        Assert.AreSame(outcome, exception.Outcome);
        Assert.AreEqual(TimeSpan.FromSeconds(2.25), exception.HoldTime);
        Assert.AreEqual(
            "A result arrived for the tool call 'c1', but no call with that id started within 2.25 seconds, so the result was dropped.",
            exception.Message);

        // One second is a second, not "1 seconds".
        Assert.AreEqual(
            "A result arrived for the tool call 'c1', but no call with that id started within 1 second, so the result was dropped.",
            new BitAiOrphanedToolResultException("c1", outcome, TimeSpan.FromSeconds(1)).Message);

        Assert.ThrowsExactly<ArgumentException>(() => new BitAiOrphanedToolResultException(" ", outcome, TimeSpan.Zero));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BitAiOrphanedToolResultException("c1", null!, TimeSpan.Zero));
    }

    [TestMethod]
    public void AHoldLongerThanATimerCanWaitAtOnceIsHeld()
    {
        // The system clock: its timer is the one that takes at most 49.7 days at a time, and throws for more.
        var conversation = new BitAiConversation();
        var processor = new BitAiConversationStreamProcessor(conversation, TimeProvider.System)
        {
            ToolResultHoldTime = TimeSpan.MaxValue,
        };
        var dropped = RecordDrops(processor);

        processor.BeginGeneration();
        processor.Apply(Result("c1"));
        processor.Apply(new BitAiTextDeltaEvent("Done."));
        processor.EndGeneration();

        var message = Assert.ContainsSingle(conversation.Messages);
        Assert.IsEmpty(dropped);
        Assert.IsEmpty(message.PartsOf<BitAiErrorPart>());

        processor.DiscardHeldToolResults();
    }

    [TestMethod]
    public void AResultWaitingForTheArgumentsOfACallOnAnExistingMessageIsGivenWhenTheGenerationEnds()
    {
        // A stream that names existing messages only never has a current one: the generation ends with none, and the
        // results waiting for arguments are given all the same.
        var (conversation, processor, _) = Create();
        var existing = conversation.AddMessage(new BitAiMessage(BitAiRole.Assistant, "m1"));

        processor.Apply(new BitAiToolCallStartedEvent("c1", "search") { MessageId = "m1" });
        processor.Apply(new BitAiToolCallDeltaEvent("c1", """{"q":"docs"}""") { MessageId = "m1" });
        var result = Result("c1") with { MessageId = "m1" };
        processor.Apply(result);
        Assert.IsNull(processor.Current);

        processor.EndGeneration();

        var call = existing.FindToolCall("c1")!;
        Assert.IsFalse(call.IsStreaming);
        Assert.AreEqual("docs", call.Arguments!.Value.GetProperty("q").GetString());
        Assert.AreSame(result.Outcome, call.Result);
        Assert.ContainsSingle(conversation.Messages);
    }
}
