using Microsoft.Extensions.AI;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// An answer and the follow-up suggestions that belong to it reach the panel by two entirely different routes: the
/// answer is streamed frame by frame down the hub method the panel is enumerating, while the suggestions are written
/// by the model itself through the <c>SendFollowUpSuggestions</c> tool and published to the device (See
/// <c>SharedAppMessages.SHOW_FOLLOW_UP_SUGGESTIONS</c>). Both halves are covered here because neither is observable
/// anywhere else, and because the tool call is what makes the model's turn take more than one round trip - which is
/// exactly the thing the panel's frame routing is fragile about.
/// <para>
/// That routing has no message identity on the wire: the stream is a bare sequence of strings, and which assistant
/// bubble a frame belongs to is worked out by COUNTING terminal markers against the number of questions asked
/// (<c>AppAiChatPanel.RunChannel</c>). One marker too many, or one too few, and the count is permanently out of step
/// with the conversation - after which every frame fails its own "is this the current answer" test and is dropped. The
/// panel then answers nothing, for ever, with no error anywhere: empty bubbles, a loader that flashes and stops, and a
/// server that is answering every question correctly the whole time.
/// </para>
/// <para>
/// Playwright rather than bUnit because every defect these pin lives in the crossing: the counter and the subscription
/// live in the client, the marker and the published suggestions are produced by the server, and the tool that sends
/// them is dispatched by the agent pipeline. Nothing short of the real transport puts those together.
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

    [TestMethod]
    public async Task Panel_Should_ShowTheSuggestions_TheAssistantSendsWithTheFollowUpTool()
    {
        var chatClient = new TestChatClient { StreamingUpdates = AnswerThenSendFollowUpSuggestions };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, FirstQuestion, chatClient);

        await Expect(panel.GetByText(FirstAnswer)).ToBeVisibleAsync();

        // The payoff: these were never part of the answer's stream. They travelled as a published message, from a tool
        // the model called, into the panel's subscription - so a break anywhere along that route shows up right here.
        foreach (var suggestion in followUpSuggestions)
        {
            await Expect(panel.Locator(".default-prompt-button").GetByText(suggestion)).ToBeVisibleAsync();
        }
    }

    /// <summary>
    /// The tool call turns one question into two round trips with the model, and the terminal marker must still be
    /// sent exactly once for it. A second marker - the follow-up round trip reporting itself - would leave the panel's
    /// counter one ahead of the conversation for good, and a counter that is ahead discards every later answer in
    /// silence.
    /// </summary>
    [TestMethod]
    public async Task Panel_Should_ShowTheSecondAnswer_WhenTheFirstAnswerEndedWithAFollowUpToolCall()
    {
        var chatClient = new TestChatClient { StreamingUpdates = AnswerThenSendFollowUpSuggestions };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, FirstQuestion, chatClient);

        await Expect(panel.GetByText(FirstAnswer)).ToBeVisibleAsync();

        // The suggestions of the first answer are on screen, which is the proof that its tool call really ran - the
        // extra round trip this test is about has happened by the time the second question is asked.
        await Expect(panel.Locator(".default-prompt-button").GetByText(followUpSuggestions[0])).ToBeVisibleAsync();

        // Sent through the connection the first message already proved is up, so the conversation - and the counter
        // this test is about - survives into the second turn.
        await SendFollowUpMessage(panel, SecondQuestion, chatClient);

        // No timeout of its own: the suite's default is what every other assertion here waits on, and a shorter one
        // turns "the machine is busy" into a failure that reads like the bug.
        await Expect(panel.GetByText(SecondAnswer)).ToBeVisibleAsync();
    }

    /// <summary>
    /// Behaves the way the seeded system prompt asks the model to: answer, then call <c>SendFollowUpSuggestions</c>
    /// with three suggestions. The call is not simulated - <c>AsAIAgent</c> wraps the client in a
    /// <c>FunctionInvokingChatClient</c>, so the real tool runs with these arguments and really publishes to the
    /// device, and this client is then called again to finish the turn.
    /// </summary>
    private static ChatResponseUpdate[] AnswerThenSendFollowUpSuggestions(int callIndex, ChatMessage[] conversation)
    {
        // The turn after the tool ran. The model has already said everything it had to say, so it adds nothing -
        // which is also what stops this from calling the tool round after round.
        if (conversation.Any(message => message.Contents.OfType<FunctionResultContent>().Any()))
            return [new ChatResponseUpdate(ChatRole.Assistant, "")];

        var question = conversation.Last(message => message.Role == ChatRole.User).Text;

        return
        [
            new ChatResponseUpdate(ChatRole.Assistant, question == FirstQuestion ? FirstAnswer : SecondAnswer),
            new ChatResponseUpdate(ChatRole.Assistant, (IList<AIContent>)
            [
                new FunctionCallContent($"follow-up-{callIndex}", "SendFollowUpSuggestions", new Dictionary<string, object?>
                {
                    ["suggestions"] = JsonSerializer.SerializeToElement(followUpSuggestions)
                })
            ])
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
