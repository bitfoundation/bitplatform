//+:cnd:noEmit
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Server.Api.Infrastructure.Services;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

public partial class AppHub
{
    // For open telemetry metrics.
    private static readonly UpDownCounter<long> ongoingConversationsCount = Meter.Current.CreateUpDownCounter<long>("appHub.ongoing_conversations_count", "Number of ongoing conversations in the chatbot hub.");

    /// <summary>
    /// This method is accepting stream of user messages and returning stream of string charecters of AI chatbot responses.
    /// The basic implementation idea is brought from here: https://learn.microsoft.com/en-us/aspnet/core/signalr/streaming?view=aspnetcore-10.0
    /// Accepting stream of user messages drastically reduces the latency instead of sending different requests for each user message.
    /// 
    /// During this process, if a new user message arrives, the ongoing message processing is cancelled and the new message is processed,
    /// and server is able to send commands such as navigate to specific page, change theme etc to the client as well,
    /// which means as AI chatbot behind this hub is able to guide user through the application and a lot more interactive experience can be built.
    /// Using SignalR's `InvokeAsync` method, the AI chatbot would know if the command is successfully executed on client side or not, because it's not a fire-and-forget operation.
    /// 
    /// By accepting stream of user messages, we can store `chatMessages` field inside `AppChatbot` for current conversation only,
    /// which is **scalable** without forcing client to send the whole chat history each time or storing chat history on redis or database.
    /// 
    /// The above 3 reasons are the main motivations of this design/implementation using SignalR instead of using SSE or other techniques.
    /// Checkout <see cref="AppChatbot"/> for more details.
    /// </summary>
    [HubMethodName(SharedAppMessages.StartChat)]
    public async IAsyncEnumerable<string> StartChat(
        StartChatRequest request,
        IAsyncEnumerable<string> incomingMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        [FromServices] AppChatbot chatbotService)
    {
        try
        {
            await chatbotService.StartChat(request,
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
                        await messageSpecificCancellationTokenSrc.TryCancel();

                    messageSpecificCancellationTokenSrc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    _ = chatbotService.ProcessNewMessage(
                        generateFollowUpSuggestions: true,
                        incomingMessage,
                        request.ServerApiAddress,
                        Context.GetHttpContext()!.User,
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
            await Clients.Caller.Publish(SharedAppMessages.EXCEPTION_THROWN, problemDetails, cancellationToken);
        }
        catch { }
    }
}
