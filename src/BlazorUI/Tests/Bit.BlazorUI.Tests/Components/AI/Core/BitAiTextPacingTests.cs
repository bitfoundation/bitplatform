using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

[TestClass]
public class BitAiTextPacingTests
{
    [TestMethod]
    public async Task PacingPreservesTheCompleteTextAndTheOrderOfOtherEvents()
    {
        var source = new BitAiEvent[]
        {
            new BitAiMessageStartedEvent(),
            new BitAiTextDeltaEvent("Hello there, "),
            new BitAiTextDeltaEvent("this is a longer burst of output."),
            new BitAiToolCallStartedEvent("c1", "t"),
            new BitAiTextDeltaEvent("And more after the tool."),
            new BitAiMessageCompletedEvent(),
        };

        var options = new BitAiTextPacingOptions
        {
            Interval = TimeSpan.FromMilliseconds(1),
            TargetDrain = TimeSpan.FromMilliseconds(4),
            MaxCharsPerTick = 8,
        };

        var received = new List<BitAiEvent>();
        await foreach (var evt in Replay(source).Paced(options)) received.Add(evt);

        var text = string.Concat(received.OfType<BitAiTextDeltaEvent>().Select(e => e.Delta));
        Assert.AreEqual("Hello there, this is a longer burst of output.And more after the tool.", text);

        Assert.IsExactInstanceOfType<BitAiMessageStartedEvent>(received[0]);
        Assert.IsExactInstanceOfType<BitAiMessageCompletedEvent>(received[^1]);

        var toolIndex = received.FindIndex(e => e is BitAiToolCallStartedEvent);
        var afterTool = received.Skip(toolIndex).OfType<BitAiTextDeltaEvent>().Select(e => e.Delta);
        Assert.AreEqual("And more after the tool.", string.Concat(afterTool));
    }

    [TestMethod]
    public async Task PacingSplitsABurstIntoSeveralDeltas()
    {
        var source = new BitAiEvent[] { new BitAiTextDeltaEvent(new string('x', 200)) };
        var options = new BitAiTextPacingOptions
        {
            Interval = TimeSpan.FromMilliseconds(1),
            TargetDrain = TimeSpan.FromMilliseconds(20),
            MaxCharsPerTick = 16,
        };

        var count = 0;
        await foreach (var _ in Replay(source).Paced(options)) count++;

        Assert.IsTrue(count > 1, "A 200 character burst should be released over several ticks.");
    }

    [TestMethod]
    public async Task ABurstDrainsWithinOneDrainWindowRatherThanTricklingOutItsTail()
    {
        // Nine ticks to drain: a 68 character burst goes out in nine chunks of eight at most, and the event queued
        // behind it right after them - not a tail released a character a tick as the buffer shrinks.
        var source = new BitAiEvent[] { new BitAiTextDeltaEvent(new string('x', 68)), new BitAiMessageCompletedEvent() };
        var options = new BitAiTextPacingOptions
        {
            Interval = TimeSpan.FromMilliseconds(1),
            TargetDrain = TimeSpan.FromMilliseconds(9),
        };

        var received = new List<BitAiEvent>();
        await foreach (var evt in Replay(source).Paced(options)) received.Add(evt);

        var deltas = received.OfType<BitAiTextDeltaEvent>().ToList();
        Assert.AreEqual(68, deltas.Sum(d => d.Delta.Length));
        Assert.IsLessThanOrEqualTo(9, deltas.Count, $"The burst took {deltas.Count} ticks to drain.");
        Assert.IsExactInstanceOfType<BitAiMessageCompletedEvent>(received[^1]);
    }

    [TestMethod]
    public async Task PacingNeverSplitsASurrogatePair()
    {
        var source = new BitAiEvent[] { new BitAiTextDeltaEvent(string.Concat(Enumerable.Repeat("a\U0001F600", 20))) };
        var options = new BitAiTextPacingOptions
        {
            Interval = TimeSpan.FromMilliseconds(1),
            TargetDrain = TimeSpan.FromMilliseconds(100),
            MaxCharsPerTick = 2,
        };

        var deltas = new List<string>();
        await foreach (var evt in Replay(source).Paced(options))
        {
            if (evt is BitAiTextDeltaEvent text) deltas.Add(text.Delta);
        }

        Assert.IsTrue(deltas.Count > 1);
        foreach (var delta in deltas)
        {
            Assert.IsFalse(char.IsHighSurrogate(delta[^1]), "A delta ended with half a surrogate pair.");
            Assert.IsFalse(char.IsLowSurrogate(delta[0]), "A delta started with half a surrogate pair.");
        }
    }

    [TestMethod]
    public void PacingSettingsThatWouldStallTheStreamAreRejected()
    {
        // Zero characters per tick never drains the buffer; minus one millisecond is Task.Delay's "forever".
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BitAiTextPacingOptions { MaxCharsPerTick = 0 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BitAiTextPacingOptions { MinCharsPerTick = 0 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BitAiTextPacingOptions { Interval = TimeSpan.FromMilliseconds(-1) });
    }

    [TestMethod]
    public void AMinimumAboveTheMaximumIsRejectedWhereTheSettingsAreUsed()
    {
        var inverted = new BitAiTextPacingOptions { MinCharsPerTick = 10, MaxCharsPerTick = 5 };

        var error = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Replay([]).Paced(inverted));
        Assert.Contains(nameof(BitAiTextPacingOptions.MaxCharsPerTick), error.Message, StringComparison.Ordinal);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BitAiChatSessionOptions { Pacing = inverted });
    }

    [TestMethod]
    public async Task AMinimumAboveTheDefaultMaximumIsFineOnceTheMaximumIsRaised()
    {
        // Set in this order, the minimum briefly exceeds the default maximum, which must not throw.
        var options = new BitAiTextPacingOptions
        {
            MinCharsPerTick = 60,
            MaxCharsPerTick = 100,
            Interval = TimeSpan.FromMilliseconds(1),
        };

        var text = new System.Text.StringBuilder();
        await foreach (var evt in Replay([new BitAiTextDeltaEvent(new string('x', 250))]).Paced(options))
        {
            if (evt is BitAiTextDeltaEvent delta) text.Append(delta.Delta);
        }

        Assert.AreEqual(250, text.Length);
        Assert.AreEqual(options, new BitAiChatSessionOptions { Pacing = options }.Pacing);
    }

    [TestMethod]
    public async Task AConsumerThatStopsEarlyAlsoStopsTheSource()
    {
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await foreach (var _ in Endless(stopped).Paced(BitAiTextPacingOptions.Default))
        {
            break;
        }

        var finished = await Task.WhenAny(stopped.Task, Task.Delay(10_000));
        Assert.AreSame(stopped.Task, finished);
    }

    private static async IAsyncEnumerable<BitAiEvent> Endless(
        TaskCompletionSource stopped,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            while (true)
            {
                await Task.Delay(5, cancellationToken);
                yield return new BitAiStatusEvent("working");
            }
        }
        finally
        {
            stopped.TrySetResult();
        }
    }

    private static async IAsyncEnumerable<BitAiEvent> Replay(IEnumerable<BitAiEvent> events)
    {
        foreach (var evt in events)
        {
            await Task.Yield();
            yield return evt;
        }
    }
}
