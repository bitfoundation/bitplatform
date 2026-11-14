//+:cnd:noEmit
using System.Runtime.CompilerServices;
namespace Boilerplate.Server.Api.Infrastructure.SignalR;

/// <summary>
/// The AI chat panel's end of the hub. <see cref="AppHub"/> is a BitAiHub of Bit.BlazorUI.AI.Server, so the panel's
/// BitAiSignalRTransport streams each answer from it over the app's own SignalR connection, in the library's wire format:
/// the conversation goes up as a BitAiRequest with every message, and the answer comes back as BitAiEvents - the text as
/// the model writes it, the tools it calls, the products and forms they show. Stopping the answer in the panel cancels the
/// stream, and with it the model.
/// <para>
/// The connection is what makes it more than a stream: the chatbot's tools act on the user's app over it, as it streams -
/// navigating it, changing its theme or language, showing the sign-in modal (See <see cref="AppChatbot"/>) - and a tool
/// call that waits on the user's approval (ClearAppFiles) gets the decision back over it too, on the same instance
/// (BitAiHub's SendBitAiApproval, through the BitAiApprovalBroker).
/// </para>
/// <para>
/// What does NOT travel on this stream is audio and pictures. Dictation and read aloud are ordinary requests to
/// <c>ChatbotController</c>, and an image goes up to <c>AttachmentController</c> before its message does, so the message
/// carries only the id it was stored under.
/// </para>
/// </summary>
public partial class AppHub
{
    protected override async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var services = request.Services ?? throw new InvalidOperationException("BitAiHub hands every request the services of its invocation.");

        // Read before the first await: a hub instance doesn't outlive the invocation, and the stream does.
        var connectionId = Context.ConnectionId;
        var httpContext = Context.GetHttpContext();
        var user = httpContext?.User ?? Context.User ?? new ClaimsPrincipal();

        // Azure SignalR runs the hub on a persistent connection with no ambient ASP.NET Core request, so it never sets the
        // IHttpContextAccessor AsyncLocal the chatbot and its tools read the request from.
        services.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        ChatbotMetrics.ActiveConversations.Add(1);

        try
        {
            await foreach (var evt in services.GetRequiredService<AppChatbot>().Reply(request, connectionId, user, cancellationToken))
            {
                yield return evt;
            }
        }
        finally
        {
            ChatbotMetrics.ActiveConversations.Add(-1);
        }
    }
}
