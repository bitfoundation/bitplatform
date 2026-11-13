using Bit.BlazorUI;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Server.Api.Features.Chatbot;
using static Boilerplate.Tests.Features.Chatbot.TestAiChatPanel;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>Every question the AI chat panel sends is paid for, so what bounds the bill is the server's, not the panel's.</summary>
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

        await using var panel = new TestAiChatPanel(scope.ServiceProvider, httpContext.User);

        var refused = await panel.Ask(new string('a', AiChatMessage.MaxContentLength + 1), cancellationToken: TestContext.CancellationToken);

        Assert.IsEmpty(chatClient.ReceivedConversations, "A message longer than AiChatMessage.MaxContentLength reached the model.");

        Assert.AreEqual(BitAiMessageStatus.Failed, refused.Status, "The refused message's answer must still end, as failed, or the panel keeps waiting for it.");
        Assert.IsNotNull(refused.Error, "The refused message's answer must say why it failed.");
        Assert.IsFalse(refused.Error.IsRetryable, "Sending the same message again would be refused again, so the panel must not offer to.");

        await panel.Ask(new string('a', AiChatMessage.MaxContentLength), cancellationToken: TestContext.CancellationToken);

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

        await using var panel = new TestAiChatPanel(scope.ServiceProvider, httpContext.User);

        for (var message = 1; message <= ChatbotRateLimiter.MessagesPerMinute; message++)
        {
            await panel.Ask($"question {message}", cancellationToken: TestContext.CancellationToken);
        }

        var conversationsWithinTheLimit = chatClient.ReceivedConversations.Count;

        Assert.AreEqual(ChatbotRateLimiter.MessagesPerMinute, conversationsWithinTheLimit, "Every message within the limit must reach the model.");

        var refused = await panel.Ask("one question too many", cancellationToken: TestContext.CancellationToken);

        Assert.HasCount(conversationsWithinTheLimit, chatClient.ReceivedConversations, "The message past the per-minute limit reached the model.");
        Assert.AreEqual(BitAiMessageStatus.Failed, refused.Status, "The answer to a message past the limit must say it was refused.");

        // The same question is answered once the minute is over, so the panel says when and offers it again then
        Assert.IsNotNull(refused.Error, "The refused message's answer must say why it failed.");
        Assert.AreEqual(BitAiErrorKind.RateLimit, refused.Error.Kind, "The refusal must say it's the rate limit, which the panel waits out.");
        Assert.IsTrue(refused.Error.IsRetryable, "Past the wait, the same question is answered, so the panel must offer to send it again.");
        Assert.IsNotNull(refused.Error.RetryAfter, "The refusal must say how long to wait.");
        Assert.IsTrue(refused.Error.RetryAfter > TimeSpan.Zero && refused.Error.RetryAfter <= TimeSpan.FromMinutes(1),
            $"The wait must be what's left of the limit's minute, not {refused.Error.RetryAfter}.");
    }

    /// <summary>
    /// The whole conversation goes up with every question, so a message too long to be sent as a question must not
    /// reach the model as part of the history either - nor cost the conversation the rest of it.
    /// </summary>
    [TestMethod]
    public async Task AnOverLongMessageInTheConversation_Should_BeDropped_RatherThanFailTheAnswer()
    {
        var chatClient = new TestChatClient();

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppAddress);

        var overLongAnswer = new string('a', AiChatMessage.MaxContentLength + 1);
        var overLongQuestion = new string('b', AiChatMessage.MaxContentLength + 1);

        var kept = new BitAiConversation();
        UserMessage(kept, "what is bit platform?");
        SignedAnswer(kept, scope.ServiceProvider, overLongAnswer);
        UserMessage(kept, overLongQuestion);

        await using var panel = new TestAiChatPanel(scope.ServiceProvider, httpContext.User, kept);

        var answer = await panel.Ask("the newest question", cancellationToken: TestContext.CancellationToken);

        Assert.AreEqual(BitAiMessageStatus.Complete, answer.Status, "The newest question must still be answered.");

        var conversation = chatClient.ReceivedConversations[^1];

        Assert.Contains(message => message.Text == "what is bit platform?", conversation,
            "The conversation must still reach the model, minus what is too long.");

        Assert.DoesNotContain(message => message.Text == overLongAnswer || message.Text == overLongQuestion, conversation,
            "A message longer than AiChatMessage.MaxContentLength reached the model as part of the conversation.");
    }

    [TestMethod]
    public async Task TheModel_Should_BeAskedForAnAnswerNoLongerThanTheConfiguredCap()
    {
        var chatClient = new TestChatClient();

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppAddress);

        await using var panel = new TestAiChatPanel(scope.ServiceProvider, httpContext.User);

        await panel.Ask("what is bit platform?", cancellationToken: TestContext.CancellationToken);

        var configuredCap = server.ApiApp.Services.GetRequiredService<IConfiguration>().GetValue<int?>("AI:ChatOptions:MaxOutputTokens");

        Assert.IsNotNull(configuredCap, "AI:ChatOptions:MaxOutputTokens is not set, so an answer can run as long as the model allows.");
        Assert.IsNotNull(chatClient.LastStreamingOptions, "The model was never asked for an answer.");
        Assert.AreEqual(configuredCap, chatClient.LastStreamingOptions.MaxOutputTokens, "The configured answer length cap did not reach the model.");
    }
}
