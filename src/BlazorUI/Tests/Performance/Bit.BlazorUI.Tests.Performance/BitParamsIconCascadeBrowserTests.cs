using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for an icon cascaded through a BitParams. A component takes one icon through several
/// parameters - an Icon, its IconName, an IconUrl, a template or a text drawn in its place, an icon that replaces it
/// in a state - and a params object supplies none of them to a component that picked its icon through any one, so
/// what the component asked for is what the page shows.
///
/// The page renders every component a params object hands an icon to, the cascaded glyph always Heart and the
/// component's own always Emoji2, and each case is read off the DOM the browser built: run them with BROWSER=firefox
/// and BROWSER=webkit as well.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitParamsIconCascadeBrowserTests
public class BitParamsIconCascadeBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/params-icon-cascade";

    private const string Cascaded = ".bit-icon--Heart";
    private const string Own = ".bit-icon--Emoji2";
    private const string CascadedTemplate = ".cascaded-template";
    private const string CascadedUrl = "img[src*='cascaded-icon.svg']";
    private const string DividerText = ".bit-brc-dtx";

    [TestMethod]
    [DataRow("button-cascaded", Cascaded, null)]
    [DataRow("button-own", Own, Cascaded)]
    [DataRow("button-own-vs-url", Own, CascadedUrl)]
    [DataRow("button-own-null", Cascaded, null)]
    [DataRow("action-button-cascaded", Cascaded, null)]
    [DataRow("action-button-own", Own, Cascaded)]
    [DataRow("menu-button-own", Own, Cascaded)]
    [DataRow("toggle-button-own-vs-on", Own, Cascaded)]
    [DataRow("toggle-button-off-state", Cascaded, Own)]
    [DataRow("checkbox-cascaded", Cascaded, null)]
    [DataRow("checkbox-own", Own, Cascaded)]
    [DataRow("toggle-own", Own, Cascaded)]
    [DataRow("search-box-own", Own, Cascaded)]
    [DataRow("number-field-own", Own, Cascaded)]
    [DataRow("text-field-own", Own, Cascaded)]
    [DataRow("text-field-clear", Own, CascadedTemplate)]
    [DataRow("text-field-reveal", Own, CascadedTemplate)]
    [DataRow("rating-own", Own, Cascaded)]
    [DataRow("calendar-own", Own, Cascaded)]
    [DataRow("date-picker-cascaded", CascadedTemplate, null)]
    [DataRow("date-picker-own", Own, CascadedTemplate)]
    [DataRow("date-range-picker-own", Own, CascadedTemplate)]
    [DataRow("time-picker-own", Own, CascadedTemplate)]
    [DataRow("circular-time-picker-own", Own, CascadedTemplate)]
    [DataRow("dropdown-own", Own, Cascaded)]
    [DataRow("breadcrumb-cascaded-text", DividerText, null)]
    [DataRow("breadcrumb-own-icon", Own, DividerText)]
    [DataRow("breadcrumb-own-text", DividerText, CascadedTemplate)]
    [DataRow("drop-menu-own", Own, Cascaded)]
    [DataRow("nav-own", Own, Cascaded)]
    [DataRow("pagination-own", Own, Cascaded)]
    [DataRow("badge-own", Own, Cascaded)]
    [DataRow("message-own", Own, Cascaded)]
    [DataRow("persona-own", Own, Cascaded)]
    [DataRow("persona-cascaded", Cascaded, null)]
    [DataRow("tag-own", Own, CascadedUrl)]
    [DataRow("accordion-own", Own, Cascaded)]
    [DataRow("accordion-collapsed-state", Cascaded, Own)]
    [DataRow("splitter-own", Own, CascadedTemplate)]
    [DataRow("link-own", Own, Cascaded)]
    [DataRow("accordion-list-own", Own, Cascaded)]
    [DataRow("error-boundary-own", Own, CascadedTemplate)]
    [DataRow("error-boundary-empty-name", ".bit-erb-svg", ".bit-erb-ico")]
    [DataRow("message-box-own", Own, Cascaded)]
    [DataRow("phone-input-own", Own, CascadedTemplate)]
    public async Task BitParams_Icon_ShowsWhatTheComponentPicked(string section, string shown, string? notShown)
    {
        await Open();

        await AssertShows(section, shown, notShown);
    }

    [TestMethod]
    public async Task BitParams_Icon_IsTakenBackAndGivenAgainAsTheComponentPicksAndDropsItsOwn()
    {
        await Open();

        // An icon bound to nothing picks nothing, so the cascaded one fills in.
        await AssertShows("button-dynamic", Cascaded, Own);

        await Page.ClickAsync("#toggle-dynamic");
        await WaitFor("button-dynamic", Own);
        await AssertShows("button-dynamic", Own, Cascaded);

        await Page.ClickAsync("#toggle-dynamic");
        await WaitFor("button-dynamic", Cascaded);
        await AssertShows("button-dynamic", Cascaded, Own);
    }



    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive
    }

    private Task WaitFor(string section, string selector)
    {
        return Page.WaitForSelectorAsync($"#{section} {selector}", new() { State = WaitForSelectorState.Attached, Timeout = 5000 });
    }

    private async Task AssertShows(string section, string shown, string? notShown)
    {
        Assert.AreEqual(1, await Page.Locator($"#{section}").CountAsync(), $"#{section} is not on the page.");

        var shownCount = await Page.Locator($"#{section} {shown}").CountAsync();
        Assert.IsGreaterThan(0, shownCount, $"#{section} does not show {shown}.");

        // A glyph is drawn by its font, so one the stylesheet does not know would be an empty box.
        if (shown.StartsWith(".bit-icon--"))
        {
            var glyph = await Page.Locator($"#{section} {shown}").First.EvaluateAsync<string>("e => getComputedStyle(e, '::before').content");
            Assert.IsFalse(glyph is "none" or "normal" or "\"\"", $"#{section} {shown} draws no glyph ({glyph}).");
        }

        if (notShown is null) return;

        var notShownCount = await Page.Locator($"#{section} {notShown}").CountAsync();
        Assert.AreEqual(0, notShownCount, $"#{section} shows {notShown}, which the component's own choice outranks.");
    }
}
