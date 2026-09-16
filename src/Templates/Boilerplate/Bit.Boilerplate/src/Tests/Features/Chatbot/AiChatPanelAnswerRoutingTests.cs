using Microsoft.Extensions.AI;
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// A turn is one json document (See <see cref="AssistantTurn"/>), streamed down the hub method the panel is
/// enumerating, so what reaches the screen is a property of a document reassembled from prefixes rather than the frames
/// themselves (<c>AppAiChatPanel.RunChannel</c>, <see cref="PartialJsonReader{T}"/>). Follow-up suggestions come the other
/// way, from a tool the model calls (See <c>AppChatbot.ShowFollowUpSuggestions</c>).
/// <para>
/// The wire carries no message identity: a frame belongs to whichever question has waited longest, and a turn ends
/// when its document does. One document too many or too few and the queue is out of step with the conversation for
/// good - after which every answer lands in the wrong bubble or none, with no error anywhere.
/// </para>
/// <para>
/// Playwright rather than bUnit because these defects live in the crossing: the reader is the client's, the document
/// is the server's, and the tools are dispatched by the agent pipeline.
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

        var suggestions = panel.Locator(".default-prompt-button");

        // The blank one dropped, and the one too many left out.
        await Expect(suggestions).ToHaveTextAsync(followUpSuggestions);

        // And the document around the answer is the panel's business, never the user's.
        await Expect(panel).Not.ToContainTextAsync("sentAt");

        var conversationsBefore = chatClient.ReceivedConversations.Count;

        await suggestions.GetByText(followUpSuggestions[2]).ClickAsync();

        Assert.IsTrue(await WaitForServerToReceiveAMessage(chatClient, conversationsBefore, TimeSpan.FromSeconds(15)),
                      "Tapping a suggestion sent nothing.");

        Assert.AreEqual(followUpSuggestions[2], chatClient.ReceivedConversations[^1].Last(message => message.Role == ChatRole.User).Text,
                        "The tapped suggestion must be sent as the user's own words.");

        await Expect(panel.GetByText(SecondAnswer)).ToBeVisibleAsync();
        await Expect(suggestions).ToHaveCountAsync(0);
    }

    /// <summary>
    /// A tool call turns one question into two round trips with the model, and the turn must still be exactly one
    /// document. A second closing - the extra round trip reporting itself - would end a turn that isn't over and put
    /// the panel's queue one ahead of the conversation for good, discarding every later answer in silence.
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

        // Sent through the connection the first message already proved is up, so the conversation - and the queue this
        // test is about - survives into the second turn.
        await SendFollowUpMessage(panel, SecondQuestion, chatClient);

        // No timeout of its own: the suite's default is what every other assertion here waits on, and a shorter one
        // turns "the machine is busy" into a failure that reads like the bug.
        await Expect(panel.GetByText(SecondAnswer)).ToBeVisibleAsync();
    }

    /// <summary>
    /// A model that calls a tool can say so first ("let me look that up"), and write the answer on the round trip after
    /// the tool. Only what came before the tool used to reach the screen, so the answer never did.
    /// </summary>
    [TestMethod]
    public async Task Panel_Should_ShowTheAnswerWrittenAfterAToolCall_UnderWhatWasWrittenBeforeIt()
    {
        var chatClient = new TestChatClient { StreamingUpdates = SayItThenAskTheTimeThenAnswer };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, FirstQuestion, chatClient);

        await Expect(panel.GetByText(FirstAnswer)).ToBeVisibleAsync();
        await Expect(panel.GetByText(Preamble)).ToBeVisibleAsync();
        await Expect(panel).Not.ToContainTextAsync("sentAt");

        // Two round trips still close the turn once, or the next answer would land in no bubble.
        await SendFollowUpMessage(panel, SecondQuestion, chatClient);

        await Expect(panel.GetByText(SecondAnswer)).ToBeVisibleAsync();
    }

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

        await server.Build(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IChatClient>(chatClient));
        },
        configuration =>
        {
            // Without a chat api key the AI agents are never registered and the panel would have nothing to talk to.
            configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-these-tests";
        }).Start(TestContext.CancellationToken);

        await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.Home).ToString(),
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
