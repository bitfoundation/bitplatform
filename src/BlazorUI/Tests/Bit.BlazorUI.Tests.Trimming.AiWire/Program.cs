using Bit.BlazorUI;
using Bit.BlazorUI.Tests.Components.AI.Wire;

// Every built-in event, written, read back and written again: the same JSON both times, and the same type.
var failed = 0;
foreach (var evt in BitAiWireSamples.All)
{
    var json = BitAiWireSerializer.Serialize(evt);
    var back = BitAiWireSerializer.Deserialize(json);
    if (back is null || back.GetType() != evt.GetType() || BitAiWireSerializer.Serialize(back) != json)
    {
        failed++;
        Console.WriteLine($"FAILED {evt.Type}");
    }
}

// An event of a newer producer is skipped, not a failure.
if (BitAiWireSerializer.Deserialize("""{"type":"from.the.future","x":1}""") is not null)
{
    failed++;
    Console.WriteLine("FAILED unknown type");
}

// A request, read into live messages, with a tool choice (which has no public constructor) and a tool call.
var request = BitAiWireSerializer.ToRequest(BitAiWireSerializer.DeserializeRequest("""
    {"conversationId":"c1","messages":[
      {"messageId":"m1","role":"user","parts":[{"partId":"p1","kind":"text","text":"hi"}]},
      {"messageId":"m2","role":"assistant","parts":[{"partId":"p2","kind":"tool-call","toolCallId":"t1","toolName":"search","arguments":{"q":"x"},"toolStatus":"completed","outcome":{"isSuccess":true,"text":"found"}}]}],
     "options":{"temperature":0.2,"toolChoice":{"mode":"specific","toolName":"search"},"responseFormat":{"kind":"json"}},
     "tools":[{"name":"search","risk":"write"}]}
    """));

if (request.Messages[0].GetText() != "hi"
    || ((BitAiMessage)request.Messages[1]).FindToolCall("t1")?.Result?.GetDisplayText() != "found"
    || request.Options?.ToolChoice?.ToolName != "search"
    || request.Options.ResponseFormat?.Kind is not BitAiResponseFormatKind.Json
    || request.Tools[0].Risk is not BitAiToolRisk.Write)
{
    failed++;
    Console.WriteLine("FAILED request");
}

Console.WriteLine($"{BitAiWireSamples.All.Count} events round-tripped, {failed} failed");
return failed;
