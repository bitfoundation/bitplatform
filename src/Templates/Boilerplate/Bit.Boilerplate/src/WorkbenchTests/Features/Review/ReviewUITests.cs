namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// Leaving comments: picking a spot on a view, on a desktop with Comment mode and on a phone with the Tools panel's button, the
/// composer, replies, resolving, reopening, deleting, the Open and Done lists, the pins and Show the view.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ReviewUITests : ReviewPageTest
{
    private static readonly WorkbenchReviewAnchor headingSpot = new("h2", null, WorkbenchStrings.ShiftsHeading, 0.5, 0.5);

    // A dialog this button opens, in the page's own state, which tells a press that reached the page from one that didn't.
    private const string OpenEditorState = """{"buildings":{"Draft":{"id":"d98de887-43b0-49c3-8b78-fd6161c7337d","name":"","city":"","province":"Groningen","type":"Museum","year":2026,"height":null,"floors":null,"latitude":52.2,"longitude":5.3},"IsDraftNew":true}}""";

    private ILocator ShiftsHeading => View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading });

    private ILocator AddBuilding => View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd, Exact = true });

    private ILocator BuildingEditor => View().GetByText(WorkbenchStrings.BuildingsAddTitle, new() { Exact = true });

    /// <summary>Opens the shifts page without comments, as a press where a pin is opens that pin's thread.</summary>
    private async Task OpenShiftsWithNoPins(string query = "")
    {
        await ClearComments("shifts");
        await OpenWorkbench("shifts", query);
    }

    [TestMethod, WithAnAgent]
    public async Task AComment_Should_BePinnedOnTheSpotItWasLeftOnAndReachTheAgent(WorkbenchSurface surface)
    {
        await OpenShiftsWithNoPins("?theme=dark");

        await PickSpot(ShiftsHeading);

        await Expect(Tools.Locator(".composer")).ToContainTextAsync($"“{WorkbenchStrings.ShiftsHeading}”");

        var text = Unique("Make the heading bolder");

        await Composer.FillAsync(text);
        await Press(ToolsButton(WorkbenchStrings.Send));

        await Expect(ThreadWith(text)).ToBeVisibleAsync();
        await Expect(Composer).ToHaveCountAsync(0);

        var threads = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Messages.FirstOrDefault()?.Text == text));
        var thread = threads.Single(t => t.Messages[0].Text == text);

        Assert.AreEqual("shifts", thread.PageKey);
        Assert.AreEqual("?theme=dark", thread.Query);
        Assert.AreEqual(WorkbenchStrings.ShiftsHeading, thread.Anchor.Text);
        Assert.IsTrue(thread.HasState, "The view's page state was saved with the comment.");

        await CloseTools();

        await Expect(Pin(thread.Id)).ToBeVisibleAsync();
        await Expect(Pin(thread.Id)).ToHaveTextAsync(thread.Number.ToString(CultureInfo.InvariantCulture));
        await ExpectPinOn(Pin(thread.Id), ShiftsHeading);
    }

    [TestMethod, WithAnAgent]
    public async Task PickingASpot_Should_KeepThePagesOwnButtonFromActing(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");

        await PickSpot(AddBuilding);

        await Expect(Tools.Locator(".composer")).ToContainTextAsync($"“{WorkbenchStrings.BuildingsAdd}”");

        await Press(ToolsButton(WorkbenchStrings.Cancel));
        await CloseTools();

        await Expect(BuildingEditor).ToHaveCountAsync(0);
    }

    [TestMethod, WithAnAgent]
    public async Task PickingALink_Should_NotLeaveThePage(WorkbenchSurface surface)
    {
        await OpenShiftsWithNoPins();

        var url = Page.Url;

        await PickSpot(View().Locator("a[href]:visible").Last);

        await Expect(ShiftsHeading).ToBeVisibleAsync();

        Assert.AreEqual(url, Page.Url);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task EnterOnAPageButton_Should_DoNothingWhilePicking(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");
        await StartPicking();

        await AddBuilding.FocusAsync();
        await AddBuilding.PressAsync("Enter");
        await AddBuilding.PressAsync("Space");

        await Button(WorkbenchStrings.ModeBrowse).ClickAsync();

        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("data-wb-mode", WorkbenchModes.Comment);
        await Expect(BuildingEditor).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task CommentMode_Should_OpenTheToolsWhenTheyAreFoldedAway(WorkbenchSurface surface)
    {
        await OpenShiftsWithNoPins();

        await Button(WorkbenchStrings.Tools).ClickAsync();
        await Expect(Button(WorkbenchStrings.Tools)).ToHaveAttributeAsync("aria-expanded", "false");

        await PickSpot(ShiftsHeading);

        await Expect(Button(WorkbenchStrings.Tools)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Composer).ToBeInViewportAsync();
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task UseThePage_Should_LeaveCommentModeAndGiveThePageBack(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");
        await StartPicking();

        await Button(WorkbenchStrings.ModeBrowse).ClickAsync();

        await Expect(Button(WorkbenchStrings.ModeComment)).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("data-wb-mode", WorkbenchModes.Comment);

        await AddBuilding.ClickAsync();

        await Expect(BuildingEditor).ToBeVisibleAsync();
        await Expect(Composer).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.PhoneWithAgent)]
    public async Task PinAComment_Should_CloseTheToolsAndAskForASpot(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");

        await OpenTools();
        await Press(ToolsButton(WorkbenchStrings.PinComment));

        await Expect(Tools).ToHaveAttributeAsync("inert", string.Empty);
        await Expect(PickBanner).ToBeVisibleAsync();
        await Expect(PickBanner).ToContainTextAsync(WorkbenchStrings.TapToComment);
        await Expect(PickBanner.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Cancel, Exact = true })).ToBeVisibleAsync();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-mode", WorkbenchModes.Comment);
    }

    [TestMethod, Surfaces(WorkbenchSurface.PhoneWithAgent)]
    public async Task TheBanner_Should_TakeALineAndLeaveThePageToTap(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");
        await StartPicking();

        var banner = await PickBanner.BoundingBoxAsync();
        var heading = await ShiftsHeading.BoundingBoxAsync();

        Assert.IsNotNull(banner);
        Assert.IsNotNull(heading);
        Assert.IsLessThan(48, banner.Height, "Its text fits on one line.");
        Assert.IsLessThanOrEqualTo(heading.Y, banner.Y + banner.Height, "The page's content under the app bar stays free to tap.");
    }

    [TestMethod, Surfaces(WorkbenchSurface.PhoneWithAgent)]
    public async Task TheBannersCancel_Should_LeavePickingAndGiveThePageBack(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");
        await StartPicking();

        await Press(PickBanner.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Cancel, Exact = true }));

        await Expect(PickBanner).ToHaveCountAsync(0);
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("data-wb-mode", WorkbenchModes.Comment);

        await Press(AddBuilding);

        await Expect(BuildingEditor).ToBeVisibleAsync();
        await Expect(Composer).ToHaveCountAsync(0);
        await Expect(Tools).ToHaveAttributeAsync("inert", string.Empty);
    }

    [TestMethod, Surfaces(WorkbenchSurface.PhoneWithAgent)]
    public async Task ATapOnThePage_Should_OpenTheToolsWithTheDraftAndEndPicking(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");
        await StartPicking();

        await Press(AddBuilding);

        await ExpectToolsOpen();
        await Expect(Composer).ToBeVisibleAsync();
        await Expect(Composer).ToBeFocusedAsync();
        await Expect(PickBanner).ToHaveCountAsync(0);
        await Expect(Tools.GetByText(WorkbenchStrings.DataStateHint)).ToHaveCountAsync(0);

        var text = Unique("Half a thought");

        await Composer.FillAsync(text);
        await CloseTools();

        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("data-wb-mode", WorkbenchModes.Comment);
        await Expect(DraftPin).ToBeVisibleAsync();
        await Expect(BuildingEditor).ToHaveCountAsync(0);

        await Press(AddBuilding);

        await Expect(BuildingEditor).ToBeVisibleAsync();
        await Expect(Tools).ToHaveAttributeAsync("inert", string.Empty);

        await OpenTools();

        await Expect(Composer).ToHaveValueAsync(text);
    }

    [TestMethod, Surfaces(WorkbenchSurface.PhoneWithAgent)]
    public async Task ASwipeWhilePicking_Should_ScrollThePageAndPickNothing(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");
        await StartPicking();

        var heading = View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 2 }).First;
        var before = await heading.BoundingBoxAsync();

        Assert.IsNotNull(before);

        await SwipeUp(fromY: 600, toY: 300);

        await WorkbenchTestReviewer.WaitFor(async () => (await heading.BoundingBoxAsync())!.Y, y => y < before.Y - 50);

        await Expect(PickBanner).ToBeVisibleAsync();
        await Expect(Composer).ToHaveCountAsync(0);
        await Expect(Tools).ToHaveAttributeAsync("inert", string.Empty);
    }

    [TestMethod, Surfaces(WorkbenchSurface.PhoneWithAgent)]
    public async Task TheToolsButton_Should_NotEndPicking(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");
        await StartPicking();

        await OpenTools();

        await Expect(ToolsButton(WorkbenchStrings.PinComment)).ToBeVisibleAsync();

        await CloseTools();

        await Expect(PickBanner).ToBeVisibleAsync();

        await Press(AddBuilding);

        await Expect(Composer).ToBeVisibleAsync();
        await Expect(BuildingEditor).ToHaveCountAsync(0);
    }

    [TestMethod, WithAnAgent]
    public async Task TheComposersCancel_Should_DropTheDraftAndItsPin(WorkbenchSurface surface)
    {
        await OpenShiftsWithNoPins();

        await PickSpot(ShiftsHeading);

        var text = Unique("Not worth saying");

        await Composer.FillAsync(text);
        await Press(ToolsButton(WorkbenchStrings.Cancel));

        await Expect(Composer).ToHaveCountAsync(0);
        await Expect(DraftPin).ToHaveCountAsync(0);
        await Expect(ThreadWith(text)).ToHaveCountAsync(0);

        Assert.DoesNotContain(t => t.Messages[0].Text == text, await Server.Agent.GetThreads(CancellationToken.None));

        await CloseTools();

        await PickSpot(ShiftsHeading);

        await Expect(Composer).ToHaveValueAsync(string.Empty);
    }

    [TestMethod, WithAnAgent]
    public async Task TheSendButton_Should_WaitForATextAndThenSend(WorkbenchSurface surface)
    {
        await OpenShiftsWithNoPins();

        await PickSpot(ShiftsHeading);

        var send = ToolsButton(WorkbenchStrings.Send);

        await Expect(send).ToBeDisabledAsync();

        await Composer.FillAsync("   ");
        await Expect(send).ToBeDisabledAsync();

        var text = Unique("Sent with the button");

        await Composer.FillAsync(text);
        await Expect(send).ToBeEnabledAsync();

        await Press(send);

        await Expect(ThreadWith(text)).ToBeVisibleAsync();
    }

    [TestMethod, WithAnAgent]
    public async Task CtrlEnter_Should_SendTheCommentAndEnterOnlyStartANewLine(WorkbenchSurface surface)
    {
        await OpenShiftsWithNoPins();

        await PickSpot(ShiftsHeading);

        var text = Unique("First line");

        await Composer.FillAsync(text);
        await Composer.PressAsync("Enter");

        await Expect(Composer).ToBeVisibleAsync();
        await Expect(ThreadWith(text)).ToHaveCountAsync(0);

        await Composer.PressAsync("Control+Enter");

        await Expect(ThreadWith(text)).ToBeVisibleAsync();
        await Expect(Composer).ToHaveCountAsync(0);
    }

    [TestMethod, WithAnAgent]
    public async Task YourName_Should_SignTheCommentAndStayAfterAReload(WorkbenchSurface surface)
    {
        var name = $"Nora {Guid.NewGuid().ToString("N")[..6]}";
        var text = Unique("Signed");

        await OpenShiftsWithNoPins();

        await PickSpot(ShiftsHeading);

        var nameField = Tools.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.YourName, Exact = true });

        await Expect(nameField).ToHaveValueAsync(WorkbenchStrings.Reviewer);

        await nameField.FillAsync(name);
        await Composer.FillAsync(text);
        await Press(ToolsButton(WorkbenchStrings.Send));

        await Expect(ThreadWith(text).Locator(".message-author")).ToContainTextAsync(name);

        await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Messages[0].Text == text && t.Messages[0].Author == name));

        await Page.ReloadAsync();
        await WaitForViews();

        await PickSpot(View().Locator(".filters"));

        await Expect(nameField).ToHaveValueAsync(name);
    }

    [TestMethod, WithAnAgent]
    public async Task AnEmptyName_Should_SignAsReviewer(WorkbenchSurface surface)
    {
        var text = Unique("Signed by nobody");

        await OpenShiftsWithNoPins();

        await PickSpot(ShiftsHeading);

        await Tools.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.YourName, Exact = true }).FillAsync("   ");
        await Composer.FillAsync(text);
        await Press(ToolsButton(WorkbenchStrings.Send));

        await Expect(ThreadWith(text).Locator(".message-author")).ToContainTextAsync(WorkbenchStrings.Reviewer);
    }

    [TestMethod, WithAnAgent]
    public async Task AReply_Should_ShowInTheThreadSignedAndReachTheAgent(WorkbenchSurface surface)
    {
        var text = Unique("Is the week right?");
        var id = await SeedComment("shifts", text);

        await OpenWorkbench("shifts");
        await OpenReview();
        await OpenThread(text);

        var send = ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Reply, Exact = true });

        await Expect(send).ToBeDisabledAsync();

        var withTheButton = Unique("With the button");

        await ReplyBox.FillAsync(withTheButton);
        await Press(send);

        await Expect(ThreadWith(text)).ToContainTextAsync(withTheButton);
        await Expect(ReplyBox).ToHaveValueAsync(string.Empty);

        var withTheKeys = Unique("With the keys");

        await ReplyBox.FillAsync(withTheKeys);
        await ReplyBox.PressAsync("Enter");

        await Expect(ThreadWith(text).Locator(".message")).ToHaveCountAsync(2);

        await ReplyBox.PressAsync("Control+Enter");

        await Expect(ThreadWith(text)).ToContainTextAsync(withTheKeys);
        await Expect(ThreadWith(text).Locator(".message")).ToHaveCountAsync(3);
        await Expect(ThreadWith(text).Locator(".message").Nth(2).Locator(".message-author")).ToContainTextAsync(WorkbenchStrings.Reviewer);
        await Expect(ThreadWith(text).Locator(".message.from-agent")).ToHaveCountAsync(0);

        var thread = (await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Single(t => t.Id == id).Messages.Length == 3)).Single(t => t.Id == id);

        Assert.IsFalse(thread.Messages[1].FromAgent);
        Assert.Contains(withTheKeys, thread.Messages[2].Text);
    }

    [TestMethod, EverySurface]
    public async Task AThreadsHead_Should_OpenAndCloseItsWholeConversation(WorkbenchSurface surface)
    {
        var first = Unique("The first message");
        var last = Unique("The last message");
        var id = await SeedComment("shifts", first);

        await Server.Agent.Reply("Claude Code", id, last, CancellationToken.None);

        await OpenWorkbench("shifts");
        await OpenReview();

        var thread = ThreadWith(last);
        var head = thread.Locator("button.thread-head");

        await Expect(thread).ToBeVisibleAsync();
        await Expect(thread).Not.ToContainTextAsync(first);
        await Expect(head).ToHaveAttributeAsync("aria-expanded", "false");

        await Press(head);

        await Expect(head).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(thread).ToContainTextAsync(first);
        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowView })).ToBeVisibleAsync();

        await Press(head);

        await Expect(head).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(thread).Not.ToContainTextAsync(first);
        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowView })).ToHaveCountAsync(0);
    }

    [TestMethod, WithAnAgent]
    public async Task DeletingAComment_Should_AskOnceMoreAndRemoveItEverywhere(WorkbenchSurface surface)
    {
        var text = Unique("Never mind");
        var id = await SeedComment("shifts", text, anchor: headingSpot);

        await OpenWorkbench("shifts");
        await OpenReview();
        await OpenThread(text);

        await CloseTools();
        await Expect(Pin(id)).ToBeVisibleAsync();
        await OpenTools();

        var delete = ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DeleteComment });

        await Press(delete);
        await Expect(delete).ToContainTextAsync(WorkbenchStrings.DeleteCommentConfirm);
        await Expect(ThreadWith(text)).ToBeVisibleAsync();

        await Press(delete);

        await Expect(ThreadWith(text)).ToHaveCountAsync(0);
        await Expect(Pin(id)).ToHaveCountAsync(0);
        await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Id == id) is false);
    }

    [TestMethod, WithAnAgent]
    public async Task OpeningAnotherThread_Should_CancelTheDeleteQuestion(WorkbenchSurface surface)
    {
        var text = Unique("Almost deleted");
        var other = Unique("Another comment");

        await SeedComment("shifts", text);
        await SeedComment("shifts", other);

        await OpenWorkbench("shifts");
        await OpenReview();
        await OpenThread(text);

        var delete = ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DeleteComment });

        await Press(delete);
        await Expect(delete).ToContainTextAsync(WorkbenchStrings.DeleteCommentConfirm);

        await OpenThread(other);
        await OpenThread(text);

        await Expect(delete).Not.ToContainTextAsync(WorkbenchStrings.DeleteCommentConfirm);

        await Press(delete);

        await Expect(ThreadWith(text)).ToBeVisibleAsync();
        await Expect(delete).ToContainTextAsync(WorkbenchStrings.DeleteCommentConfirm);
    }

    [TestMethod, EverySurface]
    public async Task TheLists_Should_ShowOpenAndDoneCommentsApart(WorkbenchSurface surface)
    {
        await ClearComments("settings");

        var open = Unique("Still open");
        var working = Unique("Being worked on");
        var done = Unique("Already done");

        await SeedComment("settings", open);

        var workingId = await SeedComment("settings", working);
        var doneId = await SeedComment("settings", done);

        await Server.Agent.SetStatus("Claude Code", workingId, WorkbenchReviewStatus.Working, CancellationToken.None);
        await Server.Agent.SetStatus("Claude Code", doneId, WorkbenchReviewStatus.Done, CancellationToken.None);

        await OpenWorkbench("settings");
        await OpenReview();

        await Expect(ListButton(WorkbenchStrings.OpenComments)).ToHaveTextAsync(ListName(WorkbenchStrings.OpenComments, 2));
        await Expect(ListButton(WorkbenchStrings.OpenComments)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(ListButton(WorkbenchStrings.DoneComments)).ToHaveTextAsync(ListName(WorkbenchStrings.DoneComments, 1));
        await Expect(ThreadWith(open)).ToBeVisibleAsync();
        await Expect(ThreadWith(working)).ToBeVisibleAsync();
        await Expect(ThreadWith(done)).ToHaveCountAsync(0);

        await Press(ListButton(WorkbenchStrings.DoneComments));

        await Expect(ListButton(WorkbenchStrings.DoneComments)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(ThreadWith(done)).ToBeVisibleAsync();
        await Expect(ThreadWith(open)).ToHaveCountAsync(0);

        await Press(ListButton(WorkbenchStrings.OpenComments));

        await Expect(ThreadWith(open)).ToBeVisibleAsync();
        await Expect(ThreadWith(done)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task EmptyLists_Should_SayWhatToDo(WorkbenchSurface surface)
    {
        await ClearComments("settings");

        await OpenWorkbench("settings");
        await OpenReview();

        // Only a desktop in dev mode has a Comment mode to point at.
        var empty = surface is WorkbenchSurface.DesktopWithAgent ? WorkbenchStrings.NoComments : WorkbenchStrings.NoCommentsYet;

        await Expect(Tools.GetByText(empty, new() { Exact = true })).ToBeVisibleAsync();

        await Press(ListButton(WorkbenchStrings.DoneComments));

        await Expect(Tools.GetByText(WorkbenchStrings.NoDoneComments, new() { Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task AComment_Should_ListOnlyOnItsOwnPage(WorkbenchSurface surface)
    {
        var text = Unique("On the theme page");

        await SeedComment("theme", text);

        await OpenWorkbench("shifts");
        await OpenReview();

        await Expect(Tools.Locator(".threads")).ToBeVisibleAsync();
        await Expect(ThreadWith(text)).ToHaveCountAsync(0);

        await OpenWorkbench("theme");
        await OpenReview();

        await Expect(ThreadWith(text)).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task TheReviewTab_Should_CountTheOpenCommentsLive(WorkbenchSurface surface)
    {
        await ClearComments("settings");

        await OpenWorkbench("settings");

        var count = Page.Locator("#wb-tab-review .tab-count");

        await Expect(count).ToHaveCountAsync(0);

        var id = await SeedComment("settings", Unique("Counted"));

        await Expect(count).ToHaveTextAsync("1");

        await SeedComment("settings", Unique("Counted too"));

        await Expect(count).ToHaveTextAsync("2");

        await Server.Agent.SetStatus("Claude Code", id, WorkbenchReviewStatus.Done, CancellationToken.None);

        await Expect(count).ToHaveTextAsync("1");
    }

    [TestMethod, EverySurface]
    public async Task ShowTheView_Should_PutTheViewInTheCommentsScenarioAndState(WorkbenchSurface surface)
    {
        var text = Unique("About the editor in dark");

        await SeedComment("buildings", text, "?theme=dark", OpenEditorState);

        await OpenWorkbench("buildings");

        await Expect(BuildingEditor).ToHaveCountAsync(0);
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));

        await OpenReview();
        await OpenThread(text);
        await Press(ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowView }));

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?theme=dark$"));
        await WaitForViews();

        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(BuildingEditor).ToBeVisibleAsync();

        if (IsPhone)
        {
            await Expect(Tools).ToHaveAttributeAsync("inert", string.Empty);
        }
    }

    [TestMethod, WithAnAgent]
    public async Task ShowTheView_Should_BringBackThePageAsItWasWhenTheCommentWasLeft(WorkbenchSurface surface)
    {
        var text = Unique("About the editor");

        await ClearComments("buildings");
        await OpenWorkbench("buildings");

        await Press(AddBuilding);
        await Expect(BuildingEditor).ToBeVisibleAsync();

        await LeaveAComment(BuildingEditor, text);

        await CloseTools();

        if (IsPhone is false)
        {
            await Button(WorkbenchStrings.ModeBrowse).ClickAsync();
        }

        await Press(View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Cancel, Exact = true }));
        await Expect(BuildingEditor).ToHaveCountAsync(0);

        await OpenTools();
        await Press(ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowView }));

        await WaitForViews();
        await Expect(BuildingEditor).ToBeVisibleAsync();
    }

    [TestMethod, WithAnAgent]
    public async Task APinWhilePicking_Should_OpenItsThread(WorkbenchSurface surface)
    {
        var text = Unique("Pinned");
        var other = Unique("Pinned as well");
        var id = await SeedComment("shifts", text, anchor: headingSpot);

        await SeedComment("shifts", other, anchor: headingSpot with { X = 0.2 });

        await OpenWorkbench("shifts");
        await StartPicking();

        await Expect(Pin(id)).ToBeVisibleAsync();
        await Expect(Composer).ToHaveCountAsync(0);

        await Press(Pin(id));

        await Expect(ThreadWith(text).Locator("button.thread-head")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Composer).ToHaveCountAsync(0);

        if (IsPhone)
        {
            await ExpectToolsOpen();

            await CloseTools();
        }

        await Expect(Pin(id)).ToHaveClassAsync(new Regex(@"\bselected\b"));
    }

    [TestMethod, EverySurface]
    public async Task ASelectedPin_Should_OpenItsThreadOutsideCommentModeToo(WorkbenchSurface surface)
    {
        var text = Unique("Selected");
        var id = await SeedComment("shifts", text, anchor: headingSpot);

        await OpenWorkbench("shifts");

        await Expect(Pin(id)).ToHaveCountAsync(0);

        await OpenReview();
        await OpenThread(text);
        await Press(ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowView }));
        await WaitForViews();

        await Expect(Pin(id)).ToBeVisibleAsync();

        if (IsPhone)
        {
            await Expect(Tools).ToHaveAttributeAsync("inert", string.Empty);
        }
        else
        {
            await Page.Locator($"#wb-tab-{WorkbenchModes.ViewColumn}").ClickAsync();
        }

        await Press(Pin(id));

        if (IsPhone)
        {
            await ExpectToolsOpen();
        }
        else
        {
            await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");
        }

        await Expect(ThreadWith(text).Locator("button.thread-head")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod, WithAnAgent]
    public async Task TheDraftsPin_Should_KeepTheDraft(WorkbenchSurface surface)
    {
        var text = Unique("Keep me");

        await OpenShiftsWithNoPins();

        await PickSpot(ShiftsHeading);
        await Composer.FillAsync(text);

        await CloseTools();
        await Expect(DraftPin).ToBeVisibleAsync();

        await Press(DraftPin);

        await ExpectToolsOpen();

        await LeaveTheToolsAndComeBack();

        await Expect(Composer).ToHaveValueAsync(text);
    }

    [TestMethod, WithAnAgent]
    public async Task WhatsTyped_Should_StayWhileTheReviewIsOutOfSight(WorkbenchSurface surface)
    {
        var commentText = Unique("A comment half typed");
        var replyText = Unique("A reply half typed");
        var thread = Unique("To be answered");

        await ClearComments("shifts");
        await SeedComment("shifts", thread);

        await OpenWorkbench("shifts");

        await PickSpot(ShiftsHeading);
        await Composer.FillAsync(commentText);

        await LeaveTheToolsAndComeBack();

        await Expect(Composer).ToHaveValueAsync(commentText);
        await Expect(ToolsButton(WorkbenchStrings.Send)).ToBeEnabledAsync();

        await Press(ToolsButton(WorkbenchStrings.Send));
        await Expect(ThreadWith(commentText)).ToBeVisibleAsync();

        await OpenThread(thread);
        await ReplyBox.FillAsync(replyText);

        await LeaveTheToolsAndComeBack();

        await Expect(ReplyBox).ToHaveValueAsync(replyText);

        await OpenThread(commentText);

        await Expect(ReplyBox).ToHaveValueAsync(string.Empty);
    }

    [TestMethod, WithAnAgent]
    public async Task ThePins_Should_ShowEachCommentsStatusAndLeaveWhenDone(WorkbenchSurface surface)
    {
        var working = await SeedComment("shifts", Unique("In progress"), anchor: headingSpot);
        var done = await SeedComment("shifts", Unique("Finished"), anchor: headingSpot with { X = 0.2 });

        await OpenWorkbench("shifts");
        await StartPicking();

        await Expect(Pin(working)).ToBeVisibleAsync();
        await Expect(Pin(done)).ToBeVisibleAsync();

        await Server.Agent.SetStatus("Claude Code", working, WorkbenchReviewStatus.Working, CancellationToken.None);

        await Expect(Pin(working)).ToHaveClassAsync(new Regex(@"\bworking\b"));

        await Server.Agent.SetStatus("Claude Code", done, WorkbenchReviewStatus.Done, CancellationToken.None);

        await Expect(Pin(done)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ADoneCommentsPin_Should_ShowOnlyWhileItsThreadIsOpen(WorkbenchSurface surface)
    {
        var text = Unique("Finished and looked at again");
        var id = await SeedComment("shifts", text, anchor: headingSpot);

        await Server.Agent.SetStatus("Claude Code", id, WorkbenchReviewStatus.Done, CancellationToken.None);

        await OpenWorkbench("shifts");
        await OpenReview();
        await Press(ListButton(WorkbenchStrings.DoneComments));
        await OpenThread(text);
        await CloseTools();

        await Expect(Pin(id)).ToBeVisibleAsync();
        await Expect(Pin(id)).ToHaveClassAsync(new Regex(@"\bdone\b"));
        await Expect(Pin(id)).ToHaveClassAsync(new Regex(@"\bselected\b"));

        await OpenTools();
        await Press(ListButton(WorkbenchStrings.DoneComments));
        await Press(ThreadWith(text).Locator("button.thread-head"));
        await CloseTools();

        await Expect(Pin(id)).ToHaveCountAsync(0);
    }

    [TestMethod, WithAnAgent]
    public async Task APin_Should_StayOnItsElementAsThePageScrolls(WorkbenchSurface surface)
    {
        var text = Unique("Follow the heading");
        var heading = View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 2 }).First;
        var id = await SeedComment("theme", text, anchor: new("h2", null, WorkbenchStrings.ThemeHeading, 0.1, 0.5));

        await OpenWorkbench("theme");
        await StartPicking();

        await Expect(Pin(id)).ToBeVisibleAsync();

        var (_, before) = await PinTip(Pin(id));

        await heading.EvaluateAsync("e => { let scroller = e.parentElement; while (scroller && !(scroller.scrollHeight > scroller.clientHeight && /auto|scroll/.test(getComputedStyle(scroller).overflowY))) scroller = scroller.parentElement; (scroller ?? e.ownerDocument.scrollingElement).scrollTop += 40; }");

        await WorkbenchTestReviewer.WaitFor(() => PinTip(Pin(id)), tip => Math.Abs(tip.Y - before) > 10);

        await WorkbenchTestReviewer.WaitFor(() => IsPinOn(Pin(id), heading), isOn => isOn);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task ACommentInOneOfSeveralViews_Should_BeLeftOnThatView(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");

        await Button(WorkbenchStrings.Compare).ClickAsync();
        await Page.GetByText(WorkbenchStrings.CompareThemes, new() { Exact = true }).ClickAsync();

        await Expect(Page.Locator(".pane")).ToHaveCountAsync(2);
        await WaitForViews();

        var panes = await Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");
        var text = Unique("Only in the dark");

        await StartPickingInEveryView();
        await View(panes[1]).GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading }).ClickAsync();
        await Composer.FillAsync(text);

        await Expect(View(panes[1]).Locator(".wb-pin[data-id='draft']")).ToBeVisibleAsync();
        await Expect(View(panes[0]).Locator(".wb-pin[data-id='draft']")).ToHaveCountAsync(0);

        await Composer.PressAsync("Control+Enter");

        var threads = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Messages[0].Text == text));

        Assert.AreEqual(WorkbenchThemeMode.Dark, threads.Single(t => t.Messages[0].Text == text).Scenario.Theme);
    }

    [TestMethod, EverySurface]
    public async Task AThread_Should_NameItsElementAndItsView(WorkbenchSurface surface)
    {
        var named = Unique("On a named element");
        var unnamed = Unique("On an empty spot");

        await SeedComment("shifts", named, "?theme=dark&culture=fa-IR&data=empty&device=iphone-se", anchor: new("main", "BitButton", "Save", 0.5, 0.5));
        await SeedComment("shifts", unnamed, anchor: new("main", null, null, 0.5, 0.5));

        await OpenWorkbench("shifts");
        await OpenReview();

        await Expect(ThreadWith(named).Locator(".thread-element")).ToHaveTextAsync("BitButton · “Save”");
        await Expect(ThreadWith(named).Locator(".thread-view")).ToHaveTextAsync($"iPhone SE · {WorkbenchStrings.ThemeDark} · fa-IR · {WorkbenchStrings.DataEmpty}");
        await Expect(ThreadWith(unnamed).Locator(".thread-element")).ToHaveTextAsync(WorkbenchStrings.ThisSpot);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithAgent)]
    public async Task TheReviewTab_Should_StartACommentOnTheInspectedComponent(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");

        await Button(WorkbenchStrings.ModeInspect).ClickAsync();
        await ShiftsHeading.ClickAsync();

        await Expect(Page.Locator(".component-panel .parameter").First).ToBeVisibleAsync();

        await OpenReview();

        await Expect(Composer).ToBeVisibleAsync();
        await Expect(Tools.Locator(".composer")).ToContainTextAsync(WorkbenchStrings.ShiftsHeading);

        await Press(ToolsButton(WorkbenchStrings.Cancel));

        await Expect(Composer).ToHaveCountAsync(0);

        // Going back to the tab doesn't start it again.
        await OpenTab(WorkbenchModes.ViewColumn);
        await OpenReview();

        await Expect(Composer).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task TheReviewTab_Should_StartNoCommentOnTheInspectedComponentWithoutAnAgent(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");

        await Button(WorkbenchStrings.ModeInspect).ClickAsync();
        await ShiftsHeading.ClickAsync();

        await Expect(Page.Locator(".component-panel .parameter").First).ToBeVisibleAsync();

        await OpenReview();

        await Expect(AgentLine).ToBeVisibleAsync();
        await Expect(Composer).ToHaveCountAsync(0);
    }

    private async Task StartPickingInEveryView()
    {
        await Button(WorkbenchStrings.ModeComment).ClickAsync();
        await Expect(Button(WorkbenchStrings.ModeComment)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    private static async Task<(float X, float Y)> PinTip(ILocator pin)
    {
        var box = await pin.BoundingBoxAsync();

        Assert.IsNotNull(box);

        return (box.X, box.Y + box.Height);
    }

    /// <summary>The pin's tip, its bottom left corner, sits on the element it was left on.</summary>
    private static async Task ExpectPinOn(ILocator pin, ILocator element)
    {
        Assert.IsTrue(await IsPinOn(pin, element), "The pin isn't on the element it was left on.");
    }

    private static async Task<bool> IsPinOn(ILocator pin, ILocator element)
    {
        var box = await element.BoundingBoxAsync();

        Assert.IsNotNull(box);

        var (tipX, tipY) = await PinTip(pin);

        return tipX >= box.X - 1 && tipX <= box.X + box.Width + 1 && tipY >= box.Y - 1 && tipY <= box.Y + box.Height + 1;
    }
}
