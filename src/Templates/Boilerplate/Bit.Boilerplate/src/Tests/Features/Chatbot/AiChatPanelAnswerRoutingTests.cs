using Microsoft.Extensions.AI;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// Each answer streams from <c>AppHub</c> as a stream of its own (See <c>AppHub.Chatbot.cs</c>), in Bit.BlazorUI.AI's
/// events, and the panel's chat session puts them in the answer they belong to. A tool call turns one question into two
/// round trips with the model inside that one stream, and follow-up suggestions come from a tool too (See
/// <c>AppChatbot.ShowFollowUpSuggestions</c>), so what reaches the screen is the agent pipeline's events read back by
/// the library's.
/// <para>
/// Playwright rather than bUnit because these defects live in the crossing: the session is the client's, the events are
/// the server's, and the tools are dispatched by the agent pipeline.
/// </para>
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class AiChatPanelAnswerRoutingTests : AiChatPanelTestBase
{
    private const string FirstQuestion = "first question";
    private const string SecondQuestion = "second question";
    private const string FirstAnswer = "This is the answer to the first question.";
    private const string SecondAnswer = "This is the answer to the second question.";

    private static readonly string[] followUpSuggestions =
    [
        "What else can you do?",
        "Take me to the settings page",
        "Switch to dark mode"
    ];

    /// <summary>Offered through a tool, capped to what fits above the message box, and sent as the user's words when tapped.</summary>
    [TestMethod]
    public async Task Panel_Should_ShowTheSuggestionsTheModelOffered_AndSendTheOneTapped()
    {
        var chatClient = new TestChatClient { StreamingUpdates = OfferSuggestionsThenAnswer };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, FirstQuestion, chatClient);

        await Expect(panel.GetByText(FirstAnswer)).ToBeVisibleAsync();

        // The blank one dropped, and the one too many left out.
        foreach (var suggestion in followUpSuggestions)
        {
            await Expect(Suggestion(panel, suggestion)).ToBeVisibleAsync();
        }

        await Expect(Suggestion(panel, "One too many")).ToHaveCountAsync(0);

        var conversationsBefore = chatClient.ReceivedConversations.Count;

        await Suggestion(panel, followUpSuggestions[2]).ClickAsync();

        Assert.IsTrue(await WaitForServerToReceiveAMessage(chatClient, conversationsBefore, TimeSpan.FromSeconds(15)),
                      "Tapping a suggestion sent nothing.");

        Assert.AreEqual(followUpSuggestions[2], chatClient.ReceivedConversations[^1].Last(message => message.Role == ChatRole.User).Text,
                        "The tapped suggestion must be sent as the user's own words.");

        await Expect(panel.GetByText(SecondAnswer)).ToBeVisibleAsync();

        // Offered for the answer they followed, so they go once the conversation has moved on.
        await Expect(Suggestion(panel, followUpSuggestions[0])).ToHaveCountAsync(0);
    }

    /// <summary>
    /// A tool call turns one question into two round trips with the model, inside the one stream of its answer. The
    /// first round trip ending must not end the answer, nor the stream - or the answer is written into nothing, and the
    /// next question waits on a turn that never ends.
    /// </summary>
    [TestMethod]
    public async Task Panel_Should_ShowTheSecondAnswer_WhenTheFirstAnswerNeededAToolCall()
    {
        var chatClient = new TestChatClient { StreamingUpdates = AskTheTimeThenAnswer };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, FirstQuestion, chatClient);

        // The first answer is only written on the round trip after the tool ran, so seeing it proves that extra round
        // trip happened.
        await Expect(panel.GetByText(FirstAnswer)).ToBeVisibleAsync();

        // Sent through the connection the first message already proved is up, so the conversation survives into the
        // second turn.
        await SendFollowUpMessage(panel, SecondQuestion, chatClient);

        // No timeout of its own: the suite's default is what every other assertion here waits on, and a shorter one
        // turns "the machine is busy" into a failure that reads like the bug.
        await Expect(panel.GetByText(SecondAnswer)).ToBeVisibleAsync();

        // One answer per question, each in its own: two round trips made neither a second answer nor a missing one.
        var answers = panel.Locator("[data-message-id][data-role=assistant]");

        await Expect(answers).ToHaveCountAsync(2);
        await Expect(answers.Nth(0)).ToContainTextAsync(FirstAnswer);
        await Expect(answers.Nth(1)).ToContainTextAsync(SecondAnswer);
    }

    /// <summary>
    /// A model that calls a tool can say so first ("let me look that up"), and write the answer on the round trip after
    /// the tool. Both belong on the screen, the answer under what came before it.
    /// </summary>
    [TestMethod]
    public async Task Panel_Should_ShowTheAnswerWrittenAfterAToolCall_UnderWhatWasWrittenBeforeIt()
    {
        var chatClient = new TestChatClient { StreamingUpdates = SayItThenAskTheTimeThenAnswer };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, FirstQuestion, chatClient);

        await Expect(panel.GetByText(FirstAnswer)).ToBeVisibleAsync();
        await Expect(panel.GetByText(Preamble)).ToBeVisibleAsync();

        var preambleBox = await panel.GetByText(Preamble).BoundingBoxAsync();
        var answerBox = await panel.GetByText(FirstAnswer).BoundingBoxAsync();

        Assert.IsLessThan(answerBox!.Y, preambleBox!.Y, "The answer written after the tool must come under what was written before it.");

        // Two round trips still end the answer once, or the next question would wait on it forever.
        await SendFollowUpMessage(panel, SecondQuestion, chatClient);

        await Expect(panel.GetByText(SecondAnswer)).ToBeVisibleAsync();
    }

    private static ILocator Suggestion(ILocator panel, string text) => panel.GetByRole(AriaRole.Button, new() { Name = text, Exact = true });

    /// <summary>The answer as a real model streams it: in pieces.</summary>
    private static ChatResponseUpdate[] AnswerInPieces(int callIndex, ChatMessage[] conversation)
    {
        var answer = conversation.Last(message => message.Role == ChatRole.User).Text == FirstQuestion ? FirstAnswer : SecondAnswer;

        // Seven characters at a time, so the splits land mid word.
        return [.. answer.Chunk(7).Select(piece => new ChatResponseUpdate(ChatRole.Assistant, new string(piece)))];
    }

    /// <summary>Offers suggestions through the tool for the first question - a blank one and one too many included - then answers.</summary>
    private static ChatResponseUpdate[] OfferSuggestionsThenAnswer(int callIndex, ChatMessage[] conversation)
    {
        var question = conversation.Last(message => message.Role == ChatRole.User).Text;

        if (question != FirstQuestion || conversation.Any(message => message.Contents.OfType<FunctionResultContent>().Any()))
            return AnswerInPieces(callIndex, conversation);

        return
        [
            new ChatResponseUpdate(ChatRole.Assistant, (IList<AIContent>)
            [
                new FunctionCallContent($"suggest-{callIndex}", "ShowFollowUpSuggestions", new Dictionary<string, object?>
                {
                    ["suggestions"] = new[] { followUpSuggestions[0], " ", followUpSuggestions[1], followUpSuggestions[2], "One too many" }
                })
            ])
        ];
    }

    /// <summary>
    /// Calls a tool before answering, like most real turns. Not simulated: <c>AsAIAgent</c> wraps this client in a
    /// <c>FunctionInvokingChatClient</c>, so the real <c>AppChatbot</c> method runs and this client is called again.
    /// </summary>
    private static ChatResponseUpdate[] AskTheTimeThenAnswer(int callIndex, ChatMessage[] conversation)
    {
        if (conversation.Any(message => message.Contents.OfType<FunctionResultContent>().Any()))
            return AnswerInPieces(callIndex, conversation);

        return
        [
            new ChatResponseUpdate(ChatRole.Assistant, (IList<AIContent>)
            [
                new FunctionCallContent($"what-time-is-it-{callIndex}", "GetCurrentDateTime", new Dictionary<string, object?>
                {
                    ["timeZoneId"] = "UTC"
                })
            ])
        ];
    }

    private const string Preamble = "Let me look that up.";

    /// <summary>Says what it is about to do, calls the tool in the same round trip, then answers.</summary>
    private static ChatResponseUpdate[] SayItThenAskTheTimeThenAnswer(int callIndex, ChatMessage[] conversation)
    {
        if (conversation.Any(message => message.Contents.OfType<FunctionResultContent>().Any()))
            return AnswerInPieces(callIndex, conversation);

        return
        [
            new ChatResponseUpdate(ChatRole.Assistant, Preamble),
            .. AskTheTimeThenAnswer(callIndex, conversation)
        ];
    }

    /// <summary>
    /// Boots the app with the model - and only the model - replaced, and opens the panel on the public home page.
    /// </summary>
    private async Task<ILocator> StartChat(TestChatClient chatClient)
    {
        var server = new AppTestServer(Context);

        // Disposed with the test rather than with a using in each method, so the page outlives the arrangement.
        TestCleanup.Add(server);

        await server.Build(configureTestServices: services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IChatClient>(chatClient));
        },
        configureTestConfiguration: configuration =>
        {
            // Without a chat api key the AI agents are never registered and the panel would have nothing to talk to.
            configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-these-tests";
        }).Start(TestContext.CancellationToken);

        await Page.GotoAsync(new Uri(server.WebAppAddress, PageUrls.Home).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        return await OpenChatPanel();
    }

    private readonly List<IAsyncDisposable> TestCleanup = [];

    [TestCleanup]
    public async Task DisposeServers()
    {
        foreach (var disposable in TestCleanup)
        {
            await disposable.DisposeAsync();
        }
    }
}
