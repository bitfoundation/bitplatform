//+:cnd:noEmit
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Server.Api.Services;

namespace Boilerplate.Server.Api.SignalR;

public partial class AppHub
{
    // For open telemetry metrics.
    private static readonly UpDownCounter<long> ongoingConversationsCount = Meter.Current.CreateUpDownCounter<long>("appHub.ongoing_conversations_count", "Number of ongoing conversations in the chatbot hub.");

    /// <summary>
    /// Checkout <see cref="AppChatbot"/> for more details.
    /// </summary>
    public async IAsyncEnumerable<string> Chatbot(
        StartChatbotRequest request,
        IAsyncEnumerable<string> incomingMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        [FromServices] AppChatbot chatbotService)
    {
        try
        {
            await chatbotService.Start(request,
                Context.ConnectionId,
                cancellationToken);
        }
        catch (Exception exp)
        {
            await HandleException(exp, cancellationToken);
            yield break;
        }

        async Task ReadIncomingMessages()
        {
            // While processing a user message, a new message may arrive.
            // To handle this, we cancel the ongoing message processing using `messageSpecificCancellationTokenSrc` and start processing the new message.
            CancellationTokenSource? messageSpecificCancellationTokenSrc = null;
            try
            {
                await foreach (var incomingMessage in incomingMessages)
                {
                    if (messageSpecificCancellationTokenSrc is not null)
                        await messageSpecificCancellationTokenSrc.CancelAsync();

                    messageSpecificCancellationTokenSrc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    _ = chatbotService.ProcessMessageAsync(
                        generateFollowUpSuggestions: true,
                        incomingMessage,
                        request.ServerApiAddress,
                        messageSpecificCancellationTokenSrc.Token);
                }
            }
            finally
            {
                messageSpecificCancellationTokenSrc?.Dispose();
                chatbotService.Stop();
            }
        }

        _ = ReadIncomingMessages();

        try
        {
            ongoingConversationsCount.Add(1);

            await foreach (var str in chatbotService.GetStreamingChannel().ReadAllAsync(cancellationToken).WithCancellation(cancellationToken))
            {
                yield return str;
            }
        }
        finally
        {
            ongoingConversationsCount.Add(-1);
        }
    }

    private async Task HandleException(Exception exp, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var serverExceptionHandler = scope.ServiceProvider.GetRequiredService<ServerExceptionHandler>();
        var problemDetails = serverExceptionHandler.Handle(exp);
        if (problemDetails is null || serverExceptionHandler.IgnoreException(serverExceptionHandler.UnWrapException(exp)))
            return;
        try
        {
            await Clients.Caller.SendAsync(SharedPubSubMessages.EXCEPTION_THROWN, problemDetails, cancellationToken);
        }
        catch { }
    }
}
