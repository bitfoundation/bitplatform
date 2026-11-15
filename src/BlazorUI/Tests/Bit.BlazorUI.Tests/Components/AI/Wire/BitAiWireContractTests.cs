using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Wire;

/// <summary>
/// Pins the JSON of every built-in event: the wire format is a contract between a server and the browsers of every
/// version of the package talking to it, so a change here is a change of the format (a new property is fine; a renamed
/// or removed one breaks the older readers).
/// </summary>
[TestClass]
public class BitAiWireContractTests
{
    private static readonly Dictionary<string, string> Expected = new()
    {
        ["message.started"] = """
            {"type":"message.started","timestamp":"2026-01-02T03:04:05+00:00","messageId":"m1","role":"assistant"}
            """,
        ["message.completed"] = """
            {"type":"message.completed","timestamp":"2026-01-02T03:04:05+00:00","finishReason":"toolCalls"}
            """,
        ["part.started"] = """
            {"type":"part.started","timestamp":"2026-01-02T03:04:05+00:00","partId":"p1","kind":"code","attributes":{"language":"csharp"}}
            """,
        ["part.completed"] = """
            {"type":"part.completed","timestamp":"2026-01-02T03:04:05+00:00","partId":"p1"}
            """,
        ["text.delta"] = """
            {"type":"text.delta","timestamp":"2026-01-02T03:04:05+00:00","partId":"p2","delta":"Hello \u0022world\u0022\n"}
            """,
        ["reasoning.delta"] = """
            {"type":"reasoning.delta","timestamp":"2026-01-02T03:04:05+00:00","delta":"Thinking","signature":"sig"}
            """,
        ["status"] = """
            {"type":"status","timestamp":"2026-01-02T03:04:05+00:00","status":"Searching the web"}
            """,
        ["usage.updated"] = """
            {"type":"usage.updated","timestamp":"2026-01-02T03:04:05+00:00","usage":{"inputTokens":10,"outputTokens":20,"cachedInputTokens":3,"reasoningTokens":4,"cost":0.5,"currency":"USD"}}
            """,
        ["metadata"] = """
            {"type":"metadata","timestamp":"2026-01-02T03:04:05+00:00","metadata":{"modelId":"gpt","providerId":"openai","usage":{},"finishReason":"stop","temperature":0.2}}
            """,
        ["error"] = """
            {"type":"error","timestamp":"2026-01-02T03:04:05+00:00","error":{"message":"Too many requests","code":"429","kind":"rateLimit","isRetryable":true,"retryAfter":"00:00:05"},"isTerminal":false}
            """,
        ["custom"] = """
            {"type":"custom","timestamp":"2026-01-02T03:04:05+00:00","name":"app.progress","data":{"step":2}}
            """,
        ["citation"] = """
            {"type":"citation","timestamp":"2026-01-02T03:04:05+00:00","citation":{"citationId":"c1","source":{"sourceId":"s1","title":"Docs","kind":"web","url":"https://example.com"},"quote":"q","number":1}}
            """,
        ["follow_ups"] = """
            {"type":"follow_ups","timestamp":"2026-01-02T03:04:05+00:00","followUps":["Tell me more","Why?"]}
            """,
        ["image"] = """
            {"type":"image","timestamp":"2026-01-02T03:04:05+00:00","partId":"img","url":"https://example.com/a.png","alt":"A chart","width":640,"height":480}
            """,
        ["file"] = """
            {"type":"file","timestamp":"2026-01-02T03:04:05+00:00","attachment":{"attachmentId":"f1","name":"report.pdf","contentType":"application/pdf","size":3,"kind":"document","status":"ready","data":"AQID"}}
            """,
        ["artifact.created"] = """
            {"type":"artifact.created","timestamp":"2026-01-02T03:04:05+00:00","artifactId":"a1","kind":"code","title":"Program.cs","language":"csharp","content":"//","url":"https://example.com/a1","metadata":{"lines":1}}
            """,
        ["artifact.delta"] = """
            {"type":"artifact.delta","timestamp":"2026-01-02T03:04:05+00:00","artifactId":"a1","delta":"class A {}"}
            """,
        ["artifact.completed"] = """
            {"type":"artifact.completed","timestamp":"2026-01-02T03:04:05+00:00","artifactId":"a1","summary":"Added A"}
            """,
        ["agent.run.started"] = """
            {"type":"agent.run.started","timestamp":"2026-01-02T03:04:05+00:00","runId":"r1","agentName":"Researcher","goal":"Find it"}
            """,
        ["agent.plan"] = """
            {"type":"agent.plan","timestamp":"2026-01-02T03:04:05+00:00","runId":"r1","steps":[{"stepId":"s1","title":"Search","detail":"the web"},{"stepId":"s2","title":"Summarize"}]}
            """,
        ["agent.step.started"] = """
            {"type":"agent.step.started","timestamp":"2026-01-02T03:04:05+00:00","runId":"r1","stepId":"s1","title":"Search"}
            """,
        ["agent.step.completed"] = """
            {"type":"agent.step.completed","timestamp":"2026-01-02T03:04:05+00:00","runId":"r1","stepId":"s1","status":"failed","summary":"No luck","error":{"message":"Offline","kind":"network","isRetryable":false}}
            """,
        ["agent.run.completed"] = """
            {"type":"agent.run.completed","timestamp":"2026-01-02T03:04:05+00:00","runId":"r1","status":"cancelled"}
            """,
        ["agent.input_requested"] = """
            {"type":"agent.input_requested","timestamp":"2026-01-02T03:04:05+00:00","runId":"r1","prompt":"Which one?","suggestions":["A","B"]}
            """,
        ["structured.delta"] = """
            {"type":"structured.delta","timestamp":"2026-01-02T03:04:05+00:00","partId":"sd","jsonDelta":"{\u0022a\u0022:","schemaName":"Thing"}
            """,
        ["structured"] = """
            {"type":"structured","timestamp":"2026-01-02T03:04:05+00:00","partId":"sd","value":{"a":1},"schemaName":"Thing"}
            """,
        ["generative_ui"] = """
            {"type":"generative_ui","timestamp":"2026-01-02T03:04:05+00:00","partId":"ui","componentName":"WeatherCard","props":{"city":"Paris"},"isPartial":true}
            """,
        ["generative_ui.delta"] = """
            {"type":"generative_ui.delta","timestamp":"2026-01-02T03:04:05+00:00","partId":"ui","componentName":"WeatherCard","propsDelta":"{\u0022ci"}
            """,
        ["tool_call.started"] = """
            {"type":"tool_call.started","timestamp":"2026-01-02T03:04:05+00:00","toolCallId":"t1","toolName":"search","arguments":{"q":"x"},"definition":{"name":"search","title":"Search","risk":"write","requiresApproval":true}}
            """,
        ["tool_call.delta"] = """
            {"type":"tool_call.delta","timestamp":"2026-01-02T03:04:05+00:00","toolCallId":"t1","argumentsDelta":"{\u0022q\u0022:"}
            """,
        ["tool_call.completed"] = """
            {"type":"tool_call.completed","timestamp":"2026-01-02T03:04:05+00:00","toolCallId":"t1","arguments":{"q":"x"}}
            """,
        ["tool_call.progress"] = """
            {"type":"tool_call.progress","timestamp":"2026-01-02T03:04:05+00:00","toolCallId":"t1","progress":{"fraction":0.5,"message":"Half way"}}
            """,
        ["tool_call.result"] = """
            {"type":"tool_call.result","timestamp":"2026-01-02T03:04:05+00:00","toolCallId":"t1","outcome":{"isSuccess":true,"value":{"hits":2},"text":"2 hits","duration":"00:00:01.5000000"}}
            """,
        ["tool_call.approval_requested"] = """
            {"type":"tool_call.approval_requested","timestamp":"2026-01-02T03:04:05+00:00","toolCallId":"t1","reason":"Deletes files","allowApproveAlways":false}
            """,
    };

    [TestMethod]
    public void EveryEventIsPinned()
    {
        CollectionAssert.AreEquivalent(BitAiWireSamples.All.Select(e => e.Type).ToArray(), Expected.Keys.ToArray());
    }

    [TestMethod]
    public void EveryEventIsWrittenAsPinned()
    {
        foreach (var evt in BitAiWireSamples.All)
        {
            Assert.AreEqual(Expected[evt.Type], BitAiWireSerializer.Serialize(evt), evt.Type);
        }
    }

    [TestMethod]
    public void EveryPinnedEventIsRead()
    {
        foreach (var (type, json) in Expected)
        {
            var evt = BitAiWireSerializer.Deserialize(json);

            Assert.IsNotNull(evt, type);
            Assert.AreEqual(type, evt.Type);
            Assert.AreEqual(json, BitAiWireSerializer.Serialize(evt), type);
        }
    }
}
