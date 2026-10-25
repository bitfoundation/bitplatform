using Microsoft.Extensions.AI;
using System.Threading.Channels;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Server.Api.Features.Chatbot;
using Boilerplate.Server.Api.Infrastructure.SignalR;

namespace Boilerplate.Tests.Features.Chatbot;

[TestClass, TestCategory("IntegrationTest")]
public partial class ChatbotCostLimitsTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AnOverLongMessage_Should_BeRefused_WithoutReachingTheModel()
    {
        var chatClient = new TestChatClient();

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppAddress);

        var chatbot = scope.ServiceProvider.GetRequiredService<AppChatbot>();
        await chatbot.StartChat(new StartChatRequest(), signalRConnectionId: "test-connection-id", TestContext.CancellationToken);

        await chatbot.ProcessNewMessage(new AiChatMessage { Content = new string('a', AiChatMessage.MaxContentLength + 1) }, httpContext.User, TestContext.CancellationToken);

        Assert.IsEmpty(chatClient.ReceivedConversations, "A message longer than AiChatMessage.MaxContentLength reached the model.");

        var turn = await ReadTurn(chatbot.GetStreamingChannel());

        Assert.IsFalse(turn.Successful, "The refused message's turn must still close, as unsuccessful, or the panel keeps waiting for its answer.");

        await chatbot.ProcessNewMessage(new AiChatMessage { Content = new string('a', AiChatMessage.MaxContentLength) }, httpContext.User, TestContext.CancellationToken);

        Assert.HasCount(1, chatClient.ReceivedConversations, "A message of exactly AiChatMessage.MaxContentLength must still reach the model.");
    }

    [TestMethod]
    public async Task MessagesBeyondThePerMinuteLimit_Should_BeRefused_WithoutReachingTheModel()
    {
        var chatClient = new TestChatClient();

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppAddress);

        var chatbot = scope.ServiceProvider.GetRequiredService<AppChatbot>();
        await chatbot.StartChat(new StartChatRequest(), signalRConnectionId: "test-connection-id", TestContext.CancellationToken);

        for (var message = 1; message <= ChatbotRateLimiter.MessagesPerMinute; message++)
        {
            await chatbot.ProcessNewMessage(new AiChatMessage { Content = $"question {message}" }, httpContext.User, TestContext.CancellationToken);
        }

        var conversationsWithinTheLimit = chatClient.ReceivedConversations.Count;

        Assert.AreEqual(ChatbotRateLimiter.MessagesPerMinute, conversationsWithinTheLimit, "Every message within the limit must reach the model.");

        await chatbot.ProcessNewMessage(new AiChatMessage { Content = "one question too many" }, httpContext.User, TestContext.CancellationToken);

        Assert.HasCount(conversationsWithinTheLimit, chatClient.ReceivedConversations, "The message past the per-minute limit reached the model.");
    }

    [TestMethod]
    public async Task ChatsBeyondThePerMinuteLimit_Should_BeRefusedAsTooManyRequests()
    {
        var chatClient = new TestChatClient();

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        SetCurrentHttpContext(scope.ServiceProvider, server.WebAppAddress);

        var chatbot = scope.ServiceProvider.GetRequiredService<AppChatbot>();

        for (var chat = 1; chat <= ChatbotRateLimiter.ChatsPerMinute; chat++)
        {
            await chatbot.StartChat(new StartChatRequest(), signalRConnectionId: "test-connection-id", TestContext.CancellationToken);
        }

        await Assert.ThrowsExactlyAsync<TooManyRequestsException>(
            () => chatbot.StartChat(new StartChatRequest(), signalRConnectionId: "test-connection-id", TestContext.CancellationToken),
            "A chat started past the per-minute limit must be refused.");
    }

    [TestMethod]
    public async Task AnOverLongMessageInTheResentHistory_Should_BeDropped_RatherThanFailTheChat()
    {
        var chatClient = new TestChatClient();

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppAddress);

        var overLongAnswer = new string('a', AiChatMessage.MaxContentLength + 1);
        var overLongQuestion = new string('b', AiChatMessage.MaxContentLength + 1);

        var chatbot = scope.ServiceProvider.GetRequiredService<AppChatbot>();
        await chatbot.StartChat(new StartChatRequest
        {
            ChatMessagesHistory =
            [
                new() { Role = AiChatMessageRole.User, Content = "what is bit platform?" },
                SignedAnswer(scope.ServiceProvider, overLongAnswer),
                new() { Role = AiChatMessageRole.User, Content = overLongQuestion }
            ]
        }, signalRConnectionId: "test-connection-id", TestContext.CancellationToken);

        await chatbot.ProcessNewMessage(new AiChatMessage { Content = "the newest question" }, httpContext.User, TestContext.CancellationToken);

        var conversation = chatClient.ReceivedConversations[^1];

        Assert.Contains(message => message.Text == "what is bit platform?", conversation,
            "The resent history must still reach the model, minus what is too long.");

        Assert.DoesNotContain(message => message.Text == overLongAnswer || message.Text == overLongQuestion, conversation,
            "A resent message longer than AiChatMessage.MaxContentLength reached the model.");
    }

    [TestMethod]
    public async Task TheModel_Should_BeAskedForAnAnswerNoLongerThanTheConfiguredCap()
    {
        var chatClient = new TestChatClient();

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppAddress);

        var chatbot = scope.ServiceProvider.GetRequiredService<AppChatbot>();
        await chatbot.StartChat(new StartChatRequest(), signalRConnectionId: "test-connection-id", TestContext.CancellationToken);

        await chatbot.ProcessNewMessage(new AiChatMessage { Content = "what is bit platform?" }, httpContext.User, TestContext.CancellationToken);

        var configuredCap = server.ApiApp.Services.GetRequiredService<IConfiguration>().GetValue<int?>("AI:ChatOptions:MaxOutputTokens");

        Assert.IsNotNull(configuredCap, "AI:ChatOptions:MaxOutputTokens is not set, so an answer can run as long as the model allows.");
        Assert.IsNotNull(chatClient.LastStreamingOptions, "The model was never asked for an answer.");
        Assert.AreEqual(configuredCap, chatClient.LastStreamingOptions.MaxOutputTokens, "The configured answer length cap did not reach the model.");
    }

    private async Task<AssistantTurn> ReadTurn(ChannelReader<string> responses)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        var reader = new PartialJsonReader<AssistantTurn>(AppJsonContext.Default.AssistantTurn);
        AssistantTurn? turn = null;

        while (reader.IsComplete is false)
        {
            turn = reader.Append(await responses.ReadAsync(timeout.Token));
        }

        return turn!;
    }

    private static AiChatMessage SignedAnswer(IServiceProvider scopedServices, string content)
    {
        return new()
        {
            Role = AiChatMessageRole.Assistant,
            Content = content,
            Signature = scopedServices.GetRequiredService<ChatbotAnswerSigner>().Sign(content)
        };
    }

    private static AppTestServer BuildServerWith(TestChatClient chatClient)
    {
        return new AppTestServer().Build(
            configureTestServices: services => services.Replace(ServiceDescriptor.Singleton<IChatClient>(chatClient)),
            configureTestConfiguration: configuration => configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-these-tests");
    }

    private static HttpContext SetCurrentHttpContext(IServiceProvider scopedServices, Uri serverAddress)
    {
        var httpContext = new DefaultHttpContext { RequestServices = scopedServices };
        httpContext.Request.Scheme = serverAddress.Scheme;
        httpContext.Request.Host = new HostString(serverAddress.Authority);

        scopedServices.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        return httpContext;
    }
}
