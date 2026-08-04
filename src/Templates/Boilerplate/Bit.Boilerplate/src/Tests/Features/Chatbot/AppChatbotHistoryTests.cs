using Microsoft.Extensions.AI;
using System.Threading.Channels;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Server.Api.Infrastructure.SignalR;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// The chat history <c>AppChatbot</c> keeps is a private <c>List&lt;ChatMessage&gt;</c> that nothing reads back: the
/// only place its contents become observable is the message list handed to the model on the next turn. That is why
/// these tests drive the real service against a scripted <see cref="TestChatClient"/> instead of asserting on state -
/// what the model is shown IS the behaviour, and a wrong history is invisible in every other way until the assistant
/// starts answering the wrong question.
/// <para>
/// Everything here is real except the model: the seeded system prompt is read from the database, the SupportAgent is
/// resolved from DI with its real tools, and the answer travels through the real response channel.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class AppChatbotHistoryTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// An answer that is cancelled half way is left on the user's screen, tagged as canceled - but it must not become
    /// part of the conversation the model is shown, or the model reads a truncated sentence as a complete previous
    /// answer of its own and continues from a thought it never finished. An earlier fix marked such a turn with an
    /// <c>[interrupted]</c> suffix instead of dropping it, which put a marker string into the prompt and made the
    /// model's view of the conversation differ from the client's.
    /// </summary>
    [TestMethod]
    public async Task AnInterruptedAnswer_Should_NotBeReplayedToTheModel()
    {
        var chatClient = new TestChatClient
        {
            StreamingChunks = _ => ["bit platform is", " a set of tools"],
            PauseAfterFirstChunk = new TaskCompletionSource()
        };

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppServerAddress);

        var chatbot = scope.ServiceProvider.GetRequiredService<AppChatbot>();
        await chatbot.StartChat(new StartChatRequest(), signalRConnectionId: "test-connection-id", TestContext.CancellationToken);

        var responses = chatbot.GetStreamingChannel();

        using var firstMessageCts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        var firstMessage = chatbot.ProcessNewMessage(generateFollowUpSuggestions: false, "what is bit platform?", httpContext.User, firstMessageCts.Token);

        // Reading the first chunk back is what makes the interruption deterministic: the answer is provably half
        // delivered - the user has seen this text - at the moment the message is cancelled.
        Assert.AreEqual("bit platform is", await ReadNextResponse(responses, "the first streamed chunk of the answer"),
            "The chatbot did not stream the model's first chunk through to the client.");

        await firstMessageCts.CancelAsync();

        // Interrupting a message is an ordinary, one-keystroke user action, not a fault.
        await firstMessage;

        // The terminal marker has to be sent with CancellationToken.None: on an unbounded channel WriteAsync
        // short-circuits on an already-cancelled token without enqueuing anything, and the client advances its
        // response counter only when a marker arrives - one missed marker silently discards every later answer.
        Assert.AreEqual(SharedAppMessages.MESSAGE_PROCESS_ERROR, await ReadNextResponse(responses, "the terminal marker of the interrupted message"),
            "An interrupted message must still emit its terminal marker, and it must be the error one - that is what the client renders as the 'Canceled' tag.");

        // The next message is what exposes the history.
        chatClient.PauseAfterFirstChunk = null;
        chatClient.StreamingChunks = _ => ["Bit.BlazorUI is a component library."];

        await chatbot.ProcessNewMessage(generateFollowUpSuggestions: false, "and what is Bit.BlazorUI?", httpContext.User, TestContext.CancellationToken);

        var secondConversation = chatClient.ReceivedConversations[1];

        Assert.IsFalse(secondConversation.Any(message => message.Role == ChatRole.Assistant),
            $"The interrupted answer was replayed to the model as a finished assistant turn. Conversation: {Describe(secondConversation)}");

        Assert.IsFalse(secondConversation.Any(message => message.Text?.Contains("interrupted", StringComparison.OrdinalIgnoreCase) is true),
            $"A truncated turn was kept and tagged with a marker string. AiChatMessage.Successful already carries that signal typed, and the client already renders it. Conversation: {Describe(secondConversation)}");

        Assert.IsTrue(secondConversation.Any(message => message.Role == ChatRole.User && message.Text == "what is bit platform?"),
            $"The interrupted USER turn must survive - the user asked it and never got an answer, so the model still owes one. Conversation: {Describe(secondConversation)}");
    }

    /// <summary>
    /// The other half of the same contract, so "drop the interrupted turn" can never be simplified into "drop the
    /// assistant turn": a completed answer must reach the next turn whole, reassembled from every streamed chunk.
    /// </summary>
    [TestMethod]
    public async Task ACompletedAnswer_Should_BeReplayedToTheModelInFull()
    {
        var chatClient = new TestChatClient { StreamingChunks = _ => ["bit platform ", "is a set ", "of tools."] };

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppServerAddress);

        var chatbot = scope.ServiceProvider.GetRequiredService<AppChatbot>();
        await chatbot.StartChat(new StartChatRequest(), signalRConnectionId: "test-connection-id", TestContext.CancellationToken);

        var responses = chatbot.GetStreamingChannel();

        await chatbot.ProcessNewMessage(generateFollowUpSuggestions: false, "what is bit platform?", httpContext.User, TestContext.CancellationToken);

        // Three chunks, then the success marker - the client needs that marker to stop its loader and to keep its
        // response counter in step with the number of user turns.
        Assert.AreEqual("bit platform ", await ReadNextResponse(responses, "the first streamed chunk"));
        Assert.AreEqual("is a set ", await ReadNextResponse(responses, "the second streamed chunk"));
        Assert.AreEqual("of tools.", await ReadNextResponse(responses, "the third streamed chunk"));
        Assert.AreEqual(SharedAppMessages.MESSAGE_PROCESS_SUCCESS, await ReadNextResponse(responses, "the terminal marker of the completed message"));

        await chatbot.ProcessNewMessage(generateFollowUpSuggestions: false, "and what is Bit.BlazorUI?", httpContext.User, TestContext.CancellationToken);

        var secondConversation = chatClient.ReceivedConversations[1];

        var assistantTurns = secondConversation.Where(message => message.Role == ChatRole.Assistant).ToArray();

        Assert.HasCount(1, assistantTurns,
            $"The completed answer must appear exactly once in the next turn's conversation. Conversation: {Describe(secondConversation)}");

        Assert.AreEqual("bit platform is a set of tools.", assistantTurns[0].Text,
            "The answer the model is shown must be every streamed chunk reassembled, with nothing added and nothing lost.");
    }

    /// <summary>
    /// The server keeps the history in memory for the lifetime of one SignalR connection, so on a reconnect (or when
    /// the panel is reopened) the client resends everything it has on screen - including the answers it tagged as
    /// canceled. <c>StartChat</c> therefore has to drop exactly what <c>ProcessNewMessage</c> drops, or the model sees
    /// one conversation before a reconnect and a different one after it.
    /// <para>
    /// This works only because <c>AiChatMessage.Successful</c> reaches the server at all: it used to be
    /// <c>[JsonIgnore]</c>d, which made the flag a client-only affair (See <c>AiChatMessageWireContractTests</c>).
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task AResentHistory_Should_DropTheTurnsTheClientMarkedUnsuccessful()
    {
        var chatClient = new TestChatClient();

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppServerAddress);

        var chatbot = scope.ServiceProvider.GetRequiredService<AppChatbot>();

        // Exactly what AppAiChatPanel holds after one answered question and one interrupted one: the greeting it
        // renders locally, the two user turns, the truncated answer it tagged as canceled, and the empty assistant
        // placeholder it creates for every in-flight answer.
        await chatbot.StartChat(new StartChatRequest
        {
            ChatMessagesHistory =
            [
                new() { Role = AiChatMessageRole.Assistant, Content = "Hi, how can I help?" },
                new() { Role = AiChatMessageRole.User, Content = "what is bit platform?" },
                new() { Role = AiChatMessageRole.Assistant, Content = "bit platform is" , Successful = false },
                new() { Role = AiChatMessageRole.User, Content = "and what is Bit.BlazorUI?" },
                new() { Role = AiChatMessageRole.Assistant, Content = null }
            ]
        }, signalRConnectionId: "test-connection-id", TestContext.CancellationToken);

        await chatbot.ProcessNewMessage(generateFollowUpSuggestions: false, "are they free?", httpContext.User, TestContext.CancellationToken);

        var conversation = chatClient.ReceivedConversations[0];

        Assert.IsFalse(conversation.Any(message => message.Text?.Contains("bit platform is", StringComparison.Ordinal) is true),
            $"The canceled answer the client resent was replayed to the model. Conversation: {Describe(conversation)}");

        Assert.IsFalse(conversation.Any(message => string.IsNullOrWhiteSpace(message.Text)),
            $"The panel's empty placeholder for the in-flight answer was replayed to the model as a blank assistant turn. Conversation: {Describe(conversation)}");

        // The greeting, the two earlier questions and the new one - plus the '### Variables:' system message.
        Assert.HasCount(5, conversation, $"Conversation: {Describe(conversation)}");

        Assert.AreEqual("are they free?", conversation[^1].Text, "The new user message must be the last thing the model reads.");
    }

    /// <summary>
    /// The whole conversation is resent to the provider on every single message, so an unbounded history grows the
    /// prompt - and the bill - until the provider rejects it for exceeding the context window. The cap has to bite
    /// before the conversation is materialized, not after.
    /// </summary>
    [TestMethod]
    public async Task AnOverlongConversation_Should_BeTrimmedBeforeItReachesTheModel()
    {
        const int maxChatMessages = 40; // Keep in step with AppChatbot.TrimChatHistory.

        var chatClient = new TestChatClient();

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppServerAddress);

        var chatbot = scope.ServiceProvider.GetRequiredService<AppChatbot>();

        await chatbot.StartChat(new StartChatRequest
        {
            ChatMessagesHistory = [.. Enumerable.Range(0, maxChatMessages * 2).Select(index => new AiChatMessage
            {
                Content = $"message {index}",
                Role = index % 2 == 0 ? AiChatMessageRole.User : AiChatMessageRole.Assistant
            })]
        }, signalRConnectionId: "test-connection-id", TestContext.CancellationToken);

        await chatbot.ProcessNewMessage(generateFollowUpSuggestions: false, "the newest question", httpContext.User, TestContext.CancellationToken);

        var conversation = chatClient.ReceivedConversations[0];

        // The '### Variables:' system message is prepended to the trimmed history rather than counted against it.
        Assert.HasCount(maxChatMessages + 1, conversation,
            "An 80 message history plus the new question must reach the model trimmed to the newest 40 turns (plus the Variables system message).");

        Assert.AreEqual("the newest question", conversation[^1].Text, "Trimming must drop the OLDEST turns, never the newest.");

        Assert.IsFalse(conversation.Any(message => message.Text == "message 0"),
            $"The oldest turn survived a trim that was supposed to remove it. Conversation: {Describe(conversation)}");
    }

    /// <summary>
    /// Builds the real server with the model - and only the model - replaced.
    /// </summary>
    private static AppTestServer BuildServerWith(TestChatClient chatClient)
    {
        return new AppTestServer().Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();

            // Every agent is built on the DI IChatClient (See AddAppAIAgents), so this single replacement removes the
            // network, the api key and the non-determinism while leaving the agent itself untouched.
            services.Replace(ServiceDescriptor.Singleton<IChatClient>(chatClient));
        },
        configuration =>
        {
            // Without a chat api key neither AddChatClient nor AddAppAIAgents runs, and the keyed "SupportAgent"
            // would not exist at all. The value itself is never used: the client it builds is replaced above.
            configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-these-tests";
        });
    }

    /// <summary>
    /// <c>AppChatbot</c> reads <c>IHttpContextAccessor</c> for the <c>{{WebAppUrl}}</c> variable, and - under
    /// multitenancy - <c>TenantProvider</c> reads it again while the seeded system prompt is resolved.
    /// <c>AppHub.StartChat</c> performs this same assignment for the same reason, because Azure SignalR never
    /// populates the accessor itself.
    /// </summary>
    private static HttpContext SetCurrentHttpContext(IServiceProvider scopedServices, Uri serverAddress)
    {
        var httpContext = new DefaultHttpContext { RequestServices = scopedServices };
        httpContext.Request.Scheme = serverAddress.Scheme;
        httpContext.Request.Host = new HostString(serverAddress.Authority);

        scopedServices.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        return httpContext;
    }

    private async Task<string> ReadNextResponse(ChannelReader<string> responses, string expected)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            return await responses.ReadAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (TestContext.CancellationToken.IsCancellationRequested is false)
        {
            throw new AssertFailedException($"Nothing arrived on the chatbot's response channel within 30 seconds while waiting for {expected}.");
        }
    }

    private static string Describe(IEnumerable<ChatMessage> conversation)
    {
        return string.Join(" | ", conversation.Select(message => $"{message.Role}: '{message.Text}'"));
    }
}
