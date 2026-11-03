namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>The AI assistant in a view: the suggested questions and a typed one, each answer with its card, tools and approval, the history, a new chat, the model, the data states, and all of it after a reload.</summary>
[TestClass, TestCategory("UITest")]
public class AssistantUITests : ShowcaseUITest
{
    private const string PageKey = "assistant";

    private ILocator Conversation => View().GetByRole(AriaRole.Log);

    private ILocator Messages => Conversation.GetByRole(AriaRole.Article);

    private ILocator Reply => Messages.Last;

    private ILocator Prompt => View().GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.AssistantPromptPlaceholder });

    private ILocator Send => ViewButton(WorkbenchStrings.AssistantSend);

    private ILocator Title => View().Locator(".chat-title");

    private ILocator ModelPicker => View().GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.AssistantModel });

    private ILocator History => View().GetByRole(AriaRole.Navigation, new() { Name = WorkbenchStrings.AssistantConversations, Exact = true }).Locator("visible=true");

    private ILocator HistoryRow(string title) => History.GetByRole(AriaRole.Button, new() { Name = title, Exact = true });

    private ILocator Allow => ViewButton(WorkbenchStrings.AssistantApprove);

    private ILocator TheWelcome => View().GetByText(WorkbenchStrings.AssistantWelcomeTitle);

    private async Task Ask(string suggestion)
    {
        await Press(ViewButton(suggestion));
    }

    /// <summary>Waits until the reply has finished, which is when its Regenerate button shows.</summary>
    private async Task ExpectTheReplyToFinish()
    {
        await Expect(Reply.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantRegenerate, Exact = true })).ToBeVisibleAsync();
    }

    private async Task OpenHistory()
    {
        await Press(ViewButton(WorkbenchStrings.AssistantConversations));
        await Expect(History).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheWelcome_Should_OfferTheSuggestedQuestions(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(TheWelcome).ToBeVisibleAsync();
        await Expect(View().GetByText(WorkbenchStrings.AssistantWelcomeText)).ToBeVisibleAsync();

        foreach (var suggestion in new[] { WorkbenchStrings.AssistantSalesPrompt, WorkbenchStrings.AssistantStockPrompt, WorkbenchStrings.AssistantPromoPrompt, WorkbenchStrings.AssistantStaffPrompt })
        {
            await Expect(ViewButton(suggestion)).ToBeVisibleAsync();
        }

        await Expect(Title).ToHaveTextAsync(WorkbenchStrings.AssistantNewChat);
        await Expect(Send).ToBeDisabledAsync();
        await Expect(View().GetByText(WorkbenchStrings.AssistantDisclaimer)).ToBeVisibleAsync();

        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task TheSalesQuestion_Should_ReasonCallItsToolAndAnswerWithACard(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Ask(WorkbenchStrings.AssistantSalesPrompt);

        await Expect(Messages.First).ToContainTextAsync(WorkbenchStrings.AssistantSalesPrompt);
        await Expect(Title).ToHaveTextAsync(WorkbenchStrings.AssistantSalesPrompt);

        await ExpectTheReplyToFinish();

        await Expect(Reply.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(WorkbenchStrings.AssistantReasonedFor).Replace(@"\{0}", ".+")) })).ToBeVisibleAsync();
        await Expect(Reply.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(WorkbenchStrings.AssistantToolSales)) })).ToBeVisibleAsync();

        var card = Reply.Locator(".card");

        await Expect(card).ToContainTextAsync(WorkbenchStrings.AssistantSalesCardTitle);
        await Expect(card.Locator(".card-value")).ToHaveTextAsync(PosFormat.Money(34_820m, 0));
        await Expect(card.Locator(".card-change")).ToContainTextAsync(PosFormat.SignedPercent(0.084m));
        await Expect(card.Locator(".bit-cht-svg")).ToBeVisibleAsync();

        await Expect(Reply.GetByRole(AriaRole.List, new() { Name = WorkbenchStrings.AssistantSources })).ToBeVisibleAsync();

        await Expect(TheWelcome).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheStockQuestion_Should_ShowItsCardAndAskBeforeOrdering(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Ask(WorkbenchStrings.AssistantStockPrompt);

        await Expect(Allow).ToBeVisibleAsync();

        var rows = Reply.Locator(".card .stock-row");

        await Expect(rows).ToHaveCountAsync(3);
        await Expect(rows.First).ToContainTextAsync(WorkbenchStrings.AssistantStockOatMilk);
        await Expect(rows.First).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.AssistantUnitsLeft, 4));
        await Expect(Reply).ToContainTextAsync(WorkbenchStrings.AssistantOrderReason);
        await Expect(ViewButton(WorkbenchStrings.AssistantApproveAlways)).ToBeVisibleAsync();
        await Expect(ViewButton(WorkbenchStrings.AssistantReject)).ToBeVisibleAsync();

        await Press(Allow);

        await ExpectTheReplyToFinish();

        await Expect(Reply).ToContainTextAsync("PO-1042");
        await Expect(Allow).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task DecliningTheOrder_Should_SayNothingWasOrdered(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Ask(WorkbenchStrings.AssistantStockPrompt);

        await Press(ViewButton(WorkbenchStrings.AssistantReject));

        await ExpectTheReplyToFinish();

        await Expect(Reply).ToContainTextAsync(WorkbenchStrings.AssistantOrderSkipped);
        await Expect(Reply).Not.ToContainTextAsync("PO-1042");
    }

    [TestMethod, EverySurface]
    public async Task AlwaysAllowing_Should_NotAskAgainInThatChat(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Ask(WorkbenchStrings.AssistantStockPrompt);

        await Press(ViewButton(WorkbenchStrings.AssistantApproveAlways));

        await ExpectTheReplyToFinish();
        await Expect(Reply).ToContainTextAsync("PO-1042");

        await Prompt.FillAsync(WorkbenchStrings.AssistantStockPrompt);
        await Press(Send);

        await Expect(Messages).ToHaveCountAsync(4);
        await ExpectTheReplyToFinish();

        await Expect(Reply).ToContainTextAsync("PO-1042");
        await Expect(Allow).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ThePromoQuestion_Should_WriteADocument(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Ask(WorkbenchStrings.AssistantPromoPrompt);

        await ExpectTheReplyToFinish();

        await Expect(Reply).ToContainTextAsync(WorkbenchStrings.AssistantPromoAnswer);
        await Expect(Reply).ToContainTextAsync(WorkbenchStrings.AssistantPromoTitle);
        await Expect(Reply).ToContainTextAsync(WorkbenchStrings.AssistantPromoDraft);
    }

    [TestMethod, EverySurface]
    public async Task TheStaffQuestion_Should_CallItsToolAndNameTheTopSeller(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Ask(WorkbenchStrings.AssistantStaffPrompt);

        await ExpectTheReplyToFinish();

        await Expect(Reply.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(WorkbenchStrings.AssistantToolTeam)) })).ToBeVisibleAsync();
        await Expect(Reply).ToContainTextAsync("Saleh");
    }

    [TestMethod, EverySurface]
    public async Task ATypedQuestion_Should_GetTheAnswerForAnythingElse(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Prompt.FillAsync("What is the meaning of life?");

        await Expect(Send).ToBeEnabledAsync();

        await Press(Send);

        await Expect(Messages.First).ToContainTextAsync("What is the meaning of life?");
        await ExpectTheReplyToFinish();

        await Expect(Reply).ToContainTextAsync(WorkbenchStrings.AssistantFallbackAnswer);
        await Expect(Prompt).ToHaveValueAsync(string.Empty);
    }

    [TestMethod, EverySurface]
    public async Task ASuggestionTypedByHand_Should_GetItsOwnAnswer(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Prompt.FillAsync($"  {WorkbenchStrings.AssistantSalesPrompt.ToUpperInvariant()} ");
        await Press(Send);

        await ExpectTheReplyToFinish();

        await Expect(Reply.Locator(".card .card-value")).ToHaveTextAsync(PosFormat.Money(34_820m, 0));
    }

    [TestMethod, OnDesktops]
    public async Task TheEnterKey_Should_SendAndShiftEnterShouldNot(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Prompt.FillAsync("First line");
        await Prompt.PressAsync("Shift+Enter");

        await Expect(Messages).ToHaveCountAsync(0);

        await Prompt.PressAsync("Enter");

        await Expect(Messages).Not.ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Stop_Should_EndTheReplyAndSaySo(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=slow");

        await Ask(WorkbenchStrings.AssistantSalesPrompt);

        await Press(ViewButton(WorkbenchStrings.AssistantStop));

        await Expect(Reply).ToContainTextAsync(WorkbenchStrings.AssistantStopped);
        await Expect(ViewButton(WorkbenchStrings.AssistantStop)).ToHaveCountAsync(0);

        await Expect(Prompt).ToBeEditableAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheToolCall_Should_OpenAndShowWhatItReturned(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Ask(WorkbenchStrings.AssistantSalesPrompt);

        await ExpectTheReplyToFinish();

        var tool = Reply.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(WorkbenchStrings.AssistantToolSales)) });

        await Expect(tool).ToHaveAttributeAsync("aria-expanded", "false");

        await Press(tool);

        await Expect(tool).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Reply).ToContainTextAsync(WorkbenchStrings.AssistantResult);
    }

    [TestMethod, EverySurface]
    public async Task Regenerate_Should_WriteTheAnswerAgain(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Ask(WorkbenchStrings.AssistantPromoPrompt);

        await ExpectTheReplyToFinish();

        await Press(Reply.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantRegenerate, Exact = true }));

        await Expect(ViewButton(WorkbenchStrings.AssistantStop)).ToBeVisibleAsync();
        await ExpectTheReplyToFinish();

        await Expect(Reply).ToContainTextAsync(WorkbenchStrings.AssistantPromoAnswer);
    }

    [TestMethod, EverySurface]
    public async Task EditingAQuestion_Should_AnswerTheNewOne(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Prompt.FillAsync("A first thought");
        await Press(Send);
        await ExpectTheReplyToFinish();

        await Press(Messages.First.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantEdit, Exact = true }));

        var editor = Messages.First.GetByRole(AriaRole.Textbox);

        await editor.FillAsync(WorkbenchStrings.AssistantStaffPrompt);
        await Press(Messages.First.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantSave, Exact = true }));

        await Expect(Messages.First).ToContainTextAsync(WorkbenchStrings.AssistantStaffPrompt);
        await ExpectTheReplyToFinish();
        await Expect(Reply).ToContainTextAsync("Saleh");
    }

    [TestMethod, EverySurface]
    public async Task TheFeedback_Should_ShowAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Prompt.FillAsync("Anything");
        await Press(Send);
        await ExpectTheReplyToFinish();

        var helpful = Reply.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantHelpful, Exact = true });
        var notHelpful = Reply.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantNotHelpful, Exact = true });

        await Expect(helpful).ToHaveAttributeAsync("aria-pressed", "false");

        await Press(helpful);

        await Expect(helpful).ToHaveAttributeAsync("aria-pressed", "true");

        await ReloadTheView();

        await Expect(helpful).ToHaveAttributeAsync("aria-pressed", "true");

        await Press(notHelpful);

        await Expect(notHelpful).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(helpful).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [TestMethod, EverySurface]
    public async Task Copy_Should_SayItCopied(WorkbenchSurface surface)
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);

        await OpenWorkbench(PageKey);

        await Prompt.FillAsync("Anything");
        await Press(Send);
        await ExpectTheReplyToFinish();

        await Press(Reply.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantCopy, Exact = true }));

        await Expect(Reply.GetByRole(AriaRole.Status).Filter(new() { HasText = WorkbenchStrings.AssistantCopied })).ToHaveCountAsync(1);

        Assert.Contains(WorkbenchStrings.AssistantFallbackAnswer, await View().Locator("html").EvaluateAsync<string>("() => navigator.clipboard.readText()"));
    }

    [TestMethod, EverySurface]
    public async Task AnAttachedFile_Should_ShowUnderThePromptAndGoAwayWhenRemoved(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await View().Locator("input[type='file']").SetInputFilesAsync(new FilePayload { Name = "stock.txt", MimeType = "text/plain", Buffer = "Oat milk"u8.ToArray() });

        var composer = View().Locator(".chat");
        var remove = composer.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(WorkbenchStrings.AssistantRemove)) });

        await Expect(composer).ToContainTextAsync("stock.txt");
        await Expect(Send).ToBeEnabledAsync();

        await Press(remove);

        await Expect(composer).Not.ToContainTextAsync("stock.txt");
    }

    [TestMethod, EverySurface]
    public async Task AnAttachedFile_Should_GoWithTheMessageAndLeaveTheComposer(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await View().Locator("input[type='file']").SetInputFilesAsync(new FilePayload { Name = "stock.txt", MimeType = "text/plain", Buffer = "Oat milk"u8.ToArray() });

        var composer = View().Locator(".bit-aipri-ats");

        await Expect(composer).ToContainTextAsync("stock.txt");

        await Prompt.FillAsync("Anything");
        await Press(Send);
        await ExpectTheReplyToFinish();

        await Expect(Messages.First).ToContainTextAsync("Anything");
        await Expect(Messages.First).ToContainTextAsync("stock.txt");
        await Expect(composer).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ThePrompt_Should_StayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Prompt.FillAsync("Half a thought");
        await Expect(Send).ToBeEnabledAsync();

        await ReloadTheView();

        await Expect(Prompt).ToHaveValueAsync("Half a thought");
    }

    [TestMethod, EverySurface]
    public async Task AConversation_Should_StayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Ask(WorkbenchStrings.AssistantSalesPrompt);
        await ExpectTheReplyToFinish();

        await ReloadTheView();

        await Expect(Messages).ToHaveCountAsync(2);
        await Expect(Title).ToHaveTextAsync(WorkbenchStrings.AssistantSalesPrompt);
        await Expect(Reply.Locator(".card .card-value")).ToHaveTextAsync(PosFormat.Money(34_820m, 0));
    }

    [TestMethod, EverySurface]
    public async Task TheModel_Should_DecideOnReasoningAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(ModelPicker).ToContainTextAsync(WorkbenchStrings.AssistantModelThorough);

        await Pick(ModelPicker, WorkbenchStrings.AssistantModelQuick);

        await Expect(ModelPicker).ToContainTextAsync(WorkbenchStrings.AssistantModelQuick);

        await Ask(WorkbenchStrings.AssistantStaffPrompt);
        await ExpectTheReplyToFinish();

        await Expect(Reply.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(WorkbenchStrings.AssistantReasonedFor).Replace(@"\{0}", ".+")) })).ToHaveCountAsync(0);

        await ReloadTheView();

        await Expect(ModelPicker).ToContainTextAsync(WorkbenchStrings.AssistantModelQuick);
    }

    [TestMethod, EverySurface]
    public async Task TheHistory_Should_ListTheSampleChatsAndOpenOne(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await OpenHistory();

        foreach (var title in new[] { WorkbenchStrings.AssistantHistoryWeekly, WorkbenchStrings.AssistantHistoryRestock, WorkbenchStrings.AssistantHistoryAutumn, WorkbenchStrings.AssistantHistoryTeam })
        {
            await Expect(HistoryRow(title)).ToBeVisibleAsync();
        }

        await Press(HistoryRow(WorkbenchStrings.AssistantHistoryWeekly));

        await Expect(Title).ToHaveTextAsync(WorkbenchStrings.AssistantHistoryWeekly);
        await Expect(Messages.First).ToContainTextAsync(WorkbenchStrings.AssistantSalesPrompt);
        await Expect(Reply.Locator(".card .card-value")).ToHaveTextAsync(PosFormat.Money(34_820m, 0));
        await Expect(History).ToHaveCountAsync(0);

        await ReloadTheView();

        await Expect(Title).ToHaveTextAsync(WorkbenchStrings.AssistantHistoryWeekly);
        await Expect(Messages).ToHaveCountAsync(2);
    }

    [TestMethod, EverySurface]
    public async Task AQuestionAsked_Should_JoinTheHistoryAndComeBackFromIt(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var question = $"Question {Guid.NewGuid():N}";

        await Prompt.FillAsync(question);
        await Press(Send);
        await ExpectTheReplyToFinish();

        await OpenHistory();

        await Expect(HistoryRow(question)).ToHaveAttributeAsync("aria-current", "true");

        await Press(HistoryRow(WorkbenchStrings.AssistantHistoryRestock));

        await Expect(Title).ToHaveTextAsync(WorkbenchStrings.AssistantHistoryRestock);

        await OpenHistory();
        await Press(HistoryRow(question));

        await Expect(Title).ToHaveTextAsync(question);
        await Expect(Messages.First).ToContainTextAsync(question);
    }

    [TestMethod, EverySurface]
    public async Task NewChat_Should_StartOverAndKeepTheOldOneInTheHistory(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var question = $"Question {Guid.NewGuid():N}";

        await Prompt.FillAsync(question);
        await Press(Send);
        await ExpectTheReplyToFinish();

        await OpenHistory();
        await Press(History.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantNewChat, Exact = true }));

        await Expect(TheWelcome).ToBeVisibleAsync();
        await Expect(Title).ToHaveTextAsync(WorkbenchStrings.AssistantNewChat);
        await Expect(Messages).ToHaveCountAsync(0);

        await ReloadTheView();

        await Expect(TheWelcome).ToBeVisibleAsync();

        await OpenHistory();

        await Expect(HistoryRow(question)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task DeletingAChat_Should_TakeItAwayForGood(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await OpenHistory();
        await Press(HistoryRow(WorkbenchStrings.AssistantHistoryAutumn));
        await Expect(Title).ToHaveTextAsync(WorkbenchStrings.AssistantHistoryAutumn);

        await OpenHistory();
        await Press(History.GetByRole(AriaRole.Button, new() { Name = $"{WorkbenchStrings.AssistantDelete} {WorkbenchStrings.AssistantHistoryAutumn}", Exact = true }));

        await Expect(HistoryRow(WorkbenchStrings.AssistantHistoryAutumn)).ToHaveCountAsync(0);
        await Expect(TheWelcome).ToBeVisibleAsync();
        await Expect(Title).ToHaveTextAsync(WorkbenchStrings.AssistantNewChat);

        await ReloadTheView();

        await Expect(TheWelcome).ToBeVisibleAsync();

        await Expect(HistoryRow(WorkbenchStrings.AssistantHistoryWeekly)).ToBeVisibleAsync();
        await Expect(HistoryRow(WorkbenchStrings.AssistantHistoryAutumn)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheHistoryPanel_Should_StayOpenAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await OpenHistory();

        await ReloadTheView();

        await Expect(History).ToBeVisibleAsync();
        await Expect(HistoryRow(WorkbenchStrings.AssistantHistoryWeekly)).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task AWideView_Should_ShowTheHistoryBesideTheChat(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?device=desktop-1536");

        await Expect(ViewButton(WorkbenchStrings.AssistantConversations)).Not.ToBeVisibleAsync();
        await Expect(HistoryRow(WorkbenchStrings.AssistantHistoryWeekly)).ToBeVisibleAsync();

        await Press(HistoryRow(WorkbenchStrings.AssistantHistoryTeam));

        await Expect(Title).ToHaveTextAsync(WorkbenchStrings.AssistantHistoryTeam);
        await Expect(HistoryRow(WorkbenchStrings.AssistantHistoryWeekly)).ToBeVisibleAsync();

        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task Loading_Should_HoldBackTheChat(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=loading");

        await Expect(Conversation.Locator(".placeholder")).ToBeVisibleAsync();
        await Expect(TheWelcome).ToHaveCountAsync(0);
        await Expect(Prompt).ToBeDisabledAsync();
    }

    [TestMethod, EverySurface]
    public async Task Slow_Should_ShowThePlaceholderFirstAndAnswerSlowly(WorkbenchSurface surface)
    {
        await PlaceholderTimer.Watch(Page, ".placeholder", ".bit-aiems-ttl");

        await OpenWithoutWaiting(PageKey, "?data=slow");

        await Expect(Conversation.Locator(".placeholder")).ToBeVisibleAsync();
        await Expect(TheWelcome).ToBeVisibleAsync();
        await Expect(Conversation.Locator(".placeholder")).ToHaveCountAsync(0);
        await Expect(Prompt).ToBeEnabledAsync();

        Assert.IsGreaterThanOrEqualTo(PlaceholderTimer.SlowLoad, await PlaceholderTimer.Read(Page), "The conversations came as fast as without the slow state.");
    }

    [TestMethod, EverySurface]
    public async Task Empty_Should_HaveNoHistoryButStillAnswer(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=empty");

        await Expect(TheWelcome).ToBeVisibleAsync();

        await OpenHistory();

        await Expect(History.Locator("..").GetByText(WorkbenchStrings.AssistantNoHistory)).ToBeVisibleAsync();

        await Press(History.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantNewChat, Exact = true }));

        await Prompt.FillAsync("Anything");
        await Press(Send);

        await ExpectTheReplyToFinish();

        await Expect(Reply).ToContainTextAsync(WorkbenchStrings.AssistantFallbackAnswer);
    }

    [TestMethod, EverySurface]
    public async Task Error_Should_SaySoAndARetryShouldBringTheChatBack(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=error");

        await Expect(Conversation).ToContainTextAsync(WorkbenchStrings.AssistantHistoryFailed);
        await Expect(TheWelcome).ToHaveCountAsync(0);
        await Expect(Prompt).ToBeDisabledAsync();

        await Press(ViewButton(WorkbenchStrings.AssistantRetry));

        await Expect(TheWelcome).ToBeVisibleAsync();
        await Expect(Prompt).ToBeEnabledAsync();

        await OpenHistory();

        await Expect(HistoryRow(WorkbenchStrings.AssistantHistoryWeekly)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task Mixed_Should_FailTheHistoryFirstAndThenShowAChatWhoseAnswerFailed(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=mixed");

        await Expect(Conversation).ToContainTextAsync(WorkbenchStrings.AssistantHistoryFailed);

        await Press(ViewButton(WorkbenchStrings.AssistantRetry));

        await Expect(TheWelcome).ToBeVisibleAsync();

        await OpenHistory();
        await Press(HistoryRow(WorkbenchStrings.AssistantHistoryWeekly));

        await Expect(Reply).ToContainTextAsync(WorkbenchStrings.AssistantAnswerFailed);

        await Press(Reply.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantRetry, Exact = true }));

        await Expect(Reply.Locator(".card .card-value")).ToHaveTextAsync(PosFormat.Money(34_820m, 0));
    }
}
