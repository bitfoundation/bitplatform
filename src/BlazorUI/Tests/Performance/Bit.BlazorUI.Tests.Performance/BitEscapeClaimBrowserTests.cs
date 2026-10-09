using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the Escape claim (Utils.claimEscape), driven with real keyboard input.
///
/// A component that acts on Escape in its .NET handler claims the key in its markup (data-bit-esc), and every surface
/// that closes on Escape leaves a claimed key alone - so one press does one thing: a field inside a dialog clears, and
/// the dialog closes on the next press. A press the component does not act on (a one-way binding it cannot clear, an
/// Escape with a modifier) goes straight to the surface. bUnit cannot see any of it - the claim is read by a script,
/// off the live DOM, as the key goes down - so the behavior is pinned down here, in every engine (BROWSER=chromium,
/// firefox, webkit) and for every surface.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitEscapeClaimBrowserTests
public class BitEscapeClaimBrowserTests : PerformanceTestBase
{
    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task TextField_ClearOnEscape_ClearsFirstAndTheSurfaceClosesOnTheNextPress(string surface)
    {
        await OpenSurface(surface);

        await FocusAndType("#w-tf input", "abc");
        await Expect(Page.Locator("#v-tf")).ToHaveTextAsync("abc");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#v-tf")).ToHaveTextAsync("");

        await PressEscapeAndExpectClosed();
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task TextField_ClearOnEscape_OneWayBinding_LeavesTheKeyToTheSurface(string surface)
    {
        await OpenSurface(surface);

        await Page.Locator("#w-tf1 input").FocusAsync();

        await PressEscapeAndExpectClosed();
        await Expect(Page.Locator("#clears")).ToHaveTextAsync("0");
    }

    [TestMethod, DataRow("dialog", "Shift+Escape"), DataRow("modal", "Control+Escape"), DataRow("panel", "Alt+Escape"), DataRow("overlay", "Shift+Escape")]
    public async Task TextField_ClearOnEscape_ModifiedEscape_ClosesTheSurfaceAndClearsNothing(string surface, string key)
    {
        await OpenSurface(surface);

        await FocusAndType("#w-tf input", "abc");
        await Expect(Page.Locator("#v-tf")).ToHaveTextAsync("abc");

        await Page.Keyboard.PressAsync(key);

        await Expect(Page.Locator("#surface-state")).ToHaveTextAsync("closed");
        await Expect(Page.Locator("#v-tf")).ToHaveTextAsync("abc");
        await Expect(Page.Locator("#clears")).ToHaveTextAsync("0");
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task SearchBox_ClearsFirstAndTheSurfaceClosesOnTheNextPress(string surface)
    {
        await OpenSurface(surface);

        await FocusAndType("#w-sb input", "abc");
        await Expect(Page.Locator("#v-sb")).ToHaveTextAsync("abc");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#v-sb")).ToHaveTextAsync("");

        await PressEscapeAndExpectClosed();
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task SearchBox_OneWayBinding_LeavesTheKeyToTheSurface(string surface)
    {
        await OpenSurface(surface);

        await Page.Locator("#w-sb1 input").FocusAsync();

        await PressEscapeAndExpectClosed();
        await Expect(Page.Locator("#clears")).ToHaveTextAsync("0");
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task NumberField_ClearsFirstAndTheSurfaceClosesOnTheNextPress(string surface)
    {
        await OpenSurface(surface);

        await Page.Locator("#w-nf input").FocusAsync();
        await Expect(Page.Locator("#v-nf")).ToHaveTextAsync("5");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#v-nf")).ToHaveTextAsync("");

        await PressEscapeAndExpectClosed();
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task TagsInput_TakesBackTheTextThenTheTagsAndTheSurfaceClosesOnTheNextPress(string surface)
    {
        await OpenSurface(surface);

        await FocusAndType("#w-tgi input.bit-tgi-inp", "zz");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#w-tgi input.bit-tgi-inp")).ToHaveValueAsync("");
        await Expect(Page.Locator("#v-tgi")).ToHaveTextAsync("2");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#v-tgi")).ToHaveTextAsync("0");

        await PressEscapeAndExpectClosed();
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task TagsInput_OneWayBinding_LeavesTheKeyToTheSurface(string surface)
    {
        await OpenSurface(surface);

        await Page.Locator("#w-tgi1 input.bit-tgi-inp").FocusAsync();

        await PressEscapeAndExpectClosed();
        await Expect(Page.Locator("#clears")).ToHaveTextAsync("0");
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task Dropdown_ClearOnEscape_ClearsFirstAndTheSurfaceClosesOnTheNextPress(string surface)
    {
        await OpenSurface(surface);

        await Page.Locator("#w-drp .bit-drp-wrp").FocusAsync();
        await Expect(Page.Locator("#v-drp")).ToHaveTextAsync("a");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#v-drp")).ToHaveTextAsync("");

        await PressEscapeAndExpectClosed();
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task Dropdown_ClosesItsListFirstAndKeepsTheSelection(string surface)
    {
        await OpenSurface(surface);

        await Page.Locator("#w-drp .bit-drp-wrp").FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Expect(Page.Locator("#w-drp .bit-drp-wrp")).ToHaveAttributeAsync("aria-expanded", "true");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#w-drp .bit-drp-wrp")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#v-drp")).ToHaveTextAsync("a");
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task Dropdown_ClearOnEscape_OneWayBinding_LeavesTheKeyToTheSurface(string surface)
    {
        await OpenSurface(surface);

        await Page.Locator("#w-drp1 .bit-drp-wrp").FocusAsync();

        await PressEscapeAndExpectClosed();
        await Expect(Page.Locator("#clears")).ToHaveTextAsync("0");
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task Dropdown_ModifiedEscape_ClosesTheSurfaceAndKeepsTheSelection(string surface)
    {
        await OpenSurface(surface);

        await Page.Locator("#w-drp .bit-drp-wrp").FocusAsync();

        await Page.Keyboard.PressAsync("Shift+Escape");

        await Expect(Page.Locator("#surface-state")).ToHaveTextAsync("closed");
        await Expect(Page.Locator("#v-drp")).ToHaveTextAsync("a");
        await Expect(Page.Locator("#clears")).ToHaveTextAsync("0");
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task Combo_DropsTheTermThenTheSelectionAndTheSurfaceClosesOnTheNextPress(string surface)
    {
        await OpenSurface(surface);

        var input = Page.Locator("#w-cmb input.bit-drp-inp");
        await input.FocusAsync();
        await Page.Keyboard.TypeAsync("zz");

        // The term opens the list, so the first press closes it and drops the term together.
        await PressEscapeAndExpectOpen();
        await Expect(input).ToHaveValueAsync("");
        await Expect(Page.Locator("#v-cmb")).ToHaveTextAsync("a");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#v-cmb")).ToHaveTextAsync("");

        await PressEscapeAndExpectClosed();
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task Message_DismissesFirstAndTheSurfaceClosesOnTheNextPress(string surface)
    {
        await OpenSurface(surface);

        await Page.Locator("#msg-btn").FocusAsync();

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#msg-state")).ToHaveTextAsync("dismissed");

        // The button went with the message, so the focus is put back inside the surface.
        await Page.Locator("#surface-btn").FocusAsync();

        await PressEscapeAndExpectClosed();
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel"), DataRow("overlay")]
    public async Task Message_AFieldInsideItClearsAndTheMessageStays(string surface)
    {
        await OpenSurface(surface);

        await FocusAndType("#w-mtf input", "abc");
        await Expect(Page.Locator("#v-mtf")).ToHaveTextAsync("abc");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#v-mtf")).ToHaveTextAsync("");
        await Expect(Page.Locator("#msg-state")).ToHaveTextAsync("shown");

        // The field holds nothing now, so the next press is the message's, and the one after it the surface's.
        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#msg-state")).ToHaveTextAsync("dismissed");
    }

    [TestMethod]
    public async Task Message_AModalInsideItClosesAndTheMessageStays()
    {
        await OpenPage();

        await Page.Locator("#btn-inner-modal").ClickAsync();
        await Expect(Page.Locator("#inner-modal-state")).ToHaveTextAsync("open");
        await Settle();

        await Page.Locator("#inner-modal-input").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#inner-modal-state")).ToHaveTextAsync("closed");

        // Give a wrongly routed press the time to have dismissed the message.
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#outer-msg-state")).ToHaveTextAsync("shown");
    }

    [TestMethod]
    public async Task Tooltip_LeavesAnEscapeAFieldClaimedToTheField()
    {
        await OpenPage();

        await FocusAndType("#w-ptf input", "abc");
        await Expect(Page.Locator("#v-ptf")).ToHaveTextAsync("abc");

        // Shown by the pointer while the focus stays in the field.
        await Page.Locator("#tooltip-anchor").HoverAsync();
        var tooltip = Page.Locator(".bit-ttp-wrp.bit-ttp-vis");
        await Expect(tooltip).ToHaveCountAsync(1);
        await Expect(Page.Locator("#w-ptf input")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#v-ptf")).ToHaveTextAsync("");
        await Expect(tooltip).ToHaveCountAsync(1);

        // The field is empty, so the next press is the tooltip's.
        await Page.Keyboard.PressAsync("Escape");

        await Expect(tooltip).ToHaveCountAsync(0);
    }

    private async Task OpenPage(string? surface = null)
    {
        await Page.GotoAsync($"{BaseUrl}/regression/escape-claim{(surface is null ? "" : $"?surface={surface}")}");
        await WaitForStatus("Ready");
        await Settle();
    }

    private async Task OpenSurface(string surface)
    {
        await OpenPage(surface);

        await Page.Locator("#btn-open").ClickAsync();
        await WaitForStatus("Open");
        await Expect(Page.Locator("#surface-content")).ToBeVisibleAsync();
        await Settle();
    }

    // The components start listening in calls to the browser of their own once they have rendered - a surface
    // for Escape once it is open, a field for its input once the circuit is up - which nothing on the page
    // reports; a key pressed before they land (WebKit drives the page fast enough to press one) is a key nothing
    // was listening for yet.
    private Task Settle() => Page.WaitForTimeoutAsync(500);

    // The text goes in as one input event rather than a keystroke at a time: a field bound on every keystroke over a
    // circuit can have the render of the first one land after the second is typed, and write the shorter text back
    // over it (Firefox does it often enough to fail a run) - which is Blazor's to answer, not what is tested here.
    private async Task FocusAndType(string selector, string text)
    {
        var input = Page.Locator(selector);
        await input.FillAsync(text);
        await Expect(input).ToBeFocusedAsync();
    }

    private async Task PressEscapeAndExpectOpen()
    {
        await Page.Keyboard.PressAsync("Escape");

        // Give a wrongly routed press the time to have closed the surface.
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#surface-state")).ToHaveTextAsync("open");
    }

    private async Task PressEscapeAndExpectClosed()
    {
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#surface-state")).ToHaveTextAsync("closed");
    }
}
