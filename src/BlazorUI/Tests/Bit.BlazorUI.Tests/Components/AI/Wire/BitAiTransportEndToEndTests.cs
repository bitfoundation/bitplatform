using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Wire;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

/// <summary>
/// The SSE and SignalR transports against their server ends hosted in memory (the WebSocket one is
/// <see cref="BitAiWebSocketTransportTests"/>): a chat client's reply reaches a chat
/// session with no mapping written by hand, and a decision on a call the server holds gets back to it.
/// </summary>
[TestClass]
public class BitAiTransportEndToEndTests
{
    internal static BitAiChatSessionOptions Options => new() { Pacing = null, ExecuteTools = false };

    private static async Task<WebApplication> Host(IChatClient chatClient, Action<WebApplication> map)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(chatClient);
        builder.Services.AddSingleton<BitAiApprovalBroker>();
        builder.Services.AddSignalR();

        var app = builder.Build();
        map(app);
        await app.StartAsync();
        return app;
    }

    private static BitAiSseTransport Sse(WebApplication app) =>
        new(app.GetTestClient(), new BitAiSseTransportOptions { Endpoint = "/api/ai/chat" });

    private static void MapSse(WebApplication app)
    {
        app.MapBitAiSse("/api/ai/chat", (request, http, ct) =>
            http.RequestServices.GetRequiredService<IChatClient>().StreamBitAiEventsAsync(request, cancellationToken: ct));
        app.MapBitAiApprovals("/api/ai/chat/approvals");
    }

    private static async Task<HubConnection> Connect(WebApplication app)
    {
        var server = app.GetTestServer();
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "/hubs/ai"), o =>
            {
                o.Transports = HttpTransportType.LongPolling;
                o.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();
        await connection.StartAsync();
        return connection;
    }

    [TestMethod]
    public async Task AChatClientsReplyStreamsOverSse()
    {
        var chatClient = new BitAiFakeChatClient(BitAiFakeChatClient.Text("Hello "), BitAiFakeChatClient.Text("from the server"));
        await using var app = await Host(chatClient, MapSse);

        await using var session = new BitAiChatSession(Sse(app), null, Options);
        await session.SubmitAsync("hi");

        Assert.AreEqual("Hello from the server", session.Conversation.Messages[1].GetText());
        Assert.AreEqual(BitAiMessageStatus.Complete, session.Conversation.Messages[1].Status);
        Assert.AreEqual("hi", chatClient.Messages.Single().Single().Text);
    }

    [TestMethod]
    public async Task AFailureOnTheServerIsATerminalErrorWithoutItsDetails()
    {
        var chatClient = new BitAiFakeChatClient(BitAiFakeChatClient.Text("Partial")) { Throw = new InvalidOperationException("connection string=secret") };
        await using var app = await Host(chatClient, MapSse);

        await using var session = new BitAiChatSession(Sse(app), null, Options);
        await session.SubmitAsync("hi");

        Assert.AreEqual("server_error", session.LastError?.Code);
        Assert.IsFalse(session.LastError!.Message.Contains("secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ABodyThatIsNotARequestIsABadRequest()
    {
        await using var app = await Host(new BitAiFakeChatClient(), MapSse);

        using var response = await app.GetTestClient().PostAsync("/api/ai/chat", new StringContent("{\"nope\":1}", Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ARequestTheServerCannotRebuildIsABadRequest()
    {
        await using var app = await Host(new BitAiFakeChatClient(), MapSse);

        using var response = await app.GetTestClient().PostAsync("/api/ai/chat",
            new StringContent("""{"messages":[{"messageId":"m","parts":[{"partId":"p","kind":"tool-call","toolCallId":"","toolName":"x"}]}]}""", Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task AHandlerThrowingAsItIsCalledIsATerminalError()
    {
        await using var app = await Host(new BitAiFakeChatClient(), a =>
            a.MapBitAiSse("/api/ai/chat", (_, _, _) => throw new InvalidOperationException("No IChatClient: secret")));

        await using var session = new BitAiChatSession(Sse(app), null, Options);
        await session.SubmitAsync("hi");

        Assert.AreEqual("server_error", session.LastError?.Code);
        Assert.IsFalse(session.LastError!.Message.Contains("secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ASuccessThatIsNotAnEventStreamFailsTheTurn()
    {
        // An expired sign-in redirected to a login page the client followed.
        await using var app = await Host(new BitAiFakeChatClient(), a => a.MapPost("/api/ai/chat", async http =>
        {
            http.Response.ContentType = "text/html";
            await http.Response.WriteAsync("<html><body>Sign in</body></html>");
        }));

        var transport = Sse(app);
        var failure = await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
        {
            await foreach (var _ in transport.StreamAsync(new BitAiRequest([]))) { }
        });

        StringAssert.Contains(failure.Message, "text/html");
    }

    [TestMethod]
    public async Task AnEventOfANewerServerIsSkipped()
    {
        await using var app = await Host(new BitAiFakeChatClient(), a => a.MapPost("/api/ai/chat", async http =>
        {
            http.Response.ContentType = "text/event-stream";
            await http.Response.WriteAsync(
                "data: {\"type\":\"text.delta\",\"delta\":\"a\"}\n\n" +
                "event: hologram\ndata: {\"type\":\"hologram\",\"beam\":1}\n\n" +
                ": a comment\n\n" +
                "data: {\"type\":\"text.delta\",\"delta\":\"b\"}\n\n");
        }));

        await using var session = new BitAiChatSession(Sse(app), null, Options);
        await session.SubmitAsync("hi");

        Assert.AreEqual("ab", session.Conversation.Messages[1].GetText());
        Assert.IsNull(session.LastError);
    }

    [TestMethod]
    public async Task ADecisionReachesTheCallTheServerHoldsOverSse()
    {
        await using var app = await Host(new BitAiFakeChatClient(), a =>
        {
            a.MapBitAiSse("/api/ai/chat", (_, http, ct) => HeldCall(http.RequestServices.GetRequiredService<BitAiApprovalBroker>(), ct));
            a.MapBitAiApprovals("/api/ai/chat/approvals");
        });

        await AssertHeldCallIsDecided(Sse(app));
    }

    [TestMethod]
    public async Task ADecisionPostedBeforeTheServerWaitsIsKeptForIt()
    {
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var app = await Host(new BitAiFakeChatClient(), a =>
        {
            a.MapBitAiSse("/api/ai/chat", (_, http, ct) => HeldCall(http.RequestServices.GetRequiredService<BitAiApprovalBroker>(), ct, waiting.Task));
            a.MapBitAiApprovals("/api/ai/chat/approvals");
        });

        var client = app.GetTestClient();
        var events = new List<BitAiEvent>();
        await foreach (var evt in Sse(app).StreamAsync(new BitAiRequest([])))
        {
            events.Add(evt);
            if (evt is not BitAiToolApprovalRequestedEvent) continue;

            // The handler has not got to WaitAsync yet: the decision is kept for it rather than refused.
            using var answer = await client.PostAsync("/api/ai/chat/approvals", new StringContent(
                BitAiWireSerializer.SerializeApproval(new BitAiApprovalResponse("t1", "delete_file", BitAiApprovalDecision.Approved)),
                Encoding.UTF8,
                "application/json"));
            Assert.AreEqual(HttpStatusCode.NoContent, answer.StatusCode);

            waiting.SetResult();
        }

        Assert.AreEqual("Deleted after approved", events.OfType<BitAiToolResultEvent>().Single().Outcome.GetDisplayText());
        Assert.AreEqual(0, app.Services.GetRequiredService<BitAiApprovalBroker>().PendingCount);
    }

    [TestMethod]
    public async Task ADecisionOnACallNobodyAskedAboutIsNotFound()
    {
        await using var app = await Host(new BitAiFakeChatClient(), MapSse);

        using var answer = await app.GetTestClient().PostAsync("/api/ai/chat/approvals", new StringContent(
            BitAiWireSerializer.SerializeApproval(new BitAiApprovalResponse("t1", "delete_file", BitAiApprovalDecision.Approved)),
            Encoding.UTF8,
            "application/json"));

        Assert.AreEqual(HttpStatusCode.NotFound, answer.StatusCode);
    }

    [TestMethod]
    [DataRow("/api/ai/chat", "text/plain")]
    [DataRow("/api/ai/chat", "application/x-www-form-urlencoded")]
    [DataRow("/api/ai/chat/approvals", "text/plain")]
    public async Task ABodyThatIsNotJsonIsNotRead(string path, string contentType)
    {
        // What a page of another site can post without the browser asking the server first.
        var chatClient = new BitAiFakeChatClient(BitAiFakeChatClient.Text("Hi"));
        await using var app = await Host(chatClient, MapSse);

        var body = path.EndsWith("approvals", StringComparison.Ordinal)
            ? BitAiWireSerializer.SerializeApproval(new BitAiApprovalResponse("t1", "delete_file", BitAiApprovalDecision.Approved))
            : """{"messages":[{"messageId":"m1","parts":[{"partId":"p1","kind":"text","text":"hi"}]}]}""";
        using var response = await app.GetTestClient().PostAsync(path, new StringContent(body, Encoding.UTF8, contentType));

        Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.AreEqual(0, chatClient.Messages.Count, "The model was never called.");
    }

    [TestMethod]
    public async Task AnEventTheWireFormatCannotWriteIsATerminalErrorOverSse()
    {
        await using var app = await Host(new BitAiFakeChatClient(), a => a.MapBitAiSse("/api/ai/chat", (_, _, _) => Unwritable()));

        await using var session = new BitAiChatSession(Sse(app), null, Options);
        await session.SubmitAsync("hi");

        Assert.AreEqual("server_error", session.LastError?.Code);
        Assert.AreEqual("Before", session.Conversation.Messages[1].GetText());
    }

    [TestMethod]
    public async Task TheApprovalEndpointKeepsTheStreamsQueryString()
    {
        string? tenant = null;
        await using var app = await Host(new BitAiFakeChatClient(), a =>
        {
            a.MapBitAiSse("/api/ai/chat", (_, http, ct) => HeldCall(http.RequestServices.GetRequiredService<BitAiApprovalBroker>(), ct));
            a.MapBitAiApprovals("/api/ai/chat/approvals", (response, http, _) =>
            {
                tenant = http.Request.Query["tenant"];
                return ValueTask.FromResult(http.RequestServices.GetRequiredService<BitAiApprovalBroker>().TrySubmit(response));
            });
        });

        await AssertHeldCallIsDecided(new BitAiSseTransport(app.GetTestClient(), new BitAiSseTransportOptions { Endpoint = "/api/ai/chat?tenant=42" }));

        Assert.AreEqual("42", tenant);
    }

    [TestMethod]
    public async Task AChatClientsReplyStreamsOverSignalR()
    {
        var chatClient = new BitAiFakeChatClient(BitAiFakeChatClient.Text("Hello "), BitAiFakeChatClient.Text("over SignalR"));
        await using var app = await Host(chatClient, a => a.MapHub<ChatClientHub>("/hubs/ai"));
        await using var connection = await Connect(app);

        await using var session = new BitAiChatSession(new BitAiSignalRTransport(connection), null, Options);
        await session.SubmitAsync("hi");

        Assert.AreEqual("Hello over SignalR", session.Conversation.Messages[1].GetText());
        Assert.AreEqual(BitAiMessageStatus.Complete, session.Conversation.Messages[1].Status);
    }

    [TestMethod]
    public async Task AFailureInTheHubIsATerminalErrorWithoutItsDetails()
    {
        var chatClient = new BitAiFakeChatClient(BitAiFakeChatClient.Text("Partial")) { Throw = new InvalidOperationException("secret") };
        await using var app = await Host(chatClient, a => a.MapHub<ChatClientHub>("/hubs/ai"));
        await using var connection = await Connect(app);

        await using var session = new BitAiChatSession(new BitAiSignalRTransport(connection), null, Options);
        await session.SubmitAsync("hi");

        Assert.AreEqual("server_error", session.LastError?.Code);
        Assert.IsFalse(session.LastError!.Message.Contains("secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task AHubThrowingAsItIsCalledIsATerminalError()
    {
        await using var app = await Host(new BitAiFakeChatClient(), a => a.MapHub<ThrowingHub>("/hubs/ai"));
        await using var connection = await Connect(app);

        await using var session = new BitAiChatSession(new BitAiSignalRTransport(connection), null, Options);
        await session.SubmitAsync("hi");

        Assert.AreEqual("server_error", session.LastError?.Code);
        Assert.IsFalse(session.LastError!.Message.Contains("secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ADecisionReachesTheCallTheServerHoldsOverSignalR()
    {
        await using var app = await Host(new BitAiFakeChatClient(), a => a.MapHub<HeldCallHub>("/hubs/ai"));
        await using var connection = await Connect(app);

        await AssertHeldCallIsDecided(new BitAiSignalRTransport(connection));
    }

    [TestMethod]
    public async Task AnEventTheWireFormatCannotWriteIsATerminalErrorOverSignalR()
    {
        await using var app = await Host(new BitAiFakeChatClient(), a => a.MapHub<UnwritableHub>("/hubs/ai"));
        await using var connection = await Connect(app);

        await using var session = new BitAiChatSession(new BitAiSignalRTransport(connection), null, Options);
        await session.SubmitAsync("hi");

        Assert.AreEqual("server_error", session.LastError?.Code);
        Assert.AreEqual("Before", session.Conversation.Messages[1].GetText());
    }

    [TestMethod]
    public async Task AConnectionDecidesOnlyTheCallsItWasAskedAbout()
    {
        await using var app = await Host(new BitAiFakeChatClient(), a => a.MapHub<HeldCallHub>("/hubs/ai"));
        await using var owner = await Connect(app);
        await using var other = await Connect(app);
        var decision = System.Text.Json.JsonSerializer.SerializeToElement(new { toolCallId = "t1", toolName = "delete_file", decision = "approved" });

        var transport = new BitAiSignalRTransport(owner);
        var events = new List<BitAiEvent>();
        await foreach (var evt in transport.StreamAsync(new BitAiRequest([])))
        {
            events.Add(evt);
            if (evt is not BitAiToolApprovalRequestedEvent) continue;

            Assert.IsFalse(await other.InvokeAsync<bool>(BitAiHub.ApprovalMethodName, decision), "Another connection cannot decide the call.");
            await Assert.ThrowsAsync<HubException>(() => other.InvokeAsync<bool>(BitAiHub.ApprovalMethodName, System.Text.Json.JsonSerializer.SerializeToElement(new { nope = 1 })));

            Assert.IsTrue(await owner.InvokeAsync<bool>(BitAiHub.ApprovalMethodName, decision));
            Assert.IsFalse(await owner.InvokeAsync<bool>(BitAiHub.ApprovalMethodName, decision), "A call is decided once.");
        }

        Assert.AreEqual("Deleted after approved", events.OfType<BitAiToolResultEvent>().Single().Outcome.GetDisplayText());
    }

    [TestMethod]
    public async Task TheBrokerLeavesNothingBehindACancelledWait()
    {
        var broker = new BitAiApprovalBroker();
        using var cts = new CancellationTokenSource();

        var wait = broker.WaitAsync("t1", cts.Token);
        Assert.AreEqual(1, broker.PendingCount);
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => wait);
        Assert.AreEqual(0, broker.PendingCount);
        Assert.IsFalse(broker.TrySubmit(new BitAiApprovalResponse("t1", "x", BitAiApprovalDecision.Approved)));
    }

    internal static async Task AssertHeldCallIsDecided(IBitAiTransport transport)
    {
        await using var session = new BitAiChatSession(transport, null, Options);
        // A call the server holds is shown awaiting the decision (ApprovalRequested is for the calls the session runs).
        var awaiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.StateChanged += () =>
        {
            if (session.Conversation.Messages.Count == 2
                && session.Conversation.Messages[1].FindToolCall("t1") is { Status: BitAiToolCallStatus.AwaitingApproval })
            {
                awaiting.TrySetResult();
            }
        };

        var turn = session.SubmitAsync("delete it");
        await awaiting.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.IsTrue(session.Approve("t1", BitAiApprovalDecision.Approved));
        await turn.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreEqual("Deleted after approved", session.Conversation.Messages[1].FindToolCall("t1")!.Result!.GetDisplayText());
    }

    // An event of an app type the wire format has no metadata for, after one it writes.
    internal static async IAsyncEnumerable<BitAiEvent> Unwritable()
    {
        await Task.Yield();
        yield return new BitAiTextDeltaEvent("Before");
        yield return new BitAiCustomEvent("app.point", new AppPoint(1, 2));
        yield return new BitAiTextDeltaEvent("After");
    }

    internal static async IAsyncEnumerable<BitAiEvent> HeldCall(
        BitAiApprovalBroker approvals,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        Task? beforeWaiting = null)
    {
        yield return new BitAiToolCallStartedEvent("t1", "delete_file") { Arguments = System.Text.Json.JsonDocument.Parse("{}").RootElement.Clone() };
        yield return new BitAiToolApprovalRequestedEvent("t1");

        if (beforeWaiting is not null) await beforeWaiting.WaitAsync(cancellationToken);
        var decision = await approvals.WaitAsync("t1", cancellationToken);

        yield return new BitAiToolResultEvent("t1", BitAiToolOutcome.FromText($"Deleted after {decision.ToString().ToLowerInvariant()}"));
        yield return new BitAiMessageCompletedEvent();
    }

    private sealed class ChatClientHub(IChatClient chatClient) : BitAiHub
    {
        protected override IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken) =>
            chatClient.StreamBitAiEventsAsync(request, cancellationToken: cancellationToken);
    }

    private sealed class ThrowingHub : BitAiHub
    {
        protected override IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("No IChatClient: secret");
    }

    private sealed class UnwritableHub : BitAiHub
    {
        protected override IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken) => Unwritable();
    }

    private sealed class HeldCallHub(BitAiApprovalBroker approvals) : BitAiHub
    {
        protected override IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken) =>
            HeldCall(approvals, cancellationToken);
    }
}
