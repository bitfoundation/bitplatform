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
/// The cards a tool shows in the conversation, end to end with only the model scripted: the real tool loads and signs the
/// card, the real hub carries it to the real panel, which renders, stores and resends it.
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

        var rows = (await panel.Locator(".message-row").AllInnerTextsAsync()).ToList();

        Assert.IsLessThan(rows.FindIndex(row => row.Contains(answer, StringComparison.Ordinal)),
                          rows.FindIndex(row => row.Contains("EQB SUV", StringComparison.Ordinal)),
                          $"The cards must come before the answer of their turn. Rows: {string.Join(" | ", rows)}");

        await Expect(panel.Locator(".default-prompt-button")).ToHaveTextAsync(["Which one is cheaper?"]);

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

        var approval = panel.Locator(".approval");

        await Expect(approval.GetByRole(AriaRole.Button, new() { Name = "Clear files" })).ToBeVisibleAsync();

        // Closing the panel ends no wait, and the launcher says an answer is still wanted.
        await panel.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();

        await Expect(panel).ToBeHiddenAsync();
        await Expect(Page.Locator(".open-panel-button.needs-answer")).ToBeVisibleAsync();

        panel = await OpenChatPanel();

        await approval.GetByRole(AriaRole.Button, new() { Name = "Not now" }).ClickAsync();

        await Expect(panel.GetByText("You kept the app's files.")).ToBeVisibleAsync();
        await Expect(panel.GetByText(answer)).ToBeVisibleAsync();
        await Expect(Page.Locator(".open-panel-button.needs-answer")).ToHaveCountAsync(0);

        var result = await ToolResult(chatClient, "clear-files");

        Assert.Contains("declined", result, StringComparison.OrdinalIgnoreCase, $"The tool must tell the model the user declined. It said: {result}");

        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        panel = await OpenChatPanel();

        await Expect(panel.GetByText(ClearFilesQuestion)).ToBeVisibleAsync();
        await Expect(panel.GetByText("You kept the app's files.")).ToBeVisibleAsync();
    }

    /// <summary>Sending another message instead of answering ends the wait and clears nothing.</summary>
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

        await Expect(panel.Locator(".approval")).ToBeVisibleAsync();

        await SendFollowUpMessage(panel, nextQuestion, chatClient);

        await Expect(panel.GetByText(nextAnswer)).ToBeVisibleAsync();
        await Expect(panel.Locator(".approval")).ToHaveCountAsync(0);
        await Expect(panel.GetByText("Nothing was cleared: the request went unanswered.")).ToBeVisibleAsync();

        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        panel = await OpenChatPanel();

        await Expect(panel.GetByText(nextAnswer)).ToBeVisibleAsync();
        await Expect(panel.GetByText("Nothing was cleared: the request went unanswered.")).ToBeVisibleAsync();
    }

    /// <summary>Approved on the card, the conversation is deleted from the device and the app restarts.</summary>
    [TestMethod]
    public async Task ClearingTheAppFiles_Should_ClearThem_OnceTheUserApproves()
    {
        var chatClient = new TestChatClient
        {
            StreamingUpdates = (_, conversation) => ToolsRanSinceTheQuestion(conversation)
                ? Answer("Clearing your app's files.")
                : ToolCalls(new FunctionCallContent("clear-files", "ClearAppFiles"))
        };

        var panel = await StartChat(chatClient);
        var appUrl = Page.Url;

        await SendChatMessage(panel, ClearFilesQuestion, chatClient);

        var approval = panel.Locator(".approval");

        await Expect(approval).ToBeVisibleAsync();

        Assert.IsGreaterThan(0, await StoredConversationLength(), "Control: the conversation must be stored before the files are cleared.");

        // Set on the page about to be cleared, so the app restarting shows as the flag going missing.
        await Page.EvaluateAsync("() => window.beforeClearingTheAppFiles = true");

        await approval.GetByRole(AriaRole.Button, new() { Name = "Clear files" }).ClickAsync();

        var result = await ToolResult(chatClient, "clear-files");

        Assert.Contains("approved", result, StringComparison.OrdinalIgnoreCase, $"The tool must tell the model the user approved. It said: {result}");

        await Eventually(() => Page.EvaluateAsync<bool>("""
            async () => window.beforeClearingTheAppFiles !== true
                        || (await indexedDB.databases()).every(database => database.name !== 'ai-chat')
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

        var form = panel.Locator(".card-row");

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
        await Expect(panel.Locator(".card-row").GetByRole(AriaRole.Textbox, new() { Name = "Email" })).ToHaveCountAsync(0);
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

            await panel.Locator("textarea").FillAsync(message);
            await panel.Locator(".send-message-button").ClickAsync();

            if (await WaitForServerToReceiveAMessage(chatClient, conversationsBefore, TimeSpan.FromSeconds(15)))
                return;
        }

        throw new AssertFailedException($"'{message}' never reached the server.");
    }
    //#endif
    //#endif

    /// <summary>What the schema makes a real model write (See AssistantReply), in one piece.</summary>
    private static ChatResponseUpdate[] Answer(string text)
    {
        return [new ChatResponseUpdate(ChatRole.Assistant, JsonSerializer.Serialize(new AssistantReply { Answer = text }, AppJsonContext.Default.AssistantReply))];
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

    /// <summary>How many messages and cards the panel has stored on this device (See AppAiChatPanel.RememberMessage).</summary>
    private Task<int> StoredConversationLength()
    {
        return Page.EvaluateAsync<int>("""
            async () => {
                // Opening one that isn't there would create it, without the stores the app gives it.
                if ((await indexedDB.databases()).every(database => database.name !== 'ai-chat')) return 0;

                const db = await new Promise((resolve, reject) => {
                    const request = indexedDB.open('ai-chat');
                    request.onsuccess = () => resolve(request.result);
                    request.onerror = () => reject(request.error);
                });

                try {
                    if (db.objectStoreNames.contains('messages') === false) return 0;

                    return await new Promise((resolve, reject) => {
                        const request = db.transaction('messages').objectStore('messages').count();
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
