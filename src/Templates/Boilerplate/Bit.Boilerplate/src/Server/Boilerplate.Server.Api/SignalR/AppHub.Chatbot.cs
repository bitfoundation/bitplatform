//+:cnd:noEmit
using System.Text;
using System.ComponentModel;
using System.Threading.Channels;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Server.Api.Services;

namespace Boilerplate.Server.Api.SignalR;

public partial class AppHub
{
    [AutoInject] private IConfiguration configuration = default!;

    // For open telemetry metrics.
    private static readonly UpDownCounter<long> ongoingConversationsCount = AppActivitySource.CurrentMeter.CreateUpDownCounter<long>("appHub.ongoing_conversations_count", "Number of ongoing conversations in the chatbot hub.");

    public async IAsyncEnumerable<string> Chatbot(
        StartChatbotRequest request,
        IAsyncEnumerable<string> incomingMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Incoming user messages are received via `incomingMessages`.
        // We utilize `Channel` to read incoming messages and send responses using `ChatClient`.
        // While processing a user message, a new message may arrive.
        // To handle this, we cancel the ongoing message processing using `messageSpecificCancellationTokenSrc` and start processing the new message.

        CultureInfo? culture;
        string? supportSystemPrompt;

        try
        {
            culture = CultureInfo.GetCultureInfo(request.CultureId);

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
                channel.Writer.Complete();
            }

            async Task HandleIncomingMessage(string incomingMessage, CancellationToken messageSpecificCancellationToken)
            {
                StringBuilder assistantResponse = new();
                try
                {
                    chatMessages.Add(new(ChatRole.User, incomingMessage));

                    ChatOptions chatOptions = new()
                    {
                        Tools = [
                                AIFunctionFactory.Create(async ([Required] string emailAddress, string conversationHistory) =>
                                {
                                    if (messageSpecificCancellationToken.IsCancellationRequested)
                                        return;

                                    await using var scope = serviceProvider.CreateAsyncScope();

                                    // Ideally, store these in a CRM or app database,
                                    // but for now, we'll log them!
                                    scope.ServiceProvider.GetRequiredService<ILogger<IChatClient>>()
                                        .LogError("Chat reported issue: User email: {emailAddress}, Conversation history: {conversationHistory}", emailAddress, conversationHistory);

                                }, name: "SaveUserEmailAndConversationHistory", description: "Saves the user's email address and the conversation history for future reference. Use this tool when the user provides their email address during the conversation. Parameters: emailAddress (string), conversationHistory (string)"),
                                //#if (module == "Sales")
                                AIFunctionFactory.Create(async ([Required, Description("Concise summary of these user requirements")] string userNeeds,
                                    [Description("Car manufacturer's name (Optional)")] string? manufacturer,
                                    [Description("Car price below this value (Optional)")] decimal? maxPrice,
                                    [Description("Car price above this value (Optional)")] decimal? minPrice) =>
                                {
                                    if (messageSpecificCancellationToken.IsCancellationRequested)
                                        return null;

                                    await using var scope = serviceProvider.CreateAsyncScope();
                                    var productEmbeddingService = scope.ServiceProvider.GetRequiredService<ProductEmbeddingService>();
                                    var searchQuery = string.IsNullOrWhiteSpace(manufacturer)
                                        ? userNeeds
                                        : $"**{manufacturer}** {userNeeds}";
                                    var recommendedProducts = await (await productEmbeddingService.GetProductsBySearchQuery(searchQuery, messageSpecificCancellationToken))
                                        .WhereIf(maxPrice.HasValue, p => p.Price <= maxPrice!.Value)
                                        .WhereIf(minPrice.HasValue, p => p.Price >= minPrice!.Value)
                                        .Take(10)
                                        .Project()
                                        .Select(p => new
                                        {
                                            p.Name,
                                            p.PageUrl,
                                            Manufacturer = p.CategoryName,
                                            Price = p.FormattedPrice,
                                            Description = p.DescriptionText,
                                            PreviewImageUrl = p.GetPrimaryMediumImageUrl(request.ServerApiAddress!) ?? "_content/Boilerplate.Client.Core/images/car_placeholder.png"
                                        })
                                        .ToArrayAsync(messageSpecificCancellationToken);

                                    return recommendedProducts;
                                }, name: "GetProductRecommendations", description: "This tool searches for and recommends products based on a detailed description of the user's needs and preferences and returns recommended products.")
                                //#endif
                                ]
                    };

                    configuration.GetRequiredSection("AI:ChatOptions").Bind(chatOptions);

                    await foreach (var response in chatClient.GetStreamingResponseAsync([
                        new (ChatRole.System, supportSystemPrompt),
                            .. chatMessages,
                            new (ChatRole.User, incomingMessage)
                        ], options: chatOptions, cancellationToken: messageSpecificCancellationToken))
                    {
                        if (messageSpecificCancellationToken.IsCancellationRequested)
                            break;

                        var result = response.Text;
                        assistantResponse.Append(result);
                        await channel.Writer.WriteAsync(result, messageSpecificCancellationToken);
                    }

                    await channel.Writer.WriteAsync(SharedChatProcessMessages.MESSAGE_RPOCESS_SUCESS, cancellationToken);

                    // This would generate a list of follow-up questions/suggestions to keep the conversation going.
                    // You could instead generate that list in previous chat completion call:
                    // 1: Using "tools" or "functions" feature of the model, that would not consider the latest assistant response.
                    // 2: Returning a json object containing the response and follow-up suggestions all together, losing IAsyncEnumerable streaming capability.  
                    chatOptions.ResponseFormat = ChatResponseFormat.Json;
                    chatOptions.AdditionalProperties = new() { ["response_format"] = new { type = "json_object" } };
                    var followUpItems = await chatClient.GetResponseAsync<AiChatFollowUpList>([
                        new(ChatRole.System, supportSystemPrompt),
                        new(ChatRole.User, incomingMessage),
                        new(ChatRole.Assistant, assistantResponse.ToString()),
                        new(ChatRole.User, @"Return up to 3 relevant follow-up suggestions that help users discover related topics and continue the conversation naturally based on user's query in JSON object containing string[] named FollowUpSuggestions."),],
                        chatOptions, cancellationToken: cancellationToken);

                    await channel.Writer.WriteAsync(JsonSerializer.Serialize(followUpItems.Result), cancellationToken);
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

        try
        {
            ongoingConversationsCount.Add(1);

            await foreach (var str in channel.Reader.ReadAllAsync(cancellationToken).WithCancellation(cancellationToken))
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
            await Clients.Caller.SendAsync(SignalREvents.EXCEPTION_THROWN, problemDetails, cancellationToken);
        }
        catch { }
    }
}
