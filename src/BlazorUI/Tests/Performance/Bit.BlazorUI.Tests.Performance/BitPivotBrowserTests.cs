using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser tests of the WAI-ARIA tabs pattern <see cref="BitPivot"/> implements, driven from the keyboard and the
/// pointer: the header is one tab stop, the arrow keys move the focus between the tabs (wrapping at the ends unless
/// Loop is off, stepping over a disabled tab, turned around in a header that reads right to left), Home and End go to
/// the ends, Enter and Space select the focused tab - or the focus selects on its own with SelectOnFocus - and the
/// selection is announced through aria-selected, shown in the tab panel and reported through the bound SelectedKey.
/// Two narrow headers of eight tabs show the overflow behaviours: the Menu one folds what does not fit into a menu
/// the hidden tabs are selected from, the Slide one scrolls the header with its buttons.
/// The pivots of <c>/regression/pivot-behaviour</c> start on Tab 1; Tab 4 of the five-tab ones is disabled.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitPivotBrowserTests
public class BitPivotBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/pivot-behaviour";

    [TestMethod]
    public async Task BitPivot_ArrowKeys_MoveTheFocusAndEnterSelects()
    {
        await Open();

        await TabIntoTheHeader("manual");
        await ExpectFocused("manual", "Tab 1");

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocused("manual", "Tab 2");

        // Moving the focus is not choosing: Tab 1 is still the selected one.
        await ExpectSelected("manual", "Tab 1", "1");

        await Page.Keyboard.PressAsync("Enter");
        await ExpectSelected("manual", "Tab 2", "2");

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocused("manual", "Tab 3");

        await Page.Keyboard.PressAsync(" ");
        await ExpectSelected("manual", "Tab 3", "3");

        // The disabled Tab 4 is stepped over.
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocused("manual", "Tab 5");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocused("manual", "Tab 3");
    }

    [TestMethod]
    public async Task BitPivot_HomeEndAndTheEnds_WrapUnlessLoopIsOff()
    {
        await Open();

        await TabIntoTheHeader("manual");

        await Page.Keyboard.PressAsync("End");
        await ExpectFocused("manual", "Tab 5");

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocused("manual", "Tab 1");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocused("manual", "Tab 5");

        await Page.Keyboard.PressAsync("Home");
        await ExpectFocused("manual", "Tab 1");

        await TabIntoTheHeader("noloop");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocused("noloop", "Tab 1");

        await Page.Keyboard.PressAsync("End");
        await ExpectFocused("noloop", "Tab 5");

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocused("noloop", "Tab 5");
    }

    [TestMethod]
    public async Task BitPivot_TheHeaderIsOneTabStop_AndTheNextTabReachesThePanel()
    {
        await Open();

        await Tab("manual", "Tab 3").ClickAsync();
        await ExpectSelected("manual", "Tab 3", "3");

        // The roving tab index follows the selection: tabbing in lands on the selected tab, not the first.
        await TabIntoTheHeader("manual");
        await ExpectFocused("manual", "Tab 3");

        await Page.Keyboard.PressAsync("Tab");

        var panel = Probe("manual").Locator("[role=tabpanel]");
        await Expect(panel).ToBeFocusedAsync();
        await Expect(panel).ToHaveTextAsync("Panel 3");
        await Expect(panel).ToHaveAttributeAsync("aria-labelledby", await Tab("manual", "Tab 3").GetAttributeAsync("id") ?? "");
    }

    [TestMethod]
    public async Task BitPivot_SelectOnFocus_SelectsAsTheFocusMoves()
    {
        await Open();

        await TabIntoTheHeader("auto");

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocused("auto", "Tab 2");
        await ExpectSelected("auto", "Tab 2", "2");

        await Page.Keyboard.PressAsync("End");
        await ExpectSelected("auto", "Tab 5", "5");
    }

    [TestMethod]
    public async Task BitPivot_RightToLeft_TurnsTheArrowsAround()
    {
        await Open();

        var lefts = await Probe("rtl").EvaluateAsync<double[]>(
            "el => [...el.querySelectorAll('[role=tab]')].slice(0, 2).map(t => t.getBoundingClientRect().left)");
        Assert.IsTrue(lefts[1] < lefts[0], "Tab 2 is laid out to the left of Tab 1");

        await TabIntoTheHeader("rtl");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocused("rtl", "Tab 2");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocused("rtl", "Tab 3");

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocused("rtl", "Tab 2");

        await Page.Keyboard.PressAsync("Enter");
        await ExpectSelected("rtl", "Tab 2", "2");
    }

    [TestMethod]
    public async Task BitPivot_MenuOverflow_FoldsWhatDoesNotFitAndSelectsFromTheMenu()
    {
        await Open();

        var more = Probe("menu").Locator(".bit-pvt-mor");
        await Expect(more).ToBeVisibleAsync();
        await Expect(Tab("menu", "Tab 8")).ToBeHiddenAsync();
        await Expect(Tab("menu", "Tab 1")).ToBeVisibleAsync();

        // The arrow keys only walk the tabs that are in the header.
        await TabIntoTheHeader("menu");
        await Page.Keyboard.PressAsync("End");
        var lastShown = await Probe("menu").EvaluateAsync<string>(
            "el => document.activeElement.closest('[role=tab]')?.textContent.trim()");
        Assert.AreNotEqual("Tab 8", lastShown);
        await Expect(Probe("menu").Locator($"[role=tab]:has-text('{lastShown}')")).ToBeVisibleAsync();

        await more.ClickAsync();
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "true");

        // The menu takes the focus as it opens.
        var menu = Probe("menu").Locator("[role=menu]");
        await Expect(menu).ToBeVisibleAsync();
        await Expect(menu).ToBeFocusedAsync();

        var item = menu.Locator("[role=menuitemradio]", new() { HasText = "Tab 8" });
        await Expect(item).ToHaveAttributeAsync("aria-checked", "false");
        await item.ClickAsync();

        await Expect(menu).ToBeHiddenAsync();
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "false");
        await ExpectSelected("menu", "Tab 8", "8");

        // The selected tab is still folded away, so the button that holds it says it holds the selection.
        await Expect(more).ToHaveClassAsync(new Regex(@"\bbit-pvt-mor-sel\b"));

        // The items of a closed menu stay in the DOM, so the menu is waited for before its keys are pressed.
        await more.ClickAsync();
        await Expect(menu).ToBeFocusedAsync();
        await Expect(menu.Locator("[role=menuitemradio]", new() { HasText = "Tab 8" })).ToHaveAttributeAsync("aria-checked", "true");

        // The keyboard works the menu too: the first item is a tab still folded away, and Enter selects it.
        await Page.Keyboard.PressAsync("Home");
        var firstFolded = await menu.Locator("[role=menuitemradio]").First.InnerTextAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(menu).ToBeHiddenAsync();
        await ExpectSelected("menu", firstFolded.Trim(), firstFolded.Trim().Replace("Tab ", ""));
    }

    [TestMethod]
    public async Task BitPivot_SlideOverflow_ScrollsTheHeaderWithItsButtons()
    {
        await Open();

        var prev = Probe("slide").Locator(".bit-pvt-spv");
        var next = Probe("slide").Locator(".bit-pvt-snx");

        await Expect(next).ToBeEnabledAsync();
        await Expect(prev).ToBeDisabledAsync();
        await Expect(prev).ToHaveAttributeAsync("aria-disabled", "true");

        await next.ClickAsync();

        await Page.WaitForFunctionAsync("() => document.querySelector(`[data-probe='slide'] [role=tablist]`).scrollLeft > 0",
            null, new() { Timeout = DefaultTimeout });
        await Expect(prev).ToBeEnabledAsync();

        // Walking to the far end from the keyboard brings the last tab into view. The header is reached with the
        // pointer here: what the Tab key reaches in a sliding header is the next test's.
        await Tab("slide", "Tab 2").ClickAsync();
        await ExpectSelected("slide", "Tab 2", "2");
        await Page.Keyboard.PressAsync("End");
        await ExpectFocused("slide", "Tab 8");

        await Page.WaitForFunctionAsync(@"() => {
                const header = document.querySelector(`[data-probe='slide'] [role=tablist]`).getBoundingClientRect();
                const tab = [...document.querySelectorAll(`[data-probe='slide'] [role=tab]`)].pop().getBoundingClientRect();
                return tab.left >= header.left - 1 && tab.right <= header.right + 1;
            }", null, new() { Timeout = DefaultTimeout });
        await Expect(next).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task BitPivot_SlideOverflow_TheHeaderIsStillOneTabStop()
    {
        await Open();

        var prev = Probe("slide").Locator(".bit-pvt-spv");

        await Probe("slide").Locator(".bit-pvt-snx").ClickAsync();
        await Expect(prev).ToBeEnabledAsync();

        await prev.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        await ExpectFocused("slide", "Tab 1");
    }

    [TestMethod]
    public async Task BitPivot_ClickOnTheHeadersOwnPadding_FocusesNeitherTheTablistNorATab()
    {
        await Open();

        var before = Probe("manual").Locator(".before");
        await before.FocusAsync();

        // The tablist carries tabindex="-1" to stay out of the tab order, which would also let a click on its own
        // padding focus it - a focus that belongs to no tab, and that the keyboard handler of the header would then
        // act on. The header is wider than its five tabs, so the press lands past the last of them, on the header.
        var header = Probe("manual").Locator("[role=tablist]");
        var box = await header.BoundingBoxAsync() ?? throw new AssertFailedException("The header has no box.");
        var lastTab = await Tab("manual", "Tab 5").BoundingBoxAsync() ?? throw new AssertFailedException("Tab 5 has no box.");
        Assert.IsTrue(lastTab.X + lastTab.Width < box.X + box.Width - 20, "The header has padding past its last tab.");

        await header.ClickAsync(new() { Position = new() { X = box.Width - 8, Y = box.Height / 2 } });

        await Expect(header).Not.ToBeFocusedAsync();
        await Expect(Probe("manual").Locator("[role=tab]:focus")).ToHaveCountAsync(0);
        await Expect(before).ToBeFocusedAsync();

        // Nothing changed hands: the keys of the header do not act on a focus that is not on a tab.
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectSelected("manual", "Tab 1", "1");
        await Expect(before).ToBeFocusedAsync();
    }

    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready");

        // The overflow is measured by the browser half, which is what says it is set up.
        await Expect(Probe("menu").Locator(".bit-pvt-mor")).ToBeVisibleAsync();
    }

    private ILocator Probe(string probe) => Page.Locator($"[data-probe='{probe}']");

    private ILocator Tab(string probe, string text) => Probe(probe).Locator("[role=tab]", new() { HasText = text });

    // The keyboard reaches the header the way a reader's does: from the control before it, past the previous
    // button of a sliding header while that button is enabled.
    private async Task TabIntoTheHeader(string probe)
    {
        await Probe(probe).Locator(".before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        if (await Probe(probe).Locator(".bit-pvt-spv:focus").CountAsync() > 0)
        {
            await Page.Keyboard.PressAsync("Tab");
        }

        await Expect(Probe(probe).Locator("[role=tab]:focus")).ToHaveCountAsync(1);
    }

    // The focused tab is the one tab stop of the header.
    private async Task ExpectFocused(string probe, string text)
    {
        var tab = Tab(probe, text);

        await Expect(tab).ToBeFocusedAsync();
        await Expect(tab).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Probe(probe).Locator("[role=tab][tabindex='0']")).ToHaveCountAsync(1);
    }

    // The selection as a screen reader is told it, as the panel shows it and as the page is told it.
    private async Task ExpectSelected(string probe, string text, string key)
    {
        await Expect(Tab(probe, text)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Probe(probe).Locator("[role=tab][aria-selected='true']")).ToHaveCountAsync(1);
        await Expect(Probe(probe).Locator("[role=tabpanel]")).ToHaveTextAsync($"Panel {key}");
        await Expect(Probe(probe).Locator(".report .selected")).ToHaveTextAsync(key);
    }
}
