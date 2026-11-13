using System.Runtime.CompilerServices;

namespace Boilerplate.LandingPage.Client.Components.Chat;

/// <summary>
/// Carries each question to the server and its answer back, in the wire format of Bit.BlazorUI.AI: over a WebSocket of its
/// own in the browser (the server's MapBitAiWebSocket), or straight from the server's <see cref="ILandingChatBackend"/>
/// under Blazor Server. Internal, like every type here built on a type of the lazily loaded assemblies, since the router
/// reads every public type of the assembly at startup.
/// </summary>
internal sealed class LandingAiTransport(string siteKey, Uri endpoint, ILandingChatBackend? backend) : IBitAiTransport
{
    private readonly BitAiWebSocketTransport? socket = backend is null ? new(endpoint) : null;

    public IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken = default)
    {
        // The sample and the language travel with the conversation, so the answer is about the right sample, in the visitor's language
        var question = new BitAiRequest(request.Messages)
        {
            ConversationId = request.ConversationId,
            Options = request.Options,
            Tools = request.Tools,
            Context = new Dictionary<string, object?>
            {
                [LandingChatContext.Site] = siteKey,
                [LandingChatContext.Culture] = CultureInfo.CurrentUICulture.Name
            }
        };

        return socket is not null ? socket.StreamAsync(question, cancellationToken) : StreamFromBackend(question, cancellationToken);
    }

    private async IAsyncEnumerable<BitAiEvent> StreamFromBackend(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var json = BitAiWireSerializer.SerializeRequest(await BitAiWireSerializer.ToSnapshotAsync(request, cancellationToken: cancellationToken));

        await foreach (var item in backend!.StreamAsync(json, cancellationToken))
        {
            // An event of a type this version doesn't know is skipped, as the socket skips it
            if (BitAiWireSerializer.Deserialize(item) is { } evt)
                yield return evt;
        }
    }
}
