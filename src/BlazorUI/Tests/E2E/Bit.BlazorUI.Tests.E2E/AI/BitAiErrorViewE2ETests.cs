using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiErrorView at /e2e/aierrorview in a real browser: the error announced as an alert under a generic title (or
/// its own message when Compact), the wait before a retry, the retry offered only when it could help and a handler is
/// there (its glyph from the AI font), a session's failed turn recovered by the retry (error-then-recover) and one that
/// fails again (error), the technical detail behind a disclosure that says whether it is shown and points at it, a long
/// detail scrolling (focusable) and its --bit-AiErrorView-* variables, Disabled with the page's activation handlers
/// dropped and the BitParams cascade, Styles and Classes, and RTL.
/// </summary>
[TestClass]
public class BitAiErrorViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aierrorview";

    private ILocator View(string id) => Page.Locator($"#{id}");

    private ILocator Button(string id, string name) => Page.Locator($"#{id}").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [TestMethod]
    public async Task Basic_AnErrorIsAnnouncedAsAnAlert_UnderAGenericTitle_WithTheWaitBeforeARetry()
    {
        await OpenAsync(Route);

        var view = View("erv-basic");
        await Expect(view).ToHaveClassAsync(HasClass("bit-aierv"));
        await Expect(view.Locator("[role=alert]")).ToHaveCountAsync(1);
        await Expect(view.Locator("[role=alert]")).ToContainTextAsync("Something went wrong");
        await Expect(view.Locator(".bit-aierv-txt")).ToHaveTextAsync("The model is rate limited right now.");
        await Expect(view.Locator(".bit-aierv-rta")).ToHaveTextAsync(new Regex(@"^Try again in 3[.,]0s\.$"));

        // No one to retry: the retry is not offered, the detail still is.
        await Expect(Button("erv-basic", "Try again")).ToHaveCountAsync(0);
        await Expect(Button("erv-basic", "Show details")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Retry_IsOfferedOnlyWhenItCouldHelpAndSomeoneCanDoIt_AndDrawsItsGlyphFromTheAiFont()
    {
        await OpenAsync(Route);

        var retry = Button("erv-retry", "Try again");
        await Expect(retry).ToBeVisibleAsync();
        await Expect(retry).ToHaveAttributeAsync("type", "button");
        await retry.Locator(".bit-icon-ai").AssertAiGlyphAsync("Refresh");

        await retry.ClickAsync();
        await Expect(Page.Locator("#erv-retries")).ToHaveTextAsync("1");
        await retry.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#erv-retries")).ToHaveTextAsync("2");

        // An error that says a retry would not help offers none, and with no detail either there is no actions row.
        await Expect(View("erv-plain").Locator("button")).ToHaveCountAsync(0);
        await Expect(View("erv-plain").Locator(".bit-aierv-act")).ToHaveCountAsync(0);
        await Expect(View("erv-plain").Locator(".bit-aierv-txt")).ToHaveTextAsync("The request was refused.");
    }

    [TestMethod]
    public async Task Compact_TheErrorIsTitledByItsOwnMessage()
    {
        await OpenAsync(Route);

        var view = View("erv-compact");
        await Expect(view.Locator("[role=alert]")).ToContainTextAsync("The model is rate limited right now.");
        await Expect(view).Not.ToContainTextAsync("Something went wrong");
        await Expect(view.Locator(".bit-aierv-txt")).ToHaveCountAsync(0);
        await Expect(Button("erv-compact", "Try again")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Detail_TheDisclosureShowsAndHidesTheDetail_SaysWhetherItIsShown_AndPointsAtIt()
    {
        await OpenAsync(Route);

        var toggle = Button("erv-basic", "Show details");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(toggle).Not.ToHaveAttributeAsync("aria-controls", new Regex(".*"));
        await Expect(Page.Locator("#erv-basic-dtl")).ToHaveCountAsync(0);

        await toggle.ClickAsync();

        var detail = Page.Locator("#erv-basic-dtl");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(toggle).ToHaveAttributeAsync("aria-controls", "erv-basic-dtl");
        await Expect(detail).ToBeVisibleAsync();
        await Expect(detail).ToHaveCSSAsync("white-space", "pre-wrap");
        Assert.AreEqual("HTTP 429\nretry-after: 3", (await detail.InnerTextAsync()).Replace("\r", ""));
        await Expect(detail).ToHaveCSSAsync("font-family", await Page.ResolveFontFamilyAsync("var(--bit-tpg-font-family-mono)"));
        await Expect(detail).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));

        // The detail can take the focus, so a keyboard can scroll it.
        await Expect(detail).ToHaveAttributeAsync("tabindex", "0");
        await detail.FocusAsync();
        await Expect(detail).ToBeFocusedAsync();

        // And the disclosure answers the keyboard as well.
        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(detail).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Space");
        await Expect(detail).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Detail_ALongDetailScrollsPastItsHeight_WhichItsVariablesSetWithItsColor()
    {
        await OpenAsync(Route);

        await Button("erv-long", "Show details").ClickAsync();
        var detail = Page.Locator("#erv-long-dtl");
        await Expect(detail).ToBeVisibleAsync();
        await Expect(detail).ToHaveCSSAsync("overflow-y", "auto");
        Assert.IsTrue(await detail.EvaluateAsync<bool>("e => e.scrollHeight > e.clientHeight + 10"), "A long detail scrolls inside its own box.");
        var maxHeight = AiE2EHelpers.ParsePx(await detail.EvaluateAsync<string>("e => getComputedStyle(e).maxHeight"));
        Assert.AreEqual(maxHeight, await detail.EvaluateAsync<double>("e => e.getBoundingClientRect().height"), 1.5);

        await Button("erv-vars", "Show details").ClickAsync();
        var styled = Page.Locator("#erv-vars-dtl");
        await Expect(styled).ToHaveCSSAsync("max-height", "40px");
        await Expect(styled).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        Assert.AreEqual(40, await styled.EvaluateAsync<double>("e => e.getBoundingClientRect().height"), 1.5);
    }

    // ---------- session ----------

    [TestMethod]
    public async Task Session_TheRetryOfAFailedTurn_RecoversIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#ss-ask").ClickAsync();

        var view = View("erv-ss");
        await Expect(view).ToBeVisibleAsync();
        await Expect(view.Locator(".bit-aierv-txt")).ToHaveTextAsync("The model is rate limited right now.");
        await Expect(Page.Locator("#ss-answer")).ToHaveTextAsync("Looking that up");
        await Expect(Page.Locator("#ss-state")).ToHaveTextAsync("Idle");
        await Expect(Page.Locator("#ss-messages")).ToHaveTextAsync("2");

        await Button("erv-ss", "Show details").ClickAsync();
        await Expect(Page.Locator("#erv-ss-dtl")).ToContainTextAsync("HTTP 429");

        await Button("erv-ss", "Try again").ClickAsync();

        // The failed answer is replaced by the retry's, in the same place of the conversation.
        await Expect(Page.Locator("#ss-answer")).ToHaveTextAsync("That worked on the second attempt.");
        await Expect(view).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ss-state")).ToHaveTextAsync("Idle");
        await Expect(Page.Locator("#ss-messages")).ToHaveTextAsync("2");
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Session_ARetryThatFailsAgain_ShowsTheErrorAgain()
    {
        await OpenAsync(Route);

        await Page.Locator("#ss-ask-failing").ClickAsync();
        await Expect(Page.Locator("#ss-failing-attempts")).ToHaveTextAsync("1");
        await Expect(View("erv-ss-failing")).ToBeVisibleAsync();

        await Button("erv-ss-failing", "Try again").ClickAsync();

        await Expect(Page.Locator("#ss-failing-attempts")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#erv-retries")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ss-failing-state")).ToHaveTextAsync("Idle");
        await Expect(View("erv-ss-failing").Locator(".bit-aierv-txt")).ToHaveTextAsync("The model is rate limited right now.");
        await Expect(Button("erv-ss-failing", "Try again")).ToBeEnabledAsync();
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_RetriesAndDisclosesNothing_AndDropsThePagesActivationHandlers()
    {
        await OpenAsync(Route);

        var view = View("erv-dis");
        await Expect(view).ToHaveClassAsync(HasClass("bit-dis"));
        foreach (var name in new[] { "Try again", "Show details" })
        {
            await Expect(Button("erv-dis", name)).ToBeDisabledAsync();
            await Expect(Button("erv-dis", name)).ToHaveAttributeAsync("aria-disabled", "true");
            await Button("erv-dis", name).DispatchEventAsync("click");
        }

        await view.Locator(".bit-aierv-txt").ClickAsync();
        await View("erv-enabled").Locator(".bit-aierv-txt").ClickAsync();

        await Expect(Page.Locator("#erv-root-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#erv-retries")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#erv-dis-dtl")).ToHaveCountAsync(0);

        // The cascade disables a region's views by default; one that says otherwise keeps its own.
        await Expect(Button("erv-dis-params", "Try again")).ToBeDisabledAsync();
        await Expect(Button("erv-dis-override", "Try again")).ToBeEnabledAsync();
        await Button("erv-dis-override", "Try again").ClickAsync();
        await Expect(Page.Locator("#erv-retries")).ToHaveTextAsync("1");
    }

    // ---------- style, rtl ----------

    [TestMethod]
    public async Task Style_StylesAndClasses_ReachTheirParts()
    {
        await OpenAsync(Route);

        var view = View("erv-styles");
        await Expect(view).ToHaveClassAsync(HasClass("e2e-erv-root"));
        await Expect(view.Locator(".bit-aierv-txt")).ToHaveClassAsync(HasClass("e2e-erv-text"));
        await Expect(view.Locator(".bit-aierv-rta")).ToHaveCSSAsync("color", "rgb(0, 0, 150)");

        await Button("erv-styles", "Show details").ClickAsync();
        await Expect(Page.Locator("#erv-styles-dtl")).ToHaveCSSAsync("color", "rgb(0, 100, 0)");
    }

    [TestMethod]
    public async Task Rtl_TheErrorReadsFromTheRight_InAnExplicitAndAnInheritedDirection()
    {
        await OpenAsync(Route);

        var (ltrIconLeft, _, _, _) = await View("erv-ltr").Locator(".bit-msg-ict").RectAsync();
        var (ltrTextLeft, _, _, _) = await View("erv-ltr").Locator(".bit-aierv-txt").RectAsync();
        Assert.IsTrue(ltrIconLeft < ltrTextLeft, "Left to right, the icon leads the text from the left.");

        foreach (var id in new[] { "erv-rtl", "erv-rtl-inherited" })
        {
            var (_, _, iconRight, _) = await View(id).Locator(".bit-msg-ict").RectAsync();
            var (_, _, textRight, _) = await View(id).Locator(".bit-aierv-txt").RectAsync();
            Assert.IsTrue(iconRight > textRight, $"{id}: right to left, the icon leads the text from the right.");
            await Expect(View(id).Locator(".bit-aierv-txt")).ToHaveCSSAsync("direction", "rtl");
        }

        await Expect(View("erv-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(View("erv-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
    }
}
