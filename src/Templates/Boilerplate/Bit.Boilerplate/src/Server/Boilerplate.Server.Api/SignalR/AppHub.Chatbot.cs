//+:cnd:noEmit
using System.Text;
using System.ComponentModel;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Server.Api.Services;

namespace Boilerplate.Server.Api.SignalR;

public partial class AppHub
{
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
        Uri webAppUri = default!;

        try
        {
            webAppUri = Context.GetHttpContext()!.Request.GetBaseUrl();

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
                                AIFunctionFactory.Create(async ([Description("Concise summary of these user requirements in English Language")] string userNeeds, [Description("Car manufacturer's English name (Optional)")] string? manufacturer) =>
                                {
                                    if (messageSpecificCancellationToken.IsCancellationRequested)
                                        return null;

                                    await using var scope = serviceProvider.CreateAsyncScope();
                                    var productEmbeddingService = scope.ServiceProvider.GetRequiredService<ProductEmbeddingService>();
                                    var searchQuery = string.IsNullOrWhiteSpace(manufacturer)
                                        ? userNeeds
                                        : $"{userNeeds}, Manufacturer: {manufacturer}";
                                    var recommendedProducts = await (await productEmbeddingService.GetProductsBySearchQuery(searchQuery, messageSpecificCancellationToken))
                                        .Take(10)
                                        .Project()
                                        .Select(p => new
                                        {
                                            p.Name,
                                            PageUrl = new Uri(webAppUri, p.PageUrl),
                                            Manufacturer = p.CategoryName,
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
}
