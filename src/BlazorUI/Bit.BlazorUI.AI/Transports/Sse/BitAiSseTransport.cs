using System.Net.Http.Headers;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// The settings of a <see cref="BitAiSseTransport"/>.
/// </summary>
public sealed record BitAiSseTransportOptions
{
    /// <summary>
    /// The address of the stream endpoint
    /// (<see cref="BitAiSseEndpointRouteBuilderExtensions.MapBitAiSse"/>), absolute or relative to the
    /// <see cref="HttpClient.BaseAddress"/>. Defaults to <c>api/ai/chat</c>.
    /// </summary>
    public string Endpoint { get; init; } = "api/ai/chat";

    /// <summary>
    /// The address of the approval endpoint
    /// (<see cref="BitAiSseEndpointRouteBuilderExtensions.MapBitAiApprovals(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder, string)"/>),
    /// or <see langword="null"/> for <see cref="Endpoint"/> with <c>/approvals</c> appended.
    /// </summary>
    public string? ApprovalEndpoint { get; init; }

    /// <summary>Serializer hooks, for custom parts, and the JSON settings, for the app's own types.</summary>
    public BitAiConversationSerializerOptions? Serializer { get; init; }

    /// <summary>
    /// Called with every request before it is sent, to add what the server asks for (an authorization header, an
    /// antiforgery token).
    /// </summary>
    public Func<HttpRequestMessage, CancellationToken, ValueTask>? ConfigureRequest { get; init; }
}

/// <summary>
/// A transport that posts each request to a server endpoint and reads the reply's events from its server-sent events
/// stream, in the package's wire format (<see cref="BitAiWireSerializer"/>).
/// </summary>
/// <remarks>
/// <para>
/// The server end is <see cref="BitAiSseEndpointRouteBuilderExtensions.MapBitAiSse"/>, typically over the app's
/// <c>IChatClient</c> (<see cref="BitAiChatClientExtensions.StreamBitAiEventsAsync"/>); the browser needs no provider
/// key and no hand-written mapping. Register it with
/// <see cref="BitAiBuilder.UseSseTransport(Func{IServiceProvider, HttpClient}, BitAiSseTransportOptions?)"/>.
/// </para>
/// <para>
/// The content of an attachment picked in the browser is read into the request
/// (<see cref="BitAiWireSerializer.ToSnapshotAsync"/>). A decision on a tool call the server holds is posted to the
/// approval endpoint. An event of a type this version of the package does not know is skipped. A response that is not a
/// success fails the turn with an <see cref="HttpRequestException"/>, which the chat session shows as its error.
/// </para>
/// <para>
/// In Blazor WebAssembly the response is read as it streams in (the request asks the browser's <c>fetch</c> for a
/// streamed body).
/// </para>
/// </remarks>
public sealed class BitAiSseTransport : IBitAiTransport, IBitAiApprovalTransport
{
    private static readonly HttpRequestOptionsKey<bool> BrowserStreamingKey = new("WebAssemblyEnableStreamingResponse");

    private readonly HttpClient _httpClient;
    private readonly BitAiSseTransportOptions _options;

    /// <summary>Creates the transport.</summary>
    /// <param name="httpClient">The client the requests are sent with.</param>
    /// <param name="options">The endpoints and the serializer settings.</param>
    public BitAiSseTransport(HttpClient httpClient, BitAiSseTransportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
        _options = options ?? new BitAiSseTransportOptions();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await BitAiWireSerializer.ToSnapshotAsync(request, _options.Serializer, cancellationToken).ConfigureAwait(false);

        using var message = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = new StringContent(BitAiWireSerializer.SerializeRequest(snapshot, _options.Serializer), Encoding.UTF8, "application/json"),
        };
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(BitAiSseEndpointRouteBuilderExtensions.EventStreamContentType));
        message.Options.Set(BrowserStreamingKey, true);
        if (_options.ConfigureRequest is { } configure) await configure(message, cancellationToken).ConfigureAwait(false);

        using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"The AI endpoint answered {(int)response.StatusCode} ({response.ReasonPhrase}).",
                inner: null,
                response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrEmpty(item.Data)) continue;
            if (BitAiWireSerializer.Deserialize(item.Data, _options.Serializer) is { } evt) yield return evt;
        }
    }

    /// <inheritdoc />
    public async ValueTask SendApprovalAsync(BitAiApprovalResponse response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        using var message = new HttpRequestMessage(HttpMethod.Post, _options.ApprovalEndpoint ?? _options.Endpoint.TrimEnd('/') + "/approvals")
        {
            Content = new StringContent(BitAiWireSerializer.SerializeApproval(response, _options.Serializer), Encoding.UTF8, "application/json"),
        };
        if (_options.ConfigureRequest is { } configure) await configure(message, cancellationToken).ConfigureAwait(false);

        using var answer = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

        // 404: no call was waiting for it any more (its stream ended), which the session treats as nothing to do.
        if (answer.StatusCode is not System.Net.HttpStatusCode.NotFound) answer.EnsureSuccessStatusCode();
    }
}
