using Bit.BlazorUI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Server.Api.Features.Chatbot;
using Boilerplate.Server.Api.Infrastructure.SignalR;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// The AI chat panel, as far as the server can tell: the BitAiChatSession <c>AppAiChat</c> drives, with the options it
/// sets, whose requests reach <see cref="AppChatbot.Reply"/> and whose events come back in Bit.BlazorUI.AI's wire format -
/// what BitAiSignalRTransport and BitAiHub do over the app's SignalR connection, minus the connection. So the
/// <see cref="Conversation"/> a test reads is the one the panel would show and keep, and the next question goes up with
/// it as the panel would send it, signatures and statuses included.
/// <para>
/// Everything here is real except the model (See <see cref="TestChatClient"/>): the seeded system prompt, the
/// SupportAgent with its real tools, the signer and the rate limiter.
/// </para>
/// </summary>
public sealed class TestAiChatPanel : IAsyncDisposable
{
    public const string ConnectionId = "test-connection-id";

    private readonly BitAiChatSession session;

    public TestAiChatPanel(IServiceProvider scopedServices, ClaimsPrincipal user, BitAiConversation? conversation = null)
    {
        session = new BitAiChatSession(new Transport(scopedServices, user), conversation ?? new BitAiConversation(), new()
        {
            AutoTitle = false,
            ExecuteTools = false,
            InterruptOnSubmit = true,
            // Each piece of the answer as the server streamed it, rather than evened out over time
            Pacing = null,
            RequestOptions = new() { Extra = new Dictionary<string, object?> { [AiChatProtocol.TimeZoneId] = "UTC" } }
        });
    }

    public BitAiChatSession Session => session;

    public BitAiConversation Conversation => session.Conversation;

    /// <summary>Sends <paramref name="question"/> as the user's, and waits for the whole answer: the assistant message the panel shows.</summary>
    public async Task<BitAiMessage> Ask(string question, IReadOnlyList<BitAiAttachment>? images = null, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));

        var answer = await session.SubmitAsync(question, images ?? [], timeout.Token);

        Assert.IsNotNull(answer, "The panel turned the question away, as if an answer were still being written.");

        return answer;
    }

    public ValueTask DisposeAsync() => session.DisposeAsync();

    /// <summary>A text value the server wrote on an answer (See <see cref="AiChatProtocol"/>), as the panel holds it once it came over the wire.</summary>
    public static string? Extra(IBitAiMessage message, string key) => message.Metadata?.Extra?.GetValueOrDefault(key) switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
        _ => null
    };

    /// <summary>The user's message of a conversation the panel kept, with the images they attached by the ids they went up under.</summary>
    public static BitAiMessage UserMessage(BitAiConversation conversation, string? text, params Guid[] images)
    {
        var message = conversation.AddMessage(BitAiRole.User, text ?? "");

        foreach (var attachmentId in images)
        {
            message.AddAttachment(Image(attachmentId));
        }

        return message;
    }

    /// <summary>An answer of a conversation the panel kept, with what the server wrote on it - or, for one it never wrote, whatever the caller claims.</summary>
    public static BitAiMessage AssistantMessage(BitAiConversation conversation, string text, IReadOnlyDictionary<string, object?>? extra = null, BitAiMessageStatus? status = null)
    {
        var message = conversation.AddMessage(BitAiRole.Assistant, text);

        if (extra is not null)
        {
            message.MergeMetadata(new BitAiGenerationMetadata { Extra = extra });
        }

        if (status is { } value)
        {
            message.SetStatus(value);
        }

        return message;
    }

    /// <summary>An answer as the server signs it (See <c>AppChatbot.Signed</c>).</summary>
    public static BitAiMessage SignedAnswer(BitAiConversation conversation, IServiceProvider services, string text)
    {
        return AssistantMessage(conversation, text, new Dictionary<string, object?>
        {
            [AiChatProtocol.Answer] = text,
            [AiChatProtocol.Signature] = services.GetRequiredService<ChatbotAnswerSigner>().Sign(text)
        });
    }

    /// <summary>An image as the panel attaches it once it went up (See <c>AppAiChat.UploadImage</c>): the id it was stored under, and its address.</summary>
    public static BitAiAttachment Image(Guid attachmentId) => new(attachmentId.ToString(), "picture.webp")
    {
        ContentType = "image/webp",
        Url = $"https://localhost/api/v1/Attachment/GetAttachment/{attachmentId}/AiChatImage",
        Status = BitAiAttachmentStatus.Ready,
        Metadata = new Dictionary<string, object?> { [AiChatProtocol.AttachmentId] = attachmentId.ToString() }
    };

    /// <summary>
    /// Builds the real server with the model - and only the model - replaced. Every agent is built on the DI IChatClient
    /// (See AddAppAIAgents), so this single replacement removes the network, the api key and the non-determinism while
    /// leaving the agent itself untouched.
    /// </summary>
    public static AppTestServer BuildServerWith(TestChatClient chatClient, Action<IServiceCollection>? configureTestApiAppServices = null)
    {
        return new AppTestServer().Build(
            configureTestServices: services => services.Replace(ServiceDescriptor.Singleton<IChatClient>(chatClient)),
            configureTestApiAppServices: configureTestApiAppServices,
            configureTestConfiguration: configuration =>
            {
                // Without a chat api key neither AddChatClient nor AddAppAIAgents runs, and the keyed "SupportAgent"
                // would not exist at all. The value itself is never used: the client it builds is replaced above.
                configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-these-tests";
            });
    }

    /// <summary>
    /// <c>AppChatbot</c> reads <c>IHttpContextAccessor</c> for the <c>{{WebAppUrl}}</c> variable and the rate limit, and -
    /// under multitenancy - <c>TenantProvider</c> reads it again while the seeded system prompt is resolved.
    /// <c>AppHub.StreamAsync</c> performs this same assignment for the same reason, because Azure SignalR never populates
    /// the accessor itself.
    /// </summary>
    public static HttpContext SetCurrentHttpContext(IServiceProvider scopedServices, Uri serverAddress)
    {
        var httpContext = new DefaultHttpContext { RequestServices = scopedServices };
        httpContext.Request.Scheme = serverAddress.Scheme;
        httpContext.Request.Host = new HostString(serverAddress.Authority);

        scopedServices.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        return httpContext;
    }

    public static string Describe(IEnumerable<ChatMessage> conversation)
    {
        return string.Join(" | ", conversation.Select(message => $"{message.Role}: '{message.Text}'"));
    }

    /// <summary>BitAiSignalRTransport and BitAiHub, without the connection between them: both ends of the wire format, back to back.</summary>
    private sealed class Transport(IServiceProvider scopedServices, ClaimsPrincipal user) : IBitAiTransport
    {
        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var sent = BitAiWireSerializer.SerializeRequest(await BitAiWireSerializer.ToSnapshotAsync(request, cancellationToken: cancellationToken));

            var received = BitAiWireSerializer.ToRequest(BitAiWireSerializer.DeserializeRequest(sent), services: scopedServices);

            await foreach (var evt in scopedServices.GetRequiredService<AppChatbot>().Reply(received, ConnectionId, user, cancellationToken))
            {
                var sentBack = BitAiWireSerializer.Serialize(evt);

                yield return BitAiWireSerializer.Deserialize(sentBack) ?? throw new AssertFailedException($"The panel cannot read an event of the answer: {sentBack}");
            }
        }
    }
}
