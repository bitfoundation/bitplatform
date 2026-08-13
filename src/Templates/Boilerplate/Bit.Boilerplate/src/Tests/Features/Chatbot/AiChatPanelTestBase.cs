namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// What every chat panel test has to do before it can test anything: get the panel open, and get a message all the way
/// to the server. Both are harder than they look in a suite where the transport is SignalR, so they live here rather
/// than being rewritten - subtly differently - in each test.
/// </summary>
public abstract class AiChatPanelTestBase : AppPageTest
{
    /// <summary>
    /// Stands in for the <c>SpeechRecognition</c> a headless browser has no microphone to feed.
    /// <para>
    /// The recognizer that is listening registers itself under <c>window.__microphoneProbe</c>, so a test can report
    /// speech the way a real engine would - including reporting one utterance many times, which every engine does
    /// while it revises what it heard and Android's does again after it has settled.
    /// </para>
    /// </summary>
    protected const string MicrophoneProbeScript = """
        (() => {
            const probe = { recognizer: null, startCount: 0 };
            window.__microphoneProbe = probe;

            window.SpeechRecognition = function () {
                const recognizer = this;
                this.start = () => { probe.recognizer = recognizer; probe.startCount++; };
                this.stop = () => { recognizer.onend?.(); };
            };
        })();
        """;

    /// <summary>
    /// Opens the floating chat button and waits for the panel, including its locally rendered greeting - which is
    /// the signal that the panel is fully interactive rather than merely present in the DOM.
    /// </summary>
    protected async Task<ILocator> OpenChatPanel()
    {
        await Page.Locator(".open-panel-button").ClickAsync();

        var panel = Page.Locator(".panel-cnt");
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel.Locator("textarea")).ToBeVisibleAsync();

        return panel;
    }

    /// <summary>
    /// Types a message, sends it, and waits until the server really received it.
    /// <para>
    /// The wait is not decoration. SignalR traffic is invisible to Playwright's network waits and an anonymous
    /// connection leaves no server-side row to poll (See <c>AppHub.ChangeAuthenticationStateImplementation</c>: a new
    /// anonymous connection is deliberately not recorded), so the only observable proof that the message arrived is
    /// the chatbot calling the model. And if the hub connection was not up yet, <c>AppAiChatPanel.StartChannel</c>
    /// assigns its channel BEFORE <c>StreamAsync</c> throws, which leaves the panel with a channel that is never
    /// read from - a plain resend would go nowhere. Clearing the chat is the shipped recovery for that
    /// (<c>ClearChat</c> -> <c>RestartChannel</c>), so that is what the retry does.
    /// </para>
    /// <para>
    /// The recovery wipes the conversation, so this is for the first message of a test only. A test whose later turns
    /// build on the state the earlier ones left behind - a panel setting, a message it is still pointing at - sends
    /// those with <see cref="SendFollowUpMessage"/>, which fails rather than silently starting over.
    /// </para>
    /// </summary>
    protected async Task SendChatMessage(ILocator panel, string message, TestChatClient chatClient)
    {
        var conversationsBefore = chatClient.ReceivedConversations.Count;

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            if (attempt > 1)
            {
                await panel.GetByTitle(AppStrings.Clear).ClickAsync();
            }

            await panel.Locator("textarea").FillAsync(message);
            await panel.Locator(".send-message-button").ClickAsync();

            if (await WaitForServerToReceiveAMessage(chatClient, conversationsBefore, TimeSpan.FromSeconds(15)))
                return;
        }

        Assert.Fail("The chat message never reached the server, so the hub connection was not established. " +
                    "Nothing after this point can be meaningful.");
    }

    /// <inheritdoc cref="SendChatMessage"/>
    /// <summary>
    /// Sends a further message over the connection the first one already proved is up, leaving the conversation - and
    /// whatever the panel is holding on to across turns - intact.
    /// </summary>
    protected async Task SendFollowUpMessage(ILocator panel, string message, TestChatClient chatClient)
    {
        var conversationsBefore = chatClient.ReceivedConversations.Count;

        await panel.Locator("textarea").FillAsync(message);
        await panel.Locator(".send-message-button").ClickAsync();

        Assert.IsTrue(await WaitForServerToReceiveAMessage(chatClient, conversationsBefore, TimeSpan.FromSeconds(15)),
                      $"The follow-up message '{message}' never reached the server.");
    }

    protected async Task<bool> WaitForServerToReceiveAMessage(TestChatClient chatClient, int conversationsBefore, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (chatClient.ReceivedConversations.Count > conversationsBefore)
                return true;

            await Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.CancellationToken);
        }

        return false;
    }
}
