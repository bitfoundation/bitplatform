using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the responsive panel of the country list (BitCountryPicker, BitPhoneInput) on a page
/// that reads right to left without giving the component a Dir of its own. The panel used to be mirrored through the
/// class an explicit Dir renders alone, so on such a page it was pinned to and slid in from the physical right; it
/// reads the direction it is laid out in through <c>:dir(rtl)</c> now, and has to sit against the left edge.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitCountryPickerResponsiveBrowserTests
public class BitCountryPickerResponsiveBrowserTests : PerformanceTestBase
{
    private const int ViewportWidth = 400;

    // Below the 600px the panel takes over from the inline callout at.
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = ViewportWidth, Height = 800 }
    };

    [TestMethod]
    public async Task CountryPicker_OnARightToLeftPage_OpensItsPanelAgainstTheLeftEdge()
    {
        await GoToThePage();

        await Page.Locator("#rtl button.bit-cnp-fld").ClickAsync();

        await AssertThePanelSitsOnTheLeft(".rtl-cal");
    }

    [TestMethod]
    public async Task PhoneInput_OnARightToLeftPage_OpensItsPanelAgainstTheLeftEdge()
    {
        await GoToThePage();

        await Page.Locator("#rtl button.bit-phi-drp").ClickAsync();

        await AssertThePanelSitsOnTheLeft(".rtlphone-cal");
    }

    private async Task GoToThePage()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/country-picker");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive
    }

    private async Task AssertThePanelSitsOnTheLeft(string selector)
    {
        var panel = Page.Locator(selector);

        await Expect(panel).ToBeVisibleAsync();

        // The panel slides in, so its place is read once the slide is over.
        await Page.WaitForFunctionAsync(
            "sel => { const p = document.querySelector(sel); return getComputedStyle(p).opacity === '1' && p.getAnimations().length === 0; }",
            selector,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var box = await panel.BoundingBoxAsync();

        Assert.IsNotNull(box);
        Assert.IsTrue(box.Width < ViewportWidth, $"The panel is {box.Width}px wide, the whole width of the screen.");
        Assert.AreEqual(0, box.X, 1, $"The panel starts at {box.X}px rather than against the left edge.");
        Assert.AreEqual("rtl", await panel.EvaluateAsync<string>("p => getComputedStyle(p).direction"));
    }
}
