using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the country list BitCountryPicker and BitPhoneInput share (BitCountryList): what only
/// a real engine does with it - the list relocated to the body and taken back, the keys the field's own listener
/// prevents ahead of its native click, the flag images it loads - for the cases that used to go wrong: a field
/// disabled under its open list, a list opened and closed through IsOpen, a list opened through IsOpen on a field that
/// cannot be used, a default country the list names its own way, a list naming a country twice and a country with no
/// flag. Every test runs on the engine BROWSER names (chromium, firefox or webkit).
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitCountryPickerBrowserTests
public class BitCountryPickerBrowserTests : PerformanceTestBase
{
    [TestMethod]
    public async Task CountryPicker_PicksTheCountryTheSearchFinds()
    {
        await GoToThePage();

        await Field("#basic").ClickAsync();
        await Expect(Callout("#basic")).ToBeVisibleAsync();
        await Expect(Search("#basic")).ToBeFocusedAsync();

        await Search("#basic").PressSequentiallyAsync("germ");
        await Search("#basic").PressAsync("Enter");

        await Expect(Callout("#basic")).ToBeHiddenAsync();
        await Expect(Page.Locator("#v-value")).ToHaveTextAsync("DE");
        await Expect(Field("#basic")).ToBeFocusedAsync();
        await Expect(Field("#basic").Locator(".bit-cnp-txt")).ToHaveTextAsync("Germany");
    }

    [TestMethod]
    public async Task CountryPicker_WalksTheListWithTheKeyboard()
    {
        await GoToThePage();

        await Field("#nosearch").FocusAsync();
        await Field("#nosearch").PressAsync("ArrowDown");

        await Expect(Callout("#nosearch")).ToBeVisibleAsync();

        // End lands on the last country, Home on the first, the arrows move one at a time, and Enter picks it.
        var rows = Callout("#nosearch").Locator(".bit-cnp-itm");

        await Field("#nosearch").PressAsync("End");
        await Expect(ActiveName("#nosearch")).ToHaveTextAsync(await Row("#nosearch", await rows.CountAsync() - 1).Locator(".bit-cnp-inm").TextContentAsync() ?? "");

        await Field("#nosearch").PressAsync("Home");
        await Expect(ActiveName("#nosearch")).ToHaveTextAsync(await Row("#nosearch", 0).Locator(".bit-cnp-inm").TextContentAsync() ?? "");

        await Field("#nosearch").PressAsync("ArrowDown");
        var second = await Row("#nosearch", 1).Locator(".bit-cnp-inm").TextContentAsync();
        await Expect(ActiveName("#nosearch")).ToHaveTextAsync(second ?? "");

        // A letter jumps to the first country whose name it starts.
        await Field("#nosearch").PressAsync("g");
        await Expect(ActiveName("#nosearch")).ToHaveTextAsync(new Regex("^G"));
        var picked = await ActiveName("#nosearch").TextContentAsync();

        await Field("#nosearch").PressAsync("Enter");

        await Expect(Callout("#nosearch")).ToBeHiddenAsync();
        await Expect(Field("#nosearch").Locator(".bit-cnp-txt")).ToHaveTextAsync(picked ?? "");
    }

    /// <summary>
    /// A form disabled for its submit while the list is open used to leave the relocated list on screen with
    /// nothing that could close it: the close never reached the JS side.
    /// </summary>
    [TestMethod]
    public async Task CountryPicker_DisabledWhileOpen_TakesItsListOffTheScreen()
    {
        await GoToThePage();

        await Field("#bound").ClickAsync();
        await Expect(Callout("#bound")).ToBeVisibleAsync();
        await Expect(Page.Locator("#v-open")).ToHaveTextAsync("true");

        // The overlay of the open list takes the click, so the toggle is pressed from script, the way a submit
        // handler would disable the form.
        await Page.EvaluateAsync("document.getElementById('btn-disable').click()");

        await Expect(Page.Locator("#v-disabled")).ToHaveTextAsync("true");
        await Expect(Page.Locator("#v-open")).ToHaveTextAsync("false");
        await Expect(Callout("#bound")).ToBeHiddenAsync();
        await Expect(Overlay("#bound")).ToBeHiddenAsync();

        // Nothing is left covering the page.
        await Page.Locator("#btn-hit").ClickAsync();
        await Expect(Page.Locator("#v-hits")).ToHaveTextAsync("1");

        await Page.Locator("#btn-disable").ClickAsync();
        await Field("#bound").ClickAsync();

        await Expect(Callout("#bound")).ToBeVisibleAsync();
    }

    /// <summary>
    /// An IsOpen pushed on a disabled field used to cover the page with a transparent overlay and no list.
    /// </summary>
    [TestMethod]
    public async Task CountryPicker_OpenedThroughIsOpenWhileDisabled_StaysClosed()
    {
        await GoToThePage();

        await Page.Locator("#btn-disable").ClickAsync();
        await Expect(Page.Locator("#v-disabled")).ToHaveTextAsync("true");

        await Page.Locator("#btn-open").ClickAsync();

        await Expect(Page.Locator("#v-open")).ToHaveTextAsync("false");
        await Expect(Callout("#bound")).ToBeHiddenAsync();
        await Expect(Overlay("#bound")).ToBeHiddenAsync();

        await Page.Locator("#btn-hit").ClickAsync();
        await Expect(Page.Locator("#v-hits")).ToHaveTextAsync("1");
    }

    /// <summary>
    /// A select-only list opened through IsOpen used to keep the keys of a closed field: Enter picked a country and
    /// its native click opened the list again straight after, and PageDown scrolled the page as well as the list.
    /// </summary>
    [TestMethod]
    public async Task CountryPicker_OpenedThroughIsOpen_AnswersTheKeysOfAnOpenList()
    {
        await GoToThePage();

        await Page.Locator("#btn-open-nosearch").ClickAsync();

        await Expect(Callout("#nosearch")).ToBeVisibleAsync();
        await Expect(Field("#nosearch")).ToBeFocusedAsync();

        var scrollY = await Page.EvaluateAsync<double>("window.scrollY");
        await Field("#nosearch").PressAsync("PageDown");
        await Expect(ActiveName("#nosearch")).ToHaveTextAsync(await Row("#nosearch", 10).Locator(".bit-cnp-inm").TextContentAsync() ?? "");
        Assert.AreEqual(scrollY, await Page.EvaluateAsync<double>("window.scrollY"), "PageDown scrolled the page under the open list.");

        await Field("#nosearch").PressAsync("Enter");

        await Expect(Page.Locator("#v-nosearch-value")).Not.ToBeEmptyAsync();
        await Expect(Page.Locator("#v-nosearch-open")).ToHaveTextAsync("false");

        // Long enough for a native click that slipped through to have opened the list again.
        await Page.WaitForTimeoutAsync(300);
        await Expect(Callout("#nosearch")).ToBeHiddenAsync();
        await Expect(Page.Locator("#v-nosearch-open")).ToHaveTextAsync("false");
    }

    /// <summary>
    /// A list opened by hand and closed through IsOpen used to keep the keys of an open one, so Enter and Space no
    /// longer opened the field.
    /// </summary>
    [TestMethod]
    public async Task CountryPicker_ClosedThroughIsOpen_OpensAgainOnEnter()
    {
        await GoToThePage();

        await Field("#nosearch").ClickAsync();
        await Expect(Callout("#nosearch")).ToBeVisibleAsync();

        await Page.EvaluateAsync("document.getElementById('btn-close-nosearch').click()");
        await Expect(Callout("#nosearch")).ToBeHiddenAsync();

        await Field("#nosearch").FocusAsync();
        await Field("#nosearch").PressAsync("Enter");

        await Expect(Callout("#nosearch")).ToBeVisibleAsync();
        await Expect(Page.Locator("#v-nosearch-open")).ToHaveTextAsync("true");

        await Field("#nosearch").PressAsync("Escape");
        await Expect(Callout("#nosearch")).ToBeHiddenAsync();

        await Field("#nosearch").PressAsync(" ");
        await Expect(Callout("#nosearch")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task CountryPicker_StartsOnTheDefaultCountryAsItsListNamesIt()
    {
        await GoToThePage();

        await Expect(Field("#localized").Locator(".bit-cnp-txt")).ToHaveTextAsync("Deutschland");

        await Field("#localized").ClickAsync();

        await Expect(Callout("#localized").Locator(".bit-cnp-sel .bit-cnp-inm")).ToHaveTextAsync("Deutschland");
    }

    [TestMethod]
    public async Task CountryPicker_OffersACountryNamedTwiceOnlyOnce()
    {
        await GoToThePage();

        await Field("#dupes").ClickAsync();

        await Expect(Callout("#dupes").Locator(".bit-cnp-inm")).ToHaveTextAsync(new[] { "United States", "Canada" });

        // The render that used to throw over the duplicate keys took the circuit down with it.
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        await Page.Locator("#btn-hit").EvaluateAsync("b => b.click()");
        await Expect(Page.Locator("#v-hits")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task CountryPicker_DrawsAnEmptyBoxForACountryWithNoFlag()
    {
        await GoToThePage();

        var flag = Field("#noflag").Locator(".bit-cnp-flg");
        await Expect(flag).ToHaveCountAsync(1);
        Assert.AreEqual("SPAN", await flag.EvaluateAsync<string>("e => e.tagName"));

        // The box keeps the size of a flag, so the name lines up with the names of the countries that have one.
        var box = await flag.BoundingBoxAsync();
        Assert.IsNotNull(box);
        Assert.IsTrue(box.Width > 0 && box.Height > 0, $"The empty flag box is {box.Width}x{box.Height}.");

        await Field("#noflag").ClickAsync();

        var rows = Callout("#noflag").Locator(".bit-cnp-itm");
        await Expect(rows).ToHaveCountAsync(3);
        Assert.AreEqual("SPAN", await rows.Nth(0).Locator(".bit-cnp-flg").EvaluateAsync<string>("e => e.tagName"));

        // Every image the list asks for is one that loads.
        var img = rows.Nth(1).Locator("img.bit-cnp-flg");
        await img.ScrollIntoViewIfNeededAsync();
        await Page.WaitForFunctionAsync("i => i.complete", await img.ElementHandleAsync());
        Assert.IsTrue(await img.EvaluateAsync<bool>("i => i.naturalWidth > 0"), "The packaged flag of France failed to load.");
    }

    [TestMethod]
    public async Task CountryPicker_ReportsOneFocusAndOneBlur()
    {
        await GoToThePage();

        await Field("#basic").FocusAsync();
        await Expect(Root("#basic")).ToHaveClassAsync(new Regex("bit-cnp-fcs"));
        await Expect(Page.Locator("#v-focus")).ToHaveTextAsync("1");

        await Page.Locator("#btn-hit").FocusAsync();
        await Expect(Root("#basic")).Not.ToHaveClassAsync(new Regex("bit-cnp-fcs"));
        await Expect(Page.Locator("#v-blur")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#v-focus")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task PhoneInput_DisabledWhileOpen_TakesItsListOffTheScreen()
    {
        await GoToThePage();

        await Page.Locator("#phone .bit-phi-drp").ClickAsync();

        await Expect(Callout("#phone")).ToBeVisibleAsync();
        await Expect(Page.Locator("#v-phone-open")).ToHaveTextAsync("true");

        await Page.EvaluateAsync("document.getElementById('btn-phone-disable').click()");

        await Expect(Page.Locator("#v-phone-open")).ToHaveTextAsync("false");
        await Expect(Callout("#phone")).ToBeHiddenAsync();
        await Expect(Overlay("#phone")).ToBeHiddenAsync();

        await Page.Locator("#btn-hit").ClickAsync();
        await Expect(Page.Locator("#v-hits")).ToHaveTextAsync("1");
    }



    private async Task GoToThePage()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/country-picker");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive
    }

    private ILocator Root(string wrapper) => Page.Locator($"{wrapper} .bit-cnp");

    private ILocator Field(string wrapper) => Page.Locator($"{wrapper} button.bit-cnp-fld");

    // The callout and the overlay are moved to the body while the list is open, so they are found by the class the
    // page gives each case's rather than under the wrapper.
    private ILocator Callout(string wrapper) => Page.Locator($".{wrapper.TrimStart('#')}-cal");

    private ILocator Overlay(string wrapper) => Page.Locator($".{wrapper.TrimStart('#')}-ovl");

    private ILocator Search(string wrapper) => Callout(wrapper).Locator(".bit-cnp-srch");

    private ILocator ActiveName(string wrapper) => Callout(wrapper).Locator(".bit-cnp-act .bit-cnp-inm");

    private ILocator Row(string wrapper, int index) => Callout(wrapper).Locator(".bit-cnp-itm").Nth(index);
}
