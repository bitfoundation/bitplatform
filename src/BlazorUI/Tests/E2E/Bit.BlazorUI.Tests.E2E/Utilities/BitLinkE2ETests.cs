using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// Drives BitLink (/e2e/link and /e2e/link/current) in a real browser: the anchor it renders with an Href and the
/// button without one, navigation, new tabs (noopener, the opens-in-a-new-tab hint wherever the name comes from, a
/// real popup), downloads, rel, OnClick with its arguments, StopPropagation and PreventDefault, in-page links that
/// scroll and move the focus, Match and AriaCurrent following the URL, the disabled contract (no href, aria-disabled,
/// the page's activation handlers dropped, AllowDisabledFocus), Title, AriaDescription, FocusAsync and AutoFocus,
/// the keyboard, the underline states, icons of every kind, every Color (rest, hover and disabled) and Size, the
/// public --bit-Link-* variables with the parameters winning over them, the BitParams cascade, Style/Class, RTL,
/// the prerender, runtime re-renders and unmount.
/// </summary>
[TestClass]
public class BitLinkE2ETests : E2ETestBase
{
    private const string Path = "/e2e/link";

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheLinksBeforeTheCircuitConnects()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Path}")).TextAsync();

        StringAssert.Contains(html, "id=\"lnk-basic\" class=\"bit-lnk\" href=\"/e2e/link\"");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        // The URL is already matched while prerendering.
        var current = await (await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/link/current")).TextAsync();
        StringAssert.Matches(current, new Regex("id=\"lnk-match-current\"[^>]*aria-current=\"page\""));

        await OpenAsync(Path);
        await Expect(Page.Locator("#lnk-basic")).ToHaveTextAsync("Basic link");
    }

    [TestMethod]
    public async Task Basic_AnHref_RendersAnAnchorInThePrimaryColor_ThatTakesTheTypeOfItsSentence()
    {
        await OpenAsync(Path);

        var link = Page.Locator("#lnk-basic");

        Assert.AreEqual("A", await link.EvaluateAsync<string>("el => el.tagName"));
        await Expect(link).ToHaveAttributeAsync("href", "/e2e/link");
        await Expect(link).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri)"));
        await Expect(link).ToHaveCSSAsync("text-decoration-line", "none");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Basic link", Exact = true })).ToHaveIdAsync("lnk-basic");

        await Expect(Page.Locator("#lnk-sentence")).ToHaveCSSAsync("font-size", "18px");
        await Expect(Page.Locator("#lnk-sentence")).ToHaveCSSAsync("font-weight", "300");
    }

    [TestMethod]
    public async Task Navigation_ClickingAnAnchor_NavigatesToItsHref()
    {
        await OpenAsync(Path);

        await Page.Locator("#lnk-navigate").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/link/current$"));
        await Expect(Page.Locator("#location")).ToHaveTextAsync("/e2e/link/current");
    }

    [TestMethod]
    public async Task Keyboard_EnterOnAnAnchor_FollowsIt()
    {
        await OpenAsync(Path);

        await Page.Locator("#lnk-navigate").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/link/current$"));
    }

    [TestMethod]
    public async Task Disabled_TheAnchorLosesItsHref_AndAClickRunsNoHandler()
    {
        await OpenAsync(Path);

        var link = Page.Locator("#lnk-disabled");

        await Expect(link).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(link).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(link).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(link).ToHaveAttributeAsync("role", "link");
        await Expect(link).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri-dis-text)"));
        await Expect(link).ToHaveCSSAsync("cursor", "default");

        // aria-disabled makes Playwright wait for the link to be enabled, so the clicks are forced through.
        await link.ClickAsync(new() { Force = true });
        await link.DblClickAsync(new() { Force = true });
        await link.DispatchEventAsync("pointerdown");

        // The hover is kept, the activations are dropped, and nothing navigated.
        await link.HoverAsync();
        await Expect(Page.Locator("#splat-hovers")).Not.ToHaveTextAsync("0");
        await Expect(Page.Locator("#splat-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/link$"));

        // Enabled, the same splatted handler runs.
        await Page.Locator("#lnk-enabled-splat").ClickAsync();
        await Expect(Page.Locator("#enabled-splat-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Disabled_HoveringAddsNoUnderline()
    {
        await OpenAsync(Path);

        await Page.Locator("#lnk-disabled").HoverAsync();

        await Expect(Page.Locator("#lnk-disabled")).ToHaveCSSAsync("text-decoration-line", "none");
    }

    [TestMethod]
    public async Task Underline_ByDefault_AppearsUnderThePointerOnly_InTheHoverColor()
    {
        await OpenAsync(Path);

        var link = Page.Locator("#lnk-underline-default");

        await Expect(link).ToHaveCSSAsync("text-decoration-line", "none");

        await link.HoverAsync();

        await Expect(link).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(link).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri-hover)"));
    }

    [TestMethod]
    public async Task Underline_UnderlinedKeepsIt_NoUnderlineRemovesIt_AndNoUnderlineWinsWhenBothAreSet()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-underlined")).ToHaveClassAsync(HasClass("bit-lnk-und"));
        await Expect(Page.Locator("#lnk-underlined")).ToHaveCSSAsync("text-decoration-line", "underline");

        await Page.Locator("#lnk-no-underline").HoverAsync();
        await Expect(Page.Locator("#lnk-no-underline")).ToHaveClassAsync(HasClass("bit-lnk-nun"));
        await Expect(Page.Locator("#lnk-no-underline")).ToHaveCSSAsync("text-decoration-line", "none");

        var both = Page.Locator("#lnk-both-underline");
        await Expect(both).ToHaveClassAsync(HasClass("bit-lnk-nun"));
        await Expect(both).Not.ToHaveClassAsync(HasClass("bit-lnk-und"));
        await both.HoverAsync();
        await Expect(both).ToHaveCSSAsync("text-decoration-line", "none");
    }

    [TestMethod]
    public async Task Icon_IconName_DrawsAHiddenGlyphOfTheIconsPackage_BeforeOrAfterTheText()
    {
        await OpenAsync(Path);

        var start = Page.Locator("#lnk-icon-start > i");
        await Expect(start).ToHaveClassAsync(new Regex(@"bit-lnk-icn bit-lnk-sic bit-icon bit-icon--Link"));
        await Expect(start).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.IsTrue((await start.BoundingBoxAsync())!.Width > 4, "The glyph is drawn by the icons stylesheet.");
        await Expect(Page.Locator("#lnk-icon-start")).ToHaveAccessibleNameAsync("Leading icon");
        Assert.IsTrue(await IconIsBeforeTextAsync("#lnk-icon-start"));

        var end = Page.Locator("#lnk-icon-end > i");
        await Expect(end).ToHaveClassAsync(new Regex(@"bit-lnk-icn bit-lnk-eic bit-icon bit-icon--ChevronRight"));
        Assert.IsFalse(await IconIsBeforeTextAsync("#lnk-icon-end"), "The trailing icon follows the text.");
        Assert.IsTrue(Px(await ComputedStyleAsync("#lnk-icon-end > i", "margin-left")) > 0);
        Assert.IsTrue(Px(await ComputedStyleAsync("#lnk-icon-start > i", "margin-right")) > 0);

        // The glyph is the size of the text beside it.
        Assert.AreEqual(await ComputedStyleAsync("#lnk-icon-start", "font-size"), await ComputedStyleAsync("#lnk-icon-start > i", "font-size"));
    }

    [TestMethod]
    public async Task Icon_ExternalSets_RenderTheirOwnClasses_AndIconWinsOverIconName()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-icon-fa > i")).ToHaveClassAsync(new Regex(@"bit-lnk-sic fa-brands fa-github$"));
        await Expect(Page.Locator("#lnk-icon-fa-end > i")).ToHaveClassAsync(new Regex(@"bit-lnk-eic fa-solid fa-arrow-up-right-from-square$"));
        await Expect(Page.Locator("#lnk-icon-bi > i")).ToHaveClassAsync(new Regex(@"bit-lnk-sic bi bi-github$"));
        await Expect(Page.Locator("#lnk-icon-css > i")).ToHaveClassAsync(new Regex(@"bit-lnk-sic e2e-css-icon e2e-star$"));
        await Expect(Page.Locator("#lnk-icon-both > i")).ToHaveClassAsync(new Regex(@"bi bi-github$"));
        await Expect(Page.Locator("#lnk-icon-both > i")).Not.ToHaveClassAsync(new Regex("bit-icon--Link"));

        // Each set's stylesheet draws its glyph into the element - waited for rather than read once, since the stylesheet
        // is a link of the page's head, which the interactive render of the head may put in again.
        foreach (var id in new[] { "#lnk-icon-fa", "#lnk-icon-bi", "#lnk-icon-css" })
        {
            await Page.WaitForFunctionAsync("sel => document.querySelector(sel).getBoundingClientRect().width > 2", $"{id} > i");
        }
    }

    [TestMethod]
    public async Task NewTab_ABlankTarget_AddsNoopenerAndAHiddenHint_AndOpensAPopupWithoutAnOpener()
    {
        await OpenAsync(Path);

        var link = Page.Locator("#lnk-blank");

        await Expect(link).ToHaveAttributeAsync("target", "_blank");
        await Expect(link).ToHaveAttributeAsync("rel", "noopener");
        await Expect(link).ToHaveAccessibleNameAsync("Opens in a new tab (opens in a new tab)");

        var hint = link.Locator(".bit-lnk-hnt");
        await Expect(hint).ToHaveTextAsync("(opens in a new tab)");
        var box = await hint.BoundingBoxAsync();
        Assert.IsTrue(box!.Width <= 1 && box.Height <= 1, "The hint is for screen readers only.");

        var popup = await Page.RunAndWaitForPopupAsync(() => link.ClickAsync());
        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        StringAssert.EndsWith(popup.Url, "/e2e/link/current");
        Assert.IsTrue(await popup.EvaluateAsync<bool>("() => window.opener === null"), "noopener leaves the new tab no opener.");
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/link$"));
        await popup.CloseAsync();
    }

    [TestMethod]
    public async Task NewTab_TheHint_IsReworded_Dropped_OrJoinsTheNameWhereverItComesFrom()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-blank-hint")).ToHaveAccessibleNameAsync("Translated hint (در زبانه جدید باز می‌شود)");

        await Expect(Page.Locator("#lnk-blank-nohint .bit-lnk-hnt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#lnk-blank-nohint")).ToHaveAccessibleNameAsync("Page says it (opens in a new tab)");
        await Expect(Page.Locator("#lnk-blank-nohint")).ToHaveAttributeAsync("rel", "noopener");

        await Expect(Page.Locator("#lnk-blank-aria")).ToHaveAttributeAsync("aria-label", "Docs (opens in a new tab)");
        await Expect(Page.Locator("#lnk-blank-aria .bit-lnk-hnt")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#lnk-blank-labelledby")).ToHaveAttributeAsync("aria-labelledby", "external-link-name lnk-blank-labelledby-nth");
        await Expect(Page.Locator("#lnk-blank-labelledby")).ToHaveAccessibleNameAsync("External name (opens in a new tab)");

        await Expect(Page.Locator("#lnk-self")).ToHaveAttributeAsync("target", "_self");
        await Expect(Page.Locator("#lnk-self")).Not.ToHaveAttributeAsync("rel", new Regex(".*"));
        await Expect(Page.Locator("#lnk-self .bit-lnk-hnt")).ToHaveCountAsync(0);

        // A disabled link is not followed, so it opens nothing and says nothing about a tab.
        await Expect(Page.Locator("#lnk-blank-disabled")).Not.ToHaveAttributeAsync("target", new Regex(".*"));
        await Expect(Page.Locator("#lnk-blank-disabled .bit-lnk-hnt")).ToHaveCountAsync(0);

        // A target and a rel written on as plain attributes are honoured the same way.
        await Expect(Page.Locator("#lnk-splat-target")).ToHaveAttributeAsync("target", "_blank");
        await Expect(Page.Locator("#lnk-splat-target")).ToHaveAttributeAsync("rel", "external noopener");
        await Expect(Page.Locator("#lnk-splat-target .bit-lnk-hnt")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Download_SavesTheFile_UnderItsOwnNameOrTheOneGiven()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-download")).ToHaveAttributeAsync("download", "");
        var download = await Page.RunAndWaitForDownloadAsync(() => Page.Locator("#lnk-download").ClickAsync());
        Assert.AreEqual("link-download.svg", download.SuggestedFilename);

        await Expect(Page.Locator("#lnk-download-named")).ToHaveAttributeAsync("download", "bit-platform-logo.svg");
        var named = await Page.RunAndWaitForDownloadAsync(() => Page.Locator("#lnk-download-named").ClickAsync());
        Assert.AreEqual("bit-platform-logo.svg", named.SuggestedFilename);

        await Expect(Page.Locator("#lnk-download-disabled")).Not.ToHaveAttributeAsync("download", new Regex(".*"));
    }

    [TestMethod]
    public async Task Rel_CombinesItsFlags_StaysOnADisabledLink_AndIsDroppedForAnInPageOne()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-rel")).ToHaveAttributeAsync("rel", "nofollow noreferrer");
        await Expect(Page.Locator("#lnk-rel-sponsored")).ToHaveAttributeAsync("rel", "privacy-policy sponsored ugc");
        await Expect(Page.Locator("#lnk-rel-opener")).ToHaveAttributeAsync("rel", "opener");
        await Expect(Page.Locator("#lnk-rel-blank")).ToHaveAttributeAsync("rel", "author noopener");
        await Expect(Page.Locator("#lnk-rel-disabled")).ToHaveAttributeAsync("rel", "sponsored");
        await Expect(Page.Locator("#lnk-rel-hash")).Not.ToHaveAttributeAsync("rel", new Regex(".*"));
    }

    [TestMethod]
    public async Task Rel_Opener_KeepsTheOpenerOfTheNewTab()
    {
        await OpenAsync(Path);

        var popup = await Page.RunAndWaitForPopupAsync(() => Page.Locator("#lnk-rel-opener").ClickAsync());
        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Assert.IsTrue(await popup.EvaluateAsync<bool>("() => window.opener !== null"));
        await popup.CloseAsync();
    }

    [TestMethod]
    public async Task OnClick_WithoutAnHref_RendersAButton_AndFiresWithTheClicksArguments()
    {
        await OpenAsync(Path);

        var link = Page.Locator("#lnk-button");

        Assert.AreEqual("BUTTON", await link.EvaluateAsync<string>("el => el.tagName"));
        await Expect(link).ToHaveAttributeAsync("type", "button");
        await Expect(link).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri)"));
        await Expect(link).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        await link.ClickAsync();
        await Expect(Page.Locator("#button-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#button-args")).ToHaveTextAsync("detail=1;button=0;type=click");

        // The keyboard activates it as a button: Enter and Space.
        await link.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#button-clicks")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#button-args")).ToHaveTextAsync("detail=0;button=0;type=click");

        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#button-clicks")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task OnClick_OnAnAnchor_RunsAlongsideTheNavigation()
    {
        await OpenAsync(Path);

        var popup = await Page.RunAndWaitForPopupAsync(() => Page.Locator("#lnk-anchor-click").ClickAsync());

        await Expect(Page.Locator("#anchor-clicks")).ToHaveTextAsync("1");
        await popup.CloseAsync();
    }

    [TestMethod]
    public async Task StopPropagation_KeepsTheClickFromAClickableContainer()
    {
        await OpenAsync(Path);

        await Page.Locator("#lnk-stop").ClickAsync();
        await Expect(Page.Locator("#inner-clicks")).ToHaveTextAsync("1");

        await Page.Locator("#lnk-bubble").ClickAsync();
        await Expect(Page.Locator("#bubble-clicks")).ToHaveTextAsync("1");

        // Only the link that does not stop it reached the container.
        await Expect(Page.Locator("#container-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task PreventDefault_CancelsTheNavigation_ButKeepsTheHref()
    {
        await OpenAsync(Path);

        var link = Page.Locator("#lnk-prevent");

        await Expect(link).ToHaveAttributeAsync("href", "/e2e/link/current");
        await link.ClickAsync();

        await Expect(Page.Locator("#guard-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#location")).ToHaveTextAsync("/e2e/link");
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/link$"));
    }

    [TestMethod]
    public async Task InPage_AHashHref_ScrollsTheTargetIntoView_AndMovesTheFocusThere_WithoutTouchingTheUrl()
    {
        await OpenAsync(Path);

        var target = Page.Locator("#article-end");
        await Expect(target).Not.ToBeInViewportAsync();

        await Page.Locator("#lnk-hash").ClickAsync();

        await Expect(target).ToBeInViewportAsync();
        await Expect(target).ToBeFocusedAsync();
        await Expect(target).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Page.Locator("#hash-clicks")).ToHaveTextAsync("1");
        Assert.IsFalse(Page.Url.Contains('#'), $"The address bar keeps no fragment ({Page.Url}).");

        // The next Tab carries on from there.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#article-button")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task InPage_AnEncodedFragment_FindsItsTarget_AndAFocusableTargetIsLeftAsItIs()
    {
        await OpenAsync(Path);

        await Page.Locator("#lnk-hash-encoded").ClickAsync();
        await Expect(Page.Locator("[id='section 1']")).ToBeFocusedAsync();

        await Page.Locator("#lnk-hash-button").ClickAsync();
        await Expect(Page.Locator("#article-button")).ToBeFocusedAsync();
        await Expect(Page.Locator("#article-button")).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));

        await Page.Locator("#lnk-hash-empty").ClickAsync();
        Assert.IsFalse(Page.Url.Contains('#'));
    }

    [TestMethod]
    public async Task Match_FollowsTheUrl_ExactPrefixRegexAndWildcard()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-match-prefix")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.Locator("#lnk-match-exact")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.Locator("#lnk-match-exact")).ToHaveClassAsync(HasClass("bit-lnk-cur"));
        await Expect(Page.Locator("#lnk-match-current")).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));
        await Expect(Page.Locator("#lnk-match-other")).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));
        await Expect(Page.Locator("#lnk-match-prefix-partial")).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));
        await Expect(Page.Locator("#lnk-match-location")).ToHaveAttributeAsync("aria-current", "location");

        // The current link keeps its underline at rest, so it is not told apart by color alone.
        await Expect(Page.Locator("#lnk-match-exact")).ToHaveCSSAsync("text-decoration-line", "underline");

        await Page.Locator("#btn-go-current").ClickAsync();
        await Expect(Page.Locator("#location")).ToHaveTextAsync("/e2e/link/current");

        await Expect(Page.Locator("#lnk-match-current")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.Locator("#lnk-match-regex")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.Locator("#lnk-match-wildcard")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.Locator("#lnk-match-exact")).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));
        await Expect(Page.Locator("#lnk-match-exact")).Not.ToHaveClassAsync(HasClass("bit-lnk-cur"));
        await Expect(Page.Locator("#lnk-match-prefix")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.Locator("#lnk-match-location")).ToHaveAttributeAsync("aria-current", "location");

        await Page.Locator("#btn-go-link").ClickAsync();
        await Expect(Page.Locator("#lnk-match-exact")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.Locator("#lnk-match-current")).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));
    }

    [TestMethod]
    public async Task AriaCurrent_SetByHand_MarksTheStep_AndEveryKindIsWritten()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-step-0")).ToHaveAttributeAsync("aria-current", "step");

        await Page.Locator("#lnk-step-2").ClickAsync();
        await Expect(Page.Locator("#lnk-step-2")).ToHaveAttributeAsync("aria-current", "step");
        await Expect(Page.Locator("#lnk-step-2")).ToHaveClassAsync(HasClass("bit-lnk-cur"));
        await Expect(Page.Locator("#lnk-step-0")).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));

        await Expect(Page.Locator("#lnk-current-page")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.Locator("#lnk-current-date")).ToHaveAttributeAsync("aria-current", "date");
        await Expect(Page.Locator("#lnk-current-time")).ToHaveAttributeAsync("aria-current", "time");
        await Expect(Page.Locator("#lnk-current-true")).ToHaveAttributeAsync("aria-current", "true");
        await Expect(Page.Locator("#lnk-current-splat")).ToHaveAttributeAsync("aria-current", "location");
        await Expect(Page.Locator("#lnk-current-splat")).ToHaveClassAsync(HasClass("bit-lnk-cur"));

        await Expect(Page.Locator("#lnk-current-nounderline")).ToHaveCSSAsync("text-decoration-line", "none");
    }

    [TestMethod]
    public async Task Accessibility_TitleAndAriaDescription()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-title")).ToHaveAttributeAsync("title", "github.com/bitfoundation/bitplatform");
        await Expect(Page.Locator("#lnk-title")).ToHaveAccessibleNameAsync("Hover for the full address");

        var described = Page.Locator("#lnk-description");
        await Expect(described).ToHaveAttributeAsync("aria-describedby", "lnk-description-dsc");
        await Expect(described).ToHaveAccessibleDescriptionAsync("SVG, 12 kilobytes");
        await Expect(described).ToHaveAccessibleNameAsync("Download the brand guidelines");
        var box = await Page.Locator("#lnk-description-dsc").BoundingBoxAsync();
        Assert.IsTrue(box!.Width <= 1 && box.Height <= 1, "The description is for screen readers only.");

        await Expect(Page.Locator("#lnk-description-splat")).ToHaveAttributeAsync("aria-describedby", "external-description lnk-description-splat-dsc");
        await Expect(Page.Locator("#lnk-description-splat")).ToHaveAccessibleDescriptionAsync("Outside More");
    }

    [TestMethod]
    public async Task Disabled_ALinkLeavesTheTabOrder_UnlessAllowDisabledFocusKeepsIt()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-a11y-disabled-focus")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Page.Locator("#lnk-a11y-disabled-focus")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Page.Locator("#lnk-button-disabled")).ToHaveAttributeAsync("disabled", "");
        await Expect(Page.Locator("#lnk-button-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Page.Locator("#lnk-button-disabled-focus")).Not.ToHaveAttributeAsync("disabled", new Regex(".*"));
        await Expect(Page.Locator("#lnk-button-disabled-focus")).ToHaveAttributeAsync("aria-disabled", "true");

        await Page.Locator("#before-disabled").FocusAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#lnk-a11y-disabled-focus")).ToBeFocusedAsync();

        // Still focusable, still not activated.
        await Page.Keyboard.PressAsync("Enter");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#lnk-button-disabled-focus")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await Page.Locator("#lnk-button-disabled-focus").ClickAsync(new() { Force = true });

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#lnk-focus-trigger")).ToBeFocusedAsync();

        await Expect(Page.Locator("#disabled-focus-clicks")).ToHaveTextAsync("0");
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/link$"));

        // A focused disabled link draws its ring in the disabled color.
        await Page.Locator("#before-disabled").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        StringAssert.Contains(await ComputedStyleAsync("#lnk-a11y-disabled-focus", "box-shadow"), await ResolveAsync("color", "var(--bit-clr-pri-dis-text)"));
    }

    [TestMethod]
    public async Task FocusAsync_MovesTheFocusOntoTheLink_AndAutoFocusFocusesOnTheFirstRender()
    {
        await OpenAsync(Path);

        await Page.Locator("#lnk-focus-trigger").ClickAsync();
        await Expect(Page.Locator("#lnk-focus-target")).ToBeFocusedAsync();

        await Page.Locator("#btn-show-autofocus").ClickAsync();
        await Expect(Page.Locator("#lnk-autofocus")).ToBeFocusedAsync();
        await Expect(Page.Locator("#lnk-autofocus-disabled")).Not.ToHaveAttributeAsync("autofocus", new Regex(".*"));

        await Expect(Page.Locator("#lnk-tabindex")).ToHaveAttributeAsync("tabindex", "-1");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAContainer_RestyleEveryState()
    {
        await OpenAsync(Path);

        var link = Page.Locator("#lnk-css");

        await Expect(link).ToHaveCSSAsync("color", "rgb(10, 20, 30)");
        StringAssert.Contains(await ComputedStyleAsync("#lnk-css", "font-family"), "monospace");
        await Expect(link).ToHaveCSSAsync("font-size", "19px");
        await Expect(link).ToHaveCSSAsync("font-weight", "600");
        await Expect(link).ToHaveCSSAsync("text-decoration-color", "rgb(200, 0, 0)");
        await Expect(link).ToHaveCSSAsync("text-decoration-thickness", "3px");
        await Expect(link).ToHaveCSSAsync("text-underline-offset", "5px");
        await Expect(link).ToHaveCSSAsync("border-radius", "7px");
        await Expect(link.Locator("> i")).ToHaveCSSAsync("margin-left", "9px");
        await Expect(Page.Locator("#lnk-css-start-icon > i")).ToHaveCSSAsync("margin-right", "9px");

        await link.HoverAsync();
        await Expect(link).ToHaveCSSAsync("color", "rgb(40, 50, 60)");

        // Pressed.
        var button = Page.Locator("#lnk-css-button");
        await button.HoverAsync();
        await Page.Mouse.DownAsync();
        await Expect(button).ToHaveCSSAsync("color", "rgb(70, 80, 90)");
        await Page.Mouse.UpAsync();

        // Keyboard focus: the hover color inside a ring in the focus color.
        await Page.Mouse.MoveAsync(0, 0);
        await Page.Locator("#before-css").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(link).ToBeFocusedAsync();
        await Expect(link).ToHaveCSSAsync("color", "rgb(40, 50, 60)");
        StringAssert.Contains(await ComputedStyleAsync("#lnk-css", "box-shadow"), "rgb(0, 150, 0)");
    }

    [TestMethod]
    public async Task CssVariables_ColorSizeNoColorAndDisabled_WinOverTheVariables()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-css-params")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-err-fg)"));
        await Expect(Page.Locator("#lnk-css-params")).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", "var(--bit-tpg-fs-xs)"));

        await Expect(Page.Locator("#lnk-css-nocolor")).ToHaveCSSAsync("color", await ComputedStyleAsync("#css-container", "color"));

        await Expect(Page.Locator("#lnk-css-disabled")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#lnk-css-disabled-default")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri-dis-text)"));
    }

    [TestMethod]
    public async Task CssVariables_TheCurrentLink_TakesTheCurrentColorAndWeight()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-css-current")).ToHaveCSSAsync("color", "rgb(5, 6, 7)");
        await Expect(Page.Locator("#lnk-css-current")).ToHaveCSSAsync("font-weight", "800");
        await Expect(Page.Locator("#lnk-css-not-current")).ToHaveCSSAsync("color", "rgb(100, 100, 100)");
    }

    [TestMethod]
    public async Task BitParams_CascadeTheirDefaults_AndALinksOwnValueWins()
    {
        await OpenAsync(Path);

        foreach (var id in new[] { "#lnk-params-1", "#lnk-params-2" })
        {
            await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass("bit-lnk-und"));
            await Expect(Page.Locator(id)).ToHaveCSSAsync("text-decoration-line", "underline");
            await Expect(Page.Locator(id)).ToHaveAttributeAsync("target", "_blank");
            await Expect(Page.Locator(id)).ToHaveAttributeAsync("rel", "noopener");
            await Expect(Page.Locator($"{id} .bit-lnk-hnt")).ToHaveTextAsync("(nueva pestaña)");
        }

        await Expect(Page.Locator("#lnk-params-3")).Not.ToHaveClassAsync(HasClass("bit-lnk-und"));
        await Expect(Page.Locator("#lnk-params-3")).ToHaveCSSAsync("text-decoration-line", "none");
        await Expect(Page.Locator("#lnk-params-3")).ToHaveAttributeAsync("target", "_blank");
    }

    [TestMethod]
    [DataRow("Primary", "pri", "var(--bit-clr-pri)", "var(--bit-clr-pri-hover)", "var(--bit-clr-pri-dis-text)")]
    [DataRow("Secondary", "sec", "var(--bit-clr-sec-fg)", "color-mix(in srgb, var(--bit-clr-sec-fg) 80%, var(--bit-clr-fg-pri))", "var(--bit-clr-sec-dis-text)")]
    [DataRow("Tertiary", "ter", "var(--bit-clr-ter-fg)", "color-mix(in srgb, var(--bit-clr-ter-fg) 80%, var(--bit-clr-fg-pri))", "var(--bit-clr-ter-dis-text)")]
    [DataRow("Info", "inf", "var(--bit-clr-inf-fg)", "color-mix(in srgb, var(--bit-clr-inf-fg) 80%, var(--bit-clr-fg-pri))", "var(--bit-clr-inf-dis-text)")]
    [DataRow("Success", "suc", "var(--bit-clr-suc-fg)", "color-mix(in srgb, var(--bit-clr-suc-fg) 80%, var(--bit-clr-fg-pri))", "var(--bit-clr-suc-dis-text)")]
    [DataRow("Warning", "wrn", "var(--bit-clr-wrn-fg)", "color-mix(in srgb, var(--bit-clr-wrn-fg) 80%, var(--bit-clr-fg-pri))", "var(--bit-clr-wrn-dis-text)")]
    [DataRow("SevereWarning", "swr", "var(--bit-clr-swr-fg)", "color-mix(in srgb, var(--bit-clr-swr-fg) 80%, var(--bit-clr-fg-pri))", "var(--bit-clr-swr-dis-text)")]
    [DataRow("Error", "err", "var(--bit-clr-err-fg)", "color-mix(in srgb, var(--bit-clr-err-fg) 80%, var(--bit-clr-fg-pri))", "var(--bit-clr-err-dis-text)")]
    [DataRow("PrimaryBackground", "pbg", "var(--bit-clr-fg-pri)", "var(--bit-clr-fg-pri-hover)", "var(--bit-clr-bg-pri-dis-text)")]
    [DataRow("SecondaryBackground", "sbg", "var(--bit-clr-fg-pri)", "var(--bit-clr-fg-pri-hover)", "var(--bit-clr-bg-sec-dis-text)")]
    [DataRow("TertiaryBackground", "tbg", "var(--bit-clr-fg-pri)", "var(--bit-clr-fg-pri-hover)", "var(--bit-clr-bg-ter-dis-text)")]
    [DataRow("PrimaryForeground", "pfg", "var(--bit-clr-fg-pri)", "var(--bit-clr-fg-pri-hover)", "var(--bit-clr-fg-pri-dis-text)")]
    [DataRow("SecondaryForeground", "sfg", "var(--bit-clr-fg-sec)", "var(--bit-clr-fg-sec-hover)", "var(--bit-clr-fg-sec-dis-text)")]
    [DataRow("TertiaryForeground", "tfg", "var(--bit-clr-fg-ter)", "var(--bit-clr-fg-ter-hover)", "var(--bit-clr-fg-ter-dis-text)")]
    [DataRow("PrimaryBorder", "pbr", "var(--bit-clr-fg-pri)", "var(--bit-clr-fg-pri-hover)", "var(--bit-clr-brd-pri-dis-text)")]
    [DataRow("SecondaryBorder", "sbr", "var(--bit-clr-fg-pri)", "var(--bit-clr-fg-pri-hover)", "var(--bit-clr-brd-sec-dis-text)")]
    [DataRow("TertiaryBorder", "tbr", "var(--bit-clr-fg-pri)", "var(--bit-clr-fg-pri-hover)", "var(--bit-clr-brd-ter-dis-text)")]
    public async Task Color_EveryValue_PaintsTheRestHoverAndDisabledColorsOfItsRole(string color, string suffix, string rest, string hover, string disabled)
    {
        await OpenAsync(Path);

        var link = Page.Locator($"#lnk-color-{color}");

        await Expect(link).ToHaveClassAsync(HasClass($"bit-lnk-{suffix}"));
        await Expect(link).ToHaveCSSAsync("color", await ResolveAsync("color", rest));

        await link.HoverAsync();
        await Expect(link).ToHaveCSSAsync("color", await ResolveAsync("color", hover));

        await Expect(Page.Locator($"#lnk-color-dis-{color}")).ToHaveCSSAsync("color", await ResolveAsync("color", disabled));
    }

    [TestMethod]
    public async Task NoColor_TheContentKeepsTheColorAroundIt()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-nocolor")).ToHaveClassAsync(HasClass("bit-lnk-ncl"));
        await Expect(Page.Locator("#lnk-nocolor")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Page.Locator("#lnk-nocolor").HoverAsync();
        await Expect(Page.Locator("#nocolor-text")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");

        // Unset is the primary role.
        await Expect(Page.Locator("#lnk-color-none")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri)"));
    }

    [TestMethod]
    [DataRow("Small", "sm", "--bit-tpg-fs-xs")]
    [DataRow("Medium", "md", "--bit-tpg-fs-sm")]
    [DataRow("Large", "lg", "--bit-tpg-fs-md")]
    public async Task Size_EveryValue_SetsTheFontSize_AndTheIconFollows(string size, string suffix, string token)
    {
        await OpenAsync(Path);

        var link = Page.Locator($"#lnk-size-{size}");
        var expected = await ResolveAsync("font-size", $"var({token})");

        await Expect(link).ToHaveClassAsync(HasClass($"bit-lnk-{suffix}"));
        await Expect(link).ToHaveCSSAsync("font-size", expected);
        await Expect(link.Locator("> i")).ToHaveCSSAsync("font-size", expected);

        // Unset, it takes the size around it.
        await Expect(Page.Locator("#lnk-size-none")).ToHaveCSSAsync("font-size", "23px");
    }

    [TestMethod]
    public async Task StyleClassAndAttributes_LandOnTheRoot()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-style")).ToHaveCSSAsync("color", "rgb(218, 165, 32)");
        await Expect(Page.Locator("#lnk-style")).ToHaveCSSAsync("font-weight", "700");
        await Expect(Page.Locator("#lnk-class")).ToHaveClassAsync(HasClass("e2e-link-class"));

        await Expect(Page.Locator("#lnk-attrs")).ToHaveAttributeAsync("data-e2e", "splatted");
        await Expect(Page.Locator("#lnk-attrs")).ToHaveAttributeAsync("title", "Splatted title");
        await Expect(Page.Locator("#lnk-attrs")).ToHaveAttributeAsync("aria-label", "Aria name");

        await Expect(Page.Locator("#lnk-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Page.Locator("#lnk-collapsed")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitOrInheritedDirection_KeepsALeadingIconOnTheLeadingSide()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lnk-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#lnk-rtl")).ToHaveCSSAsync("direction", "rtl");

        foreach (var id in new[] { "#lnk-rtl-icon", "#lnk-rtl-inherited" })
        {
            Assert.IsFalse(await IconIsBeforeTextAsync(id), $"{id}: the leading icon is to the right of the text.");
            Assert.IsTrue(Px(await ComputedStyleAsync($"{id} > i", "margin-left")) > 0, $"{id}: the gap is on the icon's inline end (left).");
            Assert.AreEqual(0, Px(await ComputedStyleAsync($"{id} > i", "margin-right")));
        }

        Assert.IsTrue(await IconIsBeforeTextAsync("#lnk-ltr-icon"));
    }

    [TestMethod]
    public async Task Runtime_ChangingTheParameters_ReRendersTheLink()
    {
        await OpenAsync(Path);

        var link = Page.Locator("#lnk-runtime");

        Assert.AreEqual("A", await link.EvaluateAsync<string>("el => el.tagName"));

        await Page.Locator("#btn-clear-href").ClickAsync();
        await Expect(link).ToHaveAttributeAsync("type", "button");
        await link.ClickAsync();
        await Expect(Page.Locator("#runtime-clicks")).ToHaveTextAsync("1");

        await Page.Locator("#btn-set-href").ClickAsync();
        await Expect(link).ToHaveAttributeAsync("href", "/e2e/link");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(link).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(link).ToHaveAttributeAsync("aria-disabled", "true");
        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(link).ToHaveAttributeAsync("href", "/e2e/link");
        await Expect(link).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));

        await Page.Locator("#btn-set-color").ClickAsync();
        await Expect(link).ToHaveClassAsync(HasClass("bit-lnk-suc"));
        await Expect(link).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-suc-fg)"));

        await Page.Locator("#btn-set-target").ClickAsync();
        await Expect(link).ToHaveAttributeAsync("target", "_blank");
        await Expect(link.Locator(".bit-lnk-hnt")).ToHaveTextAsync("(opens in a new tab)");

        await Page.Locator("#btn-set-text").ClickAsync();
        await Expect(link).ToHaveAccessibleNameAsync("Changed (opens in a new tab)");
    }

    [TestMethod]
    public async Task Unmount_ALinkFollowingTheUrl_StopsListeningWithoutAnError()
    {
        await OpenAsync(Path);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#lnk-mounted")).ToHaveCountAsync(0);

        // A navigation after the unmount raises the location event it no longer listens to.
        await Page.Locator("#btn-go-current").ClickAsync();
        await Expect(Page.Locator("#location")).ToHaveTextAsync("/e2e/link/current");

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#lnk-mounted")).ToHaveAttributeAsync("aria-current", "page");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private Task<bool> IconIsBeforeTextAsync(string selector)
    {
        return Page.Locator(selector).EvaluateAsync<bool>(
            @"el => {
                const text = [...el.childNodes].find(n => n.nodeType === Node.TEXT_NODE && n.textContent.trim());
                const range = document.createRange();
                range.selectNodeContents(text);
                const t = range.getBoundingClientRect();
                const i = el.querySelector('i').getBoundingClientRect();
                return i.right <= t.left + 1;
            }");
    }

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    // What a CSS value - a theme token, typically - resolves to on this page, read off a probe element.
    private Task<string> ResolveAsync(string property, string value)
    {
        return Page.EvaluateAsync<string>(
            @"([property, value]) => {
                const probe = document.createElement('div');
                probe.style.setProperty(property, value);
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).getPropertyValue(property);
                probe.remove();
                return resolved;
            }", new[] { property, value });
    }
}
