using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Wire;

/// <summary>
/// The WebSocket transport against its server end, on a real Kestrel listening on a loopback port (the in-memory test
/// server hands out a WebSocket of its own, where the transport opens a ClientWebSocket): a chat client's reply reaches a
/// chat session, a decision travels back up the socket the call came down on and nowhere else, and closing the socket
/// stops the handler.
/// </summary>
[TestClass]
public class BitAiWebSocketTransportTests
{
    private const string Path = "/api/ai/ws";

    private static BitAiChatSessionOptions Options => BitAiTransportEndToEndTests.Options;

    private static async Task<WebApplication> Host(Action<WebApplication> map, IChatClient? chatClient = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(chatClient ?? new BitAiFakeChatClient());
        builder.Services.AddSingleton<BitAiApprovalBroker>();

        var app = builder.Build();
        map(app);
        await app.StartAsync();
        return app;
    }

    private static Uri Address(WebApplication app, string scheme = "http") =>
        new UriBuilder(new Uri(app.Urls.First())) { Scheme = scheme, Path = Path }.Uri;

    private static void MapChatClient(WebApplication app) =>
        app.MapBitAiWebSocket(Path, (request, http, ct) =>
            http.RequestServices.GetRequiredService<IChatClient>().StreamBitAiEventsAsync(request, cancellationToken: ct));

    [TestMethod]
    public async Task AChatClientsReplyStreamsOverAWebSocket()
    {
        var chatClient = new BitAiFakeChatClient(BitAiFakeChatClient.Text("Hello "), BitAiFakeChatClient.Text("over a WebSocket"));
        await using var app = await Host(MapChatClient, chatClient);

        await using var session = new BitAiChatSession(new BitAiWebSocketTransport(Address(app)), null, Options);
        await session.SubmitAsync("hi");

        Assert.IsNull(session.LastError);
        Assert.AreEqual("Hello over a WebSocket", session.Conversation.Messages[1].GetText());
        Assert.AreEqual(BitAiMessageStatus.Complete, session.Conversation.Messages[1].Status);
        Assert.AreEqual("hi", chatClient.Messages.Single().Single().Text);
    }

    [TestMethod]
    public async Task ALongReplyArrivesWhole()
    {
        // An event larger than one receive of either end, so both reassemble it from fragments.
        var text = new string('x', 200_000);
        await using var app = await Host(MapChatClient, new BitAiFakeChatClient(BitAiFakeChatClient.Text(text)));

        await using var session = new BitAiChatSession(new BitAiWebSocketTransport(Address(app)), null, Options);
        await session.SubmitAsync(new string('y', 100_000));

        Assert.IsNull(session.LastError);
        Assert.AreEqual(text, session.Conversation.Messages[1].GetText());
    }

    [TestMethod]
    public async Task AFailureOnTheServerIsATerminalErrorWithoutItsDetails()
    {
        var chatClient = new BitAiFakeChatClient(BitAiFakeChatClient.Text("Partial")) { Throw = new InvalidOperationException("connection string=secret") };
        await using var app = await Host(MapChatClient, chatClient);

        await using var session = new BitAiChatSession(new BitAiWebSocketTransport(Address(app)), null, Options);
        await session.SubmitAsync("hi");

        Assert.AreEqual("server_error", session.LastError?.Code);
        Assert.IsFalse(session.LastError!.Message.Contains("secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ADecisionReachesTheCallTheServerHoldsOverAWebSocket()
    {
        await using var app = await Host(a => a.MapBitAiWebSocket(Path, (_, http, ct) =>
            BitAiTransportEndToEndTests.HeldCall(http.RequestServices.GetRequiredService<BitAiApprovalBroker>(), ct)));

        await BitAiTransportEndToEndTests.AssertHeldCallIsDecided(new BitAiWebSocketTransport(Address(app)));
    }

    [TestMethod]
    public async Task ADecisionSentBeforeTheServerWaitsIsKeptForIt()
    {
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var taken = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var app = await Host(a => a.MapBitAiWebSocket(
            Path,
            (_, http, ct) => BitAiTransportEndToEndTests.HeldCall(http.RequestServices.GetRequiredService<BitAiApprovalBroker>(), ct, waiting.Task),
            new BitAiWebSocketEndpointOptions
            {
                OnApproval = (response, http, _) =>
                {
                    taken.TrySetResult(http.RequestServices.GetRequiredService<BitAiApprovalBroker>().TrySubmit(response));
                    return ValueTask.FromResult(true);
                },
            }));
        var broker = app.Services.GetRequiredService<BitAiApprovalBroker>();

        var transport = new BitAiWebSocketTransport(Address(app));
        var events = new List<BitAiEvent>();
        await foreach (var evt in transport.StreamAsync(new BitAiRequest([])))
        {
            events.Add(evt);
            if (evt is not BitAiToolApprovalRequestedEvent) continue;

            // Sent while the handler has not got to WaitAsync yet: the broker keeps it for the handler.
            await transport.SendApprovalAsync(new BitAiApprovalResponse("t1", "delete_file", BitAiApprovalDecision.Approved));
            Assert.IsTrue(await taken.Task.WaitAsync(TimeSpan.FromSeconds(10)));
            waiting.SetResult();
        }

        Assert.AreEqual("Deleted after approved", events.OfType<BitAiToolResultEvent>().Single().Outcome.GetDisplayText());
        Assert.AreEqual(0, broker.PendingCount);
    }

    [TestMethod]
    public async Task AnEventTheWireFormatCannotWriteIsATerminalError()
    {
        await using var app = await Host(a => a.MapBitAiWebSocket(Path, (_, _, _) => BitAiTransportEndToEndTests.Unwritable()));

        await using var session = new BitAiChatSession(new BitAiWebSocketTransport(Address(app)), null, Options);
        await session.SubmitAsync("hi");

        Assert.AreEqual("server_error", session.LastError?.Code);
        Assert.AreEqual("Before", session.Conversation.Messages[1].GetText());
    }

    [TestMethod]
    public async Task AMessageOfOneReadLargerThanTheLimitFailsTheTurn()
    {
        // 4 KB is below the 16 KB a single receive reads, so the event arrives whole in one read.
        await using var app = await Host(MapChatClient, new BitAiFakeChatClient(BitAiFakeChatClient.Text(new string('x', 10_000))));

        await using var session = new BitAiChatSession(
            new BitAiWebSocketTransport(Address(app), new BitAiWebSocketTransportOptions { MaxMessageSize = 4096 }),
            null,
            Options);
        await session.SubmitAsync("hi");

        Assert.IsNotNull(session.LastError);
        Assert.AreNotEqual(10_000, session.Conversation.Messages[1].GetText().Length);
    }

    [TestMethod]
    public async Task ADecisionOnACallTheReplyDidNotAskAboutIsDropped()
    {
        var decided = new List<string>();
        var asked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var app = await Host(a => a.MapBitAiWebSocket(Path, (_, _, ct) => AskThenWait(asked, release, ct), new BitAiWebSocketEndpointOptions
        {
            OnApproval = (response, _, _) =>
            {
                lock (decided) decided.Add(response.ToolCallId);
                if (response.ToolCallId == "t1") release.TrySetResult();
                return ValueTask.FromResult(true);
            },
        }));

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(Address(app, "ws"), CancellationToken.None);
        await SendAsync(socket, BitAiWireSerializer.SerializeRequest(BitAiWireSerializer.ToSnapshot(new BitAiRequest([]))));
        await asked.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Another reply's call, then this one's twice: only the first decision on this reply's call is taken.
        await SendAsync(socket, BitAiWireSerializer.SerializeApproval(new BitAiApprovalResponse("someone-elses", "x", BitAiApprovalDecision.Approved)));
        await SendAsync(socket, BitAiWireSerializer.SerializeApproval(new BitAiApprovalResponse("t1", "x", BitAiApprovalDecision.Approved)));
        await SendAsync(socket, BitAiWireSerializer.SerializeApproval(new BitAiApprovalResponse("t1", "x", BitAiApprovalDecision.Rejected)));

        await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await DrainAsync(socket);

        CollectionAssert.AreEqual(new[] { "t1" }, decided);
    }

    [TestMethod]
    public async Task StoppingTheReplyCancelsTheHandler()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var app = await Host(a => a.MapBitAiWebSocket(Path, (_, _, ct) => UntilCancelled(started, cancelled, ct)));

        var transport = new BitAiWebSocketTransport(Address(app));
        using var stop = new CancellationTokenSource();
        var reading = Task.Run(async () =>
        {
            await foreach (var _ in transport.StreamAsync(new BitAiRequest([]), stop.Token)) { }
        });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await stop.CancelAsync();

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.ThrowsAsync<OperationCanceledException>(() => reading);
    }

    [TestMethod]
    public async Task LeavingTheEnumerationEarlyCancelsTheHandler()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var app = await Host(a => a.MapBitAiWebSocket(Path, (_, _, ct) => UntilCancelled(started, cancelled, ct)));

        await foreach (var evt in new BitAiWebSocketTransport(Address(app)).StreamAsync(new BitAiRequest([])))
        {
            Assert.IsInstanceOfType<BitAiMessageStartedEvent>(evt);
            break;
        }

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [TestMethod]
    public async Task AFirstMessageThatIsNotARequestClosesTheSocketAsInvalid()
    {
        await using var app = await Host(MapChatClient);

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(Address(app, "ws"), CancellationToken.None);
        await SendAsync(socket, "not json");

        await DrainAsync(socket);

        Assert.AreEqual(WebSocketCloseStatus.InvalidPayloadData, socket.CloseStatus);
    }

    [TestMethod]
    public async Task ARequestLargerThanTheLimitClosesTheSocketAsTooBig()
    {
        await using var app = await Host(a => a.MapBitAiWebSocket(Path, (request, http, ct) => AsyncEnumerable.Empty<BitAiEvent>(), new BitAiWebSocketEndpointOptions { MaxRequestSize = 1024 }));

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(Address(app, "ws"), CancellationToken.None);
        var message = new BitAiMessage(BitAiRole.User);
        message.AddPart(new BitAiTextPart(new string('x', 4096)));
        await SendAsync(socket, BitAiWireSerializer.SerializeRequest(BitAiWireSerializer.ToSnapshot(new BitAiRequest([message]))));

        await DrainAsync(socket);

        Assert.AreEqual(WebSocketCloseStatus.MessageTooBig, socket.CloseStatus);
    }

    [TestMethod]
    public async Task TheSubprotocolIsAgreedOn()
    {
        await using var app = await Host(MapChatClient);

        using var socket = new ClientWebSocket();
        socket.Options.AddSubProtocol(BitAiWebSocketTransport.SubProtocol);
        await socket.ConnectAsync(Address(app, "ws"), CancellationToken.None);

        Assert.AreEqual(BitAiWebSocketTransport.SubProtocol, socket.SubProtocol);
    }

    [TestMethod]
    public async Task ARequestThatIsNotAWebSocketIsABadRequest()
    {
        await using var app = await Host(MapChatClient);
        using var client = new HttpClient();

        using var response = await client.GetAsync(Address(app));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ARefusedUpgradeIsAnHttpFailureWithItsStatus()
    {
        await using var app = await Host(a => a.MapGet(Path, () => Results.StatusCode(StatusCodes.Status403Forbidden)));

        var transport = new BitAiWebSocketTransport(Address(app));
        var failure = await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
        {
            await foreach (var _ in transport.StreamAsync(new BitAiRequest([]))) { }
        });

        Assert.AreEqual(HttpStatusCode.Forbidden, failure.StatusCode);
    }

    [TestMethod]
    [DataRow("http://example.com/ai", "ws://example.com/ai")]
    [DataRow("https://example.com/ai", "wss://example.com/ai")]
    [DataRow("https://example.com:8443/ai?x=1", "wss://example.com:8443/ai?x=1")]
    [DataRow("ws://example.com/ai", "ws://example.com/ai")]
    [DataRow("wss://example.com/ai", "wss://example.com/ai")]
    public void AnHttpAddressIsSpokenToAsAWebSocketOne(string address, string expected)
    {
        Assert.AreEqual(new Uri(expected), new BitAiWebSocketTransport(new Uri(address)).Endpoint);
    }

    [TestMethod]
    [DataRow("api/ai/ws")]
    [DataRow("ftp://example.com/ai")]
    public void AnAddressThatIsNotAnAbsoluteWebOneIsRefused(string address)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BitAiWebSocketTransport(new Uri(address, UriKind.RelativeOrAbsolute)));
    }

    [TestMethod]
    public void TheSignalRTransportsDefaultsAreTheHubsMethods()
    {
        // The SignalR client package cannot reference the hub (Bit.BlazorUI.AI.Server), so it writes the names out.
        Assert.AreEqual(BitAiHub.StreamMethodName, BitAiSignalRTransportOptions.DefaultStreamMethodName);
        Assert.AreEqual(BitAiHub.ApprovalMethodName, BitAiSignalRTransportOptions.DefaultApprovalMethodName);
        Assert.AreEqual(BitAiHub.StreamMethodName, new BitAiSignalRTransportOptions().StreamMethodName);
        Assert.AreEqual(BitAiHub.ApprovalMethodName, new BitAiSignalRTransportOptions().ApprovalMethodName);
    }

    [TestMethod]
    public void TheSseTransportsMediaTypeIsTheEndpoints()
    {
        Assert.AreEqual(BitAiSseEndpointRouteBuilderExtensions.EventStreamContentType, BitAiSseTransport.EventStreamContentType);
    }

    private static async IAsyncEnumerable<BitAiEvent> AskThenWait(TaskCompletionSource asked, TaskCompletionSource release, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new BitAiToolCallStartedEvent("t1", "delete_file") { Arguments = System.Text.Json.JsonDocument.Parse("{}").RootElement.Clone() };
        yield return new BitAiToolApprovalRequestedEvent("t1");
        asked.TrySetResult();

        await release.Task.WaitAsync(cancellationToken);

        // Time for the decisions that follow the first to arrive, and be dropped, before the reply ends.
        await Task.Delay(200, cancellationToken);
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> UntilCancelled(TaskCompletionSource started, TaskCompletionSource cancelled, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new BitAiMessageStartedEvent();
        started.TrySetResult();

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        finally
        {
            if (cancellationToken.IsCancellationRequested) cancelled.TrySetResult();
        }
    }

    private static Task SendAsync(ClientWebSocket socket, string json) =>
        socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);

    // Reads until the server closes, and answers the close.
    private static async Task DrainAsync(ClientWebSocket socket)
    {
        var buffer = new byte[16 * 1024];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (socket.State is WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, timeout.Token);
            if (result.MessageType is WebSocketMessageType.Close) break;
        }

        if (socket.State is WebSocketState.CloseReceived)
        {
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token);
        }
    }
}
