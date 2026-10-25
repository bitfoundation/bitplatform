using System;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser tests of what a reader does with a <see cref="BitSwiper"/>, driven with real pointer and keyboard input:
/// a mouse drag far enough carries the swiper on to the next item and a short one snaps it back, the previous and
/// next buttons and the arrow, Home and End keys move it, a right-to-left swiper has its next item on the left and
/// turns the drag and the arrows around, and every move is reported back through OnChange, OnReachEnd and
/// OnReachStart. The swipers of <c>/regression/swiper-behaviour</c> show one 300px item at a time out of six and
/// snap to the start of an item, so item n rests at a scroll of n * 300px.
/// </summary>
/// <remarks>
/// A touch swipe is left to the browser's own scrolling (only the mouse is dragged by the component), so it is not
/// driven here.
/// </remarks>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitSwiperBrowserTests
public class BitSwiperBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/swiper-behaviour";

    [TestMethod]
    public async Task BitSwiper_NextAndPreviousButtons_MoveOneItemAndReportIt()
    {
        await Open();

        await Expect(Prev("ltr")).ToBeHiddenAsync();

        await Next("ltr").ClickAsync();

        await ExpectAt("ltr", 1);
        await Expect(Report("ltr", "changes")).ToHaveTextAsync("1");
        await Expect(Prev("ltr")).ToBeVisibleAsync();

        await Next("ltr").ClickAsync();
        await ExpectAt("ltr", 2);

        await Prev("ltr").ClickAsync();
        await ExpectAt("ltr", 1);

        await Prev("ltr").ClickAsync();
        await ExpectAt("ltr", 0);
        await Expect(Report("ltr", "reached-start")).ToHaveTextAsync("1");
        await Expect(Prev("ltr")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task BitSwiper_MouseDrag_PastHalfAnItemMovesOnAndAShortOneSnapsBack()
    {
        await Open();

        // A third of an item is not far enough: the swiper snaps back to where it was.
        await Drag("ltr", -100);
        await ExpectScroll("ltr", 0);
        await Expect(Report("ltr", "index")).ToHaveTextAsync("0");
        await Expect(Report("ltr", "changes")).ToHaveTextAsync("0");

        // Two thirds is: it comes to rest on the next item.
        await Drag("ltr", -200);
        await ExpectAt("ltr", 1);

        // And back again.
        await Drag("ltr", 200);
        await ExpectAt("ltr", 0);
    }

    [TestMethod]
    public async Task BitSwiper_Keyboard_ArrowsHomeAndEndMoveTheSwiper()
    {
        await Open();

        await TabToSwiper("ltr");

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectAt("ltr", 1);

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectAt("ltr", 2);

        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectAt("ltr", 1);

        await Page.Keyboard.PressAsync("End");
        await ExpectAt("ltr", 5);
        await Expect(Report("ltr", "reached-end")).ToHaveTextAsync("1");
        await Expect(Next("ltr")).ToBeHiddenAsync();

        await Page.Keyboard.PressAsync("Home");
        await ExpectAt("ltr", 0);
        await Expect(Report("ltr", "reached-start")).ToHaveTextAsync("1");
    }

    [TestMethod]
    [DataRow("own", DisplayName = "Dir=Rtl")]
    [DataRow("inherited", DisplayName = "inside dir=rtl")]
    public async Task BitSwiper_RightToLeft_HasItsNextItemOnTheLeft(string probe)
    {
        await Open();

        var lefts = await Probe(probe).EvaluateAsync<double[]>(
            "el => [...el.querySelectorAll('.bit-swpi')].slice(0, 2).map(i => i.getBoundingClientRect().left)");
        Assert.IsTrue(lefts[1] < lefts[0], "the second item is laid out to the left of the first one");

        await Next(probe).ClickAsync();
        await ExpectAt(probe, 1);

        await Prev(probe).ClickAsync();
        await ExpectAt(probe, 0);

        // Dragging the content to the right brings in what is on the left: the next item.
        await Drag(probe, 200);
        await ExpectAt(probe, 1);

        await Drag(probe, -200);
        await ExpectAt(probe, 0);

        await TabToSwiper(probe);

        // The arrows move the view the way they point.
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectAt(probe, 1);

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectAt(probe, 0);
    }



    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready");

        // The next button is only shown once the browser half has measured the swiper and found somewhere to go.
        foreach (var probe in new[] { "ltr", "own", "inherited" })
        {
            await Expect(Next(probe)).ToBeVisibleAsync();
        }
    }

    private ILocator Probe(string probe) => Page.Locator($"[data-probe='{probe}']");

    private ILocator Next(string probe) => Probe(probe).Locator(".bit-swp-rbt");

    private ILocator Prev(string probe) => Probe(probe).Locator(".bit-swp-lbt");

    private ILocator Report(string probe, string name) => Probe(probe).Locator($".report .{name}");

    // What the reader sees and what the page is told: the item standing in the viewport, the scroll that puts it
    // there, the current item marker and the index reported through OnChange.
    private async Task ExpectAt(string probe, int index)
    {
        await Expect(Report(probe, "index")).ToHaveTextAsync(index.ToString());
        await ExpectScroll(probe, index * 300);
        await Expect(Probe(probe).Locator(".bit-swpi").Nth(index)).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bbit-swpi-cur\b"));

        var offset = await Probe(probe).EvaluateAsync<double>(
            "(el, i) => el.querySelectorAll('.bit-swpi')[i].getBoundingClientRect().left - el.querySelector('.bit-swp-cnt').getBoundingClientRect().left",
            index);
        Assert.AreEqual(0, offset, 1, $"item {index} fills the viewport");
    }

    // The scroll the swiper settles at once its glide is over, as a distance from the start of its content.
    private Task ExpectScroll(string probe, double expected)
    {
        return Page.WaitForFunctionAsync(
            "([probe, expected]) => Math.abs(Math.abs(document.querySelector(`[data-probe='${probe}'] .bit-swp-cnt`).scrollLeft) - expected) < 1",
            new object[] { probe, expected },
            new() { Timeout = DefaultTimeout });
    }

    // A mouse drag across the swiper, from a quarter of the way in from the edge it moves away from: the whole drag
    // stays inside the window (a release outside of it is never delivered) and off the previous and next buttons
    // drawn over the edges. The pointer comes to rest before it is let go, so the drag
    // ends without the flick a fast release carries on with, and where it lands depends on how far it went alone.
    private async Task Drag(string probe, float dx)
    {
        var container = Probe(probe).Locator(".bit-swp-cnt");
        await container.ScrollIntoViewIfNeededAsync();

        var box = await container.BoundingBoxAsync();
        Assert.IsNotNull(box);

        var x = dx < 0 ? box.X + box.Width * 0.75f : box.X + box.Width * 0.25f;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + dx, y, new() { Steps = 12 });
        await Page.WaitForTimeoutAsync(80);
        await Page.Mouse.MoveAsync(x + dx + Math.Sign(dx), y);
        await Page.Mouse.UpAsync();
    }

    // The keyboard reaches the swiper the way a reader's does: from the control before it.
    private async Task TabToSwiper(string probe)
    {
        await Probe(probe).Locator(".before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Probe(probe).Locator(".bit-swp")).ToBeFocusedAsync();
    }
}
