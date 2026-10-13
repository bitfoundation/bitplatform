using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the teardown of the components whose parts unregister from them while they are
/// disposed, and of the ones their script calls back (/regression/teardown).
///
/// In a browser a component's disposal awaits a real round trip to its script, and the renderer disposes the parts
/// the component rendered while that round trip is in flight - the window bUnit can only imitate. Nothing the
/// component raises may come of its teardown: no selection moved off each part in turn, no re-read of the server, no
/// dismissal, no error on the page or on the server, and the circuit has to survive to render the component again.
/// A part removed from a component that stays is still reacted to as before, and the callbacks the script sends a
/// live component still arrive. Pinned down in every engine (BROWSER=chromium, firefox, webkit).
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitTeardownBrowserTests
public class BitTeardownBrowserTests : PerformanceTestBase
{
    private static readonly HttpClient _http = new();

    private readonly List<string> _pageErrors = [];

    [TestMethod]
    [DataRow("accordion-list")]
    [DataRow("button-group")]
    [DataRow("pivot")]
    [DataRow("choice-group")]
    [DataRow("dropdown")]
    [DataRow("carousel")]
    [DataRow("swiper")]
    [DataRow("timeline")]
    [DataRow("breadcrumb")]
    [DataRow("nav")]
    [DataRow("navbar")]
    [DataRow("menu-button")]
    [DataRow("data-grid")]
    [DataRow("modal")]
    [DataRow("overlay")]
    [DataRow("panel")]
    [DataRow("tooltip")]
    [DataRow("message")]
    [DataRow("snack-bar")]
    [DataRow("scrollable-pane")]
    [DataRow("header")]
    [DataRow("footer")]
    [DataRow("basic-list")]
    [DataRow("media-query")]
    [DataRow("pull-to-refresh")]
    [DataRow("sticky")]
    [DataRow("app-shell")]
    [DataRow("country-picker")]
    [DataRow("phone-input")]
    [DataRow("infinite-scrolling")]
    [DataRow("markdown-editor")]
    [DataRow("virtualize")]
    [DataRow("file-upload")]
    [DataRow("otp-input")]
    public async Task Component_GoesAwayWithItsPartsWithoutRaisingAnything(string part)
    {
        await OpenPage(part);

        // Three times over, so a teardown that only breaks once the circuit has seen one shows up too.
        for (var cycle = 0; cycle < 3; cycle++)
        {
            await Activate(part);
            await Settle();

            var log = await Text("#log");
            var count = await Text("#count");
            var reads = await Text("#reads");

            await Press("#btn-unmount");
            await Expect(Page.Locator("#mounted")).ToHaveTextAsync("no");
            await Expect(Page.Locator("#part")).ToHaveCountAsync(0);

            // Give the disposal its round trips to the script, and a wrongly raised callback the time to land.
            await Page.WaitForTimeoutAsync(750);

            Assert.AreEqual(count, await Text("#count"), $"cycle {cycle}: the teardown raised [{Added(log, await Text("#log"))}]");
            Assert.AreEqual(reads, await Text("#reads"), $"cycle {cycle}: the teardown read the server again");

            await Press("#btn-remount");
            await Expect(Page.Locator("#mounted")).ToHaveTextAsync("yes");
            await Expect(Page.Locator("#part")).ToHaveCountAsync(1);
        }

        await AssertNoErrors();
    }

    [TestMethod]
    [DataRow("accordion-list", "ExpandedKey:")]
    [DataRow("button-group", "ToggleKey:")]
    [DataRow("pivot", "SelectedKey:two")]
    public async Task Component_ThatStays_StillMovesItsSelectionOffARemovedPart(string part, string expected)
    {
        await OpenPage(part);
        await Settle();

        await Press("#btn-remove-first");

        await Expect(Page.Locator("#log")).ToContainTextAsync(expected);
        await Expect(Page.Locator("#part")).ToContainTextAsync("Second");
        await Expect(Page.Locator("#part")).Not.ToContainTextAsync("First");

        await AssertNoErrors();
    }

    [TestMethod]
    [DataRow("choice-group")]
    [DataRow("carousel")]
    [DataRow("swiper")]
    [DataRow("timeline")]
    [DataRow("breadcrumb")]
    [DataRow("nav")]
    [DataRow("navbar")]
    public async Task Component_ThatStays_StillDropsARemovedPart(string part)
    {
        await OpenPage(part);
        await Settle();

        await Press("#btn-remove-first");

        await Expect(Page.Locator("#part")).Not.ToContainTextAsync("First");
        await Expect(Page.Locator("#part")).ToContainTextAsync("Second");

        // The component is still alive and answering: remounting it renders the first part again.
        await Press("#btn-unmount");
        await Press("#btn-remount");
        await Expect(Page.Locator("#part")).ToContainTextAsync("First");

        await AssertNoErrors();
    }

    [TestMethod]
    [DataRow("dropdown")]
    [DataRow("menu-button")]
    public async Task Callout_ThatStays_StillDropsARemovedPart(string part)
    {
        await OpenPage(part);
        await Settle();

        await Press("#btn-remove-first");
        await Settle();

        await Activate(part);

        await Expect(Page.GetByText("Second", new() { Exact = true }).First).ToBeVisibleAsync();
        await Expect(Page.GetByText("First", new() { Exact = true })).ToHaveCountAsync(0);

        await AssertNoErrors();
    }

    [TestMethod]
    public async Task DataGrid_ThatStays_StillReadsAgainWhenTheSortedColumnIsRemoved()
    {
        await OpenPage("data-grid");

        await SortGrid();

        var reads = int.Parse(await Text("#reads"));

        await Press("#btn-remove-first");

        await Expect(Page.Locator("#reads")).ToHaveTextAsync((reads + 1).ToString());

        await AssertNoErrors();
    }

    [TestMethod]
    [DataRow("modal")]
    [DataRow("overlay")]
    [DataRow("panel")]
    public async Task Surface_ThatStays_StillClosesOnEscape(string part)
    {
        await OpenPage(part);
        await Settle();

        await Page.Locator($"#{part}-content").ClickAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#log")).ToContainTextAsync("IsOpen:False");

        await AssertNoErrors();
    }

    [TestMethod]
    public async Task Tooltip_ThatStays_StillShowsAndHides()
    {
        await OpenPage("tooltip");

        await Page.Locator("#tooltip-anchor").HoverAsync();
        await Expect(Page.Locator("#log")).ToContainTextAsync("OnShow");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#log")).ToContainTextAsync("OnHide");

        await AssertNoErrors();
    }

    [TestMethod]
    public async Task ScrollablePane_ThatStays_StillReportsItsScroll()
    {
        await OpenPage("scrollable-pane");

        await Page.Locator("#pane").EvaluateAsync("el => el.scrollTop = 2000");

        await Expect(Page.Locator("#log")).ToContainTextAsync("OnScroll");
        await Expect(Page.Locator("#log")).ToContainTextAsync("OnReachedBottom");

        await AssertNoErrors();
    }

    [TestMethod]
    public async Task Header_ThatStays_StillReportsThePageScroll()
    {
        await OpenPage("header");

        await Page.EvaluateAsync("() => window.scrollTo(0, 500)");

        await Expect(Page.Locator("#log")).ToContainTextAsync("OnScrolledChanged:True");

        await AssertNoErrors();
    }

    [TestMethod]
    public async Task MediaQuery_ThatStays_StillReportsAChangeOfTheViewport()
    {
        await Page.SetViewportSizeAsync(1000, 700);
        await OpenPage("media-query");

        await Page.SetViewportSizeAsync(600, 700);

        await Expect(Page.Locator("#log")).ToContainTextAsync("OnChange:False");

        await AssertNoErrors();
    }

    [TestMethod]
    public async Task OtpInput_ThatStays_StillTakesAPastedCode()
    {
        await OpenPage("otp-input");

        await Page.Locator("#otp input").First.FocusAsync();
        await Page.Keyboard.TypeAsync("1234", new() { Delay = 100 });

        await Expect(Page.Locator("#log")).ToContainTextAsync("Value:1234");
        await Expect(Page.Locator("#log")).ToContainTextAsync("OnFill:1234");

        await AssertNoErrors();
    }

    [TestMethod]
    public async Task SnackBar_ThatStays_StillShowsAndDismisses()
    {
        await OpenPage("snack-bar");

        await Expect(Page.Locator("#log")).ToContainTextAsync("OnShow:Snack");

        await Page.Locator(".bit-snb-cbt").First.ClickAsync();

        await Expect(Page.Locator("#log")).ToContainTextAsync("OnDismiss:Snack");

        await AssertNoErrors();
    }

    [TestMethod]
    public async Task InfiniteScrolling_ThatStays_StillLoadsItsItems()
    {
        await OpenPage("infinite-scrolling");

        await Expect(Page.Locator("#log")).ToContainTextAsync("OnItemsLoaded:20");

        await AssertNoErrors();
    }



    private async Task OpenPage(string part)
    {
        await _http.DeleteAsync($"{BaseUrl}/api/server-errors");

        await Page.GotoAsync($"{BaseUrl}/regression/teardown?part={part}");
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

    // Puts the component in the state its teardown has the most to undo in: a popup open, a tooltip shown, a pane
    // scrolled, a grid sorted on the column that goes away with it.
    private async Task Activate(string part)
    {
        switch (part)
        {
            case "dropdown":
                await Page.Locator("#dropdown [role=combobox]").ClickAsync();
                await Expect(Page.Locator("#dropdown [role=combobox]")).ToHaveAttributeAsync("aria-expanded", "true");
                break;

            case "menu-button":
                await Page.Locator("#menu-button button").First.ClickAsync();
                break;

            case "country-picker":
                await Page.Locator("#country-picker").ClickAsync();
                await Expect(Page.Locator("#log")).ToContainTextAsync("OnOpen");
                break;

            case "phone-input":
                await Page.Locator("#phone-input button").First.ClickAsync();
                break;

            case "tooltip":
                await Page.Locator("#tooltip-anchor").HoverAsync();
                await Expect(Page.Locator("#log")).ToContainTextAsync("OnShow");
                break;

            case "scrollable-pane":
                await Page.Locator("#pane").EvaluateAsync("el => el.scrollTop = 300");
                break;

            case "header":
            case "footer":
            case "sticky":
                await Page.EvaluateAsync("() => window.scrollTo(0, 400)");
                break;

            case "data-grid":
                await SortGrid();
                break;
        }
    }

    private async Task SortGrid()
    {
        var reads = int.Parse(await Text("#reads"));

        await Press("#btn-sort");

        await Expect(Page.Locator("#reads")).Not.ToHaveTextAsync(reads.ToString());
    }

    private async Task AssertNoErrors()
    {
        var errors = _pageErrors.Where(e => e.Contains("favicon", System.StringComparison.OrdinalIgnoreCase) is false).ToList();

        Assert.AreEqual(0, errors.Count, $"the page reported: {string.Join(" || ", errors)}");

        var json = await _http.GetStringAsync($"{BaseUrl}/api/server-errors");
        var serverErrors = JsonSerializer.Deserialize<string[]>(json) ?? [];

        Assert.AreEqual(0, serverErrors.Length, $"the server logged: {string.Join(" || ", serverErrors)}");
    }

    // The controls of the page are clicked by an event of their own rather than with the pointer: an open popup or
    // a modal's overlay would take a real click, and dismissing what is open is not what the control is for.
    private Task Press(string selector) => Page.Locator(selector).DispatchEventAsync("click");

    private Task<string> Text(string selector) => Page.Locator(selector).InnerTextAsync();

    private static string Added(string before, string after) => after.Length > before.Length ? after[before.Length..].TrimStart('|') : after;

    // The components register with their scripts in calls of their own once they have rendered, which nothing on the
    // page reports; an action taken before they land is one nothing was listening for yet.
    private Task Settle() => Page.WaitForTimeoutAsync(500);
}
