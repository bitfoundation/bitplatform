using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Wire;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

/// <summary>
/// The <c>Microsoft.Extensions.AI</c> adapter: a chat client's reply as events, and a request as chat messages and
/// options.
/// </summary>
[TestClass]
public class BitAiChatClientAdapterTests
{
    private static readonly JsonElement SearchArguments = JsonDocument.Parse("""{"q":"blazor"}""").RootElement.Clone();

    private static async Task<List<BitAiEvent>> Collect(IAsyncEnumerable<BitAiEvent> events)
    {
        var list = new List<BitAiEvent>();
        await foreach (var evt in events) list.Add(evt);
        return list;
    }

    private static BitAiRequest Request(params IBitAiMessage[] messages) => new(messages);

    private static BitAiMessage User(string text)
    {
        var message = new BitAiMessage(BitAiRole.User);
        message.AddPart(new BitAiTextPart(text));
        return message;
    }

    [TestMethod]
    public async Task AReplyOpensStreamsAndCloses()
    {
        var client = new BitAiFakeChatClient(
            BitAiFakeChatClient.Text("Hello "),
            new ChatResponseUpdate(ChatRole.Assistant, "world") { ModelId = "gpt-test", FinishReason = ChatFinishReason.Length });

        var events = await Collect(client.StreamBitAiEventsAsync(Request(User("hi"))));

        Assert.IsInstanceOfType<BitAiMessageStartedEvent>(events[0]);
        Assert.AreEqual("Hello world", string.Concat(events.OfType<BitAiTextDeltaEvent>().Select(e => e.Delta)));
        Assert.AreEqual("gpt-test", events.OfType<BitAiMetadataEvent>().Single().Metadata.ModelId);
        Assert.AreEqual(BitAiFinishReason.Length, ((BitAiMessageCompletedEvent)events[^1]).FinishReason);
    }

    [TestMethod]
    public async Task EveryContentKindMapsToItsEvent()
    {
        var client = new BitAiFakeChatClient(
            BitAiFakeChatClient.Of(new TextReasoningContent("Let me think") { ProtectedData = "sig" }),
            BitAiFakeChatClient.Of(new FunctionCallContent("call1", "search", new Dictionary<string, object?> { ["q"] = "blazor" })),
            BitAiFakeChatClient.Of(new FunctionResultContent("call1", JsonDocument.Parse("""{"hits":3}""").RootElement.Clone())),
            BitAiFakeChatClient.Of(new DataContent(new byte[] { 1, 2, 3 }, "image/png")),
            BitAiFakeChatClient.Of(new UriContent(new Uri("https://example.com/report.pdf"), "application/pdf")),
            BitAiFakeChatClient.Of(new UsageContent(new UsageDetails { InputTokenCount = 5, OutputTokenCount = 7 })),
            BitAiFakeChatClient.Of(new ErrorContent("partial failure") { ErrorCode = "e1" }),
            BitAiFakeChatClient.Of(new TextContent("See the docs.")
            {
                Annotations = [new CitationAnnotation { Title = "Docs", Url = new Uri("https://example.com/docs"), Snippet = "quote" }],
            }));

        var events = await Collect(client.StreamBitAiEventsAsync(Request(User("hi"))));

        var reasoning = events.OfType<BitAiReasoningDeltaEvent>().Single();
        Assert.AreEqual("Let me think", reasoning.Delta);
        Assert.AreEqual("sig", reasoning.Signature);

        var call = events.OfType<BitAiToolCallStartedEvent>().Single();
        Assert.AreEqual("call1", call.ToolCallId);
        Assert.AreEqual("blazor", call.Arguments!.Value.GetProperty("q").GetString());

        var result = events.OfType<BitAiToolResultEvent>().Single();
        Assert.IsTrue(result.Outcome.IsSuccess);
        Assert.AreEqual(3, result.Outcome.Value!.Value.GetProperty("hits").GetInt32());

        Assert.StartsWith("data:image/png;base64,", events.OfType<BitAiImageEvent>().Single().Url);

        var file = events.OfType<BitAiFileEvent>().Single().Attachment;
        Assert.AreEqual("report.pdf", file.Name);
        Assert.AreEqual("https://example.com/report.pdf", file.Url);
        Assert.AreEqual(BitAiAttachmentKind.Document, file.Kind);

        var usage = events.OfType<BitAiUsageUpdatedEvent>().Single().Usage;
        Assert.AreEqual(5, usage.InputTokens);
        Assert.AreEqual(7, usage.OutputTokens);

        var error = events.OfType<BitAiErrorEvent>().Single();
        Assert.IsFalse(error.IsTerminal);
        Assert.AreEqual("e1", error.Error.Code);

        var citation = events.OfType<BitAiCitationEvent>().Single().Citation;
        Assert.AreEqual("https://example.com/docs", citation.Source.Url);
        Assert.AreEqual("quote", citation.Quote);
    }

    [TestMethod]
    public async Task TheUsageOfEveryModelCallAddsUp()
    {
        var client = new BitAiFakeChatClient(
            BitAiFakeChatClient.Of(new UsageContent(new UsageDetails { InputTokenCount = 5, OutputTokenCount = 1 })),
            BitAiFakeChatClient.Of(new UsageContent(new UsageDetails { InputTokenCount = 8, OutputTokenCount = 2 })));

        var events = await Collect(client.StreamBitAiEventsAsync(Request(User("hi"))));

        var last = events.OfType<BitAiUsageUpdatedEvent>().Last().Usage;
        Assert.AreEqual(13, last.InputTokens);
        Assert.AreEqual(3, last.OutputTokens);
    }

    [TestMethod]
    public async Task ACallStreamedTwiceIsReportedOnce()
    {
        var call = new FunctionCallContent("call1", "search", new Dictionary<string, object?>());
        var client = new BitAiFakeChatClient(BitAiFakeChatClient.Of(call), BitAiFakeChatClient.Of(call));

        var events = await Collect(client.StreamBitAiEventsAsync(Request(User("hi"))));

        Assert.AreEqual(1, events.OfType<BitAiToolCallStartedEvent>().Count());
    }

    [TestMethod]
    public async Task ACallWithoutArgumentsStartsWithAnEmptyObject()
    {
        var client = new BitAiFakeChatClient(
            BitAiFakeChatClient.Of(new FunctionCallContent("call1", "now", arguments: null)),
            BitAiFakeChatClient.Of(new FunctionCallContent("call2", "search", arguments: null) { Exception = new JsonException("bad arguments") }));

        var events = await Collect(client.StreamBitAiEventsAsync(Request(User("hi"))));

        foreach (var call in events.OfType<BitAiToolCallStartedEvent>())
        {
            Assert.AreEqual(JsonValueKind.Object, call.Arguments?.ValueKind, call.ToolCallId);
        }
        Assert.AreEqual(2, events.OfType<BitAiToolCallStartedEvent>().Count());
    }

    [TestMethod]
    public async Task AnAddressKeepsItsEscapes()
    {
        const string signed = "https://bucket.example.com/a%20b.png?X-Amz-Credential=AKIA%2F2026%2Fs3%2Faws4_request&sig=a%2Bb";
        const string file = "https://bucket.example.com/r%2Fport.pdf?sig=a%2Bb";
        var client = new BitAiFakeChatClient(
            BitAiFakeChatClient.Of(new UriContent(new Uri(signed), "image/png")),
            BitAiFakeChatClient.Of(new UriContent(new Uri(file), "application/pdf")),
            BitAiFakeChatClient.Of(new TextContent("x") { Annotations = [new CitationAnnotation { Url = new Uri(signed) }] }));

        var events = await Collect(client.StreamBitAiEventsAsync(Request(User("hi"))));

        Assert.AreEqual(signed, events.OfType<BitAiImageEvent>().Single().Url);
        Assert.AreEqual(file, events.OfType<BitAiFileEvent>().Single().Attachment.Url);
        Assert.AreEqual(signed, events.OfType<BitAiCitationEvent>().Single().Citation.Source.Url);
    }

    [TestMethod]
    public async Task ACallNothingAnsweredIsAnsweredForTheModel()
    {
        var assistant = new BitAiMessage(BitAiRole.Assistant);
        var stopped = assistant.AddPart(new BitAiToolCallPart("stopped", "search"));
        stopped.CompleteArguments(SearchArguments);
        stopped.SetStatus(BitAiToolCallStatus.Cancelled);
        var rejected = assistant.AddPart(new BitAiToolCallPart("rejected", "delete"));
        rejected.CompleteArguments(SearchArguments);
        rejected.SetStatus(BitAiToolCallStatus.Rejected);
        var answeredLater = assistant.AddPart(new BitAiToolCallPart("later", "search"));
        answeredLater.CompleteArguments(SearchArguments);

        var tool = new BitAiMessage(BitAiRole.User);
        tool.AddPart(new BitAiToolResultPart("later", BitAiToolOutcome.FromText("found")));

        var messages = await BitAiChatClientExtensions.ToChatMessagesAsync([User("go"), assistant, tool, User("again")]);

        var results = messages.SelectMany(m => m.Contents.OfType<FunctionResultContent>()).ToList();
        CollectionAssert.AreEquivalent(new[] { "stopped", "rejected", "later" }, results.Select(r => r.CallId).ToArray(), "Every call has one result, none twice.");
        StringAssert.Contains((string)results.Single(r => r.CallId == "stopped").Result!, "cancelled");
        StringAssert.Contains((string)results.Single(r => r.CallId == "rejected").Result!, "rejected");
    }

    [TestMethod]
    public async Task AFailedFunctionIsAFailedOutcome()
    {
        var client = new BitAiFakeChatClient(BitAiFakeChatClient.Of(new FunctionResultContent("call1", null) { Exception = new InvalidOperationException("nope") }));

        var events = await Collect(client.StreamBitAiEventsAsync(Request(User("hi"))));

        var outcome = events.OfType<BitAiToolResultEvent>().Single().Outcome;
        Assert.IsFalse(outcome.IsSuccess);
        Assert.AreEqual(BitAiErrorKind.Tool, outcome.Error!.Kind);
    }

    [TestMethod]
    public async Task TheTranscriptMapsToChatMessages()
    {
        var user = User("Find blazor");
        user.AddAttachment(new BitAiAttachment("a1", "notes.txt") { ContentType = "text/plain", Data = new byte[] { 65 } });
        user.AddAttachment(new BitAiAttachment("a2", "big.bin") { ContentFactory = _ => ValueTask.FromResult<Stream>(new MemoryStream([7, 8])) });
        user.AddAttachment(new BitAiAttachment("a3", "photo.png") { ContentType = "image/png", Url = "https://example.com/photo.png" });

        var assistant = new BitAiMessage(BitAiRole.Assistant);
        assistant.AddPart(new BitAiReasoningPart("hmm") { Signature = "sig" });
        assistant.AddPart(new BitAiTextPart("Searching."));
        var call = assistant.AddPart(new BitAiToolCallPart("call1", "search"));
        call.CompleteArguments(SearchArguments);
        call.SetResult(BitAiToolOutcome.FromText("3 hits"));

        var messages = await BitAiChatClientExtensions.ToChatMessagesAsync([user, assistant]);

        Assert.AreEqual(3, messages.Count);

        Assert.AreEqual(ChatRole.User, messages[0].Role);
        Assert.AreEqual(user.MessageId, messages[0].MessageId);
        Assert.AreEqual("Find blazor", messages[0].Contents.OfType<TextContent>().Single().Text);
        var data = messages[0].Contents.OfType<DataContent>().ToList();
        Assert.AreEqual(2, data.Count);
        Assert.AreEqual("notes.txt", data[0].Name);
        CollectionAssert.AreEqual(new byte[] { 7, 8 }, data[1].Data.ToArray());
        Assert.AreEqual("https://example.com/photo.png", messages[0].Contents.OfType<UriContent>().Single().Uri.ToString());

        Assert.AreEqual(ChatRole.Assistant, messages[1].Role);
        Assert.AreEqual("sig", messages[1].Contents.OfType<TextReasoningContent>().Single().ProtectedData);
        var functionCall = messages[1].Contents.OfType<FunctionCallContent>().Single();
        Assert.AreEqual("call1", functionCall.CallId);
        Assert.AreEqual("blazor", ((JsonElement)functionCall.Arguments!["q"]!).GetString());

        Assert.AreEqual(ChatRole.Tool, messages[2].Role);
        var functionResult = messages[2].Contents.OfType<FunctionResultContent>().Single();
        Assert.AreEqual("call1", functionResult.CallId);
        Assert.AreEqual("3 hits", ((JsonElement)functionResult.Result!).GetString());
    }

    [TestMethod]
    public void TheAppsOptionsWinOverTheRequests()
    {
        var request = new BitAiRequest([User("hi")])
        {
            Options = new BitAiRequestOptions
            {
                ModelId = "expensive-model",
                Temperature = 0.9,
                TopP = 0.5,
                MaxOutputTokens = 100,
                SystemPrompt = "Be a pirate",
                ToolChoice = BitAiToolChoice.Specific("search"),
                ResponseFormat = BitAiResponseFormat.Json,
                Extra = new Dictionary<string, object?> { ["apiKey"] = "x" },
            },
        };

        var options = BitAiChatClientExtensions.ToChatOptions(request, new ChatOptions { ModelId = "cheap-model", Instructions = "Be helpful" });

        Assert.AreEqual("cheap-model", options.ModelId);
        Assert.AreEqual("Be helpful", options.Instructions);
        Assert.AreEqual(0.9f, options.Temperature);
        Assert.AreEqual(0.5f, options.TopP);
        Assert.AreEqual(100, options.MaxOutputTokens);
        Assert.AreEqual("search", ((RequiredChatToolMode)options.ToolMode!).RequiredFunctionName);
        Assert.AreSame(ChatResponseFormat.Json, options.ResponseFormat);
        Assert.IsNull(options.AdditionalProperties);
    }

    [TestMethod]
    public void TheRequestsToolsAreDeclaredUnlessTheAppHasThem()
    {
        var serverTool = AIFunctionFactory.Create(() => "x", "search");
        var request = new BitAiRequest([User("hi")])
        {
            Tools = [new BitAiToolDefinition("search"), new BitAiToolDefinition("pick_color") { Description = "Picks a color" }],
        };

        var appOptions = new ChatOptions { Tools = [serverTool] };
        var options = BitAiChatClientExtensions.ToChatOptions(request, appOptions);

        Assert.AreEqual(2, options.Tools!.Count);
        Assert.AreSame(serverTool, options.Tools[0]);
        var declaration = (AIFunctionDeclaration)options.Tools[1];
        Assert.AreEqual("pick_color", declaration.Name);
        Assert.IsNotInstanceOfType<AIFunction>(declaration, "A browser tool is declared, never invoked on the server.");
        Assert.AreEqual(1, appOptions.Tools.Count, "The app's options are not changed.");
    }

    [TestMethod]
    public async Task WhatFollowsACallWithItsResultGoesAfterThatResult()
    {
        // The reply of a function invoking client: a round with server-run calls, then the model's answer.
        var assistant = new BitAiMessage(BitAiRole.Assistant);
        assistant.AddPart(new BitAiReasoningPart("look it up") { Signature = "sig1" });
        assistant.AddPart(new BitAiTextPart("Searching."));
        var first = assistant.AddPart(new BitAiToolCallPart("call1", "search"));
        first.CompleteArguments(SearchArguments);
        first.SetResult(BitAiToolOutcome.FromText("3 hits"));
        var second = assistant.AddPart(new BitAiToolCallPart("call2", "search"));
        second.CompleteArguments(SearchArguments);
        second.SetResult(BitAiToolOutcome.FromText("1 hit"));
        assistant.AddPart(new BitAiReasoningPart("got them") { Signature = "sig2" });
        assistant.AddPart(new BitAiTextPart("Found 4."));

        var messages = await BitAiChatClientExtensions.ToChatMessagesAsync([User("go"), assistant]);

        CollectionAssert.AreEqual(
            new[] { ChatRole.User, ChatRole.Assistant, ChatRole.Tool, ChatRole.Assistant },
            messages.Select(m => m.Role).ToArray());

        Assert.AreEqual("sig1", messages[1].Contents.OfType<TextReasoningContent>().Single().ProtectedData);
        CollectionAssert.AreEqual(new[] { "call1", "call2" }, messages[1].Contents.OfType<FunctionCallContent>().Select(c => c.CallId).ToArray(),
            "The calls of one round stay in one message.");
        Assert.IsFalse(messages[1].Contents.OfType<TextContent>().Any(t => t.Text == "Found 4."), "The answer is not replayed ahead of the results it read.");

        CollectionAssert.AreEqual(new[] { "call1", "call2" }, messages[2].Contents.OfType<FunctionResultContent>().Select(r => r.CallId).ToArray());

        Assert.AreEqual("sig2", messages[3].Contents.OfType<TextReasoningContent>().Single().ProtectedData);
        Assert.AreEqual("Found 4.", messages[3].Contents.OfType<TextContent>().Single().Text);
        Assert.AreEqual(assistant.MessageId, messages[3].MessageId);
    }

    [TestMethod]
    [DataRow("/uploads/a.png")]
    [DataRow("uploads/a.png")]
    [DataRow("//cdn.example.com/a.png")]
    [DataRow("file:///etc/passwd")]
    [DataRow(@"C:\uploads\a.png")]
    [DataRow(@"\\server\share\a.png")]
    public async Task AnAddressTheProviderCannotFetchIsLeftOutOnEveryPlatform(string url)
    {
        var user = User("look");
        user.AddPart(new BitAiImagePart(url));
        user.AddAttachment(new BitAiAttachment("a1", "a.png") { ContentType = "image/png", Url = url });

        var messages = await BitAiChatClientExtensions.ToChatMessagesAsync([user]);

        Assert.AreEqual("look", messages.Single().Contents.OfType<TextContent>().Single().Text);
        Assert.IsFalse(messages.Single().Contents.OfType<UriContent>().Any(), url);
    }

    [TestMethod]
    public async Task AnAbsoluteAddressReachesTheProvider()
    {
        var user = User("look");
        user.AddAttachment(new BitAiAttachment("a1", "a.png") { ContentType = "image/png", Url = "https://example.com/a.png" });
        user.AddAttachment(new BitAiAttachment("a2", "b.pdf") { ContentType = "application/pdf", Url = "gs://bucket/b.pdf" });

        var messages = await BitAiChatClientExtensions.ToChatMessagesAsync([user]);

        CollectionAssert.AreEqual(
            new[] { "https://example.com/a.png", "gs://bucket/b.pdf" },
            messages.Single().Contents.OfType<UriContent>().Select(u => u.Uri.ToString()).ToArray());
    }

    [TestMethod]
    public async Task TheChatClientTransportDrivesASession()
    {
        var client = new BitAiFakeChatClient(BitAiFakeChatClient.Text("Hi "), BitAiFakeChatClient.Text("there"));

        await using var session = new BitAiChatSession(new BitAiChatClientTransport(client), null, new BitAiChatSessionOptions { Pacing = null });
        await session.SubmitAsync("hello");

        Assert.AreEqual("Hi there", session.Conversation.Messages[1].GetText());
        Assert.AreEqual("hello", client.Messages[0].Single().Text);
    }

    [TestMethod]
    public async Task ABrowserToolIsRunByTheSessionAndItsResultSentBack()
    {
        var client = new BitAiFakeChatClient((call, messages) => call == 0
            ? [new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("c1", "pick_color", new Dictionary<string, object?>())]) { FinishReason = ChatFinishReason.ToolCalls }]
            : [BitAiFakeChatClient.Text("You picked " + ((JsonElement)messages[^1].Contents.OfType<FunctionResultContent>().Single().Result!).GetString())]);

        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(new BitAiToolDefinition("pick_color"), (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("red"))));

        await using var session = new BitAiChatSession(new BitAiChatClientTransport(client), null, new BitAiChatSessionOptions { Pacing = null }, registry);
        await session.SubmitAsync("pick one");

        Assert.AreEqual(2, client.Messages.Count);
        Assert.AreEqual("pick_color", ((AIFunctionDeclaration)client.Options[0]!.Tools!.Single()).Name);
        StringAssert.EndsWith(session.Conversation.Messages[1].GetText(), "You picked red");
    }
}
