using Bit.BlazorUI;

namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>
/// Sends the server the newest <see cref="AiChatProtocol.MaxHistory"/> messages of the conversation only: all it reads
/// (See <c>AppChatbot.ToChatMessages</c>), and what keeps a long conversation within the hub's message size limit
/// (<c>HubOptions:MaximumReceiveMessageSize</c>), as the whole of it goes up with every question. The panel goes on
/// showing every message.
/// <para>
/// Wraps the transport rather than replacing it, so the user's decisions on tool calls still reach the server through it
/// (<see cref="IBitAiApprovalTransport"/>). In Shared, like <see cref="AiChatStrings"/>: the router reads every type of
/// Client.Core at startup, before the AI chat's assemblies are loaded.
/// </para>
/// </summary>
public sealed class AiChatHistoryWindow(IBitAiTransport transport) : IBitAiTransport, IBitAiApprovalTransport
{
    public IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken)
    {
        if (request.Messages.Count <= AiChatProtocol.MaxHistory)
            return transport.StreamAsync(request, cancellationToken);

        return transport.StreamAsync(new BitAiRequest([.. request.Messages.TakeLast(AiChatProtocol.MaxHistory)])
        {
            ConversationId = request.ConversationId,
            Options = request.Options,
            Tools = request.Tools,
            Services = request.Services,
            Context = request.Context
        }, cancellationToken);
    }

    public ValueTask SendApprovalAsync(BitAiApprovalResponse response, CancellationToken cancellationToken)
    {
        return transport is IBitAiApprovalTransport approvals
            ? approvals.SendApprovalAsync(response, cancellationToken)
            : ValueTask.CompletedTask;
    }
}
