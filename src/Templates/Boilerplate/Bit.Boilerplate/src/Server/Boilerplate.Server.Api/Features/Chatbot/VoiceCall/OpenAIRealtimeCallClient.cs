using System.Net.WebSockets;
using System.Text.Json.Nodes;
using System.Runtime.CompilerServices;

namespace Boilerplate.Server.Api.Features.Chatbot.VoiceCall;

/// <summary>
/// OpenAI Realtime calls: created from the browser's offer with the server's key, then joined over a sideband WebSocket
/// (https://developers.openai.com/api/docs/guides/voice-server-controls). Virtual, so tests can stand in for the provider.
/// </summary>
public class OpenAIRealtimeCallClient(ServerApiSettings appSettings, IHttpClientFactory httpClientFactory)
{
    private OpenAIOptions Options => appSettings.AI?.OpenAI ?? throw new InvalidOperationException("The AI:OpenAI configuration section is required.");

    public virtual async Task<RealtimeCall> CreateCall(string offerSdp, JsonObject session, string safetyIdentifier, CancellationToken cancellationToken)
    {
        using MultipartFormDataContent form = new();

        var sdp = new StringContent(offerSdp);
        sdp.Headers.ContentType = new("application/sdp");
        form.Add(sdp, "sdp");

        var sessionContent = new StringContent(session.ToJsonString());
        sessionContent.Headers.ContentType = new("application/json");
        form.Add(sessionContent, "session");

        using HttpRequestMessage request = new(HttpMethod.Post, Url("realtime/calls")) { Content = form };
        request.Headers.Authorization = new("Bearer", Options.RealtimeApiKey);
        request.Headers.Add("OpenAI-Safety-Identifier", safetyIdentifier);

        using var response = await httpClientFactory.CreateClient("AI").SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode is false)
            throw new InvalidOperationException($"The realtime provider refused the call ({(int)response.StatusCode}): {body}");

        // The call id is the last segment of the Location header.
        var callId = response.Headers.Location?.OriginalString.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
            ?? throw new InvalidOperationException("The realtime provider answered without a call id.");

        return new(body, callId);
    }

    public virtual async Task<IRealtimeSideband> ConnectSideband(string callId, CancellationToken cancellationToken)
    {
        var endpoint = Url($"realtime?call_id={Uri.EscapeDataString(callId)}");

        var url = new UriBuilder(endpoint)
        {
            Scheme = endpoint.Scheme == Uri.UriSchemeHttp ? Uri.UriSchemeWs : Uri.UriSchemeWss,
            Port = endpoint.IsDefaultPort ? -1 : endpoint.Port
        };

        var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Authorization", $"Bearer {Options.RealtimeApiKey}");

        try
        {
            await socket.ConnectAsync(url.Uri, cancellationToken);

            return new WebSocketSideband(socket);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public virtual async Task HangUp(string callId, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, Url($"realtime/calls/{Uri.EscapeDataString(callId)}/hangup"));
        request.Headers.Authorization = new("Bearer", Options.RealtimeApiKey);

        // Not checked: an already ended call answers with an error.
        using var _ = await httpClientFactory.CreateClient("AI").SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Has the provider validate the session (key, model, voice) without starting a call. Used by <see cref="RealtimeHealthCheck"/>.
    /// </summary>
    public virtual async Task CreateClientSecret(JsonObject session, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["expires_after"] = new JsonObject { ["anchor"] = "created_at", ["seconds"] = 10 },
            ["session"] = session.DeepClone()
        };

        using HttpRequestMessage request = new(HttpMethod.Post, Url("realtime/client_secrets"))
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new("Bearer", Options.RealtimeApiKey);

        using var response = await httpClientFactory.CreateClient("AI").SendAsync(request, cancellationToken);

        // The success body holds the secret, so only an error body is read.
        if (response.IsSuccessStatusCode is false)
            throw new InvalidOperationException($"The realtime provider refused the session ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}");
    }

    private Uri Url(string relativeUrl)
        => new($"{(Options.RealtimeEndpoint ?? new Uri("https://api.openai.com/v1")).AbsoluteUri.TrimEnd('/')}/{relativeUrl}");

    private sealed class WebSocketSideband(ClientWebSocket socket) : IRealtimeSideband
    {
        /// <summary>A WebSocket allows one send at a time.</summary>
        private readonly SemaphoreSlim sendLock = new(1, 1);

        public async Task Send(JsonObject clientEvent, CancellationToken cancellationToken)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(clientEvent.ToJsonString());

            await sendLock.WaitAsync(cancellationToken);

            try
            {
                await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            }
            finally
            {
                sendLock.Release();
            }
        }

        public async IAsyncEnumerable<JsonObject> ReadEvents([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var buffer = new byte[16 * 1024];
            using MemoryStream message = new();

            while (socket.State is WebSocketState.Open)
            {
                var received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (received.MessageType is WebSocketMessageType.Close) yield break;

                message.Write(buffer, 0, received.Count);

                if (received.EndOfMessage is false) continue;

                var serverEvent = JsonNode.Parse(message.GetBuffer().AsSpan(0, (int)message.Length)) as JsonObject;
                message.SetLength(0);

                if (serverEvent is not null)
                    yield return serverEvent;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (socket.State is WebSocketState.Open)
            {
                // Bounded: a provider that never answers the close would keep the call's scope alive.
                using var closeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, closeTimeout.Token);
                }
                catch (Exception exp) when (exp is WebSocketException or OperationCanceledException)
                {
                    // Already closed by the provider, or no answer in time; Dispose below aborts it.
                }
            }

            socket.Dispose();
            sendLock.Dispose();
        }
    }
}

public record RealtimeCall(string AnswerSdp, string CallId);

/// <summary>The server's own connection to a call.</summary>
public interface IRealtimeSideband : IAsyncDisposable
{
    Task Send(JsonObject clientEvent, CancellationToken cancellationToken);

    IAsyncEnumerable<JsonObject> ReadEvents(CancellationToken cancellationToken);
}
