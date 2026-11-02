using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

/// <summary>
/// Transports apply events off the renderer's thread while components read the same objects during
/// render. Each test runs one writer against a reader looping on the hot read paths.
/// </summary>
[TestClass]
public class BitAiThreadSafetyTests
{
    private const int Writes = 20_000;

    [TestMethod]
    public async Task TextCanBeReadWhileItIsBeingAppended()
    {
        var part = new BitAiTextPart();

        await RaceAsync(
            () =>
            {
                for (var i = 0; i < Writes; i++) part.Append("token ");
            },
            () =>
            {
                var text = part.Text;
                Assert.IsTrue(text.Length % 6 == 0, "A reader saw a torn append.");
                _ = part.Length;
                _ = part.IsEmpty;
            });

        Assert.AreEqual(Writes * 6, part.Text.Length);
    }

    [TestMethod]
    public async Task ReasoningCanBeReadWhileItIsBeingAppended()
    {
        var part = new BitAiReasoningPart();

        await RaceAsync(
            () =>
            {
                for (var i = 0; i < Writes; i++) part.Append("x");
            },
            () => _ = part.Text);

        Assert.AreEqual(Writes, part.Length);
    }

    [TestMethod]
    public async Task ToolArgumentsCanBeReadWhileTheyStreamIn()
    {
        var call = new BitAiToolCallPart("c1", "search");

        await RaceAsync(
            () =>
            {
                call.AppendArguments("{\"q\":\"");
                for (var i = 0; i < Writes / 10; i++) call.AppendArguments("a");
                call.AppendArguments("\"}");
                call.CompleteArguments();
            },
            () =>
            {
                _ = call.RawArguments;
                _ = call.DisplayArguments;
                _ = call.Arguments;
            });

        Assert.AreEqual(Writes / 10, call.Arguments!.Value.GetProperty("q").GetString()!.Length);
    }

    [TestMethod]
    public async Task StructuredAndGenerativePayloadsCanBeReadWhileTheyStreamIn()
    {
        var structured = new BitAiStructuredDataPart("list");
        var generative = new BitAiGenerativeUiPart("card");

        await RaceAsync(
            () =>
            {
                structured.Append("[");
                generative.AppendProps("""{"items":[""");
                for (var i = 0; i < Writes / 10; i++)
                {
                    structured.Append(i == 0 ? "1" : ",1");
                    generative.AppendProps(i == 0 ? "1" : ",1");
                }

                structured.Append("]");
                generative.AppendProps("]}");
                structured.Complete();
                generative.Complete();
            },
            () =>
            {
                _ = structured.Value;
                _ = structured.RawJson;
                _ = generative.Props;
                _ = generative.RawProps;
            });

        Assert.AreEqual(Writes / 10, structured.Value!.Value.GetArrayLength());
        Assert.AreEqual(Writes / 10, generative.Props!.Value.GetProperty("items").GetArrayLength());
    }

    [TestMethod]
    public async Task ArtifactContentAndHistoryCanBeReadWhileItIsRevised()
    {
        var artifact = new BitAiArtifact("a1", BitAiArtifactKind.Document, "Notes");

        await RaceAsync(
            () =>
            {
                for (var revision = 0; revision < 50; revision++)
                {
                    for (var i = 0; i < 200; i++) artifact.Append("line\n");
                    artifact.Complete();
                    artifact.BeginRevision();
                }
            },
            () =>
            {
                _ = artifact.Content;
                _ = artifact.Length;
                foreach (var version in artifact.Versions) _ = version.Content;
            });

        Assert.AreEqual(50, artifact.Versions.Count);
    }

    [TestMethod]
    public async Task MessagePartsAndCitationsCanBeEnumeratedWhileTheyAreAdded()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        await RaceAsync(
            () =>
            {
                for (var i = 0; i < 2000; i++)
                {
                    message.AddPart(new BitAiTextPart($"paragraph {i}"));
                    message.AddCitation(new BitAiCitation($"cit{i}", new BitAiCitationSource($"src{i % 50}", "Source")));
                    message.AddAttachment(new BitAiAttachment($"att{i}", "file.txt"));
                }
            },
            () =>
            {
                foreach (var part in message.Parts) _ = part.PartId;
                foreach (var citation in message.Citations) _ = citation.Number;
                foreach (var attachment in message.Attachments) _ = attachment.Name;
                _ = message.GetText();
                _ = message.GetSources();
            });

        Assert.AreEqual(2000, message.Parts.Count);
        Assert.AreEqual(2000, message.Citations.Count);
    }

    [TestMethod]
    public async Task ConversationMessagesCanBeEnumeratedWhileTheThreadGrows()
    {
        var conversation = new BitAiConversation();

        await RaceAsync(
            () =>
            {
                for (var i = 0; i < 2000; i++)
                {
                    conversation.AddMessage(BitAiRole.User, $"question {i}");
                    conversation.GetOrAddArtifact($"art{i}", BitAiArtifactKind.Document, "Notes");
                    conversation.GetOrAddRun($"run{i}");
                }

                conversation.TruncateAfter(conversation.Messages[999].MessageId);
            },
            () =>
            {
                foreach (var message in conversation.Messages) _ = message.MessageId;
                foreach (var artifact in conversation.Artifacts.Values) _ = artifact.Title;
                foreach (var run in conversation.Runs.Values) _ = run.Status;
                _ = conversation.ToSummary();
                _ = conversation.TotalUsage;
            });

        Assert.AreEqual(1000, conversation.Messages.Count);
    }

    [TestMethod]
    public async Task AgentStepsCanBeEnumeratedWhileThePlanIsRevised()
    {
        var run = new BitAiAgentRun("run1");

        await RaceAsync(
            () =>
            {
                for (var i = 0; i < 2000; i++)
                {
                    run.SetPlan([new BitAiAgentPlanStep("a", "A"), new BitAiAgentPlanStep($"s{i}", "Step")]);
                    run.GetOrAddStep($"extra{i}").Start();
                }
            },
            () =>
            {
                foreach (var step in run.Steps) _ = step.Status;
                _ = run.Progress;
                _ = run.CurrentStep;
            });

        Assert.AreEqual(3, run.Steps.Count);
    }

    [TestMethod]
    public async Task AStepsToolCallsCanBeEnumeratedWhileCallsAreRecorded()
    {
        var step = new BitAiAgentRun("run1").GetOrAddStep("s1");

        await RaceAsync(
            () =>
            {
                // Several writers, as when tools running in parallel report into the same step, each
                // recording its call twice.
                Parallel.For(0, 2000, i =>
                {
                    step.AddToolCall($"call{i}");
                    step.AddToolCall($"call{i}");
                });
            },
            () =>
            {
                foreach (var id in step.ToolCallIds) _ = id.Length;
            });

        Assert.AreEqual(2000, step.ToolCallIds.Count);
        Assert.AreEqual(2000, step.ToolCallIds.Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public async Task ARunsUsageIsNeverReadHalfUpdated()
    {
        var run = new BitAiAgentRun("run1");

        await RaceAsync(
            () =>
            {
                for (var i = 1; i <= 20000; i++) run.AddUsage(new BitAiTokenUsage { InputTokens = 1, OutputTokens = 1 });
            },
            () =>
            {
                var usage = run.Usage;
                Assert.AreEqual(usage.InputTokens, usage.OutputTokens);
            });

        Assert.AreEqual(20000, run.Usage.InputTokens);
    }

    private static async Task RaceAsync(Action write, Action read)
    {
        using var done = new CancellationTokenSource();
        var reader = Task.Run(() =>
        {
            while (!done.IsCancellationRequested) read();
        });

        try
        {
            await Task.Run(write);
        }
        finally
        {
            done.Cancel();
        }

        await reader;
    }
}
