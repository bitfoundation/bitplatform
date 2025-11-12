//+:cnd:noEmit
using System.Text;
using System.ComponentModel;
using System.Threading.Channels;
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Server.Api.Services;
using Microsoft.Extensions.Caching.Hybrid;

namespace Boilerplate.Server.Api.SignalR;

/// <summary>
/// Service responsible for managing chatbot conversations, maintaining chat history,
/// and handling AI interactions including getting user feedbacks, describing app's features and pages etc.
/// This service is exposed over SignalR's AppHub.Chat.cs, so it can accept stream of user messages and return stream of AI responses using AiChatPanel.razor
/// It's also exposed as MCP tool over MinimalApi so you can add chatbot capabilities to any MCP-enabled client such as https://CrystaCode.ai and instruct each client differently,
/// based on your needs. For example you can customize CrystaCode.AI as a agent that would allow users to talk with their own voice and CrystaCode.AI would call MCP tool developed in <see cref="AppMcpService"/>
/// to get answers it need about the app.
/// </summary>
public partial class AppChatbot : IAsyncDisposable
{
    private IChatClient? chatClient = default!;

    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private HybridCache cache = default!;
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private ILogger<AppChatbot> logger = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;

    private string? supportSystemPrompt;
    private List<ChatMessage> chatMessages = [];

    /// <summary>
    /// This is a heart of streaming AI responses back to the client.
    /// </summary>
    private readonly Channel<string> responseChannel = Channel.CreateUnbounded<string>(new() { SingleReader = true, SingleWriter = true });

    /// <summary>
    /// Starts the chat session with history and system prompt
    /// </summary>
    public async Task Start(
        List<AiChatMessage> chatMessagesHistory,
        int? cultureId,
        string? deviceInfo,
        CancellationToken cancellationToken)
    {
        chatMessages = [.. chatMessagesHistory.Select(c => new ChatMessage(c.Role is AiChatMessageRole.Assistant ? ChatRole.Assistant : ChatRole.User, c.Content))];

        CultureInfo? culture = null;
        if (cultureId is not null && CultureInfoManager.InvariantGlobalization is false)
        {
            culture = CultureInfo.GetCultureInfo(cultureId.Value);
        }

        supportSystemPrompt = await cache.GetOrCreateAsync(
            $"SystemPrompt_{PromptKind.Support}",
            async cancel =>
            {
                var prompt = await dbContext.SystemPrompts
                    .FirstOrDefaultAsync(p => p.PromptKind == PromptKind.Support, cancel);
                return prompt?.Markdown ?? throw new ResourceNotFoundException();
            },
            new()
            {
                Expiration = TimeSpan.FromHours(1),
                LocalCacheExpiration = TimeSpan.FromHours(1)
            },
            tags: ["SystemPrompts", $"SystemPrompt_{PromptKind.Support}"],
            cancellationToken: cancellationToken);

        supportSystemPrompt = supportSystemPrompt
            .Replace("{{UserCulture}}", culture?.NativeName ?? "English")
            .Replace("{{DeviceInfo}}", deviceInfo ?? "Generic Device");
    }

    /// <summary>
    /// Get the response channel for streaming AI responses
    /// </summary>
    public ChannelReader<string> GetStreamingChannel() => responseChannel.Reader;

    /// <summary>
    /// Stops streaming
    /// </summary>
    public void Stop() => responseChannel.Writer.Complete();

    /// <summary>
    /// Process an incoming message and stream the AI response
    /// </summary>
    public async Task ProcessMessageAsync(
        string incomingMessage,
        Uri? serverApiAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(supportSystemPrompt))
            throw new InvalidOperationException("Chat session must be started before processing messages. Call Start method first.");

        chatClient ??= serviceProvider.GetRequiredService<IChatClient>();

        StringBuilder assistantResponse = new();
        try
        {
            chatMessages.Add(new(ChatRole.User, incomingMessage));

            var chatOptions = CreateChatOptions(serverApiAddress, cancellationToken);

            await foreach (var response in chatClient.GetStreamingResponseAsync([
                new (ChatRole.System, supportSystemPrompt),
                    .. chatMessages,
                    new (ChatRole.User, incomingMessage)
                ], options: chatOptions, cancellationToken: cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var result = response.Text;
                assistantResponse.Append(result);
                await responseChannel.Writer.WriteAsync(result, cancellationToken);
            }

            await responseChannel.Writer.WriteAsync(SharedChatProcessMessages.MESSAGE_RPOCESS_SUCESS, cancellationToken);

            // Generate follow-up suggestions
            var followUpSuggestions = await GenerateFollowUpSuggestionsAsync(
                incomingMessage,
                assistantResponse.ToString(),
                chatOptions,
                cancellationToken);

            await responseChannel.Writer.WriteAsync(JsonSerializer.Serialize(followUpSuggestions), cancellationToken);
        }
        catch (Exception exp)
        {
            logger.LogError(exp, "Error processing message in chatbot service");
            await responseChannel.Writer.WriteAsync(SharedChatProcessMessages.MESSAGE_RPOCESS_ERROR, cancellationToken);
        }
        finally
        {
            chatMessages.Add(new(ChatRole.Assistant, assistantResponse.ToString()));
        }
    }

    /// <summary>
    /// Create chat options with AI tools
    /// </summary>
    private ChatOptions CreateChatOptions(Uri? serverApiAddress, CancellationToken cancellationToken)
    {
        var tools = new List<AIFunction>
        {
            AIFunctionFactory.Create(async ([Required] string emailAddress, string conversationHistory) =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                await using var scope = serviceProvider.CreateAsyncScope();

                // Ideally, store these in a CRM or app database,
                // but for now, we'll log them!
                scope.ServiceProvider.GetRequiredService<ILogger<IChatClient>>()
                    .LogError("Chat reported issue: User email: {emailAddress}, Conversation history: {conversationHistory}", emailAddress, conversationHistory);

            }, name: "SaveUserEmailAndConversationHistory", description: "Saves the user's email address and the conversation history for future reference. Use this tool when the user provides their email address during the conversation. Parameters: emailAddress (string), conversationHistory (string)"),
            //#if (module == "Sales")
            //#if (database == "PostgreSQL" || database == "SqlServer")
            AIFunctionFactory.Create(async ([Required, Description("Concise summary of these user requirements")] string userNeeds,
                [Description("Car manufacturer's name (Optional)")] string? manufacturer,
                [Description("Car price below this value (Optional)")] decimal? maxPrice,
                [Description("Car price above this value (Optional)")] decimal? minPrice) =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                await using var scope = serviceProvider.CreateAsyncScope();
                var productEmbeddingService = scope.ServiceProvider.GetRequiredService<ProductEmbeddingService>();
                var searchQuery = string.IsNullOrWhiteSpace(manufacturer)
                    ? userNeeds
                    : $"**{manufacturer}** {userNeeds}";
                var recommendedProducts = await (await productEmbeddingService.SearchProducts(searchQuery, cancellationToken))
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
                        PreviewImageUrl = p.GetPrimaryMediumImageUrl(serverApiAddress!) ?? "_content/Boilerplate.Client.Core/images/car_placeholder.png"
                    })
                    .ToArrayAsync(cancellationToken);

                return recommendedProducts;
            }, name: "GetProductRecommendations", description: "This tool searches for and recommends products based on a detailed description of the user's needs and preferences and returns recommended products.")
            //#endif
            //#endif
        };

        var chatOptions = new ChatOptions { Tools = [.. tools] };
        configuration.GetRequiredSection("AI:ChatOptions").Bind(chatOptions);
        return chatOptions;
    }

    /// <summary>
    /// Generate follow-up suggestions based on the conversation
    /// </summary>
    private async Task<AiChatFollowUpList> GenerateFollowUpSuggestionsAsync(
        string incomingMessage,
        string assistantResponse,
        ChatOptions chatOptions,
        CancellationToken cancellationToken)
    {
        // This would generate a list of follow-up questions/suggestions to keep the conversation going.
        // You could instead generate that list in previous chat completion call:
        // 1: Using "tools" or "functions" feature of the model, that would not consider the latest assistant response.
        // 2: Returning a json object containing the response and follow-up suggestions all together, losing IAsyncEnumerable streaming capability.  
        chatOptions.ResponseFormat = ChatResponseFormat.Json;
        chatOptions.AdditionalProperties = new() { ["response_format"] = new { type = "json_object" } };

        var followUpItems = await chatClient.GetResponseAsync<AiChatFollowUpList>([
            new(ChatRole.System, supportSystemPrompt),
            new(ChatRole.User, incomingMessage),
            new(ChatRole.Assistant, assistantResponse),
            new(ChatRole.User, @"Return up to 3 relevant follow-up suggestions that help users discover related topics and continue the conversation naturally based on user's query in JSON object containing string[] named FollowUpSuggestions.
Only suggest follow-up questions that are within the assistant's scope and knowledge.
Do not suggest questions that require access to data or functionality that is unavailable or out of scope for this assistant.
Avoid suggesting questions that the assistant would not be able to answer."),],
            chatOptions, cancellationToken: cancellationToken);

        return followUpItems.Result ?? new AiChatFollowUpList();
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
    }
}
