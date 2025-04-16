//+:cnd:noEmit
using System.Text;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Server.Api.Services;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Controllers.Identity;
using System.ComponentModel;

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
        StartChatbotRequest request,
        IAsyncEnumerable<string> incomingMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Incoming user messages are received via `incomingMessages`.
        // We utilize `Channel` to read incoming messages and send responses using `ChatClient`.
        // While processing a user message, a new message may arrive.
        // To handle this, we cancel the ongoing message processing using `messageSpecificCancellationTokenSrc` and start processing the new message.

        string? supportSystemPrompt = null;
        var culture = CultureInfo.GetCultureInfo(request.CultureId);

        try
        {
            await using var scope = rootScopeProvider();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            supportSystemPrompt = (await dbContext
                    .SystemPrompts.FirstOrDefaultAsync(p => p.PromptKind == PromptKind.Support, cancellationToken))?.Markdown ?? throw new ResourceNotFoundException();

            supportSystemPrompt = supportSystemPrompt
                .Replace("{{UserCulture}}", culture.NativeName)
                .Replace("{{DeviceInfo}}", request.DeviceInfo);
        }
        catch (Exception exp)
        {
            await HandleException(exp, cancellationToken);
            yield break;
        }

        Channel<string> channel = Channel.CreateUnbounded<string>(new() { SingleReader = true, SingleWriter = true });
        var chatClient = rootScopeProvider().ServiceProvider.GetRequiredService<IChatClient>();

        async Task ReadIncomingMessages()
        {
            List<ChatMessage> chatMessages = request.ChatMessagesHistory
                .Select(c => new ChatMessage(c.Role is AiChatMessageRole.Assistant ? ChatRole.Assistant : ChatRole.User, c.Content))
                .ToList();

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
                            Tools = [
                                AIFunctionFactory.Create(async (string emailAddress, string conversationHistory) =>
                                {
                                    await using var scope = rootScopeProvider();
                                    // Ideally, store these in a CRM or app database,
                                    // but for now, we'll log them!
                                    scope.ServiceProvider.GetRequiredService<ILogger<IChatClient>>()
                                        .LogError("Chat reported issue: User email: {emailAddress}, Conversation history: {conversationHistory}", emailAddress, conversationHistory);
                                }, name: "SaveUserEmailAndConversationHistory", description: "Saves the user's email address and the conversation history for future reference. Use this tool when the user provides their email address during the conversation. Parameters: emailAddress (string), conversationHistory (string)"),
                                //#if (module == "Sales")
                                AIFunctionFactory.Create(async ([Description("Concise summary of these user requirements")] string userNeeds) =>
                                {
                                    if (messageSpecificCancellationToken.IsCancellationRequested)
                                        return null;

                                    await using var scope = rootScopeProvider();
                                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                                    var recommendedProducts = await dbContext.Products // TODO: Implement RAG & Instruct LLM to accept recommended products as is.
                                        .Project()
                                        .OrderByDescending(p => p.HasPrimaryImage)
                                        .ToArrayAsync(messageSpecificCancellationToken);

                                    var markdown = new StringBuilder();

                                    foreach (var product in recommendedProducts)
                                    {
                                        markdown.AppendLine($"## [{product.Name}]({product.PageUrl})");
                                        markdown.AppendLine($"**Price**: ${product.FormattedPrice}");

                                        if (string.IsNullOrEmpty(product.DescriptionText) is false)
                                        {
                                            markdown.AppendLine(product.DescriptionText);
                                        }

                                        if (product.HasPrimaryImage)
                                        {
                                            markdown.AppendLine($"![{product.Name}]({product.GetPrimaryMediumImageUrl(Context.GetHttpContext()!.Request.GetBaseUrl())})");
                                        }

                                        markdown.AppendLine("---");
                                    }

                                    return markdown.ToString();
                                }, name: "GetProductRecommendations", description: "This tool searches for and recommends products based on a detailed description of the user's needs and preferences. It should only be used after the user explicitly asks for recommendations and provides specific criteria (e.g., product type, intended use, required features, budget hints, etc.)")
                                //#endif
                                ]
                        }, cancellationToken: messageSpecificCancellationToken))
                    {
                        if (messageSpecificCancellationToken.IsCancellationRequested)
                            break;

                        var result = response.Text;
                        assistantResponse.Append(result);
                        await channel.Writer.WriteAsync(result, messageSpecificCancellationToken);
                    }

                    await channel.Writer.WriteAsync(SharedChatProcessMessages.MESSAGE_RPOCESS_SUCESS, cancellationToken);
                }
                catch (Exception exp)
                {
                    _ = HandleException(exp, cancellationToken);
                    await channel.Writer.WriteAsync(SharedChatProcessMessages.MESSAGE_RPOCESS_ERROR, cancellationToken);
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

    private async Task HandleException(Exception exp, CancellationToken cancellationToken)
    {
        await using var scope = rootScopeProvider();
        var serverExceptionHandler = scope.ServiceProvider.GetRequiredService<ServerExceptionHandler>();
        var problemDetails = serverExceptionHandler.Handle(exp);
        if (problemDetails is null || serverExceptionHandler.IgnoreException(serverExceptionHandler.UnWrapException(exp)))
            return;
        try
        {
            await Clients.Caller.SendAsync(SignalREvents.EXCEPTION_THROWN, problemDetails, cancellationToken);
        }
        catch { }
    }
}
