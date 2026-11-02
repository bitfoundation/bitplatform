namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>What the review's browser tests share: the review's parts on each surface, and a review written the way a saved one reads.</summary>
public abstract class ReviewPageTest : WorkbenchPageTest
{
    protected static readonly WorkbenchReviewAnchor SomeSpot = new("main", "BitButton", "Save", 0.5, 0.5);

    protected static string Unique(string text) => $"{text} {Guid.NewGuid():N}";

    protected ILocator ThreadWith(string text) => Tools.Locator("article.thread").Filter(new() { HasText = text });

    protected ILocator Composer => Tools.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.NewComment, Exact = true });

    protected ILocator ReplyBox => Tools.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.Reply, Exact = true });

    protected ILocator PickBanner => Page.Locator(".pick-banner");

    protected ILocator AgentLine => Tools.Locator(".agent");

    protected ILocator ToolsButton(string name) => Tools.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    protected ILocator Pin(string threadId) => View().Locator($".wb-pin[data-id='{threadId}']");

    protected ILocator DraftPin => Pin("draft");

    /// <summary>The Open or the Done list's button, whatever count it shows.</summary>
    protected ILocator ListButton(string format) => Tools.GetByRole(AriaRole.Button, new() { NameRegex = new Regex($"^{Regex.Escape(format).Replace(@"\{0}", @"\d+")}$") });

    protected static string ListName(string format, int count) => string.Format(CultureInfo.InvariantCulture, format, count);

    /// <summary>Brings the review into sight: a desktop's tab, or a phone's Tools panel.</summary>
    protected Task OpenReview() => OpenTab(WorkbenchModes.ReviewColumn);

    /// <summary>The tools are in sight: a phone's panel has opened, which a desktop has already.</summary>
    protected async Task ExpectToolsOpen()
    {
        if (IsPhone)
        {
            await Expect(Tools).Not.ToHaveAttributeAsync("inert", string.Empty);
        }
    }

    /// <summary>Looks away from a part of the tools and comes back to it: a phone closes its tools, a desktop opens another tab.</summary>
    protected async Task LeaveTheToolsAndComeBack(string column = WorkbenchModes.ReviewColumn)
    {
        if (IsPhone)
        {
            await CloseTools();
            await Expect(Page.Locator(".tools-header")).ToHaveCountAsync(0);
            await OpenTools();
        }
        else
        {
            await OpenTab(WorkbenchModes.ChecksColumn);
            await OpenTab(column);
        }
    }

    /// <summary>Starts picking the spot a comment goes on, each surface's way.</summary>
    protected async Task StartPicking()
    {
        if (IsPhone)
        {
            await OpenTools();
            await Press(ToolsButton(WorkbenchStrings.PinComment));
            await Expect(Tools).ToHaveAttributeAsync("inert", string.Empty);
            await Expect(PickBanner).ToBeVisibleAsync();
        }
        else
        {
            await Button(WorkbenchStrings.ModeComment).ClickAsync();
            await Expect(Button(WorkbenchStrings.ModeComment)).ToHaveAttributeAsync("aria-pressed", "true");
            await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");
        }

        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-mode", WorkbenchModes.Comment);
    }

    /// <summary>Picks a spot in the view and waits for the composer.</summary>
    protected async Task PickSpot(ILocator spot)
    {
        await StartPicking();
        await Press(spot);
        await Expect(Composer).ToBeVisibleAsync();
    }

    /// <summary>Leaves a comment on a spot of the view and returns its id, once the agent's copy of the review has it.</summary>
    protected async Task<string> LeaveAComment(ILocator spot, string text)
    {
        await PickSpot(spot);

        await Composer.FillAsync(text);
        await Press(ToolsButton(WorkbenchStrings.Send));

        await Expect(ThreadWith(text)).ToBeVisibleAsync();

        var threads = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Messages.FirstOrDefault()?.Text == text));

        return threads.Single(t => t.Messages[0].Text == text).Id;
    }

    /// <summary>Opens a comment's thread, unless it's open already.</summary>
    protected async Task OpenThread(string text)
    {
        var head = ThreadWith(text).Locator("button.thread-head");

        if (await head.GetAttributeAsync("aria-expanded") is not "true")
        {
            await Press(head);
        }

        await Expect(head).ToHaveAttributeAsync("aria-expanded", "true");
    }

    /// <summary>A comment in the review's own document, which is how a copy without an agent finds the comments it was given.</summary>
    protected async Task<string> SeedComment(string pageKey, string text, string query = "", string? state = null, string author = "Sara", WorkbenchReviewAnchor? anchor = null)
    {
        var id = Guid.NewGuid().ToString("N")[..16];
        var hasState = await Server.Services.GetRequiredService<WorkbenchReviewAttachments>().SaveState(id, state);

        await Server.ReviewHost.EditAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.AddThread(document.GetMap("threads"), id, pageKey, query, anchor ?? SomeSpot, author, text, hasState));

        return id;
    }

    /// <summary>Clears a page's comments, for a test that needs to know what its lists hold.</summary>
    protected async Task ClearComments(string pageKey)
    {
        await Server.ReviewHost.EditAsync(WorkbenchReviewDocument.Id, document =>
        {
            var threads = document.GetMap("threads");

            foreach (var thread in WorkbenchReviewDocument.ReadThreads(threads).Where(t => t.PageKey == pageKey))
            {
                threads.Delete(thread.Id);
            }
        });
    }

    /// <summary>Puts a page back to having no status.</summary>
    protected async Task ClearPageStatus(string pageKey)
    {
        await Server.ReviewHost.EditAsync(WorkbenchReviewDocument.Id, document => document.GetMap("pages").Delete(pageKey));
    }

    /// <summary>Records the agent as last seen at a time, to tell an agent that went away from one that's listening.</summary>
    protected async Task SetAgentLastSeen(string name, string? threadId, DateTimeOffset at)
    {
        await Server.ReviewHost.EditAsync(WorkbenchReviewDocument.Id, document =>
        {
            var agent = document.GetMap("agent");

            WorkbenchReviewDocument.SetAgent(agent, name, threadId);
            agent.Set("at", at.ToString("O", CultureInfo.InvariantCulture));
        });
    }

    /// <summary>A finger moving up the screen, which Playwright's own touch can't do.</summary>
    protected async Task SwipeUp(int fromY, int toY)
    {
        var devTools = await Context.NewCDPSessionAsync(Page);

        Task Touch(string type, params Dictionary<string, object>[] points) => devTools.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = type, ["touchPoints"] = points });

        await Touch("touchStart", new Dictionary<string, object> { ["x"] = 200, ["y"] = fromY });

        for (var y = fromY - 20; y >= toY; y -= 20)
        {
            await Touch("touchMove", new Dictionary<string, object> { ["x"] = 200, ["y"] = y });
        }

        await Touch("touchEnd");
    }

    /// <summary>Marks the page, so <see cref="ExpectNoReload"/> can tell a change reached it live.</summary>
    protected Task MarkThePage() => Page.EvaluateAsync("() => { window.__sameDocument = true; }");

    protected async Task ExpectNoReload()
    {
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__sameDocument === true"), "The page was reloaded.");
    }
}
