using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for BitTimeZonePicker: what only a real engine does with it - the list relocated to the body
/// and taken back, the keys the field's own listener prevents ahead of its native click, the time zone the browser
/// reports - and the offsets read off the time zone data of the server it runs on, which is where the ids of the
/// table and the ids of the runtime meet. Every test runs on the engine BROWSER names (chromium, firefox or webkit).
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitTimeZonePickerBrowserTests
public class BitTimeZonePickerBrowserTests : PerformanceTestBase
{
    [TestMethod]
    public async Task TimeZonePicker_PicksTheTimeZoneTheSearchFinds()
    {
        await GoToThePage();

        await Field("#basic").ClickAsync();
        await Expect(Callout("#basic")).ToBeVisibleAsync();
        await Expect(Search("#basic")).ToBeFocusedAsync();

        await Search("#basic").PressSequentiallyAsync("tehr");
        await Search("#basic").PressAsync("Enter");

        await Expect(Callout("#basic")).ToBeHiddenAsync();
        await Expect(Page.Locator("#v-value")).ToHaveTextAsync("Asia/Tehran");
        await Expect(Field("#basic")).ToBeFocusedAsync();
        await Expect(Field("#basic").Locator(".bit-tzp-txt")).ToHaveTextAsync("Tehran");
        await Expect(Field("#basic").Locator(".bit-tzp-ofs")).ToHaveTextAsync("UTC+03:30");
    }

    [TestMethod]
    public async Task TimeZonePicker_FindsTheTimeZonesOfAnOffset()
    {
        await GoToThePage();

        await Field("#basic").ClickAsync();
        await Expect(Search("#basic")).ToBeFocusedAsync();
        await Search("#basic").PressSequentiallyAsync("+5:45");

        await Expect(Callout("#basic").Locator(".bit-tzp-inm")).ToHaveTextAsync(new[] { "Kathmandu" });
    }

    [TestMethod]
    public async Task TimeZonePicker_BindsATimeZoneInfoOfTheServer()
    {
        await GoToThePage();

        await Field("#info").ClickAsync();
        await Expect(Search("#info")).ToBeFocusedAsync();
        await Search("#info").PressSequentiallyAsync("Asia/Calcutta");
        await Search("#info").PressAsync("Enter");

        // The id a browser built on CLDR still reports finds the zone under its canonical id, which the TimeZoneInfo of
        // the server stands for.
        await Expect(Page.Locator("#v-info")).ToHaveTextAsync("Asia/Kolkata");
        await Expect(Field("#info").Locator(".bit-tzp-ofs")).ToHaveTextAsync("UTC+05:30");
    }

    [TestMethod]
    public async Task TimeZonePicker_WalksTheListWithTheKeyboard()
    {
        await GoToThePage();

        await Field("#nosearch").FocusAsync();
        await Field("#nosearch").PressAsync("ArrowDown");

        await Expect(Callout("#nosearch")).ToBeVisibleAsync();

        // West to east: New York, London, Tehran, Tokyo.
        await Expect(Callout("#nosearch").Locator(".bit-tzp-inm")).ToHaveTextAsync(new[] { "New York", "London", "Tehran", "Tokyo" });

        await Field("#nosearch").PressAsync("End");
        await Expect(ActiveName("#nosearch")).ToHaveTextAsync("Tokyo");

        await Field("#nosearch").PressAsync("Home");
        await Expect(ActiveName("#nosearch")).ToHaveTextAsync("New York");

        await Field("#nosearch").PressAsync("ArrowDown");
        await Expect(ActiveName("#nosearch")).ToHaveTextAsync("London");

        // A letter jumps to the first time zone whose name it starts.
        await Field("#nosearch").PressAsync("t");
        await Expect(ActiveName("#nosearch")).ToHaveTextAsync("Tehran");

        await Field("#nosearch").PressAsync("Enter");

        await Expect(Callout("#nosearch")).ToBeHiddenAsync();
        await Expect(Page.Locator("#v-nosearch-value")).ToHaveTextAsync("Asia/Tehran");

        // Long enough for a native click that slipped through to have opened the list again.
        await Page.WaitForTimeoutAsync(300);
        await Expect(Callout("#nosearch")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task TimeZonePicker_DisabledWhileOpen_TakesItsListOffTheScreen()
    {
        await GoToThePage();

        await Field("#bound").ClickAsync();
        await Expect(Callout("#bound")).ToBeVisibleAsync();
        await Expect(Page.Locator("#v-open")).ToHaveTextAsync("true");

        // The overlay of the open list takes the click, so the toggle is pressed from script, the way a submit handler
        // would disable the form.
        await Page.EvaluateAsync("document.getElementById('btn-disable').click()");

        await Expect(Page.Locator("#v-disabled")).ToHaveTextAsync("true");
        await Expect(Page.Locator("#v-open")).ToHaveTextAsync("false");
        await Expect(Callout("#bound")).ToBeHiddenAsync();
        await Expect(Overlay("#bound")).ToBeHiddenAsync();

        await Page.Locator("#btn-hit").ClickAsync();
        await Expect(Page.Locator("#v-hits")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task TimeZonePicker_DetectsTheTimeZoneOfTheBrowser()
    {
        await GoToThePage();

        var browserTimeZone = await Page.EvaluateAsync<string>("Intl.DateTimeFormat().resolvedOptions().timeZone");

        // The field starts on the time zone the browser runs in, under its canonical id - which is the browser's own,
        // unless the browser still reports one IANA has since renamed (Asia/Calcutta).
        await Expect(Page.Locator("#v-detected")).Not.ToBeEmptyAsync();
        var detected = await Page.Locator("#v-detected").TextContentAsync();

        Assert.IsTrue(detected == browserTimeZone || browserTimeZone is "Asia/Calcutta" or "Europe/Kiev" or "Asia/Saigon" or "Asia/Rangoon" or "Asia/Katmandu",
                      $"The browser runs in {browserTimeZone}, the field detected {detected}.");
        await Expect(Field("#detect").Locator(".bit-tzp-txt")).ToHaveTextAsync(new Regex(".+"));
    }



    private async Task GoToThePage()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/time-zone-picker");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive
    }

    private ILocator Field(string wrapper) => Page.Locator($"{wrapper} button.bit-tzp-fld");

    // The callout and the overlay are moved to the body while the list is open, so they are found by the class the page
    // gives each case's rather than under the wrapper.
    private ILocator Callout(string wrapper) => Page.Locator($".{wrapper.TrimStart('#')}-cal");

    private ILocator Overlay(string wrapper) => Page.Locator($".{wrapper.TrimStart('#')}-ovl");

    private ILocator Search(string wrapper) => Callout(wrapper).Locator(".bit-tzp-srch");

    private ILocator ActiveName(string wrapper) => Callout(wrapper).Locator(".bit-tzp-act .bit-tzp-inm");
}
