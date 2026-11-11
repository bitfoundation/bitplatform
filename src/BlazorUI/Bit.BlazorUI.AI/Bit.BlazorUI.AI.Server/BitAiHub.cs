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
/// (the JSON protocol, the default). An exception of <see cref="StreamAsync"/> ends the stream with a terminal
/// <see cref="BitAiErrorEvent"/> holding a generic message, and is logged; its own text never reaches the browser.
/// </para>
/// </remarks>
public abstract class BitAiHub : Hub
{
    /// <summary>The name of the streaming hub method.</summary>
    public const string StreamMethodName = nameof(StreamBitAiEvents);

    /// <summary>The name of the hub method taking a decision on a tool call.</summary>
    public const string ApprovalMethodName = nameof(SendBitAiApproval);

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

        await foreach (var evt in BitAiServerStreams.Guard(() => StreamAsync(bitRequest, cancellationToken), logger, cancellationToken).ConfigureAwait(false))
        {
            yield return BitAiWireSerializer.SerializeToElement(evt, options);
        }
    }

    /// <summary>
    /// The hub method the transport sends a decision on a tool call to. Not meant to be called by the app's own code.
    /// </summary>
    /// <param name="response">The decision, in the wire format.</param>
    /// <param name="services">The services of the invocation, injected by SignalR.</param>
    /// <returns>Whether a held call was waiting for it.</returns>
    public async Task<bool> SendBitAiApproval(JsonElement response, [BitAiFromServices] IServiceProvider services) =>
        await OnApprovalAsync(BitAiWireSerializer.DeserializeApproval(response, SerializerOptions), services, Context.ConnectionAborted).ConfigureAwait(false);

    /// <summary>
    /// Produces the reply to a request.
    /// </summary>
    /// <param name="request">The request, with the services of the hub invocation as its <see cref="BitAiRequest.Services"/>.</param>
    /// <param name="cancellationToken">Cancelled when the browser stops the reply or goes away.</param>
    /// <returns>The events of the reply.</returns>
    protected abstract IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Acts on a decision on a tool call this server holds. By default, hands it to the app's
    /// <see cref="BitAiApprovalBroker"/>, when one is registered.
    /// </summary>
    /// <param name="response">The decision.</param>
    /// <param name="services">The services of the invocation.</param>
    /// <param name="cancellationToken">Cancelled when the connection goes away.</param>
    /// <returns>Whether a held call was waiting for it.</returns>
    protected virtual ValueTask<bool> OnApprovalAsync(BitAiApprovalResponse response, IServiceProvider services, CancellationToken cancellationToken) =>
        ValueTask.FromResult(services.GetService<BitAiApprovalBroker>()?.TrySubmit(response) ?? false);
}

/// <summary>
/// Marks a hub method parameter SignalR resolves from the services of the invocation. The package's own rather than
/// MVC's <c>[FromServices]</c>: a reference to an MVC assembly makes every Blazor WebAssembly app referencing the package
/// generate an application part attribute it cannot compile.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class BitAiFromServicesAttribute : Attribute, IFromServiceMetadata;
