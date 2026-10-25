using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitPagination (/e2e/pagination) in a real browser: the prerendered landmark, selecting pages by
/// click and keyboard with the two-way and one-way SelectedPage, DefaultSelectedPage, OnChange, the ranges BoundaryCount
/// and MiddleCount generate, the plain and clickable ellipses, the first/previous/next/last buttons with their texts,
/// icons and Loop, the focus handed over when a button disables itself, the summary, TotalItems with the page size
/// selector and OnPageSizeChange, the go to page input, HideOnSinglePage, Alignment, the links GetPageHref makes, the
/// aria labels and FocusAsync, the disabled contract, every Variant, Color and Size, Rounded, the external icons,
/// Style/Class/Styles/Classes, Visibility, HtmlAttributes, the BitParams cascade, the public css variables, RTL and a
/// re-render and unmount.
/// </summary>
[TestClass]
public class BitPaginationE2ETests : E2ETestBase
{
    private const string Url = "/e2e/pagination";
    private const string Transparent = "rgba(0, 0, 0, 0)";

    // The semantic colors the pagination takes (BitPagination.razor.cs Color switch) and their tokens.
    private static readonly (string Color, string Suffix, string Token)[] Colors =
    [
        ("Primary", "pri", "pri"), ("Secondary", "sec", "sec"), ("Tertiary", "ter", "ter"), ("Info", "inf", "inf"),
        ("Success", "suc", "suc"), ("Warning", "wrn", "wrn"), ("SevereWarning", "swr", "swr"), ("Error", "err", "err")
    ];

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheNavigationLandmark()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"pg-basic\"");
        StringAssert.Contains(html, "aria-label=\"Pagination\"");
        StringAssert.Contains(html, "aria-current=\"page\"");
        StringAssert.Contains(html, ">Loading<");

        await OpenAsync(Url);
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Basic_ClickingAPage_SelectsIt_AndOnChangeReportsItOnce()
    {
        await OpenAsync(Url);

        await Expect(Selected("pg-basic")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-page")).ToHaveTextAsync("1");
        await Expect(Nav("pg-basic", "Previous page")).ToBeDisabledAsync();
        await Expect(Nav("pg-basic", "Previous page")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Nav("pg-basic", "Next page")).ToBeEnabledAsync();
        await ExpectSequenceAsync("pg-basic", "1 2 3 4 5");

        await PageButton("pg-basic", 3).ClickAsync();

        await Expect(Selected("pg-basic")).ToHaveTextAsync("3");
        await Expect(PageButton("pg-basic", 3)).ToHaveClassAsync(HasClass("bit-pgn-sel"));
        await Expect(PageButton("pg-basic", 1)).Not.ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.Locator("#basic-page")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-change-arg")).ToHaveTextAsync("3");

        // The selected page again changes nothing; the next one is reported once more.
        await PageButton("pg-basic", 3).ClickAsync();
        await Nav("pg-basic", "Next page").ClickAsync();
        await Expect(Page.Locator("#basic-change-arg")).ToHaveTextAsync("4");
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("2");

        await Nav("pg-basic", "Previous page").ClickAsync();
        await Expect(Selected("pg-basic")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task Basic_NextReachingTheEnd_DisablesItAndHandsTheFocusToTheSelectedPage()
    {
        await OpenAsync(Url);

        await PageButton("pg-basic", 4).ClickAsync();
        await Nav("pg-basic", "Next page").ClickAsync();

        await Expect(Selected("pg-basic")).ToHaveTextAsync("5");
        await Expect(Nav("pg-basic", "Next page")).ToBeDisabledAsync();
        await Expect(PageButton("pg-basic", 5)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Basic_ThePageSetsTheBoundSelection_AndAnOutOfRangeOneIsWrittenBackClamped()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-basic-set-4").ClickAsync();
        await Expect(Selected("pg-basic")).ToHaveTextAsync("4");
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("0");

        await Page.Locator("#btn-basic-set-99").ClickAsync();
        await Expect(Selected("pg-basic")).ToHaveTextAsync("5");
        await Expect(Page.Locator("#basic-page")).ToHaveTextAsync("5");
    }

    [TestMethod]
    public async Task DefaultSelectedPage_PicksTheStartingPage_ClampedIntoTheRange()
    {
        await OpenAsync(Url);

        await Expect(Selected("pg-default-3")).ToHaveTextAsync("3");
        await Expect(Selected("pg-default-out")).ToHaveTextAsync("5");
        await Expect(Selected("pg-default-none")).ToHaveTextAsync("1");

        await PageButton("pg-default-3", 1).ClickAsync();
        await Expect(Selected("pg-default-3")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Disabled_EveryControlIsDisabled_ARealClickChangesNothing_AndThePagesHandlersAreDropped()
    {
        await OpenAsync(Url);

        var root = Root("pg-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));

        var buttons = root.Locator("button");
        var count = await buttons.CountAsync();
        Assert.IsTrue(count >= 7);
        for (var i = 0; i < count; i++)
        {
            await Expect(buttons.Nth(i)).ToBeDisabledAsync();
            await Expect(buttons.Nth(i)).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await Expect(root.Locator("select")).ToBeDisabledAsync();
        await Expect(root.Locator("select")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(root.Locator("input")).ToBeDisabledAsync();
        await Expect(root.Locator("input")).ToHaveAttributeAsync("aria-disabled", "true");

        var page2 = await BoxAsync(PageButton("pg-disabled", 2));
        await Page.Mouse.ClickAsync(page2.X + page2.Width / 2, page2.Y + page2.Height / 2);

        await root.DispatchEventAsync("dblclick");
        await root.DispatchEventAsync("pointerdown");
        await root.DispatchEventAsync("mouseover");

        await Expect(Page.Locator("#disabled-hovers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-activations")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-changes")).ToHaveTextAsync("0");
        await Expect(Selected("pg-disabled")).ToHaveTextAsync("3");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(PageButton("pg-disabled", 2)).ToBeEnabledAsync();
        await Expect(PageButton("pg-disabled", 2)).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await root.DispatchEventAsync("dblclick");
        await Expect(Page.Locator("#disabled-activations")).ToHaveTextAsync("1");

        await PageButton("pg-disabled", 2).ClickAsync();
        await Expect(Selected("pg-disabled")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#disabled-changes")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task OneWay_AClickIsOnlyReported_AndTheHeldValueIsWhatIsRendered()
    {
        await OpenAsync(Url);

        await Expect(Selected("pg-one-way")).ToHaveTextAsync("2");

        await PageButton("pg-one-way", 4).ClickAsync();
        await Expect(Page.Locator("#one-way-requested")).ToHaveTextAsync("4");
        await Expect(Selected("pg-one-way")).ToHaveTextAsync("2");

        await Page.Locator("#btn-one-way-apply").ClickAsync();
        await Expect(Selected("pg-one-way")).ToHaveTextAsync("4");

        // A held value outside the range still renders a current page, the nearest end.
        await Page.Locator("#btn-one-way-9").ClickAsync();
        await Expect(Page.Locator("#one-way-page")).ToHaveTextAsync("9");
        await Expect(Selected("pg-one-way")).ToHaveTextAsync("5");
        await Expect(Nav("pg-one-way", "Next page")).ToBeDisabledAsync();
    }

    [TestMethod]
    [DataRow("none", "bit-pgn-fil", "main", "on", "main")]
    [DataRow("Fill", "bit-pgn-fil", "main", "on", "main")]
    [DataRow("Outline", "bit-pgn-otl", "transparent", "main", "main")]
    [DataRow("Text", "bit-pgn-txt", "transparent", "main", "transparent")]
    public async Task Variant_EveryLook_PaintsTheButtonsItsOwnWay(string variant, string cls, string background, string color, string border)
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        var id = $"pg-variant-{variant}";
        var button = PageButton(id, 2);
        await Expect(button).ToHaveClassAsync(HasClass(cls));

        var main = await ResolveColorAsync("var(--bit-clr-pri)");
        var on = await ResolveColorAsync("var(--bit-clr-pri-text)");
        string Expected(string slot) => slot switch { "main" => main, "on" => on, _ => Transparent };

        await Expect(button).ToHaveCSSAsync("background-color", Expected(background));
        await Expect(button).ToHaveCSSAsync("color", Expected(color));
        await Expect(button).ToHaveCSSAsync("border-top-color", Expected(border));

        // The current page is filled with the dark shade in every look, and set heavier.
        var selected = Selected(id);
        await Expect(selected).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri-dark)"));
        await Expect(selected).ToHaveCSSAsync("color", on);
        await Expect(selected).ToHaveCSSAsync("font-weight", "600");
    }

    [TestMethod]
    public async Task Rounded_TurnsTheButtonsIntoCirclesAndPills()
    {
        await OpenAsync(Url);

        await Expect(Root("pg-rounded")).ToHaveClassAsync(HasClass("bit-pgn-rnd"));
        await Expect(Root("pg-not-rounded")).Not.ToHaveClassAsync(HasClass("bit-pgn-rnd"));

        var page = await BoxAsync(PageButton("pg-rounded", 9));
        Assert.IsTrue(ParsePx(await PageButton("pg-rounded", 9).EvaluateAsync<string>("el => getComputedStyle(el).borderTopLeftRadius")) >= page.Height / 2);
        var pill = Nav("pg-rounded", "Previous page");
        var pillBox = await BoxAsync(pill);
        Assert.IsTrue(pillBox.Width > pillBox.Height, "A button with a text is wider than it is tall.");
        Assert.IsTrue(ParsePx(await pill.EvaluateAsync<string>("el => getComputedStyle(el).borderTopLeftRadius")) >= pillBox.Height / 2);
        Assert.IsTrue(ParsePx(await Root("pg-rounded").Locator(".bit-pgn-elp").First.EvaluateAsync<string>("el => getComputedStyle(el).borderTopLeftRadius")) >= page.Height / 2, "The ellipsis takes the shape too.");

        var square = await BoxAsync(PageButton("pg-not-rounded", 4));
        Assert.IsTrue(ParsePx(await PageButton("pg-not-rounded", 4).EvaluateAsync<string>("el => getComputedStyle(el).borderTopLeftRadius")) < square.Height / 2);
    }

    [TestMethod]
    public async Task Ranges_BoundaryCountAndMiddleCount_DecideWhichPagesShow()
    {
        await OpenAsync(Url);

        await ExpectSequenceAsync("pg-range-default-10", "1 2 3 4 5 6 … 9 10");
        await ExpectSequenceAsync("pg-range-default-20", "1 2 … 9 10 11 … 19 20");
        await ExpectSequenceAsync("pg-range-b1", "1 … 5 6 7 … 11");
        await ExpectSequenceAsync("pg-range-b1-m5", "1 … 8 9 10 11 12 … 20");
    }

    [TestMethod]
    public async Task Ranges_WalkingTheRange_MovesTheWindow_AndKeepsTheFocusOnTheActivatedPage()
    {
        await OpenAsync(Url);

        await ExpectSequenceAsync("pg-range-walk", "1 2 3 4 5 6 … 19 20");

        // Activated from the keyboard, so the focus is the user's own in every engine (WebKit focuses no button on a click).
        await PageButton("pg-range-walk", 6).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#walk-page")).ToHaveTextAsync("6");
        await ExpectSequenceAsync("pg-range-walk", "1 2 … 5 6 7 … 19 20");
        await Expect(PageButton("pg-range-walk", 6)).ToBeFocusedAsync();

        await PageButton("pg-range-walk", 20).ClickAsync();
        await ExpectSequenceAsync("pg-range-walk", "1 2 … 15 16 17 18 19 20");
    }

    [TestMethod]
    public async Task Ellipsis_TextAndAriaLabel_NameAPlainGap()
    {
        await OpenAsync(Url);

        await ExpectSequenceAsync("pg-ellipsis-text", "1 2 … 11 12 13 … 23 24");

        var gaps = Root("pg-ellipsis-text").Locator(".bit-pgn-elp");
        await Expect(gaps).ToHaveCountAsync(2);
        await Expect(gaps.First).ToHaveTextAsync("...");
        await Expect(gaps.First).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.AreEqual("DIV", await gaps.First.EvaluateAsync<string>("el => el.tagName"));
        await Expect(Root("pg-ellipsis-text").Locator("li[aria-label='Hidden pages']")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task ClickableEllipsis_JumpsIntoTheMiddleOfTheHiddenPages_AndTheFocusFollows()
    {
        await OpenAsync(Url);

        var gaps = Root("pg-ellipsis-click").Locator("button.bit-pgn-elb");
        await Expect(gaps).ToHaveCountAsync(2);
        await Expect(gaps.First).ToHaveTextAsync("•••");
        await Expect(gaps.First).ToHaveAttributeAsync("aria-label", "More pages (Page 125)");
        await Expect(gaps.First).ToHaveAttributeAsync("title", "More pages (Page 125)");
        await Expect(gaps.Nth(1)).ToHaveAttributeAsync("aria-label", "More pages (Page 375)");

        await gaps.First.ClickAsync();

        await Expect(Page.Locator("#ellipsis-page")).ToHaveTextAsync("125");
        await Expect(Selected("pg-ellipsis-click")).ToHaveTextAsync("125");
        await Expect(PageButton("pg-ellipsis-click", 125)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task FirstAndLast_JumpToTheEnds_DisableThemselves_AndHandTheFocusOver()
    {
        await OpenAsync(Url);

        var first = Nav("pg-first-last", "First page");
        var last = Nav("pg-first-last", "Last page");

        await Expect(first).ToBeDisabledAsync();
        await Expect(first).ToHaveAttributeAsync("title", "First page");
        await Expect(last).ToHaveAttributeAsync("title", "Last page");

        await last.ClickAsync();
        await Expect(Page.Locator("#first-last-page")).ToHaveTextAsync("24");
        await Expect(last).ToBeDisabledAsync();
        await Expect(Nav("pg-first-last", "Next page")).ToBeDisabledAsync();
        await Expect(PageButton("pg-first-last", 24)).ToBeFocusedAsync();

        await first.ClickAsync();
        await Expect(Page.Locator("#first-last-page")).ToHaveTextAsync("1");
        await Expect(PageButton("pg-first-last", 1)).ToBeFocusedAsync();

        // ShowPreviousButton and ShowNextButton turn those two off.
        await Expect(Nav("pg-no-prev-next", "Previous page")).ToHaveCountAsync(0);
        await Expect(Nav("pg-no-prev-next", "Next page")).ToHaveCountAsync(0);
        await Expect(Nav("pg-no-prev-next", "First page")).ToHaveCountAsync(1);
        await Expect(Nav("pg-no-prev-next", "Last page")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Loop_WrapsPreviousAndNextAroundTheEnds()
    {
        await OpenAsync(Url);

        var previous = Nav("pg-loop", "Previous page");
        var next = Nav("pg-loop", "Next page");

        await Expect(previous).ToBeEnabledAsync();
        await previous.ClickAsync();
        await Expect(Page.Locator("#loop-page")).ToHaveTextAsync("5");
        await Expect(next).ToBeEnabledAsync();

        await next.ClickAsync();
        await Expect(Page.Locator("#loop-page")).ToHaveTextAsync("1");

        // A single page has nowhere to loop to.
        await Expect(Nav("pg-loop-single", "Previous page")).ToBeDisabledAsync();
        await Expect(Nav("pg-loop-single", "Next page")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task ButtonTexts_SitBesideTheIcons_AndTheSpokenNameStays()
    {
        await OpenAsync(Url);

        foreach (var (label, text) in new[] { ("First page", "First"), ("Previous page", "Previous"), ("Next page", "Next"), ("Last page", "Last") })
        {
            var button = Nav("pg-texts", label);
            await Expect(button.Locator(".bit-pgn-btx")).ToHaveTextAsync(text);
            await Expect(button).ToHaveClassAsync(HasClass("bit-pgn-lbl"));
            // The tooltip would only repeat the text on screen.
            Assert.IsNull(await button.GetAttributeAsync("title"));
        }

        // Previous reads icon then text, Next text then icon.
        var prevIcon = await BoxAsync(Nav("pg-texts", "Previous page").Locator("i"));
        var prevText = await BoxAsync(Nav("pg-texts", "Previous page").Locator(".bit-pgn-btx"));
        Assert.IsTrue(prevIcon.X < prevText.X);
        var nextIcon = await BoxAsync(Nav("pg-texts", "Next page").Locator("i"));
        var nextText = await BoxAsync(Nav("pg-texts", "Next page").Locator(".bit-pgn-btx"));
        Assert.IsTrue(nextText.X < nextIcon.X);
    }

    [TestMethod]
    public async Task IconNames_ReplaceTheDefaultChevrons()
    {
        await OpenAsync(Url);

        await Expect(Nav("pg-icon-names", "First page").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--DoubleChevronLeft"));
        await Expect(Nav("pg-icon-names", "Previous page").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Previous"));
        await Expect(Nav("pg-icon-names", "Next page").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Next"));
        await Expect(Nav("pg-icon-names", "Last page").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--DoubleChevronRight"));

        await Expect(Nav("pg-first-last", "First page").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChevronRightEnd6"));
        await Expect(Nav("pg-first-last", "First page").Locator("i")).ToHaveClassAsync(HasClass("bit-pgn-trs"));
        await Expect(Nav("pg-first-last", "Next page").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(Nav("pg-first-last", "Next page").Locator("i")).ToHaveClassAsync(HasClass("bit-pgn-tre"));
        await Expect(Nav("pg-first-last", "Next page").Locator("i")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [TestMethod]
    public async Task Summary_ShowsThePosition_AndFollowsTheSelection()
    {
        await OpenAsync(Url);

        var summary = Root("pg-summary").Locator(".bit-pgn-sum");
        await Expect(summary).ToHaveAttributeAsync("role", "status");
        await Expect(summary).ToHaveTextAsync("Page 4 of 12");

        await PageButton("pg-summary", 5).ClickAsync();
        await Expect(summary).ToHaveTextAsync("Page 5 of 12");

        // ShowPageButtons="false" leaves the navigation buttons alone.
        await Expect(Root("pg-compact").Locator("[aria-current]")).ToHaveCountAsync(0);
        await Expect(Root("pg-compact").Locator(".bit-pgn-btn")).ToHaveCountAsync(4);
        await Nav("pg-compact", "Next page").ClickAsync();
        await Expect(Root("pg-compact").Locator(".bit-pgn-sum")).ToHaveTextAsync("Page 5 of 12");

        await Expect(Root("pg-get-summary").Locator(".bit-pgn-sum")).ToHaveTextAsync("4 / 12");
        await Nav("pg-get-summary", "Previous page").ClickAsync();
        await Expect(Root("pg-get-summary").Locator(".bit-pgn-sum")).ToHaveTextAsync("3 / 12");
    }

    [TestMethod]
    public async Task TotalItems_ThePageSizeSelectorRecomputesTheRange_AndKeepsTheFirstItemInView()
    {
        await OpenAsync(Url);

        var summary = Root("pg-total").Locator(".bit-pgn-sum");
        await Expect(summary).ToHaveTextAsync("Showing 1 to 10 of 240 results");
        await Expect(Page.Locator("#total-size")).ToHaveTextAsync("10");
        await ExpectSequenceAsync("pg-total", "1 2 3 4 5 6 … 23 24");

        var select = Root("pg-total").Locator("select");
        await Expect(Root("pg-total").Locator("label.bit-pgn-psl")).ToHaveTextAsync("Items per page");
        Assert.AreEqual("10 25 100", string.Join(' ', await select.Locator("option").AllTextContentsAsync()));

        await PageButton("pg-total", 6).ClickAsync();
        await Expect(summary).ToHaveTextAsync("Showing 51 to 60 of 240 results");

        await select.SelectOptionAsync("25");

        await Expect(Page.Locator("#total-size")).ToHaveTextAsync("25");
        await Expect(Page.Locator("#total-page")).ToHaveTextAsync("3");
        await Expect(summary).ToHaveTextAsync("Showing 51 to 75 of 240 results");
        // OnChange reports the moved page ahead of OnPageSizeChange.
        await Expect(Page.Locator("#total-log")).ToHaveTextAsync("change:6;change:3;size:25;");
        await ExpectSequenceAsync("pg-total", "1 2 3 4 5 6 … 9 10");

        await Expect(Root("pg-total-default").Locator(".bit-pgn-sum")).ToHaveTextAsync("1 - 10 of 95");
        await ExpectSequenceAsync("pg-total-default", "1 2 3 4 5 6 … 9 10");
    }

    [TestMethod]
    public async Task PageSize_WithCount_IsTheConsumersToRecompute_AndTheSelectorIsNamedWithoutALabel()
    {
        await OpenAsync(Url);

        var select = Root("pg-count-size").Locator("select");
        await Expect(Root("pg-count-size").Locator("label")).ToHaveCountAsync(0);
        await Expect(select).ToHaveAttributeAsync("aria-label", "Rows per page");

        await select.SelectOptionAsync("20");

        await Expect(Page.Locator("#count-size-size")).ToHaveTextAsync("20");
        await Expect(Page.Locator("#count-size-count")).ToHaveTextAsync("5");
        await ExpectSequenceAsync("pg-count-size", "1 2 3 4 5");

        // A size the options do not hold is offered in its place among them.
        var unlisted = Root("pg-size-unlisted").Locator("select");
        Assert.AreEqual("10 15 20 50", string.Join(' ', await unlisted.Locator("option").AllTextContentsAsync()));
        await Expect(unlisted).ToHaveValueAsync("15");

        // A size that is not positive falls back to the first option and is written back.
        await Expect(Page.Locator("#zero-size")).ToHaveTextAsync("10");
        await Expect(Root("pg-size-zero").Locator("select")).ToHaveValueAsync("10");
    }

    [TestMethod]
    public async Task GoToPage_EnterJumpsToTheTypedPage_ClampsPastTheEnds_AndClearsTheInput()
    {
        await OpenAsync(Url);

        var input = Root("pg-go").Locator("input.bit-pgn-gti");
        await Expect(Root("pg-go").Locator("label.bit-pgn-gtl")).ToHaveTextAsync("Go to");
        await Expect(input).ToHaveAttributeAsync("type", "number");
        await Expect(input).ToHaveAttributeAsync("max", "1250");

        await input.FillAsync("37");
        await input.PressAsync("Enter");
        await Expect(Page.Locator("#go-page")).ToHaveTextAsync("37");
        await Expect(Selected("pg-go")).ToHaveTextAsync("37");
        await Expect(input).ToHaveValueAsync("");

        await input.FillAsync("99999");
        await input.PressAsync("Enter");
        await Expect(Page.Locator("#go-page")).ToHaveTextAsync("1250");

        await input.FillAsync("0");
        await input.PressAsync("Enter");
        await Expect(Page.Locator("#go-page")).ToHaveTextAsync("1");

        // Losing the focus commits it as well.
        await input.FillAsync("12");
        await input.PressAsync("Tab");
        await Expect(Page.Locator("#go-page")).ToHaveTextAsync("12");
        await Expect(input).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task GoToPage_WithoutText_IsNamedByItsAriaLabel_AndEnterDoesNotSubmitAForm()
    {
        await OpenAsync(Url);

        await Expect(Root("pg-go-no-text").Locator("label")).ToHaveCountAsync(0);
        await Expect(Root("pg-go-no-text").Locator("input")).ToHaveAttributeAsync("aria-label", "Jump to page");
        Assert.IsNull(await Root("pg-go").Locator("input").GetAttributeAsync("aria-label"), "The visible label names it while it is there.");

        var input = Root("pg-go-form").Locator("input");
        await input.FillAsync("7");
        await input.PressAsync("Enter");

        await Expect(Page.Locator("#go-form-page")).ToHaveTextAsync("7");
        await Expect(Page.Locator("#go-form-submits")).ToHaveTextAsync("0");
        Assert.AreEqual(BaseUrl + Url, Page.Url.Split('?')[0]);
    }

    [TestMethod]
    public async Task HideOnSinglePage_RendersNothingForASinglePage_AndAppearsWithASecond()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#pg-hide-single")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#hide-single-box nav")).ToHaveCountAsync(0);

        await Page.Locator("#btn-hide-count-2").ClickAsync();
        await Expect(Page.Locator("#pg-hide-single")).ToBeVisibleAsync();
        await Expect(Root("pg-hide-single").Locator(".bit-pgn-sum")).ToHaveTextAsync("Page 1 of 2");

        await Page.Locator("#btn-hide-count-1").ClickAsync();
        await Expect(Page.Locator("#pg-hide-single")).ToHaveCountAsync(0);

        // Without it a single page still renders, with nowhere to go.
        await ExpectSequenceAsync("pg-show-single", "1");
        await Expect(Nav("pg-show-single", "Next page")).ToBeDisabledAsync();
        await Expect(Nav("pg-show-single", "Previous page")).ToBeDisabledAsync();
    }

    [TestMethod]
    [DataRow("Start", "flex-start")]
    [DataRow("End", "flex-end")]
    [DataRow("Center", "center")]
    [DataRow("SpaceBetween", "space-between")]
    [DataRow("SpaceAround", "space-around")]
    [DataRow("SpaceEvenly", "space-evenly")]
    [DataRow("Baseline", "")]
    [DataRow("Stretch", "")]
    public async Task Alignment_LinesTheControlsUpInsideTheContainer(string alignment, string justify)
    {
        await OpenAsync(Url);

        var root = Root($"pg-align-{alignment}");
        var container = await BoxAsync(Page.Locator($"#align-box-{alignment}"));
        var buttons = root.Locator(".bit-pgn-btn");
        var first = await BoxAsync(buttons.First);
        var last = await BoxAsync(buttons.Last);

        if (justify.Length == 0)
        {
            await Expect(root).Not.ToHaveClassAsync(HasClass("bit-pgn-aln"));
            await Expect(root).ToHaveCSSAsync("display", "inline-flex");
            Assert.IsTrue((await BoxAsync(root)).Width < container.Width / 2, "Not stretched.");
            return;
        }

        await Expect(root).ToHaveClassAsync(HasClass("bit-pgn-aln"));
        await Expect(root).ToHaveCSSAsync("display", "flex");
        await Expect(root).ToHaveCSSAsync("justify-content", justify);

        var left = first.X - container.X;
        var right = container.X + container.Width - (last.X + last.Width);
        switch (alignment)
        {
            case "Start":
                Assert.IsTrue(left < 3 && right > 100);
                break;
            case "End":
                Assert.IsTrue(right < 3 && left > 100);
                break;
            case "Center":
                Assert.AreEqual(left, right, 3);
                break;
            case "SpaceBetween":
                Assert.IsTrue(left < 3 && right < 3);
                break;
            default:
                Assert.IsTrue(left > 3 && right > 3);
                Assert.AreEqual(left, right, 3);
                break;
        }
    }

    [TestMethod]
    public async Task Links_GetPageHrefTurnsEveryControlIntoALink_AndAPlainClickSelectsThePage()
    {
        await OpenAsync(Url);

        var root = Root("pg-links");
        await Expect(root.Locator("button")).ToHaveCountAsync(0);

        var page2 = PageButton("pg-links", 2);
        Assert.AreEqual("A", await page2.EvaluateAsync<string>("el => el.tagName"));
        await Expect(page2).ToHaveAttributeAsync("href", "/e2e/pagination?page=2");

        // A link with nowhere to go drops its href, keeps a role and reports aria-disabled.
        var previous = Nav("pg-links", "Previous page");
        Assert.IsNull(await previous.GetAttributeAsync("href"));
        await Expect(previous).ToHaveAttributeAsync("role", "link");
        await Expect(previous).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Nav("pg-links", "Next page")).ToHaveAttributeAsync("rel", "next");
        await Expect(Nav("pg-links", "Last page")).ToHaveAttributeAsync("href", "/e2e/pagination?page=10");
        await Expect(root.Locator("a.bit-pgn-elb")).ToHaveAttributeAsync("href", "/e2e/pagination?page=7");

        await page2.ClickAsync();
        await Expect(Page.Locator("#link-page")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#link-url")).ToHaveTextAsync("/e2e/pagination?page=2");
        await Expect(Nav("pg-links", "Previous page")).ToHaveAttributeAsync("href", "/e2e/pagination?page=1");
        await Expect(Nav("pg-links", "Previous page")).ToHaveAttributeAsync("rel", "prev");
    }

    [TestMethod]
    public async Task Links_AModifiedClick_LeavesTheSelectionAlone()
    {
        await OpenAsync(Url);

        // The browser's own handling of the modified click (a new tab) is kept out of the test.
        await Page.EvaluateAsync("() => window.addEventListener('click', e => { if (e.ctrlKey || e.shiftKey) e.preventDefault(); }, true)");

        await PageButton("pg-links", 3).ClickAsync(new() { Modifiers = [KeyboardModifier.Control] });
        await PageButton("pg-links", 4).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Nav("pg-links", "Next page").ClickAsync();

        // Had either modified click selected its page, Next would have gone past 2.
        await Expect(Page.Locator("#link-page")).ToHaveTextAsync("2");
        await Expect(Selected("pg-links")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Accessibility_ANamedLandmarkHoldingAListOfNamedControls()
    {
        await OpenAsync(Url);

        var root = Root("pg-a11y");
        Assert.AreEqual("NAV", await root.EvaluateAsync<string>("el => el.tagName"));
        await Expect(root).ToHaveAttributeAsync("aria-label", "Search results pages");
        await Expect(Root("pg-a11y-default")).ToHaveAttributeAsync("aria-label", "Pagination");
        await Expect(Root("pg-attrs")).ToHaveAttributeAsync("aria-label", "Splatted name");

        await Expect(root.Locator("ul")).ToHaveAttributeAsync("role", "list");
        var items = await root.Locator("ul > li").CountAsync();
        Assert.AreEqual(items, await root.Locator("ul > li[role='listitem']").CountAsync());

        var first = root.Locator(".bit-pgn-btn").First;
        await Expect(first).ToHaveAttributeAsync("aria-label", "Go to the first page");
        await Expect(first).ToHaveAttributeAsync("title", "Go to the first page");
        await Expect(root.Locator(".bit-pgn-btn[aria-label='Go to the previous page']")).ToHaveCountAsync(1);
        await Expect(root.Locator(".bit-pgn-btn[aria-label='Go to the next page']")).ToHaveCountAsync(1);
        await Expect(root.Locator(".bit-pgn-btn[aria-label='Go to the last page']")).ToHaveCountAsync(1);

        await Expect(root.Locator("[aria-current='page']")).ToHaveCountAsync(1);
        await Expect(root.Locator("[aria-current='page']")).ToHaveAttributeAsync("aria-label", "Page 4, results 31 to 40 (current)");
        await Expect(PageButton("pg-a11y", 5)).ToHaveAttributeAsync("aria-label", "Page 5, results 41 to 50");
        await Expect(PageButton("pg-a11y-default", 2)).ToHaveAttributeAsync("aria-label", "Page 2");
        await Expect(PageButton("pg-a11y", 5)).ToHaveAttributeAsync("type", "button");
    }

    [TestMethod]
    public async Task FocusAsync_PutsTheFocusOnTheSelectedPage_OrTheFirstNavigationButton()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-focus").ClickAsync();
        await Expect(PageButton("pg-a11y", 4)).ToBeFocusedAsync();

        await Page.Locator("#btn-focus-compact").ClickAsync();
        await Expect(Nav("pg-a11y-compact", "First page")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_TabSkipsTheDisabledControls_EnterAndSpaceSelectAPage()
    {
        await OpenAsync(Url);

        await Nav("pg-a11y-default", "Next page").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        // First and Previous are disabled on the first page, so the page itself is next.
        await Expect(PageButton("pg-keyboard", 1)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(PageButton("pg-keyboard", 2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#keyboard-page")).ToHaveTextAsync("2");
        await Expect(PageButton("pg-keyboard", 2)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync(" ");
        await Expect(Page.Locator("#keyboard-page")).ToHaveTextAsync("3");

        // The focus ring is drawn for the keyboard.
        await Expect(PageButton("pg-keyboard", 3)).Not.ToHaveCSSAsync("box-shadow", "none");

        await Nav("pg-keyboard", "Last page").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#keyboard-page")).ToHaveTextAsync("5");
        await Expect(PageButton("pg-keyboard", 5)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(PageButton("pg-keyboard", 4)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task BitParams_CascadesItsDefaults_AndAPaginationsOwnParametersWin()
    {
        await OpenAsync(Url);

        var one = Root("pg-params-1");
        await Expect(one).ToHaveClassAsync(HasClass("bit-pgn-rnd"));
        await Expect(PageButton("pg-params-1", 2)).ToHaveClassAsync(HasClass("bit-pgn-otl"));
        await Expect(one.Locator(".bit-pgn-sum")).ToHaveTextAsync("Página 4 de 12");
        await Expect(Nav("pg-params-1", "First page")).ToHaveCountAsync(1);
        await Expect(Nav("pg-params-1", "Página siguiente")).ToHaveCountAsync(1);
        await Expect(one).ToHaveAttributeAsync("aria-label", "Páginas de resultados");

        var two = Root("pg-params-2");
        await Expect(PageButton("pg-params-2", 2)).ToHaveClassAsync(HasClass("bit-pgn-fil"));
        await Expect(two.Locator(".bit-pgn-sum")).ToHaveCountAsync(0);
        await Expect(two).ToHaveClassAsync(HasClass("bit-pgn-rnd"));

        var outside = Root("pg-params-outside");
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-pgn-rnd"));
        await Expect(outside.Locator(".bit-pgn-sum")).ToHaveCountAsync(0);
        await Expect(Nav("pg-params-outside", "Next page")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Color_EveryColor_PaintsTheButtonsAndTheSelectedPageFromItsRole()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        foreach (var (color, suffix, token) in Colors)
        {
            var root = Root($"pg-color-{color}");
            await Expect(root).ToHaveClassAsync(HasClass($"bit-pgn-{suffix}"));

            var main = await ResolveColorAsync($"var(--bit-clr-{token})");
            await Expect(PageButton($"pg-color-{color}", 2)).ToHaveCSSAsync("background-color", main);
            await Expect(PageButton($"pg-color-{color}", 2)).ToHaveCSSAsync("color", await ResolveColorAsync($"var(--bit-clr-{token}-text)"));
            await Expect(Selected($"pg-color-{color}")).ToHaveCSSAsync("background-color", await ResolveColorAsync($"var(--bit-clr-{token}-dark)"));

            var outline = PageButton($"pg-color-outline-{color}", 2);
            await Expect(outline).ToHaveCSSAsync("border-top-color", main);
            await Expect(outline).ToHaveCSSAsync("color", main);
            await Expect(outline).ToHaveCSSAsync("background-color", Transparent);
        }
    }

    [TestMethod]
    public async Task Hover_ThePointerRepaintsAPageButtonWithTheHoverColor()
    {
        await OpenAsync(Url);

        var button = PageButton("pg-basic", 2);
        var rest = await button.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri-hover)"));
        Assert.AreNotEqual(rest, await ResolveColorAsync("var(--bit-clr-pri-hover)"));

        await Selected("pg-basic").HoverAsync();
        await Expect(Selected("pg-basic")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri-dark-hover)"));
    }

    [TestMethod]
    public async Task ExternalIcons_TheNavigationButtonsRenderTheGivenClasses()
    {
        await OpenAsync(Url);

        await Expect(Nav("pg-ext-fa", "First page").Locator("i")).ToHaveClassAsync(HasClass("fa-angles-left"));
        await Expect(Nav("pg-ext-fa", "First page").Locator("i")).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(Nav("pg-ext-fa", "Previous page").Locator("i")).ToHaveClassAsync(HasClass("fa-angle-left"));
        await Expect(Nav("pg-ext-fa", "Next page").Locator("i")).ToHaveClassAsync(HasClass("fa-angle-right"));
        await Expect(Nav("pg-ext-fa", "Last page").Locator("i")).ToHaveClassAsync(HasClass("fa-angles-right"));

        await Expect(Nav("pg-ext-bi", "Previous page").Locator("i")).ToHaveClassAsync(HasClass("bi-caret-left-fill"));
        await Expect(Nav("pg-ext-bi", "Previous page").Locator("i")).ToHaveClassAsync(HasClass("bi"));
        await Expect(Nav("pg-ext-bi", "First page").Locator("i")).ToHaveClassAsync(HasClass("bi-skip-start-fill"));
        await Expect(Nav("pg-ext-bi", "Last page").Locator("i")).ToHaveClassAsync(HasClass("bi-skip-end-fill"));
        // The icon wins over the icon name.
        await Expect(Nav("pg-ext-bi", "Next page").Locator("i")).ToHaveClassAsync(HasClass("bi-caret-right-fill"));
        await Expect(Nav("pg-ext-bi", "Next page").Locator("i")).Not.ToHaveClassAsync(HasClass("bit-icon--Next"));
    }

    [TestMethod]
    [DataRow("Small", "bit-pgn-sm")]
    [DataRow("Medium", "bit-pgn-md")]
    [DataRow("Large", "bit-pgn-lg")]
    public async Task Size_EverySize_ScalesTheButtonsTheSummaryAndTheInput(string size, string cls)
    {
        await OpenAsync(Url);

        var root = Root($"pg-size-{size}");
        await Expect(root).ToHaveClassAsync(HasClass(cls));
        await Expect(Root("pg-size-none")).Not.ToHaveClassAsync(new Regex(@"bit-pgn-(sm|md|lg)\b"));

        var heights = new Dictionary<string, double>();
        foreach (var name in new[] { "Small", "Medium", "Large", "none" })
        {
            heights[name] = (await BoxAsync(PageButton($"pg-size-{name}", 2))).Height;
        }

        var height = heights[size];
        Assert.AreEqual(height, (await BoxAsync(root.Locator(".bit-pgn-sum"))).Height, 1, "The summary stands at the button height.");
        Assert.AreEqual(height, (await BoxAsync(root.Locator("input"))).Height, 1, "The input stands at the button height.");

        switch (size)
        {
            case "Small":
                Assert.IsTrue(heights["Small"] < heights["Medium"]);
                break;
            case "Medium":
                Assert.AreEqual(heights["none"], heights["Medium"], 0.5, "An unset Size is the medium one.");
                break;
            default:
                Assert.IsTrue(heights["Large"] > heights["Medium"]);
                break;
        }
    }

    [TestMethod]
    public async Task StyleClassVisibilityAndHtmlAttributes_LandOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("pg-style")).ToHaveCSSAsync("margin-left", "13px");
        await Expect(Root("pg-style")).ToHaveCSSAsync("flex-direction", "column");
        await Expect(Root("pg-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("pg-class")).ToHaveClassAsync(HasClass("bit-pgn"));
        await Expect(Root("pg-attrs")).ToHaveAttributeAsync("data-test", "pagination-attr");

        var visibility = Root("pg-visibility");
        await Page.Locator("#btn-visibility-hidden").ClickAsync();
        await Expect(visibility).ToHaveCSSAsync("visibility", "hidden");
        await Page.Locator("#btn-visibility-collapsed").ClickAsync();
        await Expect(visibility).ToHaveCSSAsync("display", "none");
        await Page.Locator("#btn-visibility-visible").ClickAsync();
        await Expect(visibility).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        var root = Root("pg-styles");
        await Expect(root).ToHaveCSSAsync("column-gap", "11px");
        await Expect(PageButton("pg-styles", 14)).ToHaveCSSAsync("background-color", "rgb(46, 46, 46)");
        await Expect(Selected("pg-styles")).ToHaveCSSAsync("background-color", "rgb(255, 99, 71)");

        await Expect(Nav("pg-styles", "First page")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(Nav("pg-styles", "First page").Locator("i")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(Nav("pg-styles", "Previous page")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(Nav("pg-styles", "Previous page").Locator("i")).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(Nav("pg-styles", "Next page")).ToHaveCSSAsync("color", "rgb(13, 14, 15)");
        await Expect(Nav("pg-styles", "Next page").Locator("i")).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(Nav("pg-styles", "Last page")).ToHaveCSSAsync("color", "rgb(19, 20, 21)");
        await Expect(Nav("pg-styles", "Last page").Locator("i")).ToHaveCSSAsync("color", "rgb(22, 23, 24)");
        await Expect(root.Locator(".bit-pgn-btx")).ToHaveCSSAsync("color", "rgb(25, 26, 27)");
        await Expect(root.Locator(".bit-pgn-elp").First).ToHaveCSSAsync("color", "rgb(28, 29, 30)");
        await Expect(root.Locator(".bit-pgn-sum")).ToHaveCSSAsync("color", "rgb(31, 32, 33)");
        await Expect(root.Locator(".bit-pgn-gtp")).ToHaveCSSAsync("color", "rgb(34, 35, 36)");
        await Expect(root.Locator(".bit-pgn-gtl")).ToHaveCSSAsync("color", "rgb(37, 38, 39)");
        await Expect(root.Locator(".bit-pgn-gti")).ToHaveCSSAsync("color", "rgb(40, 41, 42)");
        await Expect(root.Locator(".bit-pgn-pss")).ToHaveCSSAsync("color", "rgb(43, 44, 45)");
        await Expect(root.Locator(".bit-pgn-psl")).ToHaveCSSAsync("color", "rgb(46, 47, 48)");
        await Expect(root.Locator(".bit-pgn-pse")).ToHaveCSSAsync("color", "rgb(49, 50, 51)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart()
    {
        await OpenAsync(Url);

        var root = Root("pg-classes");
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        await Expect(PageButton("pg-classes", 14)).ToHaveClassAsync(HasClass("c-button"));
        await Expect(PageButton("pg-classes", 14)).Not.ToHaveClassAsync(HasClass("c-selected"));
        await Expect(Selected("pg-classes")).ToHaveClassAsync(HasClass("c-selected"));

        await Expect(Nav("pg-classes", "First page")).ToHaveClassAsync(HasClass("c-first"));
        await Expect(Nav("pg-classes", "First page").Locator("i")).ToHaveClassAsync(HasClass("c-first-icon"));
        await Expect(Nav("pg-classes", "Previous page")).ToHaveClassAsync(HasClass("c-prev"));
        await Expect(Nav("pg-classes", "Previous page").Locator("i")).ToHaveClassAsync(HasClass("c-prev-icon"));
        await Expect(Nav("pg-classes", "Next page")).ToHaveClassAsync(HasClass("c-next"));
        await Expect(Nav("pg-classes", "Next page").Locator("i")).ToHaveClassAsync(HasClass("c-next-icon"));
        await Expect(Nav("pg-classes", "Last page")).ToHaveClassAsync(HasClass("c-last"));
        await Expect(Nav("pg-classes", "Last page").Locator("i")).ToHaveClassAsync(HasClass("c-last-icon"));
        await Expect(root.Locator(".bit-pgn-btx")).ToHaveClassAsync(HasClass("c-button-text"));
        await Expect(root.Locator(".bit-pgn-elp").First).ToHaveClassAsync(HasClass("c-ellipsis"));
        await Expect(root.Locator(".bit-pgn-sum")).ToHaveClassAsync(HasClass("c-summary"));
        await Expect(root.Locator(".bit-pgn-gtp")).ToHaveClassAsync(HasClass("c-go"));
        await Expect(root.Locator(".bit-pgn-gtl")).ToHaveClassAsync(HasClass("c-go-label"));
        await Expect(root.Locator(".bit-pgn-gti")).ToHaveClassAsync(HasClass("c-go-input"));
        await Expect(root.Locator(".bit-pgn-pss")).ToHaveClassAsync(HasClass("c-pss"));
        await Expect(root.Locator(".bit-pgn-psl")).ToHaveClassAsync(HasClass("c-pss-label"));
        await Expect(root.Locator(".bit-pgn-pse")).ToHaveClassAsync(HasClass("c-pss-select"));

        // SelectedButton follows the selection.
        await PageButton("pg-classes", 14).ClickAsync();
        await Expect(PageButton("pg-classes", 14)).ToHaveClassAsync(HasClass("c-selected"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheDefault()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(Selected("pg-css-vars")).ToHaveCSSAsync("background-color", "rgb(15, 118, 110)");
        await Expect(Selected("pg-css-vars")).ToHaveCSSAsync("font-weight", "800");
        await Expect(PageButton("pg-css-vars", 2)).ToHaveCSSAsync("background-color", "rgb(200, 100, 50)");
        await Expect(PageButton("pg-css-vars", 2)).ToHaveCSSAsync("border-top-left-radius", "3px");
        Assert.AreEqual(44, (await BoxAsync(PageButton("pg-css-vars", 2))).Height, 0.5);
        await Expect(Root("pg-css-vars")).ToHaveCSSAsync("column-gap", "9px");

        var a = await BoxAsync(PageButton("pg-css-vars", 2));
        var b = await BoxAsync(PageButton("pg-css-vars", 3));
        Assert.AreEqual(9, b.X - (a.X + a.Width), 0.5);

        await PageButton("pg-css-vars", 2).HoverAsync();
        await Expect(PageButton("pg-css-vars", 2)).ToHaveCSSAsync("background-color", "rgb(204, 251, 241)");
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorSizeOrRounded_WinsOverTheVariable()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(PageButton("pg-css-vars-color", 2)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err)"));
        await Expect(Selected("pg-css-vars-color")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err-dark)"));

        var rounded = await BoxAsync(PageButton("pg-css-vars-rounded", 2));
        Assert.IsTrue(ParsePx(await PageButton("pg-css-vars-rounded", 2).EvaluateAsync<string>("el => getComputedStyle(el).borderTopLeftRadius")) >= rounded.Height / 2);

        Assert.IsTrue((await BoxAsync(PageButton("pg-css-vars-size", 2))).Height < 40, "Size wins over --bit-Pagination-button-size.");

        // The variant's own twin wins over the shared variable.
        await Expect(PageButton("pg-css-vars-twin", 2)).ToHaveCSSAsync("border-top-color", "rgb(1, 2, 3)");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDir_MirrorsTheOrderAndTheChevrons()
    {
        await OpenAsync(Url);

        var root = Root("pg-rtl");
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));

        await AssertMirroredAsync("pg-rtl", rtl: true);
        await AssertMirroredAsync("pg-rtl-inherited", rtl: true);
        await AssertMirroredAsync("pg-ltr", rtl: false);

        await Expect(Root("pg-rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
    }

    [TestMethod]
    public async Task Lifecycle_ACountChangedAtRuntimeReRenders_AndUnmountingLeavesNoError()
    {
        await OpenAsync(Url);

        await ExpectSequenceAsync("pg-dynamic", "1 2 3");
        await Page.Locator("#btn-count-up").ClickAsync();
        await ExpectSequenceAsync("pg-dynamic", "1 2 3 4");

        await PageButton("pg-dynamic", 4).ClickAsync();
        await Expect(Page.Locator("#dynamic-page")).ToHaveTextAsync("4");

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#pg-dynamic")).ToHaveCountAsync(0);
        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Selected("pg-dynamic")).ToHaveTextAsync("4");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }



    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Selected(string id) => Page.Locator($"#{id} [aria-current='page']");

    private ILocator PageButton(string id, int page)
    {
        return Page.Locator($"#{id} li > .bit-pgn-btn").Filter(new() { HasTextRegex = new Regex($@"^\s*{page}\s*$") });
    }

    private ILocator Nav(string id, string label) => Page.Locator($"#{id} .bit-pgn-btn[aria-label='{label}']");

    // The run of pages and gaps the pagination renders, e.g. "1 2 … 9 10 11 … 19 20".
    private Task<string> SequenceAsync(string id)
    {
        return Page.Locator($"#{id}").EvaluateAsync<string>(@"root => [...root.querySelectorAll('ul > li')]
            .map(li => li.querySelector('.bit-pgn-elp') ? '…' : li.textContent.trim())
            .filter(t => t === '…' || /^\d+$/.test(t))
            .join(' ')");
    }

    // Waits for the pagination to render the given run of pages and gaps.
    private async Task ExpectSequenceAsync(string id, string expected)
    {
        try
        {
            await Page.WaitForFunctionAsync(@"([id, expected]) => {
                const root = document.getElementById(id);
                if (!root) return false;
                return [...root.querySelectorAll('ul > li')]
                    .map(li => li.querySelector('.bit-pgn-elp') ? '…' : li.textContent.trim())
                    .filter(t => t === '…' || /^\d+$/.test(t))
                    .join(' ') === expected;
            }", new object[] { id, expected }, new() { Timeout = 10000 });
        }
        catch (TimeoutException)
        {
            Assert.AreEqual(expected, await SequenceAsync(id), $"The pages of {id}.");
        }
    }

    private async Task AssertMirroredAsync(string id, bool rtl)
    {
        var first = await BoxAsync(Nav(id, "First page"));
        var last = await BoxAsync(Nav(id, "Last page"));
        var one = await BoxAsync(PageButton(id, 1));
        var five = await BoxAsync(PageButton(id, 5));

        Assert.AreEqual(rtl, first.X > last.X, $"{id}: the first button is on the {(rtl ? "right" : "left")}.");
        Assert.AreEqual(rtl, one.X > five.X, $"{id}: the pages run {(rtl ? "right to left" : "left to right")}.");

        // The default chevrons turn with the direction.
        var start = await Nav(id, "First page").Locator("i").EvaluateAsync<string>("el => getComputedStyle(el).transform");
        var end = await Nav(id, "Next page").Locator("i").EvaluateAsync<string>("el => getComputedStyle(el).transform");
        Assert.AreEqual(rtl ? "matrix(1, 0, 0, 1, 0, 0)" : "matrix(-1, 0, 0, 1, 0, 0)", start, $"{id}: the start chevron.");
        Assert.AreEqual(rtl ? "matrix(-1, 0, 0, 1, 0, 0)" : "matrix(1, 0, 0, 1, 0, 0)", end, $"{id}: the end chevron.");
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no box.");
        return box;
    }

    private Task<string> ResolveColorAsync(string value)
    {
        return Page.EvaluateAsync<string>(@"v => {
            const probe = document.createElement('div');
            probe.style.backgroundColor = v;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).backgroundColor;
            probe.remove();
            return color;
        }", value);
    }

    private static double ParsePx(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);
}
