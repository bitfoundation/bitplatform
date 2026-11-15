using Microsoft.Extensions.AI;

namespace Bit.BlazorUI;

/// <summary>
/// A transport that sends each request to an <see cref="IChatClient"/> where the components run - a Blazor Server
/// circuit, a Hybrid app, or a WebAssembly app whose chat client calls a model directly.
/// </summary>
/// <remarks>
/// <para>
/// Registered through <see cref="BitAiChatClientBuilderExtensions.UseChatClient"/>, it is
/// the whole integration: the mapping both ways is <see cref="BitAiChatClientExtensions"/>'.
/// </para>
/// <para>
/// A WebAssembly app keeps its keys off the browser by streaming from its server instead
/// (<see cref="BitAiSseTransport"/>, <see cref="BitAiWebSocketTransport"/> or <c>BitAiSignalRTransport</c>), where the same
/// mapping runs.
/// </para>
/// </remarks>
public sealed class BitAiChatClientTransport : IBitAiTransport
{
    private readonly IChatClient _chatClient;
    private readonly ChatOptions? _options;

    /// <summary>Creates the transport.</summary>
    /// <param name="chatClient">The chat client every request goes to.</param>
    /// <param name="options">The chat options every request is sent with, which win over what a request asks for.</param>
    public BitAiChatClientTransport(IChatClient chatClient, ChatOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        _chatClient = chatClient;
        _options = options;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken = default) =>
        _chatClient.StreamBitAiEventsAsync(request, _options, cancellationToken);
}
