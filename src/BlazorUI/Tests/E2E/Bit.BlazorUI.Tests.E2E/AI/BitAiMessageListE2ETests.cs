using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiMessageList at /e2e/aimessagelist in a real browser: the transcript as a polite log region and a scroll
/// container, its messages as articles in order, the system messages it hides unless asked, regeneration offered on the
/// newest reply only, the scroll script (starting at the newest message, OnPinnedChange reported once per transition,
/// following a stream while pinned and staying put once the reader scrolls up, ScrollToLatestAsync with and without the
/// smooth scroll, reduced motion, AutoScroll off), the newest messages it draws past the threshold (and prerenders) with
/// the older ones drawn a page at a time as the reader scrolls up,
/// the templates (an empty one, a header and a footer, a message template keyed without a wrapper), Disabled, every
/// --bit-AiMessageList-* variable, Styles/Classes, RTL and print.
/// </summary>
[TestClass]
public class BitAiMessageListE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aimessagelist";

    private ILocator List(string id) => Page.Locator($"#{id}");

    private ILocator Articles(string id) => List(id).Locator("article.bit-aicms");

    private ILocator Pinned => Page.Locator("#msl-pinned");

    private ILocator PinButton => Page.Locator("#msl-pin-stl");

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_TheTranscript_IsAPoliteLogOfArticlesInOrder()
    {
        await OpenAsync(Route);

        var list = List("msl-basic");
        await Expect(list).ToHaveClassAsync(HasClass("bit-aimsl"));
        await Expect(list).ToHaveAttributeAsync("role", "log");
        await Expect(list).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(list).ToHaveAttributeAsync("aria-relevant", "additions");
        await Expect(list).ToHaveAttributeAsync("aria-label", "Conversation");

        await Expect(Articles("msl-basic")).ToHaveCountAsync(4);
        Assert.AreEqual("user,assistant,user,assistant",
                        string.Join(",", await Articles("msl-basic").EvaluateAllAsync<string[]>("els => els.map(e => e.dataset.role)")));
        Assert.AreEqual("bas1,bas2,bas3,bas4",
                        string.Join(",", await Articles("msl-basic").EvaluateAllAsync<string[]>("els => els.map(e => e.dataset.messageId)")));

        // Each message is an article of the column, which is laid out as one flex column with a gap between them.
        await Expect(list.Locator(":scope > .bit-aimsl-con > article")).ToHaveCountAsync(4);
        await Expect(list.Locator(".bit-aimsl-con")).ToHaveCSSAsync("flex-direction", "column");
        await Expect(list).ToHaveCSSAsync("overflow-y", "auto");
        if (await Page.SupportsCssAsync("overscroll-behavior", "contain"))
        {
            await Expect(list).ToHaveCSSAsync("overscroll-behavior-y", "contain");
        }
    }

    [TestMethod]
    public async Task Basic_OnlyTheNewestReply_OffersRegeneration()
    {
        await OpenAsync(Route);

        var replies = List("msl-basic").Locator("article[data-role='assistant']");
        await Expect(replies).ToHaveCountAsync(2);
        await Expect(replies.Nth(0).Locator("button[aria-label='Regenerate']")).ToHaveCountAsync(0);
        await Expect(replies.Nth(1).Locator("button[aria-label='Regenerate']")).ToHaveCountAsync(1);

        // Regenerating the newest reply replaces it with a new one, which is then the one offering it.
        await replies.Nth(1).HoverAsync();
        await replies.Nth(1).Locator("button[aria-label='Regenerate']").ClickAsync();
        await Expect(List("msl-basic").Locator("article[data-message-id='bas4']")).ToHaveCountAsync(0);
        await Expect(replies).ToHaveCountAsync(2);
        await Expect(replies.Nth(1).Locator(".bit-aicms-bdy")).ToHaveTextAsync("You said: Message 3: how does this part work?");
        await Expect(replies.Nth(1).Locator("button[aria-label='Regenerate']")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Entrance_OnlyANewMessage_FadesInAndTheRestoredOnesStayPut()
    {
        await OpenAsync(Route);

        // The history the list opened on is drawn without the entrance, and stays so through later renders.
        var articles = Articles("msl-basic");
        await Expect(articles).ToHaveCountAsync(4);
        await NextFramesAsync();
        Assert.AreEqual(0, await List("msl-basic").Locator("article.bit-aicms-ent").CountAsync(), "The restored messages do not play the entrance.");

        // Regenerating the newest reply adds a new one, which fades in on its own.
        var replies = List("msl-basic").Locator("article[data-role='assistant']");
        await replies.Nth(1).HoverAsync();
        await replies.Nth(1).Locator("button[aria-label='Regenerate']").ClickAsync();
        await Expect(replies.Nth(1).Locator(".bit-aicms-bdy")).ToHaveTextAsync("You said: Message 3: how does this part work?");
        await Expect(replies.Nth(1)).ToHaveClassAsync(HasClass("bit-aicms-ent"));
        Assert.AreEqual("bas1,bas2,bas3",
                        string.Join(",", await List("msl-basic").Locator("article:not(.bit-aicms-ent)").EvaluateAllAsync<string[]>("els => els.map(e => e.dataset.messageId)")),
                        "The messages that were there before keep still.");
    }

    [TestMethod]
    public async Task Basic_SystemMessages_AreHiddenUnlessAskedAndThenDrawnAsANote()
    {
        await OpenAsync(Route);

        var list = List("msl-system");
        await Expect(list).ToHaveAttributeAsync("aria-label", "Support transcript");
        await Expect(Page.GetByRole(AriaRole.Log, new() { Name = "Support transcript" })).ToHaveCountAsync(1);
        await Expect(Articles("msl-system")).ToHaveCountAsync(3);

        var note = list.Locator("article[data-role='system']");
        await Expect(note).ToHaveClassAsync(HasClass("bit-aicms-sys"));
        await Expect(note.Locator(".bit-aicms-pil")).ToHaveTextAsync("You are a terse assistant.");

        // The note is a pill in the middle of the column.
        var column = await list.Locator(".bit-aimsl-con").RectAsync();
        var pill = await note.Locator(".bit-aicms-pil").RectAsync();
        Assert.AreEqual((column.Left + column.Right) / 2, (pill.Left + pill.Right) / 2, 1.5);

        // The basic list hides its system messages by default.
        await Expect(List("msl-basic").Locator("article[data-role='system']")).ToHaveCountAsync(0);
    }

    // ---------- pinned ----------

    [TestMethod]
    public async Task Pinned_OnOpen_TheListStartsAtTheNewestMessageAndReportsNothing()
    {
        await OpenAsync(Route);

        await List("msl-pin").WaitForScrolledToBottomAsync();
        var state = await List("msl-pin").ScrollStateAsync();
        Assert.IsTrue(state.Height > state.ClientHeight * 2, "The transcript is taller than its box.");
        await Expect(Pinned).ToHaveTextAsync("True");
        await Expect(PinButton).ToHaveCountAsync(0);

        // A list that opens at its newest message was never away from it: there is no transition to report.
        await NextFramesAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(PinLog).ToHaveTextAsync("", new() { Timeout = 1000 });
    }

    [TestMethod]
    public async Task Pinned_ScrollingAwayAndBack_ReportsEachTransitionOnce()
    {
        await OpenAsync(Route);

        var list = List("msl-pin");
        var log = await SettledLogAsync();

        await Page.WheelOverAsync(list, -300);
        await Expect(Pinned).ToHaveTextAsync("False");
        await Expect(PinLog).ToHaveTextAsync(Log(log, "False"));
        await Expect(PinButton).ToBeVisibleAsync();

        // Scrolling on, further away, is the same transition: nothing more is reported.
        await Page.WheelOverAsync(list, -300);
        await Page.WheelOverAsync(list, -300);
        await AiE2EHelpers.PollAsync(async () => (await list.ScrollStateAsync()).FromBottom > 600, "The view scrolled further up.");
        await NextFramesAsync();
        await Expect(PinLog).ToHaveTextAsync(Log(log, "False"));

        // Back at the bottom, within the threshold, the list is pinned again.
        await Page.WheelToBottomAsync(list);
        await list.WaitForScrolledToBottomAsync();
        await Expect(Pinned).ToHaveTextAsync("True");
        await Expect(PinLog).ToHaveTextAsync(Log(log, "False", "True"));
        await Expect(PinButton).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Pinned_WhilePinned_TheViewFollowsTheStream()
    {
        await OpenAsync(Route);

        var list = List("msl-pin");
        var log = await SettledLogAsync();
        var before = await list.ScrollStateAsync();

        await Page.Locator("#msl-pin-ask").ClickAsync();
        var reply = list.Locator("article[data-role='assistant']").Last;
        await Expect(reply.GetByText("Paragraph 10:", new() { Exact = false })).ToHaveCountAsync(1);

        var during = await list.ScrollStateAsync();
        Assert.IsTrue(during.Top > before.Top + 100, "The view moved down with the reply.");
        Assert.IsTrue(during.FromBottom <= 48, $"The view is at the newest line: {during.FromBottom}px from the bottom.");

        await Expect(reply).ToHaveAttributeAsync("aria-busy", "false", new() { Timeout = 20000 });
        await list.WaitForScrolledToBottomAsync();
        await Expect(Pinned).ToHaveTextAsync("True");
        await Expect(PinLog).ToHaveTextAsync(log);
    }

    [TestMethod]
    public async Task Pinned_ScrolledUp_TheStreamDoesNotMoveTheView()
    {
        await OpenAsync(Route);

        var list = List("msl-pin");
        await SettledLogAsync();
        await Page.WheelOverAsync(list, -500);
        await Expect(Pinned).ToHaveTextAsync("False");
        await NextFramesAsync();
        var before = await list.ScrollStateAsync();

        await Page.Locator("#msl-pin-ask").ClickAsync();
        var reply = list.Locator("article[data-role='assistant']").Last;
        await Expect(reply.GetByText("Paragraph 8:", new() { Exact = false })).ToHaveCountAsync(1);

        var during = await list.ScrollStateAsync();
        Assert.AreEqual(before.Top, during.Top, 1, "The view stays where the reader left it.");
        Assert.IsTrue(during.Height > before.Height + 200, "The reply grew the transcript under it.");
        await Expect(Pinned).ToHaveTextAsync("False");

        await Expect(reply).ToHaveAttributeAsync("aria-busy", "false", new() { Timeout = 20000 });
        Assert.AreEqual(before.Top, (await list.ScrollStateAsync()).Top, 1);
    }

    [TestMethod]
    public async Task ScrollToLatest_Pressed_ReturnsToTheNewestMessageAndFollowsAgain()
    {
        await OpenAsync(Route);

        var list = List("msl-pin");
        var log = await SettledLogAsync();
        await Page.WheelOverAsync(list, -800);
        await Expect(PinButton).ToBeVisibleAsync();

        await Page.Locator("#msl-pin-ask").ClickAsync();
        var reply = list.Locator("article[data-role='assistant']").Last;
        await Expect(reply.GetByText("Paragraph 4:", new() { Exact = false })).ToHaveCountAsync(1);

        await PinButton.Locator("button").ClickAsync();
        await Expect(Pinned).ToHaveTextAsync("True");
        await Expect(PinButton).ToHaveCountAsync(0);
        await list.WaitForScrolledToBottomAsync(48);

        await Expect(reply).ToHaveAttributeAsync("aria-busy", "false", new() { Timeout = 20000 });
        await list.WaitForScrolledToBottomAsync();
        await Expect(PinLog).ToHaveTextAsync(Log(log, "False", "True"));
    }

    [TestMethod]
    public async Task ScrollToLatest_Smooth_AsksForASmoothScrollUnlessReducedMotionIsAsked()
    {
        await OpenAsync(Route);

        var list = List("msl-pin");
        await SettledLogAsync();
        await RecordScrollCallsAsync();

        // The button asks for a smooth scroll (an engine may still jump, as headless Chromium does).
        await list.EvaluateAsync("e => e.scrollTop = 0");
        await Expect(Pinned).ToHaveTextAsync("False");
        await PinButton.Locator("button").ClickAsync();
        await list.WaitForScrolledToBottomAsync();
        await Expect(Pinned).ToHaveTextAsync("True");
        CollectionAssert.Contains(await ScrollCallsAsync(), "smooth");

        // Under reduced motion the motion factor of the theme is 0, so the same call jumps.
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        Assert.AreEqual("0", (await list.EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bit-mot-duration-factor')")).Trim());
        await RecordScrollCallsAsync();
        await list.EvaluateAsync("e => e.scrollTop = 0");
        await Expect(Pinned).ToHaveTextAsync("False");
        await PinButton.Locator("button").ClickAsync();
        await list.WaitForScrolledToBottomAsync();
        var calls = await ScrollCallsAsync();
        Assert.IsTrue(calls.Length > 0 && calls.All(c => c == "auto"), $"Every scroll jumps: {string.Join(", ", calls)}.");
    }

    [TestMethod]
    public async Task ScrollToLatest_NotSmooth_JumpsAndPins()
    {
        await OpenAsync(Route);

        var list = List("msl-pin");
        await SettledLogAsync();
        await RecordScrollCallsAsync();
        await list.EvaluateAsync("e => e.scrollTop = 0");
        await Expect(Pinned).ToHaveTextAsync("False");

        await Page.Locator("#msl-pin-latest-instant").ClickAsync();
        await Expect(Pinned).ToHaveTextAsync("True");
        await list.WaitForScrolledToBottomAsync();
        var calls = await ScrollCallsAsync();
        Assert.IsTrue(calls.Length > 0 && calls.All(c => c == "auto"), $"The scroll jumps: {string.Join(", ", calls)}.");
        await Expect(PinButton).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AutoScroll_Off_TheViewDoesNotFollowTheStream()
    {
        await OpenAsync(Route);

        var list = List("msl-noauto");
        var before = await list.ScrollStateAsync();

        await Page.Locator("#msl-noauto-ask").ClickAsync();
        var reply = list.Locator("article[data-role='assistant']").Last;
        await Expect(reply.GetByText("Paragraph 8:", new() { Exact = false })).ToHaveCountAsync(1);

        var during = await list.ScrollStateAsync();
        Assert.IsTrue(during.Height > during.ClientHeight + 200, "The reply outgrew the box.");
        Assert.AreEqual(before.Top, during.Top, 1, "With AutoScroll off the view stays where it was.");
    }

    // ---------- virtualize ----------

    [TestMethod]
    public async Task Virtualize_PastTheThreshold_DrawsTheNewestMessagesAndTheOlderOnesAsTheReaderScrollsUp()
    {
        await OpenAsync(Route);

        var list = List("msl-virtual");
        await Expect(list.Locator("article[data-message-id='v200']")).ToBeVisibleAsync();
        await list.WaitForScrolledToBottomAsync(5);

        // The newest twenty - the threshold - are drawn, and the list says how many older ones it left out.
        await Expect(Articles("msl-virtual")).ToHaveCountAsync(20);
        await Expect(list).ToHaveAttributeAsync("data-bit-aimsl-old", "180");
        await Expect(list.Locator("article[data-message-id='v180']")).ToHaveCountAsync(0);

        // At the start of what is drawn, the next page comes in above it, and the message in view stays where it was.
        const string offsetOfV181 = "e => e.querySelector(\"article[data-message-id='v181']\").getBoundingClientRect().top - e.getBoundingClientRect().top";
        var before = await list.EvaluateAsync<double>($"e => {{ e.scrollTop = 0; return ({offsetOfV181})(e); }}");
        await Expect(list.Locator("article[data-message-id='v171']")).ToHaveCountAsync(1);
        await NextFramesAsync();
        Assert.AreEqual(before, await list.EvaluateAsync<double>(offsetOfV181), 2, "The message in view stays put as older ones come in above it.");

        // All the way up, every message is drawn - the newest still among them, nothing blank on the way.
        await AiE2EHelpers.PollAsync(async () =>
        {
            await list.EvaluateAsync("e => e.scrollTop = 0");
            return await list.Locator("article[data-message-id='v1']").CountAsync() == 1;
        }, "The first message is drawn once the view reaches the start.");
        Assert.IsFalse(await list.EvaluateAsync<bool>("e => e.hasAttribute('data-bit-aimsl-old')"));
        await Expect(list.Locator("article[data-message-id='v200']")).ToHaveCountAsync(1);

        // Back at the newest message, the pages above it are let go of again.
        await list.EvaluateAsync("e => e.scrollTop = e.scrollHeight");
        await Expect(Articles("msl-virtual")).ToHaveCountAsync(20);
        await list.WaitForScrolledToBottomAsync(5);
        await Expect(list).ToHaveAttributeAsync("data-bit-aimsl-old", "180");
    }

    [TestMethod]
    public async Task Virtualize_BelowTheThreshold_RendersEveryMessage()
    {
        await OpenAsync(Route);

        await Expect(Articles("msl-below")).ToHaveCountAsync(30);
        await List("msl-below").WaitForScrolledToBottomAsync();
    }

    [TestMethod]
    public async Task Prerender_ALongTranscript_DrawsItsNewestMessagesOnly()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        var virtualIds = Regex.Matches(html, @"data-message-id=""v(\d+)""").Select(m => int.Parse(m.Groups[1].Value)).ToList();
        Assert.AreEqual(20, virtualIds.Count, "The prerendered tail is as long as the threshold.");
        Assert.AreEqual(181, virtualIds.Min());
        Assert.AreEqual(200, virtualIds.Max());
        Assert.AreEqual(30, Regex.Matches(html, @"data-message-id=""b\d+""").Count, "Below the threshold every message is prerendered.");
        StringAssert.Contains(html, "role=\"log\"");
    }

    // ---------- templates ----------

    [TestMethod]
    public async Task Templates_TheEmptyOneGivesWayToTheMessagesBetweenTheHeaderAndTheFooter()
    {
        await OpenAsync(Route);

        var list = List("msl-tpl");
        await Expect(list.Locator("#msl-tpl-empty")).ToHaveTextAsync("No messages yet");
        await Expect(list.Locator("#msl-tpl-header")).ToBeVisibleAsync();
        await Expect(list.Locator("#msl-tpl-footer")).ToBeVisibleAsync();

        await Page.Locator("#msl-tpl-add").ClickAsync();

        await Expect(list.Locator("#msl-tpl-empty")).ToHaveCountAsync(0);
        await Expect(Articles("msl-tpl")).ToHaveTextAsync(["Templated message 1"]);

        // The header, the messages and the footer are one column, in that order, inside the scroll area.
        Assert.AreEqual("msl-tpl-header,ARTICLE,msl-tpl-footer",
                        string.Join(",", await list.Locator(".bit-aimsl-con > *").EvaluateAllAsync<string[]>("els => els.map(e => e.id && !e.matches('article') ? e.id : e.tagName)")));
    }

    [TestMethod]
    public async Task MessageTemplate_EachMessage_IsDrawnByTheTemplateWithNoWrapperAndKeptWhenAnotherGoes()
    {
        await OpenAsync(Route);

        var items = List("msl-custom").Locator(".bit-aimsl-con > .msl-custom-item");
        await Expect(items).ToHaveCountAsync(4);
        await Expect(items.First).ToHaveTextAsync("user: Message 1: how does this part work?");
        await Expect(Articles("msl-custom")).ToHaveCountAsync(0);

        // Mark the elements of the third and fourth messages; removing the first one keeps them as they were.
        await items.Nth(2).EvaluateAsync("e => e.__e2eKept = true");
        await items.Nth(3).EvaluateAsync("e => e.__e2eKept = true");
        await Page.Locator("#msl-tpl-remove").ClickAsync();
        await Expect(items).ToHaveCountAsync(3);
        Assert.AreEqual("cus2,cus3,cus4", string.Join(",", await items.EvaluateAllAsync<string[]>("els => els.map(e => e.dataset.id)")));
        Assert.IsTrue(await items.Nth(1).EvaluateAsync<bool>("e => e.__e2eKept === true"), "The element of message 3 was kept.");
        Assert.IsTrue(await items.Nth(2).EvaluateAsync<bool>("e => e.__e2eKept === true"), "The element of message 4 was kept.");
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_TheList_DisablesTheActionsOfItsMessages()
    {
        await OpenAsync(Route);

        var list = List("msl-disabled");
        await Expect(list).ToHaveClassAsync(HasClass("bit-dis"));
        var reply = list.Locator("article[data-role='assistant']");
        await Expect(reply.Locator("button[aria-label='Regenerate']")).ToBeDisabledAsync();
        await Expect(reply.Locator("button[aria-label='Good response']")).ToBeDisabledAsync();
        await Expect(list.Locator("article[data-role='user'] button[aria-label='Edit']")).ToBeDisabledAsync();
    }

    // ---------- style ----------

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheColumnAndTheScrollbar()
    {
        await OpenAsync(Route);

        var column = List("msl-var").Locator(".bit-aimsl-con");
        await Expect(column).ToHaveCSSAsync("max-width", "420px");
        await Expect(column).ToHaveCSSAsync("row-gap", "30px");
        await Expect(column).ToHaveCSSAsync("padding-top", "11px");
        await Expect(column).ToHaveCSSAsync("padding-right", "12px");
        await Expect(column).ToHaveCSSAsync("padding-bottom", "13px");
        await Expect(column).ToHaveCSSAsync("padding-left", "14px");
        if (await Page.SupportsCssAsync("scrollbar-color", "red transparent"))
        {
            StringAssert.StartsWith(await ComputedStyleAsync("#msl-var", "scrollbar-color"), "rgb(200, 10, 20)");
        }

        // The two messages are 30px apart, once any entrance has landed.
        await List("msl-var").EvaluateAsync("e => Promise.all(e.getAnimations({ subtree: true }).map(a => a.finished))");
        var first = await Articles("msl-var").Nth(0).RectAsync();
        var second = await Articles("msl-var").Nth(1).RectAsync();
        Assert.AreEqual(30, second.Top - first.Bottom, 1);
    }

    [TestMethod]
    public async Task CssVariables_TheDefaults_ComeFromTheThemeSpacing()
    {
        await OpenAsync(Route);

        var column = List("msl-basic").Locator(".bit-aimsl-con");
        await Expect(column).ToHaveCSSAsync("max-width", await column.ResolveLengthInAsync("calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * 92)"));
        await Expect(column).ToHaveCSSAsync("row-gap", await column.ResolveLengthInAsync("calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * 4)"));
        if (await Page.SupportsCssAsync("scrollbar-color", "red transparent"))
        {
            StringAssert.StartsWith(await ComputedStyleAsync("#msl-basic", "scrollbar-color"), await Page.ResolveColorAsync("var(--bit-clr-brd-sec)"));
        }
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheRootAndTheColumn()
    {
        await OpenAsync(Route);

        await Expect(List("msl-classes")).ToHaveClassAsync(HasClass("e2e-msl-root"));
        await Expect(List("msl-classes")).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");
        await Expect(List("msl-classes").Locator(".bit-aimsl-con")).ToHaveClassAsync(HasClass("e2e-msl-content"));
        await Expect(List("msl-classes").Locator(".bit-aimsl-con")).ToHaveCSSAsync("max-width", "300px");
    }

    [TestMethod]
    public async Task Rtl_TheList_PutsTheQuestionsAtTheLeft()
    {
        await OpenAsync(Route);

        var list = List("msl-rtl");
        await Expect(list).ToHaveAttributeAsync("dir", "rtl");
        await Expect(list).ToHaveCSSAsync("direction", "rtl");

        var column = await list.Locator(".bit-aimsl-con").RectAsync();
        var bubble = await list.Locator("article[data-role='user'] .bit-aicms-bbl").RectAsync();
        Assert.IsTrue(bubble.Left < column.Left + 40, $"The bubble is at the left: {bubble.Left} vs {column.Left}.");
    }

    [TestMethod]
    public async Task Print_TheList_ShowsTheWholeConversation()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(List("msl-pin")).ToHaveCSSAsync("overflow-y", "visible");
    }

    [TestMethod]
    public async Task Unmount_LeavingThePage_ReleasesTheScrollObservers()
    {
        await OpenAsync(Route);

        await Page.Locator("#msl-pin-ask").ClickAsync();
        await Expect(List("msl-pin").GetByText("Paragraph 1:", new() { Exact = false })).ToHaveCountAsync(1);

        await OpenAsync("/e2e/aiemptystate");
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ---------- helpers ----------

    private ILocator PinLog => Page.Locator("#msl-pin-log");

    // Waits for the pinned list to have settled at its newest message after the page opened, and answers what it has
    // reported by then, which the transitions a test drives are then appended to.
    private async Task<string> SettledLogAsync()
    {
        await List("msl-pin").WaitForScrolledToBottomAsync();
        await Expect(Pinned).ToHaveTextAsync("True");
        await NextFramesAsync();
        await Page.WaitForTimeoutAsync(300);
        await Expect(Pinned).ToHaveTextAsync("True");
        return await PinLog.TextContentAsync() ?? "";
    }

    private static string Log(string settled, params string[] more) => string.Join(",", new[] { settled }.Where(s => s.Length > 0).Concat(more));

    // Records the behavior of every scrollTo the scripts of the page call from now on.
    private Task RecordScrollCallsAsync() => Page.EvaluateAsync(
        """
        () => {
            window.__e2eScrollCalls = [];
            if (window.__e2eScrollSpy) return;
            window.__e2eScrollSpy = true;
            const original = Element.prototype.scrollTo;
            Element.prototype.scrollTo = function (...args) {
                const o = args[0];
                window.__e2eScrollCalls.push(o && typeof o === 'object' ? (o.behavior ?? 'auto') : 'auto');
                return original.apply(this, args);
            };
        }
        """);

    private Task<string[]> ScrollCallsAsync() => Page.EvaluateAsync<string[]>("() => window.__e2eScrollCalls ?? []");
}
