//+:cnd:noEmit
using Microsoft.Extensions.AI;
using Boilerplate.Shared.Features.Chatbot;
//#if (module == "Sales")
//#if (database == "PostgreSQL" || database == "SqlServer")
using Boilerplate.Server.Api.Infrastructure.SignalR;
//#endif
//#endif

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// The cards a tool shows in the conversation, end to end with only the model scripted: the real tool says what to show,
/// the real hub streams it with the answer, and the real panel draws it from the tool call (See AppAiChat.RegisterRenderers),
/// keeps it on the device and sends it back with the answer, signed.
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class AiChatPanelCardsUITests : AiChatPanelTestBase
{
    private const string ClearFilesQuestion = "the app keeps crashing, please fix it";

    //#if (module == "Sales")
    //#if (database == "PostgreSQL" || database == "SqlServer")
    /// <summary>
    /// The model only picks cars by id; the cards show the database's data, above the answer, and reach later turns as the
    /// assistant's signed message - after a reload too.
    /// </summary>
    [TestMethod]
    public async Task ProductCards_Should_ShowWhatTheToolLoaded_AndReachLaterTurnsAsTheAssistants()
    {
        const string question = "show me electric SUVs";
        const string answer = "Both are electric SUVs; the first one seats seven.";
        const string laterAnswer = "It seats seven.";

        var chatClient = new TestChatClient
        {
            StreamingUpdates = (_, conversation) =>
            {
                if (LastQuestion(conversation) != question)
                    return Answer(laterAnswer);

                if (ToolsRanSinceTheQuestion(conversation))
                    return Answer(answer);

                return ToolCalls(ShowProducts("Electric SUVs",
                                              new(10000, ["Seats seven", " ", "Electric", "All-wheel drive", "One too many"]),
                                              new(99999, ["A car that doesn't exist"]),
                                              new(10000, ["The same car again"]),
                                              new(10003, null)),
                                 ShowFollowUpSuggestions("Which one is cheaper?"));
            }
        };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, question, chatClient);

        var cards = panel.Locator(".products .product");

        // Numbered in the model's order, without the car that doesn't exist and the repeated one; the first leads.
        await Expect(panel.Locator(".products .product-name")).ToHaveTextAsync(["1. EQB SUV", "2. EQE SUV"]);
        await Expect(cards.Nth(0)).ToContainTextAsync("Best match");

        // The model's highlights, without the blank one and no more than three.
        await Expect(cards.Nth(0)).ToContainTextAsync("All-wheel drive");
        await Expect(cards.Nth(0)).Not.ToContainTextAsync("One too many");
        await Expect(cards.Nth(0)).Not.ToContainTextAsync("The same car again");

        // The answer streams in under the cards it talks about.
        await Expect(panel.GetByText(answer)).ToBeVisibleAsync();

        var cardsBox = await panel.Locator(".products").First.BoundingBoxAsync();
        var answerBox = await panel.GetByText(answer).BoundingBoxAsync();

        Assert.IsLessThan(answerBox!.Y, cardsBox!.Y, "The cards must come before the answer of their turn.");

        await Expect(panel.GetByRole(AriaRole.Button, new() { Name = "Which one is cheaper?", Exact = true })).ToBeVisibleAsync();

        // So "open the second one" has a page to open: GetAppPages lists no product pages.
        var shownResult = await ToolResult(chatClient, "show-products");

        Assert.Contains("2. [EQE SUV](/product/10003)", shownResult, $"The model must be told which page each numbered card opens. It was told: {shownResult}");

        // Asking about a car on its card is the user's next message.
        var conversationsBefore = chatClient.ReceivedConversations.Count;

        await cards.Nth(0).GetByRole(AriaRole.Button, new() { Name = "Ask" }).ClickAsync();

        Assert.IsTrue(await WaitForServerToReceiveAMessage(chatClient, conversationsBefore, TimeSpan.FromSeconds(15)),
                      "Asking about a car on its card sent nothing.");

        var nextConversation = chatClient.ReceivedConversations[^1];

        Assert.AreEqual("Tell me more about the EQB SUV", LastQuestion(nextConversation));

        AssertShownTheCardsAsTheAssistants(nextConversation);

        await Expect(panel.GetByText(laterAnswer)).ToBeVisibleAsync();

        // Reloaded: the cards come off the device, and the new connection believes them only by their signature.
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        panel = await OpenChatPanel();

        await Expect(panel.Locator(".products .product-name")).ToHaveTextAsync(["1. EQB SUV", "2. EQE SUV"]);
        await Expect(panel.GetByText(laterAnswer)).ToBeVisibleAsync();

        await SendWithoutClearing(panel, "and the other one?", chatClient);

        AssertShownTheCardsAsTheAssistants(chatClient.ReceivedConversations[^1]);
    }

    /// <summary>On a phone a car opened from its card closes the panel, and no more than six cars are shown.</summary>
    [TestMethod]
    public async Task OnAPhone_ProductCards_Should_ShowAtMostSix_AndOpenTheCarOverThePanel()
    {
        await Page.SetViewportSizeAsync(400, 800);

        int[] productIds = [10000, 10001, 10003, 10004, 10005, 10006, 10007, 10008];

        var chatClient = new TestChatClient
        {
            StreamingUpdates = (_, conversation) => ToolsRanSinceTheQuestion(conversation)
                ? Answer("Here are our cars.")
                : ToolCalls(ShowProducts("All cars", [.. productIds.Select(id => new AppChatbot.ProductToShow(id, null))]))
        };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, "show me all your cars", chatClient);

        await Expect(panel.Locator(".products .product-name")).ToHaveTextAsync(["1. EQB SUV", "2. EQE Sedan", "3. EQE SUV", "4. EQS SUV", "5. EQS Sedan", "6. GLA SUV"]);
        await Expect(panel.GetByText("Here are our cars.")).ToBeVisibleAsync();

        await panel.GetByRole(AriaRole.Link, new() { Name = "1. EQB SUV", Exact = true }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex($"{PageUrls.Product}/10000$"));
        await Expect(panel).ToBeHiddenAsync();
    }
    //#endif
    //#endif

    /// <summary>Waits for the user's tap, through closing and reopening the panel, and clears nothing when declined.</summary>
    [TestMethod]
    public async Task ClearingTheAppFiles_Should_WaitForTheUsersTap_AndClearNothingWhenDeclined()
    {
        const string answer = "Okay, I left your app's files as they are.";

        var chatClient = new TestChatClient
        {
            StreamingUpdates = (_, conversation) => ToolsRanSinceTheQuestion(conversation)
                ? Answer(answer)
                : ToolCalls(new FunctionCallContent("clear-files", "ClearAppFiles"))
        };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, ClearFilesQuestion, chatClient);

        // What clearing does is the app's own words, under the question, never the model's (See AiChatApprovalDetails).
        await Expect(panel.GetByText(AppStrings.AiChatApprovalClearAppFilesRestart)).ToBeVisibleAsync();
        await Expect(Approve(panel)).ToBeVisibleAsync();

        // Closing the panel ends no wait, and the launcher says an answer is still wanted.
        await panel.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();

        await Expect(panel).ToBeHiddenAsync();
        await Expect(Page.Locator(".open-panel-button.needs-answer")).ToBeVisibleAsync();

        panel = await OpenChatPanel();

        await Decline(panel).ClickAsync();

        await Expect(panel.GetByText(answer)).ToBeVisibleAsync();
        await Expect(Approve(panel)).ToHaveCountAsync(0);
        await Expect(Page.Locator(".open-panel-button.needs-answer")).ToHaveCountAsync(0);

        var result = await ToolResult(chatClient, "clear-files");

        Assert.Contains("declined", result, StringComparison.OrdinalIgnoreCase, $"The tool must tell the model the user declined. It said: {result}");

        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        panel = await OpenChatPanel();

        // The decision is kept with the conversation, so the question isn't asked again.
        await Expect(panel.GetByText(ClearFilesQuestion)).ToBeVisibleAsync();
        await Expect(panel.GetByText(answer)).ToBeVisibleAsync();
        await Expect(Approve(panel)).ToHaveCountAsync(0);
    }

    /// <summary>Sending another message instead of answering stops the answer waiting on it, and clears nothing.</summary>
    [TestMethod]
    public async Task ClearingTheAppFiles_Should_ClearNothing_WhenTheUserMovesOnWithoutAnswering()
    {
        const string nextQuestion = "never mind, what cars do you sell?";
        const string nextAnswer = "All kinds of cars.";

        var chatClient = new TestChatClient
        {
            StreamingUpdates = (_, conversation) => LastQuestion(conversation) == nextQuestion
                ? Answer(nextAnswer)
                : ToolsRanSinceTheQuestion(conversation)
                    ? Answer("Okay.")
                    : ToolCalls(new FunctionCallContent("clear-files", "ClearAppFiles"))
        };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, ClearFilesQuestion, chatClient);

        await Expect(Approve(panel)).ToBeVisibleAsync();

        await SendFollowUpMessage(panel, nextQuestion, chatClient);

        await Expect(panel.GetByText(nextAnswer)).ToBeVisibleAsync();
        await Expect(Approve(panel)).ToHaveCountAsync(0);
        await Expect(Page.Locator(".open-panel-button.needs-answer")).ToHaveCountAsync(0);

        Assert.DoesNotContain(message => message.Role == ChatRole.Assistant, chatClient.ReceivedConversations[^1],
            "The answer stopped while it waited on the user is not the model's to read.");

        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        panel = await OpenChatPanel();

        await Expect(panel.GetByText(nextAnswer)).ToBeVisibleAsync();
        await Expect(Approve(panel)).ToHaveCountAsync(0);
    }

    /// <summary>Approved on the card, the conversation is deleted from the device and the app restarts.</summary>
    [TestMethod]
    public async Task ClearingTheAppFiles_Should_ClearThem_OnceTheUserApproves()
    {
        const string greeting = "hello";
        const string greetingAnswer = "Hi, how can I help?";

        var chatClient = new TestChatClient
        {
            StreamingUpdates = (_, conversation) => LastQuestion(conversation) == greeting
                ? Answer(greetingAnswer)
                : ToolsRanSinceTheQuestion(conversation)
                    ? Answer("Clearing your app's files.")
                    : ToolCalls(new FunctionCallContent("clear-files", "ClearAppFiles"))
        };

        var panel = await StartChat(chatClient);
        var appUrl = Page.Url;

        // The conversation is kept as each answer ends, and the one waiting on the approval won't have yet: so one answered
        // before it, for the conversation to be there to clear.
        await SendChatMessage(panel, greeting, chatClient);
        await Expect(panel.GetByText(greetingAnswer)).ToBeVisibleAsync();

        await SendFollowUpMessage(panel, ClearFilesQuestion, chatClient);

        await Expect(Approve(panel)).ToBeVisibleAsync();

        await Eventually(async () => await StoredConversationLength() > 0, "Control: the conversation must be stored before the files are cleared.");

        // Set on the page about to be cleared, so the app restarting shows as the flag going missing.
        await Page.EvaluateAsync("() => window.beforeClearingTheAppFiles = true");

        await Approve(panel).ClickAsync();

        var result = await ToolResult(chatClient, "clear-files");

        Assert.Contains("approved", result, StringComparison.OrdinalIgnoreCase, $"The tool must tell the model the user approved. It said: {result}");

        await Eventually(() => Page.EvaluateAsync<bool>("""
            async () => window.beforeClearingTheAppFiles !== true
                        || (await indexedDB.databases()).every(database => database.name !== 'ai-chat-conversation')
            """), "Nothing was cleared: the conversation's database is still there, and the app never restarted.");

        // The app may be restarting itself right now, which aborts a navigation of our own.
        await Eventually(async () =>
        {
            await Page.GotoAsync(appUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            return true;
        }, "The app could not be opened again after its files were cleared.");

        Assert.AreEqual(0, await StoredConversationLength(), "The conversation is still stored on the device after the app's files were cleared.");
    }

    /// <summary>A sent contact form stays sent after a reload, rather than asking again for what was already sent.</summary>
    [TestMethod]
    public async Task ASentContactForm_Should_StaySent_AfterAReload()
    {
        const string sent = "Your request was sent successfully. A human operator will follow up with you soon.";

        var chatClient = new TestChatClient
        {
            StreamingUpdates = (_, conversation) => ToolsRanSinceTheQuestion(conversation)
                ? Answer("Please leave your contact details in the form.")
                : ToolCalls(new FunctionCallContent("follow-up", "RequestHumanFollowUp", new Dictionary<string, object?> { ["conversationSummary"] = "The user can't sign in." }))
        };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, "I still can't sign in", chatClient);

        var form = panel.Locator("form");

        await form.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("someone@example.com");

        // The email field binds after a debounce, so an early tap only shows a validation message.
        await Eventually(async () =>
        {
            if (await panel.GetByText(sent).IsVisibleAsync()) return true;

            await form.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

            return false;
        }, "The contact form was never sent.");

        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        panel = await OpenChatPanel();

        await Expect(panel.GetByText(sent)).ToBeVisibleAsync();
        await Expect(panel.GetByRole(AriaRole.Textbox, new() { Name = "Email" })).ToHaveCountAsync(0);
    }

    /// <summary>
    /// A contact form nobody sent comes back after a reload still to be sent: the model was told it showed one (See
    /// AiChatProtocol.Replay), so it won't offer another.
    /// </summary>
    [TestMethod]
    public async Task AnUnsentContactForm_Should_ComeBackStillToBeSent_AfterAReload()
    {
        const string answer = "Please leave your contact details in the form.";

        var chatClient = new TestChatClient
        {
            StreamingUpdates = (_, conversation) => ToolsRanSinceTheQuestion(conversation)
                ? Answer(answer)
                : ToolCalls(new FunctionCallContent("follow-up", "RequestHumanFollowUp", new Dictionary<string, object?> { ["conversationSummary"] = "The user can't sign in." }))
        };

        var panel = await StartChat(chatClient);

        await SendChatMessage(panel, "I still can't sign in", chatClient);

        await Expect(panel.GetByRole(AriaRole.Textbox, new() { Name = "Email" })).ToBeVisibleAsync();
        await Expect(panel.GetByText(answer)).ToBeVisibleAsync();
        await WaitUntilKeptOnTheDevice(answer);

        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        panel = await OpenChatPanel();

        await Expect(panel.GetByText(answer)).ToBeVisibleAsync();
        await Expect(panel.GetByRole(AriaRole.Textbox, new() { Name = "Email" })).ToBeVisibleAsync();
    }

    //#if (module == "Sales")
    //#if (database == "PostgreSQL" || database == "SqlServer")
    private static FunctionCallContent ShowProducts(string title, params AppChatbot.ProductToShow[] products)
    {
        return new("show-products", "ShowProducts", new Dictionary<string, object?> { ["title"] = title, ["products"] = products });
    }

    private static FunctionCallContent ShowFollowUpSuggestions(params string[] suggestions)
    {
        return new("show-suggestions", "ShowFollowUpSuggestions", new Dictionary<string, object?> { ["suggestions"] = suggestions });
    }

    /// <summary>What the cards showed, in their order and with their links, is the assistant's own message.</summary>
    private static void AssertShownTheCardsAsTheAssistants(ChatMessage[] conversation)
    {
        Assert.Contains(message => message.Role == ChatRole.Assistant
                                   && message.Text.Contains("1. [EQB SUV](/product/10000)", StringComparison.Ordinal)
                                   && message.Text.Contains("2. [EQE SUV](/product/10003)", StringComparison.Ordinal),
                        conversation,
                        $"The model must be shown what the cards showed, as its own. Conversation: {string.Join(" | ", conversation.Select(message => $"{message.Role}: '{message.Text}'"))}");
    }

    /// <summary>Right after a reload a message can beat the hub connection, so it is retried - without the base's clearing.</summary>
    private async Task SendWithoutClearing(ILocator panel, string message, TestChatClient chatClient)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var conversationsBefore = chatClient.ReceivedConversations.Count;

            await Send(panel, message);

            if (await WaitForServerToReceiveAMessage(chatClient, conversationsBefore, TimeSpan.FromSeconds(15)))
                return;
        }

        throw new AssertFailedException($"'{message}' never reached the server.");
    }
    //#endif
    //#endif

    private static ILocator Approve(ILocator panel) => panel.GetByRole(AriaRole.Button, new() { Name = AppStrings.AiChatApprovalApprove, Exact = true });

    private static ILocator Decline(ILocator panel) => panel.GetByRole(AriaRole.Button, new() { Name = AppStrings.AiChatApprovalDecline, Exact = true });

    /// <summary>An answer in one piece.</summary>
    private static ChatResponseUpdate[] Answer(string text)
    {
        return [new ChatResponseUpdate(ChatRole.Assistant, text)];
    }

    private static ChatResponseUpdate[] ToolCalls(params FunctionCallContent[] calls)
    {
        return [new ChatResponseUpdate(ChatRole.Assistant, (IList<AIContent>)[.. calls]) { FinishReason = ChatFinishReason.ToolCalls }];
    }

    private static string LastQuestion(ChatMessage[] conversation) => conversation.Last(message => message.Role == ChatRole.User).Text;

    /// <summary>This turn's tools have run, so the scripted model answers - like a real one.</summary>
    private static bool ToolsRanSinceTheQuestion(ChatMessage[] conversation)
    {
        return conversation.AsEnumerable()
                           .Reverse()
                           .TakeWhile(message => message.Role != ChatRole.User)
                           .SelectMany(message => message.Contents)
                           .OfType<FunctionResultContent>()
                           .Any();
    }

    /// <summary>What a tool call told the model, once the model has been told.</summary>
    private async Task<string> ToolResult(TestChatClient chatClient, string callId)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = chatClient.ReceivedConversations
                                   .SelectMany(conversation => conversation)
                                   .SelectMany(message => message.Contents)
                                   .OfType<FunctionResultContent>()
                                   .FirstOrDefault(content => content.CallId == callId);

            if (result is not null)
                return result.Result?.ToString() ?? string.Empty;

            await Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.CancellationToken);
        }

        throw new AssertFailedException($"The model was never told what the '{callId}' tool call did.");
    }

    /// <summary>Polls against a deadline, trying again when the page's own navigation interrupts a check.</summary>
    private async Task Eventually(Func<Task<bool>> check, string failure)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                if (await check()) return;
            }
            catch (PlaywrightException)
            {
                // The page is being replaced.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.CancellationToken);
        }

        throw new AssertFailedException(failure);
    }

    /// <summary>How many conversations the panel has stored on this device - one at most (See AppAiChat.razor.History.cs).</summary>
    private Task<int> StoredConversationLength()
    {
        return Page.EvaluateAsync<int>("""
            async () => {
                // Opening one that isn't there would create it, without the stores the app gives it.
                if ((await indexedDB.databases()).every(database => database.name !== 'ai-chat-conversation')) return 0;

                const db = await new Promise((resolve, reject) => {
                    const request = indexedDB.open('ai-chat-conversation');
                    request.onsuccess = () => resolve(request.result);
                    request.onerror = () => reject(request.error);
                });

                try {
                    if (db.objectStoreNames.contains('conversation') === false) return 0;

                    return await new Promise((resolve, reject) => {
                        const request = db.transaction('conversation').objectStore('conversation').count();
                        request.onsuccess = () => resolve(request.result);
                        request.onerror = () => reject(request.error);
                    });
                } finally {
                    db.close();
                }
            }
            """);
    }

    /// <summary>Boots the app with the model - and only the model - replaced, and opens the panel on the public home page.</summary>
    private async Task<ILocator> StartChat(TestChatClient chatClient)
    {
        var server = new AppTestServer(Context);

        // Disposed with the test rather than with a using in each method, so the page outlives the arrangement.
        testCleanup.Add(server);

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

    private readonly List<IAsyncDisposable> testCleanup = [];

    [TestCleanup]
    public async Task DisposeServers()
    {
        foreach (var disposable in testCleanup)
        {
            await disposable.DisposeAsync();
        }
    }
}
