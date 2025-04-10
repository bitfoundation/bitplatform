using Boilerplate.Server.Api.Services;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Controllers.Identity;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;

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
    [AutoInject] private IChatClient chatClient = default;
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
            await using var scope = rootScopeProvider.Invoke();
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

            await using var scope = rootScopeProvider.Invoke();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.UserSessions.Where(us => us.Id == Context.User!.GetSessionId()).ExecuteUpdateAsync(us => us.SetProperty(x => x.SignalRConnectionId, (string?)null));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async IAsyncEnumerable<string> Chatbot(
        //#if (captcha == "reCaptcha")
        string googleRecpatchaToken,
        //#endif
        string cultureNativeName,
        IAsyncEnumerable<string> incomingMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int incomingMessagesCount = 0;
        List<(string userQuery, string assistantResponses)> chatHistory = [];
        string? supportSystemPrompt, summarizationSystemPrompt, chatSummary = null;

        await using (var scope = rootScopeProvider.Invoke())
        {
            //#if (captcha == "reCaptcha")
            var googleRecaptchaService = scope.ServiceProvider.GetRequiredService<GoogleRecaptchaService>();
            if (await googleRecaptchaService.Verify(googleRecpatchaToken, cancellationToken) is false)
                throw new BadRequestException(nameof(AppStrings.InvalidGoogleRecaptchaResponse));
            //#endif

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            supportSystemPrompt = (await dbContext
                    .SystemPrompts.FirstOrDefaultAsync(p => p.PromptKind == PromptKind.Support, cancellationToken))?.Markdown ?? throw new ResourceNotFoundException();

            supportSystemPrompt = supportSystemPrompt.Replace("{{UserCulture}}", cultureNativeName);

            summarizationSystemPrompt = (await dbContext
                .SystemPrompts.FirstOrDefaultAsync(p => p.PromptKind == PromptKind.SummarizeConversationContext, cancellationToken))?.Markdown ?? throw new ResourceNotFoundException();
        }

        await foreach (var incomingMessage in incomingMessages.WithCancellation(cancellationToken))
        {
            incomingMessagesCount++;

            StringBuilder assistantResponse = new();

            var supportSystemPromptWithChatContext = supportSystemPrompt
                .Replace("{{SummarizedConversationContext}}", $"    - Chat summary: {chatSummary ?? "No summary available."} {Environment.NewLine}     - Previous user queries: {ChatHistoryAsString()}");

            await foreach (var response in chatClient.GetStreamingResponseAsync([
                            new (ChatRole.System, supportSystemPromptWithChatContext),
                            new (ChatRole.User, incomingMessage)
                            ], options: new() { }, cancellationToken: cancellationToken))
            {
                assistantResponse.Append(response.Text);
                yield return response.Text;
            }

            if (incomingMessagesCount % 5 == 0) // Summarize every 5 message into one.
            {
                var chatHistoryString = ChatHistoryAsString();
                chatHistory.Clear();

                var response = await chatClient.GetResponseAsync([
                        new (ChatRole.System, summarizationSystemPrompt),
                        new (ChatRole.User, chatHistoryString)
                ], cancellationToken: cancellationToken);

                chatSummary = response.Text;
            }
            else
            {
                chatHistory.Add((incomingMessage, assistantResponse.ToString()));
            }

            yield return "MESSAGE_PROCESSED";
        }

        string ChatHistoryAsString()
        {
            if (chatHistory.Count == 0)
                return "No chat history available.";

            return string.Join(Environment.NewLine, chatHistory.Select((history, index) => $"# {index} User's query: {history.userQuery}, Assistant response: {history.assistantResponses}"));
        }
    }
}
