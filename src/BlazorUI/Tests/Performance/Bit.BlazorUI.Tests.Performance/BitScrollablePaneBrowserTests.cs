using System;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser tests of what <see cref="BitScrollablePane"/> does in a real layout: its scrolling API (ScrollBy, ScrollTo,
/// ScrollToElement, ScrollToStart, ScrollToEnd) actually moves the pane, the mouse wheel scrolls it, the Fade marks
/// exactly the edges that still have content beyond them, and the position and the edges reached are reported back
/// through OnScroll and the OnReached* callbacks. The panes of <c>/regression/scrollable-pane-behaviour</c> show 300 by
/// 200 pixels (a 1px border included) of 1000 by 1000 pixels of content, with a 50px target 600px from the left and
/// 500px from the top of it; one of them reads right to left, where it starts at the right edge of its content.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitScrollablePaneBrowserTests
public class BitScrollablePaneBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/scrollable-pane-behaviour";

    [TestMethod]
    public async Task BitScrollablePane_Fade_MarksTheEdgesWithContentBeyondThem()
    {
        await Open();

        // Both panes start at the start of their content, which is the visual right edge of the right-to-left one.
        await ExpectFaded("ltr", "bottom,right");
        await ExpectFaded("rtl", "bottom,left");

        // The marks are what the mask is drawn from: a faded edge has a band of the fade's size, the others none.
        var bands = await Pane("ltr").EvaluateAsync<string[]>(@"el => {
            const s = getComputedStyle(el);
            return ['--bit-scp-fbs', '--bit-scp-fbe', '--bit-scp-fis', '--bit-scp-fie'].map(n => s.getPropertyValue(n).trim());
        }");
        var maskSupported = await Page.EvaluateAsync<bool>("() => CSS.supports('mask-composite', 'intersect')");
        if (maskSupported)
        {
            Assert.AreEqual("0px", bands[0], "no band at the top");
            Assert.AreNotEqual("0px", bands[1], "a band at the bottom");
            Assert.AreEqual("0px", bands[2], "no band on the left");
            Assert.AreNotEqual("0px", bands[3], "a band on the right");
        }

        await Click("ltr", "scroll-by");
        await ExpectFaded("ltr", "top,bottom,left,right");

        await Click("ltr", "scroll-to-end");
        await ExpectFaded("ltr", "top,left");

        await Click("ltr", "scroll-to-start");
        await ExpectFaded("ltr", "bottom,right");
    }

    [TestMethod]
    public async Task BitScrollablePane_ScrollByAndScrollTo_MoveThePaneAndReportIt()
    {
        await Open();

        await Click("ltr", "scroll-by");
        await ExpectPosition("ltr", 100, 150);

        await Click("ltr", "scroll-by");
        await ExpectPosition("ltr", 200, 300);

        await Click("ltr", "scroll-to");
        await ExpectPosition("ltr", 250, 300);
    }

    [TestMethod]
    public async Task BitScrollablePane_ScrollToEndAndStart_ReachTheEdgesAndReportThem()
    {
        await Open();

        var (maxX, maxY) = await Max("ltr");

        await Click("ltr", "scroll-to-end");
        await ExpectPosition("ltr", maxX, maxY);
        await Expect(Report("ltr", "reached-bottom")).ToHaveTextAsync("1");
        await Expect(Report("ltr", "reached-right")).ToHaveTextAsync("1");

        await Click("ltr", "scroll-to-start");
        await ExpectPosition("ltr", 0, 0);
        await Expect(Report("ltr", "reached-top")).ToHaveTextAsync("1");
        await Expect(Report("ltr", "reached-left")).ToHaveTextAsync("1");
        await Expect(Report("ltr", "reached-bottom")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task BitScrollablePane_ScrollToElement_BringsTheTargetToTheStartOrTheCenter()
    {
        await Open();

        await Click("ltr", "scroll-to-element");
        await ExpectPosition("ltr", 600, 500);

        var start = await TargetInPane("ltr");
        Assert.AreEqual(0, start.Left, 1, "the target is at the left edge of the pane");
        Assert.AreEqual(0, start.Top, 1, "the target is at the top edge of the pane");

        await Click("ltr", "scroll-to-element-center");

        var (clientWidth, clientHeight) = await Client("ltr");
        await ExpectPosition("ltr", 600 - (clientWidth - 50) / 2, 500 - (clientHeight - 50) / 2);

        var center = await TargetInPane("ltr");
        Assert.AreEqual((clientWidth - 50) / 2, center.Left, 1, "the target is centered across");
        Assert.AreEqual((clientHeight - 50) / 2, center.Top, 1, "the target is centered down");
    }

    [TestMethod]
    public async Task BitScrollablePane_MouseWheel_ScrollsThePaneAndIsReported()
    {
        await Open();

        var box = await Pane("ltr").BoundingBoxAsync();
        Assert.IsNotNull(box);

        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.WheelAsync(0, 240);

        // How far one turn of the wheel goes is the engine's own (Firefox scales it), but wherever it took the pane,
        // what is reported is where it is once it has come to rest, and the top edge now has content beyond it.
        await Page.WaitForFunctionAsync(@"() => {
                const el = document.querySelector(`[data-probe='ltr'] .bit-scp`);
                const reported = document.querySelector(`[data-probe='ltr'] .report .top`).textContent;
                return el.scrollTop > 100 && reported === String(Math.round(el.scrollTop));
            }", null, new() { Timeout = DefaultTimeout });

        await ExpectFaded("ltr", "top,bottom,right");
        await Expect(Report("ltr", "scrolls")).ToHaveTextAsync("some");
    }

    [TestMethod]
    public async Task BitScrollablePane_RightToLeft_CountsFromTheLeftAndEndsOnTheLeft()
    {
        await Open();

        var (maxX, maxY) = await Max("rtl");

        // A position is counted from the visual left edge whichever way the pane reads...
        await Click("rtl", "scroll-to");
        await ExpectPosition("rtl", 250, 300);

        // ...and a positive distance always moves it rightwards.
        await Click("rtl", "scroll-by");
        await ExpectPosition("rtl", 350, 450);

        // The end of the content is its visual left edge.
        await Click("rtl", "scroll-to-end");
        await ExpectPosition("rtl", 0, maxY);
        await ExpectFaded("rtl", "top,right");
        await Expect(Report("rtl", "reached-left")).ToHaveTextAsync("1");

        await Click("rtl", "scroll-to-start");
        await ExpectPosition("rtl", maxX, 0);
        await ExpectFaded("rtl", "bottom,left");
        await Expect(Report("rtl", "reached-right")).ToHaveTextAsync("1");

        // The start of an element is its right edge there: the target's right edge meets the pane's.
        await Click("rtl", "scroll-to-element");
        var (clientWidth, _) = await Client("rtl");
        await ExpectPosition("rtl", 650 - clientWidth, 500);

        var target = await TargetInPane("rtl");
        Assert.AreEqual(clientWidth - 50, target.Left, 1, "the target is at the right edge of the pane");
        Assert.AreEqual(0, target.Top, 1);
    }



    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready");

        // The fade is drawn once the browser half has measured the pane, which is what says it is listening.
        await ExpectFaded("ltr", "bottom,right");
    }

    private ILocator Probe(string probe) => Page.Locator($"[data-probe='{probe}']");

    private ILocator Pane(string probe) => Probe(probe).Locator(".bit-scp");

    private ILocator Report(string probe, string name) => Probe(probe).Locator($".report .{name}");

    private Task Click(string probe, string button) => Probe(probe).Locator($".buttons .{button}").ClickAsync();

    private Task ExpectFaded(string probe, string edges)
    {
        return Page.WaitForFunctionAsync(@"([probe, edges]) => {
                const el = document.querySelector(`[data-probe='${probe}'] .bit-scp`);
                const faded = [['top', 'ft'], ['bottom', 'fb'], ['left', 'fl'], ['right', 'fr']]
                    .filter(([, a]) => el.hasAttribute(`data-bit-scp-${a}`)).map(([name]) => name).join(',');
                return faded === edges;
            }", new object[] { probe, edges }, new() { Timeout = DefaultTimeout });
    }

    // Where the pane stands, counted from the visual left edge of its content, both on the screen and as reported.
    private async Task ExpectPosition(string probe, double left, double top)
    {
        await Page.WaitForFunctionAsync(@"([probe, left, top]) => {
                const el = document.querySelector(`[data-probe='${probe}'] .bit-scp`);
                const max = el.scrollWidth - el.clientWidth;
                const x = getComputedStyle(el).direction === 'rtl' ? max + el.scrollLeft : el.scrollLeft;
                return Math.abs(x - left) < 1 && Math.abs(el.scrollTop - top) < 1;
            }", new object[] { probe, left, top }, new() { Timeout = DefaultTimeout });

        await Expect(Report(probe, "left")).ToHaveTextAsync(Math.Round(left).ToString());
        await Expect(Report(probe, "top")).ToHaveTextAsync(Math.Round(top).ToString());
    }

    private async Task<(double X, double Y)> Max(string probe)
    {
        var max = await Pane(probe).EvaluateAsync<double[]>("el => [el.scrollWidth - el.clientWidth, el.scrollHeight - el.clientHeight]");

        return (max[0], max[1]);
    }

    private async Task<(double Width, double Height)> Client(string probe)
    {
        var size = await Pane(probe).EvaluateAsync<double[]>("el => [el.clientWidth, el.clientHeight]");

        return (size[0], size[1]);
    }

    // The target's place inside the visible area of the pane (inside its border).
    private async Task<(double Left, double Top)> TargetInPane(string probe)
    {
        var at = await Pane(probe).EvaluateAsync<double[]>(@"el => {
            const p = el.getBoundingClientRect();
            const t = el.querySelector('.target').getBoundingClientRect();
            return [t.left - p.left - el.clientLeft, t.top - p.top - el.clientTop];
        }");

        return (at[0], at[1]);
    }
}
