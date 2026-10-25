using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the keyboard handling an open callout hands over to the JS side in one call -
/// BitDropMenu through Callouts.setupMenuKeyboard, BitCallout through Callouts.setupKeyboard - driven with real
/// keyboard and mouse input on a server-rendered page, where every call is a round trip.
///
/// The Escape, the Tab key kept inside the callout or handed back to the page after its trigger, the watch on the
/// focus going into a callout the pointer opened, the switches between them while it is open, and the focus moved in
/// last are all the script's, which bUnit cannot run - so the behavior is pinned down here, in every engine
/// (BROWSER=chromium, firefox, webkit).
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitCalloutKeyboardHandoverBrowserTests
public class BitCalloutKeyboardHandoverBrowserTests : PerformanceTestBase
{
    private static readonly HttpClient _http = new();
    private readonly List<string> _pageErrors = [];

    private ILocator MenuButton => Page.Locator("#menu .bit-drm-btn");
    private ILocator OpenState => Page.Locator("#open-state");

    [TestMethod]
    public async Task DropMenu_OpenedByAClick_TakesTheTabIntoItAndHandsItBackToThePageAfterTheButton()
    {
        await OpenPage("menu");

        await MenuButton.ClickAsync();
        await Expect(OpenState).ToHaveTextAsync("open");
        await Settle();

        // A click leaves the focus on the button, and the content reads as if it sat right after it.
        await Expect(MenuButton).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#item2")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after")).ToBeFocusedAsync();
        await Expect(OpenState).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task DropMenu_OpenedByTheArrowKey_MovesTheFocusInAndEscapeHandsItBack()
    {
        await OpenPage("menu");

        await FocusTheButtonFromThePage();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(OpenState).ToHaveTextAsync("open");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(OpenState).ToHaveTextAsync("closed");
        await Expect(MenuButton).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task DropMenu_WithAutoFocus_MovesTheFocusInOnAClick()
    {
        await OpenPage("menu", "&autofocus=true");

        await MenuButton.ClickAsync();

        await Expect(OpenState).ToHaveTextAsync("open");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task DropMenu_WithTrapFocus_KeepsTheTabInsideAndEscapeHandsItBack()
    {
        await OpenPage("menu", "&trap=true");

        await FocusTheButtonFromThePage();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(OpenState).ToHaveTextAsync("open");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#item2")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Page.Locator("#item2")).ToBeFocusedAsync();
        await Expect(OpenState).ToHaveTextAsync("open");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(OpenState).ToHaveTextAsync("closed");
        await Expect(MenuButton).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task DropMenu_SwitchedBetweenTheTrapAndTheTabOutWhileOpen_FollowsTheSwitch()
    {
        await OpenPage("menu");

        await FocusTheButtonFromThePage();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();

        await ToggleTrapFocus("on");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#item2")).ToBeFocusedAsync();

        // The tab-out the trap replaced is gone: the Tab wraps rather than leaving for the page.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();
        await Expect(OpenState).ToHaveTextAsync("open");

        await ToggleTrapFocus("off");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#item2")).ToBeFocusedAsync();

        // And the trap the tab-out replaced is gone too.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after")).ToBeFocusedAsync();
        await Expect(OpenState).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task DropMenu_OpenedByHovering_LeavesTheFocusAloneAndClosesOnAnEscapePressedElsewhere()
    {
        await OpenPage("menu", "&hover=true");

        await Page.Locator("#before").FocusAsync();
        await Page.Keyboard.TypeAsync("abc");

        await Page.Locator("#menu").HoverAsync();
        await Expect(OpenState).ToHaveTextAsync("open");
        await Settle();

        // The pointer passing over the button does not pull the focus out of what the user is typing into.
        await Expect(Page.Locator("#before")).ToBeFocusedAsync();

        // Content shown on hover is dismissible without moving the pointer or the focus (WCAG 1.4.13).
        await Page.Keyboard.PressAsync("Escape");
        await Expect(OpenState).ToHaveTextAsync("closed");
        await Expect(Page.Locator("#before")).ToBeFocusedAsync();
        await Expect(Page.Locator("#before")).ToHaveValueAsync("abc");
    }

    [TestMethod]
    public async Task DropMenu_OpenedByHovering_LetsATabOnTheButtonMoveOnThroughThePage()
    {
        await OpenPage("menu", "&hover=true");

        await FocusTheButtonFromThePage();

        await Page.Locator("#menu").HoverAsync();
        await Expect(OpenState).ToHaveTextAsync("open");
        await Settle();

        await Page.Keyboard.PressAsync("Tab");

        await Expect(Page.Locator("#after")).ToBeFocusedAsync();

        // The pointer is still on it, so it stays (WCAG 1.4.13).
        await Expect(OpenState).ToHaveTextAsync("open");
    }

    [TestMethod]
    public async Task DropMenu_OpenedByHovering_IsTakenOverByTheArrowKeyWithItsFocusAndItsEscape()
    {
        await OpenPage("menu", "&hover=true");

        await FocusTheButtonFromThePage();

        await Page.Locator("#menu").HoverAsync();
        await Expect(OpenState).ToHaveTextAsync("open");
        await Settle();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();

        // The keyboard's now: the pointer leaving no longer closes it.
        await Page.Mouse.MoveAsync(1, 1);
        await Page.WaitForTimeoutAsync(800);
        await Expect(OpenState).ToHaveTextAsync("open");

        // And its Tab key is handled as for one the keyboard opened.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#item2")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(OpenState).ToHaveTextAsync("closed");
        await Expect(MenuButton).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task DropMenu_OpenedByHovering_IsTakenOverByTheFocusGoingIn()
    {
        await OpenPage("menu", "&hover=true");

        await Page.Locator("#menu").HoverAsync();
        await Expect(OpenState).ToHaveTextAsync("open");
        await Settle();

        // A script focusing something in it is not the pointer.
        await Page.EvaluateAsync("document.getElementById('item1').focus()");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();
        await Settle();

        await Page.Mouse.MoveAsync(1, 1);
        await Page.WaitForTimeoutAsync(800);
        await Expect(OpenState).ToHaveTextAsync("open");

        // Its Escape is now only the one pressed inside it.
        await Page.Locator("#before").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(300);
        await Expect(OpenState).ToHaveTextAsync("open");

        await Page.Locator("#item1").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(OpenState).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task DropMenu_ClosedWhileTheKeyboardIsBeingHandedOver_LeavesNothingBehind()
    {
        await OpenPage("menu", "&hover=true");

        await FocusTheButtonFromThePage();

        await Page.Locator("#menu").HoverAsync();
        await Expect(OpenState).ToHaveTextAsync("open");
        await Settle();

        // The Escape follows the arrow key before its round trip is back.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(OpenState).ToHaveTextAsync("closed");
        await Settle();

        // Nothing of the handover is left on the closed callout: the focus is not in it, and the Tab on the button
        // moves on through the page rather than into it.
        Assert.IsFalse(await Page.EvaluateAsync<bool>(
            "() => document.getElementById(document.querySelector('#menu .bit-drm-btn').getAttribute('aria-controls')).contains(document.activeElement)"));

        await MenuButton.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after")).ToBeFocusedAsync();

        // And the next opening starts over: one opened by hovering closes on an Escape pressed anywhere again.
        await Page.Mouse.MoveAsync(1, 1);
        await Page.Locator("#menu").HoverAsync();
        await Expect(OpenState).ToHaveTextAsync("open");
        await Settle();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(OpenState).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task Callout_OpenedFromItsAnchor_TakesTheTabIntoItAndEscapeHandsItBack()
    {
        await OpenPage("callout");

        await Page.Locator("#callout-anchor").ClickAsync();
        await Expect(OpenState).ToHaveTextAsync("open");
        await Settle();

        await Page.Locator("#callout-anchor").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(OpenState).ToHaveTextAsync("closed");
        await Expect(Page.Locator("#callout-anchor")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Callout_WithTrapFocus_KeepsTheTabInsideAndEscapeHandsItBack()
    {
        await OpenPage("callout", "&trap=true");

        await Page.Locator("#callout-anchor").ClickAsync();
        await Expect(OpenState).ToHaveTextAsync("open");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#item2")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#item1")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(OpenState).ToHaveTextAsync("closed");
        await Expect(Page.Locator("#callout-anchor")).ToBeFocusedAsync();
    }

    // A handover the JS side failed - a registration that threw, a callback into a disposed component - shows on the
    // page only as an error in the console or a circuit the server tore down, so every test ends by reading both.
    [TestCleanup]
    public async Task AssertNoErrors()
    {
        var errors = _pageErrors.Where(e => e.Contains("favicon", System.StringComparison.OrdinalIgnoreCase) is false).ToList();

        Assert.AreEqual(0, errors.Count, $"the page reported: {string.Join(" || ", errors)}");

        var json = await _http.GetStringAsync($"{BaseUrl}/api/server-errors");
        var serverErrors = JsonSerializer.Deserialize<string[]>(json) ?? [];

        Assert.AreEqual(0, serverErrors.Length, $"the server logged: {string.Join(" || ", serverErrors)}");
    }

    private async Task OpenPage(string caseName, string query = "")
    {
        await _http.DeleteAsync($"{BaseUrl}/api/server-errors");

        await Page.GotoAsync($"{BaseUrl}/regression/callout-keyboard-handover?case={caseName}{query}");
        await WaitForStatus("Ready");

        // Listened to only once this page's circuit is up: leaving the page the test started on aborts that page's
        // own negotiation with the server, which Firefox reports as a NetworkError of the page that is left.
        Page.Console += (_, message) =>
        {
            if (message.Type == "error") _pageErrors.Add(message.Text);
        };
        Page.PageError += (_, error) => _pageErrors.Add(error);

        await Settle();
    }

    // The button reached the way the keyboard reaches it, which is what leaves it focused in every engine - WebKit
    // focuses no button on a click.
    private async Task FocusTheButtonFromThePage()
    {
        await Page.Locator("#before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(MenuButton).ToBeFocusedAsync();
    }

    // The overlay of the open callout takes every press of the pointer, so the switch is clicked from a script.
    private async Task ToggleTrapFocus(string expected)
    {
        await Page.EvaluateAsync("document.getElementById('toggle-trap').click()");
        await Expect(Page.Locator("#trap-state")).ToHaveTextAsync(expected);
        await Settle();
    }

    // The components start listening in calls to the browser of their own once they have rendered, which nothing on
    // the page reports; a key pressed before they land is a key nothing was listening for yet.
    private Task Settle() => Page.WaitForTimeoutAsync(500);
}
