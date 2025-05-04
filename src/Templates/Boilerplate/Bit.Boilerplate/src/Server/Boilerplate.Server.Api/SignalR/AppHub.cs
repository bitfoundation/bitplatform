//+:cnd:noEmit
using System.Text;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.Diagnostic;
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
    [AutoInject] private IServiceProvider serviceProvider = default!;

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
            await using var scope = serviceProvider.CreateAsyncScope();
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

            await using var scope = serviceProvider.CreateAsyncScope();
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
            await using var scope = serviceProvider.CreateAsyncScope();

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
        var chatClient = serviceProvider.CreateAsyncScope().ServiceProvider.GetRequiredService<IChatClient>();

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
                                    await using var scope = serviceProvider.CreateAsyncScope();
                                    // Ideally, store these in a CRM or app database,
                                    // but for now, we'll log them!
                                    scope.ServiceProvider.GetRequiredService<ILogger<IChatClient>>()
                                        .LogError("Chat reported issue: User email: {emailAddress}, Conversation history: {conversationHistory}", emailAddress, conversationHistory);
                                }, name: "SaveUserEmailAndConversationHistory", description: "Saves the user's email address and the conversation history for future reference. Use this tool when the user provides their email address during the conversation. Parameters: emailAddress (string), conversationHistory (string)"),
                                //#if (module == "Sales")
                                AIFunctionFactory.Create(async ([Description("Concise summary of these user requirements in English Language")] string userNeeds, [Description("Car manufactor's English name (Optional)")] string? manufactor) =>
                                {
                                    if (messageSpecificCancellationToken.IsCancellationRequested)
                                        return null;

                                    var baseApiUrl = Context.GetHttpContext()!.Request.GetBaseUrl();

                                    await using var scope = serviceProvider.CreateAsyncScope();
                                    var productEmbeddingService = scope.ServiceProvider.GetRequiredService<ProductEmbeddingService>();
                                    var recommendedProducts = await (await productEmbeddingService.GetProductsBySearchQuery($"{userNeeds}, Manufactor: {manufactor}", messageSpecificCancellationToken))
                                        .Project()
                                        .Select(p => new
                                        {
                                            p.Name,
                                            PageUrl = new Uri(baseApiUrl, p.PageUrl),
                                            Manufactor = p.CategoryName,
                                            Price = p.FormattedPrice,
                                            Description = p.DescriptionText
                                        })
                                        .ToArrayAsync(messageSpecificCancellationToken);

                                    return recommendedProducts;
                                }, name: "GetProductRecommendations", description: "This tool searches for and recommends products based on a detailed description of the user's needs and preferences and returns recommended products.")
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
        await using var scope = serviceProvider.CreateAsyncScope();
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

    /// <summary>
    /// <inheritdoc cref="SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE"/>
    /// </summary>
    /// <param name="userQuery">`UserId`, `UserSessionId`, `Email` or `PhoneNumber`</param>
    /// <returns></returns>
    [Authorize(Policy = AppClaimTypes.Management.ReadLogs)]
    public async Task<DiagnosticLogDto[]> GetUserDiagnosticLogs(string? userQuery)
    {
        if (string.IsNullOrEmpty(userQuery))
            return [];

        userQuery = userQuery.Trim().ToUpperInvariant();

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var isGuidId = Guid.TryParse(userQuery, out var id);

        var userSessionSignalRConnectionIds = await dbContext.UserSessions
            .WhereIf(isGuidId, us => us.UserId == id || us.UserId == id)
            .WhereIf(isGuidId is false, us => us.User!.NormalizedEmail == userQuery || us.User.PhoneNumber == userQuery)
            .Where(us => us.SignalRConnectionId != null)
            .Select(us => us.SignalRConnectionId)
            .ToArrayAsync(Context.ConnectionAborted);

        return [.. (await Task.WhenAll(userSessionSignalRConnectionIds.Select(id => Clients.Client(id!).InvokeAsync<DiagnosticLogDto[]>(SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE, Context.ConnectionAborted))))
            .SelectMany(_ => _)];
    }
}
