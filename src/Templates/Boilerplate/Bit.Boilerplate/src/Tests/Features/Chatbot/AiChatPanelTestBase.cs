namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// What every chat panel test has to do before it can test anything: get the panel open, and get a message all the way
/// to the server. Both are harder than they look in a suite where the transport is SignalR, so they live here rather
/// than being rewritten - subtly differently - in each test.
/// </summary>
public abstract class AiChatPanelTestBase : AppPageTest
{
    /// <summary>
    /// Opens the floating chat button and waits for the panel's message box - the signal that the lazily loaded chat
    /// is there and interactive rather than merely the panel around it.
    /// </summary>
    protected async Task<ILocator> OpenChatPanel()
    {
        // A menu that is only markup yet swallows the click, so it never opens and the wait below times out.
        await Page.WaitForBlazorInteractive();
        await Page.Locator(".open-panel-button").ClickAsync();

        var panel = Page.Locator(".panel-cnt");
        await Expect(panel).ToBeVisibleAsync();
        await Expect(MessageBox(panel)).ToBeVisibleAsync();

        return panel;
    }

    protected async Task WaitUntilKeptOnTheDevice(string text)
    {
        await Page.WaitForFunctionAsync("""
            async text => {
                if ((await indexedDB.databases()).some(database => database.name === 'ai-chat-conversation') === false) return false;
                const database = await new Promise((resolve, reject) => { const request = indexedDB.open('ai-chat-conversation'); request.onsuccess = () => resolve(request.result); request.onerror = () => reject(request.error); });
                try {
                    if (database.objectStoreNames.contains('conversation') === false) return false;
                    const values = await new Promise(resolve => { const request = database.transaction('conversation').objectStore('conversation').getAll(); request.onsuccess = () => resolve(request.result); request.onerror = () => resolve([]); });
                    return values.some(value => (typeof value === 'string' ? value : JSON.stringify(value)).includes(text));
                } finally {
                    database.close();
                }
            }
            """, text, new() { PollingInterval = 100 });
    }

    /// <summary>Where the user types: the panel's only multi-line text box.</summary>
    protected static ILocator MessageBox(ILocator panel) => panel.Locator("textarea").First;

    /// <summary>
    /// Types a message, sends it, and waits until the server really received it.
    /// <para>
    /// The wait is not decoration. SignalR traffic is invisible to Playwright's network waits and an anonymous
    /// connection leaves no server-side row to poll (See <c>AppHub.ChangeAuthenticationStateImplementation</c>: a new
    /// anonymous connection is deliberately not recorded), so the only observable proof that the message arrived is
    /// the chatbot calling the model. A question sent before the app's hub connection is up fails as an answer the
    /// user can retry; clearing the chat and asking again is what the retry here does.
    /// </para>
    /// <para>
    /// Clearing wipes the conversation, so this is for the first message of a test only. A test whose later turns
    /// build on the state the earlier ones left behind sends those with <see cref="SendFollowUpMessage"/>, which fails
    /// rather than silently starting over.
    /// </para>
    /// </summary>
    protected async Task SendChatMessage(ILocator panel, string message, TestChatClient chatClient)
    {
        var conversationsBefore = chatClient.ReceivedConversations.Count;

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            if (attempt > 1)
            {
                await panel.GetByRole(AriaRole.Button, new() { Name = AppStrings.Clear, Exact = true }).ClickAsync();
            }

            await Send(panel, message);

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

        await Send(panel, message);

        Assert.IsTrue(await WaitForServerToReceiveAMessage(chatClient, conversationsBefore, TimeSpan.FromSeconds(15)),
                      $"The follow-up message '{message}' never reached the server.");
    }

    /// <summary>Sends as a user does: Enter in the message box.</summary>
    protected static async Task Send(ILocator panel, string message)
    {
        await MessageBox(panel).FillAsync(message);
        await MessageBox(panel).PressAsync("Enter");
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
