using System.Text;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Server.Api.Services;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Controllers.Identity;

namespace Boilerplate.Server.Api.SignalR;

/// <summary>
/// SignalR supports basic scenarios like sending messages to all connected clients using `Clients.All()`, 
/// which broadcasts to all SignalR connections, whether authenticated or not. Similarly, `Clients.User(userId)`
/// sends messages to all open browser tabs or applications associated with a specific user.
///
/// In addition to these, the following enhanced scenarios are supported:
/// 1. `Clients.Group("AuthenticatedClients")`: Sends a message to all browser tabs and apps that are signed in.
/// 2. Each user session knows its own <see cref="UserSession.SignalRConnectionId"/>. The application 
///    already uses this approach in the `<see cref="UserController.RevokeSession(Guid, CancellationToken)"/>` method by sending a SignalR message to 
///    `Clients.Client(userSession.SignalRConnectionId)`. This ensures that the corresponding browser tab or app clears 
///    its access/refresh tokens from storage and navigates to the sign-in page automatically.
/// </summary>
[AllowAnonymous]
public partial class AppHub : Hub
{
    [AutoInject] private RootServiceScopeProvider rootScopeProvider = default!;

    public override async Task OnConnectedAsync()
    {
        if (Context.User.IsAuthenticated() is false)
        {
            if (Context.GetHttpContext()?.Request.Headers.Authorization.Any() is true)
            {
                // AppHub allows anonymous connections. However, if an Authorization is included
                // and the user is not authenticated, it indicates the client has sent an invalid or expired access token.
                // In this scenario, we should refresh the access token and attempt to reconnect.
                throw new HubException(nameof(AppStrings.UnauthorizedException)).WithData("ConnectionId", Context.ConnectionId);
            }
        }
        else
        {
            await using var scope = rootScopeProvider();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.UserSessions.Where(us => us.Id == Context.User!.GetSessionId()).ExecuteUpdateAsync(us => us.SetProperty(x => x.SignalRConnectionId, Context.ConnectionId));

            await Groups.AddToGroupAsync(Context.ConnectionId, "AuthenticatedClients");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User.IsAuthenticated())
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuthenticatedClients");

            await using var scope = rootScopeProvider();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.UserSessions.Where(us => us.Id == Context.User!.GetSessionId()).ExecuteUpdateAsync(us => us.SetProperty(x => x.SignalRConnectionId, (string?)null));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async IAsyncEnumerable<string> Chatbot(
        string cultureNativeName,
        IAsyncEnumerable<string> incomingMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? supportSystemPrompt = null;

        try
        {
            await using var scope = rootScopeProvider();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            supportSystemPrompt = (await dbContext
                    .SystemPrompts.FirstOrDefaultAsync(p => p.PromptKind == PromptKind.Support, cancellationToken))?.Markdown ?? throw new ResourceNotFoundException();

            supportSystemPrompt = supportSystemPrompt.Replace("{{UserCulture}}", cultureNativeName);
        }
        catch (Exception exp)
        {
            await HandleException(exp);
            yield break;
        }

        Channel<string> channel = Channel.CreateUnbounded<string>();
        var chatClient = rootScopeProvider().ServiceProvider.GetRequiredService<IChatClient>();

        async Task ReadIncomingMessages()
        {
            List<ChatMessage> chatMessages = [];

            CancellationTokenSource? messageSpecificCancellationTokenSrc = null;
            try
            {
                await foreach (var incomingMessage in incomingMessages)
                {
                    if (messageSpecificCancellationTokenSrc is not null)
                        await messageSpecificCancellationTokenSrc.CancelAsync();

                    messageSpecificCancellationTokenSrc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    _ = HandleIncomingMessage(incomingMessage, messageSpecificCancellationTokenSrc.Token);
                }
            }
            finally
            {
                messageSpecificCancellationTokenSrc?.Dispose();
            }

            async Task HandleIncomingMessage(string incomingMessage, CancellationToken messageSpecificCancellationToken)
            {
                StringBuilder assistantResponse = new();
                try
                {
                    chatMessages.Add(new(ChatRole.User, incomingMessage));

                    await foreach (var response in chatClient.GetStreamingResponseAsync([
                        new (ChatRole.System, supportSystemPrompt),
                            .. chatMessages,
                            new (ChatRole.User, incomingMessage)
                        ], options: new()
                        {
                            Temperature = 0,
                            Tools = [AIFunctionFactory.Create(async (string emailAddress, string conversationHistory) =>
                            {
                                await using var scope = rootScopeProvider();
                                // Ideally, store these in a CRM or app database,
                                // but for now, we'll log them!
                                scope.ServiceProvider.GetRequiredService<ILogger<IChatClient>>()
                                    .LogError("Chat reported issue: User email: {emailAddress}, Conversation history: {conversationHistory}", emailAddress, conversationHistory);
                            }, name: "SaveUserEmailAndConversationHistory", description: "Saves the user's email and their conversation history.")]
                        }, cancellationToken: messageSpecificCancellationToken))
                    {
                        assistantResponse.Append(response.Text);
                        await channel.Writer.WriteAsync(response.Text, messageSpecificCancellationToken);
                    }
                    await channel.Writer.WriteAsync(ChatMessageProcessStatus.MESSAGE_RPOCESS_SUCESS, cancellationToken);
                }
                catch (Exception exp)
                {
                    await HandleException(exp);
                    await channel.Writer.WriteAsync(ChatMessageProcessStatus.MESSAGE_RPOCESS_ERROR, cancellationToken);
                }
                finally
                {
                    chatMessages.Add(new(ChatRole.Assistant, assistantResponse.ToString()));
                }
            }
        }

        _ = ReadIncomingMessages();

        await foreach (var str in channel.Reader.ReadAllAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            yield return str;
        }
    }

    private async Task HandleException(Exception exp)
    {
        await using var scope = rootScopeProvider();
        var serverExceptionHandler = scope.ServiceProvider.GetRequiredService<ServerExceptionHandler>();
        var problemDetails = serverExceptionHandler.Handle(exp);

        if (problemDetails is not null)
        {
            try
            {
                await Clients.Caller.SendAsync(SignalREvents.EXCEPTION_THROWN, problemDetails, Context.ConnectionAborted);
            }
            catch { }
        }
    }
}
