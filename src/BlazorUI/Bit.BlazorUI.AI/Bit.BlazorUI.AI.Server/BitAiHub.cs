using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// The server end of <c>BitAiSignalRTransport</c> (Bit.BlazorUI.AI.SignalR): a hub whose streaming method reads a
/// <see cref="BitAiRequest"/> and streams the reply's events, in the package's wire format
/// (<see cref="BitAiWireSerializer"/>).
/// </summary>
/// <remarks>
/// <para>
/// Derive from it and produce the reply - typically from the app's chat client - then map it like any hub
/// (<c>app.MapHub&lt;AiHub&gt;("/hubs/ai")</c>):
/// </para>
/// <code>
/// public sealed class AiHub(IChatClient chatClient) : BitAiHub
/// {
///     protected override IAsyncEnumerable&lt;BitAiEvent&gt; StreamAsync(BitAiRequest request, CancellationToken cancellationToken) =&gt;
///         chatClient.StreamBitAiEventsAsync(request, cancellationToken: cancellationToken);
/// }
/// </code>
/// <para>
/// The request and the events cross the hub as JSON values (<see cref="JsonElement"/>) the package writes and reads
/// itself, so they need nothing of the hub protocol's settings and stay the same with any of them that carries JSON
/// (the JSON protocol, the default). An exception of <see cref="StreamAsync"/>, or an event the wire format cannot write,
/// ends the stream with a terminal <see cref="BitAiErrorEvent"/> holding a generic message, and is logged; its own text
/// never reaches the browser.
/// </para>
/// <para>
/// A decision on a tool call is taken only on a call a reply streaming on the same connection asked about (with a
/// <see cref="BitAiToolApprovalRequestedEvent"/>), and once, as the WebSocket endpoint takes it.
/// </para>
/// </remarks>
public abstract class BitAiHub : Hub
{
    /// <summary>The name of the streaming hub method.</summary>
    public const string StreamMethodName = nameof(StreamBitAiEvents);

    /// <summary>The name of the hub method taking a decision on a tool call.</summary>
    public const string ApprovalMethodName = nameof(SendBitAiApproval);

    // The key of the calls a connection was asked about, in its Items.
    private static readonly object AskedCallsKey = new();

    /// <summary>Serializer hooks, for custom parts, and the JSON settings, for the app's own types.</summary>
    protected virtual BitAiConversationSerializerOptions? SerializerOptions => null;

    /// <summary>
    /// The hub method the transport streams from. Not meant to be called by the app's own code.
    /// </summary>
    /// <param name="request">The request, in the wire format.</param>
    /// <param name="services">The services of the invocation, injected by SignalR.</param>
    /// <param name="cancellationToken">Cancelled when the browser stops the reply or goes away.</param>
    /// <returns>The events, in the wire format.</returns>
    public async IAsyncEnumerable<JsonElement> StreamBitAiEvents(
        JsonElement request,
        [BitAiFromServices] IServiceProvider services,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = SerializerOptions;

        BitAiRequest bitRequest;
        try
        {
            bitRequest = BitAiWireSerializer.ToRequest(BitAiWireSerializer.DeserializeRequest(request, options), options, services);
        }
        catch (JsonException exception)
        {
            // A request the client got wrong is the client's error, told to it as such (a HubException's message reaches
            // the caller) rather than logged as a failure of the server.
            throw new HubException($"The request is not a BitAi request: {exception.Message}");
        }

        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType().FullName ?? nameof(BitAiHub));
        var connectionCalls = AskedCalls(Context);
        var expected = new BitAiAskedCalls(services.GetService<BitAiApprovalBroker>());
        var asked = new List<string>();

        try
        {
            await foreach (var evt in BitAiServerStreams.Guard(() => StreamAsync(bitRequest, cancellationToken), logger, cancellationToken).ConfigureAwait(false))
            {
                // Recorded before it is sent: a decision is taken only on a call this connection was asked about, and the
                // broker keeps one that arrives before the reply waits for it.
                if (evt is BitAiToolApprovalRequestedEvent approval && connectionCalls.TryAdd(approval.ToolCallId, 0)) asked.Add(approval.ToolCallId);
                expected.Record(evt);

                var written = BitAiServerStreams.TryWrite(evt, e => BitAiWireSerializer.SerializeToElement(e, options), logger, out var json);
                yield return json;
                if (!written) yield break;
            }
        }
        finally
        {
            foreach (var toolCallId in asked) connectionCalls.TryRemove(toolCallId, out _);
            expected.Forget();
        }
    }

    /// <summary>
    /// The hub method the transport sends a decision on a tool call to. Not meant to be called by the app's own code.
    /// </summary>
    /// <param name="response">The decision, in the wire format.</param>
    /// <param name="services">The services of the invocation, injected by SignalR.</param>
    /// <returns>Whether a held call was waiting for it.</returns>
    /// <remarks>
    /// Only a decision on a call a reply streaming on this very connection asked about is acted on, and once: one on any
    /// other call is answered <see langword="false"/>, so a client can never decide a call of someone else's reply.
    /// </remarks>
    public async Task<bool> SendBitAiApproval(JsonElement response, [BitAiFromServices] IServiceProvider services)
    {
        BitAiApprovalResponse decision;
        try
        {
            decision = BitAiWireSerializer.DeserializeApproval(response, SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new HubException($"The message is not a BitAi decision: {exception.Message}");
        }

        if (!AskedCalls(Context).TryRemove(decision.ToolCallId, out _)) return false;

        return await OnApprovalAsync(decision, services, Context.ConnectionAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Produces the reply to a request.
    /// </summary>
    /// <param name="request">The request, with the services of the hub invocation as its <see cref="BitAiRequest.Services"/>.</param>
    /// <param name="cancellationToken">Cancelled when the browser stops the reply or goes away.</param>
    /// <returns>The events of the reply.</returns>
    protected abstract IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Acts on a decision on a tool call this server holds. By default, hands it to the app's
    /// <see cref="BitAiApprovalBroker"/>, when one is registered. Called only for a call a reply streaming on the same
    /// connection asked about, and once per call.
    /// </summary>
    /// <param name="response">The decision.</param>
    /// <param name="services">The services of the invocation.</param>
    /// <param name="cancellationToken">Cancelled when the connection goes away.</param>
    /// <returns>Whether a held call was waiting for it.</returns>
    protected virtual ValueTask<bool> OnApprovalAsync(BitAiApprovalResponse response, IServiceProvider services, CancellationToken cancellationToken) =>
        ValueTask.FromResult(services.GetService<BitAiApprovalBroker>()?.TrySubmit(response) ?? false);

    // The calls the replies streaming on a connection asked about, kept with the connection: a hub instance lives for one
    // invocation, and the decision comes in an invocation of its own.
    private static ConcurrentDictionary<string, byte> AskedCalls(HubCallerContext context)
    {
        var items = context.Items;
        lock (items)
        {
            if (items.TryGetValue(AskedCallsKey, out var existing) && existing is ConcurrentDictionary<string, byte> calls) return calls;

            calls = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
            items[AskedCallsKey] = calls;
            return calls;
        }
    }

}

/// <summary>
/// Marks a hub method parameter SignalR resolves from the services of the invocation. The package's own rather than
/// MVC's <c>[FromServices]</c>: a reference to an MVC assembly makes every Blazor WebAssembly app referencing the package
/// generate an application part attribute it cannot compile.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class BitAiFromServicesAttribute : Attribute, IFromServiceMetadata;
