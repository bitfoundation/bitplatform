using Microsoft.Extensions.AI;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// The panel has no message identity on the wire: the stream is a bare sequence of strings, and which assistant
/// bubble a frame belongs to is worked out by COUNTING terminal markers against the number of questions asked
/// (<c>AppAiChatPanel.HandleStreamedResponse</c>). One marker too many, or one too few, and the count is permanently
/// out of step with the conversation - after which every frame fails its own "is this the current answer" test and is
/// dropped. The panel then answers nothing, for ever, with no error anywhere: empty bubbles, a loader that flashes
/// and stops, and a server that is answering every question correctly the whole time.
/// <para>
/// The drift this pins is <b>a marker too many</b>. The server sends the answer's terminal marker and THEN generates
/// follow-up suggestions on the same message's cancellation token. Ask the next question while those are still being
/// generated - which is ordinary use: the input box is idle and the answer is already on screen - and that generation
/// is cancelled. Its failure used to be reported as a second terminal marker for a message that was already answered.
/// </para>
/// <para>
/// Playwright rather than bUnit because the defect only exists in the crossing: the counter lives in the client, the
/// extra marker is produced by the server, and the cancellation that produces it is raised by the hub. Nothing short
/// of the real transport puts those three together.
/// </para>
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class AiChatPanelAnswerRoutingTests : AiChatPanelTestBase
{
    private const string FirstAnswer = "This is the answer to the first question.";
    private const string SecondAnswer = "This is the answer to the second question.";

    [TestMethod]
    public async Task Panel_Should_ShowTheSecondAnswer_WhenTheFirstMessagesFollowUpSuggestionsAreStillBeingGenerated()
    {
        var chatClient = new TestChatClient
        {
            StreamingChunks = callIndex => [callIndex == 0 ? FirstAnswer : SecondAnswer],

            // Held open for the whole test. The follow-up suggestions for question one are therefore still in flight
            // when question two arrives, which is what makes the hub cancel them.
            PauseNonStreamingResponse = new TaskCompletionSource()
        };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, "first question", chatClient);

        await Expect(panel.GetByText(FirstAnswer)).ToBeVisibleAsync();

        // The answer being on screen is NOT proof that its turn is over: the agent's stream can still be yielding
        // trailing updates, and interrupting THERE is a different code path (the in-loop cancellation check), which
        // would make this test pass or fail on timing. The follow-up agent only runs once the answer's terminal
        // marker has gone out, so waiting for its call is what pins the state this test is about: question one
        // answered, and its suggestions still being generated.
        await WaitForFollowUpGenerationToStart(chatClient);

        // Sent through the connection the first message already proved is up, so the conversation - and the counter
        // this test is about - survives into the second turn.
        await SendFollowUpMessage(panel, "second question", chatClient);

        // The payoff. Before the fix this answer was streamed by the server, accepted by the client's stream loop,
        // and then discarded frame by frame because the counter said it belonged to an older question.
        // No timeout of its own: the suite's default is what every other assertion here waits on, and a shorter one
        // turns "the machine is busy" into a failure that reads like the bug.
        await Expect(panel.GetByText(SecondAnswer)).ToBeVisibleAsync();
    }

    /// <summary>
    /// Waits until the follow-up suggestions agent has been called for the answer that just finished. It is
    /// recognisable without guessing at counts: it is the only call whose conversation ends with the assistant
    /// message the streaming agent produced.
    /// </summary>
    private async Task WaitForFollowUpGenerationToStart(TestChatClient chatClient)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (chatClient.ReceivedConversations.Any(conversation => conversation.LastOrDefault()?.Text == FirstAnswer))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.CancellationToken);
        }

        Assert.Fail("The follow-up suggestions for the first answer were never requested, so the state this test " +
                    "depends on - answered, but not finished - was never reached.");
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
